using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NexusHRM.Data;
using NexusHRM.Infrastructure;
using NexusHRM.Models;

namespace NexusHRM.Pages;

public class KpiItem
{
    public string Label { get; set; } = "";
    public string Value { get; set; } = "";
    public string? Sub { get; set; }
    public string IconSvg { get; set; } = "";
    public string Color { get; set; } = "#5B5BD6";
    public string Color22 => Color + "22";
}

public class IndexModel : AppPageModel
{
    public List<KpiItem> KPIs { get; private set; } = new();
    public List<LeaveRequest> RecentLeave { get; private set; } = new();
    public List<Employee> PeopleSnapshot { get; private set; } = new();
    public string Greeting { get; private set; } = "";
    public string PeopleTitle { get; private set; } = "People";

    public IndexModel(DummyData data) : base(data) { }

    public IActionResult OnGet()
    {
        var now = DateTime.Now.Hour;
        Greeting = (now < 12 ? "Good morning" : now < 18 ? "Good afternoon" : "Good evening") + ", " + Me.Name.Split(' ')[0];

        var month   = Data.Today;
        var pending = Data.PendingFor(Me);

        KPIs = UserRole switch
        {
            "Employee" => EmployeeKpis(),
            "Manager"  => new List<KpiItem>
            {
                new() { Label="Team Size",      Value=Data.DirectReports(UserId).Count().ToString(),                                             Sub="Direct reports",   IconSvg=PeopleIcon, Color="#5B5BD6" },
                new() { Label="Pending Leave",  Value=pending.ToString(),                                                                        Sub="Need your approval", IconSvg=CalIcon,  Color="#F59E0B" },
                new() { Label="On Leave Today", Value=Data.DirectReports(UserId).Count(e => e.Status == "On Leave").ToString(),                  Sub="Team members",     IconSvg=AlertIcon,  Color="#EF4444" },
                new() { Label="Reviews Due",    Value=Data.DirectReports(UserId).Count(e => !Data.PerformanceReviews.Any(r => r.EmployeeId == e.Id && r.Period == CurrentPeriod)).ToString(), Sub=CurrentPeriod, IconSvg=StarIcon, Color="#EC4899" },
            },
            _ => new List<KpiItem>
            {
                new() { Label="Total Employees", Value=Data.ActiveEmployees.Count().ToString(),                                  Sub="All departments", IconSvg=PeopleIcon, Color="#5B5BD6" },
                new() { Label="Pending Leave",   Value=pending.ToString(),                                                        Sub="Need approval",   IconSvg=CalIcon,    Color="#F59E0B" },
                new() { Label="On Leave Today",  Value=Data.ActiveEmployees.Count(e => e.Status == "On Leave").ToString(),        Sub="Across the company", IconSvg=AlertIcon, Color="#EF4444" },
                new() { Label=Data.CurrentMonth + " Payroll", Value="R " + (Data.PayrollRecords.Sum(p => p.NetPay) / 1000m).ToString("N0") + "K", Sub="Net pay, " + Data.PayrollRecords.Count(p => p.IsPaid) + " of " + Data.PayrollRecords.Count + " paid", IconSvg=WalletIcon, Color="#10B981" },
            }
        };

        RecentLeave = Data.VisibleLeave(Me).OrderByDescending(l => l.AppliedDate).ThenByDescending(l => l.Id).Take(5).ToList();

        PeopleSnapshot = UserRole switch
        {
            "Employee" => Data.ActiveEmployees.Where(e => e.Department == Me.Department).Take(6).ToList(),
            "Manager"  => Data.DirectReports(UserId).Take(6).ToList(),
            _          => Data.ActiveEmployees.Take(6).ToList(),
        };
        PeopleTitle = UserRole switch { "Employee" => "My Department", "Manager" => "My Team", _ => "People" };

        return Page();
    }

    private static double HoursWorked(AttendanceRecord a)
    {
        var start = TimeSpan.Parse(a.ClockIn!);
        var end   = a.ClockOut != null ? TimeSpan.Parse(a.ClockOut) : DateTime.Now.TimeOfDay;
        return Math.Max(0, (end - start).TotalHours);
    }

    private string CurrentPeriod => $"Q{(Data.Today.Month - 1) / 3 + 1} {Data.Today.Year}";

    private List<KpiItem> EmployeeKpis()
    {
        var annual = Data.BalancesFor(UserId).First(b => b.Type == "Annual");
        var mine   = Data.AttendanceRecords.Where(a => a.EmployeeId == UserId).ToList();
        var weekStart = Data.Today.AddDays(-(((int)Data.Today.DayOfWeek + 6) % 7));
        var hours = mine.Where(a => a.Date >= weekStart && a.ClockIn != null).Sum(HoursWorked);
        var workdays = mine.Where(a => a.Status != "On Leave" && (a.Date < Data.Today || a.ClockIn != null)).ToList();
        var present  = workdays.Count(a => a.ClockIn != null);
        var rating   = Data.PerformanceReviews.Where(r => r.EmployeeId == UserId).Select(r => (double?)r.OverallRating).Average();

        return new List<KpiItem>
        {
            new() { Label="Leave Balance",   Value=$"{annual.Remaining} days", Sub="Annual remaining",  IconSvg=CalIcon,   Color="#5B5BD6" },
            new() { Label="Hours This Week", Value=$"{hours:0.0} hrs",         Sub="Logged so far",     IconSvg=ClockIcon, Color="#10B981" },
            new() { Label="My Rating",       Value=rating.HasValue ? $"{rating:0.0} / 5" : "None yet", Sub="Latest review", IconSvg=StarIcon, Color="#F59E0B" },
            new() { Label="Attendance",      Value=workdays.Count == 0 ? "N/A" : $"{present * 100 / workdays.Count}%", Sub="Last 5 working days", IconSvg=TrendIcon, Color="#EC4899" },
        };
    }

    private const string PeopleIcon = "<svg fill='none' stroke='currentColor' stroke-width='2' viewBox='0 0 24 24'><path d='M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2'/><circle cx='9' cy='7' r='4'/><path d='M23 21v-2a4 4 0 0 0-3-3.87'/><path d='M16 3.13a4 4 0 0 1 0 7.75'/></svg>";
    private const string CalIcon    = "<svg fill='none' stroke='currentColor' stroke-width='2' viewBox='0 0 24 24'><rect x='3' y='4' width='18' height='18' rx='2'/><line x1='16' y1='2' x2='16' y2='6'/><line x1='8' y1='2' x2='8' y2='6'/><line x1='3' y1='10' x2='21' y2='10'/></svg>";
    private const string ClockIcon  = "<svg fill='none' stroke='currentColor' stroke-width='2' viewBox='0 0 24 24'><circle cx='12' cy='12' r='10'/><polyline points='12 6 12 12 16 14'/></svg>";
    private const string StarIcon   = "<svg fill='none' stroke='currentColor' stroke-width='2' viewBox='0 0 24 24'><polygon points='12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2'/></svg>";
    private const string TrendIcon  = "<svg fill='none' stroke='currentColor' stroke-width='2' viewBox='0 0 24 24'><polyline points='22 7 13.5 15.5 8.5 10.5 2 17'/><polyline points='16 7 22 7 22 13'/></svg>";
    private const string AlertIcon  = "<svg fill='none' stroke='currentColor' stroke-width='2' viewBox='0 0 24 24'><circle cx='12' cy='12' r='10'/><line x1='12' y1='8' x2='12' y2='12'/><line x1='12' y1='16' x2='12.01' y2='16'/></svg>";
    private const string WalletIcon = "<svg fill='none' stroke='currentColor' stroke-width='2' viewBox='0 0 24 24'><path d='M20 12V8H6a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2h14v4'/><path d='M4 6v12a2 2 0 0 0 2 2h14v-4'/><circle cx='16' cy='14' r='1' fill='currentColor'/></svg>";
}
