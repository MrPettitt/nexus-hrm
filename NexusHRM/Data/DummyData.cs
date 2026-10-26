// DummyData.cs
// In-memory demo data store. Everything resets when the app restarts.
// All dates are generated relative to "today" so the demo always looks current.
using NexusHRM.Models;

namespace NexusHRM.Data;

public class DummyData
{
    /// <summary>Serialises request handlers so the shared lists are never mutated concurrently.</summary>
    public SemaphoreSlim Gate { get; } = new(1, 1);

    /// <summary>The demo's "today": the current date, or the last working day on a weekend.</summary>
    public DateTime Today
    {
        get
        {
            var d = DateTime.Today;
            while (IsWeekend(d)) d = d.AddDays(-1);
            return d;
        }
    }

    public string CurrentMonth => Today.ToString("MMMM yyyy");

    public List<Employee> Employees { get; } = new()
    {
        new() { Id=1, Name="Sarah Johnson",  Initials="SJ", Email="sarah.johnson@nexus.io",  Department="Engineering", Role="Employee", Title="Senior Developer",    Salary=780000,  Joined=new DateTime(2021,3,15), Phone="+27 71 234 5678", ManagerId=2,    AvatarColor="#5B5BD6" },
        new() { Id=2, Name="Michael Chen",   Initials="MC", Email="michael.chen@nexus.io",   Department="Engineering", Role="Manager",  Title="Engineering Manager", Salary=1150000, Joined=new DateTime(2019,7,1),  Phone="+27 72 345 6789", ManagerId=null, AvatarColor="#0EA5E9" },
        new() { Id=3, Name="Amara Osei",     Initials="AO", Email="amara.osei@nexus.io",     Department="HR",          Role="HR",       Title="HR Manager",          Salary=920000,  Joined=new DateTime(2020,1,10), Phone="+27 73 456 7890", ManagerId=null, AvatarColor="#10B981" },
        new() { Id=4, Name="Lena Fischer",   Initials="LF", Email="lena.fischer@nexus.io",   Department="Finance",     Role="Employee", Title="Financial Analyst",   Salary=540000,  Joined=new DateTime(2022,6,20), Phone="+27 74 567 8901", ManagerId=5,    AvatarColor="#F59E0B" },
        new() { Id=5, Name="David Nkosi",    Initials="DN", Email="david.nkosi@nexus.io",    Department="Finance",     Role="Manager",  Title="Finance Manager",     Salary=1100000, Joined=new DateTime(2018,11,5), Phone="+27 75 678 9012", ManagerId=null, AvatarColor="#EF4444" },
        new() { Id=6, Name="Priya Sharma",   Initials="PS", Email="priya.sharma@nexus.io",   Department="Engineering", Role="Employee", Title="Frontend Developer",  Salary=480000,  Joined=new DateTime(2023,1,8),  Phone="+27 76 789 0123", ManagerId=2,    AvatarColor="#8B5CF6" },
        new() { Id=7, Name="James Okafor",   Initials="JO", Email="james.okafor@nexus.io",   Department="HR",          Role="Employee", Title="HR Coordinator",      Salary=360000,  Joined=new DateTime(2022,9,14), Phone="+27 77 890 1234", ManagerId=3,    AvatarColor="#EC4899" },
        new() { Id=8, Name="Claudia Mendes", Initials="CM", Email="claudia.mendes@nexus.io", Department="Finance",     Role="Employee", Title="Accountant",          Salary=450000,  Joined=new DateTime(2021,8,22), Phone="+27 78 901 2345", ManagerId=5,    AvatarColor="#14B8A6" },
    };

    public List<LeaveRequest> LeaveRequests { get; } = new();
    public List<PayrollRecord> PayrollRecords { get; } = new();
    public List<AttendanceRecord> AttendanceRecords { get; } = new();
    public List<PerformanceReview> PerformanceReviews { get; } = new();
    public List<HrMessage> Messages { get; } = new();

    public static readonly Dictionary<string, int> LeaveEntitlements = new()
    {
        ["Annual"]   = 21,
        ["Sick"]     = 10,
        ["Personal"] = 3,
    };

    public DummyData()
    {
        SeedLeave();
        RefreshStatuses();
        SeedPayroll();
        SeedAttendance();
        SeedReviews();
        SeedMessages();
    }

    // ── Seeding ──────────────────────────────────────────────────────────

    private void SeedLeave()
    {
        var t = Today;
        void Add(int emp, string type, DateTime from, DateTime to, string reason, string status, DateTime applied, int? decidedBy = null) =>
            LeaveRequests.Add(new LeaveRequest
            {
                Id = LeaveRequests.Count + 1, EmployeeId = emp, LeaveType = type, FromDate = from, ToDate = to,
                Days = Workdays(from, to), Reason = reason, Status = status, AppliedDate = applied, DecidedById = decidedBy,
            });

        Add(1, "Annual",   OffsetWorkdays(t, 12), OffsetWorkdays(t, 16), "Family holiday",         "Pending",  OffsetWorkdays(t, -2));
        Add(6, "Sick",     OffsetWorkdays(t, -4), OffsetWorkdays(t, -3), "Medical appointment",    "Approved", OffsetWorkdays(t, -4), 2);
        Add(8, "Annual",   OffsetWorkdays(t, -2), OffsetWorkdays(t, 3),  "Annual getaway",         "Approved", OffsetWorkdays(t, -12), 5);
        Add(4, "Personal", OffsetWorkdays(t, 4),  OffsetWorkdays(t, 4),  "Family responsibility",  "Pending",  OffsetWorkdays(t, -1));
        Add(7, "Sick",     OffsetWorkdays(t, -9), OffsetWorkdays(t, -9), "Not feeling well",       "Rejected", OffsetWorkdays(t, -9), 3);
        Add(1, "Annual",   OffsetWorkdays(t, 40), OffsetWorkdays(t, 44), "End of year break",      "Pending",  OffsetWorkdays(t, -3));
        Add(2, "Annual",   OffsetWorkdays(t, 25), OffsetWorkdays(t, 29), "Conference and travel",  "Pending",  OffsetWorkdays(t, -5));
    }

    private void SeedPayroll()
    {
        foreach (var e in Employees)
            PayrollRecords.Add(PayrollCalculator.Calculate(e, CurrentMonth, paid: e.Id <= 5));
    }

    private void SeedAttendance()
    {
        var days    = Enumerable.Range(-4, 5).Select(i => OffsetWorkdays(Today, i)).ToArray();
        var ciTimes = new[] { "08:52", "08:47", "09:03", "08:58", "09:11", "08:44", "09:06", "08:55" };
        var coTimes = new[] { "17:05", "17:18", "17:02", "17:10", "16:59", "17:14", "17:07", "17:22" };

        foreach (var (e, ei) in Employees.Select((e, i) => (e, i)))
        foreach (var (d, di) in days.Select((d, i) => (d, i)))
        {
            var rec = new AttendanceRecord { EmployeeId = e.Id, Date = d };
            if (IsOnApprovedLeave(e.Id, d))
            {
                rec.Status = "On Leave";
            }
            else if (d == Today && e.Id == 1)
            {
                rec.Status = "Not Clocked In";   // leaves the demo employee free to clock in
            }
            else if (d == Today)
            {
                rec.ClockIn = ciTimes[(ei * 2 + di) % 8];
                rec.Status  = "In Progress";
            }
            else
            {
                rec.ClockIn  = ciTimes[(ei * 2 + di) % 8];
                rec.ClockOut = coTimes[(ei * 2 + di) % 8];
                rec.Status   = "Present";
            }
            AttendanceRecords.Add(rec);
        }
    }

    private void SeedReviews()
    {
        var prev   = Today.AddMonths(-3);
        var period = $"Q{(prev.Month - 1) / 3 + 1} {prev.Year}";
        var when   = Today.AddDays(-20);

        void Add(int emp, int reviewer, int q, int c, int t, int i, string notes) =>
            PerformanceReviews.Add(new PerformanceReview
            {
                Id = PerformanceReviews.Count + 1, EmployeeId = emp, ReviewerId = reviewer, Period = period,
                QualityRating = q, CommunicationRating = c, TeamworkRating = t, InitiativeRating = i,
                OverallRating = (int)Math.Round((q + c + t + i) / 4.0, MidpointRounding.AwayFromZero), Notes = notes,
                ReviewDate = when.AddDays(PerformanceReviews.Count),
            });

        Add(1, 2, 4, 5, 4, 4, "Consistent high-quality delivery. Excellent team player throughout the quarter.");
        Add(6, 2, 3, 3, 4, 3, "Good first full year. Documentation practices need improvement before the next review cycle.");
        Add(4, 5, 5, 4, 5, 5, "Exceptional performance. Financial modelling work directly saved the company significant time and resources.");
        Add(7, 3, 4, 4, 4, 3, "Great initiative shown in organising HR events. Recommended for consideration in the next promotion cycle.");
        Add(8, 5, 3, 3, 3, 3, "Performance was impacted by personal challenges this quarter. A support and improvement plan is now in place.");
    }

    private void SeedMessages()
    {
        var now = DateTime.Now;
        HrMessage M(int from, int? to, string subject, string body, TimeSpan ago, params int[] readBy) => new()
        {
            Id = Messages.Count + 1, FromId = from, ToId = to, Subject = subject, Body = body,
            SentAt = now - ago, ReadBy = readBy.ToList(),
        };

        Messages.Add(M(3, null, "Company Update and Town Hall",
            "Hi team,\n\nThank you for a strong quarter. Headcount has grown to 8 and we are on track to hit our hiring targets. A town hall is scheduled for next Thursday at 10:00. Please block your calendars.\n\nBest,\nAmara Osei\nHR Manager",
            TimeSpan.FromDays(2), 2, 3, 5));
        Messages.Add(M(3, 1, "Leave Request Received",
            "Hi Sarah,\n\nYour annual leave request has been received and is currently under review. You will be notified once a decision has been made.\n\nKind regards,\nAmara",
            TimeSpan.FromDays(2).Add(TimeSpan.FromHours(3)), 3));
        Messages.Add(M(3, null, "Updated Hybrid Work Policy",
            "Dear all,\n\nFrom the first of next month the company moves to a hybrid model: 3 days in the office (Mon, Tue, Thu) and 2 days remote. Please review the updated policy document on the shared drive.\n\nFor questions, contact HR.\n\nAmara",
            TimeSpan.FromDays(5), 3));
        Messages.Add(M(3, 6, "Mid-Year Check-In Reminder",
            "Hi Priya,\n\nYour mid-year check-in with Michael is coming up. Please prepare a brief self-assessment covering your key achievements, challenges and goals for the next quarter.\n\nLet me know if you have any questions.\n\nAmara",
            TimeSpan.FromDays(1), 3));
        Messages.Add(M(1, 3, "Question about my leave balance",
            "Hi Amara,\n\nCould you confirm how many annual leave days I have left for this year? I am planning a trip before the end of the year and want to make sure I am within my balance.\n\nThanks,\nSarah",
            TimeSpan.FromHours(5), 1));
    }

    // ── Dates ────────────────────────────────────────────────────────────

    public static bool IsWeekend(DateTime d) => d.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

    /// <summary>Working days (Mon to Fri) in the inclusive range.</summary>
    public static int Workdays(DateTime from, DateTime to)
    {
        var n = 0;
        for (var d = from.Date; d <= to.Date; d = d.AddDays(1))
            if (!IsWeekend(d)) n++;
        return n;
    }

    /// <summary>Moves a date forward or back by whole working days.</summary>
    public static DateTime OffsetWorkdays(DateTime from, int days)
    {
        var step = days < 0 ? -1 : 1;
        var d = from.Date;
        for (var left = Math.Abs(days); left > 0;)
        {
            d = d.AddDays(step);
            if (!IsWeekend(d)) left--;
        }
        return d;
    }

    // ── Lookups ──────────────────────────────────────────────────────────

    public Employee? GetEmployee(int id) => Employees.FirstOrDefault(e => e.Id == id);

    public Employee? FindByEmail(string email) =>
        Employees.FirstOrDefault(e => e.Email.Equals(email.Trim(), StringComparison.OrdinalIgnoreCase));

    public IEnumerable<Employee> ActiveEmployees => Employees.Where(e => !e.IsOffboarded);

    public IEnumerable<Employee> DirectReports(int managerId) =>
        ActiveEmployees.Where(e => e.ManagerId == managerId);

    /// <summary>Demo passwords are one per role. Real authentication is out of scope for the demo.</summary>
    public static string DemoPassword(string role) => role switch
    {
        "HR"      => "HRAdmin123!",
        "Manager" => "Manager123!",
        _         => "Employee123!",
    };

    public bool IsOnApprovedLeave(int employeeId, DateTime date) =>
        LeaveRequests.Any(l => l.EmployeeId == employeeId && l.Status == "Approved" &&
                               date.Date >= l.FromDate.Date && date.Date <= l.ToDate.Date);

    /// <summary>Keeps Active / On Leave in step with approved leave covering today.</summary>
    public void RefreshStatuses()
    {
        foreach (var e in Employees.Where(e => !e.IsOffboarded))
            e.Status = IsOnApprovedLeave(e.Id, Today) ? "On Leave" : "Active";
    }

    // ── Employees ────────────────────────────────────────────────────────

    public Employee AddEmployee(Employee e)
    {
        e.Id = Employees.Max(x => x.Id) + 1;
        Employees.Add(e);
        PayrollRecords.Add(PayrollCalculator.Calculate(e, CurrentMonth));
        AttendanceRecords.Add(new AttendanceRecord { EmployeeId = e.Id, Date = Today, Status = "Not Clocked In" });
        return e;
    }

    // ── Leave ────────────────────────────────────────────────────────────

    public IEnumerable<LeaveRequest> VisibleLeave(Employee user) => user.Role switch
    {
        "HR"      => LeaveRequests,
        "Manager" => LeaveRequests.Where(l => l.EmployeeId == user.Id || GetEmployee(l.EmployeeId)?.ManagerId == user.Id),
        _         => LeaveRequests.Where(l => l.EmployeeId == user.Id),
    };

    /// <summary>HR can decide anyone's request but their own; a manager can decide their direct reports'.</summary>
    public bool CanDecide(Employee approver, LeaveRequest req)
    {
        if (req.Status != "Pending" || req.EmployeeId == approver.Id) return false;
        if (approver.Role == "HR") return true;
        return approver.Role == "Manager" && GetEmployee(req.EmployeeId)?.ManagerId == approver.Id;
    }

    public int PendingFor(Employee user) => LeaveRequests.Count(l => CanDecide(user, l));

    public List<LeaveBalance> BalancesFor(int employeeId)
    {
        var year = Today.Year;
        var mine = LeaveRequests.Where(l => l.EmployeeId == employeeId && l.FromDate.Year == year).ToList();
        return LeaveEntitlements.Select(kv => new LeaveBalance(
            kv.Key, kv.Value,
            mine.Where(l => l.LeaveType == kv.Key && l.Status == "Approved").Sum(l => l.Days),
            mine.Where(l => l.LeaveType == kv.Key && l.Status == "Pending").Sum(l => l.Days))).ToList();
    }

    // ── Payroll / attendance / reviews ───────────────────────────────────

    public IEnumerable<PayrollRecord> VisiblePayroll(Employee user) =>
        user.Role == "HR" ? PayrollRecords : PayrollRecords.Where(p => p.EmployeeId == user.Id);

    public IEnumerable<AttendanceRecord> VisibleAttendance(Employee user) => user.Role switch
    {
        "HR"      => AttendanceRecords,
        "Manager" => AttendanceRecords.Where(a => a.EmployeeId == user.Id || GetEmployee(a.EmployeeId)?.ManagerId == user.Id),
        _         => AttendanceRecords.Where(a => a.EmployeeId == user.Id),
    };

    public IEnumerable<PerformanceReview> VisibleReviews(Employee user) => user.Role switch
    {
        "HR"      => PerformanceReviews,
        "Manager" => PerformanceReviews.Where(r => r.EmployeeId == user.Id || GetEmployee(r.EmployeeId)?.ManagerId == user.Id),
        _         => PerformanceReviews.Where(r => r.EmployeeId == user.Id),
    };

    public IEnumerable<Employee> ReviewableBy(Employee user) => user.Role switch
    {
        "HR"      => ActiveEmployees.Where(e => e.Id != user.Id),
        "Manager" => DirectReports(user.Id),
        _         => Enumerable.Empty<Employee>(),
    };

    // ── Messages ─────────────────────────────────────────────────────────

    public bool IsInbox(HrMessage m, int userId) => m.FromId != userId && (m.ToId == null || m.ToId == userId);

    public int UnreadCount(int userId) =>
        Messages.Count(m => IsInbox(m, userId) && !m.ReadBy.Contains(userId));
}
