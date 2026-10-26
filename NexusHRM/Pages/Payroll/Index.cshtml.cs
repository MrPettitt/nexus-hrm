using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using NexusHRM.Data;
using NexusHRM.Infrastructure;
using NexusHRM.Models;

namespace NexusHRM.Pages.Payroll;

public class IndexModel : AppPageModel
{
    public List<PayrollRecord> Payroll { get; private set; } = new();
    public string  Month           { get; private set; } = "";
    public decimal TotalGross      { get; private set; }
    public decimal TotalDeductions { get; private set; }
    public decimal TotalNet        { get; private set; }
    public int     PaidCount       { get; private set; }

    public IndexModel(DummyData data) : base(data) { }

    public IActionResult OnGet(string? export)
    {
        Month   = Data.CurrentMonth;
        Payroll = Data.VisiblePayroll(Me).ToList();

        TotalGross      = Payroll.Sum(p => p.Gross);
        TotalDeductions = Payroll.Sum(p => p.TotalDeductions);
        TotalNet        = Payroll.Sum(p => p.NetPay);
        PaidCount       = Payroll.Count(p => p.IsPaid);

        if (export == "xlsx")
            return IsHR ? BuildXlsxDownload() : Forbidden("Only HR can export payroll.");

        return Page();
    }

    public IActionResult OnPost(string? action, int? employeeId)
    {
        if (!IsHR)
            return Forbidden("Only HR can process payroll.");

        if (action == "runAll")
        {
            var due = Data.PayrollRecords.Where(p => !p.IsPaid).ToList();
            due.ForEach(p => p.IsPaid = true);
            Flash(due.Count == 0 ? "Everyone has already been paid." : $"Payroll processed for {due.Count} employee(s).");
        }
        else if (action == "payOne" && employeeId.HasValue)
        {
            var rec = Data.PayrollRecords.FirstOrDefault(p => p.EmployeeId == employeeId);
            if (rec != null && !rec.IsPaid)
            {
                rec.IsPaid = true;
                Flash($"Payment released to {Data.GetEmployee(rec.EmployeeId)?.Name}.");
            }
        }

        return RedirectToPage();
    }

    // ── Excel export ──────────────────────────────────────────────────────
    private FileContentResult BuildXlsxDownload()
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add($"Payroll {Month}");

        // ── Title block ───────────────────────────────────────────────────
        ws.Cell("A1").Value = "Nexus HRM Payroll Report";
        ws.Cell("A1").Style.Font.Bold      = true;
        ws.Cell("A1").Style.Font.FontSize  = 14;
        ws.Cell("A1").Style.Font.FontColor = XLColor.FromHtml("#5B5BD6");
        ws.Range("A1:J1").Merge();

        ws.Cell("A2").Value = $"Pay Period: {Month}     Generated: {DateTime.Now:d MMM yyyy HH:mm}";
        ws.Cell("A2").Style.Font.FontSize  = 10;
        ws.Cell("A2").Style.Font.FontColor = XLColor.FromHtml("#6B7280");
        ws.Range("A2:J2").Merge();

        ws.Row(3).Height = 6; // spacer

        // ── Header row ────────────────────────────────────────────────────
        var headers = new[]
        {
            "Employee", "Department", "Title", "Pay Period",
            "Basic Salary", "Allowances", "UIF", "PAYE Tax", "Net Pay", "Status"
        };

        for (int col = 1; col <= headers.Length; col++)
        {
            var cell = ws.Cell(4, col);
            cell.Value                          = headers[col - 1];
            cell.Style.Font.Bold                = true;
            cell.Style.Font.FontColor           = XLColor.White;
            cell.Style.Fill.BackgroundColor     = XLColor.FromHtml("#0D1117");
            cell.Style.Alignment.Horizontal     = XLAlignmentHorizontalValues.Left;
            cell.Style.Border.BottomBorder      = XLBorderStyleValues.Thin;
            cell.Style.Border.BottomBorderColor = XLColor.FromHtml("#5B5BD6");
        }

        // ── Data rows ─────────────────────────────────────────────────────
        var currencyFmt = "\"R \"#,##0.00";
        var altFill     = XLColor.FromHtml("#F7F8FA");
        var paidColor   = XLColor.FromHtml("#D1FAE5");
        var pendColor   = XLColor.FromHtml("#FEF3C7");

        int row = 5;
        foreach (var p in Data.PayrollRecords)
        {
            var emp = Data.GetEmployee(p.EmployeeId);
            if (emp == null) continue;

            var isAlt = (row % 2 == 0);

            void StyleCell(IXLCell c, bool isNumeric = false)
            {
                c.Style.Fill.BackgroundColor    = isAlt ? altFill : XLColor.White;
                c.Style.Border.BottomBorder     = XLBorderStyleValues.Hair;
                c.Style.Border.BottomBorderColor= XLColor.FromHtml("#E4E7ED");
                if (isNumeric) c.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            }

            ws.Cell(row, 1).Value  = emp.Name;
            ws.Cell(row, 2).Value  = emp.Department;
            ws.Cell(row, 3).Value  = emp.Title;
            ws.Cell(row, 4).Value  = p.Month;
            ws.Cell(row, 5).Value  = (double)p.BasicSalary;
            ws.Cell(row, 6).Value  = (double)p.Allowances;
            ws.Cell(row, 7).Value  = (double)p.UIF;
            ws.Cell(row, 8).Value  = (double)p.PAYE;
            ws.Cell(row, 9).Value  = (double)p.NetPay;
            ws.Cell(row, 10).Value = p.IsPaid ? "Paid" : "Pending";

            // Apply currency format to salary columns
            foreach (int c in new[] { 5, 6, 7, 8, 9 })
                ws.Cell(row, c).Style.NumberFormat.Format = currencyFmt;

            // Colour the Status cell
            var statusCell = ws.Cell(row, 10);
            statusCell.Style.Fill.BackgroundColor = p.IsPaid ? paidColor : pendColor;
            statusCell.Style.Font.FontColor       = p.IsPaid
                ? XLColor.FromHtml("#065F46")
                : XLColor.FromHtml("#92400E");
            statusCell.Style.Font.Bold = true;
            statusCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            for (int c = 1; c <= 9; c++) StyleCell(ws.Cell(row, c), c >= 5);

            row++;
        }

        // ── Totals row ────────────────────────────────────────────────────
        int lastData = row - 1;
        ws.Cell(row, 1).Value = "TOTAL";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Range(row, 1, row, 4).Merge();

        foreach (int c in new[] { 5, 6, 7, 8, 9 })
        {
            ws.Cell(row, c).FormulaA1             = $"=SUM({ws.Cell(5, c).Address}:{ws.Cell(lastData, c).Address})";
            ws.Cell(row, c).Style.NumberFormat.Format = currencyFmt;
            ws.Cell(row, c).Style.Font.Bold       = true;
            ws.Cell(row, c).Style.Fill.BackgroundColor = XLColor.FromHtml("#EEEEFF");
            ws.Cell(row, c).Style.Font.FontColor  = XLColor.FromHtml("#5B5BD6");
            ws.Cell(row, c).Style.Border.TopBorder = XLBorderStyleValues.Medium;
            ws.Cell(row, c).Style.Border.TopBorderColor = XLColor.FromHtml("#5B5BD6");
            ws.Cell(row, c).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        }

        ws.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#EEEEFF");
        ws.Cell(row, 1).Style.Font.FontColor       = XLColor.FromHtml("#5B5BD6");
        ws.Cell(row, 1).Style.Border.TopBorder     = XLBorderStyleValues.Medium;
        ws.Cell(row, 1).Style.Border.TopBorderColor= XLColor.FromHtml("#5B5BD6");

        // ── Column widths ─────────────────────────────────────────────────
        ws.Column(1).Width  = 24;  // Employee
        ws.Column(2).Width  = 16;  // Department
        ws.Column(3).Width  = 24;  // Title
        ws.Column(4).Width  = 16;  // Pay Period
        ws.Column(5).Width  = 18;  // Basic
        ws.Column(6).Width  = 16;  // Allowances
        ws.Column(7).Width  = 12;  // UIF
        ws.Column(8).Width  = 14;  // PAYE
        ws.Column(9).Width  = 18;  // Net Pay
        ws.Column(10).Width = 12;  // Status

        // Freeze header row so it stays visible when scrolling
        ws.SheetView.FreezeRows(4);

        // ── Convert to bytes and return ───────────────────────────────────
        using var stream = new MemoryStream();
        wb.SaveAs(stream);
        var fileName = $"NexusHRM_Payroll_{Month.Replace(" ", "")}_{DateTime.Now:yyyyMMdd}.xlsx";
        return File(stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }

}
