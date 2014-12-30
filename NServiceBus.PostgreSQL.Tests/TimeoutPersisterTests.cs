using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NServiceBus.PostgreSQL.Tests
{
    using System.Data;
    using Dapper;
    using Npgsql;

    class TimeoutPersisterTests
    {
        private readonly Func<IDbConnection> _connFactory;
        public TimeoutPersisterTests()
        {
            const string connstring = "Server=127.0.0.1;Port=5432;User Id=NServiceBus.PostgreSQL.Tests;Password=password;Database=dev;";
            _connFactory = () => new NpgsqlConnection(connstring);
            using (var conn = _connFactory())
            {
                conn.Execute("DROP TABLE IF EXISTS timeouts");
            }
        }
    }
}
