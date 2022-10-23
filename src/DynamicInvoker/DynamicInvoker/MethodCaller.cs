using System.Reflection;

#if NET6_0
using System.Diagnostics.CodeAnalysis;
#endif

namespace DynamicInvoker;

/// <summary>
/// Wrapper class for method with reflection.
/// </summary>
public class MethodCaller : Caller
{
    /// <summary>
    /// Create dynamic wrapper for method.
    /// </summary>
    /// <param name="type"><see cref="System.Type"/> that contains <paramref name="method"/></param>
    /// <param name="method">Specific <see cref="System.Reflection.MethodInfo"/> to wrap.</param>
    public static MethodCaller Create(
#if NET6_0
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
#endif
        Type type, 
        MethodInfo method)
    {
        return new MethodCaller(method, type);
    }
    
    private MethodCaller(MethodInfo method, Type type)
    {
        var dynMethod = CreateDynamicMethod(method, type);
        Delegate = CreateDelegate(dynMethod);
    }

    private DynamicDelegate Delegate { get; }
    
    /// <summary>
    /// Call the method.
    /// </summary>
    /// <param name="target">Instance that contains this method.</param>
    /// <param name="args">The arguments of method.</param>
    /// <returns>If return type of the method is <see cref="System.Void"/>, null. otherwise, return value of the method.</returns>
    public object? Call(TypedReference target, object?[] args) => Delegate.Invoke(target, args);
}