namespace NServiceBus.PostgreSQL.Benchmarks
{
    using System.Collections.Generic;

    internal interface IBenchmark
    {
        IEnumerable<TimingInfo> Execute(int iterations);
    }
}