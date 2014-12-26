namespace NServiceBus.PostgreSQL
{
    using System;
    using System.Configuration;
    using System.Data;
    using System.Data.Common;
    using Features;

    public class PostgreSQLStorageSession:Feature
    {
        private const string ConnectionStringName = "NServiceBus/Persistence/PostgreSQL";
        protected override void Setup(FeatureConfigurationContext context)
        {
            var providerFactory = DbProviderFactories.GetFactory(ConfigurationManager.ConnectionStrings[ConnectionStringName].ProviderName);
            context.Container.RegisterSingleton<Func<IDbConnection>>(() =>
            {
                var conn = providerFactory.CreateConnection();
                conn.ConnectionString = ConfigurationManager.ConnectionStrings[ConnectionStringName].ConnectionString;
                return conn;
            });
        }
    }
}