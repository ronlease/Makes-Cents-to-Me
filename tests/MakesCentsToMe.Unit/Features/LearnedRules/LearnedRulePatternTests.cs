// Feature: Learned Rule Pattern Derivation and Matching
//
// Scenario: Derive strips store numbers, reference codes and trailing punctuation
//   Given a raw vendor description
//   When Derive is called
//   Then the stable leading portion of the description is returned in upper case
//
// Scenario: Derive falls back to the first token when the leading portion is too short
//   Given a description whose text before the first digit is shorter than three characters
//   When Derive is called
//   Then the first token is returned
//
// Scenario: Derive returns the whole normalized description when no token is long enough
//   Given a description whose first token is shorter than three characters
//   When Derive is called
//   Then the whole normalized description is returned
//
// Scenario: Pattern matches at a word boundary only
//   Given the pattern "WHOLEFDS MKT"
//   When it is matched against "WHOLEFDS MKTPLACE"
//   Then it does not match
//
// Scenario: Matching ignores case and repeated whitespace
//   Given the pattern "WHOLEFDS MKT"
//   When it is matched against "wholefds   mkt 10245"
//   Then it matches
//
// Scenario: Pattern in the middle of a description does not match
//   Given the pattern "WHOLEFDS MKT"
//   When it is matched against "THE WHOLEFDS MKT 10245"
//   Then it does not match
//
// Scenario: Longest matching pattern wins
//   Given two rules whose patterns both match a description
//   When FindBestMatch is called
//   Then the rule with the longer pattern is returned
//
// Scenario: Equal length patterns are resolved by most recent update
//   Given two rules with the same pattern length that both match a description
//   When FindBestMatch is called
//   Then the most recently updated rule is returned
//
// Scenario: No rule matches
//   Given rules that do not match a description
//   When FindBestMatch is called
//   Then null is returned

using FluentAssertions;
using MakesCentsToMe.Api.Features.LearnedRules;
using MakesCentsToMe.Api.Models.Entities;

namespace MakesCentsToMe.Unit.Features.LearnedRules;

public class LearnedRulePatternTests
{
    [Fact]
    public void Derive_FirstTokenShorterThanMinimum_ReturnsWholeNormalizedDescription()
    {
        // Arrange
        var description = "ab 12345 foo";

        // Act
        var result = LearnedRulePattern.Derive(description);

        // Assert
        result.Should().Be("AB 12345 FOO");
    }

    [Theory]
    [InlineData("AMAZON.COM*1Z0", "AMAZON.COM")]
    [InlineData("AMZN MKTP US*2K4F7B1G3", "AMZN MKTP US")]
    [InlineData("NETFLIX.COM", "NETFLIX.COM")]
    [InlineData("SQ *BLUE BOTTLE 123", "SQ *BLUE BOTTLE")]
    [InlineData("TST* JOES DINER 0042", "TST* JOES DINER")]
    [InlineData("WAL-MART #1234 SPRINGFIELD IL", "WAL-MART")]
    [InlineData("WHOLEFDS MKT 10245", "WHOLEFDS MKT")]
    [InlineData("7-ELEVEN 12345", "7-ELEVEN")]
    public void Derive_KnownDescription_ReturnsStablePrefix(string description, string expected)
    {
        // Arrange

        // Act
        var result = LearnedRulePattern.Derive(description);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void Derive_LowerCaseDescription_ReturnsUpperCasePattern()
    {
        // Arrange
        var description = "wholefds mkt 10245";

        // Act
        var result = LearnedRulePattern.Derive(description);

        // Assert
        result.Should().Be("WHOLEFDS MKT");
    }

    [Fact]
    public void Derive_PrefixShorterThanMinimum_FallsBackToFirstToken()
    {
        // Arrange
        var description = "A1B STORE 55";

        // Act
        var result = LearnedRulePattern.Derive(description);

        // Assert
        result.Should().Be("A1B");
    }

    [Fact]
    public void FindBestMatch_EqualLengthPatterns_ReturnsMostRecentlyUpdated()
    {
        // Arrange
        var older = CreateRule("AMZN MKTP", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var newer = CreateRule("AMZN MKTP", new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc));

        // Act
        var result = LearnedRulePattern.FindBestMatch([older, newer], "AMZN MKTP US*9X8Y7Z6W5");

        // Assert
        result.Should().BeSameAs(newer);
    }

    [Fact]
    public void FindBestMatch_MultipleMatchingPatterns_ReturnsLongestPattern()
    {
        // Arrange
        var shorter = CreateRule("AMZN", DateTime.UtcNow);
        var longer = CreateRule("AMZN MKTP", DateTime.UtcNow.AddDays(-30));

        // Act
        var result = LearnedRulePattern.FindBestMatch([shorter, longer], "AMZN MKTP US*9X8Y7Z6W5");

        // Assert
        result.Should().BeSameAs(longer);
    }

    [Fact]
    public void FindBestMatch_NoRuleMatches_ReturnsNull()
    {
        // Arrange
        var rule = CreateRule("WHOLEFDS MKT", DateTime.UtcNow);

        // Act
        var result = LearnedRulePattern.FindBestMatch([rule], "TARGET 00012");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Matches_CaseAndWhitespaceDiffer_ReturnsTrue()
    {
        // Arrange
        var pattern = "WHOLEFDS MKT";

        // Act
        var result = LearnedRulePattern.Matches(pattern, "wholefds   mkt 10245");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Matches_DescriptionEqualsPattern_ReturnsTrue()
    {
        // Arrange
        var pattern = "NETFLIX.COM";

        // Act
        var result = LearnedRulePattern.Matches(pattern, "NETFLIX.COM");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Matches_PatternContinuesIntoLongerWord_ReturnsFalse()
    {
        // Arrange
        var pattern = "WHOLEFDS MKT";

        // Act
        var result = LearnedRulePattern.Matches(pattern, "WHOLEFDS MKTPLACE");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Matches_PatternInMiddleOfDescription_ReturnsFalse()
    {
        // Arrange
        var pattern = "WHOLEFDS MKT";

        // Act
        var result = LearnedRulePattern.Matches(pattern, "THE WHOLEFDS MKT 10245");

        // Assert
        result.Should().BeFalse();
    }

    private static LearnedRule CreateRule(string pattern, DateTime updatedAt) =>
        new()
        {
            Id = Guid.NewGuid(),
            NormalizedVendor = "Vendor",
            Pattern = pattern,
            UpdatedAt = updatedAt,
        };
}
