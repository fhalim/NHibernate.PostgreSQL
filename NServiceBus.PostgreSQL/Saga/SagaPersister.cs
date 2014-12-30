namespace NServiceBus.PostgreSQL.Saga
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using Dapper;
    using Newtonsoft.Json;
    using NServiceBus.Saga;

    public class SagaPersister : ISagaPersister
    {
        private static readonly Func<Type, string> TypeMapper = t => t.FullName;
        private readonly Func<IDbConnection> _connectionFactory;

        public SagaPersister(ConnectionFactoryHolder connectionFactoryHolder)
        {
            _connectionFactory = connectionFactoryHolder.ConnectionFactory;
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
                p.Add(":type", dbType: DbType.String, value: TypeMapper(typeof (TSagaData)));
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
                p.Add(":type", dbType: DbType.String, value: TypeMapper(typeof (TSagaData)));
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
                    new {type = TypeMapper(saga.GetType()), id = saga.Id});
            }
        }

        private static DynamicParameters GetUpdateParameters(IContainSagaData saga)
        {
            var p = new DynamicParameters();
            p.Add(":type", dbType: DbType.String, value: TypeMapper(saga.GetType()));
            p.Add(":id", dbType: DbType.Guid, value: saga.Id);
            p.Add(":originalmessageid", dbType: DbType.String, value: saga.OriginalMessageId);
            p.Add(":originator", dbType: DbType.String, value: saga.Originator);
            p.Add(":sagadata", dbType: DbType.String, value: JsonConvert.SerializeObject(saga));
            return p;
        }

        public static void Initialize(Func<IDbConnection> connectionFactory, IEnumerable<Type> sagaTypes)
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

                foreach (var sagaType in sagaTypes.Where(t => UniqueAttribute.GetUniqueProperties(t).Any()))
                {
                    var typeName = TypeMapper(sagaType);
                    var typeCollapsedName = typeName.Replace('.', '_');

                    var jsonFieldsExpression = String.Join(", ", UniqueAttribute.GetUniqueProperties(sagaType)
                        .Select(f => String.Format("(sagadata -> '{0}')", f.Name)));
                    var indexCreationStatement = String.Format(
                        "CREATE UNIQUE INDEX CONCURRENTLY idx_sagas_json_{0} ON sagas({1}) WHERE type = '{2}'",
                        typeCollapsedName, jsonFieldsExpression, typeName
                        );
                    // Create Unique constraint for type
                    try
                    {
                        conn.Execute(indexCreationStatement);
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
}