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

        // TODO: Wrap this in a try / catch and if anything goes wrong, just return the original component

        string name = typeToWrap.Name;
        string wrappedName = Prefix + name;
        TypeBuilder typeBuilder = _moduleBuilder.DefineType(wrappedName, TypeAttributes.Public | TypeAttributes.Class, typeToWrap);

        var ctors = typeToWrap.GetConstructors(BindingFlags.Instance | BindingFlags.Public);
        ctors = [.. ctors.OrderBy(ctor => ctor.GetParameters().Length)];
        var targetCtor = ctors.FirstOrDefault();
        if (targetCtor == null)
        {
            _logger.LogWarning("No suitable constructor found for type {TypeName}", typeToWrap.Name);
            return typeToWrap;
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

        var methods = typeToWrap.GetMethods(BindingFlags.Instance | BindingFlags.Public);
        foreach (var m in methods)
        {
            if (!m.IsVirtual)
            {
                // Do not wrap non-virtual methods, as they cannot be overridden.
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
            methodIL.Emit(OpCodes.Call, m);
            methodIL.Emit(OpCodes.Ret);
        }

        // For ComponentBase, we must transfer invocations for BuildRenderTree to the underlying component instance.
        // It's a protected method, but we still need to forward calls to the underlying component instance.
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

        // TODO: Events?

        Type generated = typeBuilder.CreateType();
        _wrappedTypes[typeToWrap] = generated;

        return generated;
    }
}