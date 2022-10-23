using System.Reflection;
using System.Reflection.Emit;

#if NET6_0
using System.Diagnostics.CodeAnalysis;
#endif

namespace DynamicInvoker;

/// <summary>
/// General delegate for generated methods.
/// </summary>
public delegate object? DynamicDelegate(TypedReference @this, object?[] args);

/// <summary>
/// Wrapper class for methods with reflection.
/// </summary>
public abstract class Caller
{
    private static object _dummy = new();
    
    /// <summary>
    /// Gets a dummy object used to make dummy <see cref="System.TypedReference"/>.
    /// DO NOT set value of this property.
    /// </summary>
    /// <returns>a dummy object used to make dummy <see cref="System.TypedReference"/>.</returns>
    public static ref object Dummy => ref _dummy;

    /// <summary>
    /// Wrap <see cref="System.Reflection.MethodInfo"/> to <see cref="System.Reflection.Emit.DynamicMethod"/>.
    /// </summary>
    /// <param name="callee">Specific <see cref="System.Reflection.MethodInfo"/> to wrap.</param>
    /// <param name="type">The <see cref="System.Type"/> that contains <paramref name="callee"/>.</param>
    /// <returns>Wrapped <see cref="System.Reflection.Emit.DynamicMethod"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="callee"/> is null -or- <paramref name="type"/> is null.</exception>
    public static DynamicMethod CreateDynamicMethod(
        MethodInfo callee,
#if NET6_0
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
#endif
        Type type)
    {
        if (callee is null)
            throw new ArgumentNullException(nameof(callee));
        if (type is null)
            throw new ArgumentNullException(nameof(type));

        var method = new DynamicMethod(
            Guid.NewGuid().ToString("N"),
            MethodAttributes.Public | MethodAttributes.Static,
            CallingConventions.Standard,
            typeof(object),
            new[]
            {
                typeof(TypedReference), // typedref target
                typeof(object[]) // object[] args
            },
            typeof(Caller).Module,
            true);
        
        _ = method.DefineParameter(1, ParameterAttributes.None, "target");
        _ = method.DefineParameter(2, ParameterAttributes.None, "args");

        var il = method.GetILGenerator();

        if (!callee.IsStatic) // if callee is instance method
        {
            // (void*) &target
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Refanyval, type);

            if (!type.IsValueType)
            {
                il.Emit(OpCodes.Ldind_Ref);
            }
        }

        var parameters = callee.GetParameters();
        for (var i = 0; i < parameters.Length; ++i)
        {
            // push args[i] onto stack
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldc_I4, i);
            il.Emit(OpCodes.Ldelem_Ref);

            // cast args[i] to parameter type
            var paramT = parameters[i].ParameterType;
            il.Emit(paramT.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, paramT); // cast to parameter type
        }

        // invoke callee
        //
        // OpCodes.Call     : for static   ∵ NOT check whether this == null
        // OpCodes.Callvirt : for instance ∵ check whether this == null
        il.EmitCall(callee.IsStatic ? OpCodes.Call : OpCodes.Callvirt, callee, null);

        if (callee.ReturnType == typeof(void))
        {
            // return null
            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Ret);
        }
        else
        {
            // box return value if return type is value type
            if (callee.ReturnType.IsValueType)
                il.Emit(OpCodes.Box, callee.ReturnType);

            // return return value of callee
            il.Emit(OpCodes.Ret);
        }

        return method;
    }

    /// <summary>
    /// Wrap <see cref="System.Reflection.ConstructorInfo"/> to <see cref="System.Reflection.Emit.DynamicMethod"/>.
    /// </summary>
    /// <param name="callee">Specific <see cref="System.Reflection.ConstructorInfo"/> to wrap.</param>
    /// <param name="type">The <see cref="System.Type"/> that contains <paramref name="callee"/>.</param>
    /// <returns>Wrapped <see cref="System.Reflection.Emit.DynamicMethod"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="callee"/> is null -or- <paramref name="type"/> is null.</exception>
    public static DynamicMethod CreateDynamicMethod(
        ConstructorInfo callee,
#if NET6_0
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
#endif
        Type type)
    {
        if (callee is null)
            throw new ArgumentNullException(nameof(callee));
        if (type is null)
            throw new ArgumentNullException(nameof(type));

        var method = new DynamicMethod(
            Guid.NewGuid().ToString("N"),
            MethodAttributes.Public | MethodAttributes.Static,
            CallingConventions.Standard,
            typeof(object),
            new[]
            {
                typeof(TypedReference), // void* target
                typeof(object[]) // object[] args
            },
            typeof(Caller).Module,
            true);

        _ = method.DefineParameter(1, ParameterAttributes.None, "target");
        _ = method.DefineParameter(2, ParameterAttributes.None, "args");

        var il = method.GetILGenerator();

        var parameters = callee.GetParameters();
        for (var i = 0; i < parameters.Length; ++i)
        {
            // push args[i] onto stack
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldc_I4, i);
            il.Emit(OpCodes.Ldelem_Ref);

            // cast args[i] to parameter type
            var paramT = parameters[i].ParameterType;
            il.Emit(paramT.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, paramT); // cast to parameter type
        }

        il.Emit(OpCodes.Newobj, callee);

        // box return value if return type is value type
        if (type.IsValueType)
            il.Emit(OpCodes.Box, type);
        il.Emit(OpCodes.Ret);

        return method;
    }

    /// <summary>
    /// Wrap generated <see cref="System.Reflection.Emit.DynamicMethod"/> to <see cref="DynamicInvoker.DynamicDelegate"/>.
    /// </summary>
    /// <param name="caller">Specific <see cref="System.Reflection.Emit.DynamicMethod"/> to wrap.</param>
    /// <returns>Wrapped <see cref="DynamicInvoker.DynamicDelegate"/>.</returns>
    public static DynamicDelegate CreateDelegate(DynamicMethod caller)
    {
        return (DynamicDelegate)caller.CreateDelegate(typeof(DynamicDelegate), null);
    }
}