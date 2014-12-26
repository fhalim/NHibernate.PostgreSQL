namespace NServiceBus.PostgreSQL
{
    using Features;
    using Persistence;

    public class PostgreSQLPersistence : PersistenceDefinition
    {
        /// <summary>
        /// Defines the capabilities
        /// </summary>
        public PostgreSQLPersistence()
        {
            Defaults(s =>
            {
                s.EnableFeatureByDefault<PostgreSQLStorageSession>();
                /*          s.EnableFeatureByDefault<RavenDbStorageSession>();
                s.EnableFeatureByDefault<SharedDocumentStore>();*/
            });

            Supports(Storage.GatewayDeduplication, s => s.EnableFeatureByDefault<PostgreSQLGatewayDeduplication>());
            Supports(Storage.Timeouts, s => s.EnableFeatureByDefault<PostgreSQLTimeoutStorage>());
            Supports(Storage.Sagas, s => s.EnableFeatureByDefault<PostgreSQLSagaStorage>());
            //Supports(Storage.Subscriptions, s => s.EnableFeatureByDefault<RavenDbSubscriptionStorage>());
        }
    }
}
