namespace NServiceBus.PostgreSQL.Tests
{
    using System;
    using System.Configuration;
    using Dapper;
    using Npgsql;
    using NServiceBus.Outbox;
    using Outbox;
    using Xunit;

    public class OutboxPersisterTests
    {
        private readonly ConnectionFactoryHolder _connectionFactoryHolder;

        public OutboxPersisterTests()
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

        [Fact]
        public void Should_create_table_on_initialize()
        {
            using (var conn = _connectionFactoryHolder.ConnectionFactory())
            {
                Assert.Equal(0,
                    conn.ExecuteScalar<int>("SELECT COUNT(*) FROM information_schema.tables WHERE table_name = 'outboxes'"));
                var persister = GetPersister();
                Assert.Equal(1,
                    conn.ExecuteScalar<int>("SELECT COUNT(*) FROM information_schema.tables WHERE table_name = 'outboxes'"));
            }
        }

        [Fact]
        public void Should_be_able_to_add_stuff()
        {
            var persister = GetPersister();
            persister.Store("123", new[] {new TransportOperation("messageId", null, null, null)});
            using (var conn = _connectionFactoryHolder.ConnectionFactory())
            {
                Assert.Equal(1, conn.ExecuteScalar<int>("SELECT COUNT(*) FROM outboxes"));
            }
        }

        [Fact]
        public void Should_be_able_to_read_resultsBack()
        {
            var persister = GetPersister();
            OutboxMessage message;
            Assert.False(persister.TryGet("123", out message));
            persister.Store("123", new[] { new TransportOperation("123", null, null, null) });
            Assert.True(persister.TryGet("123", out message));
            Assert.NotNull(message);
            Assert.Equal("123", message.MessageId);
            Assert.Equal(1, message.TransportOperations.Count);
            Assert.NotNull(message.TransportOperations[0]);
        }

        [Fact]
        public void Should_be_able_to_mark_messages_as_dispatched()
        {
            var persister = GetPersister();
            persister.Store("123", new[] { new TransportOperation("123", null, null, null) });
            persister.SetAsDispatched("123");
            using (var conn = _connectionFactoryHolder.ConnectionFactory()) {
                Assert.Equal(true, conn.ExecuteScalar<bool>("SELECT dispatched FROM outboxes"));
            }
        }

        [Fact]
        public void Should_be_able_to_purge_dispatched_messages()
        {
            var persister = GetPersister();
            persister.Store("123", new[] { new TransportOperation("123", null, null, null) });
            persister.SetAsDispatched("123");
            OutboxPersister.PurgeDispatched(_connectionFactoryHolder.ConnectionFactory, DateTime.UtcNow);
            using (var conn = _connectionFactoryHolder.ConnectionFactory()) {
                Assert.Equal(0, conn.ExecuteScalar<int>("SELECT COUNT(*) FROM outboxes"));
            }
        }
        private OutboxPersister GetPersister()
        {
            OutboxPersister.Initialize(_connectionFactoryHolder.ConnectionFactory);
            OutboxPersister.Initialize(_connectionFactoryHolder.ConnectionFactory);
            var persister = new OutboxPersister(_connectionFactoryHolder);
            return persister;
        }
    }
}