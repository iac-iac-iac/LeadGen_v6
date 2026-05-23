using FluentAssertions;
using LeadGen.Models;
using LeadGen.Services;

namespace LeadGen.Tests.Services;

public class BitrixMapperTests
{
    [Fact]
    public void MapToBitrix_MapsLeadFields()
    {
        var leads = new List<LeadRecord>
        {
            new()
            {
                LeadTitle = "Cat - Company",
                CompanyName = "Company",
                WorkPhone = "79001234567",
                Address = "Москва",
                Manager = "Иван"
            }
        };

        var rows = BitrixMapper.MapToBitrix(leads, new BitrixSettings());
        rows.Should().HaveCount(1);
        rows[0]["Название лида"].Should().Be("Cat - Company");
        rows[0]["Ответственный"].Should().Be("Иван");
        rows[0]["Стадия"].Should().Be("Новая заявка");
    }

    [Fact]
    public void ExportToCsv_CreatesQuotedSemicolonFile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"bitrix_{Guid.NewGuid():N}.csv");
        try
        {
            var rows = BitrixMapper.MapToBitrix(
            [
                new LeadRecord { LeadTitle = "Test", WorkPhone = "79001234567" }
            ], new BitrixSettings());

            BitrixMapper.ExportToCsv(path, rows);

            var content = File.ReadAllText(path);
            content.Should().Contain(";");
            content.Should().Contain("Название лида");
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
