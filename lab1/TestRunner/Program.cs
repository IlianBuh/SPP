using System;

namespace TestRunner
{
    class Program
    {
        static int Main(string[] args)
        {
            try
            {
                var runner = new TestRunner();
                var result = runner.RunTests();
                return result ? 0 : 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fatal error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                return 1;
            }
        }
    }
}
