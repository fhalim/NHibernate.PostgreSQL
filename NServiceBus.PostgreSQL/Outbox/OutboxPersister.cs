namespace NServiceBus.PostgreSQL.Outbox
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using Dapper;
    using Logging;
    using MethodTimer;
    using Newtonsoft.Json;
    using NServiceBus.Outbox;

    public class OutboxPersister : IOutboxStorage
    {
        static readonly ILog Logger = LogManager.GetLogger(typeof(OutboxPersister));
        private readonly Func<IDbConnection> _connectionFactory;

        public OutboxPersister(ConnectionFactoryHolder connectionFactoryHolder)
        {
            _connectionFactory = connectionFactoryHolder.ConnectionFactory;
        }

        [Time]
        public bool TryGet(string messageId, out OutboxMessage message)
        {
            using (var conn = _connectionFactory())
            {
                message =
                    conn.Query<string>("SELECT transportoperations FROM outboxes WHERE messageId = :messageId", new { messageId })
                        .Select(
                            r =>
                            {
                                var m = new OutboxMessage(messageId);
                                if (r != null)
                                {
                                    m.TransportOperations.AddRange(
                                        JsonConvert.DeserializeObject<List<TransportOperation>>(r));
                                }
                                return m;
                            }).FirstOrDefault();
                return message != default(OutboxMessage);
            }
        }

        [Time]
        public void Store(string messageId, IEnumerable<TransportOperation> transportOperations)
        {
            using (var conn = _connectionFactory())
            {
                var p = new DynamicParameters(new {messageId});
                p.Add(":transportoperations", JsonConvert.SerializeObject(transportOperations), DbType.String);
                conn.Execute(
                    "INSERT INTO outboxes(messageId, transportoperations) VALUES (:messageId, :transportoperations)",
                    p);
            }
        }

        [Time]
        public void SetAsDispatched(string messageId)
        {
            using (var conn = _connectionFactory())
            {
                conn.Execute("UPDATE outboxes SET dispatched = true, dispatchedat = :date WHERE messageId = :messageId",
                    new {messageId, date = DateTime.UtcNow});
            }
        }

        public static void Initialize(Func<IDbConnection> connectionFactory)
        {
            Logger.Info("Initializing");
            using (var conn = connectionFactory())
            {
                conn.Execute(
                    "CREATE TABLE IF NOT EXISTS outboxes (messageId TEXT, dispatched BOOL, dispatchedat TIMESTAMP WITHOUT TIME ZONE, transportoperations JSONB, PRIMARY KEY(messageId))");
                conn.CreateIndexIfNotExists(
                    "CREATE INDEX idx_outboxes_dispatched ON outboxes (dispatchedat) WHERE dispatched = true",
                    "idx_outboxes_dispatched");
            }
        }

        public static void PurgeDispatched(Func<IDbConnection> connectionFactory, DateTime endTime)
        {
            using (var conn = connectionFactory())
            {
                conn.Execute("DELETE FROM outboxes WHERE dispatched = true AND dispatchedat <= :endTime", new {endTime});
            }
        }
    }
}