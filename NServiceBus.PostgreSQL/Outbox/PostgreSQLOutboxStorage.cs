namespace NServiceBus.PostgreSQL.Outbox
{
    using System;
    using System.Configuration;
    using System.Data;
    using System.Threading;
    using Features;

    public class PostgreSQLOutboxStorage:Feature
    {
        Timer _cleanupTimer;
        private Func<IDbConnection> _connectionFactory;
        private TimeSpan _timeToKeepDeduplicationData;
        internal PostgreSQLOutboxStorage()
        {
            DependsOn<Outbox>();
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            _connectionFactory = PostgreSQLStorageSession.GetConnectionFactory(context.Settings);
            OutboxPersister.Initialize(_connectionFactory);
            context.Container.ConfigureComponent<OutboxPersister>(DependencyLifecycle.InstancePerCall);
            var frequencyToRunDeduplicationDataCleanup = ReadTimeSpanConfig("NServiceBus/Outbox/PostgreSQL/FrequencyToRunDeduplicationDataCleanup", TimeSpan.FromMinutes(1));
            _timeToKeepDeduplicationData = ReadTimeSpanConfig("NServiceBus/Outbox/PostgreSQL/TimeToKeepDeduplicationData", TimeSpan.FromDays(7));
            _cleanupTimer = new Timer(PerformCleanup, null, TimeSpan.FromMinutes(1), frequencyToRunDeduplicationDataCleanup);
        }

        private static TimeSpan ReadTimeSpanConfig(string key, TimeSpan defaultValue)
        {
            TimeSpan frequencyToRunDeduplicationDataCleanup;
            var freqConfig =
                ConfigurationManager.AppSettings.Get(key);
            if (freqConfig == null)
            {
                frequencyToRunDeduplicationDataCleanup = defaultValue;
            }
            else
            {
                if (!TimeSpan.TryParse(freqConfig, out frequencyToRunDeduplicationDataCleanup))
                {
                    throw new Exception(
                        "Invalid value in \"" + key + "\" AppSetting. Please ensure it is a TimeSpan.");
                }
            }
            return frequencyToRunDeduplicationDataCleanup;
        }

        void PerformCleanup(object state)
        {
            OutboxPersister.PurgeDispatched(_connectionFactory, DateTime.UtcNow - _timeToKeepDeduplicationData);
        }
    }
}