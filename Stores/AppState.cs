using System;

namespace ECoopSystem.Stores;

public sealed class AppState
{
    public string InstallationId { get; set; } = "";
    public long InsallationUnixTime { get; set; }
    public bool WelcomeShown { get; set; }
    public DateTimeOffset? LastVerifiedUtc { get; set; }
}
