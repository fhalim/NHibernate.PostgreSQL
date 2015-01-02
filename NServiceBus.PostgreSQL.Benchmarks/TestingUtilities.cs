namespace NServiceBus.PostgreSQL.Benchmarks
{
    using System;
    using System.Diagnostics;
    using HdrHistogram.NET;

    internal static class TestingUtilities
    {
        public static TimingInfo BenchmarkOperation(int iterations, Action<int> action, string messagePrefix = "")
        {
            var startTime = DateTime.UtcNow;
            var min = TimeSpan.MaxValue;
            var max = TimeSpan.MinValue;
            var hist = new Histogram(50000000, 5);
            for (var x = 0; x < iterations; x++)
            {
                var sw = Stopwatch.StartNew();
                action(x);
                sw.Stop();
                hist.recordValue(sw.ElapsedTicks/10);
            }
            
            return new TimingInfo(messagePrefix, hist, iterations, startTime);
        }
    }
}