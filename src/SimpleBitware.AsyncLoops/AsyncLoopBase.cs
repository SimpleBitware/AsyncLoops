using System.Threading.Tasks;
using System.Threading;
using System;
using Microsoft.Extensions.Logging;
using System.Linq;
using SimpleBitware.Common.Abstractions;

namespace SimpleBitware.AsyncLoops;

/// <summary>
/// Async loop base class.
/// </summary>
public abstract class AsyncLoopBase(
    AsyncLoopConfiguration configuration,
    ITask taskWrapper,
    IDateTime dateTimeWrapper,
    ILogger<AsyncLoopBase> logger)
    : IAsyncLoop
{
    private readonly ITask taskWrapper = taskWrapper ?? throw new ArgumentNullException(nameof(taskWrapper));
    private readonly IDateTime dateTimeWrapper = dateTimeWrapper ?? throw new ArgumentNullException(nameof(dateTimeWrapper));
    protected readonly AsyncLoopConfiguration Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    protected readonly ILogger<AsyncLoopBase> Logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("Loop started");

        while (!cancellationToken.IsCancellationRequested)
        {
            var iterationContinuation = await ExecuteIterationInternalAsync(cancellationToken);
            switch (iterationContinuation)
            {
                case IterationResult.Continue:
                    Logger.LogInformation("Iteration completed.");
                    break;
                case IterationResult.Stop:
                    Logger.LogInformation("Iteration completed and loop stopped.");
                    return;
                case IterationResult.Wait:
                    Logger.LogInformation("Iteration completed. Next run at {nextRun}", dateTimeWrapper.UtcNow.AddMilliseconds(Configuration.WaitingTimeInMs));
                    await taskWrapper.Delay(Configuration.WaitingTimeInMs, cancellationToken);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    private async Task<IterationResult> ExecuteIterationInternalAsync(CancellationToken cancellationToken)
    {
        try
        {
            Logger.LogInformation("Iteration started");
            return await ExecuteIterationAsync(cancellationToken);
        }
        catch (OperationCanceledException ex)
        {
            Logger.LogWarning(ex, "Loop cancelled.");
            return IterationResult.Stop;
        }
        catch (AggregateException ae)
        {
            ae.Flatten().InnerExceptions
                .ToList()
                .ForEach(HandleException);
        }
        catch (Exception ex)
        {
            HandleException(ex);
        }
        
        return IterationResult.Wait;
    }

    protected abstract Task<IterationResult> ExecuteIterationAsync(CancellationToken cancellationToken);

    protected virtual void HandleException(Exception ex)
    {
        Logger.LogError(ex, "Unexpected exception.");
        if ((ex is StackOverflowException or OutOfMemoryException) || Configuration.PropagateExceptions)
            throw ex;
    }
}
