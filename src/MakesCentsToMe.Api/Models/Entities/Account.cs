namespace MakesCentsToMe.Api.Models.Entities;

public class Account
{
    public AccountType AccountType { get; set; }
    public Guid Id { get; set; }
    public ImportProfile? ImportProfile { get; set; }
    public Institution Institution { get; set; } = null!;
    public Guid InstitutionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
