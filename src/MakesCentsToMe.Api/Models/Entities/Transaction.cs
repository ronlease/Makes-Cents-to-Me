namespace MakesCentsToMe.Api.Models.Entities;

public class Transaction
{
    public Account Account { get; set; } = null!;
    public Guid AccountId { get; set; }
    public decimal Amount { get; set; }
    public decimal? Balance { get; set; }
    public string? Category { get; set; }
    public string? CheckNumber { get; set; }
    public DateTime Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Fees { get; set; }
    public Guid Id { get; set; }
    public decimal Interest { get; set; }
    public decimal Principal { get; set; }
    public string RawCsvRow { get; set; } = string.Empty;
    public Dictionary<string, string> RawData { get; set; } = new();
}
