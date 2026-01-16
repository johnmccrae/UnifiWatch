using UnifiWatch.Models;

namespace UnifiWatch.Services;

public interface IUnifiStockService
{
    Task<List<UnifiProduct>> GetStockAsync(string store, string[]? collections = null, CancellationToken cancellationToken = default);
}
