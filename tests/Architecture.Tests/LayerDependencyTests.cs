using System.Reflection;
using NetArchTest.Rules;

namespace Architecture.Tests;

/// <summary>Type-level dependency rules, checked on the compiled service assemblies.</summary>
public sealed class LayerDependencyTests
{
    private static readonly Dictionary<string, (Assembly Domain, Assembly Application, Assembly Infrastructure)> Layers = new()
    {
        ["Identity"] = (typeof(Hrms.Identity.Domain.AssemblyReference).Assembly,
            typeof(Hrms.Identity.Application.AssemblyReference).Assembly,
            typeof(Hrms.Identity.Infrastructure.AssemblyReference).Assembly),
        ["CoreHr"] = (typeof(Hrms.CoreHr.Domain.AssemblyReference).Assembly,
            typeof(Hrms.CoreHr.Application.AssemblyReference).Assembly,
            typeof(Hrms.CoreHr.Infrastructure.AssemblyReference).Assembly),
        ["Time"] = (typeof(Hrms.Time.Domain.AssemblyReference).Assembly,
            typeof(Hrms.Time.Application.AssemblyReference).Assembly,
            typeof(Hrms.Time.Infrastructure.AssemblyReference).Assembly),
        ["Payroll"] = (typeof(Hrms.Payroll.Domain.AssemblyReference).Assembly,
            typeof(Hrms.Payroll.Application.AssemblyReference).Assembly,
            typeof(Hrms.Payroll.Infrastructure.AssemblyReference).Assembly),
        ["Workflow"] = (typeof(Hrms.Workflow.Domain.AssemblyReference).Assembly,
            typeof(Hrms.Workflow.Application.AssemblyReference).Assembly,
            typeof(Hrms.Workflow.Infrastructure.AssemblyReference).Assembly),
        ["Expense"] = (typeof(Hrms.Expense.Domain.AssemblyReference).Assembly,
            typeof(Hrms.Expense.Application.AssemblyReference).Assembly,
            typeof(Hrms.Expense.Infrastructure.AssemblyReference).Assembly),
        ["Talent"] = (typeof(Hrms.Talent.Domain.AssemblyReference).Assembly,
            typeof(Hrms.Talent.Application.AssemblyReference).Assembly,
            typeof(Hrms.Talent.Infrastructure.AssemblyReference).Assembly),
    };

    public static TheoryData<string> Services => new(Layers.Keys);

    [Theory]
    [MemberData(nameof(Services))]
    public void Domain_has_no_infrastructure_dependencies(string service)
    {
        var result = Types.InAssembly(Layers[service].Domain)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Microsoft.EntityFrameworkCore",
                "Npgsql",
                "MassTransit",
                "Microsoft.AspNetCore",
                "MediatR",
                $"Hrms.{service}.Application",
                $"Hrms.{service}.Infrastructure",
                $"Hrms.{service}.Api")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(Describe(result));
    }

    [Theory]
    [MemberData(nameof(Services))]
    public void Application_does_not_depend_on_infrastructure(string service)
    {
        var result = Types.InAssembly(Layers[service].Application)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Microsoft.EntityFrameworkCore",
                "Npgsql",
                $"Hrms.{service}.Infrastructure",
                $"Hrms.{service}.Api")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(Describe(result));
    }

    [Theory]
    [MemberData(nameof(Services))]
    public void Service_types_do_not_depend_on_other_services(string service)
    {
        var others = Layers.Keys.Where(s => s != service).Select(s => $"Hrms.{s}.").ToArray();
        var (domain, application, infrastructure) = Layers[service];

        var result = Types.InAssemblies([domain, application, infrastructure])
            .ShouldNot()
            .HaveDependencyOnAny(others)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(Describe(result));
    }

    private static string Describe(TestResult result) =>
        result.IsSuccessful
            ? string.Empty
            : "violating types: " + string.Join(", ", result.FailingTypeNames ?? []);
}
