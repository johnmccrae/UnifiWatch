using UnifiWatch.Models;

namespace UnifiWatch.Services;

public interface IunifiwatchService
{
    Task<List<UnifiProduct>> GetStockAsync(string store, string[]? collections = null, CancellationToken cancellationToken = default);
}
