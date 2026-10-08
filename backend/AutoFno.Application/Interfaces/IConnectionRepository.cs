using AutoFno.Domain.Entities;

namespace AutoFno.Application.Interfaces;

public interface IConnectionRepository
{
    Task<UpstoxConnection?> GetActiveConnectionAsync(CancellationToken ct = default);
    Task<int> SaveConnectionAsync(UpstoxConnection connection, CancellationToken ct = default);
    Task DeactivateConnectionAsync(CancellationToken ct = default);
}
