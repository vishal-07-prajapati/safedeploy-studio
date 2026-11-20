using FluentAssertions;
using SafeDeployStudio.AI.Services;
using Xunit;

namespace SafeDeployStudio.UnitTests;

public class SargabilityAnalyzerTests
{
    private readonly SargabilityAnalyzer _analyzer = new();

    [Fact]
    public void Analyze_WhenYearFunctionInPredicate_ShouldWarnSargability()
    {
        const string sql = @"
SELECT Id, Total 
FROM Orders 
WHERE YEAR(OrderDate) = 2024;";

        var warnings = _analyzer.Analyze(sql);
        warnings.Should().Contain(w => w.RuleId == "SARG001");
    }

    [Fact]
    public void Analyze_WhenSelectStarInProcedure_ShouldWarnSelectStar()
    {
        const string sql = @"
CREATE PROCEDURE dbo.sp_GetUsers
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM dbo.Users;
END";

        var warnings = _analyzer.Analyze(sql);
        warnings.Should().Contain(w => w.RuleId == "PERF001");
    }

    [Fact]
    public void Analyze_WhenMissingSetNoCountOn_ShouldWarnNoCount()
    {
        const string sql = @"
CREATE PROCEDURE dbo.sp_QuickCalc
AS
BEGIN
    SELECT 1 AS Val;
END";

        var warnings = _analyzer.Analyze(sql);
        warnings.Should().Contain(w => w.RuleId == "PERF003");
    }
}
