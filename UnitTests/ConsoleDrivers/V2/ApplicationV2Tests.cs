using Terminal.Gui.ConsoleDrivers.V2;

namespace UnitTests.ConsoleDrivers.V2;
public class ApplicationV2Tests
{
    [Fact]
    public void TestInit_CreatesKeybindings ()
    {
        var v2 = new ApplicationV2 ();

        Application.KeyBindings.Clear();

        Assert.Empty(Application.KeyBindings.GetBindings ());

        v2.Init ();

        Assert.NotEmpty (Application.KeyBindings.GetBindings ());
    }
}
