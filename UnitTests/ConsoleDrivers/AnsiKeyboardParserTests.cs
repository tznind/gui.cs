using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests.ConsoleDrivers;
public class AnsiKeyboardParserTests
{
    private readonly AnsiKeyboardParser _parser = new ();

    public static IEnumerable<object []> GetKeyboardTestData ()
    {
        // Test data for various ANSI escape sequences and their expected Key values
        yield return new object [] { "\u001b[A", Key.CursorUp };
        yield return new object [] { "\u001b[B", Key.CursorDown };
        yield return new object [] { "\u001b[C", Key.CursorRight };
        yield return new object [] { "\u001b[D", Key.CursorLeft };

        // Invalid inputs
        yield return new object [] { "\u001b[Z", null };
        yield return new object [] { "\u001b[invalid", null };
        yield return new object [] { "\u001b[1", null };
        yield return new object [] { "\u001b[AB", null };
        yield return new object [] { "\u001b[;A", null };
    }

    // Consolidated test for all keyboard events (e.g., arrow keys)
    [Theory]
    [MemberData (nameof (GetKeyboardTestData))]
    public void ProcessKeyboardInput_ReturnsCorrectKey (string input, Key? expectedKey)
    {
        // Act
        Key? result = _parser.ProcessKeyboardInput (input);

        // Assert
        Assert.Equal (expectedKey, result); // Verify the returned key matches the expected one
    }
}
