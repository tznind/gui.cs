using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Terminal.Gui.Analyzers;

[DiagnosticAnalyzer (LanguageNames.CSharp)]
public class HandledEventArgsAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "TGUIG001";
    private static readonly LocalizableString Title = "Event handler should set e.Handled = true";
    private static readonly LocalizableString MessageFormat = "Event handler does not set e.Handled = true";
    private static readonly LocalizableString Description = "Handlers for CommandEventArgs should mark the event as handled by setting e.Handled = true.";
    private const string Category = "Usage";

    private static DiagnosticDescriptor _rule = new DiagnosticDescriptor (
        DiagnosticId,
        Title,
        MessageFormat,
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: Description);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create (_rule);

    public override void Initialize (AnalysisContext context)
    {
        context.EnableConcurrentExecution ();

        // Only analyze non-generated code
        context.ConfigureGeneratedCodeAnalysis (GeneratedCodeAnalysisFlags.None);

        // Register to analyze syntax nodes of lambda expressions and anonymous methods
        context.RegisterSyntaxNodeAction (AnalyzeLambdaOrAnonymousMethod, SyntaxKind.ParenthesizedLambdaExpression, SyntaxKind.SimpleLambdaExpression, SyntaxKind.AnonymousMethodExpression);
    }

    private static void AnalyzeLambdaOrAnonymousMethod (SyntaxNodeAnalysisContext context)
    {
        var lambda = (AnonymousFunctionExpressionSyntax)context.Node;

        // Check if this lambda is assigned to an event called "Accepting"
        // We'll look for the parent assignment or event subscription

        var parent = lambda.Parent;

        // Common case: assignment or += event subscription like b.Accepting += (s,e) => { ... };
        if (parent is AssignmentExpressionSyntax assignment)
        {
            if (!IsAcceptingEvent (assignment.Left, context))
            {
                return;
            }
        }
        else if (parent is ArgumentSyntax argument)
        {
            // Could be passed as argument, skip for simplicity
            return;
        }
        else if (parent is EqualsValueClauseSyntax equalsValue)
        {
            // Assigned in variable declaration
            return;
        }
        else if (parent is AnonymousFunctionExpressionSyntax)
        {
            // nested lambda, ignore for now
            return;
        }
        else if (parent is ArgumentListSyntax)
        {
            // passed as argument, skip
            return;
        }
        else if (parent is ExpressionSyntax expr)
        {
            // Try to get grandparent assignment, eg b.Accepting += ...
            if (parent.Parent is AssignmentExpressionSyntax assign2 && IsAcceptingEvent (assign2.Left, context))
            {
                // ok continue
            }
            else
            {
                return;
            }
        }
        else
        {
            return;
        }

        // Look for the parameter named "e"
        var parameters = GetParameters(lambda);
        IParameterSymbol eParamSymbol = null;

        if (parameters == null || parameters.Count == 0)
        {
            return;
        }

        // Usually second param is "e"
        foreach (var param in parameters)
        {
            if (param.Identifier.Text == "e")
            {
                eParamSymbol = context.SemanticModel.GetDeclaredSymbol (param) as IParameterSymbol;
                break;
            }
        }

        if (eParamSymbol == null)
        {
            return;
        }

        // Check the type of "e" parameter: should be CommandEventArgs or derived
        var paramType = eParamSymbol.Type;
        if (paramType == null)
        {
            return;
        }

        if (paramType.Name != "CommandEventArgs")
        {
            return;
        }

        // Now check if the body contains assignment to e.Handled = true
        if (lambda.Body is BlockSyntax block)
        {
            bool setsHandled = false;

            foreach (var statement in block.Statements)
            {
                // Look for assignment expressions
                var assignments = statement.DescendantNodes ().OfType<AssignmentExpressionSyntax> ();

                foreach (var assignmentExpr in assignments)
                {
                    if (IsHandledAssignment (assignmentExpr, eParamSymbol, context))
                    {
                        setsHandled = true;
                        break;
                    }
                }

                if (setsHandled)
                {
                    break;
                }
            }

            if (!setsHandled)
            {
                // Report diagnostic on the lambda expression itself
                var diag = Diagnostic.Create (_rule, lambda.GetLocation ());
                context.ReportDiagnostic (diag);
            }
        }
        else if (lambda.Body is ExpressionSyntax exprBody)
        {
            // Expression-bodied lambda, less common for event handlers, skip for now
        }
    }
    private static SeparatedSyntaxList<ParameterSyntax> GetParameters (AnonymousFunctionExpressionSyntax lambda)
    {
        switch (lambda)
        {
            case ParenthesizedLambdaExpressionSyntax p:
                return p.ParameterList.Parameters;
            case SimpleLambdaExpressionSyntax s:
                // Simple lambda has a single parameter, wrap it in a list
                return SyntaxFactory.SeparatedList (new [] { s.Parameter });
            case AnonymousMethodExpressionSyntax a:
                return a.ParameterList?.Parameters ?? default;
            default:
                return default;
        }
    }

    private static bool IsAcceptingEvent (ExpressionSyntax expr, SyntaxNodeAnalysisContext context)
    {
        // Check if expr is b.Accepting or similar

        // Get symbol info
        var symbolInfo = context.SemanticModel.GetSymbolInfo (expr);
        var symbol = symbolInfo.Symbol;

        if (symbol == null)
        {
            return false;
        }

        // Accepting event symbol should be an event named "Accepting"
        if (symbol.Kind == SymbolKind.Event && symbol.Name == "Accepting")
        {
            return true;
        }

        return false;
    }

    private static bool IsHandledAssignment (AssignmentExpressionSyntax assignment, IParameterSymbol eParamSymbol, SyntaxNodeAnalysisContext context)
    {
        // Check if left side is "e.Handled" and right side is "true"
        // Left side should be MemberAccessExpression: e.Handled

        if (assignment.Left is MemberAccessExpressionSyntax memberAccess)
        {
            // Check that member access expression is "e.Handled"
            var exprSymbol = context.SemanticModel.GetSymbolInfo (memberAccess.Expression).Symbol;
            if (exprSymbol == null)
            {
                return false;
            }

            if (!SymbolEqualityComparer.Default.Equals (exprSymbol, eParamSymbol))
            {
                return false;
            }

            if (memberAccess.Name.Identifier.Text != "Handled")
            {
                return false;
            }

            // Check right side is true literal
            if (assignment.Right is LiteralExpressionSyntax literal &&
                literal.IsKind (SyntaxKind.TrueLiteralExpression))
            {
                return true;
            }
        }

        return false;
    }
}
