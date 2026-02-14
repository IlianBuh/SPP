using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
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
        private int timedOutTests = 0;

        private readonly int maxDegreeOfParallelism;
        private readonly object consoleLock = new object();
        private readonly List<string> testLog = new List<string>();

        public TestRunner(int maxDegreeOfParallelism = 0)
        {
            this.maxDegreeOfParallelism = maxDegreeOfParallelism > 0
                ? maxDegreeOfParallelism
                : Environment.ProcessorCount;
        }

        public bool RunTests()
        {
            Console.WriteLine("=== MyTestFramework Test Runner ===\n");
            Console.WriteLine($"Max Degree of Parallelism: {maxDegreeOfParallelism}\n");

            Assembly assembly;
            try
            {
                assembly = Assembly.GetAssembly(typeof(DriverServiceTests));
                Console.WriteLine($"Loaded assembly: {assembly.GetName().Name}\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nFatal error loading assembly: {ex.Message}");
                return false;
            }

            var testClasses = FindTestClasses(assembly);
            if (testClasses.Count == 0)
            {
                Console.WriteLine("No test classes found.");
                return false;
            }

            Console.WriteLine($"Found {testClasses.Count} test class(es)\n");

            // === PARALLEL RUN ===
            Console.WriteLine("────────────────────────────────────────");
            Console.WriteLine("  PARALLEL EXECUTION");
            Console.WriteLine("────────────────────────────────────────\n");

            ResetCounters();
            var parallelStopwatch = Stopwatch.StartNew();

            foreach (var testClass in testClasses)
            {
                RunTestClassParallel(testClass);
            }

            parallelStopwatch.Stop();
            var parallelTime = parallelStopwatch.ElapsedMilliseconds;

            PrintSummary("Parallel", parallelTime);
            var parallelResult = failedTests == 0 && brokenTests == 0;

            // === SEQUENTIAL RUN ===
            Console.WriteLine("\n────────────────────────────────────────");
            Console.WriteLine("  SEQUENTIAL EXECUTION");
            Console.WriteLine("────────────────────────────────────────\n");

            ResetCounters();
            var sequentialStopwatch = Stopwatch.StartNew();

            foreach (var testClass in testClasses)
            {
                RunTestClassSequential(testClass);
            }

            sequentialStopwatch.Stop();
            var sequentialTime = sequentialStopwatch.ElapsedMilliseconds;

            PrintSummary("Sequential", sequentialTime);

            // === COMPARISON ===
            Console.WriteLine("\n════════════════════════════════════════");
            Console.WriteLine("  PERFORMANCE COMPARISON");
            Console.WriteLine("════════════════════════════════════════");
            Console.WriteLine($"  Sequential: {sequentialTime} ms");
            Console.WriteLine($"  Parallel:   {parallelTime} ms");

            if (sequentialTime > 0 && parallelTime > 0)
            {
                var speedup = (double)sequentialTime / parallelTime;
                Console.WriteLine($"  Speedup:    {speedup:F2}x");
            }

            if (parallelTime < sequentialTime)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"  ✓ Parallel execution was faster by {sequentialTime - parallelTime} ms");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"  ⊘ Parallel execution was not faster (overhead may dominate for small test suites)");
            }
            Console.ResetColor();
            Console.WriteLine("════════════════════════════════════════\n");

            return parallelResult;
        }

        private void ResetCounters()
        {
            totalTests = 0;
            passedTests = 0;
            failedTests = 0;
            brokenTests = 0;
            ignoredTests = 0;
            timedOutTests = 0;
            testLog.Clear();
        }

        // ──────────────────────────────────────────
        //  PARALLEL: method-level parallelism
        // ──────────────────────────────────────────
        private void RunTestClassParallel(Type testClassType)
        {
            SafeWriteLine($"Running tests in class: {testClassType.Name}");

            var classInitialize = FindMethodWithAttribute(testClassType, typeof(ClassInitializeAttribute));
            var classCleanup = FindMethodWithAttribute(testClassType, typeof(ClassCleanupAttribute));
            var testMethods = FindTestMethods(testClassType);

            if (testMethods.Count == 0)
            {
                SafeWriteLine("  No test methods found.\n");
                return;
            }

            // ClassInitialize — runs once before all tests in the class
            object classInitInstance = null;
            if (classInitialize != null)
            {
                try
                {
                    classInitInstance = Activator.CreateInstance(testClassType);
                    InvokeMethod(classInitInstance, classInitialize);
                }
                catch (Exception ex)
                {
                    SafeWriteLine($"  ERROR: ClassInitialize failed: {ex.Message}\n");
                    return;
                }
            }

            // Run test methods in parallel, limited by SemaphoreSlim
            var semaphore = new SemaphoreSlim(maxDegreeOfParallelism);
            var tasks = new List<Task>();

            foreach (var testMethod in testMethods)
            {
                var method = testMethod; // capture
                tasks.Add(Task.Run(async () =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        RunTestMethod(testClassType, method, parallel: true);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }));
            }

            Task.WhenAll(tasks).GetAwaiter().GetResult();

            // ClassCleanup — runs once after all tests in the class
            if (classCleanup != null && classInitInstance != null)
            {
                try
                {
                    InvokeMethod(classInitInstance, classCleanup);
                }
                catch (Exception ex)
                {
                    SafeWriteLine($"  WARNING: ClassCleanup failed: {ex.Message}");
                }
            }

            SafeWriteLine("");
        }

        // ──────────────────────────────────────────
        //  SEQUENTIAL: original behavior
        // ──────────────────────────────────────────
        private void RunTestClassSequential(Type testClassType)
        {
            SafeWriteLine($"Running tests in class: {testClassType.Name}");

            var classInitialize = FindMethodWithAttribute(testClassType, typeof(ClassInitializeAttribute));
            var classCleanup = FindMethodWithAttribute(testClassType, typeof(ClassCleanupAttribute));
            var testMethods = FindTestMethods(testClassType);

            if (testMethods.Count == 0)
            {
                SafeWriteLine("  No test methods found.\n");
                return;
            }

            object classInitInstance = null;
            if (classInitialize != null)
            {
                try
                {
                    classInitInstance = Activator.CreateInstance(testClassType);
                    InvokeMethod(classInitInstance, classInitialize);
                }
                catch (Exception ex)
                {
                    SafeWriteLine($"  ERROR: ClassInitialize failed: {ex.Message}\n");
                    return;
                }
            }

            foreach (var testMethod in testMethods)
            {
                RunTestMethod(testClassType, testMethod, parallel: false);
            }

            if (classCleanup != null && classInitInstance != null)
            {
                try
                {
                    InvokeMethod(classInitInstance, classCleanup);
                }
                catch (Exception ex)
                {
                    SafeWriteLine($"  WARNING: ClassCleanup failed: {ex.Message}");
                }
            }

            SafeWriteLine("");
        }

        // ──────────────────────────────────────────
        //  Discovery helpers
        // ──────────────────────────────────────────
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

        // ──────────────────────────────────────────
        //  Test method execution
        // ──────────────────────────────────────────
        private void RunTestMethod(Type testClassType, MethodInfo testMethod, bool parallel)
        {
            var testMethodAttr = testMethod.GetCustomAttribute<TestMethodAttribute>();
            var testName = testMethodAttr?.DisplayName ?? testMethod.Name;

            if (testMethodAttr?.Ignore == true)
            {
                PrintTestResult(testName, TestResult.Ignored, null);
                Interlocked.Increment(ref ignoredTests);
                Interlocked.Increment(ref totalTests);
                return;
            }

            var parameters = testMethod.GetParameters();
            var dataRows = testMethod.GetCustomAttributes<DataRowAttribute>().ToList();

            if (parameters.Length > 0 && dataRows.Count == 0)
            {
                PrintTestResult(testName, TestResult.ConfigurationError,
                    "Method requires parameters but no DataRow attributes found");
                Interlocked.Increment(ref brokenTests);
                Interlocked.Increment(ref totalTests);
                return;
            }

            if (parameters.Length == 0)
            {
                ExecuteSingleTest(testClassType, testMethod, testName);
            }
            else
            {
                foreach (var dataRow in dataRows)
                {
                    if (dataRow.Data.Length != parameters.Length)
                    {
                        PrintTestResult($"{testName} (DataRow)", TestResult.ConfigurationError,
                            $"DataRow has {dataRow.Data.Length} values but method requires {parameters.Length} parameters");
                        Interlocked.Increment(ref brokenTests);
                        Interlocked.Increment(ref totalTests);
                        continue;
                    }

                    ExecuteSingleTest(testClassType, testMethod, $"{testName} (DataRow)", dataRow.Data);
                }
            }
        }

        private void ExecuteSingleTest(Type testClassType, MethodInfo testMethod, string testName,
            object[] parameters = null)
        {
            Interlocked.Increment(ref totalTests);

            var timeoutAttr = testMethod.GetCustomAttribute<TimeoutAttribute>();
            int? timeoutMs = timeoutAttr?.TimeoutMilliseconds;

            try
            {
                if (timeoutMs.HasValue)
                {
                    ExecuteWithTimeout(testClassType, testMethod, testName, parameters, timeoutMs.Value);
                }
                else
                {
                    ExecuteTestCore(testClassType, testMethod, testName, parameters);
                }

                PrintTestResult(testName, TestResult.Passed, null);
                Interlocked.Increment(ref passedTests);
            }
            catch (TestTimeoutException ex)
            {
                PrintTestResult(testName, TestResult.TimedOut, ex.Message);
                Interlocked.Increment(ref timedOutTests);
                Interlocked.Increment(ref failedTests);
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

        private void ExecuteTestCore(Type testClassType, MethodInfo testMethod, string testName,
            object[] parameters)
        {
            object instance = Activator.CreateInstance(testClassType);

            // Inject context
            var sharedContext = new Dictionary<string, object>();
            InjectContext(instance, sharedContext);

            // TestInitialize
            var testInitialize = FindMethodWithAttribute(testClassType, typeof(TestInitializeAttribute));
            if (testInitialize != null)
            {
                InvokeMethod(instance, testInitialize);
            }

            // Execute test
            object result = InvokeMethod(instance, testMethod, parameters);

            // Await async results
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

            // TestCleanup
            var testCleanup = FindMethodWithAttribute(testClassType, typeof(TestCleanupAttribute));
            if (testCleanup != null)
            {
                InvokeMethod(instance, testCleanup);
            }
        }

        private void ExecuteWithTimeout(Type testClassType, MethodInfo testMethod, string testName,
            object[] parameters, int timeoutMs)
        {
            using var cts = new CancellationTokenSource();
            var testTask = Task.Run(() =>
            {
                ExecuteTestCore(testClassType, testMethod, testName, parameters);
            }, cts.Token);

            var delayTask = Task.Delay(timeoutMs);

            var completed = Task.WhenAny(testTask, delayTask).GetAwaiter().GetResult();

            if (completed == delayTask)
            {
                // Timeout — test did not complete in time
                cts.Cancel();
                throw new TestTimeoutException(
                    $"Test exceeded timeout of {timeoutMs}ms");
            }
            else
            {
                // Test completed — rethrow any exception
                testTask.GetAwaiter().GetResult();
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
                Interlocked.Increment(ref failedTests);
            }
            else if (ex is TestConfigurationException)
            {
                PrintTestResult(testName, TestResult.ConfigurationError, ex.Message);
                Interlocked.Increment(ref brokenTests);
            }
            else
            {
                PrintTestResult(testName, TestResult.Broken, $"{ex.GetType().Name}: {ex.Message}");
                Interlocked.Increment(ref brokenTests);
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

        // ──────────────────────────────────────────
        //  Thread-safe output
        // ──────────────────────────────────────────
        private void SafeWriteLine(string text)
        {
            lock (consoleLock)
            {
                Console.WriteLine(text);
            }
        }

        private void PrintTestResult(string testName, TestResult result, string message)
        {
            lock (consoleLock)
            {
                var threadId = Thread.CurrentThread.ManagedThreadId;
                switch (result)
                {
                    case TestResult.Passed:
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"  ✓ PASSED: {testName}  [Thread {threadId}]");
                        break;
                    case TestResult.Failed:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"  ✗ FAILED: {testName}  [Thread {threadId}]");
                        if (!string.IsNullOrEmpty(message))
                        {
                            Console.WriteLine($"    {message}");
                        }
                        break;
                    case TestResult.Broken:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"  ✗ BROKEN: {testName}  [Thread {threadId}]");
                        if (!string.IsNullOrEmpty(message))
                        {
                            Console.WriteLine($"    {message}");
                        }
                        break;
                    case TestResult.Ignored:
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"  ⊘ IGNORED: {testName}  [Thread {threadId}]");
                        break;
                    case TestResult.ConfigurationError:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"  ✗ CONFIG ERROR: {testName}  [Thread {threadId}]");
                        if (!string.IsNullOrEmpty(message))
                        {
                            Console.WriteLine($"    {message}");
                        }
                        break;
                    case TestResult.TimedOut:
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        Console.WriteLine($"  ⏱ TIMED OUT: {testName}  [Thread {threadId}]");
                        if (!string.IsNullOrEmpty(message))
                        {
                            Console.WriteLine($"    {message}");
                        }
                        break;
                }
                Console.ResetColor();
            }
        }

        private void PrintSummary(string mode, long elapsedMs)
        {
            lock (consoleLock)
            {
                Console.WriteLine($"\n=== Test Summary ({mode}) ===");
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
                if (timedOutTests > 0)
                {
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine($"Timed Out: {timedOutTests}");
                    Console.ResetColor();
                }
                Console.WriteLine($"Time: {elapsedMs} ms");
            }
        }

        private enum TestResult
        {
            Passed,
            Failed,
            Broken,
            Ignored,
            ConfigurationError,
            TimedOut
        }
    }

    internal class DefaultContext : IContext
    {
        private readonly Dictionary<string, object> data = new Dictionary<string, object>();
        private readonly object syncLock = new object();

        public void SetData(string key, object value)
        {
            lock (syncLock) { data[key] = value; }
        }

        public object GetData(string key)
        {
            lock (syncLock) { return data.ContainsKey(key) ? data[key] : null; }
        }

        public bool ContainsData(string key)
        {
            lock (syncLock) { return data.ContainsKey(key); }
        }

        public void RemoveData(string key)
        {
            lock (syncLock)
            {
                if (data.ContainsKey(key))
                {
                    data.Remove(key);
                }
            }
        }
    }

    internal class TestTimeoutException : Exception
    {
        public TestTimeoutException(string message) : base(message) { }
    }
}
