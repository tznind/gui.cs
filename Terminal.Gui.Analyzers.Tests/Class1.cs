using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.CSharp.Testing;

namespace Terminal.Gui.Analyzers.Tests;

using Verify = CSharpAnalyzerVerifier<HandledEventArgsAnalyzer, DefaultVerifier>;

public class HandledEventArgsAnalyzerTests
{
    [Fact]
    public async Task ReportsWarning_WhenHandledNotSet ()
    {
        var testCode = @"
using Terminal.Gui;
class TestClass {
    void TestMethod() {
        var b = new Button();
        b.Accepting += (s, e) => {
            Logging.Information(""hello"");
        };
    }
}";

        var expected = Verify.Diagnostic ("TGUIG001")
                             .WithSeverity (DiagnosticSeverity.Warning)
                             .WithSpan (6, 9, 9, 11); // adjust line/column as needed

        await Verify.VerifyAnalyzerAsync (testCode, expected);
    }
}