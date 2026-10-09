using System.Diagnostics;
using System.Text;
using System.Text.Json;
using GoRules.Api.Data;
using GoRules.Api.Models;
using Microsoft.EntityFrameworkCore;
using ZenEngineType = GoRules.ZenEngine.ZenEngine;
using JsonBufferType = GoRules.ZenEngine.JsonBuffer;

namespace GoRules.Api.Services;

public class GoRulesDecisionService
{
    private readonly GoRulesDbContext _dbContext;

    public GoRulesDecisionService(GoRulesDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<RuleExecutionResponse> EvaluateSingleRuleAsync(Rule rule, Dictionary<string, object> input)
    {
        var decisionJson = BuildDecisionJson(rule);
        var startedAt = Stopwatch.StartNew();
        var filteredInput = FilterInputForRule(rule, input);
        var normalizedInput = NormalizeInputForEvaluation(filteredInput);

        using var engine = new ZenEngineType(loader: null, customNode: null);
        var decision = engine.CreateDecision(new JsonBufferType(Encoding.UTF8.GetBytes(decisionJson)));
        var context = new JsonBufferType(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(normalizedInput)));

        var response = await decision.Evaluate(context, null);
        var jsonText = ConvertEvaluationResult(response.Result);

        using var document = JsonDocument.Parse(jsonText);
        var result = document.RootElement;
        var decisionValue = result.TryGetProperty("decision", out var decisionProperty)
            ? decisionProperty.GetString() ?? "No Match"
            : "No Match";
        var messageValue = result.TryGetProperty("message", out var messageProperty)
            ? messageProperty.GetString() ?? string.Empty
            : string.Empty;

        rule.ExecutionCount++;
        await _dbContext.SaveChangesAsync();

        return new RuleExecutionResponse
        {
            Decision = decisionValue,
            MatchedRule = rule.RuleName,
            Message = messageValue,
            ExecutionTimeMs = startedAt.ElapsedMilliseconds,
            Input = filteredInput,
            DecisionJson = decisionJson,
            EngineResultJson = jsonText,
        };
    }

    public async Task<RuleExecutionResponse> EvaluateBestRuleAsync(Dictionary<string, object> input)
    {
        var rules = await _dbContext.Rules
            .Include(r => r.Conditions)
            .Include(r => r.Results)
            .Where(r => r.IsActive)
            .OrderBy(r => r.Id)
            .ToListAsync();

        foreach (var rule in rules)
        {
            var evaluation = await EvaluateSingleRuleAsync(rule, input);
            if (!string.IsNullOrWhiteSpace(evaluation.Decision) && evaluation.Decision != "No Match")
            {
                return evaluation;
            }
        }

        var fallback = new RuleExecutionResponse
        {
            Decision = "Reject",
            MatchedRule = "Default Fallback",
            Message = "No active rule matched the supplied input.",
            ExecutionTimeMs = 0,
        };

        return fallback;
    }

    public async Task<RuleExecutionBatchResponse> EvaluateSelectedRulesBatchAsync(List<Rule> rules, Dictionary<string, object> input)
    {
        if (rules.Count == 0)
        {
            return new RuleExecutionBatchResponse();
        }

        var combinedDecisionJson = BuildCombinedDecisionJson(rules);
        var startedAt = Stopwatch.StartNew();
        var filteredInput = FilterInputForRules(rules, input);
        var normalizedInput = NormalizeInputForEvaluation(filteredInput);

        using var engine = new ZenEngineType(loader: null, customNode: null);
        var decision = engine.CreateDecision(new JsonBufferType(Encoding.UTF8.GetBytes(combinedDecisionJson)));
        var context = new JsonBufferType(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(normalizedInput)));

        var response = await decision.Evaluate(context, null);
        var engineResultJson = ConvertEvaluationResult(response.Result);

        using var document = JsonDocument.Parse(engineResultJson);
        var result = document.RootElement;
        var matchedRuleName = result.TryGetProperty("matchedRule", out var matchedRuleProperty)
            ? matchedRuleProperty.GetString() ?? ""
            : string.Empty;
        var decisionValue = result.TryGetProperty("decision", out var decisionProperty)
            ? decisionProperty.GetString() ?? "No Match"
            : "No Match";
        var messageValue = result.TryGetProperty("message", out var messageProperty)
            ? messageProperty.GetString() ?? string.Empty
            : string.Empty;

        var responses = new List<RuleExecutionResponse>();
        foreach (var rule in rules)
        {
            var localMatch = EvaluateRuleLocally(rule, filteredInput);
            var isCurrentMatch = !string.IsNullOrWhiteSpace(matchedRuleName) &&
                string.Equals(rule.RuleName, matchedRuleName, StringComparison.OrdinalIgnoreCase);

            responses.Add(new RuleExecutionResponse
            {
                Decision = isCurrentMatch ? decisionValue : localMatch.Decision,
                MatchedRule = rule.RuleName,
                Message = isCurrentMatch ? messageValue : localMatch.Message,
                ExecutionTimeMs = startedAt.ElapsedMilliseconds,
                Input = filteredInput,
                DecisionJson = combinedDecisionJson,
                EngineResultJson = engineResultJson,
            });
        }

        return new RuleExecutionBatchResponse
        {
            SelectedRuleIds = rules.Select(rule => rule.Id).ToList(),
            Results = responses,
            CombinedDecisionJson = combinedDecisionJson,
            CombinedEngineResultJson = engineResultJson,
        };
    }

    public string BuildDecisionJson(Rule rule)
    {
        var conditions = rule.Conditions.OrderBy(c => c.DisplayOrder).ToList();
        var result = rule.Results.FirstOrDefault() ?? new RuleResult { Decision = "Approve", Message = "Eligible for processing" };

        var inputColumns = conditions
            .Select((condition, index) => new
            {
                id = $"input_{index + 1}",
                name = condition.LeftOperand,
                field = condition.LeftOperand,
                type = "expression",
            })
            .DistinctBy(column => column.field)
            .ToList();

        var outputColumns = new[]
        {
            new { id = "decision_out", name = "Decision", field = "decision", type = "expression" },
            new { id = "message_out", name = "Message", field = "message", type = "expression" },
        };

        var rows = new List<Dictionary<string, string>>();
        var ruleValues = new Dictionary<string, string>();

        foreach (var condition in conditions)
        {
            var normalizedValue = NormalizeValue(condition.RightOperand);
            ruleValues[$"input_{conditions.IndexOf(condition) + 1}"] = $"{condition.Operator} {normalizedValue}";
        }

        if (conditions.Count == 0)
        {
            ruleValues["input_1"] = "";
        }

        ruleValues["decision_out"] = $"\"{Escape(result.Decision)}\"";
        ruleValues["message_out"] = $"\"{Escape(result.Message)}\"";
        rows.Add(ruleValues);

        var defaultRow = new Dictionary<string, string>
        {
            ["decision_out"] = "\"Reject\"",
            ["message_out"] = "\"No matching rule was met\"",
        };

        if (conditions.Count > 0)
        {
            var placeholderIndex = 1;
            foreach (var condition in conditions)
            {
                defaultRow[$"input_{placeholderIndex}"] = "";
                placeholderIndex++;
            }
        }
        else
        {
            defaultRow["input_1"] = "";
        }

        rows.Add(defaultRow);

        var inputNode = new
        {
            id = "request",
            type = "inputNode",
            name = "Request",
            position = new { x = 180, y = 210 },
        };

        var decisionNode = new
        {
            id = "decision",
            type = "decisionTableNode",
            name = rule.RuleName,
            position = new { x = 470, y = 210 },
            content = new
            {
                hitPolicy = "first",
                inputs = inputColumns,
                outputs = outputColumns,
                rules = rows.Select(row =>
                {
                    var output = new Dictionary<string, string>();
                    foreach (var kvp in row)
                    {
                        output[kvp.Key] = kvp.Value;
                    }

                    return output;
                }).ToList(),
            },
        };

        var outputNode = new
        {
            id = "response",
            type = "outputNode",
            name = "Response",
            position = new { x = 780, y = 210 },
        };

        var payload = new
        {
            nodes = new object[] { inputNode, decisionNode, outputNode },
            edges = new object[]
            {
                new { id = "edge-1", type = "edge", sourceId = "request", targetId = "decision" },
                new { id = "edge-2", type = "edge", sourceId = "decision", targetId = "response" },
            },
        };

        return JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
    }

    private string BuildCombinedDecisionJson(List<Rule> rules)
    {
        var allOperands = rules
            .SelectMany(rule => rule.Conditions)
            .Select(condition => condition.LeftOperand)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var outputColumns = new[]
        {
            new { id = "decision_out", name = "Decision", field = "decision", type = "expression" },
            new { id = "message_out", name = "Message", field = "message", type = "expression" },
            new { id = "rule_out", name = "Matched Rule", field = "matchedRule", type = "expression" },
        };

        var rows = new List<Dictionary<string, string>>();

        foreach (var rule in rules)
        {
            var row = new Dictionary<string, string>();

            for (var index = 0; index < allOperands.Count; index++)
            {
                row[$"input_{index + 1}"] = string.Empty;
            }

            foreach (var condition in rule.Conditions.OrderBy(c => c.DisplayOrder))
            {
                var operandIndex = allOperands.IndexOf(condition.LeftOperand) + 1;
                row[$"input_{operandIndex}"] = $"{condition.Operator} {NormalizeValue(condition.RightOperand)}";
            }

            var result = rule.Results.FirstOrDefault() ?? new RuleResult { Decision = "Approve", Message = "Eligible for processing" };
            row["decision_out"] = $"\"{Escape(result.Decision)}\"";
            row["message_out"] = $"\"{Escape(result.Message)}\"";
            row["rule_out"] = $"\"{Escape(rule.RuleName)}\"";
            rows.Add(row);
        }

        if (rows.Count == 0)
        {
            rows.Add(new Dictionary<string, string>
            {
                ["decision_out"] = "\"Reject\"",
                ["message_out"] = "\"No matching rule was met\"",
                ["rule_out"] = "\"No Match\"",
            });
        }

        var inputColumns = allOperands
            .Select((operand, index) => new
            {
                id = $"input_{index + 1}",
                name = operand,
                field = operand,
                type = "expression",
            })
            .ToList();

        var inputNode = new
        {
            id = "request",
            type = "inputNode",
            name = "Request",
            position = new { x = 180, y = 210 },
        };

        var decisionNode = new
        {
            id = "decision",
            type = "decisionTableNode",
            name = "Combined Rule Evaluation",
            position = new { x = 470, y = 210 },
            content = new
            {
                hitPolicy = "first",
                inputs = inputColumns,
                outputs = outputColumns,
                rules = rows,
            },
        };

        var outputNode = new
        {
            id = "response",
            type = "outputNode",
            name = "Response",
            position = new { x = 780, y = 210 },
        };

        var payload = new
        {
            nodes = new object[] { inputNode, decisionNode, outputNode },
            edges = new object[]
            {
                new { id = "edge-1", type = "edge", sourceId = "request", targetId = "decision" },
                new { id = "edge-2", type = "edge", sourceId = "decision", targetId = "response" },
            },
        };

        return JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
    }

    private static (string Decision, string Message) EvaluateRuleLocally(Rule rule, Dictionary<string, object> input)
    {
        var conditions = rule.Conditions.OrderBy(c => c.DisplayOrder).ToList();
        var result = rule.Results.FirstOrDefault() ?? new RuleResult { Decision = "Approve", Message = "Eligible for processing" };

        foreach (var condition in conditions)
        {
            if (!input.TryGetValue(condition.LeftOperand, out var rawValue))
            {
                return ("Reject", "No matching rule was met");
            }

            if (!ConditionMatches(condition, rawValue))
            {
                return ("Reject", "No matching rule was met");
            }
        }

        return (result.Decision, result.Message);
    }

    private static bool ConditionMatches(RuleCondition condition, object rawValue)
    {
        var rightOperand = condition.RightOperand;
        var comparisonTarget = rawValue switch
        {
            string text => text,
            IConvertible convertible => convertible.ToString(),
            _ => rawValue.ToString(),
        };

        if (double.TryParse(comparisonTarget, out var leftNumber) && double.TryParse(rightOperand, out var rightNumber))
        {
            return condition.Operator switch
            {
                ">" => leftNumber > rightNumber,
                "<" => leftNumber < rightNumber,
                ">=" => leftNumber >= rightNumber,
                "<=" => leftNumber <= rightNumber,
                "==" => leftNumber == rightNumber,
                "!=" => leftNumber != rightNumber,
                _ => false,
            };
        }

        var leftText = comparisonTarget ?? string.Empty;
        return condition.Operator switch
        {
            "==" => string.Equals(NormalizeComparisonText(leftText), NormalizeComparisonText(rightOperand), StringComparison.OrdinalIgnoreCase),
            "!=" => !string.Equals(NormalizeComparisonText(leftText), NormalizeComparisonText(rightOperand), StringComparison.OrdinalIgnoreCase),
            _ => false,
        };
    }

    private static string NormalizeComparisonText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value
            .Trim()
            .Trim(',', '.', ';', ':', '"', '\'', '(', ')', '[', ']', '{', '}', '-', '_')
            .ToUpperInvariant();
    }

    private static string ConvertEvaluationResult(object? evaluationResult)
    {
        if (evaluationResult is null)
        {
            return "{}";
        }

        if (evaluationResult is string text)
        {
            return TryDecodeValueWrapper(text) ?? text;
        }

        if (evaluationResult is JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty("Value", out var valueProperty))
            {
                var valueText = valueProperty.ValueKind == JsonValueKind.String
                    ? valueProperty.GetString()
                    : valueProperty.ToString();

                var decoded = TryDecodeValueWrapper(valueText);
                if (!string.IsNullOrWhiteSpace(decoded))
                {
                    return decoded;
                }
            }

            return element.GetRawText();
        }

        var serialized = JsonSerializer.Serialize(evaluationResult);

        try
        {
            using var document = JsonDocument.Parse(serialized);
            if (document.RootElement.ValueKind == JsonValueKind.Object &&
                document.RootElement.TryGetProperty("Value", out var valueProperty))
            {
                var valueText = valueProperty.ValueKind == JsonValueKind.String
                    ? valueProperty.GetString()
                    : valueProperty.ToString();

                var decoded = TryDecodeValueWrapper(valueText);
                if (!string.IsNullOrWhiteSpace(decoded))
                {
                    return decoded;
                }
            }
        }
        catch
        {
            // Ignore parsing issues and fall back to the serialized form.
        }

        return TryDecodeValueWrapper(serialized) ?? serialized;
    }

    private static Dictionary<string, object> NormalizeInputForEvaluation(Dictionary<string, object> input)
    {
        var normalizedInput = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        foreach (var pair in input)
        {
            normalizedInput[pair.Key] = NormalizeInputValue(pair.Value);
        }

        return normalizedInput;
    }

    private static Dictionary<string, object> FilterInputForRule(Rule rule, Dictionary<string, object> input)
    {
        return FilterInputByOperands(rule.Conditions.Select(condition => condition.LeftOperand), input);
    }

    private static Dictionary<string, object> FilterInputForRules(List<Rule> rules, Dictionary<string, object> input)
    {
        return FilterInputByOperands(
            rules.SelectMany(rule => rule.Conditions).Select(condition => condition.LeftOperand),
            input);
    }

    private static Dictionary<string, object> FilterInputByOperands(IEnumerable<string> operands, Dictionary<string, object> input)
    {
        var filteredInput = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        foreach (var operand in operands.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (input.TryGetValue(operand, out var value))
            {
                filteredInput[operand] = value;
            }
        }

        return filteredInput;
    }

    private static object NormalizeInputValue(object? value)
    {
        if (value is null)
        {
            return string.Empty;
        }

        if (value is string text)
        {
            return NormalizeComparisonText(text);
        }

        if (value is JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.String)
            {
                return NormalizeComparisonText(element.GetString());
            }

            if (element.ValueKind == JsonValueKind.Object)
            {
                var normalizedObject = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                foreach (var property in element.EnumerateObject())
                {
                    normalizedObject[property.Name] = NormalizeInputValue(property.Value);
                }

                return normalizedObject;
            }

            if (element.ValueKind == JsonValueKind.Array)
            {
                var normalizedArray = new List<object>();
                foreach (var item in element.EnumerateArray())
                {
                    normalizedArray.Add(NormalizeInputValue(item));
                }

                return normalizedArray;
            }
        }

        return value;
    }

    private static string? TryDecodeValueWrapper(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        try
        {
            var bytes = Convert.FromBase64String(value);
            var decodedText = Encoding.UTF8.GetString(bytes);

            if (decodedText.TrimStart().StartsWith("{") || decodedText.TrimStart().StartsWith("["))
            {
                return decodedText;
            }
        }
        catch
        {
            // Not a base64-encoded wrapper; leave the original value alone.
        }

        return null;
    }

    private static string NormalizeValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        if (bool.TryParse(value, out var boolValue))
        {
            return boolValue ? "true" : "false";
        }

        if (double.TryParse(value, out _))
        {
            return value;
        }

        if (value.StartsWith('"') || value.StartsWith('\''))
        {
            return value;
        }

        return $"\"{value}\"";
    }

    private static string Escape(string value)
    {
        return value.Replace("\"", "\\\"");
    }
}
