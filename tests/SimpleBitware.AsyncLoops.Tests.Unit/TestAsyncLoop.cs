using Microsoft.Extensions.Logging;
using SimpleBitware.Common.Abstractions;

namespace SimpleBitware.AsyncLoops.Tests.Unit;

public class TestAsyncLoop(
    AsyncLoopConfiguration configuration, 
    ITask taskWrapper, 
    IDateTime dateTimeWrapper, 
    ILogger<AsyncLoopBase> logger,
    Func<CancellationToken, Task<IterationResult>> func) 
    : AsyncLoopBase(configuration, taskWrapper, dateTimeWrapper, logger)
{
    protected override Task<IterationResult> ExecuteIterationAsync(CancellationToken cancellationToken)
    {
        return func(cancellationToken);
    }
}
