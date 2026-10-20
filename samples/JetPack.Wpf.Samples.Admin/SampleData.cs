namespace JetPack.Wpf.Samples.Admin;

// Fixed sample content for the pages. A real application would load this from its own data layer.

public sealed record Employee(string Name, string Title, string Department, string Email, string Phone,
    DateTime StartDate, double HoursPerWeek, bool IsActive);

public sealed record Project(string Name, string Customer, string Manager, double BudgetHours, double UsedHours,
    DateTime Deadline, int Satisfaction, string Notes, IReadOnlyList<string> Team)
{
    public double Progress => UsedHours / BudgetHours * 100;
}

public sealed record Customer(string Name, string City, IReadOnlyList<Project> Projects);

public sealed record TimesheetRow(string Employee, string Project, double Monday, double Tuesday, double Wednesday,
    double Thursday, double Friday, string Status)
{
    public double Total => Monday + Tuesday + Wednesday + Thursday + Friday;
}

public sealed record Activity(string Time, string Text);

public static class SampleData
{
    public static IReadOnlyList<string> Departments { get; } = ["Consulting", "Development", "Finance", "Sales", "Support"];

    public static IReadOnlyList<string> DepartmentFilter { get; } = ["All departments", .. Departments];

    public static IReadOnlyList<Employee> Employees { get; } =
    [
        new("Charles Hoyle", "Senior consultant", "Consulting", "charles.hoyle@contoso.com", "(555) 555-0141", new(2019, 3, 1), 37, true),
        new("Daniel Tiedemann", "Developer", "Development", "daniel.tiedemann@contoso.com", "(555) 555-0187", new(2021, 8, 15), 37, true),
        new("David Ritter", "Developer", "Development", "david.ritter@contoso.com", "(555) 555-0112", new(2024, 1, 2), 30, true),
        new("Heather Andrade", "Project manager", "Consulting", "heather.andrade@contoso.com", "(555) 555-1234", new(2018, 5, 1), 37, true),
        new("Ida Kragh", "Controller", "Finance", "ida.kragh@contoso.com", "(555) 555-0163", new(2020, 2, 1), 37, true),
        new("Lindsey Ballard", "Account manager", "Sales", "lindsey.ballard@contoso.com", "(555) 555-0195", new(2022, 9, 1), 20, true),
        new("Mads Lund", "Support engineer", "Support", "mads.lund@contoso.com", "(555) 555-0128", new(2023, 4, 17), 37, true),
        new("Sofia Brandt", "UX designer", "Development", "sofia.brandt@contoso.com", "(555) 555-0176", new(2025, 6, 1), 37, true),
        new("Vivian Smail", "Account manager", "Sales", "vivian.smail@contoso.com", "(555) 555-0150", new(2017, 11, 1), 37, false),
    ];

    public static IReadOnlyList<Customer> Customers { get; } =
    [
        new("Fabrikam", "Seattle",
        [
            new("Payroll integration", "Fabrikam", "Heather Andrade", 480, 455, new(2026, 11, 20), 4,
                "Nightly export of approved hours to the payroll system. The customer asked for a CSV fallback.",
                ["Daniel Tiedemann", "David Ritter", "Heather Andrade"]),
            new("Reporting portal", "Fabrikam", "Charles Hoyle", 300, 120, new(2027, 2, 1), 5,
                "Dashboards for the regional managers, built on the existing data warehouse.",
                ["Charles Hoyle", "Sofia Brandt"]),
        ]),
        new("Southridge Video", "Portland",
        [
            new("Booking app", "Southridge Video", "Heather Andrade", 620, 410, new(2026, 12, 18), 3,
                "The second release adds waiting lists. Testing starts in week 46.",
                ["Daniel Tiedemann", "Sofia Brandt", "Mads Lund"]),
        ]),
        new("Soar Airlines", "Denver",
        [
            new("Crew planning", "Soar Airlines", "Charles Hoyle", 900, 870, new(2026, 10, 30), 4,
                "Go-live is planned for 30 October; hours are close to the budget.",
                ["Charles Hoyle", "David Ritter"]),
            new("Support agreement", "Soar Airlines", "Mads Lund", 200, 64, new(2027, 6, 30), 5,
                "Second-line support, 8 to 16 on weekdays.",
                ["Mads Lund"]),
        ]),
    ];

    public static IReadOnlyList<Project> Projects { get; } = Customers.SelectMany(c => c.Projects).ToList();

    public static IReadOnlyList<string> Weeks { get; } = ["Week 42 · Oct 12–16", "Week 41 · Oct 5–9", "Week 40 · Sep 28–Oct 2"];

    public static IReadOnlyList<string> Statuses { get; } = ["All statuses", "Draft", "Submitted", "Approved", "Rejected"];

    public static IReadOnlyList<TimesheetRow> Timesheet { get; } =
    [
        new("Charles Hoyle", "Crew planning", 7.5, 7.5, 6, 7.5, 4.5, "Approved"),
        new("Charles Hoyle", "Reporting portal", 0, 0, 1.5, 0, 3, "Approved"),
        new("Daniel Tiedemann", "Payroll integration", 6, 4.5, 7.5, 6, 6, "Submitted"),
        new("Daniel Tiedemann", "Booking app", 1.5, 3, 0, 1.5, 1.5, "Submitted"),
        new("David Ritter", "Crew planning", 6, 6, 6, 6, 0, "Draft"),
        new("Heather Andrade", "Payroll integration", 3, 3, 4.5, 3, 1.5, "Submitted"),
        new("Heather Andrade", "Booking app", 4.5, 4.5, 3, 4.5, 3, "Submitted"),
        new("Mads Lund", "Support agreement", 7.5, 7.5, 7.5, 7.5, 7.5, "Approved"),
        new("Sofia Brandt", "Booking app", 7.5, 6, 7.5, 3, 4.5, "Rejected"),
        new("Sofia Brandt", "Reporting portal", 0, 1.5, 0, 4.5, 3, "Draft"),
    ];

    public static IReadOnlyList<Activity> RecentActivity { get; } =
    [
        new("Today 09:42", "Mads Lund submitted the timesheet for week 42"),
        new("Today 09:12", "Heather Andrade moved \"Booking app\" testing to week 46"),
        new("Today 08:30", "Soar Airlines approved the crew planning release plan"),
        new("Yesterday 15:05", "Sofia Brandt joined \"Reporting portal\""),
        new("Yesterday 11:00", "Payroll export for week 41 completed"),
        new("Wednesday 14:20", "Vivian Smail was set to inactive"),
    ];
}
