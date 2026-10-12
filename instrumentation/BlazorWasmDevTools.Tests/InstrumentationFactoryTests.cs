using System.Reflection;
using BlazorWasmDevTools.Events;
using BlazorWasmDevTools.Tests.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace BlazorWasmDevTools.Tests;

public sealed class InstrumentationFactoryTests
{
    [Fact]
    public void Wrap_ShouldReturnWrappedComponent()
    {
        // Arrange
        var serviceProvider = new ServiceCollection()
            .AddScoped<TestComponent>()
            .BuildServiceProvider();
        var eventSink = Substitute.For<IEventSink>();
        var factory = new InstrumentationFactory(serviceProvider, eventSink);
        var component = new TestComponent();

        // Act
        var wrappedComponent = factory.Wrap(component);

        // Assert
        Assert.NotNull(wrappedComponent);
        // We don't want to assert that the wrapped component is of the original type, since it will be a dynamically generated type.
        // If we inherit from the original component type, then we will have issues with shadowing properties and methods - better to see cast failures and know that our strategy won't work.
        //Assert.IsAssignableFrom<TestComponent>(wrappedComponent);
    }

    [Fact]
    public void Wrap_ShouldReturnDifferentInstance()
    {
        // Arrange
        var serviceProvider = new ServiceCollection()
            .AddScoped<TestComponent>()
            .BuildServiceProvider();
        var eventSink = Substitute.For<IEventSink>();
        var factory = new InstrumentationFactory(serviceProvider, eventSink);
        var component = new TestComponent();

        // Act
        var wrappedComponent = factory.Wrap(component);

        // Assert
        Assert.NotSame(component, wrappedComponent);
    }

    [Fact]
    public void Wrap_ShouldPreserveTestString()
    {
        // Arrange
        var serviceProvider = new ServiceCollection()
            .AddScoped<TestComponent>()
            .BuildServiceProvider();
        var eventSink = Substitute.For<IEventSink>();
        var factory = new InstrumentationFactory(serviceProvider, eventSink);
        var component = new TestComponent { TestString = "Hello" };

        // Act
        dynamic wrappedComponent = factory.Wrap(component);
        Type t = wrappedComponent.GetType();

        // Assert
        Assert.Equal("Hello", wrappedComponent.TestString);
    }


    [Fact]
    public void Wrap_WrapsProperties()
    {
        // Arrange
        var serviceProvider = new ServiceCollection()
            .AddScoped<TestComponent>()
            .BuildServiceProvider();
        var eventSink = Substitute.For<IEventSink>();
        var factory = new InstrumentationFactory(serviceProvider, eventSink);
        var component = new TestComponent { TestString = "Hello" };

        // Act
        dynamic wrappedComponent = factory.Wrap(component);
        wrappedComponent.TestString = "World";

        // Assert
        Assert.Equal("World", wrappedComponent.TestString);
        Assert.Equal("World", component.TestString);
    }

    [Fact]
    public void Wrap_ShouldForwardBuildRenderTree()
    {
        // Arrange
        var serviceProvider = new ServiceCollection()
            .AddScoped<TestComponent>()
            .BuildServiceProvider();
        var eventSink = Substitute.For<IEventSink>();
        var factory = new InstrumentationFactory(serviceProvider, eventSink);
        var component = new TestComponent();
        Assert.Equal(0, component.BRTInvocationCount);

        // Act
        dynamic wrappedComponent = factory.Wrap(component);

        // Assert
        // We can't directly assert on BuildRenderTree since it's protected, but we can ensure no exceptions are thrown when invoking it via reflection.
        var method = wrappedComponent.GetType().GetMethod("BuildRenderTree", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        var renderTreeBuilder = new RenderTreeBuilder();
        method.Invoke(wrappedComponent, new object[] { renderTreeBuilder });
        Assert.Equal(1, component.BRTInvocationCount);
    }
}