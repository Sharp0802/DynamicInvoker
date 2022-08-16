using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;

namespace DynamicInvoker;

/// <summary>
/// General delegate for generated methods.
/// </summary>
public delegate object? DynamicDelegate(object? @this, object?[] args);

/// <summary>
/// Wrapper class for methods with reflection.
/// </summary>
public abstract class Caller
{
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
                typeof(object), // object? @this 
                typeof(object[]) // object[] args
            },
            typeof(Caller).Module,
            true);

        _ = method.DefineParameter(1, ParameterAttributes.None, "target"); // define parameter 'object target'
        _ = method.DefineParameter(2, ParameterAttributes.None, "args"); // define parameter 'object[] args'

        var il = method.GetILGenerator();

        if (!callee.IsStatic) // if callee is instance method
        {
            if (type.IsValueType)
            {
                il.Emit(OpCodes.Ldarga_S, (short)0); // load this (target)
            }
            else
            {
                il.Emit(OpCodes.Ldarg_0); // load this (target)
                il.Emit(OpCodes.Castclass, type);
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
                typeof(object), // object? target = null
                typeof(object[]) // object[] args
            },
            typeof(Caller).Module,
            true);

        _ = method.DefineParameter(1, ParameterAttributes.None, "target"); // define parameter 'object target'
        _ = method.DefineParameter(2, ParameterAttributes.None, "args"); // define parameter 'object[] args'

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