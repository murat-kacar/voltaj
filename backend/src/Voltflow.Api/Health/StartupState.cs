namespace Voltflow.Api.Health;

/// <summary>T6: flips once, when one-time initialization (schema + seed) has finished.</summary>
public sealed class StartupState
{
    private int _completed;

    public bool IsCompleted => Volatile.Read(ref _completed) == 1;

    public void MarkCompleted() => Interlocked.Exchange(ref _completed, 1);
}
