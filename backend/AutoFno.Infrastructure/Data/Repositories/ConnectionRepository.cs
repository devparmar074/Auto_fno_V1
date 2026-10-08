using System.Data;
using AutoFno.Application.Interfaces;
using AutoFno.Domain.Entities;
using Dapper;

namespace AutoFno.Infrastructure.Data.Repositories;

public class ConnectionRepository : IConnectionRepository
{
    private readonly SqlConnectionFactory _connectionFactory;

    public ConnectionRepository(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<UpstoxConnection?> GetActiveConnectionAsync(CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        return await db.QuerySingleOrDefaultAsync<UpstoxConnection>(
            "dbo.sp_GetActiveUpstoxConnection",
            commandType: CommandType.StoredProcedure);
    }

    public async Task<int> SaveConnectionAsync(UpstoxConnection connection, CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        var id = await db.ExecuteScalarAsync<int>(
            "dbo.sp_SaveUpstoxConnection",
            new
            {
                connection.EncryptedAccessToken,
                connection.EncryptedRefreshToken,
                connection.ExpiresAtUtc,
                connection.UserId,
                connection.UserName,
                connection.Email,
                connection.PrimaryIp,
                connection.Broker
            },
            commandType: CommandType.StoredProcedure);

        connection.Id = id;
        return id;
    }

    public async Task DeactivateConnectionAsync(CancellationToken ct = default)
    {
        using var db = _connectionFactory.CreateConnection();
        await db.ExecuteAsync("dbo.sp_DeactivateUpstoxConnection", commandType: CommandType.StoredProcedure);
    }
}
