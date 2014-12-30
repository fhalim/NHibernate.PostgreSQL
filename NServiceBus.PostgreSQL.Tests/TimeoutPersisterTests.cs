namespace NServiceBus.PostgreSQL.Tests
{
    using System;
    using System.Data;
    using Dapper;
    using Npgsql;
    using NServiceBus.Timeout.Core;
    using Timeout;
    using Xunit;

    public class TimeoutPersisterTests
    {
        private readonly Func<IDbConnection> _connFactory;

        public TimeoutPersisterTests()
        {
            const string connstring =
                "Server=127.0.0.1;Port=5432;User Id=NServiceBus.PostgreSQL.Tests;Password=password;Database=dev;";
            _connFactory = () => new NpgsqlConnection(connstring);
            using (var conn = _connFactory())
            {
                conn.Execute("DROP TABLE IF EXISTS timeouts");
            }
        }

        [Fact]
        public void Should_create_timeout_table()
        {
            GetPersister();
            using (var conn = _connFactory())
            {
                Assert.Equal(1,
                    conn.ExecuteScalar<int>(
                        "SELECT COUNT(*) FROM information_schema.tables WHERE table_name = 'timeouts'"));
            }
        }

        [Fact]
        public void Should_insert_row()
        {
            var persister = GetPersister();
            persister.Add(new TimeoutData {Id = "Hello"});
            using (var conn = _connFactory())
            {
                Assert.Equal(1, conn.ExecuteScalar<int>("SELECT COUNT(*) FROM timeouts"));
            }
        }


        private TimeoutPersister GetPersister()
        {
            TimeoutPersister.Initialize(_connFactory);
            var persister = new TimeoutPersister(new ConnectionFactoryHolder {ConnectionFactory = _connFactory});
            return persister;
        }
    }
}