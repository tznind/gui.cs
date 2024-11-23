namespace UnitTests.ConsoleDrivers;

public class MouseParserTests
{
    private readonly MouseParser _parser;

    public MouseParserTests () { _parser = new (); }

    // Consolidated test for all mouse events: button press/release, wheel scroll, position, modifiers
    [Theory]
    [InlineData ("\u001b[<0;100;200M", 100, 200, MouseFlags.Button1Pressed)] // Button 1 Pressed
    [InlineData ("\u001b[<0;150;250m", 150, 250, MouseFlags.Button1Released)] // Button 1 Released
    [InlineData ("\u001b[<1;120;220M", 120, 220, MouseFlags.Button2Pressed)] // Button 2 Pressed
    [InlineData ("\u001b[<1;180;280m", 180, 280, MouseFlags.Button2Released)] // Button 2 Released
    [InlineData ("\u001b[<2;200;300M", 200, 300, MouseFlags.Button3Pressed)] // Button 3 Pressed
    [InlineData ("\u001b[<2;250;350m", 250, 350, MouseFlags.Button3Released)] // Button 3 Released
    [InlineData ("\u001b[<64;100;200M", 100, 200, MouseFlags.WheeledUp)] // Wheel Scroll Up
    [InlineData ("\u001b[<65;150;250m", 150, 250, MouseFlags.WheeledDown)] // Wheel Scroll Down
    [InlineData ("\u001b[<39;100;200m", 100, 200, MouseFlags.ButtonShift | MouseFlags.ReportMousePosition)] // Mouse Position (No Button)
    [InlineData ("\u001b[<43;120;240m", 120, 240, MouseFlags.ButtonAlt | MouseFlags.ReportMousePosition)] // Mouse Position (No Button)
    [InlineData ("\u001b[<8;100;200M", 100, 200, MouseFlags.Button1Pressed | MouseFlags.ButtonAlt)] // Button 1 Pressed + Alt
    [InlineData ("\u001b[<invalid;100;200M", 0, 0, MouseFlags.None)] // Invalid Input (Expecting null)
    [InlineData ("\u001b[<100;200;300Z", 0, 0, MouseFlags.None)] // Invalid Input (Expecting null)
    [InlineData ("\u001b[<invalidInput>", 0, 0, MouseFlags.None)] // Invalid Input (Expecting null)
    public void ProcessMouseInput_ReturnsCorrectFlags (string input, int expectedX, int expectedY, MouseFlags expectedFlags)
    {
        // Act
        MouseEventArgs result = _parser.ProcessMouseInput (input);

        // Assert
        if (expectedFlags == MouseFlags.None)
        {
            Assert.Null (result); // Expect null for invalid inputs
        }
        else
        {
            Assert.NotNull (result); // Expect non-null result for valid inputs
            Assert.Equal (new (expectedX, expectedY), result!.Position); // Verify position
            Assert.Equal (expectedFlags, result.Flags); // Verify flags
        }
    }
}
