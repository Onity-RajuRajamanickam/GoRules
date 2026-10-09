# GoRules Rule Engine POC

This repository contains a small .NET + React proof of concept for integrating the GoRules ZEN Engine into an application and evaluating business rules dynamically.

## What this demo does

- Stores business rules in a lightweight data model
- Builds a GoRules decision graph from each rule
- Evaluates the incoming JSON payload against active rules
- Exposes the rule engine through a REST API
- Provides a React UI to create, edit, and execute rules

## ZEN Engine guide

For a full explanation of how the GoRules ZEN Engine is used in this repo, what request it expects, how single-rule and multi-rule execution works, and when to use it instead of handwritten logic, see [docs/GoRules-ZEN-Engine-Guide.md](docs/GoRules-ZEN-Engine-Guide.md).

## Why GoRules.ZenEngine is useful here

GoRules is useful when business policy changes more often than code changes. In a typical application, rules like credit thresholds, review conditions, approval decisions, premium calculations, or fraud checks become mixed into application logic, which makes them hard to change without redeploying code.

With GoRules, the rule logic is modeled as a decision structure and evaluated at runtime. That gives you:

- business rules that can be changed without a code release
- a clearer separation between application plumbing and policy logic
- reusable rule definitions for different scenarios or customers
- a visual/structured way to reason about decisions compared with nested if/else logic

This demo is intentionally small, but it shows the same pattern you would use in a real enterprise system.

## How this differs from writing rules directly in code

If you write the rule directly in C# or JavaScript, it often looks like this:

```csharp
if (creditScore > 700 && debtRatio < 40)
{
    return "Approve";
}

if (loanAmount > 500000)
{
    return "Review";
}
```

That works for a fixed implementation, but it has drawbacks:

- the business logic is buried inside application code
- changing policy requires a code deployment and regression testing
- people who understand business policy may not be comfortable editing code
- the rules are harder to trace, version, and audit

GoRules makes the rule payload explicit and runtime-driven. Instead of embedding decisions in compiled code, the application sends a structured decision document to the engine. The engine evaluates that document against the runtime input and returns a result object.

This structure is especially valuable when rules are:

- reviewed by business users
- updated frequently
- stored in a database or admin form
- validated and audited separately from deployment pipelines

## What the UI shows

The React UI shows the exact data flow:

1. The user enters input JSON in the Execute Rule screen.
2. The app builds a GoRules decision payload from the selected rule.
3. The app sends that payload to the GoRules engine.
4. The engine evaluates the payload against the runtime context.
5. The UI displays both the generated request and the returned decision output.

This lets you inspect the exact payload that GoRules receives and the exact response JSON it returns, without needing to open the underlying engine internals.

## Tech stack

- .NET 10 Web API
- GoRules.ZenEngine
- EF Core with an in-memory database by default
- React + Vite

## Solution structure

- `GoRules.Api` – REST API and rule engine integration
- `GoRules.React` – React front-end demo
- `GoRules.Poc.slnx` – solution file

## Run the API

From the repository root:

```powershell
cd GoRules.Api
dotnet restore
dotnet run --urls http://localhost:5147
```

The API starts on:

- http://localhost:5147

## Run the UI

From the repository root:

```powershell
cd GoRules.React
npm install
npm run dev
```

The React app starts on:

- http://localhost:5173

## API endpoints

### Dashboard

```http
GET http://localhost:5147/api/dashboard
```

### List all rules

```http
GET http://localhost:5147/api/rules
```

### Get one rule

```http
GET http://localhost:5147/api/rules/1
```

### Create a rule

```http
POST http://localhost:5147/api/rules
Content-Type: application/json
```

```json
{
  "ruleName": "High Risk Applicant",
  "description": "Reject applicants with high debt and low credit score.",
  "isActive": true,
  "conditions": [
    {
      "leftOperand": "CreditScore",
      "operator": ">",
      "rightOperand": "650",
      "logicalOperator": "AND",
      "displayOrder": 1
    },
    {
      "leftOperand": "DebtRatio",
      "operator": ">",
      "rightOperand": "40",
      "logicalOperator": "AND",
      "displayOrder": 2
    }
  ],
  "result": {
    "decision": "Reject",
    "message": "Applicant exceeds policy thresholds"
  }
}
```

### Update a rule

```http
PUT http://localhost:5147/api/rules/1
Content-Type: application/json
```

### Delete a rule

```http
DELETE http://localhost:5147/api/rules/1
```

### Execute a rule or all active rules

```http
POST http://localhost:5147/api/rules/execute
Content-Type: application/json
```

```json
{
  "ruleId": 1,
  "input": {
    "CreditScore": 720,
    "LoanAmount": 300000,
    "DebtRatio": 35
  }
}
```

Or evaluate all active rules:

```json
{
  "input": {
    "CreditScore": 720,
    "LoanAmount": 300000,
    "DebtRatio": 35
  }
}
```

## Notes

- The app uses an in-memory database by default for local experimentation.
- The GoRules engine evaluates a generated decision graph rather than a flat custom rule object.
- This project is intended as a learning/demo project rather than a production-ready rules engine.
