using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace DynamicInvoker;

/// <summary>
/// Wrapper class for constructors with reflection
/// </summary>
public class ConstructorCaller : Caller
{
    /// <summary>
    /// Create dynamic wrapper for .ctor.
    /// </summary>
    /// <param name="type">The type that contains specific .ctor.</param>
    /// <param name="args"></param>
    /// <returns></returns>
    public static ConstructorCaller Create(
#if NET6_0
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
#endif
        Type type, Type[] args)
    {
        return new ConstructorCaller(type, args);
    }
    
    private ConstructorCaller(Type type, Type[] args)
    {
        var ctor = type.GetConstructor(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            CallingConventions.HasThis,
            args,
            null);
        if (ctor is null)
            throw new MissingMethodException(
                $"cannot find {type.FullName}::ctor({string.Join(", ", args.Select(t => t.FullName))})");

        Ctor = CreateDelegate(CreateDynamicMethod(ctor, type));
    }

    private DynamicDelegate Ctor { get; }

    /// <summary>
    /// Initialize new object.
    /// </summary>
    /// <param name="args">Arguments of .ctor.</param>
    /// <returns>Initialized object.</returns>
    public object Call(object[] args)
    {
        return Ctor.Invoke(__makeref(Const.Dummy), args)!;
    }
}