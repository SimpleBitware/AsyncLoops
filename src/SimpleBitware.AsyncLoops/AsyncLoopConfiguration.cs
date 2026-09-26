namespace SimpleBitware.AsyncLoops;

/// <summary>
/// Async loop configuration.
/// </summary>
public record AsyncLoopConfiguration
{
    /// <summary>
    /// Wait time between iterations in milliseconds.
    /// </summary>
    public required int WaitingTimeInMs { get; init; }

    /// <summary>
    /// If true, loop exits and exceptions will be propagated to the caller.
    /// Default is false. All exceptions will be logged, and the loop will continue execution.
    /// </summary>
    public required bool PropagateExceptions { get; init; }
}
