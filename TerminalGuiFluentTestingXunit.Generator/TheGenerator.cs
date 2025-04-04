using System.Collections.Immutable;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TerminalGuiFluentTestingXunit.Generator;

[Generator]
public class TheGenerator : IIncrementalGenerator
{
    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.SyntaxProvider.CreateSyntaxProvider(

                                                     predicate: static (node, _) => IsClass(node,"XunitContextExtensions"),
                                                     transform: static (ctx, _) =>
                                                                   (ClassDeclarationSyntax)ctx.Node)
               .Where(m => m is { });

        var compilation = context.CompilationProvider.Combine(provider.Collect());
        context.RegisterSourceOutput(compilation, Execute);
    }

    private static bool IsClass (SyntaxNode node, string named)
    {
        return node is ClassDeclarationSyntax c && c.Identifier.Text == named;
    }

    private void Execute(SourceProductionContext context, (Compilation Left, ImmutableArray<ClassDeclarationSyntax> Right) arg2)
    {
        var sb = new StringBuilder ();
        var assertType = arg2.Left.GetTypeByMetadataName ("Xunit.Assert");

            var equalMethods = assertType
                               .GetMembers ("Equal")
                               .OfType<IMethodSymbol> ()
                               .Where (m => m.Parameters.Length == 2)
                               .ToList ();

            /*foreach (var method in equalMethods)
            {
                var signature = string.Join (", ", method.Parameters.Select (p => p.Type.ToDisplayString ()));
                context.ReportDiagnostic (Diagnostic.Create (
                                                             new DiagnosticDescriptor ("GEN002", "Equal Overload", $"Equal({signature})", "Generator", DiagnosticSeverity.Info, true),
                                                             Location.None));
            }*/
        


        string header = """"
                        using TerminalGuiFluentTesting;
                        using Xunit;

                        namespace TerminalGuiFluentTestingXunit;

                        public static partial class XunitContextExtensions
                        {
                        
                            public static GuiTestContext AssertTrue (this GuiTestContext context, bool? condition)
                            {
                                context.Then (
                                              () =>
                                              {
                                                  Assert.True (condition);
                                              });
                                return context;
                            }
                        """";

        string tail = """

                      }
                      """;

        sb.AppendLine (header);

        foreach (var m in equalMethods)
        //for (int i = 0; i < 1; i++)
        {
            string method = """
                            public static GuiTestContext AssertEqual (this GuiTestContext context, object? expected, object? actual)
                            {
                                context.Then (
                                              () =>
                                              {
                                                  Assert.Equal (expected,actual);
                                              });
                                return context;
                            }
                            """;

            sb.AppendLine (method);

            break;
        }

        sb.AppendLine (tail);

        context.AddSource("XunitContextExtensions.g.cs", sb.ToString());
    }

}
