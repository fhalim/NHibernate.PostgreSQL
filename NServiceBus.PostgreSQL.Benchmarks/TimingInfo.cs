namespace NServiceBus.PostgreSQL.Benchmarks
{
    using System;
    using HdrHistogram.NET;

    class TimingInfo
    {
        private readonly string _name;
        private readonly Histogram _histogram;
        private readonly int _iterations;
        private readonly DateTime _startTime;

        public TimingInfo(string name, Histogram histogram, int iterations, DateTime startTime)
        {
            _name = name;
            _histogram = histogram;
            _iterations = iterations;
            _startTime = startTime;
        }

        public DateTime StartTime
        {
            get { return _startTime; }
        }

        public int Iterations
        {
            get { return _iterations; }
        }

        public Histogram Histogram
        {
            get { return _histogram; }
        }

        public string Name
        {
            get { return _name; }
        }
    }
}