using GoRules.Api.Data;
using GoRules.Api.Models;
using GoRules.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var useInMemoryDatabase = builder.Configuration.GetValue<bool>("UseInMemoryDatabase");
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
    "Server=(localdb)\\mssqllocaldb;Database=GoRulesDemo;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

builder.Services.AddDbContext<GoRulesDbContext>(options =>
{
    if (useInMemoryDatabase)
    {
        options.UseInMemoryDatabase("GoRulesDemo");
    }
    else
    {
        options.UseSqlServer(connectionString);
    }
});

builder.Services.AddScoped<GoRulesDecisionService>();

var app = builder.Build();

app.UseCors("ReactPolicy");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<GoRulesDbContext>();

    if (dbContext.Database.IsRelational())
    {
        dbContext.Database.Migrate();
    }
    else
    {
        dbContext.Database.EnsureCreated();
    }

    SeedData.Initialize(dbContext);
}

app.MapGet("/api/dashboard", async (GoRulesDbContext dbContext) =>
{
    var totalRules = await dbContext.Rules.CountAsync();
    var activeRules = await dbContext.Rules.CountAsync(r => r.IsActive);
    var executionCount = await dbContext.Rules.SumAsync(r => r.ExecutionCount);

    return Results.Ok(new
    {
        totalRules,
        activeRules,
        executionCount,
    });
});

app.MapGet("/api/rules", async (GoRulesDbContext dbContext) =>
{
    var rules = await dbContext.Rules
        .Include(r => r.Conditions)
        .Include(r => r.Results)
        .OrderBy(r => r.Id)
        .ToListAsync();

    return Results.Ok(rules.Select(rule => new
    {
        id = rule.Id,
        ruleName = rule.RuleName,
        description = rule.Description,
        isActive = rule.IsActive,
        executionCount = rule.ExecutionCount,
        conditions = rule.Conditions.OrderBy(c => c.DisplayOrder).Select(c => new
        {
            id = c.Id,
            ruleId = c.RuleId,
            leftOperand = c.LeftOperand,
            @operator = c.Operator,
            rightOperand = c.RightOperand,
            logicalOperator = c.LogicalOperator,
            displayOrder = c.DisplayOrder,
        }).ToList(),
        result = rule.Results.FirstOrDefault() is null
            ? null
            : new
            {
                id = rule.Results.First().Id,
                decision = rule.Results.First().Decision,
                message = rule.Results.First().Message,
            },
    }));
});

app.MapGet("/api/rules/{id:int}", async (int id, GoRulesDbContext dbContext) =>
{
    var rule = await dbContext.Rules
        .Include(r => r.Conditions)
        .Include(r => r.Results)
        .FirstOrDefaultAsync(r => r.Id == id);

    if (rule is null)
    {
        return Results.NotFound();
    }

    return Results.Ok(new
    {
        id = rule.Id,
        ruleName = rule.RuleName,
        description = rule.Description,
        isActive = rule.IsActive,
        executionCount = rule.ExecutionCount,
        conditions = rule.Conditions.OrderBy(c => c.DisplayOrder).Select(c => new
        {
            id = c.Id,
            ruleId = c.RuleId,
            leftOperand = c.LeftOperand,
            @operator = c.Operator,
            rightOperand = c.RightOperand,
            logicalOperator = c.LogicalOperator,
            displayOrder = c.DisplayOrder,
        }).ToList(),
        result = rule.Results.FirstOrDefault() is null ? null : new
        {
            id = rule.Results.First().Id,
            decision = rule.Results.First().Decision,
            message = rule.Results.First().Message,
        },
    });
});

app.MapPost("/api/rules", async (RuleCreateRequest request, GoRulesDbContext dbContext, GoRulesDecisionService decisionService) =>
{
    if (string.IsNullOrWhiteSpace(request.RuleName))
    {
        return Results.BadRequest("Rule name is required.");
    }

    var rule = new Rule
    {
        RuleName = request.RuleName,
        Description = request.Description,
        IsActive = request.IsActive,
        Conditions = request.Conditions.OrderBy(c => c.DisplayOrder).Select((condition, index) => new RuleCondition
        {
            LeftOperand = condition.LeftOperand,
            Operator = condition.Operator,
            RightOperand = condition.RightOperand,
            LogicalOperator = condition.LogicalOperator,
            DisplayOrder = index + 1,
        }).ToList(),
        Results = new List<RuleResult>(),
        DataSources =
        {
            new RuleDataSource { SourceType = "JSON", JsonSchema = "{\"type\":\"object\"}" },
        },
    };

    var defaultDecision = request.Result ?? new RuleResultModel { Decision = "Approve", Message = "Eligible for processing" };
    rule.Results.Add(new RuleResult
    {
        Decision = defaultDecision.Decision,
        Message = defaultDecision.Message,
    });

    dbContext.Rules.Add(rule);
    await dbContext.SaveChangesAsync();

    var item = new
    {
        id = rule.Id,
        ruleName = rule.RuleName,
        description = rule.Description,
        isActive = rule.IsActive,
        executionCount = rule.ExecutionCount,
        conditions = rule.Conditions.OrderBy(c => c.DisplayOrder).Select(c => new
        {
            id = c.Id,
            ruleId = c.RuleId,
            leftOperand = c.LeftOperand,
            @operator = c.Operator,
            rightOperand = c.RightOperand,
            logicalOperator = c.LogicalOperator,
            displayOrder = c.DisplayOrder,
        }).ToList(),
        result = new
        {
            id = rule.Results.First().Id,
            decision = rule.Results.First().Decision,
            message = rule.Results.First().Message,
        },
    };

    return Results.Created($"/api/rules/{rule.Id}", item);
});

app.MapPut("/api/rules/{id:int}", async (int id, RuleCreateRequest request, GoRulesDbContext dbContext) =>
{
    var rule = await dbContext.Rules
        .Include(r => r.Conditions)
        .Include(r => r.Results)
        .FirstOrDefaultAsync(r => r.Id == id);

    if (rule is null)
    {
        return Results.NotFound();
    }

    rule.RuleName = request.RuleName;
    rule.Description = request.Description;
    rule.IsActive = request.IsActive;

    dbContext.RuleConditions.RemoveRange(rule.Conditions);
    rule.Conditions = request.Conditions.OrderBy(c => c.DisplayOrder).Select((condition, index) => new RuleCondition
    {
        LeftOperand = condition.LeftOperand,
        Operator = condition.Operator,
        RightOperand = condition.RightOperand,
        LogicalOperator = condition.LogicalOperator,
        DisplayOrder = index + 1,
        Rule = rule,
    }).ToList();

    if (rule.Results.Any())
    {
        dbContext.RuleResults.RemoveRange(rule.Results);
    }

    var decision = request.Result ?? new RuleResultModel { Decision = "Approve", Message = "Eligible for processing" };
    rule.Results = new List<RuleResult>
    {
        new RuleResult
        {
            Decision = decision.Decision,
            Message = decision.Message,
            Rule = rule,
        },
    };

    await dbContext.SaveChangesAsync();
    return Results.Ok(new
    {
        id = rule.Id,
        ruleName = rule.RuleName,
        description = rule.Description,
        isActive = rule.IsActive,
        executionCount = rule.ExecutionCount,
        conditions = rule.Conditions.OrderBy(c => c.DisplayOrder).Select(c => new
        {
            id = c.Id,
            ruleId = c.RuleId,
            leftOperand = c.LeftOperand,
            @operator = c.Operator,
            rightOperand = c.RightOperand,
            logicalOperator = c.LogicalOperator,
            displayOrder = c.DisplayOrder,
        }).ToList(),
        result = new
        {
            id = rule.Results.First().Id,
            decision = rule.Results.First().Decision,
            message = rule.Results.First().Message,
        },
    });
});

app.MapDelete("/api/rules/{id:int}", async (int id, GoRulesDbContext dbContext) =>
{
    var rule = await dbContext.Rules
        .Include(r => r.Conditions)
        .Include(r => r.Results)
        .FirstOrDefaultAsync(r => r.Id == id);

    if (rule is null)
    {
        return Results.NotFound();
    }

    dbContext.Rules.Remove(rule);
    await dbContext.SaveChangesAsync();
    return Results.NoContent();
});

app.MapPost("/api/rules/execute", async (RuleExecutionRequest request, GoRulesDbContext dbContext, GoRulesDecisionService decisionService) =>
{
    var input = request.Input ?? new Dictionary<string, object>();

    if (request.RuleId is not null)
    {
        var rule = await dbContext.Rules
            .Include(r => r.Conditions)
            .Include(r => r.Results)
            .FirstOrDefaultAsync(r => r.Id == request.RuleId.Value && r.IsActive);

        if (rule is null)
        {
            return Results.NotFound();
        }

        var response = await decisionService.EvaluateSingleRuleAsync(rule, input);
        return Results.Ok(response);
    }

    var allRules = await dbContext.Rules
        .Include(r => r.Conditions)
        .Include(r => r.Results)
        .Where(r => r.IsActive)
        .ToListAsync();

    RuleExecutionResponse? foundResponse = null;

    foreach (var rule in allRules)
    {
        var response = await decisionService.EvaluateSingleRuleAsync(rule, input);
        if (response.Decision != "No Match" && response.Decision != "Reject")
        {
            foundResponse = response;
            break;
        }
    }

    if (foundResponse is null)
    {
        var fallback = await decisionService.EvaluateBestRuleAsync(input);
        return Results.Ok(fallback);
    }

    return Results.Ok(foundResponse);
});

app.MapPost("/api/rules/execute-all", async (RuleExecutionRequest request, GoRulesDbContext dbContext, GoRulesDecisionService decisionService) =>
{
    var input = request.Input ?? new Dictionary<string, object>();
    var activeRules = await dbContext.Rules
        .Include(r => r.Conditions)
        .Include(r => r.Results)
        .Where(r => r.IsActive)
        .OrderBy(r => r.Id)
        .ToListAsync();

    var selectedRules = request.RuleIds is { Count: > 0 }
        ? activeRules.Where(rule => request.RuleIds.Contains(rule.Id)).ToList()
        : activeRules;

    var batchResult = await decisionService.EvaluateSelectedRulesBatchAsync(selectedRules, input);
    return Results.Ok(batchResult);
});

app.Run();
