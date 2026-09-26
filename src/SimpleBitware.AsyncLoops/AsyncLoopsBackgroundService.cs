using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleBitware.AsyncLoops;

/// <summary>
/// Background service that runs simple loops.
/// </summary>
public class AsyncLoopsBackgroundService : BackgroundService
{
    private readonly IHostApplicationLifetime hostApplicationLifetime;
    private readonly IEnumerable<IAsyncLoop> asyncLoops;
    private readonly ILogger<AsyncLoopsBackgroundService> logger;

    public AsyncLoopsBackgroundService(
        IHostApplicationLifetime hostApplicationLifetime,
        IEnumerable<IAsyncLoop> asyncLoops,
        ILogger<AsyncLoopsBackgroundService> logger)
    {
        this.hostApplicationLifetime = hostApplicationLifetime ?? throw new ArgumentNullException(nameof(hostApplicationLifetime));
        this.asyncLoops = asyncLoops ?? throw new ArgumentNullException(nameof(asyncLoops));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting the service.");
        return base.StartAsync(cancellationToken);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Stopping the service.");
        return base.StopAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        if (!asyncLoops.Any())
        {
            logger.LogWarning("No loops found.");
            return;
        }

        LogLoopTypesToBeExecuted(asyncLoops);
        await Task.Yield();

        try
        {
            var simpleLoopsTasks = asyncLoops
                .Select(loop => loop.RunAsync(cancellationToken))
                .ToArray();
            await WaitForLoopsAsync(simpleLoopsTasks, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred.");
        }
        finally
        {
            await AfterExecuteAsync(cancellationToken);
        }
    }

    protected virtual async Task WaitForLoopsAsync(Task[] tasks, CancellationToken cancellationToken)
    {
        await Task.WhenAny(tasks).ConfigureAwait(false);
    }

    protected virtual async Task AfterExecuteAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
        hostApplicationLifetime.StopApplication();
    }

    private void LogLoopTypesToBeExecuted(IEnumerable<IAsyncLoop> loops)
    {
        var loopTypes = string.Join(", ", loops.Select(x => x.GetType()));
        logger.LogInformation("Loops to be executed: {LoopTypes}", loopTypes);
    }
}
