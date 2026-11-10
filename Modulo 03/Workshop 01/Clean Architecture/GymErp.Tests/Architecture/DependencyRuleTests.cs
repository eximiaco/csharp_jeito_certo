using System.Reflection;
using GymErp.Application.UseCases;
using GymErp.Domain.Enrollments;

namespace GymErp.Tests.Architecture;

// A regra de dependência de Martin como teste: as dependências só apontam para dentro.
// Se alguém fizer a Application usar EF Core, ASP.NET ou Microsoft.Extensions, este teste quebra no pipeline.
public class DependencyRuleTests
{
    private static readonly string[] AllowedLibraries = ["CSharpFunctionalExtensions"];

    private static string[] NonBclReferences(Assembly assembly) =>
        assembly.GetReferencedAssemblies()
            .Select(r => r.Name!)
            .Where(n => n is not ("mscorlib" or "netstandard") && !n.StartsWith("System"))
            .Except(AllowedLibraries)
            .OrderBy(n => n)
            .ToArray();

    [Fact]
    public void Domain_depends_on_nothing_but_the_BCL_and_allowed_libraries() =>
        Assert.Empty(NonBclReferences(typeof(Enrollment).Assembly));

    [Fact]
    public void Application_depends_only_on_Domain() =>
        Assert.Equal(new[] { "GymErp.Domain" }, NonBclReferences(typeof(EnrollmentError).Assembly));

    [Fact]
    public void Use_case_ports_do_not_expose_domain_entities()
    {
        var domainTypes = typeof(Enrollment).Assembly.GetTypes().ToHashSet();
        var useCasePorts = typeof(EnrollmentError).Assembly.GetTypes()
            .Where(t => t.IsInterface && t.Namespace!.StartsWith("GymErp.Application.UseCases"));

        var exposed = useCasePorts
            .SelectMany(i => i.GetMethods())
            .SelectMany(m => m.GetParameters().Select(p => p.ParameterType).Append(m.ReturnType))
            .SelectMany(Unwrap)
            .Where(domainTypes.Contains)
            .Distinct();

        Assert.Empty(exposed);
    }

    private static IEnumerable<Type> Unwrap(Type type) =>
        type.IsGenericType ? type.GetGenericArguments().SelectMany(Unwrap).Append(type) : [type];
}
