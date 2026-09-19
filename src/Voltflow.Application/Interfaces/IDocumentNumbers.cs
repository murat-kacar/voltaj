namespace Voltflow.Application.Interfaces;

/// <summary>The running numbers of documents: TK-000001, HS-000042, ...</summary>
public interface IDocumentNumbers
{
    /// <summary>
    /// Prepares the next number of a series. Nothing is taken until the returned function is called, so a document that turns out
    /// to be invalid never uses one up, and the counter is saved together with whatever else the caller commits. Two requests
    /// that prepare the same series at once cannot both save: the second is refused instead of receiving a duplicate.
    /// </summary>
    Task<Func<string>> PrepareAsync(string key, string prefix, CancellationToken ct = default);
}
