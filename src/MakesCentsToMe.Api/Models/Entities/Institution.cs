namespace MakesCentsToMe.Api.Models.Entities;

public class Institution
{
    public ICollection<Account> Accounts { get; set; } = new List<Account>();
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
