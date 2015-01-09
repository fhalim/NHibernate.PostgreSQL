namespace NServiceBus.PostgreSQL
{
    using System;
    using System.Reflection;

    public class MethodExecutionInfo
    {
        private readonly MethodBase _methodBase;
        private readonly TimeSpan _timeSpan;

        public MethodExecutionInfo(MethodBase methodBase, TimeSpan timeSpan)
        {
            _methodBase = methodBase;
            _timeSpan = timeSpan;
        }

        public TimeSpan TimeSpan
        {
            get { return _timeSpan; }
        }

        public MethodBase MethodBase
        {
            get { return _methodBase; }
        }
    }
}