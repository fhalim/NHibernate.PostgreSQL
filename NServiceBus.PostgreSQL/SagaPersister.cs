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

        public SagaPersister(ConnectionFactoryHolder connectionFactoryHolder)
        {
            _connectionFactory = connectionFactoryHolder.ConnectionFactory;
            _typeMapper = t => t.FullName;
        }

        public void Save(IContainSagaData saga)
        {
            using (var conn = _connectionFactory())
            {
                var p = GetUpdateParameters(saga);
                conn.Execute(
                    "INSERT INTO sagas(type, id, originalmessageid, originator, sagadata) VALUES (:type, :id, :originalmessageid, :originator, :sagadata)",
                    p);
            }
        }

        public void Update(IContainSagaData saga)
        {
            using (var conn = _connectionFactory())
            {
                var p = GetUpdateParameters(saga);
                conn.Execute(
                    "UPDATE sagas SET originalmessageid = :originalmessageid, originator = :originator, sagadata = :sagadata WHERE type = :type AND id = :id",
                    p);
            }
        }

        public TSagaData Get<TSagaData>(Guid sagaId) where TSagaData : IContainSagaData
        {
            using (var conn = _connectionFactory())
            {
                var p = new DynamicParameters();
                p.Add(":type", dbType: DbType.String, value: _typeMapper(typeof (TSagaData)));
                p.Add(":id", dbType: DbType.Guid, value: sagaId);
                var data =
                    conn.Query<string>("SELECT sagadata FROM sagas WHERE type = :type AND id = :id", p).FirstOrDefault();
                return data == default(string) ? default(TSagaData) : JsonConvert.DeserializeObject<TSagaData>(data);
            }
        }

        public TSagaData Get<TSagaData>(string propertyName, object propertyValue) where TSagaData : IContainSagaData
        {
            using (var conn = _connectionFactory())
            {
                var p = new DynamicParameters();
                var search = "{" + JsonConvert.SerializeObject(propertyName) + ": " +
                             JsonConvert.SerializeObject(propertyValue) + "}";
                Console.WriteLine(search);
                p.Add(":type", dbType: DbType.String, value: _typeMapper(typeof (TSagaData)));
                p.Add(":jsonString", dbType: DbType.String, value: search);
                var data =
                    conn.Query<string>("SELECT sagadata FROM sagas WHERE type = :type AND sagadata @> :jsonString", p)
                        .FirstOrDefault();
                return data == default(string) ? default(TSagaData) : JsonConvert.DeserializeObject<TSagaData>(data);
            }
        }

        public void Complete(IContainSagaData saga)
        {
            using (var conn = _connectionFactory())
            {
                conn.Execute("DELETE FROM sagas WHERE type = :type AND id = :id",
                    new {type = _typeMapper(saga.GetType()), id = saga.Id});
            }
        }

        private DynamicParameters GetUpdateParameters(IContainSagaData saga)
        {
            var p = new DynamicParameters();
            p.Add(":type", dbType: DbType.String, value: _typeMapper(saga.GetType()));
            p.Add(":id", dbType: DbType.Guid, value: saga.Id);
            p.Add(":originalmessageid", dbType: DbType.String, value: saga.OriginalMessageId);
            p.Add(":originator", dbType: DbType.String, value: saga.Originator);
            p.Add(":sagadata", dbType: DbType.String, value: JsonConvert.SerializeObject(saga));
            return p;
        }

        public static void Initialize(Func<IDbConnection> connectionFactory)
        {
            using (var conn = connectionFactory())
            {
                conn.Execute(
                    "CREATE TABLE IF NOT EXISTS sagas(type TEXT, id UUID, originalmessageid TEXT, originator TEXT, sagadata JSONB, PRIMARY KEY (type, id))");
                try
                {
                    conn.Execute("CREATE INDEX idx_sagas_json ON sagas USING gin (sagadata jsonb_path_ops);");
                }
                catch (Exception ex)
                {
                    if (!ex.Message.Contains("already exists"))
                    {
                        throw;
                    }
                }
            }
        }
    }
}