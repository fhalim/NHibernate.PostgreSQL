namespace NServiceBus.PostgreSQL
{
    using System.Collections.Concurrent;
    using System.Reflection;
    using HdrHistogram.NET;

    public static class MethodTimeLogger
    {
        public static readonly ConcurrentDictionary<MethodBase, Histogram> Histograms = new ConcurrentDictionary<MethodBase, Histogram>();
        public static void Log(MethodBase methodBase, long milliseconds)
        {
            var histogram = Histograms.GetOrAdd(methodBase, b => new Histogram(99999, 5));
            lock (histogram)
            {
                histogram.recordValue(milliseconds);

            }
        }
    }
}