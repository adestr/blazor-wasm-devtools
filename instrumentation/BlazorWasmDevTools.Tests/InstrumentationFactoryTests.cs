using System.Reflection;
using BlazorWasmDevTools.Events;
using BlazorWasmDevTools.Tests.Components;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace BlazorWasmDevTools.Tests;

public sealed class InstrumentationFactoryTests
{
    [Fact]
    public void InstrumentationFactory_Wrap_ShouldReturnComponent_WhichExtendsWrappedComponent()
    {
        // Arrange
        var eventSink = Substitute.For<IEventSink>();
        var factory = new InstrumentationFactory(eventSink, NullLogger<InstrumentationFactory>.Instance);

        // Act
        var wrappedComponentType = factory.Wrap(typeof(TestComponent));

        // Assert
        Assert.NotNull(wrappedComponentType);
        Assert.True(wrappedComponentType.BaseType == typeof(TestComponent), "Wrapped component's base type should be TestComponent");
    }

    [Fact]
    public void Wrap_ShouldReturnDifferentInstance()
    {
        // Arrange
        var eventSink = Substitute.For<IEventSink>();
        var factory = new InstrumentationFactory(eventSink, NullLogger<InstrumentationFactory>.Instance);
        var component = new TestComponent();

        // Act
        var wrappedComponentType = factory.Wrap(typeof(TestComponent));

        // Assert
        Assert.NotSame(component, wrappedComponentType);
    }

    [Fact]
    public void Wrap_ShouldForwardBuildRenderTree()
    {
        // Arrange
        var serviceProvider = new ServiceCollection()
            .AddScoped<TestComponent>()
            .BuildServiceProvider();
        var eventSink = Substitute.For<IEventSink>();
        var factory = new InstrumentationFactory(eventSink, NullLogger<InstrumentationFactory>.Instance);
        var component = new TestComponent();
        Assert.Equal(0, component.BRTInvocationCount);

        // Act
        var wrappedComponentType = factory.Wrap(typeof(TestComponent));
        dynamic wrappedComponent = ActivatorUtilities.CreateInstance(serviceProvider, wrappedComponentType);

        // Assert
        // We can't directly assert on BuildRenderTree since it's protected, but we can ensure no exceptions are thrown when invoking it via reflection.
        Assert.NotNull(wrappedComponent);
        var method = wrappedComponent.GetType().GetMethod("BuildRenderTree", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        var renderTreeBuilder = new RenderTreeBuilder();
        method.Invoke(wrappedComponent, new object[] { renderTreeBuilder });
        Assert.Equal(1, wrappedComponent.BRTInvocationCount);
    }

    [Fact]
    public void Wrap_WhenTypeDoesNotImplementIComponent_ReturnsOriginalType()
    {
        // Arrange
        var factory = CreateFactory();

        // Act
        var result = factory.Wrap(typeof(NotAComponent));

        // Assert
        Assert.Same(typeof(NotAComponent), result);
    }

    [Fact]
    public void Wrap_WhenTypeIsSealed_ReturnsOriginalType()
    {
        // Arrange
        var factory = CreateFactory();

        // Act
        var result = factory.Wrap(typeof(SealedComponent));

        // Assert
        Assert.True(typeof(IComponent).IsAssignableFrom(typeof(SealedComponent)));
        Assert.Same(typeof(SealedComponent), result);
    }

    [Fact]
    public void Wrap_WhenTypeHasNoPublicConstructor_ReturnsOriginalType()
    {
        // Arrange
        var factory = CreateFactory();

        // Act
        var result = factory.Wrap(typeof(PrivateConstructorComponent));

        // Assert
        Assert.Same(typeof(PrivateConstructorComponent), result);
    }

    [Fact]
    public void Wrap_WhenTypeImplementsIComponentWithoutComponentBase_ReturnsRenderableSubclass()
    {
        // Arrange
        var factory = CreateFactory();
        using var ctx = new BunitContext();

        // Act
        var wrappedType = factory.Wrap(typeof(ComponentBaseFreeComponent));
        var cut = ctx.Render(builder =>
        {
            builder.OpenComponent(0, wrappedType);
            builder.CloseComponent();
        });

        // Assert
        Assert.False(typeof(ComponentBase).IsAssignableFrom(typeof(ComponentBaseFreeComponent)));
        Assert.NotSame(typeof(ComponentBaseFreeComponent), wrappedType);
        Assert.Equal(typeof(ComponentBaseFreeComponent), wrappedType.BaseType);
        Assert.True(typeof(IComponent).IsAssignableFrom(wrappedType));
        cut.MarkupMatches("<p>component-base-free</p>");
    }

    [Fact]
    public void Wrap_WhenNonComponentBaseOverridesRender_InvokesOverride()
    {
        // Arrange
        var factory = CreateFactory();
        using var ctx = new BunitContext();

        // Act
        var wrappedType = factory.Wrap(typeof(RenderOverrideComponent));
        var cut = ctx.Render(builder =>
        {
            builder.OpenComponent(0, wrappedType);
            builder.CloseComponent();
        });

        // Assert
        cut.MarkupMatches("<p>overridden-render</p>");
    }

    [Fact]
    public void Wrap_WhenFrameworkComponentDoesNotDeriveFromComponentBase_StillRenders()
    {
        // Arrange
        var factory = CreateFactory();
        using var ctx = new BunitContext();

        // Act
        var wrappedType = factory.Wrap(typeof(LayoutView));
        var cut = ctx.Render(builder =>
        {
            builder.OpenComponent(0, wrappedType);
            builder.AddAttribute(1, nameof(LayoutView.Layout), typeof(WrappingLayout));
            builder.AddAttribute(2, nameof(LayoutView.ChildContent), (RenderFragment)(child =>
            {
                child.AddMarkupContent(0, "hello");
            }));
            builder.CloseComponent();
        });

        // Assert
        Assert.False(typeof(ComponentBase).IsAssignableFrom(typeof(LayoutView)));
        Assert.NotSame(typeof(LayoutView), wrappedType);
        Assert.True(typeof(IComponent).IsAssignableFrom(wrappedType));
        cut.MarkupMatches("<div>hello</div>");
    }

    [Fact]
    public void Wrap_WhenIComponentIsImplementedExplicitly_StillRenders()
    {
        // Arrange
        var factory = CreateFactory();
        using var ctx = new BunitContext();

        // Act
        var wrappedType = factory.Wrap(typeof(ExplicitComponent));
        var cut = ctx.Render(builder =>
        {
            builder.OpenComponent(0, wrappedType);
            builder.CloseComponent();
        });

        // Assert
        Assert.False(typeof(ComponentBase).IsAssignableFrom(typeof(ExplicitComponent)));
        Assert.Equal(typeof(ExplicitComponent), wrappedType.BaseType);
        cut.MarkupMatches("<p>explicit</p>");
    }

    private static InstrumentationFactory CreateFactory()
    {
        return new InstrumentationFactory(Substitute.For<IEventSink>(), NullLogger<InstrumentationFactory>.Instance);
    }

    private sealed class SealedComponent : ComponentBase;

    private class NotAComponent;

    private class PrivateConstructorComponent : IComponent
    {
        private PrivateConstructorComponent()
        {
        }

        public void Attach(RenderHandle renderHandle)
        {
        }

        public Task SetParametersAsync(ParameterView parameters) => Task.CompletedTask;
    }

    // Public so the dynamic assembly created by the factory can subclass them.
    public class ExplicitComponent : IComponent
    {
        private RenderHandle _renderHandle;

        void IComponent.Attach(RenderHandle renderHandle)
        {
            _renderHandle = renderHandle;
        }

        Task IComponent.SetParametersAsync(ParameterView parameters)
        {
            _renderHandle.Render(builder =>
            {
                builder.OpenElement(0, "p");
                builder.AddContent(1, "explicit");
                builder.CloseElement();
            });
            return Task.CompletedTask;
        }
    }

    public class ComponentBaseFreeComponent : IComponent
    {
        private RenderHandle _renderHandle;

        public void Attach(RenderHandle renderHandle)
        {
            _renderHandle = renderHandle;
        }

        public Task SetParametersAsync(ParameterView parameters)
        {
            _renderHandle.Render(Render);
            return Task.CompletedTask;
        }

        protected virtual void Render(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "p");
            builder.AddContent(1, "component-base-free");
            builder.CloseElement();
        }
    }

    public class RenderOverrideComponent : ComponentBaseFreeComponent
    {
        protected override void Render(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "p");
            builder.AddContent(1, "overridden-render");
            builder.CloseElement();
        }
    }

    public class WrappingLayout : LayoutComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "div");
            builder.AddContent(1, Body);
            builder.CloseElement();
        }
    }
}