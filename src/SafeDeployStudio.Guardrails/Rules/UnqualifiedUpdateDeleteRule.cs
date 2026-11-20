using Microsoft.SqlServer.TransactSql.ScriptDom;
using SafeDeployStudio.Domain.Entities;
using SafeDeployStudio.Domain.Enums;
using SafeDeployStudio.Domain.ValueObjects;

namespace SafeDeployStudio.Guardrails.Rules;

public class UnqualifiedUpdateDeleteRule : IGuardrailRule
{
    public string Name => "Unqualified UPDATE/DELETE Check";

    public Task<IEnumerable<GuardrailViolation>> EvaluateAsync(string sql, GuardrailPolicy policy, CancellationToken ct = default)
    {
        var violations = new List<GuardrailViolation>();
        if (!policy.RequireWhereOnUpdateDelete || string.IsNullOrWhiteSpace(sql))
            return Task.FromResult<IEnumerable<GuardrailViolation>>(violations);

        var parser = new TSql160Parser(initialQuotedIdentifiers: true);
        using var reader = new StringReader(sql);
        var fragment = parser.Parse(reader, out IList<ParseError> errors);

        var visitor = new UnqualifiedStatementVisitor();
        fragment?.Accept(visitor);
        violations.AddRange(visitor.Violations);

        return Task.FromResult<IEnumerable<GuardrailViolation>>(violations);
    }

    private class UnqualifiedStatementVisitor : TSqlFragmentVisitor
    {
        public List<GuardrailViolation> Violations { get; } = [];

        public override void ExplicitVisit(UpdateStatement node)
        {
            if (node.UpdateSpecification?.WhereClause == null)
            {
                var target = node.UpdateSpecification?.Target?.ToString() ?? "table";
                Violations.Add(new GuardrailViolation(
                    "UnqualifiedUpdate",
                    GuardrailSeverity.Error,
                    $"UPDATE statement on {target} is missing a WHERE clause. This will update every row in the target table.",
                    node.StartLine,
                    null,
                    "Add an explicit WHERE clause or use a WHERE 1=1 guard if updating all rows is truly intended."
                ));
            }
        }

        public override void ExplicitVisit(DeleteStatement node)
        {
            if (node.DeleteSpecification?.WhereClause == null)
            {
                var target = node.DeleteSpecification?.Target?.ToString() ?? "table";
                Violations.Add(new GuardrailViolation(
                    "UnqualifiedDelete",
                    GuardrailSeverity.Error,
                    $"DELETE statement on {target} is missing a WHERE clause. This will delete every row in the target table.",
                    node.StartLine,
                    null,
                    "Add an explicit WHERE clause to prevent accidental bulk data loss."
                ));
            }
        }
    }
}
