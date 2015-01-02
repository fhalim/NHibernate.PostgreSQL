namespace NServiceBus.PostgreSQL.Benchmarks
{
    using System;
    using System.IO;

    internal class Program
    {
        private static void Main(string[] args)
        {
            var benchmarks = new IBenchmark[] {new SagaPersisterBenchmark()};
            var iterations = 10000;
            Console.WriteLine("Executing benchmarks. Please wait...");
            var outfile = "log.csv";
            var writeheaders = !File.Exists(outfile);
            if (!File.Exists(outfile))
            {
            }
            for(var runIdx = 0; runIdx < 100; runIdx++)
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
                            stream.WriteLine(String.Format("{0},{1},{2},{3},{4},{5},{6}", timingInfo.Name,
                                timingInfo.StartTime.ToString("o"), timingInfo.Histogram.getMinValue(),
                                timingInfo.Histogram.getMaxValue(), timingInfo.Histogram.getMean(),
                                timingInfo.Histogram.getValueAtPercentile(95), timingInfo.Histogram.getValueAtPercentile(99)));
                        }
                    }
                }
            }
            
            Console.WriteLine("Execution complete. Please press enter to stop");
            Console.ReadLine();
        }
    }
}