namespace NServiceBus.PostgreSQL
{
    using System;
    using System.Data;
    using Features;

    public class PostgreSQLSagaStorage : Feature
    {
        internal PostgreSQLSagaStorage()
        {
            DependsOn<Sagas>();
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            var connFactory = PostgreSQLStorageSession.GetConnectionFactory(context.Settings);
            SagaPersister.Initialize(connFactory);
            context.Container.ConfigureComponent<SagaPersister>(DependencyLifecycle.InstancePerCall);
        }
    }
}