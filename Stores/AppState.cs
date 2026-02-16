using System;
using System.Collections.Generic;

namespace ECoopSystem.Stores;

public sealed class AppState
{
    public string InstallationId { get; set; } = "";
    public long InsallationUnixTime { get; set; }
    public DateTimeOffset? LastVerifiedUtc { get; set; }

    public List<DateTimeOffset> FailedActivationsUtc { get; set; } = new();
    public DateTimeOffset? LockedUntilUtc { get; set; }
}
