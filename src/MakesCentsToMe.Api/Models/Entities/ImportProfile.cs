namespace MakesCentsToMe.Api.Models.Entities;

public class ImportProfile
{
    public Account Account { get; set; } = null!;
    public Guid AccountId { get; set; }
    public AmountType AmountType { get; set; }
    public bool BalanceProvided { get; set; }
    public ICollection<ColumnMapping> ColumnMappings { get; set; } = new List<ColumnMapping>();
    public string DateFormat { get; set; } = string.Empty;
    public Guid Id { get; set; }
}
