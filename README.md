# Introduction
Implementation of NServiceBus Persistence on JSONB in PostgreSQL

# Motivation
We'd like to take advantage of native JSON storage in PostgreSQL to allow easier/faster
querying for NServiceBus usage.

# Status
This is currently very beta software

[![fawad MyGet Build Status](https://www.myget.org/BuildSource/Badge/fawad?identifier=5a5a9722-edab-4452-b8b4-d4045ccb8964)](https://www.myget.org/)

# Configuration

## Application settings

- NServiceBus/Outbox/PostgreSQL/FrequencyToRunDeduplicationDataCleanup: Timespan between cleanup calls. Defaults to 1 minute.
- NServiceBus/Outbox/PostgreSQL/TimeToKeepDeduplicationData: Duration to keep dispatched outbox messages. Defaults to 7 days.