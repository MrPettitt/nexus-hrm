using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NexusHRM.Data;
using NexusHRM.Models;

namespace NexusHRM.Infrastructure;

/// <summary>
/// Base class for every signed-in page. Resolves the current user from the session,
/// redirects anonymous or offboarded users to the sign-in page, and serialises
/// handlers so the shared in-memory data is never mutated concurrently.
/// </summary>
public abstract class AppPageModel : PageModel
{
    public const string SessionUserKey = "UserId";

    protected readonly DummyData Data;

    protected AppPageModel(DummyData data) => Data = data;

    public Employee Me { get; private set; } = null!;
    public int    UserId   => Me.Id;
    public string UserRole => Me.Role;
    public bool   IsHR      => Me.Role == "HR";
    public bool   IsManager => Me.Role == "Manager";

    public override async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        await Data.Gate.WaitAsync();
        try
        {
            var user = int.TryParse(HttpContext.Session.GetString(SessionUserKey), out var id) ? Data.GetEmployee(id) : null;
            if (user == null || user.IsOffboarded)
            {
                HttpContext.Session.Clear();
                context.Result = RedirectToPage("/Account/Login");
                return;
            }

            Me = user;
            Data.RefreshStatuses();
            await next();
        }
        finally
        {
            Data.Gate.Release();
        }
    }

    protected void Flash(string message)      => TempData["Flash"] = message;
    protected void FlashError(string message) => TempData["FlashError"] = message;

    protected IActionResult Forbidden(string message = "You do not have permission to do that.")
    {
        FlashError(message);
        return RedirectToPage();
    }

    public Employee? GetEmployee(int id) => Data.GetEmployee(id);
}
