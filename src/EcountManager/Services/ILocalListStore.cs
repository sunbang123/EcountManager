namespace EcountManager.Services;

public interface ILocalListStore<T>
{
    Task<IReadOnlyList<T>> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(IReadOnlyCollection<T> items, CancellationToken cancellationToken = default);
}
