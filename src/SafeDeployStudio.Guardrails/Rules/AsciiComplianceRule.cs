using SafeDeployStudio.Domain.Entities;
using SafeDeployStudio.Domain.Enums;
using SafeDeployStudio.Domain.ValueObjects;

namespace SafeDeployStudio.Guardrails.Rules;

public class AsciiComplianceRule : IGuardrailRule
{
    public string Name => "ASCII Compliance Check";

    public Task<IEnumerable<GuardrailViolation>> EvaluateAsync(string sql, GuardrailPolicy policy, CancellationToken ct = default)
    {
        var violations = new List<GuardrailViolation>();
        if (!policy.RequireAsciiOnly || string.IsNullOrEmpty(sql))
            return Task.FromResult<IEnumerable<GuardrailViolation>>(violations);

        var lines = sql.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            for (int col = 0; col < line.Length; col++)
            {
                char c = line[col];
                if (c > 127)
                {
                    string charDescription = c switch
                    {
                        '\u201C' or '\u201D' => "smart double quotes (\u201C or \u201D)",
                        '\u2018' or '\u2019' => "smart single quote / apostrophe (\u2018 or \u2019)",
                        '\u2013' or '\u2014' => "dash / em-dash (\u2013 or \u2014)",
                        '\u00A0' => "non-breaking space",
                        _ => $"Unicode character '\\u{(int)c:X4}'"
                    };

                    violations.Add(new GuardrailViolation(
                        "AsciiCompliance",
                        GuardrailSeverity.Warning,
                        $"Non-ASCII character {charDescription} found at column {col + 1}.",
                        i + 1,
                        c.ToString(),
                        "Replace non-ASCII characters with standard 7-bit ASCII equivalents (e.g. standard single quote ' or double quote \")."
                    ));

                    // Flag at most one non-ASCII warning per line to avoid noise
                    break;
                }
            }
        }

        return Task.FromResult<IEnumerable<GuardrailViolation>>(violations);
    }
}
