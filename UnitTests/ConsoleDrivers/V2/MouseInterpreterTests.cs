using Moq;

namespace UnitTests.ConsoleDrivers.V2;
public class MouseInterpreterTests
{
    [Theory]
    [MemberData (nameof (SequenceTests))]
    public void TestMouseEventSequences_InterpretedOnlyAsFlag (List<MouseEventArgs> events, MouseFlags expected)
    {
        // Arrange: Mock dependencies and set up the interpreter
        var viewFinder = Mock.Of<IViewFinder> ();
        var interpreter = new MouseInterpreter (null, viewFinder);

        // Act and Assert: Process all but the last event and ensure they yield no results
        for (int i = 0; i < events.Count - 1; i++)
        {
            var intermediateResult = interpreter.Process (events [i]);
            Assert.Empty (intermediateResult);
        }

        // Process the final event and verify the expected result
        var finalResult = interpreter.Process (events [^1]).ToArray (); // ^1 is the last item in the list
        var singleResult = Assert.Single (finalResult); // Ensure only one result is produced
        Assert.Equal (expected, singleResult.Flags);
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
            // Means click
            MouseFlags.Button1Clicked
        };
    }

}
