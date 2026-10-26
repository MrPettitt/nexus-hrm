using Microsoft.AspNetCore.Mvc;
using NexusHRM.Data;
using NexusHRM.Infrastructure;
using NexusHRM.Models;

namespace NexusHRM.Pages.Employees;

public class IndexModel : AppPageModel
{
    private static readonly string[] Roles = { "Employee", "Manager", "HR" };

    public List<Employee> Employees   { get; private set; } = new();
    public List<Employee> Managers    { get; private set; } = new();
    public List<string>   Departments { get; private set; } = new();
    public string  Query         { get; private set; } = "";
    public string  SelectedDept  { get; private set; } = "All";
    public int     CurrentUserId => UserId;

    public IndexModel(DummyData data) : base(data) { }

    public IActionResult OnGet(string? q, string? dept)
    {
        Query        = q?.Trim() ?? "";
        SelectedDept = dept ?? "All";

        Departments = new[] { "All" }
            .Concat(Data.ActiveEmployees.Select(e => e.Department).Distinct().OrderBy(x => x))
            .ToList();

        Managers = Data.ActiveEmployees
            .Where(e => e.Role is "Manager" or "HR")
            .OrderBy(e => e.Name)
            .ToList();

        Employees = Data.Employees
            .Where(e =>
                (Query == "" ||
                 e.Name.Contains(Query, StringComparison.OrdinalIgnoreCase) ||
                 e.Title.Contains(Query, StringComparison.OrdinalIgnoreCase) ||
                 e.Department.Contains(Query, StringComparison.OrdinalIgnoreCase)) &&
                (SelectedDept == "All" || e.Department == SelectedDept))
            .OrderBy(e => e.IsOffboarded)
            .ThenBy(e => e.Name)
            .ToList();

        return Page();
    }

    public IActionResult OnPost(
        string  action,
        string? name,        string? email,      string? title,
        string? phone,       string? department, string? newDepartment,
        string? role,        decimal? salary,    int?    managerId,
        DateTime? joined,    string? avatarColor,
        int? employeeId)
    {
        if (!IsHR)
            return Forbidden("Only HR can add or offboard employees.");

        if (action == "add")
            return HandleAdd(name, email, title, phone, department, newDepartment,
                             role, salary, managerId, joined, avatarColor);

        if (action == "offboard" && employeeId.HasValue)
            return HandleOffboard(employeeId.Value);

        return RedirectToPage();
    }

    private IActionResult HandleAdd(
        string? name, string? email, string? title, string? phone,
        string? department, string? newDepartment, string? role,
        decimal? salary, int? managerId, DateTime? joined, string? avatarColor)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(title) || salary == null || salary < 0 || joined == null)
            return Forbidden("Please complete every required field.");

        email = email.Trim().ToLowerInvariant();
        if (Data.FindByEmail(email) != null)
            return Forbidden("An employee with that email address already exists.");

        var dept = department == "__new__" ? newDepartment?.Trim() : department;
        if (string.IsNullOrWhiteSpace(dept))
            return Forbidden("Please choose a department.");

        role = Roles.Contains(role) ? role! : "Employee";

        var parts    = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var initials = parts.Length >= 2
            ? $"{parts[0][0]}{parts[^1][0]}".ToUpper()
            : name.Trim()[..Math.Min(2, name.Trim().Length)].ToUpper();

        var color = !string.IsNullOrEmpty(avatarColor) && avatarColor.StartsWith('#') && avatarColor.Length == 7
            ? avatarColor
            : "#5B5BD6";

        var manager = managerId.HasValue ? Data.GetEmployee(managerId.Value) : null;

        var created = Data.AddEmployee(new Employee
        {
            Name        = name.Trim(),
            Initials    = initials,
            Email       = email,
            Title       = title.Trim(),
            Phone       = phone?.Trim() ?? "",
            Department  = dept,
            Role        = role,
            Salary      = salary.Value,
            ManagerId   = manager is { IsOffboarded: false } ? manager.Id : null,
            Joined      = joined.Value,
            Status      = "Active",
            AvatarColor = color,
        });

        Flash($"{created.Name} was added. They can sign in with {created.Email} and the standard {created.Role} demo password.");
        return RedirectToPage();
    }

    private IActionResult HandleOffboard(int employeeId)
    {
        var emp = Data.GetEmployee(employeeId);
        if (emp == null || emp.IsOffboarded)
            return RedirectToPage();
        if (emp.Id == UserId)
            return Forbidden("You cannot offboard your own account.");

        emp.Status = "Offboarded";

        // Direct reports lose their manager link; open leave requests are closed.
        foreach (var r in Data.Employees.Where(e => e.ManagerId == emp.Id)) r.ManagerId = null;
        foreach (var l in Data.LeaveRequests.Where(l => l.EmployeeId == emp.Id && l.Status == "Pending"))
            l.Status = "Rejected";

        Data.PayrollRecords.RemoveAll(p => p.EmployeeId == emp.Id && !p.IsPaid);

        Flash($"{emp.Name} has been offboarded.");
        return RedirectToPage();
    }

    public string GetManagerName(int? managerId) =>
        managerId.HasValue ? (Data.GetEmployee(managerId.Value)?.Name ?? "None") : "None";
}
