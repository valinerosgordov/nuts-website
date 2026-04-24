using System.Reflection;
using System.Runtime.CompilerServices;
using NetArchTest.Rules;
using Nuts.Domain.Common;
using Nuts.Domain.Entities;

namespace Nuts.Tests.Architecture;

public class LayerTests
{
    private static readonly Assembly DomainAssembly = typeof(Result).Assembly;
    private static readonly Assembly ApplicationAssembly =
        Assembly.Load("Nuts.Application");

    [Fact]
    public void Domain_ShouldNotDependOn_Infrastructure()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("Nuts.Infrastructure")
            .GetResult();

        Assert.True(result.IsSuccessful,
            string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Domain_ShouldNotDependOn_Api()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("Nuts.Api")
            .GetResult();

        Assert.True(result.IsSuccessful,
            string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Domain_ShouldNotDependOn_EntityFrameworkCore()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult();

        Assert.True(result.IsSuccessful,
            string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Domain_ShouldNotDependOn_SystemNetHttp()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("System.Net.Http")
            .GetResult();

        Assert.True(result.IsSuccessful,
            string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Application_ShouldNotDependOn_Infrastructure()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOn("Nuts.Infrastructure")
            .GetResult();

        Assert.True(result.IsSuccessful,
            string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Application_ShouldNotDependOn_Api()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOn("Nuts.Api")
            .GetResult();

        Assert.True(result.IsSuccessful,
            string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Application_ShouldNotDependOn_EntityFrameworkCore()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult();

        Assert.True(result.IsSuccessful,
            string.Join(", ", result.FailingTypeNames ?? []));
    }

    private static Type[] GetDomainEntityTypes() =>
        DomainAssembly.GetTypes()
            .Where(t => t.Namespace == typeof(Order).Namespace
                        && t.IsClass
                        && !t.IsAbstract
                        && !t.IsNested
                        && t.GetCustomAttribute<CompilerGeneratedAttribute>() is null
                        && !t.Name.Contains('<'))
            .ToArray();

    [Fact]
    public void DomainEntities_AreSealed()
    {
        var entityTypes = GetDomainEntityTypes();

        var nonSealed = entityTypes.Where(t => !t.IsSealed).Select(t => t.FullName).ToArray();

        Assert.True(nonSealed.Length == 0,
            $"Entities must be sealed. Offenders: {string.Join(", ", nonSealed)}");
    }

    [Fact]
    public void DomainEntities_HavePrivateParameterlessConstructor()
    {
        var entityTypes = GetDomainEntityTypes();

        var missing = entityTypes
            .Where(t =>
            {
                var ctor = t.GetConstructor(
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
                    binder: null, types: Type.EmptyTypes, modifiers: null);
                return ctor is null || ctor.IsPublic;
            })
            .Select(t => t.FullName)
            .ToArray();

        Assert.True(missing.Length == 0,
            $"Entities must have a private parameterless ctor. Offenders: {string.Join(", ", missing)}");
    }

    [Fact]
    public void DomainEntities_DoNotReference_EntityFrameworkCore()
    {
        // Any reference from Domain assembly to EF Core (checked via module references).
        var referencedAssemblies = DomainAssembly.GetReferencedAssemblies();
        var efRef = referencedAssemblies.FirstOrDefault(a =>
            a.Name != null && a.Name.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal));

        Assert.True(efRef is null,
            $"Domain assembly references EF Core: {efRef?.FullName}");
    }
}
