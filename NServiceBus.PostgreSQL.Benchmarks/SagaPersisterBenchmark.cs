namespace NServiceBus.PostgreSQL.Benchmarks
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using Dapper;
    using Npgsql;
    using Saga;
    using Tests;

    internal class SagaPersisterBenchmark : IBenchmark
    {
        private readonly ConnectionFactoryHolder _connectionFactoryHolder;

        public SagaPersisterBenchmark()
        {
            var connstring = ConfigurationManager.AppSettings["testpgsqlconnstring"];
            _connectionFactoryHolder = new ConnectionFactoryHolder
            {
                ConnectionFactory = () => new NpgsqlConnection(connstring)
            };
            using (var conn = _connectionFactoryHolder.ConnectionFactory())
            {
                conn.Execute("DROP TABLE IF EXISTS sagas");
            }
        }

        public IEnumerable<TimingInfo> Execute(int iterations)
        {
            var persister = GetPersister();
            var ids = new List<Guid>(iterations);
            yield return TestingUtilities.BenchmarkOperation(iterations, x =>
            {
                var id = Guid.NewGuid();
                persister.Save(new FakeSagaData {Id = id, MyOtherId = x, Message = "Message" + x});
                ids.Add(id);
            }, "Saving_FakeSagaData");
            yield return TestingUtilities.BenchmarkOperation(iterations,
                x =>
                    persister.Save(new BiggerSagaData
                    {
                        Id = Guid.NewGuid(),
                        MyId = Guid.NewGuid().ToString(),
                        MyDateTime = DateTime.UtcNow,
                        MyInteger = x
                    }), "Saving_BiggerSagaData");
            yield return TestingUtilities.BenchmarkOperation(iterations, x => persister.Get<FakeSagaData>(ids[x]),
                "Query_FakeSagaData_ById");
            yield return
                TestingUtilities.BenchmarkOperation(iterations, x => persister.Get<BiggerSagaData>("MyInteger", x),
                    "Query_BiggerSagaData_ByArbitraryField_Existing");
            yield return TestingUtilities.BenchmarkOperation(iterations,
                x => persister.Update(new FakeSagaData {Id = ids[x], MyOtherId = x, Message = "Message" + x, Originator = "bob"}),
                "Update_FakeSagaData");
            yield return TestingUtilities.BenchmarkOperation(iterations,
                x => persister.Get<BiggerSagaData>("MyInteger", Int32.MaxValue - x),
                "Query_BiggerSagaData_ByArbitraryField_NonExistant");
            yield return TestingUtilities.BenchmarkOperation(iterations,
                x => persister.Complete(new FakeSagaData {Id = ids[x], MyOtherId = x, Message = "Message" + x}),
                "Complete_FakeSagaData");
        }

        private SagaPersister GetPersister()
        {
            SagaPersister.Initialize(_connectionFactoryHolder.ConnectionFactory,
                new[] {typeof (FakeSagaData), typeof (BiggerSagaData)});
            var persister = new SagaPersister(_connectionFactoryHolder);
            return persister;
        }
    }
}