namespace VerseStrings.Services;

/// <summary>
/// Result of a single <see cref="UpdateOrchestrator.SyncNowAsync"/> attempt,
/// reported back to the standalone-mode UI so it can toast meaningfully.
/// The tray mode's continuous loop doesn't observe these — it relies on
/// the orchestrator's internal toasts plus the StatusChanged event.
/// </summary>
public enum SyncOutcome
{
    /// <summary>No work needed: SHA matches, or no LIVE folder configured,
    /// or the upstream release returned nothing. The orchestrator stays
    /// silent in these cases; the caller decides whether to toast "already
    /// up to date" or stay quiet.</summary>
    NoChange,

    /// <summary>A new release was downloaded, verified, and applied. The
    /// orchestrator's existing "VerseStrings updated" toast already fired
    /// from inside the install flow.</summary>
    Installed,

    /// <summary>Star Citizen was running and the caller asked us not to
    /// wait. The user needs to close the game and re-trigger the sync.</summary>
    GameRunning,

    /// <summary>The install attempt threw. The orchestrator's existing
    /// "VerseStrings update failed" toast already fired with the exception
    /// message.</summary>
    Failed,
}
