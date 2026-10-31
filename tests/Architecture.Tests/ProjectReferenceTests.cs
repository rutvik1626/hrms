using System.Xml.Linq;

namespace Architecture.Tests;

/// <summary>
/// Enforces service and layer boundaries on the project-reference graph. This catches violations
/// even when the offending reference isn't used in code yet (the compiler would drop it from the
/// assembly metadata, so type-level checks alone can't see it).
/// </summary>
public sealed class ProjectReferenceTests
{
    public static TheoryData<string> Services => new(RepoLayout.Services);

    [Theory]
    [MemberData(nameof(Services))]
    public void Service_projects_do_not_reference_other_services(string service)
    {
        var offending = RepoLayout.ServiceProjects(service)
            .SelectMany(p => RepoLayout.ProjectReferences(p).Select(r => (Project: p, Reference: r)))
            .Where(x => x.Reference.StartsWith(RepoLayout.ServicesDir, StringComparison.OrdinalIgnoreCase)
                        && !x.Reference.StartsWith(Path.Combine(RepoLayout.ServicesDir, service) + Path.DirectorySeparatorChar,
                            StringComparison.OrdinalIgnoreCase))
            .Select(x => $"{Path.GetFileName(x.Project)} -> {Path.GetFileName(x.Reference)}")
            .ToList();

        offending.Should().BeEmpty("a service must never reference another service's projects");
    }

    [Theory]
    [MemberData(nameof(Services))]
    public void Domain_references_only_core_building_blocks(string service)
    {
        var allowed = new[] { "Hrms.Common.csproj", "Hrms.Domain.Abstractions.csproj" };

        var references = RepoLayout.ProjectReferences(RepoLayout.LayerProject(service, "Domain"))
            .Select(Path.GetFileName);

        references.Should().OnlyContain(r => allowed.Contains(r), "Domain must stay free of infrastructure and other layers");
    }

    [Theory]
    [MemberData(nameof(Services))]
    public void Application_does_not_reference_Infrastructure_or_Api(string service)
    {
        var references = RepoLayout.ProjectReferences(RepoLayout.LayerProject(service, "Application"))
            .Select(Path.GetFileName)
            .ToList();

        references.Should().NotContain(r => r!.EndsWith(".Infrastructure.csproj", StringComparison.Ordinal)
                                             || r.EndsWith(".Api.csproj", StringComparison.Ordinal)
                                             || r == "Hrms.Persistence.csproj"
                                             || r == "Hrms.Web.csproj");
    }

    [Fact]
    public void Building_blocks_do_not_reference_services_or_gateway()
    {
        var offending = Directory.EnumerateFiles(RepoLayout.BuildingBlocksDir, "*.csproj", SearchOption.AllDirectories)
            .SelectMany(RepoLayout.ProjectReferences)
            .Where(r => !r.StartsWith(RepoLayout.BuildingBlocksDir, StringComparison.OrdinalIgnoreCase))
            .ToList();

        offending.Should().BeEmpty("building blocks are shared libraries and must not depend on services");
    }
}

internal static class RepoLayout
{
    public static readonly string[] Services = ["Identity", "CoreHr", "Time", "Payroll", "Workflow", "Expense", "Talent"];

    public static readonly string Root = FindRoot();
    public static readonly string ServicesDir = Path.Combine(Root, "src", "Services");
    public static readonly string BuildingBlocksDir = Path.Combine(Root, "src", "BuildingBlocks");

    public static IEnumerable<string> ServiceProjects(string service) =>
        Directory.EnumerateFiles(Path.Combine(ServicesDir, service), "*.csproj", SearchOption.AllDirectories);

    public static string LayerProject(string service, string layer) =>
        Path.Combine(ServicesDir, service, $"Hrms.{service}.{layer}", $"Hrms.{service}.{layer}.csproj");

    /// <summary>Absolute, normalised paths of the project's ProjectReference items.</summary>
    public static IEnumerable<string> ProjectReferences(string csproj)
    {
        var dir = Path.GetDirectoryName(csproj)!;
        return XDocument.Load(csproj)
            .Descendants("ProjectReference")
            .Select(e => e.Attribute("Include")?.Value)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => Path.GetFullPath(Path.Combine(dir, v!.Replace('\\', Path.DirectorySeparatorChar))));
    }

    private static string FindRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Hrms.sln")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("Could not locate Hrms.sln above the test output directory.");
    }
}
