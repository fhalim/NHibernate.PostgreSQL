namespace NServiceBus.PostgreSQL
{
    using System;
    using System.Collections.Generic;
    using Timeout.Core;

    public class TimeoutPersister:IPersistTimeouts
    {
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
    }
}