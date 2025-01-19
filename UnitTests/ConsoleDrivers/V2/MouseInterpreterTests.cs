using Moq;

namespace UnitTests.ConsoleDrivers.V2;
public class MouseInterpreterTests
{
    [Theory]
    [MemberData (nameof (SequenceTests))]
    public void TestMouseEventSequences_InterpretedOnlyAsFlag (List<MouseEventArgs> events, params MouseFlags?[] expected)
    {
        // Arrange: Mock dependencies and set up the interpreter
        var interpreter = new MouseInterpreter (null);

        // Act and Assert
        for (int i = 0; i < events.Count; i++)
        {
            var results = interpreter.Process (events [i]).ToArray();

            // Raw input event should be there
            Assert.Equal (events [i].Flags, results [0].Flags);

            // also any expected should be there
            if (expected [i] != null)
            {
                Assert.Equal (expected [i], results [1].Flags);
            }
            else
            {
                Assert.Single (results);
            }
        }
    }


    public static IEnumerable<object []> SequenceTests ()
    {
        yield return new object []
        {
            new List<MouseEventArgs> ()
            {
                // Mouse was down
                new ()
                {
                    Flags = MouseFlags.Button1Pressed
                },

                // Then it wasn't
                new()
            },
            // No extra then click
            null,
            MouseFlags.Button1Clicked
        };

        yield return new object []
        {
            new List<MouseEventArgs> ()
            {
                // Mouse was down
                new ()
                {
                    Flags = MouseFlags.Button1Pressed
                },

                // Then it wasn't
                new(),

                // Then it was again
                new ()
                {
                    Flags = MouseFlags.Button1Pressed
                },

                // Then it wasn't
                new()
            },
            // No extra then click, then into none/double click
            null,
            MouseFlags.Button1Clicked,
            null,
            MouseFlags.Button1DoubleClicked
        };
    }

}
