using System;
using System.Reflection;

/// <summary>
/// 
/// </summary>
public interface IMethodDecorator
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="method"></param>
    void OnEntry(MethodBase method);
    /// <summary>
    /// 
    /// </summary>
    /// <param name="method"></param>
    void OnExit(MethodBase method);
    /// <summary>
    /// 
    /// </summary>
    /// <param name="method"></param>
    /// <param name="exception"></param>
    void OnException(MethodBase method, Exception exception);
}
