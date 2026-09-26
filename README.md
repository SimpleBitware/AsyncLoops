# SimpleLoops
Library for running async loops

## How to use it
`AsyncLoopsBackgroundService` detects all `IAsyncLoop` service registrations (such as `services.AddSingleton<IAsyncLoop, AsyncLoop()`) and run them. <br/>
At minimum, the following services needs to be registered:<br/>
```
services.AddHostedService<AsyncLoopsBackgroundService>();     /* background service which runs the loops */
services.AddSingleton<IAsyncLoop, AsyncLoop>();               /* custom async loop, expending `AsyncLoopBase` or implementing `IAsyncLoop` */
services.AddSingleton<AsyncLoopConfiguration>();              /* only if applied to all async loops */

/* dependencies */
services.AddSingleton<ITask, TaskDelayWrapper>();
services.AddSingleton<IDateTime, DateTimeWrapper>();
```

[Code sample](https://github.com/SimpleBitware/AsyncLoops/blob/main/tests/SimpleBitware.AsyncLoops.Tests.Unit/TestAsyncLoop.cs)
