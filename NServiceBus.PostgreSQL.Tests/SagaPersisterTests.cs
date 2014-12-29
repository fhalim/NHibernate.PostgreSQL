namespace NServiceBus.PostgreSQL.Tests
{
    using System;
    using System.Data;
    using Dapper;
    using Npgsql;
    using Xunit;

    public class SagaPersisterTests
    {
        private readonly Func<IDbConnection> _connFactory; 
        public SagaPersisterTests()
        {
            const string connstring = "Server=127.0.0.1;Port=5432;User Id=NServiceBus.PostgreSQL.Tests;Password=password;Database=dev;";
            _connFactory = () => new NpgsqlConnection(connstring);
            using (var conn = _connFactory())
            {
                conn.Execute("DROP TABLE IF EXISTS sagas");
            }
        }
        [Fact]
        public void Table_should_have_been_created()
        {
            using (var conn = _connFactory()) {
                Assert.Equal(0, conn.ExecuteScalar<int>("SELECT COUNT(*) FROM information_schema.tables WHERE table_name = 'sagas';"));
            }
            var persister = new SagaPersister(_connFactory);
            using (var conn = _connFactory())
            {
                Assert.Equal(1, conn.ExecuteScalar<int>("SELECT COUNT(*) FROM information_schema.tables WHERE table_name = 'sagas';"));
            }
        }

        [Fact]
        public void Insert_Should_result_in_row_being_added()
        {
            var persister = new SagaPersister(_connFactory);
            persister.Save(new FakeSagaData{CorrelationId = 123, Message = "Hello world"});
            using (var conn = _connFactory()) {
                Assert.Equal(1, conn.ExecuteScalar<int>("SELECT COUNT(*) FROM sagas;"));
            }
        }

        [Fact]
        public void Should_be_able_read_inserted_row()
        {
            var persister = new SagaPersister(_connFactory);
            var id = Guid.NewGuid();
            persister.Save(new FakeSagaData { CorrelationId = 123, Message = "Hello world", Id = id});
            var fakeSagaData = persister.Get<FakeSagaData>(id);
            Assert.NotNull(fakeSagaData);
        }
    }
}
