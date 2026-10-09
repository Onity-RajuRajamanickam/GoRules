using GoRules.Api.Models;

namespace GoRules.Api.Data;

public static class SeedData
{
    public static void Initialize(GoRulesDbContext context)
    {
        context.Rules.RemoveRange(context.Rules);
        context.SaveChanges();

        var rules = new List<Rule>
        {
            new()
            {
                RuleName = "Appraisal Amount Approved",
                Description = "Approve when the appraisal amount is at least $1,080,000 and the document status is successful.",
                IsActive = true,
                Conditions =
                {
                    new RuleCondition { LeftOperand = "PD_Appr_Appraisal_Amt", Operator = ">=", RightOperand = "1080000", LogicalOperator = "AND", DisplayOrder = 1 },
                    new RuleCondition { LeftOperand = "PD_Doc_Appraisal1_DocumentStatus", Operator = "==", RightOperand = "SUCCESSFUL", LogicalOperator = "AND", DisplayOrder = 2 },
                },
                Results =
                {
                    new RuleResult { Decision = "Approve", Message = "Appraisal meets policy value and document status requirements." },
                },
                DataSources =
                {
                    new RuleDataSource { SourceType = "JSON", JsonSchema = "{\"type\":\"object\"}" },
                },
            },
            new()
            {
                RuleName = "Property Location Approved",
                Description = "Approve when the collateral is in CA, Newhall, ZIP 91321.",
                IsActive = true,
                Conditions =
                {
                    new RuleCondition { LeftOperand = "PD_Doc_Prop_Address_State", Operator = "==", RightOperand = "CA", LogicalOperator = "AND", DisplayOrder = 1 },
                    new RuleCondition { LeftOperand = "PD_Doc_Prop_Address_City", Operator = "==", RightOperand = "NEWHALL", LogicalOperator = "AND", DisplayOrder = 2 },
                    new RuleCondition { LeftOperand = "PD_Doc_Prop_Address_Zip", Operator = "==", RightOperand = "91321", LogicalOperator = "AND", DisplayOrder = 3 },
                },
                Results =
                {
                    new RuleResult { Decision = "Approve", Message = "Collateral location is within the approved market area." },
                },
                DataSources =
                {
                    new RuleDataSource { SourceType = "JSON", JsonSchema = "{\"type\":\"object\"}" },
                },
            },
            new()
            {
                RuleName = "Appraiser License Valid",
                Description = "Approve when the appraiser certification is valid and active for the subject property.",
                IsActive = true,
                Conditions =
                {
                    new RuleCondition { LeftOperand = "PD_Appr_Appraiser_State_CertificationNumber", Operator = "==", RightOperand = "AR017444", LogicalOperator = "AND", DisplayOrder = 1 },
                    new RuleCondition { LeftOperand = "PD_Appr_Appraiser_ExpirationDateOfCertificationOrLicense", Operator = "==", RightOperand = "03/24/2026", LogicalOperator = "AND", DisplayOrder = 2 },
                },
                Results =
                {
                    new RuleResult { Decision = "Approve", Message = "Appraiser certification is active and valid." },
                },
                DataSources =
                {
                    new RuleDataSource { SourceType = "JSON", JsonSchema = "{\"type\":\"object\"}" },
                },
            },
            new()
            {
                RuleName = "Low Risk Collateral",
                Description = "Approve when collateral risk score indicates low underwriting risk.",
                IsActive = true,
                Conditions =
                {
                    new RuleCondition { LeftOperand = "PD_Doc_CollateralUnderwriting_RiskScore", Operator = "==", RightOperand = "1", LogicalOperator = "AND", DisplayOrder = 1 },
                    new RuleCondition { LeftOperand = "PD_Doc_DocFileID", Operator = "==", RightOperand = "110252D6CC", LogicalOperator = "AND", DisplayOrder = 2 },
                },
                Results =
                {
                    new RuleResult { Decision = "Approve", Message = "Risk score is within the approved low-risk threshold." },
                },
                DataSources =
                {
                    new RuleDataSource { SourceType = "JSON", JsonSchema = "{\"type\":\"object\"}" },
                },
            },
        };

        context.Rules.AddRange(rules);
        context.SaveChanges();
    }
}
