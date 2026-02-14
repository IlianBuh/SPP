using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using MyTestFramework;
using MyProject.Tests;

namespace TestRunner
{
    using MyTestFramework.Attributes;
    using MyTestFramework.Exceptions;

    public class TestRunner
    {
        private int totalTests = 0;
        private int passedTests = 0;
        private int failedTests = 0;
        private int brokenTests = 0;
        private int ignoredTests = 0;
        private Stopwatch stopwatch = new Stopwatch();

        public bool RunTests()
        {
            Console.WriteLine("=== MyTestFramework Test Runner ===\n");
            stopwatch.Start();

            try
            {
                var assembly = Assembly.GetAssembly(typeof(DriverServiceTests));
                Console.WriteLine($"Loaded assembly: {assembly.GetName().Name}\n");

                var testClasses = FindTestClasses(assembly);
                if (testClasses.Count == 0)
                {
                    Console.WriteLine("No test classes found.");
                    return false;
                }

                Console.WriteLine($"Found {testClasses.Count} test class(es)\n");

                var sharedContext = new Dictionary<string, object>();

                foreach (var testClass in testClasses)
                {
                    RunTestClass(testClass, sharedContext);
                }

                stopwatch.Stop();

                PrintSummary();

                return failedTests == 0 && brokenTests == 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nFatal error during test execution: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                return false;
            }
        }

        private List<Type> FindTestClasses(Assembly assembly)
        {
            var testClasses = new List<Type>();
            foreach (var type in assembly.GetTypes())
            {
                if (type.GetCustomAttribute<TestClassAttribute>() != null)
                {
                    testClasses.Add(type);
                }
            }
            return testClasses;
        }

        private void RunTestClass(Type testClassType, Dictionary<string, object> sharedContext)
        {
            Console.WriteLine($"Running tests in class: {testClassType.Name}");

            var classInitialize = FindMethodWithAttribute(testClassType, typeof(ClassInitializeAttribute));
            var classCleanup = FindMethodWithAttribute(testClassType, typeof(ClassCleanupAttribute));

            var testMethods = FindTestMethods(testClassType);
            if (testMethods.Count == 0)
            {
                Console.WriteLine("  No test methods found.\n");
                return;
            }

            object classInstance = null;
            if (classInitialize != null)
            {
                try
                {
                    classInstance = Activator.CreateInstance(testClassType);
                    InvokeMethod(classInstance, classInitialize);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  ERROR: ClassInitialize failed: {ex.Message}\n");
                    return;
                }
            }

            foreach (var testMethod in testMethods)
            {
                RunTestMethod(testClassType, testMethod, sharedContext);
            }

            if (classCleanup != null && classInstance != null)
            {
                try
                {
                    InvokeMethod(classInstance, classCleanup);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  WARNING: ClassCleanup failed: {ex.Message}");
                }
            }

            Console.WriteLine();
        }

        private List<MethodInfo> FindTestMethods(Type testClassType)
        {
            var testMethods = new List<MethodInfo>();
            var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

            foreach (var method in testClassType.GetMethods(bindingFlags))
            {
                if (method.GetCustomAttribute<TestMethodAttribute>() != null)
                {
                    testMethods.Add(method);
                }
            }

            return testMethods;
        }

        private MethodInfo FindMethodWithAttribute(Type type, Type attributeType)
        {
            var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
            foreach (var method in type.GetMethods(bindingFlags))
            {
                if (method.GetCustomAttribute(attributeType) != null)
                {
                    return method;
                }
            }
            return null;
        }

        private void RunTestMethod(Type testClassType, MethodInfo testMethod, Dictionary<string, object> sharedContext)
        {
            var testMethodAttr = testMethod.GetCustomAttribute<TestMethodAttribute>();
            var testName = testMethodAttr?.DisplayName ?? testMethod.Name;

            if (testMethodAttr?.Ignore == true)
            {
                PrintTestResult(testName, TestResult.Ignored, null);
                ignoredTests++;
                totalTests++;
                return;
            }

            var parameters = testMethod.GetParameters();
            var dataRows = testMethod.GetCustomAttributes<DataRowAttribute>().ToList();

            if (parameters.Length > 0 && dataRows.Count == 0)
            {
                PrintTestResult(testName, TestResult.ConfigurationError, 
                    "Method requires parameters but no DataRow attributes found");
                brokenTests++;
                totalTests++;
                return;
            }

            if (parameters.Length == 0)
            {
                ExecuteSingleTest(testClassType, testMethod, testName, sharedContext);
            }
            else
            {
                foreach (var dataRow in dataRows)
                {
                    if (dataRow.Data.Length != parameters.Length)
                    {
                        PrintTestResult($"{testName} (DataRow)", TestResult.ConfigurationError,
                            $"DataRow has {dataRow.Data.Length} values but method requires {parameters.Length} parameters");
                        brokenTests++;
                        totalTests++;
                        continue;
                    }

                    ExecuteSingleTest(testClassType, testMethod, $"{testName} (DataRow)", sharedContext, dataRow.Data);
                }
            }
        }

        private void ExecuteSingleTest(Type testClassType, MethodInfo testMethod, string testName, 
            Dictionary<string, object> sharedContext, object[] parameters = null)
        {
            totalTests++;
            object instance = null;

            try
            {
                IContext context = null;
                if (sharedContext.ContainsKey("__Context"))
                {
                    context = (IContext)sharedContext["__Context"];
                }
                else
                {
                    context = new DefaultContext();
                    sharedContext["__Context"] = context;
                }

                var constructorWithContext = testClassType.GetConstructor(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                    null,
                    new[] { typeof(IContext) },
                    null);

                if (constructorWithContext != null)
                {
                    instance = constructorWithContext.Invoke(new object[] { context });
                }
                else
                {
                    instance = Activator.CreateInstance(testClassType);
                    InjectContext(instance, sharedContext);
                }

                var testInitialize = FindMethodWithAttribute(testClassType, typeof(TestInitializeAttribute));
                if (testInitialize != null)
                {
                    InvokeMethod(instance, testInitialize);
                }

                object result = InvokeMethod(instance, testMethod, parameters);

                if (result is Task task)
                {
                    task.GetAwaiter().GetResult();
                }
                else if (result != null && result.GetType().IsGenericType && 
                         result.GetType().GetGenericTypeDefinition() == typeof(Task<>))
                {
                    var getResultMethod = result.GetType().GetMethod("GetAwaiter")?.Invoke(result, null);
                    getResultMethod?.GetType().GetMethod("GetResult")?.Invoke(getResultMethod, null);
                }

                var testCleanup = FindMethodWithAttribute(testClassType, typeof(TestCleanupAttribute));
                if (testCleanup != null)
                {
                    InvokeMethod(instance, testCleanup);
                }

                PrintTestResult(testName, TestResult.Passed, null);
                passedTests++;
            }
            catch (TargetInvocationException ex)
            {
                HandleTestException(testName, ex.InnerException ?? ex);
            }
            catch (Exception ex)
            {
                HandleTestException(testName, ex);
            }
        }

        private void InjectContext(object instance, Dictionary<string, object> sharedContext)
        {
            IContext context;
            if (!sharedContext.ContainsKey("__Context"))
            {
                context = new DefaultContext();
                sharedContext["__Context"] = context;
            }
            else
            {
                context = (IContext)sharedContext["__Context"];
            }

            var properties = instance.GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var property in properties)
            {
                if (property.GetCustomAttribute<InjectContextAttribute>() != null)
                {
                    if (property.PropertyType == typeof(IContext) || typeof(IContext).IsAssignableFrom(property.PropertyType))
                    {
                        if (property.CanWrite)
                        {
                            property.SetValue(instance, context);
                        }
                    }
                }
            }
        }

        private void HandleTestException(string testName, Exception ex)
        {
            if (ex is TestAssertionException)
            {
                PrintTestResult(testName, TestResult.Failed, ex.Message);
                failedTests++;
            }
            else if (ex is TestConfigurationException)
            {
                PrintTestResult(testName, TestResult.ConfigurationError, ex.Message);
                brokenTests++;
            }
            else
            {
                PrintTestResult(testName, TestResult.Broken, $"{ex.GetType().Name}: {ex.Message}");
                brokenTests++;
            }
        }

        private object InvokeMethod(object instance, MethodInfo method, object[] parameters = null)
        {
            if (method.IsStatic)
            {
                return method.Invoke(null, parameters);
            }
            else
            {
                return method.Invoke(instance, parameters);
            }
        }

        private void PrintTestResult(string testName, TestResult result, string message)
        {
            switch (result)
            {
                case TestResult.Passed:
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"  ✓ PASSED: {testName}");
                    break;
                case TestResult.Failed:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"  ✗ FAILED: {testName}");
                    if (!string.IsNullOrEmpty(message))
                    {
                        Console.WriteLine($"    {message}");
                    }
                    break;
                case TestResult.Broken:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"  ✗ BROKEN: {testName}");
                    if (!string.IsNullOrEmpty(message))
                    {
                        Console.WriteLine($"    {message}");
                    }
                    break;
                case TestResult.Ignored:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"  ⊘ IGNORED: {testName}");
                    break;
                case TestResult.ConfigurationError:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"  ✗ CONFIG ERROR: {testName}");
                    if (!string.IsNullOrEmpty(message))
                    {
                        Console.WriteLine($"    {message}");
                    }
                    break;
            }
            Console.ResetColor();
        }

        private void PrintSummary()
        {
            Console.WriteLine("=== Test Summary ===");
            Console.WriteLine($"Total: {totalTests}");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Passed: {passedTests}");
            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Failed: {failedTests}");
            Console.WriteLine($"Broken: {brokenTests}");
            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Ignored: {ignoredTests}");
            Console.ResetColor();
            Console.WriteLine($"Time: {stopwatch.ElapsedMilliseconds} ms");
        }

        private enum TestResult
        {
            Passed,
            Failed,
            Broken,
            Ignored,
            ConfigurationError
        }
    }

    internal class DefaultContext : IContext
    {
        public Dictionary<string, object> Data { get; } = new Dictionary<string, object>();

        public void SetData(string key, object value)
        {
            Data[key] = value;
        }

        public object GetData(string key)
        {
            return Data.ContainsKey(key) ? Data[key] : null;
        }

        public bool ContainsData(string key)
        {
            return Data.ContainsKey(key);
        }

        public void RemoveData(string key)
        {
            if (Data.ContainsKey(key))
            {
                Data.Remove(key);
            }
        }
    }
}

