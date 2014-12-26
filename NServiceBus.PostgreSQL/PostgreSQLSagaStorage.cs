namespace NServiceBus.PostgreSQL
{
    using Features;

    public class PostgreSQLSagaStorage : Feature
    {
        internal PostgreSQLSagaStorage()
        {
            DependsOn<Sagas>();
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<SagaPersister>(DependencyLifecycle.InstancePerCall);
        }
    }
}