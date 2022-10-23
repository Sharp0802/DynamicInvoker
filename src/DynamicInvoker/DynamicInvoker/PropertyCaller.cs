﻿using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace DynamicInvoker;

/// <summary>
/// Wrapper class for properties with reflection.
/// </summary>
public class PropertyCaller : Caller
{
    /// <summary>
    /// Create dynamic wrapper for property.
    /// </summary>
    /// <param name="type"><see cref="System.Type"/> that contains <paramref name="property"/>.</param>
    /// <param name="property">Specific <see cref="PropertyInfo"/> to wrap.</param>
    /// <returns>Wrapped <see cref="DynamicInvoker.PropertyCaller"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/> is null -or- <paramref name="property"/> is null.</exception>
    public static PropertyCaller Create(
#if NET6_0
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
#endif
        Type type, 
        PropertyInfo property)
    {
        if (type is null)
            throw new ArgumentNullException(nameof(type));
        if (property is null)
            throw new ArgumentNullException(nameof(property));
        return new PropertyCaller(type, property);
    }
    
    private PropertyCaller(Type type, PropertyInfo property)
    {
        CanWrite = property.CanWrite;
        
        var getter = property.GetGetMethod();
        Getter = CreateDelegate(CreateDynamicMethod(getter, type));
        
        if (CanWrite)
        {
            var setter = property.GetSetMethod();
            Setter = CreateDelegate(CreateDynamicMethod(setter, type));
        }
    }

    /// <summary>
    /// Get whether the value of this property can be set.
    /// </summary>
    public bool CanWrite { get; }
    
    private DynamicDelegate Getter { get; }
    private DynamicDelegate? Setter { get; }

    /// <summary>
    /// Get value of property in <paramref name="instance"/>.
    /// </summary>
    /// <param name="instance">Instance that contains this property.</param>
    /// <returns>The value of property.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="instance"/> is null.</exception>
    public object? Get(TypedReference instance)
    {
        return Getter.Invoke(instance, Array.Empty<object>());
    }

    /// <summary>
    /// Set value of property in <paramref name="instance"/>.
    /// </summary>
    /// <param name="instance">Instance that contains this property.</param>
    /// <param name="value">The value of property.</param>
    /// <exception cref="ArgumentNullException"><paramref name="instance"/> is null.</exception>
    /// <exception cref="InvalidOperationException">This property is read-only.</exception>
    public void Set(TypedReference instance, object? value)
    {
        if (CanWrite)
            Setter!.Invoke(instance, new[] { value });
        else
            throw new InvalidOperationException("the property is read-only.");
    }
}