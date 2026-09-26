using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;

namespace SimpleBitware.AsyncLoops.Tests.Unit;

public class AsyncLoopsBackgroundServiceTests
{
    private Mock<ILogger<AsyncLoopsBackgroundService>> loggerMock;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        loggerMock = new Mock<ILogger<AsyncLoopsBackgroundService>>();
    }

    [Test]
    public async Task Should_Stop_When_No_Loops()
    {
        // Arrange
        var hostApplicationLifetimeMock = new Mock<IHostApplicationLifetime>();
        var simpleLoops = Array.Empty<IAsyncLoop>();

        var sut = new AsyncLoopsBackgroundService(
            hostApplicationLifetimeMock.Object,
            simpleLoops,
            loggerMock.Object);

        // Act
        await sut.StartAsync(CancellationToken.None);

        // Assert
        hostApplicationLifetimeMock.Verify(x => x.StopApplication(), Times.Never);
    }

    [Test]
    public async Task Should_Stop_When_Loop_Cancelled()
    {
        // Arrange
        var hostApplicationLifetimeMock = new Mock<IHostApplicationLifetime>();
        var simpleLoopMock = new Mock<IAsyncLoop>();
        simpleLoopMock.Setup(x => x.RunAsync(It.IsAny<CancellationToken>()))
            .Returns<CancellationToken>(cancellationToken => Task.Run(async () =>
            {
                do
                {
                    await Task.Delay(10);
                } while (!cancellationToken.IsCancellationRequested);
            }, cancellationToken));

        var cancellationTokenSource = new CancellationTokenSource();

        var backgroundService = new AsyncLoopsBackgroundService(
            hostApplicationLifetimeMock.Object,
            [simpleLoopMock.Object],
            loggerMock.Object);

        // Act
        await backgroundService.StartAsync(cancellationTokenSource.Token);
        await Task.Delay(10, cancellationTokenSource.Token);
        await cancellationTokenSource.CancelAsync();
        await backgroundService.ExecuteTask;

        // Assert
        simpleLoopMock.Verify(x => x.RunAsync(It.IsAny<CancellationToken>()), Times.Once);
        hostApplicationLifetimeMock.Verify(x => x.StopApplication(), Times.Once);
    }

    [Test]
    public async Task Should_Stop_When_A_Loop_Failed()
    {
        // Arrange
        var hostApplicationLifetimeMock = new Mock<IHostApplicationLifetime>();
        var simpleLoopMock = new Mock<IAsyncLoop>();
        simpleLoopMock.Setup(x => x.RunAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromException(new Exception()));

        var backgroundService = new AsyncLoopsBackgroundService(
            hostApplicationLifetimeMock.Object,
            [simpleLoopMock.Object],
            loggerMock.Object);

        // Act
        await backgroundService.StartAsync(CancellationToken.None);
        await backgroundService.ExecuteTask;

        // Assert
        simpleLoopMock.Verify(x => x.RunAsync(It.IsAny<CancellationToken>()), Times.Once);
        hostApplicationLifetimeMock.Verify(x => x.StopApplication(), Times.Once);
    }

    [Test]
    public async Task Should_Stop_When_One_Loop_Throw_Exception_And_Stop_The_Others()
    {
        // Arrange
        var hostApplicationLifetimeMock = new Mock<IHostApplicationLifetime>();
        var simpleLoop1Mock = new Mock<IAsyncLoop>();
        simpleLoop1Mock.Setup(x => x.RunAsync(It.IsAny<CancellationToken>()))
            .Returns<CancellationToken>(cancellationToken => Task.Run(async () =>
            {
                do
                {
                    await Task.Delay(10);
                } while (!cancellationToken.IsCancellationRequested);
            }, cancellationToken));

        var simpleLoop2Mock = new Mock<IAsyncLoop>();
        simpleLoop2Mock.Setup(x => x.RunAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromException(new Exception()));

        var backgroundService = new AsyncLoopsBackgroundService(
            hostApplicationLifetimeMock.Object,
            [simpleLoop1Mock.Object, simpleLoop2Mock.Object],
            loggerMock.Object);

        // Act
        await backgroundService.StartAsync(CancellationToken.None);
        await backgroundService.ExecuteTask;

        // Assert
        simpleLoop1Mock.Verify(x => x.RunAsync(It.IsAny<CancellationToken>()), Times.Once);
        simpleLoop2Mock.Verify(x => x.RunAsync(It.IsAny<CancellationToken>()), Times.Once);
        hostApplicationLifetimeMock.Verify(x => x.StopApplication(), Times.Once);
    }

    [Test]
    public async Task Should_Run_Loop_Until_Terminates()
    {
        // Arrange
        var hostApplicationLifetimeMock = new Mock<IHostApplicationLifetime>();
        var simpleLoopMock = new Mock<IAsyncLoop>();

        var backgroundService = new AsyncLoopsBackgroundService(
            hostApplicationLifetimeMock.Object,
            [simpleLoopMock.Object],
            loggerMock.Object);

        // Act
        await backgroundService.StartAsync(CancellationToken.None);
        await backgroundService.ExecuteTask;
        // Assert
        simpleLoopMock.Verify(x => x.RunAsync(It.IsAny<CancellationToken>()), Times.Once);
        hostApplicationLifetimeMock.Verify(x => x.StopApplication(), Times.Once);
    }
}
