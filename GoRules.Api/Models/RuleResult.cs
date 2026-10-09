namespace GoRules.Api.Models;

public class RuleResult
{
    public int Id { get; set; }
    public int RuleId { get; set; }
    public string Decision { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    public Rule Rule { get; set; } = null!;
}
