namespace GoRules.Api.Models;

public class RuleDataSource
{
    public int Id { get; set; }
    public int RuleId { get; set; }
    public string SourceType { get; set; } = "JSON";
    public string? SqlQuery { get; set; }
    public string? JsonSchema { get; set; }

    public Rule Rule { get; set; } = null!;
}
