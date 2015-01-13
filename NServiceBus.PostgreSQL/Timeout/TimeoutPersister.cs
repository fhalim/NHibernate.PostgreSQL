namespace NServiceBus.PostgreSQL.Timeout
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using Dapper;
    using Logging;
    using Newtonsoft.Json;
    using NServiceBus.Timeout.Core;

    public class TimeoutPersister : IPersistTimeouts
    {
        static readonly ILog Logger = LogManager.GetLogger(typeof(TimeoutPersister));
        private readonly Func<IDbConnection> _connectionFactory;

        public TimeoutPersister(ConnectionFactoryHolder connectionFactoryHolder)
        {
            _connectionFactory = connectionFactoryHolder.ConnectionFactory;
        }

        public string EndpointName { get; set; }

        [Time]
        public IEnumerable<Tuple<string, DateTime>> GetNextChunk(DateTime startSlice, out DateTime nextTimeToRunQuery)
        {
            using (var conn = _connectionFactory())
            {
                var param = new {endpointname = EndpointName, starttime = startSlice, endtime = DateTime.UtcNow};
                var res = conn.Query(
                    "SELECT id, time FROM timeouts WHERE endpointname = :endpointname AND time BETWEEN :starttime AND :endtime",
                    param)
                    .Select(r => Tuple.Create((string) r.id, (DateTime) r.time));
                var startOfNextChunk = conn.Query<DateTime>(
                    "SELECT time FROM timeouts WHERE endpointname = :endpointname AND time > :endtime ORDER BY TIME ASC LIMIT 1",
                    param).ToArray();
                if (startOfNextChunk.Any())
                {
                    nextTimeToRunQuery = startOfNextChunk.First();
                }
                else
                {
                    nextTimeToRunQuery = DateTime.UtcNow.AddMinutes(10);
                }
                return res;
            }
        }

        [Time]
        public void Add(TimeoutData timeout)
        {
            var id = Guid.NewGuid().ToString();
            using (var conn = _connectionFactory())
            {
                var timeoutInfo = new DynamicParameters(new
                {
                    id,
                    Destination = timeout.Destination != null ? timeout.Destination.ToString() : null,
                    timeout.SagaId,
                    timeout.State,
                    timeout.Time,
                    EndpointName
                });
                timeoutInfo.Add("Headers", JsonConvert.SerializeObject(timeout.Headers));
                conn.Execute(
                    "INSERT INTO timeouts (id, destination, sagaid, state, time, headers, endpointname) VALUES (:Id, :Destination, :SagaId, :State, :Time, :Headers, :EndpointName)",
                    timeoutInfo);
                timeout.Id = id;
            }
        }

        [Time]
        public bool TryRemove(string timeoutId, out TimeoutData timeoutData)
        {
            using (var conn = _connectionFactory())
            {
                timeoutData =
                    conn.Query(
                        "SELECT id, destination, sagaid, state, time, headers, endpointname FROM timeouts WHERE id = :id",
                        new {id = timeoutId})
                        .Select(
                            i =>
                                new TimeoutData
                                {
                                    Id = i.id,
                                    Destination = i.destination != null ? Address.Parse(i.destination) : null,
                                    SagaId = i.sagaid,
                                    State = i.state,
                                    Headers = JsonConvert.DeserializeObject<Dictionary<string, string>>(i.headers),
                                    OwningTimeoutManager = i.endpointname
                                }).FirstOrDefault();
                var result = timeoutData != default(TimeoutData);
                conn.Execute("DELETE FROM timeouts WHERE id = :id", new {id = timeoutId});
                return result;
            }
        }

        [Time]
        public void RemoveTimeoutBy(Guid sagaId)
        {
            using (var conn = _connectionFactory())
            {
                conn.Execute("DELETE FROM timeouts WHERE sagaid = :sagaId", new {sagaId});
            }
        }

        public static void Initialize(Func<IDbConnection> connFactory)
        {
            Logger.Info("Initializing");
            using (var conn = connFactory())
            {
                conn.Execute(
                    "CREATE TABLE IF NOT EXISTS timeouts(id TEXT, destination TEXT, sagaid UUID, state BYTEA, time TIMESTAMP WITHOUT TIME ZONE, headers JSONB, endpointname TEXT, PRIMARY KEY (id))");
                conn.CreateIndexIfNotExists("CREATE INDEX idx_timeouts_sagaid ON timeouts (sagaid)",
                    "idx_timeouts_sagaid");
                conn.CreateIndexIfNotExists("CREATE INDEX idx_timeouts_time ON timeouts (endpointname, time)",
                    "idx_timeouts_time");
            }
        }
    }
}