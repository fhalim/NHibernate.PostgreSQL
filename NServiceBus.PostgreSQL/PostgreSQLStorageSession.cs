using System;
using System.Data;

namespace NServiceBus.PostgreSQL
{
    using System;
    using System.Configuration;
    using System.Data;
    using System.Data.Common;
    using Features;
    using Settings;

    public class PostgreSQLStorageSession:Feature
    {
        public PostgreSQLStorageSession()
        {
            Defaults(_ => _.Set<ConnectionFactoryHolder>(new ConnectionFactoryHolder()));
        }
        private const string ConnectionStringName = "NServiceBus/Persistence/PostgreSQL";
        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Container.RegisterSingleton(new ConnectionFactoryHolder{ConnectionFactory = CreateConnectionFactory()});
        }

        public static Func<IDbConnection> GetConnectionFactory(ReadOnlySettings settings)
        {
            
            var holder = settings.Get<ConnectionFactoryHolder>();
            if (holder.ConnectionFactory != null) return holder.ConnectionFactory;
            var connectionFactory = CreateConnectionFactory();
            holder.ConnectionFactory = connectionFactory;
            return holder.ConnectionFactory;
        }

        private static Func<IDbConnection> CreateConnectionFactory()
        {
            var providerFactory =
                DbProviderFactories.GetFactory(ConfigurationManager.ConnectionStrings[ConnectionStringName].ProviderName);
            return () =>
            {
                var conn = providerFactory.CreateConnection();
                conn.ConnectionString = ConfigurationManager.ConnectionStrings[ConnectionStringName].ConnectionString;
                return conn;
            };
        }
    }
}