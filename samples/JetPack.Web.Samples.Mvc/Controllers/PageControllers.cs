using JetPack.Web.Samples.Mvc.Models;
using JetPack.Wpf.Samples.Admin;
using Microsoft.AspNetCore.Mvc;

namespace JetPack.Web.Samples.Mvc.Controllers;

// One controller per page of the admin sample. Nothing is stored: Save and Delete only show
// what the page looks like afterwards, as in the WPF version.

public class DashboardController : Controller
{
    public IActionResult Index() => View();
}

public class EmployeesController : Controller
{
    public IActionResult Index(string? id, string? search, string? department, bool activeOnly)
    {
        var page = Build(id, search, department, activeOnly, form: null);
        page.Saved = TempData["Saved"] is true;
        return View(page);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Save([Bind(Prefix = "Form")] EmployeeForm form)
    {
        if (!ModelState.IsValid)
        {
            return View(nameof(Index), Build(form.Id, null, null, false, form));
        }
        TempData["Saved"] = true;
        return RedirectToAction(nameof(Index), new { id = form.Id });
    }

    private static EmployeesPage Build(string? id, string? search, string? department, bool activeOnly, EmployeeForm? form)
    {
        department ??= SampleData.DepartmentFilter[0];
        var list = SampleData.Employees
            .Where(e => string.IsNullOrWhiteSpace(search) || e.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
                        || e.Title.Contains(search, StringComparison.OrdinalIgnoreCase))
            .Where(e => department == SampleData.DepartmentFilter[0] || e.Department == department)
            .Where(e => !activeOnly || e.IsActive)
            .ToList();
        // The WPF sample opens with the fourth row (Heather Andrade) selected
        var selected = list.FirstOrDefault(e => Slug.Of(e.Name) == id)
                       ?? (id is null && list.Count > 3 ? list[3] : list.FirstOrDefault());
        return new EmployeesPage
        {
            Search = search,
            Department = department,
            ActiveOnly = activeOnly,
            Employees = list,
            Form = form ?? (selected is null ? null : EmployeeForm.From(selected)),
        };
    }
}

public class TimesheetsController : Controller
{
    public IActionResult Index(string? week, string? status)
    {
        status ??= SampleData.Statuses[0];
        var rows = SampleData.Timesheet
            .Where(r => status == SampleData.Statuses[0] || r.Status == status)
            .ToList();
        return View(new TimesheetsPage { Week = week ?? SampleData.Weeks[0], Status = status, Rows = rows });
    }
}

public class ProjectsController : Controller
{
    public IActionResult Index(string? customer, string? project)
    {
        var c = SampleData.Customers.FirstOrDefault(x => Slug.Of(x.Name) == customer);
        var p = SampleData.Projects.FirstOrDefault(x => Slug.Of(x.Name) == project);
        // Nothing chosen yet: show the first project
        if (c is null && p is null)
        {
            p = SampleData.Projects[0];
        }
        return View(new ProjectsPage { Customer = p is null ? c : null, Project = p });
    }
}
