using FluentAssertions;
using SafeDeployStudio.Deployment;
using SafeDeployStudio.Domain.ValueObjects;
using Xunit;

namespace SafeDeployStudio.UnitTests;

public class SqlDiffEngineTests
{
    private readonly SqlDiffEngine _differ = new();

    [Fact]
    public void CompareSql_WhenIdentical_ShouldReturnNoDifferences()
    {
        const string sql = "CREATE PROCEDURE dbo.sp_Test AS SELECT 1;";
        var result = _differ.CompareSql(sql, sql, "sp_Test");

        result.HasDifferences.Should().BeFalse();
        result.AdditionsCount.Should().Be(0);
        result.DeletionsCount.Should().Be(0);
    }

    [Fact]
    public void CompareSql_WhenLinesAddedAndModified_ShouldDetectChanges()
    {
        const string oldSql = "CREATE PROCEDURE dbo.sp_Test AS\nBEGIN\n    SELECT 1;\nEND";
        const string newSql = "CREATE PROCEDURE dbo.sp_Test AS\nBEGIN\n    SELECT 1;\n    SELECT 2;\nEND";

        var result = _differ.CompareSql(oldSql, newSql, "sp_Test");

        result.HasDifferences.Should().BeTrue();
        result.AdditionsCount.Should().BeGreaterThan(0);
    }
}
