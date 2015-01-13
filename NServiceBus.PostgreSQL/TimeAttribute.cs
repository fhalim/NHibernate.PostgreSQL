namespace NServiceBus.PostgreSQL
{
    using System;
    using System.Diagnostics;
    using System.Reflection;

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor)]
    internal class TimeAttribute : Attribute, IMethodDecorator
    {
        Stopwatch timer;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="method"></param>
        public void OnEntry(MethodBase method)
        {
            timer = Stopwatch.StartNew();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="method"></param>
        public void OnExit(MethodBase method)
        {
            timer.Stop();
            Stats.Raise(null, new MethodExecutionInfo(method, timer.Elapsed));
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="method"></param>
        /// <param name="exception"></param>
        public void OnException(MethodBase method, Exception exception)
        {
            timer.Stop();
            Stats.Raise(null, new MethodExecutionInfo(method, timer.Elapsed));
        }
    }
}