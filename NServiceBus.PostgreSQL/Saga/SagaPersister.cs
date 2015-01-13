namespace NServiceBus.PostgreSQL.Saga
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using Dapper;
    using Logging;
    using Newtonsoft.Json;
    using NServiceBus.Saga;

    public class SagaPersister : ISagaPersister
    {
        static readonly ILog Logger = LogManager.GetLogger(typeof(SagaPersister));
        private static readonly Func<Type, string> TypeMapper = t => t.FullName;
        private readonly Func<IDbConnection> _connectionFactory;

        public SagaPersister(ConnectionFactoryHolder connectionFactoryHolder)
        {
            _connectionFactory = connectionFactoryHolder.ConnectionFactory;
        }

        [Time]
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
        [Time]
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
        [Time]
        public TSagaData Get<TSagaData>(Guid sagaId) where TSagaData : IContainSagaData
        {
            using (var conn = _connectionFactory())
            {
                var p = new DynamicParameters();
                p.Add(":type", dbType: DbType.String, value: TypeMapper(typeof (TSagaData)));
                p.Add(":id", dbType: DbType.Guid, value: sagaId);
                var data =
                    conn.Query<string>("SELECT sagadata FROM sagas WHERE type = :type AND id = :id", p).FirstOrDefault();
                return data == default(string) ? default(TSagaData) : JsonConvert.DeserializeObject<TSagaData>(data);
            }
        }
        [Time]
        public TSagaData Get<TSagaData>(string propertyName, object propertyValue) where TSagaData : IContainSagaData
        {
            using (var conn = _connectionFactory())
            {
                var p = new DynamicParameters();
                var search = "{" + JsonConvert.SerializeObject(propertyName) + ": " +
                             JsonConvert.SerializeObject(propertyValue) + "}";
                p.Add(":type", dbType: DbType.String, value: TypeMapper(typeof (TSagaData)));
                p.Add(":jsonString", dbType: DbType.String, value: search);
                var data =
                    conn.Query<string>("SELECT sagadata FROM sagas WHERE type = :type AND sagadata @> :jsonString", p)
                        .FirstOrDefault();
                return data == default(string) ? default(TSagaData) : JsonConvert.DeserializeObject<TSagaData>(data);
            }
        }
        [Time]
        public void Complete(IContainSagaData saga)
        {
            using (var conn = _connectionFactory())
            {
                conn.Execute("DELETE FROM sagas WHERE type = :type AND id = :id",
                    new {type = TypeMapper(saga.GetType()), id = saga.Id});
            }
        }

        private static DynamicParameters GetUpdateParameters(IContainSagaData saga)
        {
            var p = new DynamicParameters(new
            {
                type = TypeMapper(saga.GetType()),
                id = saga.Id,
                originalmessageid = saga.OriginalMessageId,
                originator = saga.Originator
            });
            p.Add(":sagadata", dbType: DbType.String, value: JsonConvert.SerializeObject(saga));
            return p;
        }

        public static void Initialize(Func<IDbConnection> connectionFactory, IEnumerable<Type> sagaTypes)
        {
            Logger.Info("Initializing");
            using (var conn = connectionFactory())
            {
                conn.Execute(
                    "CREATE TABLE IF NOT EXISTS sagas(type TEXT, id UUID, originalmessageid TEXT, originator TEXT, sagadata JSONB, PRIMARY KEY (type, id))");
                conn.CreateIndexIfNotExists("CREATE INDEX idx_sagas_json ON sagas USING gin (sagadata jsonb_path_ops)",
                    "idx_sagas_json");

                foreach (var sagaType in sagaTypes.Where(t => UniqueAttribute.GetUniqueProperties(t).Any()))
                {
                    var typeName = TypeMapper(sagaType);
                    var typeCollapsedName = typeName.Replace('.', '_').ToLower();

                    var jsonFieldsExpression = String.Join(", ", UniqueAttribute.GetUniqueProperties(sagaType)
                        .Select(f => String.Format("(sagadata ->> '{0}')", f.Name)));
                    var indexName = String.Format("idx_sagas_json_{0}", typeCollapsedName);
                    var indexCreationStatement = String.Format("CREATE UNIQUE INDEX {0} ON sagas ({1}) WHERE type = '{2}'",
                        indexName, jsonFieldsExpression, typeName
                        );
                    // Create Unique constraint for type
                    conn.CreateIndexIfNotExists(indexCreationStatement, indexName);
                }
            }
        }
    }
}