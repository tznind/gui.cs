using System.Collections.Concurrent;
using Moq;

namespace UnitTests.ConsoleDrivers.V2;
public class ApplicationV2Tests
{

    private ApplicationV2 NewApplicationV2 ()
    {
        var netInput = new Mock<INetInput> ();
        SetupRunInputMockMethodToBlock (netInput);
        var winInput = new Mock<IWindowsInput> ();
        SetupRunInputMockMethodToBlock (winInput);

        return new (
                    ()=>netInput.Object,
                    Mock.Of<IConsoleOutput>,
                    () => winInput.Object,
                    Mock.Of<IConsoleOutput>);
    }

    [Fact]
    public void TestInit_CreatesKeybindings ()
    {
        var v2 = NewApplicationV2();

        Application.KeyBindings.Clear();

        Assert.Empty(Application.KeyBindings.GetBindings ());

        v2.Init ();

        Assert.NotEmpty (Application.KeyBindings.GetBindings ());

        v2.Shutdown ();
    }

    [Fact]
    public void TestInit_DriverIsFacade ()
    {
        var v2 = NewApplicationV2();

        Assert.Null (Application.Driver);
        v2.Init ();
        Assert.NotNull (Application.Driver);

        var type = Application.Driver.GetType ();
        Assert.True(type.IsGenericType); 
        Assert.True (type.GetGenericTypeDefinition () == typeof (ConsoleDriverFacade<>));
        v2.Shutdown ();

        Assert.Null (Application.Driver);
    }

    [Fact]
    public void TestInit_ExplicitlyRequestWin ()
    {
        var netInput = new Mock<INetInput> (MockBehavior.Strict);
        var netOutput = new Mock<IConsoleOutput> (MockBehavior.Strict);
        var winInput = new Mock<IWindowsInput> (MockBehavior.Strict);
        var winOutput = new Mock<IConsoleOutput> (MockBehavior.Strict);

        winInput.Setup (i => i.Initialize (It.IsAny<ConcurrentQueue<WindowsConsole.InputRecord>> ()))
                .Verifiable(Times.Once);
        SetupRunInputMockMethodToBlock (winInput);
        winInput.Setup (i=>i.Dispose ())
                .Verifiable(Times.Once);

        var v2 = new ApplicationV2 (
                                    ()=> netInput.Object,
                                    () => netOutput.Object,
                                    () => winInput.Object,
                                    () => winOutput.Object);

        Assert.Null (Application.Driver);
        v2.Init (null,"v2win");
        Assert.NotNull (Application.Driver);

        var type = Application.Driver.GetType ();
        Assert.True (type.IsGenericType);
        Assert.True (type.GetGenericTypeDefinition () == typeof (ConsoleDriverFacade<>));
        v2.Shutdown ();

        Assert.Null (Application.Driver);

        winInput.VerifyAll();
    }

    private void SetupRunInputMockMethodToBlock (Mock<IWindowsInput> winInput)
    {
        winInput.Setup (r => r.Run (It.IsAny<CancellationToken> ()))
                .Callback<CancellationToken> (token =>
                                              {
                                                  // Simulate an infinite loop that checks for cancellation
                                                  while (!token.IsCancellationRequested)
                                                  {
                                                      // Perform the action that should repeat in the loop
                                                      // This could be some mock behavior or just an empty loop depending on the context
                                                  }
                                              })
                .Verifiable (Times.Once);
    }
    private void SetupRunInputMockMethodToBlock (Mock<INetInput> netInput)
    {
        netInput.Setup (r => r.Run (It.IsAny<CancellationToken> ()))
                .Callback<CancellationToken> (token =>
                                              {
                                                  // Simulate an infinite loop that checks for cancellation
                                                  while (!token.IsCancellationRequested)
                                                  {
                                                      // Perform the action that should repeat in the loop
                                                      // This could be some mock behavior or just an empty loop depending on the context
                                                  }
                                              })
                .Verifiable (Times.Once);
    }

    [Fact]
    public void Test_NoInitThrowOnRun ()
    {
        var app = NewApplicationV2();

        var ex = Assert.Throws<NotInitializedException> (() => app.Run (new Window ()));
        Assert.Equal ("Run cannot be accessed before Initialization", ex.Message);
    }

    [Fact]
    public void Test_InitRunShutdown ()
    {
        var orig = ApplicationImpl.Instance;

        var v2 = NewApplicationV2();
        ApplicationImpl.ChangeInstance (v2);

        v2.Init ();

        v2.AddTimeout (TimeSpan.FromMilliseconds (150),
                       () =>
                       {
                           if (Application.Top != null)
                           {
                               Application.RequestStop ();
                               return true;
                           }

                           return true;
                       }
                       );
        Assert.Null (Application.Top);

        // Blocks until the timeout call is hit

        v2.Run (new Window ());

        Assert.Null (Application.Top);
        v2.Shutdown ();

        ApplicationImpl.ChangeInstance (orig);
    }
}
