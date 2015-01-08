namespace NServiceBus.PostgreSQL.Benchmarks
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Linq;
    using Dapper;
    using Npgsql;
    using NServiceBus.Timeout.Core;
    using Timeout;

    internal class TimeoutPersisterBenchmark : IBenchmark
    {
        private readonly ConnectionFactoryHolder _connectionFactoryHolder;

        public TimeoutPersisterBenchmark()
        {
            var connstring = ConfigurationManager.AppSettings["testpgsqlconnstring"];
            _connectionFactoryHolder = new ConnectionFactoryHolder
            {
                ConnectionFactory = () => new NpgsqlConnection(connstring)
            };
            using (var conn = _connectionFactoryHolder.ConnectionFactory())
            {
                conn.Execute("DROP TABLE IF EXISTS timeouts");
            }
        }

        public IEnumerable<TimingInfo> Execute(int iterations)
        {
            var persister = GetPersister();
            persister.EndpointName = "TestPersister";
            var ids = new List<Guid>(iterations);
            
            yield return TestingUtilities.BenchmarkOperation(iterations, x =>
            {
                var id = Guid.NewGuid();
                persister.Add(new TimeoutData {Id = id.ToString(), SagaId = id});
                ids.Add(id);
            }, "Timeout_Add");
            
            yield return TestingUtilities.BenchmarkOperation(iterations, x =>
            {
                TimeoutData data;
                persister.TryRemove(ids[x].ToString(), out data);
            }, "Timeout_tryremove");
            var oneHourFromNow = DateTime.UtcNow + TimeSpan.FromHours(1);
            ids.Clear();
            foreach (var x in Enumerable.Range(0, iterations))
            {
                var id = Guid.NewGuid();
                persister.Add(new TimeoutData {Id = id.ToString(), SagaId = id, Time = oneHourFromNow});
                ids.Add(id);
            }
            yield return TestingUtilities.BenchmarkOperation(iterations, x =>
            {
                DateTime nextTime;
                persister.GetNextChunk(DateTime.UtcNow, out nextTime);
            }, "Timeout_GetNextChunk");
            yield return
                TestingUtilities.BenchmarkOperation(iterations, x => persister.RemoveTimeoutBy(ids[x]),
                    "Timeout_RemoveById");
        }

        private TimeoutPersister GetPersister()
        {
            TimeoutPersister.Initialize(_connectionFactoryHolder.ConnectionFactory);
            var persister = new TimeoutPersister(_connectionFactoryHolder);
            return persister;
        }
    }
}