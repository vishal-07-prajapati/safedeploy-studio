using FluentAssertions;
using SafeDeployStudio.Domain.Entities;
using SafeDeployStudio.Domain.Enums;
using SafeDeployStudio.Guardrails;
using Xunit;

namespace SafeDeployStudio.UnitTests;

public class GuardrailTests
{
    private readonly GuardrailEngine _engine = new();
    private readonly GuardrailPolicy _policy = new(Guid.NewGuid(), "Test Policy")
    {
        BlockDropTable = true,
        BlockTruncateTable = true,
        BlockAlterPhysicalTable = true,
        AllowTempTablesOnly = true,
        RequireAsciiOnly = true,
        RequireWhereOnUpdateDelete = true
    };

    [Fact]
    public async Task ValidateScript_WhenPhysicalTableDropped_ShouldReturnCriticalViolation()
    {
        // Arrange
        const string sql = @"
CREATE PROCEDURE dbo.sp_DeleteCustomer
AS
BEGIN
    DROP TABLE dbo.CustomerOrders;
END";

        // Act
        var result = await _engine.ValidateScriptAsync(sql, _policy);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Violations.Should().Contain(v => v.RuleName == "BlockDropTable" && v.Severity == GuardrailSeverity.Critical);
        result.EstimatedRisk.Should().Be(RiskLevel.Critical);
    }

    [Fact]
    public async Task ValidateScript_WhenTempTableDropped_ShouldPass()
    {
        // Arrange
        const string sql = @"
CREATE PROCEDURE dbo.sp_ProcessTemp
AS
BEGIN
    CREATE TABLE #TempStaging (Id INT);
    -- work
    DROP TABLE #TempStaging;
END";

        // Act
        var result = await _engine.ValidateScriptAsync(sql, _policy);

        // Assert
        result.Violations.Where(v => v.RuleName == "BlockDropTable").Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateScript_WhenAdminCommandPresent_ShouldReturnCriticalViolation()
    {
        // Arrange
        const string sql = @"
CREATE PROCEDURE dbo.sp_RunShell
AS
BEGIN
    EXEC master..xp_cmdshell 'dir C:\';
END";

        // Act
        var result = await _engine.ValidateScriptAsync(sql, _policy);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Violations.Should().Contain(v => v.RuleName == "ForbiddenAdminKeywords");
    }

    [Fact]
    public async Task ValidateScript_WhenSmartQuotesPresent_ShouldFlagAsciiWarning()
    {
        // Arrange
        const string sql = "SELECT \u201CName\u201D FROM Users;";

        // Act
        var result = await _engine.ValidateScriptAsync(sql, _policy);

        // Assert
        result.Violations.Should().Contain(v => v.RuleName == "AsciiCompliance");
    }

    [Fact]
    public async Task ValidateScript_WhenUnqualifiedUpdate_ShouldReturnError()
    {
        // Arrange
        const string sql = @"
CREATE PROCEDURE dbo.sp_ResetStatus
AS
BEGIN
    UPDATE dbo.Accounts SET IsActive = 0;
END";

        // Act
        var result = await _engine.ValidateScriptAsync(sql, _policy);

        // Assert
        result.Violations.Should().Contain(v => v.RuleName == "UnqualifiedUpdate");
    }
}
