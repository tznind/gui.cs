namespace Terminal.Gui.Analyzers.Tests;

public class HandledEventArgsAnalyzerTests
{
    [Fact]
    public async Task Should_ReportDiagnostic_When_EHandledNotSet ()
    {
        var originalCode = @"
using Terminal.Gui.Views;

class TestClass
{
    void Setup()
    {
        var b = new Button();
        b.Accepting += (s, e) =>
        {
            // Forgot e.Handled = true;
        };
    }
}";
        await new ProjectBuilder ()
              .WithSourceCode (originalCode)
              .WithAnalyzer (new Terminal.Gui.Analyzers.HandledEventArgsAnalyzer ())
              .ValidateAsync ();
    }
}
