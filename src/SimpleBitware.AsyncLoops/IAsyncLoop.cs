using System.Threading.Tasks;
using System.Threading;

namespace SimpleBitware.AsyncLoops;

/// <summary>
/// Async loop interface.
/// </summary>
public interface IAsyncLoop
{
    Task RunAsync(CancellationToken cancellationToken);
}
