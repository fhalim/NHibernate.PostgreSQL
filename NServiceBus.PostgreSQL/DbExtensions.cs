namespace NServiceBus.PostgreSQL
{
    using System;
    using System.Data;
    using Dapper;

    static class DbExtensions
    {
        public static void CreateIndexIfNotExists(this IDbConnection conn, string indexDefinition, string indexName, string schema = null)
        {
            var schemaFieldString = schema == null ? "CURRENT_SCHEMA()" : "'" + schema + "'";
            var ddl =
                String.Format(
                    "DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace WHERE c.relname = '{0}' AND n.nspname = {1}) THEN {2}; END IF;END $$", indexName, schemaFieldString, indexDefinition);
            conn.Execute(ddl);
        }
    }
}
