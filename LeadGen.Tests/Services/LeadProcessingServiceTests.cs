using FluentAssertions;
using LeadGen.Models;
using LeadGen.Services;
using LeadGen.Tests.Helpers;

namespace LeadGen.Tests.Services;

public class LeadProcessingServiceTests
{
    [Fact]
    public void ProcessFiles_ParsesJsonAndProducesLeads()
    {
        var dir = TestPaths.CreateTempDirectory();
        var jsonPath = Path.Combine(dir, "test.json");
        File.WriteAllText(jsonPath, """
            [
              {
                "title": "ООО Тест",
                "address": "Москва, ул. Ленина, д. 1",
                "phone_1": "79001234567",
                "Category 0": "Строительство"
              }
            ]
            """);

        var service = new LeadProcessingService();
        var result = service.ProcessFiles([jsonPath], [], new ProcessingSettings());

        result.Leads.Should().HaveCount(1);
        result.Leads[0].LeadTitle.Should().Be("Строительство - ООО Тест");
        result.Leads[0].WorkPhone.Should().Be("79001234567");
    }

    [Fact]
    public void ProcessFiles_RemovesDuplicatesByPhone()
    {
        var dir = TestPaths.CreateTempDirectory();
        var jsonPath = Path.Combine(dir, "dup.json");
        File.WriteAllText(jsonPath, """
            [
              {"title": "A", "address": "Москва, ул. A, 1", "phone_1": "79001111111"},
              {"title": "B", "address": "Москва, ул. B, 2", "phone_1": "79001111111"}
            ]
            """);

        var service = new LeadProcessingService();
        var result = service.ProcessFiles([jsonPath], [], new ProcessingSettings { RemoveDuplicates = true });

        result.Leads.Should().HaveCount(1);
        result.DuplicatesRemoved.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ProcessFiles_ExcludesJsonRowsWithBlockedCategory()
    {
        var dir = TestPaths.CreateTempDirectory();
        var jsonPath = Path.Combine(dir, "excluded.json");
        File.WriteAllText(jsonPath, """
            [
              {"title": "Кафе А", "address": "Москва, ул. A, 1", "phone_1": "79001111111", "Category 0": "Кафе"},
              {"title": "Строй Б", "address": "Москва, ул. B, 2", "phone_1": "79002222222", "Category 0": "Строительство"}
            ]
            """);

        var service = new LeadProcessingService();
        var result = service.ProcessFiles([jsonPath], [], new ProcessingSettings());

        result.Leads.Should().HaveCount(1);
        result.Leads[0].CompanyName.Should().Be("Строй Б");
        result.RowsRemovedExcludedCategory.Should().Be(1);
    }

    [Fact]
    public void ProcessFiles_DropsRowsWithoutAllowedAddress()
    {
        var dir = TestPaths.CreateTempDirectory();
        var jsonPath = Path.Combine(dir, "bad_addr.json");
        File.WriteAllText(jsonPath, """
            [
              {"title": "A", "address": "Неизвестный регион, ул. X", "phone_1": "79001111111"}
            ]
            """);

        var service = new LeadProcessingService();
        var result = service.ProcessFiles([jsonPath], [], new ProcessingSettings());

        result.Leads.Should().BeEmpty();
        result.RowsRemovedNoLocation.Should().Be(1);
    }
}
