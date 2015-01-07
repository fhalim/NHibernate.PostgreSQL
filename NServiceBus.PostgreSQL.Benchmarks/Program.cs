namespace NServiceBus.PostgreSQL.Benchmarks
{
    using System;
    using System.IO;
    using System.Threading;
    using Saga;

    internal class Program
    {
        private static void Main(string[] args)
        {
            var benchmarks = new IBenchmark[] {new SagaPersisterBenchmark(), new OutboxPersisterBenchmark(), };
            var iterations = 1000;
            Console.WriteLine("Executing benchmarks. Please wait...");
            var outfile = "log.csv";
            var writeheaders = !File.Exists(outfile);
            if (!File.Exists(outfile))
            {
            }
            new Timer(o => PrintHistograms(), null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30));
            for(var runIdx = 0; runIdx < 10; runIdx++)
            {
                using (var stream = File.AppendText(outfile)) {
                    if (writeheaders) {
                        stream.WriteLine("Header,StartTime,Minimum,Maximum,Mean,95th Percentile,99th Percentile");
                    }
                    foreach (var benchmark in benchmarks) {
                        Console.WriteLine("Executing benchmarks in {0}...", benchmark.GetType().Name);
                        foreach (var timingInfo in benchmark.Execute(iterations)) {
                            Console.WriteLine("{0} Min: {1}us, Max: {2}us, 95%: {3}us, 99%: {4}us", timingInfo.Name,
                                timingInfo.Histogram.getMinValue(),
                                timingInfo.Histogram.getMaxValue(), timingInfo.Histogram.getValueAtPercentile(95),
                                timingInfo.Histogram.getValueAtPercentile(99));
                            stream.WriteLine("{0},{1},{2},{3},{4},{5},{6}", timingInfo.Name, timingInfo.StartTime.ToString("o"), timingInfo.Histogram.getMinValue(), timingInfo.Histogram.getMaxValue(), timingInfo.Histogram.getMean(), timingInfo.Histogram.getValueAtPercentile(95), timingInfo.Histogram.getValueAtPercentile(99));
                        }
                    }
                }
            }
            
            Console.WriteLine("Execution complete. Please press enter to stop");
            Console.ReadLine();
        }

        private static void PrintHistograms()
        {
            foreach (var histogram in MethodTimeLogger.Histograms)
            {
                lock (histogram.Value)
                {
                    Console.WriteLine("Hist: {0} Min: {1}ms, Max: {2}ms, 95%: {3}ms, 99%: {4}ms, samples: {5}", histogram.Key,
                        histogram.Value.getMinValue(),
                        histogram.Value.getMaxValue(), histogram.Value.getValueAtPercentile(95),
                        histogram.Value.getValueAtPercentile(99), histogram.Value.getTotalCount());
                }
            }
        }
    }
}