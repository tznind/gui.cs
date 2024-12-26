using Moq;

namespace UnitTests.ConsoleDrivers.V2;
public class ApplicationV2Tests
{

    private ApplicationV2 NewApplicationV2 ()
    {
        return new (
                    Mock.Of<INetInput>,
                    Mock.Of<IConsoleOutput>,
                    Mock.Of<IWindowsInput>,
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
    public void Test_NoInitThrowOnRun ()
    {
        var app = NewApplicationV2();

        var ex = Assert.Throws<Exception> (() => app.Run (new Window ()));
        Assert.Equal ("App not Initialized",ex.Message);
    }
    /* TODO : Infinite loops
    [Fact]
    public void Test_InitRunShutdown ()
    {
        var v2 = NewApplicationV2();

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
        v2.Run (new Window ());
        Assert.Null (Application.Top);
        v2.Shutdown ();
    }*/
}
