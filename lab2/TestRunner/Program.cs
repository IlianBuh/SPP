using System;

namespace TestRunner
{
    class Program
    {
        static int Main(string[] args)
        {
            int maxParallelism = 0; // 0 = Environment.ProcessorCount

            for (int i = 0; i < args.Length; i++)
            {
                if ((args[i] == "--max-parallelism" || args[i] == "-p") && i + 1 < args.Length)
                {
                    if (int.TryParse(args[i + 1], out int val) && val > 0)
                    {
                        maxParallelism = val;
                    }
                    i++;
                }
            }

            try
            {
                var runner = new TestRunner(maxParallelism);
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
