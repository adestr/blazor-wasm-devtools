using System.Reflection;
using System.Reflection.Emit;
using BlazorWasmDevTools.Events;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace BlazorWasmDevTools;

public class InstrumentationFactory(IEventSink eventSink, ILogger<InstrumentationFactory> logger)
{
    public const string Prefix = "Instrumented";

    private readonly IEventSink _eventSink = eventSink;

    private readonly ILogger<InstrumentationFactory> _logger = logger;

    private ModuleBuilder? _moduleBuilder;

    private Dictionary<Type, Type> _wrappedTypes = new Dictionary<Type, Type>();

    /// <summary>
    /// Wraps <paramref name="componentType"/> with an instrumented layer, using the original type as the base class so that all properties and methods are still available.
    /// </summary>
    /// <param name="componentType"></param>
    /// <returns></returns>
    public Type Wrap(Type componentType)
    {
        // componentType is a better argument; typeToWrap is more meaningful within this method.
        var typeToWrap = componentType;
        if (!typeof(IComponent).IsAssignableFrom(typeToWrap))
        {
            _logger.LogInformation("Type {TypeName} does not implement IComponent and cannot be instrumented", typeToWrap.Name);
            return typeToWrap;
        }
        if (typeToWrap.IsSealed)
        {
            _logger.LogInformation("Type {TypeName} is sealed and cannot be instrumented", typeToWrap.Name);
            return typeToWrap;
        }

        if (_wrappedTypes.TryGetValue(typeToWrap, out var wrappedType))
        {
            return wrappedType;
        }

        if (_moduleBuilder == null)
        {
            AssemblyName assemblyName = new AssemblyName("DynamicAssembly");
            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            _moduleBuilder = assemblyBuilder.DefineDynamicModule("DynamicModule");
        }

        string name = typeToWrap.Name;
        string wrappedName = Prefix + name;
        TypeBuilder typeBuilder = _moduleBuilder.DefineType(wrappedName, TypeAttributes.Public | TypeAttributes.Class, typeToWrap);

        if (!TryWrapConstructor(typeToWrap, typeBuilder))
        {
            return typeToWrap;
        }

        void LogPropertySet(string name, object? value)
        {
            var e = EventFactory.Generic("PropertySet", new Dictionary<string, object?>
            {
                { "Property", name},
                { "Value", value}
            });
            _eventSink.Publish(e);
        }
        MethodInfo? miPropertySet = typeof(InstrumentationFactory).GetMethod(nameof(LogPropertySet), BindingFlags.NonPublic | BindingFlags.Instance);

        WrapPublicVirtualMethods(typeToWrap, typeBuilder);

        WrapBuildRenderTree(typeToWrap, typeBuilder);

        // TODO: Events?

        Type generated = typeBuilder.CreateType();
        _wrappedTypes[typeToWrap] = generated;

        return generated;
    }

    private void LogMethodInvocation(string methodName, params object?[]? args)
    {
        var e = EventFactory.Generic("MethodInvocation", new Dictionary<string, object?>
        {
            { "Method", methodName },
            { "Arguments", args ?? [] }
        });
        _eventSink.Publish(e);
    }

    /// <summary>
    /// Tries to wrap the constructor of the specified type.
    /// </summary>
    /// <remarks>
    /// This method wraps the first public constructor of the specified type, sorted by number of parameters.
    /// </remarks>
    /// <param name="typeToWrap"></param>
    /// <param name="typeBuilder"></param>
    /// <returns>A boolean indicating whether the constructor was successfully wrapped.</returns>
    private bool TryWrapConstructor(Type typeToWrap, TypeBuilder typeBuilder)
    {
        var ctors = typeToWrap.GetConstructors(BindingFlags.Instance | BindingFlags.Public);
        ctors = [.. ctors.OrderBy(ctor => ctor.GetParameters().Length)];
        var targetCtor = ctors.FirstOrDefault();
        if (targetCtor == null)
        {
            _logger.LogWarning("No suitable constructor found for type {TypeName}", typeToWrap.Name);
            return false;
        }

        Type[] parameterTypes = [.. targetCtor.GetParameters().Select(p => p.ParameterType)];
        ConstructorBuilder ctor = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, parameterTypes);
        ILGenerator ctorIL = ctor.GetILGenerator();
        ctorIL.Emit(OpCodes.Ldarg_0); // 'this'
        for (int i = 1; i <= parameterTypes.Length; i++)
        {
            ctorIL.Emit(OpCodes.Ldarg, i); // Load each argument onto the stack
        }
        ctorIL.Emit(OpCodes.Call, targetCtor); // Call base constructor
        ctorIL.Emit(OpCodes.Ret);
        return true;
    }

    /// <summary>
    /// Wraps all public virtual methods of the specified type.
    /// </summary>
    /// <param name="typeToWrap"></param>
    /// <param name="typeBuilder"></param>
    private static void WrapPublicVirtualMethods(Type typeToWrap, TypeBuilder typeBuilder)
    {
        var methods = typeToWrap.GetMethods(BindingFlags.Instance | BindingFlags.Public);
        foreach (var m in methods)
        {
            if (!m.IsVirtual)
            {
                continue;
            }

            var methodAttributes = MethodAttributes.Public | MethodAttributes.ReuseSlot;
            MethodBuilder methodBuilder = typeBuilder.DefineMethod(m.Name, methodAttributes, m.ReturnType, m.GetParameters().Select(p => p.ParameterType).ToArray());
            ILGenerator methodIL = methodBuilder.GetILGenerator();
            methodIL.Emit(OpCodes.Ldarg_0); // 'this'
            for (int i = 1; i <= m.GetParameters().Length; i++)
            {
                methodIL.Emit(OpCodes.Ldarg, i); // Load each argument onto the stack
            }

            if (Inspectable.IsInspectable(m.Name))
            {
                MethodInfo inspectionMethod = typeof(InstrumentationFactory).GetMethod(nameof(LogMethodInvocation), BindingFlags.Instance | BindingFlags.NonPublic)!;
                string name = m.Name;
                methodIL.Emit(OpCodes.Ldstr, name);
                for (int i = 1; i <= m.GetParameters().Length; i++)
                {
                    methodIL.Emit(OpCodes.Ldarg, i); // Load each argument onto the stack
                }
                methodIL.Emit(OpCodes.Call, inspectionMethod);
            }

            methodIL.Emit(OpCodes.Call, m);
            methodIL.Emit(OpCodes.Ret);
        }
    }

    /// <summary>
    /// Wraps the BuildRenderTree method of a Blazor component, if it exists.
    /// </summary>
    /// <param name="typeToWrap"></param>
    /// <param name="typeBuilder"></param>
    private static void WrapBuildRenderTree(Type typeToWrap, TypeBuilder typeBuilder)
    {
        if (typeof(ComponentBase).IsAssignableFrom(typeToWrap))
        {
            MethodInfo? buildRenderTree = typeToWrap.GetMethod("BuildRenderTree", BindingFlags.Instance | BindingFlags.NonPublic);
            if (buildRenderTree != null)
            {
                MethodAttributes methodAttributes = MethodAttributes.Family | MethodAttributes.ReuseSlot;
                MethodBuilder methodBuilder = typeBuilder.DefineMethod(buildRenderTree.Name, methodAttributes, buildRenderTree.ReturnType, buildRenderTree.GetParameters().Select(p => p.ParameterType).ToArray());
                ILGenerator methodIL = methodBuilder.GetILGenerator();
                methodIL.Emit(OpCodes.Ldarg_0); // 'this'
                for (int i = 1; i <= buildRenderTree.GetParameters().Length; i++)
                {
                    methodIL.Emit(OpCodes.Ldarg, i); // Load each argument onto the stack
                }
                methodIL.Emit(OpCodes.Call, buildRenderTree);
                methodIL.Emit(OpCodes.Ret);
            }
        }
    }

    private static class Inspectable
    {
        internal static bool IsInspectable(string methodName)
        {
            return methodName switch
            {
                OnInitialized => true,
                OnInitializedAsync => true,
                OnParametersSet => true,
                OnParametersSetAsync => true,
                OnAfterRender => true,
                OnAfterRenderAsync => true,
                _ => false
            };
        }

        internal const string OnInitialized = "OnInitialized";
        internal const string OnInitializedAsync = "OnInitializedAsync";
        internal const string OnParametersSet = "OnParametersSet";
        internal const string OnParametersSetAsync = "OnParametersSetAsync";
        internal const string OnAfterRender = "OnAfterRender";
        internal const string OnAfterRenderAsync = "OnAfterRenderAsync";
    }
}