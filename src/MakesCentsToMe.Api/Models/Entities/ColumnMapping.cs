namespace MakesCentsToMe.Api.Models.Entities;

public class ColumnMapping
{
    public string ApplicationField { get; set; } = string.Empty;
    public string CsvColumnName { get; set; } = string.Empty;
    public Guid Id { get; set; }
    public Guid ImportProfileId { get; set; }
}
