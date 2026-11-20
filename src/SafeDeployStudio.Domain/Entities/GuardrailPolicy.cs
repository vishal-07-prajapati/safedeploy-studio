using SafeDeployStudio.Domain.Common;

namespace SafeDeployStudio.Domain.Entities;

public class GuardrailPolicy : Entity, IAggregateRoot
{
    public Guid ProjectId { get; set; }
    public Project? Project { get; set; }

    public string Name { get; set; } = "Default SafeGuard Policy";
    public string Description { get; set; } = "Standard enterprise guardrail blocking destructive DDL and dangerous operations.";

    public bool BlockDropTable { get; set; } = true;
    public bool BlockTruncateTable { get; set; } = true;
    public bool BlockAlterPhysicalTable { get; set; } = true;
    public bool AllowTempTablesOnly { get; set; } = true;
    public bool RequireAsciiOnly { get; set; } = true;
    public int MaxScriptSizeKb { get; set; } = 512;
    public bool RequireWhereOnUpdateDelete { get; set; } = true;
    public bool WarnOnNolock { get; set; } = true;
    public bool WarnOnCursor { get; set; } = true;

    // JSON serialized list of forbidden words: xp_cmdshell, SHUTDOWN, DROP DATABASE, etc.
    public string ForbiddenKeywordsJson { get; set; } = "[\"xp_cmdshell\",\"sp_configure\",\"SHUTDOWN\",\"DROP DATABASE\",\"RECONFIGURE\"]";

    // JSON serialized custom regex rules: [{ "Pattern": "...", "Message": "...", "Severity": 3 }]
    public string CustomRegexRulesJson { get; set; } = "[]";

    public bool IsActive { get; set; } = true;

    public GuardrailPolicy() { }

    public GuardrailPolicy(Guid projectId, string name)
    {
        ProjectId = projectId;
        Name = name;
    }
}
