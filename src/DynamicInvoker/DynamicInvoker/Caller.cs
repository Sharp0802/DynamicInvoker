using System.Reflection;
using System.Reflection.Emit;

namespace DynamicInvoker;

public class Caller
{
    public static DynamicMethod CreateDynamicMethod(MethodInfo callee, Type type)
    {
        var method = new DynamicMethod(
            Guid.NewGuid().ToString("N"),
            MethodAttributes.Public | MethodAttributes.Static, 
            CallingConventions.Standard,
            typeof(object),
            new[] { typeof(object), typeof(object[]) /* object[] args */ },
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
}