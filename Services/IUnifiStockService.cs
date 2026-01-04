using UnifiStockTracker.Models;

namespace UnifiStockTracker.Services;

public interface IUnifiStockService
{
    Task<List<UnifiProduct>> GetStockAsync(string store, string[]? collections = null, CancellationToken cancellationToken = default);
}
