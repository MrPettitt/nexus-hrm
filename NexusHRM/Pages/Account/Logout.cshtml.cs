using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace NexusHRM.Pages.Account;

public class LogoutModel : PageModel
{
    public IActionResult OnGet() => RedirectToPage("/Index");

    public IActionResult OnPost()
    {
        HttpContext.Session.Clear();
        return RedirectToPage("/Account/Login");
    }
}
