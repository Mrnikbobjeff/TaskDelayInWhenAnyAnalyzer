using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace TaskDelayUseCTOnWaitAny
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class TaskDelayUseCTOnWaitAnyAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "TaskDelayUseCTOnWaitAny";
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Performance";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeSymbol, SyntaxKind.InvocationExpression);
        }

        private static void AnalyzeSymbol(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;
            if (!(context.SemanticModel.GetSymbolInfo(invocation.Expression).Symbol is IMethodSymbol methodSymbol))
                return;
            if (methodSymbol.Name.Equals("Delay")
                && methodSymbol.ContainingType.Name.Equals("Task")
                && !methodSymbol.Parameters.Any(parameter => parameter.Type.Name.Equals("CancellationToken")))
            { //Task.Delay Invocation without cancellation
                var possibleParent = invocation.Parent.FirstAncestorOrSelf<InvocationExpressionSyntax>();
                if (possibleParent is null)
                    return;
                if (!(context.SemanticModel.GetSymbolInfo(possibleParent.Expression).Symbol is IMethodSymbol possibleTaskWaitAny))
                    return;
                if (possibleTaskWaitAny.Name.Equals("WhenAny")
                    && possibleTaskWaitAny.ContainingType.Name.Equals("Task"))
                {  // WaitAny parent
                    var diagnostic = Diagnostic.Create(Rule, invocation.GetLocation(), invocation);

                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}
