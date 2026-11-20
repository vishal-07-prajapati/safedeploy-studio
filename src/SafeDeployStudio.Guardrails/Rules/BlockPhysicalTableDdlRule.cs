using Microsoft.SqlServer.TransactSql.ScriptDom;
using SafeDeployStudio.Domain.Entities;
using SafeDeployStudio.Domain.Enums;
using SafeDeployStudio.Domain.ValueObjects;

namespace SafeDeployStudio.Guardrails.Rules;

public class BlockPhysicalTableDdlRule : IGuardrailRule
{
    public string Name => "Block Physical Table DDL";

    public Task<IEnumerable<GuardrailViolation>> EvaluateAsync(string sql, GuardrailPolicy policy, CancellationToken ct = default)
    {
        var violations = new List<GuardrailViolation>();
        if (string.IsNullOrWhiteSpace(sql))
            return Task.FromResult<IEnumerable<GuardrailViolation>>(violations);

        var parser = new TSql160Parser(initialQuotedIdentifiers: true);
        using var reader = new StringReader(sql);
        var fragment = parser.Parse(reader, out IList<ParseError> errors);

        var visitor = new PhysicalDdlVisitor(policy);
        fragment?.Accept(visitor);
        violations.AddRange(visitor.Violations);

        return Task.FromResult<IEnumerable<GuardrailViolation>>(violations);
    }

    private class PhysicalDdlVisitor : TSqlFragmentVisitor
    {
        private readonly GuardrailPolicy _policy;
        public List<GuardrailViolation> Violations { get; } = [];

        public PhysicalDdlVisitor(GuardrailPolicy policy)
        {
            _policy = policy;
        }

        public override void ExplicitVisit(DropTableStatement node)
        {
            if (!_policy.BlockDropTable) return;

            foreach (var table in node.Objects)
            {
                var tableName = table.BaseIdentifier?.Value ?? string.Empty;
                if (!tableName.StartsWith('#'))
                {
                    Violations.Add(new GuardrailViolation(
                        "BlockDropTable",
                        GuardrailSeverity.Critical,
                        $"Prohibited DROP TABLE detected on physical table '{tableName}'. Only temporary tables (#temp) may be dropped in procedural migrations.",
                        node.StartLine,
                        tableName,
                        "Remove the DROP TABLE statement. Physical schema changes must be deployed via dedicated migration scripts."
                    ));
                }
            }
        }

        public override void ExplicitVisit(TruncateTableStatement node)
        {
            if (!_policy.BlockTruncateTable) return;

            var tableName = node.TableViewOrFunctionExpr?.BaseIdentifier?.Value ?? string.Empty;
            if (!tableName.StartsWith('#'))
            {
                Violations.Add(new GuardrailViolation(
                    "BlockTruncateTable",
                    GuardrailSeverity.Critical,
                    $"Prohibited TRUNCATE TABLE detected on physical table '{tableName}'.",
                    node.StartLine,
                    tableName,
                    "Remove TRUNCATE TABLE statement."
                ));
            }
        }

        public override void ExplicitVisit(AlterTableStatement node)
        {
            if (!_policy.BlockAlterPhysicalTable) return;

            var tableName = node.SchemaObjectName?.BaseIdentifier?.Value ?? string.Empty;
            if (!tableName.StartsWith('#'))
            {
                Violations.Add(new GuardrailViolation(
                    "BlockAlterPhysicalTable",
                    GuardrailSeverity.Error,
                    $"Prohibited ALTER TABLE detected on physical table '{tableName}'.",
                    node.StartLine,
                    tableName,
                    "Physical table schema alterations must be isolated and pre-approved."
                ));
            }
        }
    }
}
