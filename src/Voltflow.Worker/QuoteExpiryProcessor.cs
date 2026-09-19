using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Voltflow.Application.Interfaces;

namespace Voltflow.Worker;

/// <summary>Expires the issued quotes whose last day of validity has passed, so they stop showing as open offers.</summary>
public sealed class QuoteExpiryProcessor
{
    private readonly IQuoteRepository _quotes;
    private readonly ILogger<QuoteExpiryProcessor> _logger;

    public QuoteExpiryProcessor(IQuoteRepository quotes, ILogger<QuoteExpiryProcessor> logger)
    {
        _quotes = quotes;
        _logger = logger;
    }

    public async Task<int> ExpireDueAsync(DateTime utcNow, CancellationToken ct = default)
    {
        var due = await _quotes.ListDueForExpiryAsync(DateOnly.FromDateTime(utcNow), ct);
        var expired = 0;
        foreach (var quote in due)
        {
            try
            {
                if (!quote.ExpireIfDue(utcNow)) continue;
                await _quotes.UpdateAsync(quote, ct);
                expired++;
            }
            catch (DbUpdateConcurrencyException)
            {
                // someone decided the quote at the same moment; the next sweep looks at it again if it is still open
                _logger.LogWarning("Quote {QuoteId} changed while it was being expired; it is left as it is.", quote.Id);
            }
        }

        return expired;
    }
}
