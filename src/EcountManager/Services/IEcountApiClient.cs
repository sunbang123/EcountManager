using EcountManager.Models;

namespace EcountManager.Services;

public interface IEcountApiClient
{
    Task<EcountSession> ConnectAsync(EcountConnectionSettings settings, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EcountProduct>> GetProductsAsync(EcountSession session, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<string, decimal>> GetInventoryAsync(EcountSession session, DateOnly baseDate, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EcountWarehouseStock>> GetWarehouseInventoryAsync(EcountSession session, DateOnly baseDate, CancellationToken cancellationToken = default);
}

