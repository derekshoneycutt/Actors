namespace Actors;

/// <summary>
/// Abstract base record for machine state objects.
/// All state types used in Machine&lt;TInput, TState&gt; must inherit from this.
/// Provides a type-safe constraint and allows future shared state logic.
/// </summary>
public abstract record MachineState;
