namespace GoRules.Api.Models;

public class Rule
{
    public int Id { get; set; }
    public string RuleName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int ExecutionCount { get; set; }

    public ICollection<RuleCondition> Conditions { get; set; } = new List<RuleCondition>();
    public ICollection<RuleResult> Results { get; set; } = new List<RuleResult>();
    public ICollection<RuleDataSource> DataSources { get; set; } = new List<RuleDataSource>();
}
