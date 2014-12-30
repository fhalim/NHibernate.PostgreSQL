namespace NServiceBus.PostgreSQL
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using Features;
    using Saga;
    using Settings;

    public class PostgreSQLSagaStorage : Feature
    {
        internal PostgreSQLSagaStorage()
        {
            DependsOn<Sagas>();
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            var connFactory = PostgreSQLStorageSession.GetConnectionFactory(context.Settings);
            SagaPersister.Initialize(connFactory, GetSagaTypes(context.Settings));
            context.Container.ConfigureComponent<SagaPersister>(DependencyLifecycle.InstancePerCall);
        }

        private static List<Type> GetSagaTypes(ReadOnlySettings settings)
        {

            var sagaDataTypes = settings.GetAvailableTypes().Where(t => typeof(IContainSagaData).IsAssignableFrom(t) && !t.IsInterface).ToList();
            return sagaDataTypes;
        }
    }
}