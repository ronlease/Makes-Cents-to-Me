namespace MakesCentsToMe.Api.Models.Entities;

public class LearnedRule
{
    public Category Category { get; set; } = null!;
    public Guid CategoryId { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid Id { get; set; }
    public string NormalizedVendor { get; set; } = string.Empty;
    public string Pattern { get; set; } = string.Empty;
    public Guid? SourceTransactionId { get; set; }
    public ICollection<Transaction> Transactions { get; set; } = [];
    public DateTime UpdatedAt { get; set; }
}
