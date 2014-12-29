namespace NServiceBus.PostgreSQL.Tests
{
    using Saga;

    class FakeSagaData:ContainSagaData
    {
        [Unique]
        public virtual int CorrelationId { get; set; }

        // all other properties you want persisted
        public virtual string Message { get; set; }
    }
}