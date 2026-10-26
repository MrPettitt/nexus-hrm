using Microsoft.AspNetCore.Mvc;
using NexusHRM.Data;
using NexusHRM.Infrastructure;
using NexusHRM.Models;

namespace NexusHRM.Pages.Performance;

public class IndexModel : AppPageModel
{
    public List<PerformanceReview> Reviews             { get; private set; } = new();
    public List<Employee>          ReviewableEmployees { get; private set; } = new();
    public string DefaultPeriod { get; private set; } = "";
    public bool   CanReview => IsHR || IsManager;

    public IndexModel(DummyData data) : base(data) { }

    public IActionResult OnGet()
    {
        Reviews = Data.VisibleReviews(Me).OrderByDescending(r => r.ReviewDate).ToList();
        ReviewableEmployees = Data.ReviewableBy(Me).OrderBy(e => e.Name).ToList();
        DefaultPeriod = $"Q{(Data.Today.Month - 1) / 3 + 1} {Data.Today.Year}";
        return Page();
    }

    public IActionResult OnPost(
        string  action,
        int?    employeeId,
        string? period,
        int     qualityRating,
        int     communicationRating,
        int     teamworkRating,
        int     initiativeRating,
        string? notes)
    {
        if (!CanReview)
            return Forbidden("Only managers and HR can write performance reviews.");

        if (action != "add" || !employeeId.HasValue || string.IsNullOrWhiteSpace(period) || string.IsNullOrWhiteSpace(notes))
            return Forbidden("Please complete every field.");

        if (Data.ReviewableBy(Me).All(e => e.Id != employeeId))
            return Forbidden("You can only review your own direct reports.");

        var q = Math.Clamp(qualityRating, 1, 5);
        var c = Math.Clamp(communicationRating, 1, 5);
        var t = Math.Clamp(teamworkRating, 1, 5);
        var i = Math.Clamp(initiativeRating, 1, 5);

        Data.PerformanceReviews.Add(new PerformanceReview
        {
            Id                  = Data.PerformanceReviews.Any() ? Data.PerformanceReviews.Max(r => r.Id) + 1 : 1,
            EmployeeId          = employeeId.Value,
            ReviewerId          = UserId,
            Period              = period.Trim(),
            OverallRating       = (int)Math.Round((q + c + t + i) / 4.0, MidpointRounding.AwayFromZero),
            QualityRating       = q,
            CommunicationRating = c,
            TeamworkRating      = t,
            InitiativeRating    = i,
            Notes               = notes.Trim(),
            ReviewDate          = DateTime.Today,
        });

        Flash("Performance review saved.");
        return RedirectToPage();
    }
}
