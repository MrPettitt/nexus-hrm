using Microsoft.AspNetCore.Mvc;
using NexusHRM.Data;
using NexusHRM.Infrastructure;
using NexusHRM.Models;

namespace NexusHRM.Pages.Messages;

public class IndexModel : AppPageModel
{
    public List<HrMessage> Messages          { get; private set; } = new();
    public List<Employee>  ComposeRecipients { get; private set; } = new();
    public int     CurrentUserId => UserId;
    public int?    OpenMessageId { get; private set; }
    public string  ActiveTab     { get; private set; } = "inbox";
    public int     UnreadCount   { get; private set; }

    public IndexModel(DummyData data) : base(data) { }

    public IActionResult OnGet(string? tab, int? open)
    {
        OpenMessageId = open;
        ActiveTab     = tab is "sent" or "all" ? tab : "inbox";
        if (ActiveTab == "all" && !IsHR) ActiveTab = "inbox";

        Messages = ActiveTab switch
        {
            "sent" => Data.Messages.Where(m => m.FromId == UserId).OrderByDescending(m => m.SentAt).ToList(),
            "all"  => Data.Messages.OrderByDescending(m => m.SentAt).ToList(),
            _      => Data.Messages.Where(m => Data.IsInbox(m, UserId)).OrderByDescending(m => m.SentAt).ToList(),
        };

        UnreadCount = Data.UnreadCount(UserId);

        // HR can write to anyone; everyone else writes to HR only.
        ComposeRecipients = IsHR
            ? Data.ActiveEmployees.Where(e => e.Id != UserId).OrderBy(e => e.Name).ToList()
            : Data.ActiveEmployees.Where(e => e.Role == "HR").OrderBy(e => e.Name).ToList();

        // Opening a message from a notification marks it as read, if it is in this user's inbox.
        if (open.HasValue)
        {
            var m = Data.Messages.FirstOrDefault(x => x.Id == open && Data.IsInbox(x, UserId));
            if (m != null && !m.ReadBy.Contains(UserId))
            {
                m.ReadBy.Add(UserId);
                UnreadCount = Data.UnreadCount(UserId);
            }
        }

        return Page();
    }

    public IActionResult OnPost(string action, int? messageId, int? toId, string? subject, string? body)
    {
        switch (action)
        {
            case "send":
                return Send(toId, subject, body);

            case "markRead" when messageId.HasValue:
                var read = Data.Messages.FirstOrDefault(m => m.Id == messageId && Data.IsInbox(m, UserId));
                if (read != null && !read.ReadBy.Contains(UserId)) read.ReadBy.Add(UserId);
                break;

            case "markAllRead":
                foreach (var m in Data.Messages.Where(m => Data.IsInbox(m, UserId) && !m.ReadBy.Contains(UserId)))
                    m.ReadBy.Add(UserId);
                Flash("All messages marked as read.");
                break;

            case "delete" when messageId.HasValue:
                if (!IsHR) return Forbidden("Only HR can delete messages.");
                Data.Messages.RemoveAll(m => m.Id == messageId);
                Flash("Message deleted.");
                break;
        }

        return RedirectToPage();
    }

    private IActionResult Send(int? toId, string? subject, string? body)
    {
        if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(body))
            return Forbidden("A subject and a message are both required.");

        int? recipient;
        if (IsHR)
        {
            recipient = toId is null or 0 ? null : toId;
            if (recipient.HasValue && Data.ActiveEmployees.All(e => e.Id != recipient))
                return Forbidden("That recipient could not be found.");
        }
        else
        {
            var hr = Data.ActiveEmployees.Where(e => e.Role == "HR").ToList();
            recipient = hr.FirstOrDefault(e => e.Id == toId)?.Id ?? hr.FirstOrDefault()?.Id;
            if (recipient == null) return Forbidden("No HR contact is available right now.");
        }

        Data.Messages.Add(new HrMessage
        {
            Id      = Data.Messages.Any() ? Data.Messages.Max(m => m.Id) + 1 : 1,
            FromId  = UserId,
            ToId    = recipient,
            Subject = subject.Trim(),
            Body    = body.Trim(),
            SentAt  = DateTime.Now,
            ReadBy  = new List<int> { UserId },
        });

        Flash("Message sent.");
        return RedirectToPage(new { tab = "sent" });
    }
}
