namespace NServiceBus.PostgreSQL
{
    using System;
    using System.Reflection;
    public static class MethodTimeLogger
    {
        public static event EventHandler<MethodExecutionInfo> MethodExecuted;

        public static void Log(MethodBase methodBase, long milliseconds)
        {
            if (MethodExecuted != null)
            {
                MethodExecuted(null, new MethodExecutionInfo(methodBase, TimeSpan.FromMilliseconds(milliseconds)));
            }
        }
    }
}