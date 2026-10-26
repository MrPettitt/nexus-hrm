using Microsoft.AspNetCore.Mvc;
using NexusHRM.Data;
using NexusHRM.Infrastructure;
using NexusHRM.Models;

namespace NexusHRM.Pages.Attendance;

public class IndexModel : AppPageModel
{
    public List<AttendanceRecord> Records        { get; private set; } = new();
    public AttendanceRecord?      MyRecord       { get; private set; }
    public List<string>           AvailableDates { get; private set; } = new();
    public string SelectedDate { get; private set; } = "";
    public string TodayLabel   { get; private set; } = "";
    public bool   TeamView     => UserRole != "Employee";

    public IndexModel(DummyData data) : base(data) { }

    public IActionResult OnGet(string? date)
    {
        var today = Data.Today;
        TodayLabel = today.ToString("dddd, d MMMM yyyy");

        var visible = Data.VisibleAttendance(Me).ToList();

        AvailableDates = visible.Select(a => a.Date.ToString("yyyy-MM-dd")).Distinct().OrderBy(d => d).ToList();
        SelectedDate   = date != null && AvailableDates.Contains(date) ? date : today.ToString("yyyy-MM-dd");

        MyRecord = Data.AttendanceRecords.FirstOrDefault(a => a.EmployeeId == UserId && a.Date.Date == today);

        Records = TeamView
            ? visible.Where(a => a.Date.ToString("yyyy-MM-dd") == SelectedDate).OrderBy(a => GetEmployee(a.EmployeeId)?.Name).ToList()
            : visible.OrderByDescending(a => a.Date).ToList();

        return Page();
    }

    public IActionResult OnPost(string? action)
    {
        var today = Data.Today;
        var now   = DateTime.Now.ToString("HH:mm");
        var rec   = Data.AttendanceRecords.FirstOrDefault(a => a.EmployeeId == UserId && a.Date.Date == today);

        if (rec == null)
        {
            rec = new AttendanceRecord { EmployeeId = UserId, Date = today, Status = "Not Clocked In" };
            Data.AttendanceRecords.Add(rec);
        }

        if (rec.Status == "On Leave")
            return Forbidden("You are on approved leave today.");

        if (action == "clockIn" && rec.ClockIn == null)
        {
            rec.ClockIn = now;
            rec.Status  = "In Progress";
            Flash($"Clocked in at {now}.");
        }
        else if (action == "clockOut" && rec.ClockIn != null && rec.ClockOut == null)
        {
            rec.ClockOut = now;
            rec.Status   = "Present";
            Flash($"Clocked out at {now}. Have a good evening.");
        }

        return RedirectToPage();
    }
}
