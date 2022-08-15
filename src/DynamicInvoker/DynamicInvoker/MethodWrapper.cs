using System.Reflection;
using System.Reflection.Emit;

namespace DynamicInvoker;

public static class MethodWrapper
{
    private static DynamicMethod CreateDynamicMethod(MethodInfo callee)
    {
        var method = new DynamicMethod(
            Guid.NewGuid().ToString("N"),
            typeof(object),
            new[] { typeof(object[]) })
        {
            InitLocals = false
        };
        _ = method.DefineParameter(1, ParameterAttributes.None, "args");

        var il = method.GetILGenerator(256);

        _ = il.DeclareLocal(typeof(object));
        
        if (!callee.IsStatic)
            il.Emit(OpCodes.Ldarg_0);
        
        var parameters = method.GetParameters();
        for (var i = 0; i < parameters.Length; ++i)
        {
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldc_I4, i); 
            il.Emit(OpCodes.Ldelem_Ref);

            var paramT = parameters[i].ParameterType;
            il.Emit(paramT.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, paramT);
        }
        
        il.EmitCall(callee.IsStatic ? OpCodes.Call : OpCodes.Callvirt, callee, null);

        if (callee.ReturnType == typeof(void))
        {
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Ret);
        }
        else
        {
            if (callee.ReturnType.IsValueType)
                il.Emit(OpCodes.Box, typeof(object));
            
            il.Emit(OpCodes.Ret);
        }

        return method;
    }
}