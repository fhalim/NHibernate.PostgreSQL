namespace NServiceBus.PostgreSQL
{
    using System;
    using System.Data;
    using System.Linq;
    using Dapper;
    using Newtonsoft.Json;
    using Saga;

    public class SagaPersister : ISagaPersister
    {
        private readonly Func<IDbConnection> _connectionFactory;
        private readonly Func<Type, string> _typeMapper; 

        public SagaPersister(Func<IDbConnection> connectionFactory)
        {
            _connectionFactory = connectionFactory;
            _typeMapper = t => t.FullName;
            InitializePersistence();
        }

        public void Save(IContainSagaData saga)
        {
            using (var conn = _connectionFactory())
            {
                var p = new DynamicParameters();
                p.Add(":type", dbType: DbType.String, value: _typeMapper(saga.GetType()));
                p.Add(":id", dbType: DbType.Guid, value: saga.Id);
                p.Add(":originalmessageid", dbType: DbType.String, value: saga.OriginalMessageId);
                p.Add(":originator", dbType: DbType.String, value: saga.Originator);
                p.Add(":sagadata", dbType: DbType.String, value: JsonConvert.SerializeObject(saga));

                conn.Execute(
                    "INSERT INTO sagas(type, id, originalmessageid, originator, sagadata) VALUES (:type, :id, :originalmessageid, :originator, :sagadata)",
                    p);
            }
        }

        public void Update(IContainSagaData saga)
        {
        }

        public TSagaData Get<TSagaData>(Guid sagaId) where TSagaData : IContainSagaData
        {
            using (var conn = _connectionFactory()) {
                var p = new DynamicParameters();
                p.Add(":type", dbType: DbType.String, value: _typeMapper(typeof(TSagaData)));
                p.Add(":id", dbType: DbType.Guid, value: sagaId);
                var data = conn.Query<string>("SELECT sagadata FROM sagas WHERE type = :type AND id = :id", p).FirstOrDefault();
                if (data == default(string))
                {
                    return default(TSagaData);
                }
                else
                {
                    return JsonConvert.DeserializeObject<TSagaData>(data);
                }
            }
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
                    "CREATE TABLE IF NOT EXISTS sagas(type TEXT, id UUID, originalmessageid TEXT, originator TEXT, sagadata JSONB, PRIMARY KEY (type, id))");
            }
        }
    }
}