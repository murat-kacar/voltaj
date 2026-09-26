namespace Voltflow.Api.Health;

/// <summary>T6 startup semantics, kept as pure logic so every branch is unit-testable: started means the
/// one-time initialization has finished AND the schema has no pending migrations.</summary>
public static class StartupProbe
{
    public static async Task<(bool Started, string Status)> EvaluateAsync(
        StartupState state,
        Func<CancellationToken, Task<bool>> hasPendingMigrations,
        CancellationToken ct = default)
    {
        if (!state.IsCompleted) return (false, "starting");
        if (await hasPendingMigrations(ct)) return (false, "migrations_pending");
        return (true, "started");
    }
}
