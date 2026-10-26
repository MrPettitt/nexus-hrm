using NexusHRM.Models;

namespace NexusHRM.Data;

/// <summary>
/// South African payroll maths for the demo (2025/26 tax year, under 65, no medical credits).
/// Illustrative only: not a substitute for a registered payroll engine.
/// </summary>
public static class PayrollCalculator
{
    public const decimal AllowanceRate   = 0.08m;
    public const decimal UifRate         = 0.01m;
    public const decimal UifMonthlyCeiling = 17712m;
    public const decimal PrimaryRebate   = 17235m;

    // (lower bound, base tax at lower bound, marginal rate)
    private static readonly (decimal From, decimal Base, decimal Rate)[] Brackets =
    {
        (0m,         0m,         0.18m),
        (237_100m,   42_678m,    0.26m),
        (370_500m,   77_362m,    0.31m),
        (512_800m,   121_475m,   0.36m),
        (673_000m,   179_147m,   0.39m),
        (857_900m,   251_258m,   0.41m),
        (1_817_000m, 644_489m,   0.45m),
    };

    public static decimal AnnualTax(decimal annualTaxableIncome)
    {
        var b = Brackets.Last(x => annualTaxableIncome >= x.From);
        var tax = b.Base + (annualTaxableIncome - b.From) * b.Rate - PrimaryRebate;
        return Math.Max(0m, tax);
    }

    public static PayrollRecord Calculate(Employee e, string month, bool paid = false)
    {
        var basic = Math.Round(e.Salary / 12m, 2);
        var allow = Math.Round(basic * AllowanceRate, 2);
        var gross = basic + allow;
        var uif   = Math.Round(Math.Min(gross, UifMonthlyCeiling) * UifRate, 2);
        var paye  = Math.Round(AnnualTax(gross * 12m) / 12m, 2);

        return new PayrollRecord
        {
            EmployeeId  = e.Id,
            Month       = month,
            BasicSalary = basic,
            Allowances  = allow,
            UIF         = uif,
            PAYE        = paye,
            NetPay      = gross - uif - paye,
            IsPaid      = paid,
        };
    }
}
