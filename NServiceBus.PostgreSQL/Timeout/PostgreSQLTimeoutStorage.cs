namespace NServiceBus.PostgreSQL.Timeout
{
    using Features;
    using Saga;

    public class PostgreSQLTimeoutStorage:Feature
    {
        protected override void Setup(FeatureConfigurationContext context)
        {
            var connFactory = PostgreSQLStorageSession.GetConnectionFactory(context.Settings);
            TimeoutPersister.Initialize(connFactory);
            context.Container.ConfigureComponent<SagaPersister>(DependencyLifecycle.InstancePerCall);
            context.Container.ConfigureComponent<TimeoutPersister>(DependencyLifecycle.InstancePerCall);
        }
    }
}