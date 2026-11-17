using EcountManager.Models;

namespace EcountManager.Services;

public interface IConsultationMemoStore
{
    Task<IReadOnlyList<ConsultationMemo>> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(IReadOnlyCollection<ConsultationMemo> memos, CancellationToken cancellationToken = default);
}

