using Microsoft.EntityFrameworkCore;
using Voltflow.Application.Interfaces;
using Voltflow.Domain.Common;
using Voltflow.Infrastructure.Persistence;

namespace Voltflow.Infrastructure.Repositories;

public sealed class DocumentNumbers : IDocumentNumbers
{
    private readonly VoltflowDbContext _dbContext;

    public DocumentNumbers(VoltflowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Func<string>> PrepareAsync(string key, string prefix, CancellationToken ct = default)
    {
        var counter = await _dbContext.DocumentCounters.FirstOrDefaultAsync(x => x.Key == key, ct);
        return () =>
        {
            if (counter is null)
            {
                counter = new DocumentCounter(key);
                _dbContext.DocumentCounters.Add(counter);
            }

            return $"{prefix}-{counter.Next():D6}";
        };
    }
}
