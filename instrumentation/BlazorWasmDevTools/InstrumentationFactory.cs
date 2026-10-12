using System.Reflection;
using System.Reflection.Emit;
using BlazorWasmDevTools.Events;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorWasmDevTools;

public class InstrumentationFactory(IServiceProvider serviceProvider, IEventSink eventSink)
{
    public const string Prefix = "Instrumented";

    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly IEventSink _eventSink = eventSink;
    private ModuleBuilder? _moduleBuilder;

    private Dictionary<Type, Type> _wrappedTypes = new Dictionary<Type, Type>();

    public IComponent Wrap(IComponent component)
    {
        if (_wrappedTypes.TryGetValue(component.GetType(), out var wrappedType))
        {
            return (IComponent)ActivatorUtilities.CreateInstance(_serviceProvider, wrappedType, component)!;
        }

        if (_moduleBuilder == null)
        {
            AssemblyName assemblyName = new AssemblyName("DynamicAssembly");
            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            _moduleBuilder = assemblyBuilder.DefineDynamicModule("DynamicModule");
        }

        Type typeToWrap = component.GetType();
        string name = typeToWrap.Name;
        string wrappedName = Prefix + name;
        //Type baseType = component is ComponentBase ? typeof(ComponentBase) : typeof(IComponent);
        TypeBuilder typeBuilder = _moduleBuilder.DefineType(wrappedName, TypeAttributes.Public | TypeAttributes.Class, typeToWrap);

        FieldBuilder componentField = typeBuilder.DefineField("_component", typeToWrap, FieldAttributes.Private);

        // Define a constructor that takes the original component as a parameter
        ConstructorBuilder ctor = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new Type[] { typeToWrap });
        ILGenerator ctorIL = ctor.GetILGenerator();
        ctorIL.Emit(OpCodes.Ldarg_0); // 'this'
        ctorIL.Emit(OpCodes.Call, typeof(object).GetConstructor(Type.EmptyTypes)!); // Call base constructor
        ctorIL.Emit(OpCodes.Ldarg_0); // 'this'
        ctorIL.Emit(OpCodes.Ldarg_1); // Load the component parameter
        ctorIL.Emit(OpCodes.Stfld, componentField); // Store it in the _component field
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

        // Get all the properties for the component
        var properties = typeToWrap.GetProperties(BindingFlags.Instance | BindingFlags.Public);
        foreach (var p in properties)
        {
            MethodInfo? getter = p.GetMethod;
            MethodInfo? setter = p.SetMethod;

            if (getter == null || setter == null)
            {
                continue; // Skip properties without both getter and setter
            }

            PropertyBuilder propertyBuilder = typeBuilder.DefineProperty(p.Name, p.Attributes, p.PropertyType, Type.EmptyTypes);

            // We will need a getter and a setter
            MethodBuilder getterBuilder = typeBuilder.DefineMethod("get_" + p.Name, MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, p.PropertyType, Type.EmptyTypes);
            ILGenerator getterIL = getterBuilder.GetILGenerator();
            // What we should be doing here is calling the original getter of the property, on the underlying component instance.
            getterIL.Emit(OpCodes.Ldarg_0); // 'this'
            getterIL.Emit(OpCodes.Ldfld, componentField); // Load the underlying component instance onto the stack
            getterIL.Emit(OpCodes.Call, getter);
            getterIL.Emit(OpCodes.Ret);

            MethodBuilder setterBuilder = typeBuilder.DefineMethod("set_" + p.Name, MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, null, new Type[] { p.PropertyType });
            ILGenerator setterIL = setterBuilder.GetILGenerator();
            if (miPropertySet is not null)
            {
                setterIL.Emit(OpCodes.Ldstr, p.Name); // Load the property name onto the stack
                setterIL.Emit(OpCodes.Ldarg_1); // Load the value argument onto the stack
                setterIL.Emit(OpCodes.Call, miPropertySet);
            }
            setterIL.Emit(OpCodes.Ldarg_0); // 'this'
            setterIL.Emit(OpCodes.Ldarg_1); // Load the value argument onto the stack
            setterIL.Emit(OpCodes.Call, setter);
            setterIL.Emit(OpCodes.Ret);

            propertyBuilder.SetGetMethod(getterBuilder);
            propertyBuilder.SetSetMethod(setterBuilder);
        }

        var methods = typeToWrap.GetMethods(BindingFlags.Instance | BindingFlags.Public);
        foreach (var m in methods)
        {
            MethodBuilder methodBuilder = typeBuilder.DefineMethod(m.Name, m.Attributes, m.ReturnType, m.GetParameters().Select(p => p.ParameterType).ToArray());
            ILGenerator methodIL = methodBuilder.GetILGenerator();
            methodIL.Emit(OpCodes.Ldarg_0); // 'this'
            for (int i = 1; i <= m.GetParameters().Length; i++)
            {
                methodIL.Emit(OpCodes.Ldarg, i); // Load each argument onto the stack
            }
            methodIL.Emit(OpCodes.Call, m);
            methodIL.Emit(OpCodes.Ret);
        }

        // TODO: Events?

        Type generated = typeBuilder.CreateType();
        _wrappedTypes[component.GetType()] = generated;

        return (IComponent)ActivatorUtilities.CreateInstance(_serviceProvider, generated, component)!;
    }

    // private static void Example()
    // {
    //     // 1. Define a dynamic assembly and module container
    //     AssemblyName assemblyName = new AssemblyName("DynamicAssembly");
    //     AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
    //     ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("DynamicModule");

    //     // 2. Define the public class 'DynamicUser'
    //     TypeBuilder typeBuilder = moduleBuilder.DefineType("DynamicUser", TypeAttributes.Public | TypeAttributes.Class);

    //     // 3. Define a private backing field: private string _name;
    //     FieldBuilder fieldBuilder = typeBuilder.DefineField("_name", typeof(string), FieldAttributes.Private);

    //     // 4. Set up the constructor's parameter types (takes 1 string)
    //     Type[] constructorArgs = { typeof(string) };

    //     // 5. Define the constructor using ConstructorBuilder
    //     ConstructorBuilder constructorBuilder = typeBuilder.DefineConstructor(
    //         MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
    //         CallingConventions.Standard,
    //         constructorArgs);

    //     // 6. Generate the MSIL method body for the constructor
    //     ILGenerator ctorIL = constructorBuilder.GetILGenerator();

    //     // Target: base() [Call parent System.Object constructor]
    //     ctorIL.Emit(OpCodes.Ldarg_0); // Load 'this' pointer onto the stack
    //     ConstructorInfo objectCtor = typeof(object).GetConstructor(Type.EmptyTypes);
    //     ctorIL.Emit(OpCodes.Call, objectCtor); // Call the base constructor

    //     // Target: this._name = argument_1;
    //     ctorIL.Emit(OpCodes.Ldarg_0); // Load 'this' pointer onto the stack
    //     ctorIL.Emit(OpCodes.Ldarg_1); // Load the first argument (string) onto the stack
    //     ctorIL.Emit(OpCodes.Stfld, fieldBuilder); // Store the argument in the field

    //     ctorIL.Emit(OpCodes.Ret); // Return from constructor

    //     // 7. Define a public method 'SayHello' to verify the constructor worked
    //     MethodBuilder methodBuilder = typeBuilder.DefineMethod("SayHello", MethodAttributes.Public, typeof(void), null);
    //     ILGenerator methodIL = methodBuilder.GetILGenerator();

    //     methodIL.Emit(OpCodes.Ldstr, "Hello, my name is: ");
    //     methodIL.Emit(OpCodes.Call, typeof(Console).GetMethod("Write", new[] { typeof(string) }));

    //     methodIL.Emit(OpCodes.Ldarg_0); // 'this'
    //     methodIL.Emit(OpCodes.Ldfld, fieldBuilder); // Load field '_name'
    //     methodIL.Emit(OpCodes.Call, typeof(Console).GetMethod("WriteLine", new[] { typeof(string) }));

    //     methodIL.Emit(OpCodes.Ret);

    //     // 8. Bake and instantiate the dynamic type
    //     Type dynamicType = typeBuilder.CreateType(); // Finishes type creation

    //     // Instantiate using our custom constructor
    //     object[] instanceArgs = { "Alice" };
    //     object dynamicInstance = Activator.CreateInstance(dynamicType, instanceArgs);

    //     // Call the method to verify it prints correctly
    //     MethodInfo sayHelloMethod = dynamicType.GetMethod("SayHello");
    //     sayHelloMethod.Invoke(dynamicInstance, null);
    // }
}