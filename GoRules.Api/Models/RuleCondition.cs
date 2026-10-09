namespace GoRules.Api.Models;

public class RuleCondition
{
    public int Id { get; set; }
    public int RuleId { get; set; }
    public string LeftOperand { get; set; } = string.Empty;
    public string Operator { get; set; } = ">";
    public string RightOperand { get; set; } = string.Empty;
    public string LogicalOperator { get; set; } = "AND";
    public int DisplayOrder { get; set; }

    public Rule Rule { get; set; } = null!;
}
