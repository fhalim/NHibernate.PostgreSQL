namespace NServiceBus.PostgreSQL
{
    using System;
    using System.Data;

    public class ConnectionFactoryHolder
    {
        public Func<IDbConnection> ConnectionFactory { get; set; } 
    }
}