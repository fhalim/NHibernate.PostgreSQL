namespace NServiceBus.PostgreSQL
{
    using System;

    public static class Stats
    {
        public static event EventHandler<MethodExecutionInfo> MethodExecuted;

        public static void Raise(Object sender, MethodExecutionInfo executionInfo)
        {
            if (MethodExecuted != null)
            {
                MethodExecuted(sender, executionInfo);
            }
            
        }
    }
}