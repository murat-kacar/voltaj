namespace Voltflow.Domain.Common;

/// <summary>A running number for a kind of document (sale receipts, quotes, ...). The row's <c>Version</c> is a
/// concurrency token, so two requests that read the same value cannot both write it: the second one is rejected
/// instead of receiving a duplicate number.</summary>
public sealed class DocumentCounter : Entity
{
    public string Key { get; private set; } = string.Empty;
    public long LastValue { get; private set; }

    private DocumentCounter() { }

    public DocumentCounter(string key)
    {
        Key = Guard.NotEmpty(key, nameof(key));
    }

    public long Next()
    {
        LastValue++;
        Touch();
        return LastValue;
    }
}
