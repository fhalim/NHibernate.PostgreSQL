namespace NServiceBus.PostgreSQL
{
    using System;
    using System.Data;
    using Dapper;
    using Newtonsoft.Json;
    using Saga;

    public class SagaPersister : ISagaPersister
    {
        private readonly Func<IDbConnection> _connectionFactory;

        public SagaPersister(Func<IDbConnection> connectionFactory)
        {
            _connectionFactory = connectionFactory;
            InitializePersistence();
        }

        public void Save(IContainSagaData saga)
        {
            using (var conn = _connectionFactory())
            {
                var p = new DynamicParameters();
                p.Add(":id", dbType: DbType.Guid, value: saga.Id);
                p.Add(":originalmessageid", dbType: DbType.String, value: saga.OriginalMessageId);
                p.Add(":originator", dbType: DbType.String, value: saga.Originator);
                p.Add(":sagadata", dbType: DbType.String, value: JsonConvert.SerializeObject(saga));

                conn.Execute(
                    "INSERT INTO sagas(id, originalmessageid, originator, sagadata) VALUES (:id, :originalmessageid, :originator, :sagadata)",
                    p);
            }
        }

        public void Update(IContainSagaData saga)
        {
        }

        public TSagaData Get<TSagaData>(Guid sagaId) where TSagaData : IContainSagaData
        {
            return default(TSagaData);
        }

        public TSagaData Get<TSagaData>(string propertyName, object propertyValue) where TSagaData : IContainSagaData
        {
            return default(TSagaData);
        }

        public void Complete(IContainSagaData saga)
        {
        }

        private void InitializePersistence()
        {
            using (var conn = _connectionFactory())
            {
                conn.Execute(
                    "CREATE TABLE IF NOT EXISTS sagas(id UUID, originalmessageid TEXT, originator TEXT, sagadata JSONB)");
            }
        }
    }
}