using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NexusHRM.Data;
using NexusHRM.Infrastructure;
using NexusHRM.Models;

namespace NexusHRM.Pages.Account;

public class LoginModel : PageModel
{
    private readonly DummyData _data;

    public record PersonaOption(Employee Employee, string Description);

    public List<PersonaOption> Personas { get; private set; } = new();
    public string? ErrorMessage { get; private set; }
    public string  LastEmail    { get; private set; } = "";

    public LoginModel(DummyData data) => _data = data;

    private void BuildPersonas()
    {
        Personas = new List<PersonaOption>
        {
            new(_data.GetEmployee(3)!, "Full employee management, run payroll, all HR operations"),
            new(_data.GetEmployee(2)!, "Approve team leave, review team performance, team oversight"),
            new(_data.GetEmployee(1)!, "View your profile, apply for leave, clock in, check your payslip"),
        };
    }

    public IActionResult OnGet()
    {
        BuildPersonas();
        if (int.TryParse(HttpContext.Session.GetString(AppPageModel.SessionUserKey), out var id) &&
            _data.GetEmployee(id) is { IsOffboarded: false })
            return RedirectToPage("/Index");
        return Page();
    }

    public IActionResult OnPost(string loginType, string? email, string? password, int? employeeId)
    {
        BuildPersonas();

        // Demo access: one click sign-in for the three showcase personas only.
        if (loginType == "demo" && employeeId.HasValue && Personas.Any(p => p.Employee.Id == employeeId))
        {
            SignIn(_data.GetEmployee(employeeId.Value)!);
            return RedirectToPage("/Index");
        }

        LastEmail = email?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            ErrorMessage = "Please enter your email address and password.";
            return Page();
        }

        var emp = _data.FindByEmail(email);
        if (emp == null || emp.IsOffboarded || password != DummyData.DemoPassword(emp.Role))
        {
            ErrorMessage = "Incorrect email or password. Please try again.";
            return Page();
        }

        SignIn(emp);
        return RedirectToPage("/Index");
    }

    private void SignIn(Employee emp)
    {
        HttpContext.Session.Clear();
        HttpContext.Session.SetString(AppPageModel.SessionUserKey, emp.Id.ToString());
    }
}
