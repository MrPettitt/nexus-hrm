namespace NexusHRM.Models;

public class Employee
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Initials { get; set; } = "";
    public string Email { get; set; } = "";
    public string Department { get; set; } = "";
    public string Role { get; set; } = "Employee"; // "Manager" | "HR" | "Employee"
    public string Title { get; set; } = "";
    public decimal Salary { get; set; }
    public DateTime Joined { get; set; }
    public string Status { get; set; } = "Active"; // "Active" | "On Leave" | "Offboarded"
    public string Phone { get; set; } = "";
    public int? ManagerId { get; set; }
    public string AvatarColor { get; set; } = "#5B5BD6";

    public bool IsOffboarded => Status == "Offboarded";
}
