using Moq;

namespace UnitTests.ConsoleDrivers.V2;
public class MouseInterpreterTests
{
    [Fact]
    public void Mouse1Click ()
    {
        var v = Mock.Of<IViewFinder> ();
        var interpreter = new MouseInterpreter (null, v);

        Assert.Empty (interpreter.Process (
                             new MouseEventArgs ()
                             {
                                 Flags = MouseFlags.Button1Pressed
                             }));

        var result = interpreter.Process (
                             new MouseEventArgs ()
                             {
                                 Flags = MouseFlags.Button1Released
                             }).ToArray ();
        var e = Assert.Single (result);

        // TODO: Ultimately will not be the case as we will be dealing with double click and triple
        Assert.Equal (MouseFlags.Button1Clicked,e.Flags);
    }
}
