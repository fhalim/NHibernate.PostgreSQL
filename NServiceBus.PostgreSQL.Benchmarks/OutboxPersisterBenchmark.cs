namespace NServiceBus.PostgreSQL.Benchmarks
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using Dapper;
    using Npgsql;
    using NServiceBus.Outbox;
    using Outbox;

    internal class OutboxPersisterBenchmark : IBenchmark
    {
        private readonly ConnectionFactoryHolder _connectionFactoryHolder;

        public OutboxPersisterBenchmark()
        {
            var connstring = ConfigurationManager.AppSettings["testpgsqlconnstring"];
            _connectionFactoryHolder = new ConnectionFactoryHolder
            {
                ConnectionFactory = () => new NpgsqlConnection(connstring)
            };
            using (var conn = _connectionFactoryHolder.ConnectionFactory())
            {
                conn.Execute("DROP TABLE IF EXISTS outboxes");
            }
        }

        public IEnumerable<TimingInfo> Execute(int iterations)
        {
            var persister = GetPersister();
            var ids = new List<string>(iterations);
            yield return TestingUtilities.BenchmarkOperation(iterations, x =>
            {
                var id = Guid.NewGuid().ToString();
                persister.Store(id, new[]{new TransportOperation(id, null, null, null) });
                ids.Add(id);
            }, "Storing_single_outbox_operation");
            yield return TestingUtilities.BenchmarkOperation(iterations, x =>
            {
                OutboxMessage msg;
                var found = persister.TryGet(ids[x], out msg);
                if (!found)
                {
                    throw new Exception("Whaa?");
                }
            }, "Looking_for_existing_operation");
            yield return TestingUtilities.BenchmarkOperation(iterations, x => {
                OutboxMessage msg;
                var found = persister.TryGet(Guid.NewGuid().ToString(), out msg);
                if (found) {
                    throw new Exception("Whaa?");
                }
            }, "Looking_for_not_existing_operation");
        }

        private OutboxPersister GetPersister()
        {
            OutboxPersister.Initialize(_connectionFactoryHolder.ConnectionFactory);
            var persister = new OutboxPersister(_connectionFactoryHolder);
            return persister;
        }

    }
}