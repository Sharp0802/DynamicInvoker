# Dynamic Invoker

**⚠️Experimental project⚠️**

## Introduction

**DynamicInvoker** : More efficient way to invoke method dynamically.

It uses Lightweight Code Generation (LCG) to wrap `MethodInfo` to uniform delegate(`Func<object, object[], object>`).

## Getting started

1. Download source
2. Build src/DynamicInvoker/DynamicInvoker/DynamicInvoker.csproj
3. Add reference (build output: DynamicInvoker.dll) to your project

### Methods

```c#
class Example
{
    public static double Foo(double boo)
    {
        return boo;
    }
}

var caller = MethodCaller.Create(typeof(Example), typeof(Example).GetMethod("Foo")!);
var res = caller.Call(1.0);
```

### Constructors

```c#
class Example
{
    public Example(int a, int b) {}
}

var caller = ConstructorCaller.Create(typeof(Example), new[] { typeof(int), typeof(int) });
var res = caller.Call(1, 2); // construct one
```

### Properties

```c#
class Example
{
    public int Target { get; set; }
}

var caller = PropertyCaller.Create(typeof(Example), typeof(Example).GetProperty("Target")!);

var target = new Example();

var value = caller.Get(__makeref(target)); // get value and store in variable
caller.Set(__makeref(target), value + 1); // set value to value of variable + 1
```