using System.Runtime.CompilerServices;

namespace BlazorWasmDevTools.Lifecycle;

/// <summary>
/// Stable ids for components that report lifecycle through <see cref="Components.InstrumentedComponentBase"/>.
/// These ids are independent from Blazor renderer component ids unless also observed via renderer logging.
/// </summary>
internal static class ComponentInstanceIds
{
    private static int _nextId = 1_000_000;
    private static readonly ConditionalWeakTable<object, ComponentIdHolder> Ids = new();

    public static int GetOrAssign(object component)
    {
        if (Ids.TryGetValue(component, out var holder))
        {
            return holder.Id;
        }

        holder = new ComponentIdHolder(Interlocked.Increment(ref _nextId));
        Ids.Add(component, holder);
        return holder.Id;
    }

    private sealed class ComponentIdHolder(int id)
    {
        public int Id { get; } = id;
    }
}
