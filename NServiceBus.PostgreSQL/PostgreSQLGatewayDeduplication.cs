namespace NServiceBus.PostgreSQL
{
    using Features;

    public class PostgreSQLGatewayDeduplication:Feature
    {
        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<DeduplicationPersister>(DependencyLifecycle.InstancePerCall);
        }
    }
}