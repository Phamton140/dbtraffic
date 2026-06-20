using DbTraffic.Shared.Models;
using DbTraffic.Shared.Models.Dmv;

namespace DbTraffic.Infrastructure.SqlServer;

public interface ISqlServerInstanceClient
{
    Task<bool> CanConnectAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ActiveRequest>> GetActiveRequestsAsync(CancellationToken cancellationToken = default);
}
