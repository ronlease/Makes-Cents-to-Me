namespace MakesCentsToMe.Api.Models.Entities;

public class Category
{
    public Guid Id { get; set; }
    public bool IsDefault { get; set; }
    public string Name { get; set; } = string.Empty;
    public ICollection<Transaction> Transactions { get; set; } = [];
}
