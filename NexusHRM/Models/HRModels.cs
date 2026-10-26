namespace NexusHRM.Models;

public class LeaveRequest
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string LeaveType { get; set; } = "Annual"; // "Annual" | "Sick" | "Personal" | "Unpaid"
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public int Days { get; set; }
    public string Reason { get; set; } = "";
    public string Status { get; set; } = "Pending"; // "Pending" | "Approved" | "Rejected"
    public DateTime AppliedDate { get; set; }
    public int? DecidedById { get; set; }
}

public class PayrollRecord
{
    public int EmployeeId { get; set; }
    public string Month { get; set; } = "";
    public decimal BasicSalary { get; set; }
    public decimal Allowances { get; set; }
    public decimal UIF { get; set; }
    public decimal PAYE { get; set; }
    public decimal NetPay { get; set; }
    public bool IsPaid { get; set; }

    public decimal Gross => BasicSalary + Allowances;
    public decimal TotalDeductions => UIF + PAYE;
}

public class AttendanceRecord
{
    public int EmployeeId { get; set; }
    public DateTime Date { get; set; }
    public string? ClockIn { get; set; }
    public string? ClockOut { get; set; }
    public string Status { get; set; } = "Present"; // "Present" | "In Progress" | "On Leave" | "Not Clocked In"
}

public class PerformanceReview
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public int ReviewerId { get; set; }
    public string Period { get; set; } = "";
    public int OverallRating { get; set; }
    public int QualityRating { get; set; }
    public int CommunicationRating { get; set; }
    public int TeamworkRating { get; set; }
    public int InitiativeRating { get; set; }
    public string Notes { get; set; } = "";
    public DateTime ReviewDate { get; set; }
}

public class HrMessage
{
    public int    Id       { get; set; }
    public int    FromId   { get; set; }                  // sender employee ID
    public int?   ToId     { get; set; }                  // null = broadcast to all employees
    public string Subject  { get; set; } = "";
    public string Body     { get; set; } = "";
    public DateTime SentAt { get; set; }
    public List<int> ReadBy { get; set; } = new();        // employee IDs who have read it
}

public record LeaveBalance(string Type, int Entitlement, int Used, int Pending)
{
    public int Remaining => Math.Max(0, Entitlement - Used - Pending);
}
