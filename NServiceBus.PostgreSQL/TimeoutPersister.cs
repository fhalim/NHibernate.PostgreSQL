namespace NServiceBus.PostgreSQL
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using Timeout.Core;

    public class TimeoutPersister : IPersistTimeouts
    {
        private Func<IDbConnection> _connectionFactory;
        private Func<Type, string> _typeMapper;

        public TimeoutPersister(ConnectionFactoryHolder connectionFactoryHolder)
        {
            _connectionFactory = connectionFactoryHolder.ConnectionFactory;
            _typeMapper = t => t.FullName;
        }

        public IEnumerable<Tuple<string, DateTime>> GetNextChunk(DateTime startSlice, out DateTime nextTimeToRunQuery)
        {
            nextTimeToRunQuery = DateTime.MaxValue;
            return new Tuple<string, DateTime>[] { };
        }

        public void Add(TimeoutData timeout)
        {
        }

        public bool TryRemove(string timeoutId, out TimeoutData timeoutData)
        {
            timeoutData = null;
            return false;
        }

        public void RemoveTimeoutBy(Guid sagaId)
        {
        }

        public static void Initialize(IDbConnection conn)
        {
            
        }
    }
}