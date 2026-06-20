using DbTraffic.Core.Rules;

namespace DbTraffic.Core.Services;

public interface IRiskContextProvider
{
    Task<RuleContext?> BuildContextAsync(Guid processId, DateTime proposedStartTime, CancellationToken cancellationToken = default);
}
