using System.Reflection;
using System.Reflection.Emit;

namespace DynamicInvoker;

public static class Caller
{
    private static DynamicMethod CreateDynamicMethod(MethodInfo callee)
    {
        var method = new DynamicMethod(
            Guid.NewGuid().ToString("N"),
            typeof(object),
            new[] { typeof(object[]) /* object[] args */ })
        {
            InitLocals = false // DO NOT INIT VARIABLES IN .locals init
        };
        _ = method.DefineParameter(1, ParameterAttributes.None, "args"); // define parameter 'object[] args'

        var il = method.GetILGenerator(256);

        _ = il.DeclareLocal(typeof(object)); // .locals init [0]
        
        if (!callee.IsStatic) // if callee is instance method
            il.Emit(OpCodes.Ldarg_0); // load this (target)
        
        var parameters = method.GetParameters();
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
            // return uninitialized value
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Ret);
        }
        else
        {
            // box return value if return type is value type
            if (callee.ReturnType.IsValueType)
                il.Emit(OpCodes.Box, typeof(object));
            
            // return return value of callee
            il.Emit(OpCodes.Ret);
        }

        return method;
    }
}