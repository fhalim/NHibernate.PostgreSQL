namespace NServiceBus.PostgreSQL.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Diagnostics;
    using Dapper;
    using HdrHistogram.NET;
    using Npgsql;
    using Saga;
    using Xunit;

    public class SagaPersisterTests
    {
        private readonly ConnectionFactoryHolder _connectionFactoryHolder;

        public SagaPersisterTests()
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

        [Fact]
        public void Table_should_have_been_created()
        {
            using (var conn = _connectionFactoryHolder.ConnectionFactory())
            {
                Assert.Equal(0,
                    conn.ExecuteScalar<int>("SELECT COUNT(*) FROM information_schema.tables WHERE table_name = 'sagas';"));
                GetPersister();
                Assert.Equal(1,
                    conn.ExecuteScalar<int>("SELECT COUNT(*) FROM information_schema.tables WHERE table_name = 'sagas';"));
            }
        }

        [Fact]
        public void Insert_Should_result_in_row_being_added()
        {
            var persister = GetPersister();
            persister.Save(new FakeSagaData {CorrelationId = 123, Message = "Hello world"});
            using (var conn = _connectionFactoryHolder.ConnectionFactory())
            {
                Assert.Equal(1, conn.ExecuteScalar<int>("SELECT COUNT(*) FROM sagas;"));
            }
        }

        private SagaPersister GetPersister()
        {
            SagaPersister.Initialize(_connectionFactoryHolder.ConnectionFactory, new[] {typeof (FakeSagaData), typeof(BiggerSagaData)});
            SagaPersister.Initialize(_connectionFactoryHolder.ConnectionFactory, new[] { typeof(FakeSagaData), typeof(BiggerSagaData) });
            var persister = new SagaPersister(_connectionFactoryHolder);
            return persister;
        }

        [Fact]
        public void Should_be_able_read_inserted_row()
        {
            var persister = GetPersister();
            var id = Guid.NewGuid();
            var originalSagaData = new FakeSagaData {CorrelationId = 123, Message = "Hello world", Id = id};
            persister.Save(originalSagaData);
            var fakeSagaData = persister.Get<FakeSagaData>(id);
            Assert.Equal(originalSagaData, fakeSagaData);
        }

        [Fact]
        public void Completing_saga_should_delete_it()
        {
            var persister = GetPersister();
            var id = Guid.NewGuid();
            var originalSagaData = new FakeSagaData {CorrelationId = 123, Message = "Hello world", Id = id};
            persister.Save(originalSagaData);
            Assert.NotNull(persister.Get<FakeSagaData>(id));
            persister.Complete(originalSagaData);
            Assert.Null(persister.Get<FakeSagaData>(id));
        }

        [Fact]
        public void Should_be_able_read_inserted_row_by_field_value()
        {
            var persister = GetPersister();
            var id = Guid.NewGuid();
            var originalSagaData = new FakeSagaData {CorrelationId = 123, Message = "Hello world", Id = id};
            persister.Save(originalSagaData);
            var fakeSagaData = persister.Get<FakeSagaData>("Message", "Hello world");
            Assert.Equal(originalSagaData, fakeSagaData);

            var fakeSagaDataByCorrelationId = persister.Get<FakeSagaData>("CorrelationId", 123);
            Assert.Equal(originalSagaData, fakeSagaDataByCorrelationId);
        }

        [Fact]
        public void Persisting_saga_datas_with_conflicting_unique_values_should_throw()
        {
            var persister = GetPersister();
            var id = Guid.NewGuid();
            persister.Save(new FakeSagaData
            {
                CorrelationId = 123,
                MyOtherId = 12,
                Message = "Hello world",
                Id = Guid.NewGuid()
            });
            persister.Save(new FakeSagaData
            {
                CorrelationId = 123,
                MyOtherId = 13,
                Message = "Hello world",
                Id = Guid.NewGuid()
            });
            Assert.Throws<NpgsqlException>(
                () =>
                    persister.Save(new FakeSagaData
                    {
                        CorrelationId = 123,
                        MyOtherId = 13,
                        Message = "Hello world 2",
                        Id = Guid.NewGuid()
                    }));
        }

        [Fact]
        public void Updates_should_take_effect()
        {
            var persister = GetPersister();
            var id = Guid.NewGuid();
            persister.Save(new FakeSagaData {CorrelationId = 123, Message = "Hello world", Id = id});
            var updatedSagaData = new FakeSagaData {CorrelationId = 123, Message = "Hello world2", Id = id};
            persister.Update(updatedSagaData);
            var fakeSagaData = persister.Get<FakeSagaData>(id);
            Assert.Equal(updatedSagaData, fakeSagaData);
        }

        [Fact(Skip = "Benchmark")]
        public void Benchmark_writes()
        {
            var persister = GetPersister();
            var count = 1000;
            var ids = new List<Guid>(count);
            BenchmarkOperation(count, x=>
            {
                var id = Guid.NewGuid();
                persister.Save(new FakeSagaData {Id = id, MyOtherId = x, Message = "Message" + x});
                ids.Add(id);
            }, messagePrefix:"Saving_FakeSagaData");
            BenchmarkOperation(count, x=>persister.Save(new BiggerSagaData {Id=Guid.NewGuid(), MyId = Guid.NewGuid().ToString(), MyDateTime = DateTime.UtcNow, MyInteger = x}), messagePrefix:"Saving_BiggerSagaData");
            BenchmarkOperation(count, x=>persister.Get<FakeSagaData>(ids[x]), messagePrefix:"Query_FakeSagaData_ById");
            BenchmarkOperation(count, x=>persister.Get<BiggerSagaData>("MyInteger", x), messagePrefix:"Query_BiggerSagaData_ByArbitraryField_Existing");
            BenchmarkOperation(count, x=>persister.Get<BiggerSagaData>("MyInteger", Int32.MaxValue - x), messagePrefix:"Query_BiggerSagaData_ByArbitraryField_NonExistant");
            BenchmarkOperation(count, x=>persister.Complete(new FakeSagaData{Id = ids[x], MyOtherId = x, Message = "Message" + x}), messagePrefix:"Complete_FakeSagaData");
        }

        private static Histogram BenchmarkOperation(int iterations, Action<int> action, string messagePrefix = "")
        {
            var min = TimeSpan.MaxValue;
            var max = TimeSpan.MinValue;
            var hist = new Histogram(50000000, 5);
            for (var x = 0; x < iterations; x++) {
                var sw = Stopwatch.StartNew();
                action(x);
                sw.Stop();
                hist.recordValue(sw.ElapsedTicks / 10);
            }
            Console.WriteLine("{0} Min: {1}us, Max: {2}us, 95%: {3}us, 99%: {4}us", messagePrefix, hist.getMinValue(), hist.getMaxValue(), hist.getValueAtPercentile(95), hist.getValueAtPercentile(99));
            return hist;
        }
    }
}