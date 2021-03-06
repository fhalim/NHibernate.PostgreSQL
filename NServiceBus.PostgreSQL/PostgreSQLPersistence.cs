﻿namespace NServiceBus.PostgreSQL
{
    using Features;
    using Outbox;
    using Persistence;
    using Saga;
    using Timeout;

    public class PostgreSQLPersistence : PersistenceDefinition
    {
        /// <summary>
        /// Defines the capabilities
        /// </summary>
        public PostgreSQLPersistence()
        {
            Defaults(s => s.EnableFeatureByDefault<PostgreSQLStorageSession>());

            Supports(Storage.Outbox, s => s.EnableFeatureByDefault<PostgreSQLOutboxStorage>());
            Supports(Storage.Timeouts, s => s.EnableFeatureByDefault<PostgreSQLTimeoutStorage>());
            Supports(Storage.Sagas, s => s.EnableFeatureByDefault<PostgreSQLSagaStorage>());
        }
    }
}
