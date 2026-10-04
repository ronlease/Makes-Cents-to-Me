using System.Text;
using MakesCentsToMe.Api.Models.Entities;

namespace MakesCentsToMe.Api.Features.LearnedRules;

public static class LearnedRulePattern
{
    private const int MinimumPatternLength = 3;
    private static readonly char[] TrailingCharacters = [' ', '*', '-', '#', '.', '/'];

    public static string Derive(string description)
    {
        var normalized = Normalize(description);
        var cutIndex = normalized.AsSpan().IndexOfAny("#0123456789");
        var candidate = cutIndex >= 0 ? normalized[..cutIndex] : normalized;
        candidate = candidate.TrimEnd(TrailingCharacters);

        if (candidate.Length >= MinimumPatternLength)
        {
            return candidate;
        }

        var firstSpace = normalized.IndexOf(' ');
        var firstToken = firstSpace >= 0 ? normalized[..firstSpace] : normalized;

        return firstToken.Length >= MinimumPatternLength ? firstToken : normalized;
    }

    public static LearnedRule? FindBestMatch(IEnumerable<LearnedRule> rules, string description)
    {
        var normalizedDescription = Normalize(description);

        return rules
            .Where(rule => MatchesNormalized(rule.Pattern, normalizedDescription))
            .OrderByDescending(rule => rule.Pattern.Length)
            .ThenByDescending(rule => rule.UpdatedAt)
            .FirstOrDefault();
    }

    public static bool Matches(string normalizedPattern, string description) =>
        MatchesNormalized(normalizedPattern, Normalize(description));

    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(value.Length);
        var previousWasWhitespace = false;

        foreach (var character in value.Trim())
        {
            if (char.IsWhiteSpace(character))
            {
                if (!previousWasWhitespace)
                {
                    builder.Append(' ');
                }

                previousWasWhitespace = true;
            }
            else
            {
                builder.Append(char.ToUpperInvariant(character));
                previousWasWhitespace = false;
            }
        }

        return builder.ToString();
    }

    private static bool MatchesNormalized(string normalizedPattern, string normalizedDescription)
    {
        if (normalizedPattern.Length == 0 ||
            !normalizedDescription.StartsWith(normalizedPattern, StringComparison.Ordinal))
        {
            return false;
        }

        return normalizedDescription.Length == normalizedPattern.Length ||
            !char.IsLetterOrDigit(normalizedDescription[normalizedPattern.Length]);
    }
}
