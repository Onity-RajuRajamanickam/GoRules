namespace GoRules.Api.Models;

public class RuleExecutionRequest
{
    public int? RuleId { get; set; }
    public List<int>? RuleIds { get; set; }
    public Dictionary<string, object>? Input { get; set; }
}

public class RuleExecutionResponse
{
    public string Decision { get; set; } = string.Empty;
    public string MatchedRule { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public long ExecutionTimeMs { get; set; }
    public Dictionary<string, object>? Input { get; set; }
    public string? DecisionJson { get; set; }
    public string? EngineResultJson { get; set; }
}

public class RuleExecutionBatchResponse
{
    public List<int> SelectedRuleIds { get; set; } = new();
    public List<RuleExecutionResponse> Results { get; set; } = new();
    public string? CombinedDecisionJson { get; set; }
    public string? CombinedEngineResultJson { get; set; }
}

public class RuleCreateRequest
{
    public int? Id { get; set; }
    public string RuleName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public List<RuleCondition> Conditions { get; set; } = new();
    public RuleResultModel? Result { get; set; }
}

public class RuleResultModel
{
    public string Decision { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
