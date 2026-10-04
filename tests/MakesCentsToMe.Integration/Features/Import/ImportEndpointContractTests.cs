using FluentAssertions;
using MakesCentsToMe.Api.Features.Import;
using MakesCentsToMe.Api.Models.Entities;
using MakesCentsToMe.Integration.Infrastructure;
using System.Net;
using System.Net.Http.Json;
using System.Text;

namespace MakesCentsToMe.Integration.Features.Import;

// Feature: Import endpoint contracts (/api/v1/accounts/{accountId}/import)
//
// Scenario: Upload preview returns headers and at most five rows
//   Given an existing account
//   When a CSV file is uploaded to /upload
//   Then 200 is returned with the headers and up to five preview rows
//   And an empty file or an unknown account returns 400
//
// Scenario: Import profile lifecycle
//   Given an existing account
//   When a profile is created, read, and updated
//   Then POST returns 201 with a Location header, GET returns 200 (404 when absent), PUT returns 200 (404 when absent)
//   And a second POST or an unknown account returns 400
//
// Scenario: Process import
//   Given an account with a profile
//   When a CSV file is posted to /process
//   Then 200 is returned with the counts
//   And an empty file, unknown account, missing profile, or missing balance returns 400
[Collection(IntegrationTestCollection.Name)]
public class ImportEndpointContractTests(IntegrationTestWebApplicationFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task GetProfile_NoProfile_ReturnsNotFound()
    {
        // Arrange
        var institutionId = await ApiSeeder.CreateInstitutionAsync(Client);
        var accountId = await ApiSeeder.CreateAccountAsync(Client, institutionId);

        // Act
        var response = await Client.GetAsync($"/api/v1/accounts/{accountId}/import/profile");
        var body = await ApiJson.ReadApiResponseAsync<ImportProfileResponse>(response);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        body.Success.Should().BeFalse();
        body.Errors.Should().ContainSingle();
    }

    [Fact]
    public async Task GetProfile_ProfileExists_ReturnsOkWithMappingsOrderedByApplicationField()
    {
        // Arrange
        var accountId = await ApiSeeder.CreateAccountWithProfileAsync(Client);

        // Act
        var response = await Client.GetAsync($"/api/v1/accounts/{accountId}/import/profile");
        var body = await response.Content.ReadAsStringAsync();
        using var document = System.Text.Json.JsonDocument.Parse(body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        ApiJson.AssertApiResponseEnvelope(document);
        var data = document.RootElement.GetProperty("data");
        data.GetProperty("accountId").GetGuid().Should().Be(accountId);
        data.GetProperty("amountType").GetString().Should().Be("Single");
        data.GetProperty("balanceProvided").GetBoolean().Should().BeTrue();
        data.GetProperty("dateFormat").GetString().Should().Be("MM/dd/yyyy");
        data.GetProperty("columnMappings").EnumerateArray()
            .Select(mapping => mapping.GetProperty("applicationField").GetString())
            .Should().Equal("Amount", "Balance", "Date", "Description");
    }

    [Fact]
    public async Task ProcessImport_EmptyFile_ReturnsBadRequest()
    {
        // Arrange
        var accountId = await ApiSeeder.CreateAccountWithProfileAsync(Client);

        // Act
        var response = await ApiSeeder.PostProcessImportAsync(Client, accountId, string.Empty);
        var body = await ApiJson.ReadApiResponseAsync<ProcessImportResponse>(response);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        body.Success.Should().BeFalse();
        body.Errors.Should().ContainSingle().Which.Should().Be("No file provided.");
    }

    [Fact]
    public async Task ProcessImport_HeaderOnlyCsv_ReturnsOkWithAllZeros()
    {
        // Arrange
        var accountId = await ApiSeeder.CreateAccountWithProfileAsync(Client);

        // Act
        var response = await ApiSeeder.PostProcessImportAsync(Client, accountId, CsvSamples.HeaderOnly);
        var body = await ApiJson.ReadApiResponseAsync<ProcessImportResponse>(response);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Data.Should().Be(new ProcessImportResponse(0, 0, 0, 0));
    }

    [Fact]
    public async Task ProcessImport_NoBalanceProfileWithoutBalance_ReturnsBadRequest()
    {
        // Arrange
        var accountId = await ApiSeeder.CreateAccountWithProfileAsync(Client, profile: CsvSamples.NoBalanceProfile);

        // Act
        var response = await ApiSeeder.PostProcessImportAsync(Client, accountId, CsvSamples.NoBalanceImport);
        var body = await ApiJson.ReadApiResponseAsync<ProcessImportResponse>(response);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        body.Errors.Should().ContainSingle().Which.Should().Contain("openingBalance or closingBalance");
    }

    [Fact]
    public async Task ProcessImport_NoProfile_ReturnsBadRequest()
    {
        // Arrange
        var institutionId = await ApiSeeder.CreateInstitutionAsync(Client);
        var accountId = await ApiSeeder.CreateAccountAsync(Client, institutionId);

        // Act
        var response = await ApiSeeder.PostProcessImportAsync(Client, accountId, CsvSamples.FirstImport);
        var body = await ApiJson.ReadApiResponseAsync<ProcessImportResponse>(response);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        body.Errors.Should().ContainSingle().Which.Should().Contain("No import profile");
    }

    [Fact]
    public async Task ProcessImport_UnknownAccount_ReturnsBadRequest()
    {
        // Arrange
        var unknownAccountId = Guid.NewGuid();

        // Act
        var response = await ApiSeeder.PostProcessImportAsync(Client, unknownAccountId, CsvSamples.FirstImport);
        var body = await ApiJson.ReadApiResponseAsync<ProcessImportResponse>(response);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        body.Errors.Should().ContainSingle().Which.Should().Contain("not found");
    }

    [Fact]
    public async Task ProcessImport_ValidCsv_ReturnsOkWithCountsEnvelope()
    {
        // Arrange
        var accountId = await ApiSeeder.CreateAccountWithProfileAsync(Client);

        // Act
        var response = await ApiSeeder.PostProcessImportAsync(Client, accountId, CsvSamples.FirstImport);
        using var document = await ApiJson.ReadJsonDocumentAsync(response);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        ApiJson.AssertApiResponseEnvelope(document);
        var data = document.RootElement.GetProperty("data");
        data.GetProperty("transactionsCreated").GetInt32().Should().Be(3);
        data.GetProperty("rowsSkipped").GetInt32().Should().Be(1);
        data.GetProperty("duplicatesSkipped").GetInt32().Should().Be(0);
        data.GetProperty("autoCategorizedCount").GetInt32().Should().Be(0);
        document.RootElement.GetProperty("success").GetBoolean().Should().BeTrue();
        document.RootElement.GetProperty("errors").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task SaveProfile_AlreadyExists_ReturnsBadRequest()
    {
        // Arrange
        var accountId = await ApiSeeder.CreateAccountWithProfileAsync(Client);

        // Act
        var response = await Client.PostAsJsonAsync(
            $"/api/v1/accounts/{accountId}/import/profile",
            CsvSamples.StandardProfile,
            ApiJson.Options);
        var body = await ApiJson.ReadApiResponseAsync<ImportProfileResponse>(response);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        body.Errors.Should().ContainSingle().Which.Should().Contain("already exists");
    }

    [Fact]
    public async Task SaveProfile_UnknownAccount_ReturnsBadRequest()
    {
        // Arrange
        var unknownAccountId = Guid.NewGuid();

        // Act
        var response = await Client.PostAsJsonAsync(
            $"/api/v1/accounts/{unknownAccountId}/import/profile",
            CsvSamples.StandardProfile,
            ApiJson.Options);
        var body = await ApiJson.ReadApiResponseAsync<ImportProfileResponse>(response);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        body.Errors.Should().ContainSingle().Which.Should().Contain("not found");
    }

    [Fact]
    public async Task SaveProfile_Valid_ReturnsCreatedWithLocationAndOrderedMappings()
    {
        // Arrange
        var institutionId = await ApiSeeder.CreateInstitutionAsync(Client);
        var accountId = await ApiSeeder.CreateAccountAsync(Client, institutionId);

        // Act
        var response = await Client.PostAsJsonAsync(
            $"/api/v1/accounts/{accountId}/import/profile",
            CsvSamples.StandardProfile,
            ApiJson.Options);
        var profile = (await ApiJson.ReadApiResponseAsync<ImportProfileResponse>(response)).Data!;

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location!.ToString().Should().Be($"/api/v1/accounts/{accountId}/import/profile");
        profile.Id.Should().NotBeEmpty();
        profile.AccountId.Should().Be(accountId);
        profile.AmountType.Should().Be(AmountType.Single);
        profile.BalanceProvided.Should().BeTrue();
        profile.DateFormat.Should().Be("MM/dd/yyyy");
        profile.ColumnMappings.Select(mapping => mapping.ApplicationField)
            .Should().Equal("Amount", "Balance", "Date", "Description");
    }

    [Fact]
    public async Task UpdateProfile_AddingMapping_ThrowsConcurrencyException()
    {
        // Arrange
        var accountId = await ApiSeeder.CreateAccountWithProfileAsync(Client);
        var update = new SaveImportProfileRequest(
            AmountType.Single,
            true,
            [
                .. CsvSamples.StandardProfile.ColumnMappings,
                new ColumnMappingRequest("Memo", "Category"),
            ],
            "MM/dd/yyyy");

        // Act
        var act = async () => await Client.PutAsJsonAsync($"/api/v1/accounts/{accountId}/import/profile", update, ApiJson.Options);

        // Assert
        // Current behavior (defect, reported): a newly added mapping is created with a preset key, so EF tracks it
        // as Modified and SaveChanges throws instead of returning 200. The test host surfaces the unhandled exception.
        await act.Should().ThrowAsync<Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException>();
    }

    [Fact]
    public async Task UpdateProfile_RemovedAndChangedMappings_ArePersisted()
    {
        // Arrange
        var accountId = await ApiSeeder.CreateAccountWithProfileAsync(Client);
        var update = new SaveImportProfileRequest(
            AmountType.Single,
            false,
            [
                new ColumnMappingRequest("Amount", "Amount"),
                new ColumnMappingRequest("Date", "Date"),
                new ColumnMappingRequest("Description", "Category"),
            ],
            "yyyy-MM-dd");

        // Act
        var response = await Client.PutAsJsonAsync($"/api/v1/accounts/{accountId}/import/profile", update, ApiJson.Options);
        var updated = (await ApiJson.ReadApiResponseAsync<ImportProfileResponse>(response)).Data!;
        var reread = (await ApiJson.ReadApiResponseAsync<ImportProfileResponse>(
            await Client.GetAsync($"/api/v1/accounts/{accountId}/import/profile"))).Data!;

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        updated.BalanceProvided.Should().BeFalse();
        updated.DateFormat.Should().Be("yyyy-MM-dd");
        reread.DateFormat.Should().Be("yyyy-MM-dd");
        reread.ColumnMappings.Select(mapping => (mapping.CsvColumnName, mapping.ApplicationField))
            .Should().Equal(
                ("Amount", "Amount"),
                ("Description", "Category"),
                ("Date", "Date"));
    }

    [Fact]
    public async Task UpdateProfile_NoProfile_ReturnsNotFound()
    {
        // Arrange
        var institutionId = await ApiSeeder.CreateInstitutionAsync(Client);
        var accountId = await ApiSeeder.CreateAccountAsync(Client, institutionId);

        // Act
        var response = await Client.PutAsJsonAsync(
            $"/api/v1/accounts/{accountId}/import/profile",
            CsvSamples.StandardProfile,
            ApiJson.Options);
        var body = await ApiJson.ReadApiResponseAsync<ImportProfileResponse>(response);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        body.Success.Should().BeFalse();
    }

    [Fact]
    public async Task UploadPreview_EmptyFile_ReturnsBadRequest()
    {
        // Arrange
        var institutionId = await ApiSeeder.CreateInstitutionAsync(Client);
        var accountId = await ApiSeeder.CreateAccountAsync(Client, institutionId);

        // Act
        var response = await UploadAsync(accountId, string.Empty);
        var body = await ApiJson.ReadApiResponseAsync<UploadPreviewResponse>(response);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        body.Success.Should().BeFalse();
        body.Errors.Should().ContainSingle().Which.Should().Be("No file provided.");
    }

    [Fact]
    public async Task UploadPreview_MoreThanFiveRows_ReturnsFivePreviewRows()
    {
        // Arrange
        var institutionId = await ApiSeeder.CreateInstitutionAsync(Client);
        var accountId = await ApiSeeder.CreateAccountAsync(Client, institutionId);
        var rows = Enumerable.Range(1, 8).Select(day => $"01/{day:00}/2026,ROW {day},-1.00,{day}.00");
        var csv = string.Join("\n", rows.Prepend(CsvSamples.Header)) + "\n";

        // Act
        var response = await UploadAsync(accountId, csv);
        var preview = (await ApiJson.ReadApiResponseAsync<UploadPreviewResponse>(response)).Data!;

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        preview.PreviewRows.Should().HaveCount(5);
        preview.PreviewRows[0][1].Should().Be("ROW 1");
    }

    [Fact]
    public async Task UploadPreview_UnknownAccount_ReturnsBadRequest()
    {
        // Arrange
        var unknownAccountId = Guid.NewGuid();

        // Act
        var response = await UploadAsync(unknownAccountId, CsvSamples.FirstImport);
        var body = await ApiJson.ReadApiResponseAsync<UploadPreviewResponse>(response);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        body.Errors.Should().ContainSingle().Which.Should().Contain("not found");
    }

    [Fact]
    public async Task UploadPreview_ValidFile_ReturnsHeadersAndPreviewRowsEnvelope()
    {
        // Arrange
        var institutionId = await ApiSeeder.CreateInstitutionAsync(Client);
        var accountId = await ApiSeeder.CreateAccountAsync(Client, institutionId);

        // Act
        var response = await UploadAsync(accountId, CsvSamples.FirstImport);
        using var document = await ApiJson.ReadJsonDocumentAsync(response);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        ApiJson.AssertApiResponseEnvelope(document);
        var data = document.RootElement.GetProperty("data");
        data.GetProperty("headers").EnumerateArray().Select(header => header.GetString())
            .Should().Equal("Date", "Description", "Amount", "Balance");
        var firstRow = data.GetProperty("previewRows")[0].EnumerateArray().Select(cell => cell.GetString());
        firstRow.Should().Equal("01/15/2026", "STARBUCKS #123, SEATTLE WA", "-4.75", "995.25");
    }

    private async Task<HttpResponseMessage> UploadAsync(Guid accountId, string csv)
    {
        using var form = new MultipartFormDataContent();
        form.Add(new ByteArrayContent(Encoding.UTF8.GetBytes(csv)), "file", "upload.csv");
        return await Client.PostAsync($"/api/v1/accounts/{accountId}/import/upload", form);
    }
}
