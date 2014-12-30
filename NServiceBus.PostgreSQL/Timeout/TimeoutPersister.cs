namespace NServiceBus.PostgreSQL.Timeout
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using Dapper;
    using NServiceBus.Timeout.Core;

    public class TimeoutPersister : IPersistTimeouts
    {
        private readonly Func<IDbConnection> _connectionFactory;
        private Func<Type, string> _typeMapper;

        public TimeoutPersister(ConnectionFactoryHolder connectionFactoryHolder)
        {
            _connectionFactory = connectionFactoryHolder.ConnectionFactory;
            _typeMapper = t => t.FullName;
        }

        public IEnumerable<Tuple<string, DateTime>> GetNextChunk(DateTime startSlice, out DateTime nextTimeToRunQuery)
        {
            nextTimeToRunQuery = DateTime.MaxValue;
            return new Tuple<string, DateTime>[] {};
        }

        public void Add(TimeoutData timeout)
        {
            using (var conn = _connectionFactory())
            {
                var timeoutInfo = new
                {
                    timeout.Id,
                    Destination = timeout.Destination != null ? timeout.Destination.ToString() : null,
                    timeout.SagaId,
                    timeout.State,
                    timeout.Time,
                    timeout.Headers,
                    timeout.OwningTimeoutManager
                };
                conn.Execute(
                    "INSERT INTO timeouts (id, destination, sagaid, state, time, headers, owningtimeoutmanager) VALUES (:Id, :Destination, :SagaId, :State, :Time, :Headers, :OwningTimeoutManager)",
                    timeoutInfo);
            }
        }

        public bool TryRemove(string timeoutId, out TimeoutData timeoutData)
        {
            timeoutData = null;
            return false;
        }

        public void RemoveTimeoutBy(Guid sagaId)
        {
        }

        public static void Initialize(Func<IDbConnection> connFactory)
        {
            using (var conn = connFactory())
            {
                conn.Execute(
                    "CREATE TABLE IF NOT EXISTS timeouts(id TEXT, destination TEXT, sagaid UUID, state BYTEA, time TIMESTAMP WITH TIME ZONE, headers TEXT, owningtimeoutmanager TEXT, PRIMARY KEY (id))");
            }
        }
    }
}