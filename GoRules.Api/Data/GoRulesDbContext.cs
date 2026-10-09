using GoRules.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GoRules.Api.Data;

public class GoRulesDbContext : DbContext
{
    public GoRulesDbContext(DbContextOptions<GoRulesDbContext> options) : base(options)
    {
    }

    public DbSet<Rule> Rules => Set<Rule>();
    public DbSet<RuleCondition> RuleConditions => Set<RuleCondition>();
    public DbSet<RuleResult> RuleResults => Set<RuleResult>();
    public DbSet<RuleDataSource> RuleDataSources => Set<RuleDataSource>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Rule>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.RuleName).HasMaxLength(200).IsRequired();
            entity.Property(r => r.Description).HasMaxLength(1000);
            entity.HasMany(r => r.Conditions).WithOne(c => c.Rule).HasForeignKey(c => c.RuleId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(r => r.Results).WithOne(r => r.Rule).HasForeignKey(r => r.RuleId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(r => r.DataSources).WithOne(d => d.Rule).HasForeignKey(d => d.RuleId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RuleCondition>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.LeftOperand).HasMaxLength(200).IsRequired();
            entity.Property(c => c.Operator).HasMaxLength(20).IsRequired();
            entity.Property(c => c.RightOperand).HasMaxLength(200).IsRequired();
            entity.Property(c => c.LogicalOperator).HasMaxLength(10).IsRequired();
        });

        modelBuilder.Entity<RuleResult>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Decision).HasMaxLength(200).IsRequired();
            entity.Property(r => r.Message).HasMaxLength(500).IsRequired();
        });

        modelBuilder.Entity<RuleDataSource>(entity =>
        {
            entity.HasKey(d => d.Id);
            entity.Property(d => d.SourceType).HasMaxLength(50).IsRequired();
            entity.Property(d => d.SqlQuery).HasMaxLength(4000);
            entity.Property(d => d.JsonSchema).HasMaxLength(4000);
        });
    }
}
