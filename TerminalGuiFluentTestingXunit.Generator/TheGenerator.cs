using System.Collections.Immutable;
using System.Reflection;
using System.Text;
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
        var assertType = arg2.Left.GetTypeByMetadataName ("Xunit.Assert");

        GenerateMethods (assertType,context, "IsType",true);
        GenerateMethods (assertType, context, "Equal", false);
    }

    private void GenerateMethods (INamedTypeSymbol? assertType, SourceProductionContext context, string methodName, bool invokeTExplicitly)
    {
        var sb = new StringBuilder ();

        // Create a HashSet to track unique method signatures
        var signaturesDone = new HashSet<string> ();

        var methods = assertType
                               .GetMembers (methodName)
                               .OfType<IMethodSymbol> ()
                               .ToList ();

        string header = """"
                        #nullable enable
                        using TerminalGuiFluentTesting;
                        using Xunit;

                        namespace TerminalGuiFluentTestingXunit;

                        public static partial class XunitContextExtensions
                        {
                        

                        """";

        string tail = """

                      }
                      """;

        sb.AppendLine (header);

        foreach (IMethodSymbol? m in methods)
        {
            var signature = GetModifiedMethodSignature (m,methodName,invokeTExplicitly, out var paramNames, out var typeParams);

            if (!signaturesDone.Add (signature))
            {
                continue;
            }

            string method = $$"""
                              {{signature}}
                              {
                                  try
                                  {
                                      Assert.{{methodName}}{{typeParams}} ({{string.Join (",", paramNames)}});
                                  }
                                  catch(Exception)
                                  {
                                      context.HardStop ();
                                      
                                  
                                      throw;
                                  
                                  }
                                  
                                  return context;
                              }
                              """;

            sb.AppendLine (method);
        }
        sb.AppendLine (tail);

        context.AddSource ($"XunitContextExtensions{methodName}.g.cs", sb.ToString ());
    }

    private string GetModifiedMethodSignature (IMethodSymbol methodSymbol, string methodName, bool invokeTExplicitly, out string [] paramNames, out string typeParams)
    {
        typeParams = string.Empty;

        // Create the "this GuiTestContext context" parameter
        var contextParam = SyntaxFactory.Parameter (SyntaxFactory.Identifier ("context"))
                                         .WithType (SyntaxFactory.ParseTypeName ("GuiTestContext"))
                                         .AddModifiers (SyntaxFactory.Token (SyntaxKind.ThisKeyword)); // Add the "this" keyword


        // Extract the parameter names (expected and actual)
        paramNames = new string [methodSymbol.Parameters.Length];

        for (int i = 0; i < methodSymbol.Parameters.Length; i++)
        {
            paramNames [i] = methodSymbol.Parameters.ElementAt (i).Name;

            // Check if the parameter name is a reserved keyword and prepend "@" if it is
            if (IsReservedKeyword (paramNames [i]))
            {
                paramNames [i] = "@" + paramNames [i];
            }
            else
            {
                paramNames [i] = paramNames [i];
            }
        }

        // Get the current method parameters and add the context parameter at the start
        var parameters = methodSymbol.Parameters.Select (p =>
                                                         {
                                                             var paramName = p.Name;
                                                             // Check if the parameter name is a reserved keyword and prepend "@" if it is
                                                             if (IsReservedKeyword (paramName))
                                                             {
                                                                 paramName = "@" + paramName;
                                                             }

                                                             // Create the parameter syntax with the modified name
                                                             return SyntaxFactory.Parameter (SyntaxFactory.Identifier (paramName))
                                                                                 .WithType (SyntaxFactory.ParseTypeName (p.Type.ToDisplayString ()));
                                                         }).ToList ();

        parameters.Insert (0, contextParam); // Insert 'context' as the first parameter

        // Change the return type to GuiTestContext
        TypeSyntax returnType = SyntaxFactory.ParseTypeName ("GuiTestContext");

        // Change the method name to AssertEqual
        SyntaxToken newMethodName = SyntaxFactory.Identifier ($"Assert{methodName}");

        // Handle generic type parameters if the method is generic
        var typeParameters = methodSymbol.TypeParameters.Select (
                                                                 tp =>
                                                                     SyntaxFactory.TypeParameter (SyntaxFactory.Identifier (tp.Name))
                                                                )
                                         .ToArray ();

        MethodDeclarationSyntax dec = SyntaxFactory.MethodDeclaration (returnType, newMethodName)
                                                   .WithModifiers (
                                                                   SyntaxFactory.TokenList (
                                                                                            SyntaxFactory.Token (SyntaxKind.PublicKeyword),
                                                                                            SyntaxFactory.Token (SyntaxKind.StaticKeyword)))
                                                   .WithParameterList (SyntaxFactory.ParameterList (SyntaxFactory.SeparatedList (parameters)));

        if (typeParameters.Any ())
        {
            // Add the <T> here
            dec = dec.WithTypeParameterList (SyntaxFactory.TypeParameterList (SyntaxFactory.SeparatedList (typeParameters)));

            // Handle type parameter constraints
            var constraintClauses = methodSymbol.TypeParameters
                                                .Where (tp => tp.ConstraintTypes.Length > 0)
                                                .Select (tp =>
                                                             SyntaxFactory.TypeParameterConstraintClause (tp.Name)
                                                                          .WithConstraints (
                                                                                            SyntaxFactory.SeparatedList<TypeParameterConstraintSyntax> (
                                                                                                 tp.ConstraintTypes.Select (constraintType =>
                                                                                                         SyntaxFactory.TypeConstraint (SyntaxFactory.ParseTypeName (constraintType.ToDisplayString ()))
                                                                                                     )
                                                                                                )
                                                                                           )
                                                        ).ToList ();

            if (constraintClauses.Any ())
            {
                dec = dec.WithConstraintClauses (SyntaxFactory.List (constraintClauses));

            }

            // Add the <T> here
            if (invokeTExplicitly)
            {
                typeParams = "<" + string.Join (", ", typeParameters.Select (tp => tp.Identifier.ValueText)) + ">";
            }
        }

        // Build the method signature syntax tree
        MethodDeclarationSyntax methodSyntax = dec.NormalizeWhitespace ();

        // Convert the method syntax to a string
        string methodString = methodSyntax.ToString ();

        return methodString;
    }

    // Helper method to check if a parameter name is a reserved keyword
    private bool IsReservedKeyword (string name)
    {
        return string.Equals (name, "object");
    }
}
