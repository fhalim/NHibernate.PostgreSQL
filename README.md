# Introduction
Implementation of NServiceBus Persistence on JSONB in PostgreSQL

# Motivation
We'd like to take advantage of native JSON storage in PostgreSQL to allow easier/faster
querying for NServiceBus usage.

# Status
This is currently alpha quality software

[![fawad MyGet Build Status](https://www.myget.org/BuildSource/Badge/fawad?identifier=5a5a9722-edab-4452-b8b4-d4045ccb8964)](https://www.myget.org/)

# Configuration

## Required

- Connection string named `NServiceBus/Persistence/PostgreSQL` referencing the PostgreSQL database where to persist the data.

### Example

```xml
<connectionStrings>
  <add name="NServiceBus/Persistence/PostgreSQL" providerName="Npgsql" connectionString="Server=127.0.0.1;Port=5432;User Id=user;Password=password;Database=dev;"/>
</connectionStrings>

<system.data>
  <DbProviderFactories>
    <remove invariant="Npgsql"></remove>
    <add name="Npgsql Data Provider" invariant="Npgsql" support="FF" description=".Net Framework Data Provider for Postgresql Server" type="Npgsql.NpgsqlFactory, Npgsql" />
  </DbProviderFactories>
</system.data>
```

- While bootstrapping the Bus, execute

```csharp
void Configure(BusConfiguration configuration)
{
  configuration.UsePersistence<PostgreSQLPersistence>();
}
```

## Optional

### Application settings

- NServiceBus/Outbox/PostgreSQL/FrequencyToRunDeduplicationDataCleanup: Timespan between cleanup calls. Defaults to 1 minute.
- NServiceBus/Outbox/PostgreSQL/TimeToKeepDeduplicationData: Duration to keep dispatched outbox messages. Defaults to 7 days.

# Known limitations

- Gateway deduplication has not been implemented in this backend.
