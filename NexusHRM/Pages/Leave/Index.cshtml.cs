using Microsoft.AspNetCore.Mvc;
using NexusHRM.Data;
using NexusHRM.Infrastructure;
using NexusHRM.Models;

namespace NexusHRM.Pages.Leave;

public class IndexModel : AppPageModel
{
    private static readonly string[] LeaveTypes = { "Annual", "Sick", "Personal", "Unpaid" };

    public List<LeaveRequest> Leave    { get; private set; } = new();
    public List<LeaveRequest> AllLeave { get; private set; } = new();
    public List<LeaveBalance> Balances { get; private set; } = new();
    public string Filter { get; private set; } = "All";
    public bool   ShowActions { get; private set; }

    public IndexModel(DummyData data) : base(data) { }

    public IActionResult OnGet(string? filter)
    {
        Filter   = filter is "Pending" or "Approved" or "Rejected" ? filter : "All";
        AllLeave = Data.VisibleLeave(Me).OrderByDescending(l => l.Status == "Pending").ThenByDescending(l => l.AppliedDate).ThenByDescending(l => l.Id).ToList();
        Leave    = Filter == "All" ? AllLeave : AllLeave.Where(l => l.Status == Filter).ToList();
        Balances = Data.BalancesFor(UserId);
        ShowActions = AllLeave.Any(l => Data.CanDecide(Me, l));
        return Page();
    }

    public bool CanDecide(LeaveRequest l) => Data.CanDecide(Me, l);

    public IActionResult OnPost(int? leaveId, string? action, string? leaveType, DateTime? fromDate, DateTime? toDate, string? reason)
    {
        if (action is "approve" or "reject" && leaveId.HasValue)
        {
            var req = Data.LeaveRequests.FirstOrDefault(l => l.Id == leaveId);
            if (req == null) return Forbidden("That leave request no longer exists.");
            if (!Data.CanDecide(Me, req)) return Forbidden("You cannot decide this leave request.");

            req.Status      = action == "approve" ? "Approved" : "Rejected";
            req.DecidedById = UserId;
            var who = Data.GetEmployee(req.EmployeeId)?.Name ?? "Employee";
            Flash($"Leave for {who} was {req.Status.ToLower()}.");
            return RedirectToPage();
        }

        if (action == "apply")
            return Apply(leaveType, fromDate, toDate, reason);

        return RedirectToPage();
    }

    private IActionResult Apply(string? leaveType, DateTime? fromDate, DateTime? toDate, string? reason)
    {
        leaveType = LeaveTypes.Contains(leaveType) ? leaveType! : "Annual";

        if (!fromDate.HasValue || !toDate.HasValue || string.IsNullOrWhiteSpace(reason))
            return Forbidden("Please complete every field.");

        var from = fromDate.Value.Date;
        var to   = toDate.Value.Date;
        if (to < from)
            return Forbidden("The end date cannot be before the start date.");
        if (from < Data.Today.AddDays(-30))
            return Forbidden("Leave cannot be backdated by more than 30 days.");

        var days = DummyData.Workdays(from, to);
        if (days == 0)
            return Forbidden("The selected dates fall on a weekend. Please pick working days.");

        var clash = Data.LeaveRequests.Any(l => l.EmployeeId == UserId && l.Status != "Rejected" &&
                                                from <= l.ToDate.Date && to >= l.FromDate.Date);
        if (clash)
            return Forbidden("You already have a leave request that overlaps those dates.");

        var balance = Data.BalancesFor(UserId).FirstOrDefault(b => b.Type == leaveType);
        if (balance != null && days > balance.Remaining)
            return Forbidden($"You only have {balance.Remaining} {leaveType.ToLower()} leave day(s) remaining.");

        Data.LeaveRequests.Add(new LeaveRequest
        {
            Id          = Data.LeaveRequests.Max(l => l.Id) + 1,
            EmployeeId  = UserId,
            LeaveType   = leaveType,
            FromDate    = from,
            ToDate      = to,
            Days        = days,
            Reason      = reason.Trim(),
            Status      = "Pending",
            AppliedDate = DateTime.Today,
        });

        Flash($"Leave request submitted for {days} working day(s).");
        return RedirectToPage();
    }
}
