using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using TestHelper;
using TaskDelayUseCTOnWaitAny;
using System.Threading.Tasks;

namespace TaskDelayUseCTOnWaitAny.Test
{
    [TestClass]
    public class UnitTest : CodeFixVerifier
    {
        [TestMethod]
        public void EmptyText_NoDiagnostic()
        {
            var test = @"";

            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void NoDiagnostic_WaitAnyWithCT()
        {
            var test = @"
    using System;
    using System.Threading.Tasks;
    using System.Threading;
    namespace ConsoleApplication1
    {
        class TypeName
        {   
            public async Task Test(CancellationToken ct) => await Task.WhenAny(Task.Delay(1, ct));
        }
    }";

            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void NoDiagnostic_NotInWhenAny()
        {
            var test = @"
    using System;
    using System.Threading.Tasks;
    using System.Threading;
    namespace ConsoleApplication1
    {
        class TypeName
        {   
            public async Task Test() => await Task.Delay(1);
        }
    }";

            VerifyCSharpDiagnostic(test);
        }

        [TestMethod]
        public void SingleDiagnostic_WaitAnyWIthoutCtInDelay()
        {
            var test = @"
    using System;
    using System.Threading.Tasks;
    namespace ConsoleApplication1
    {
        class TypeName
        {   
            public async Task Test() => await Task.WhenAny(Task.Delay(1));
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = "TaskDelayUseCTOnWaitAny",
                Message = "Task.Delay invocation will cause guaranteed delay",
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 8, 60)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new TaskDelayUseCTOnWaitAnyCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new TaskDelayUseCTOnWaitAnyAnalyzer();
        }
    }
}
