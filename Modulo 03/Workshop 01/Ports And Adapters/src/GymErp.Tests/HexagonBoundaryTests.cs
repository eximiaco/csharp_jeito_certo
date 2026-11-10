using System.Reflection;
using GymErp.Core;

namespace GymErp.Tests;

// O lado de dentro não conhece o lado de fora.
public class HexagonBoundaryTests
{
    [Fact]
    public void Core_references_no_adapter_framework_or_other_project()
    {
        var references = typeof(EnrollmentManagement).Assembly.GetReferencedAssemblies()
            .Select(r => r.Name!)
            .Where(n => n is not ("mscorlib" or "netstandard") && !n.StartsWith("System"))
            .ToArray();

        Assert.Equal(new[] { "CSharpFunctionalExtensions" }, references);
    }
}
