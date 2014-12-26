namespace NServiceBus.PostgreSQL
{
    using Features;

    public class PostgreSQLTimeoutStorage:Feature
    {
        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<TimeoutPersister>(DependencyLifecycle.InstancePerCall);

        }
    }
}