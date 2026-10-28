using FlowEngineDemo.Models;
using Microsoft.EntityFrameworkCore;

namespace FlowEngineDemo.Data;

public class FlowEngineDbContext : DbContext
{
    public FlowEngineDbContext(
        DbContextOptions<FlowEngineDbContext> options)
        : base(options)
    {
    }

    public DbSet<ApprovalHistory> ApprovalHistories =>
        Set<ApprovalHistory>();

    public DbSet<ApprovalTask> ApprovalTasks =>
        Set<ApprovalTask>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ApprovalTask>(entity =>
        {
            entity.ToTable("TB_APPROVAL_TASK", "dbo");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id)
                .HasColumnName("ID");

            entity.Property(x => x.FlowInstanceId)
                .HasColumnName("FLOW_INSTANCE_ID")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.BookmarkId)
                .HasColumnName("BOOKMARK_ID")
                .HasMaxLength(100)
                .IsRequired(false);

            entity.Property(x => x.FormType)
                .HasColumnName("FORM_TYPE")
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(x => x.FormId)
                .HasColumnName("FORM_ID")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.StepCode)
                .HasColumnName("STEP_CODE")
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(x => x.StepName)
                .HasColumnName("STEP_NAME")
                .HasMaxLength(100);

            entity.Property(x => x.ApprovalMode)
                .HasColumnName("APPROVAL_MODE")
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(x => x.ApproverId)
                .HasColumnName("APPROVER_ID")
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(x => x.Status)
                .HasColumnName("STATUS")
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(x => x.CreatedAt)
                .HasColumnName("CREATED_AT")
                .IsRequired();

            entity.Property(x => x.CompletedAt)
                .HasColumnName("COMPLETED_AT");
        });
        modelBuilder.Entity<ApprovalHistory>(entity =>
        {
            entity.ToTable("TB_APPROVAL_HISTORY", "dbo");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id)
                .HasColumnName("ID");

            entity.Property(x => x.FlowInstanceId)
                .HasColumnName("FLOW_INSTANCE_ID")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.FormType)
                .HasColumnName("FORM_TYPE")
                .HasMaxLength(50);

            entity.Property(x => x.FormId)
                .HasColumnName("FORM_ID")
                .HasMaxLength(100);

            entity.Property(x => x.Action)
                .HasColumnName("ACTION")
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(x => x.ApproverId)
                .HasColumnName("APPROVER_ID")
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(x => x.Comment)
                .HasColumnName("COMMENT")
                .HasMaxLength(1000);

            entity.Property(x => x.ActionAt)
                .HasColumnName("ACTION_AT")
                .IsRequired();
        });
    }
    
}