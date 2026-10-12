using BlazorWasmDevTools.Events;
using BlazorWasmDevTools.Tests.Components;
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
        Assert.IsAssignableFrom<TestComponent>(wrappedComponent);
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
        var wrappedComponent = factory.Wrap(component);

        // Assert
        Assert.Equal("Hello", ((TestComponent)wrappedComponent).TestString);
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
        Assert.Equal("World", ((TestComponent)wrappedComponent).TestString);
        Assert.Equal("World", component.TestString);
    }
}