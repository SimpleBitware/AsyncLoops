using Microsoft.Extensions.Logging;
using Moq;
using SimpleBitware.Common.Abstractions;

namespace SimpleBitware.AsyncLoops.Tests.Unit;

public class AsyncLoopTests
{
    private Mock<ILogger<TestAsyncLoop>> loggerMock;
    private Mock<AsyncLoopConfiguration> configurationMock;
    private Mock<IDateTime> dateTimeMock;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        loggerMock = new Mock<ILogger<TestAsyncLoop>>();
        configurationMock = new Mock<AsyncLoopConfiguration>();
        dateTimeMock = new Mock<IDateTime>();
    }

    [Test]
    public async Task Should_Stop_Execution_When_CancellationToken_Cancelled()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        await cancellationTokenSource.CancelAsync();

        var funcMock = new Mock<Func<CancellationToken, Task<IterationResult>>>();
        var taskMock = new Mock<ITask>();
        var asyncLoop = new TestAsyncLoop(
            configurationMock.Object,
            taskMock.Object,
            dateTimeMock.Object,
            loggerMock.Object,
            funcMock.Object);

        // Act
        await asyncLoop.RunAsync(cancellationToken);

        // Assert
        funcMock.Verify(x => x(It.IsAny<CancellationToken>()), Times.Never);
        taskMock.Verify(x => x.Delay(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Should_Continue_Execution_Without_Waiting_When_IterationExecutor_Returns_Continue()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        var funcMock = new Mock<Func<CancellationToken, Task<IterationResult>>>();
        funcMock.Setup(x => x(It.IsAny<CancellationToken>()))
            .Callback<CancellationToken>(ct => cancellationTokenSource.Cancel())
            .ReturnsAsync(IterationResult.Continue);

        var taskMock = new Mock<ITask>();
        var asyncLoop = new TestAsyncLoop(
            configurationMock.Object,
            taskMock.Object,
            dateTimeMock.Object,
            loggerMock.Object,
            funcMock.Object);

        // Act
        await asyncLoop.RunAsync(cancellationToken);

        // Assert
        funcMock.Verify(x => x(It.IsAny<CancellationToken>()), Times.Once);
        taskMock.Verify(x => x.Delay(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Should_Wait_Between_Iterations_When_IterationExecutor_Returns_Wait()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        var funcMock = new Mock<Func<CancellationToken, Task<IterationResult>>>();
        funcMock.Setup(x => x(It.IsAny<CancellationToken>()))
            .Callback<CancellationToken>(ct => cancellationTokenSource.Cancel())
            .ReturnsAsync(IterationResult.Wait);

        var taskMock = new Mock<ITask>();
        var asyncLoop = new TestAsyncLoop(
            configurationMock.Object,
            taskMock.Object,
            dateTimeMock.Object,
            loggerMock.Object,
            funcMock.Object);

        // Act
        await asyncLoop.RunAsync(cancellationToken);

        // Assert
        funcMock.Verify(x => x(It.IsAny<CancellationToken>()), Times.Once);
        taskMock.Verify(x => x.Delay(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Should_Exit_Loop_When_IterationExecutor_Returns_Stop()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        var funcMock = new Mock<Func<CancellationToken, Task<IterationResult>>>();
        funcMock.Setup(x => x(It.IsAny<CancellationToken>()))
            .Callback<CancellationToken>(ct => cancellationTokenSource.Cancel())
            .ReturnsAsync(IterationResult.Stop);

        var taskMock = new Mock<ITask>();
        var asyncLoop = new TestAsyncLoop(
            configurationMock.Object,
            taskMock.Object,
            dateTimeMock.Object,
            loggerMock.Object,
            funcMock.Object);

        // Act
        await asyncLoop.RunAsync(cancellationToken);

        // Assert
        funcMock.Verify(x => x(It.IsAny<CancellationToken>()), Times.Once);
        taskMock.Verify(x => x.Delay(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void Should_Throw_Exception_When_PropagateException_True()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        var funcMock = new Mock<Func<CancellationToken, Task<IterationResult>>>();
        funcMock.Setup(x => x(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotSupportedException());

        var configuration = new AsyncLoopConfiguration()
        {
            WaitingTimeInMs = 1,
            PropagateExceptions = true
        };
        
        var taskMock = new Mock<ITask>();
        var asyncLoop = new TestAsyncLoop(
            configuration,
            taskMock.Object,
            dateTimeMock.Object,
            loggerMock.Object,
            funcMock.Object);

        // Act
        Assert.That(async ()=> await asyncLoop.RunAsync(cancellationToken), Throws.TypeOf<NotSupportedException>());
    }

    [Test]
    public async Task Should_Wait_Between_Iterations_When_Iterator_Throws_Exception_And_PropagateException_False()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        var funcMock = new Mock<Func<CancellationToken, Task<IterationResult>>>();
        funcMock.Setup(x => x(It.IsAny<CancellationToken>()))
            .Callback<CancellationToken>(ct => cancellationTokenSource.Cancel())
            .ThrowsAsync(new NotSupportedException());

        var configuration = new AsyncLoopConfiguration()
        {
            WaitingTimeInMs = 1,
            PropagateExceptions = false
        };
        
        var taskMock = new Mock<ITask>();
        var asyncLoop = new TestAsyncLoop(
            configuration,
            taskMock.Object,
            dateTimeMock.Object,
            loggerMock.Object,
            funcMock.Object);

        // Act
        await asyncLoop.RunAsync(cancellationToken);

        // Assert
        funcMock.Verify(x => x(It.IsAny<CancellationToken>()), Times.Once);
        taskMock.Verify(x => x.Delay(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
