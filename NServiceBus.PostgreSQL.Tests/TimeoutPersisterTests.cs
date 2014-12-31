namespace NServiceBus.PostgreSQL.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Data;
    using System.Linq;
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
            var connstring = ConfigurationManager.AppSettings["testpgsqlconnstring"];
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

        [Fact]
        public void Should_be_able_to_query_timeout_in_future()
        {
            var persister = GetPersister();
            var time = DateTime.UtcNow;
            DateTime nextTimeToRun;
            var h = new Dictionary<string, string> {{"foo", "bar"}};
            persister.Add(new TimeoutData {Time = time, Headers = h});
            var chunk = persister.GetNextChunk(DateTime.UtcNow.AddMinutes(-1), out nextTimeToRun);
            Assert.Equal(1, chunk.Count());
            Assert.InRange(chunk.First().Item2, time.AddTicks(-10), time.AddTicks(10));
        }

        [Fact]
        public void Should_honor_time_of_next_Item()
        {
            var persister = GetPersister();
            var time = DateTime.UtcNow;
            var nextTime = DateTime.UtcNow.AddDays(1);
            DateTime nextTimeToRun;
            var h = new Dictionary<string, string> {{"foo", "bar"}};
            persister.Add(new TimeoutData {Time = time, Headers = h});
            persister.Add(new TimeoutData {Time = nextTime, Headers = h});
            persister.GetNextChunk(DateTime.UtcNow.AddMinutes(-1), out nextTimeToRun);
            Assert.InRange(nextTimeToRun, nextTime.AddTicks(-10), nextTime.AddTicks(10));
        }

        [Fact]
        public void Should_be_able_to_pop_entries_off()
        {
            var persister = GetPersister();
            var time = DateTime.UtcNow;
            DateTime nextTimeToRun;
            var h = new Dictionary<string, string> {{"foo", "bar"}};
            persister.Add(new TimeoutData {Time = time, Headers = h});
            var chunk = persister.GetNextChunk(DateTime.UtcNow.AddMinutes(-1), out nextTimeToRun);
            Assert.Equal(1, chunk.Count());
            TimeoutData timeoutData;
            var entry = persister.TryRemove(chunk.First().Item1, out timeoutData);
            Assert.NotNull(timeoutData);
            Assert.Equal(h, timeoutData.Headers);
            var chunk2 = persister.GetNextChunk(DateTime.UtcNow.AddMinutes(-1), out nextTimeToRun);
            Assert.Equal(0, chunk2.Count());
        }

        private TimeoutPersister GetPersister()
        {
            TimeoutPersister.Initialize(_connFactory);
            TimeoutPersister.Initialize(_connFactory);
            var persister = new TimeoutPersister(new ConnectionFactoryHolder {ConnectionFactory = _connFactory})
            {
                EndpointName = "MyTestEndpoint"
            };
            return persister;
        }
    }
}