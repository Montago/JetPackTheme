using System.ComponentModel.DataAnnotations;
using JetPack.Wpf.Samples.Admin;

namespace JetPack.Web.Samples.Mvc.Models;

// View models for the pages. The data itself is the fixed SampleData of the WPF admin sample.

public static class Slug
{
    public static string Of(string name) => name.ToLowerInvariant().Replace(' ', '-');
}

public sealed class EmployeeForm
{
    public string Id { get; set; } = "";

    [Required]
    public string Name { get; set; } = "";

    [Required]
    public string Title { get; set; } = "";

    public string Department { get; set; } = "";

    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [DataType(DataType.Date)]
    public DateTime StartDate { get; set; }

    [Range(0, 40)]
    public double HoursPerWeek { get; set; }

    public bool IsActive { get; set; }

    public static EmployeeForm From(Employee e) => new()
    {
        Id = Slug.Of(e.Name),
        Name = e.Name,
        Title = e.Title,
        Department = e.Department,
        Email = e.Email,
        StartDate = e.StartDate,
        HoursPerWeek = e.HoursPerWeek,
        IsActive = e.IsActive,
    };
}

public sealed class EmployeesPage
{
    public string? Search { get; init; }
    public string Department { get; init; } = SampleData.DepartmentFilter[0];
    public bool ActiveOnly { get; init; }
    public IReadOnlyList<Employee> Employees { get; init; } = [];
    public EmployeeForm? Form { get; init; }
    public bool Saved { get; set; }

    /// <summary>The query string that keeps the filters when a row is opened.</summary>
    public string Link(Employee e)
    {
        var query = new List<string> { "id=" + Uri.EscapeDataString(Slug.Of(e.Name)) };
        if (!string.IsNullOrEmpty(Search)) query.Add("search=" + Uri.EscapeDataString(Search));
        if (Department != SampleData.DepartmentFilter[0]) query.Add("department=" + Uri.EscapeDataString(Department));
        if (ActiveOnly) query.Add("activeOnly=true");
        return "?" + string.Join('&', query);
    }
}

public sealed class TimesheetsPage
{
    public string Week { get; init; } = SampleData.Weeks[0];
    public string Status { get; init; } = SampleData.Statuses[0];
    public IReadOnlyList<TimesheetRow> Rows { get; init; } = [];
}

public sealed class ProjectsPage
{
    public Customer? Customer { get; init; }
    public Project? Project { get; init; }
}
