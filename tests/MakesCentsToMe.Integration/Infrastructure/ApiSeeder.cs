using FluentAssertions;
using MakesCentsToMe.Api.Features.Accounts;
using MakesCentsToMe.Api.Features.Categories;
using MakesCentsToMe.Api.Features.Import;
using MakesCentsToMe.Api.Features.Institutions;
using MakesCentsToMe.Api.Features.LearnedRules;
using MakesCentsToMe.Api.Models.Entities;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text;

namespace MakesCentsToMe.Integration.Infrastructure;

/// <summary>
/// Arranges test data through the public API. Each helper asserts the call succeeded so failed setup
/// is reported at the setup line rather than as a confusing assertion later.
/// </summary>
public static class ApiSeeder
{
    public static async Task<Guid> CreateAccountAsync(
        HttpClient client,
        Guid institutionId,
        string name = "Checking",
        AccountType accountType = AccountType.Checking)
    {
        var response = await client.PostAsJsonAsync(
            $"/api/v1/institutions/{institutionId}/accounts",
            new CreateAccountRequest(name, accountType),
            ApiJson.Options);
        return (await ReadCreatedAsync<AccountResponse>(response)).Id;
    }

    /// <summary>Creates an institution, an account, and an import profile (standard by default); returns the account id.</summary>
    public static async Task<Guid> CreateAccountWithProfileAsync(
        HttpClient client,
        string institutionName = "Test Credit Union",
        string accountName = "Checking",
        SaveImportProfileRequest? profile = null)
    {
        var institutionId = await CreateInstitutionAsync(client, institutionName);
        var accountId = await CreateAccountAsync(client, institutionId, accountName);
        await SaveImportProfileAsync(client, accountId, profile);
        return accountId;
    }

    public static async Task<CategoryResponse> CreateCategoryAsync(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync("/api/v1/categories", new CreateCategoryRequest(name), ApiJson.Options);
        return await ReadCreatedAsync<CategoryResponse>(response);
    }

    public static async Task<Guid> CreateInstitutionAsync(HttpClient client, string name = "Test Credit Union")
    {
        var response = await client.PostAsJsonAsync(
            "/api/v1/institutions",
            new CreateInstitutionRequest(name),
            ApiJson.Options);
        return (await ReadCreatedAsync<InstitutionResponse>(response)).Id;
    }

    public static async Task<LearnedRuleResponse> CreateLearnedRuleAsync(
        HttpClient client,
        Guid categoryId,
        string normalizedVendor,
        string pattern)
    {
        var response = await client.PostAsJsonAsync(
            "/api/v1/learned-rules",
            new CreateLearnedRuleRequest(categoryId, normalizedVendor, pattern, null),
            ApiJson.Options);
        return await ReadCreatedAsync<LearnedRuleResponse>(response);
    }

    /// <summary>Looks up a category id (for example a seeded default) by name.</summary>
    public static async Task<Guid> GetCategoryIdAsync(HttpClient client, string name)
    {
        var response = await client.GetAsync("/api/v1/categories");
        response.EnsureSuccessStatusCode();
        var categories = (await ApiJson.ReadApiResponseAsync<List<CategoryResponse>>(response)).Data!;
        return categories.Single(category => category.Name == name).Id;
    }

    /// <summary>Posts the CSV to the process endpoint and returns the raw response (success or failure).</summary>
    public static async Task<HttpResponseMessage> PostProcessImportAsync(
        HttpClient client,
        Guid accountId,
        string csv,
        decimal? openingBalance = null,
        decimal? closingBalance = null)
    {
        using var form = new MultipartFormDataContent();
        form.Add(new ByteArrayContent(Encoding.UTF8.GetBytes(csv)), "file", "import.csv");

        if (openingBalance.HasValue)
        {
            form.Add(new StringContent(openingBalance.Value.ToString(CultureInfo.InvariantCulture)), "openingBalance");
        }

        if (closingBalance.HasValue)
        {
            form.Add(new StringContent(closingBalance.Value.ToString(CultureInfo.InvariantCulture)), "closingBalance");
        }

        return await client.PostAsync($"/api/v1/accounts/{accountId}/import/process", form);
    }

    /// <summary>Processes a CSV, asserts HTTP 200, and returns the counts.</summary>
    public static async Task<ProcessImportResponse> ProcessImportAsync(
        HttpClient client,
        Guid accountId,
        string csv,
        decimal? openingBalance = null,
        decimal? closingBalance = null)
    {
        var response = await PostProcessImportAsync(client, accountId, csv, openingBalance, closingBalance);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return (await ApiJson.ReadApiResponseAsync<ProcessImportResponse>(response)).Data!;
    }

    public static async Task<ImportProfileResponse> SaveImportProfileAsync(
        HttpClient client,
        Guid accountId,
        SaveImportProfileRequest? request = null)
    {
        var response = await client.PostAsJsonAsync(
            $"/api/v1/accounts/{accountId}/import/profile",
            request ?? CsvSamples.StandardProfile,
            ApiJson.Options);
        return await ReadCreatedAsync<ImportProfileResponse>(response);
    }

    private static async Task<T> ReadCreatedAsync<T>(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        return (await ApiJson.ReadApiResponseAsync<T>(response)).Data!;
    }
}
