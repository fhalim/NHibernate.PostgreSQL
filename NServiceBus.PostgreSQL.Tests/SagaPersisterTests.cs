namespace NServiceBus.PostgreSQL.Tests
{
    using System;
    using Dapper;
    using Npgsql;
    using Saga;
    using Xunit;

    public class SagaPersisterTests
    {
        private readonly ConnectionFactoryHolder _connectionFactoryHolder;

        public SagaPersisterTests()
        {
            const string connstring =
                "Server=127.0.0.1;Port=5432;User Id=NServiceBus.PostgreSQL.Tests;Password=password;Database=dev;";
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
            SagaPersister.Initialize(_connectionFactoryHolder.ConnectionFactory, new[] {typeof (FakeSagaData)});
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
    }
}