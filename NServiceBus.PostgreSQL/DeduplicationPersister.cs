namespace NServiceBus.PostgreSQL
{
    using System;
    using Gateway.Deduplication;

    public class DeduplicationPersister : IDeduplicateMessages
    {
        public bool DeduplicateMessage(string clientId, DateTime timeReceived)
        {
            return false;
        }
    }
}