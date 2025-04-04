using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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

        foreach (IMethodSymbol? m in equalMethods)
        {
            var signature = GetModifiedMethodSignature (m,out var expected, out var actual);

            string method = $$"""
                            {{signature}}
                            {
                                context.Then (
                                              () =>
                                              {
                                                  Assert.Equal ({{expected}},{{actual}});
                                              });
                                return context;
                            }
                            """;

            sb.AppendLine (method);
        }

        sb.AppendLine (tail);

        context.AddSource("XunitContextExtensions.g.cs", sb.ToString());
    }

    
    private string GetModifiedMethodSignature (IMethodSymbol methodSymbol, out string expectedParamName, out string actualParamName)
    {
        // Create the "this GuiTestContext context" parameter
        var contextParam = SyntaxFactory.Parameter (SyntaxFactory.Identifier ("context"))
                                         .WithType (SyntaxFactory.ParseTypeName ("GuiTestContext"))
                                         .AddModifiers (SyntaxFactory.Token (SyntaxKind.ThisKeyword)); // Add the "this" keyword


        // Extract the parameter names (expected and actual)
        expectedParamName = methodSymbol.Parameters.FirstOrDefault ()?.Name ?? "expected";
        actualParamName = methodSymbol.Parameters.Skip (1).FirstOrDefault ()?.Name ?? "actual";


        // Get the current method parameters and add the context parameter at the start
        var parameters = methodSymbol.Parameters.Select (p =>
                                                             SyntaxFactory.Parameter (SyntaxFactory.Identifier (p.Name))
                                                                          .WithType (SyntaxFactory.ParseTypeName (p.Type.ToDisplayString ()))
                                                        ).ToList ();

        parameters.Insert (0, contextParam); // Insert 'context' as the first parameter

        // Change the return type to GuiTestContext
        TypeSyntax returnType = SyntaxFactory.ParseTypeName ("GuiTestContext");

        // Change the method name to AssertEqual
        SyntaxToken methodName = SyntaxFactory.Identifier ("AssertEqual");

        // Handle generic type parameters if the method is generic
        var typeParameters = methodSymbol.TypeParameters.Select (
                                                                 tp =>
                                                                     SyntaxFactory.TypeParameter (SyntaxFactory.Identifier (tp.Name))
                                                                )
                                         .ToArray ();

        MethodDeclarationSyntax dec = SyntaxFactory.MethodDeclaration (returnType, methodName)
                                                   .WithModifiers (
                                                                   SyntaxFactory.TokenList (
                                                                                            SyntaxFactory.Token (SyntaxKind.PublicKeyword),
                                                                                            SyntaxFactory.Token (SyntaxKind.StaticKeyword)))
                                                   .WithParameterList (SyntaxFactory.ParameterList (SyntaxFactory.SeparatedList (parameters)));

        if (typeParameters.Any ())
        {
            // Add the <T> here
            dec = dec.WithTypeParameterList (SyntaxFactory.TypeParameterList (SyntaxFactory.SeparatedList (typeParameters)));
        }

        // Build the method signature syntax tree
        MethodDeclarationSyntax methodSyntax = dec.NormalizeWhitespace ();

        // Convert the method syntax to a string
        string methodString = methodSyntax.ToString ();

        return methodString;
    }

}
