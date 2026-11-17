using EcountManager.Models;

namespace EcountManager.Services;

public interface IEcountQueryService
{
    Task<IReadOnlyList<Customer>> SearchCustomersAsync(string? searchText, CancellationToken cancellationToken = default);
    Task<CustomerDashboard> GetCustomerDashboardAsync(string customerCode, CancellationToken cancellationToken = default);
}
