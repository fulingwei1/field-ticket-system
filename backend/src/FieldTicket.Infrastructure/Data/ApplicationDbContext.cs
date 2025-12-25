using FieldTicket.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FieldTicket.Infrastructure.Data;

/// <summary>
/// 应用数据库上下文
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Ticket> Tickets { get; set; } = null!;
    public DbSet<Attachment> Attachments { get; set; } = null!;
    public DbSet<PerformanceMetrics> PerformanceMetrics { get; set; } = null!;
    public DbSet<AiAnalysisResult> AiAnalysisResults { get; set; } = null!;
    public DbSet<JudgementCard> JudgementCards { get; set; } = null!;
    public DbSet<JudgementCardChangeLog> JudgementCardChangeLogs { get; set; } = null!;
    public DbSet<JudgementCardUsageHistory> JudgementCardUsageHistories { get; set; } = null!;
    public DbSet<DeviceChangeLog> DeviceChangeLogs { get; set; } = null!;
    public DbSet<DeviceConfigSnapshot> DeviceConfigSnapshots { get; set; } = null!;
    public DbSet<DuplicateDetectionLog> DuplicateDetectionLogs { get; set; } = null!;
    public DbSet<Solution> Solutions { get; set; } = null!;
    public DbSet<TriageNote> TriageNotes { get; set; } = null!;
    public DbSet<Verification> Verifications { get; set; } = null!;
    public DbSet<NotificationRule> NotificationRules { get; set; } = null!;
    public DbSet<NotificationLog> NotificationLogs { get; set; } = null!;
    public DbSet<DiagnosisConversation> DiagnosisConversations { get; set; } = null!;
    public DbSet<HypothesisVerificationStep> HypothesisVerificationSteps { get; set; } = null!;
    public DbSet<ConfidenceCalibrationRecord> ConfidenceCalibrationRecords { get; set; } = null!;
    public DbSet<ConfidenceCalibrationModel> ConfidenceCalibrationModels { get; set; } = null!;
    public DbSet<KnowledgeGraphNode> KnowledgeGraphNodes { get; set; } = null!;
    public DbSet<KnowledgeGraphEdge> KnowledgeGraphEdges { get; set; } = null!;
    public DbSet<KnowledgeVersion> KnowledgeVersions { get; set; } = null!;
    public DbSet<KnowledgeVersionRelation> KnowledgeVersionRelations { get; set; } = null!;
    public DbSet<UserProfile> UserProfiles { get; set; } = null!;
    public DbSet<UserFillingHistory> UserFillingHistories { get; set; } = null!;
    public DbSet<ThresholdConfig> ThresholdConfigs { get; set; } = null!;
    public DbSet<ThresholdTriggerHistory> ThresholdTriggerHistories { get; set; } = null!;
    public DbSet<TicketStatusHistory> TicketStatusHistories { get; set; } = null!;
    public DbSet<TicketTemplate> TicketTemplates { get; set; } = null!;
    public DbSet<CommunicationTemplate> CommunicationTemplates { get; set; } = null!;
    public DbSet<CustomerCommunication> CustomerCommunications { get; set; } = null!;
    public DbSet<CorrectiveAction> CorrectiveActions { get; set; } = null!;
    public DbSet<KnowledgeVerificationHistory> KnowledgeVerificationHistories { get; set; } = null!;
    public DbSet<JudgementCardRelation> JudgementCardRelations { get; set; } = null!;
    public DbSet<Project> Projects { get; set; } = null!;
    public DbSet<FieldProblem> FieldProblems { get; set; } = null!;
    public DbSet<RootCauseAnalysis> RootCauseAnalyses { get; set; } = null!;
    public DbSet<MissingInfoConversationHistory> MissingInfoConversationHistories { get; set; } = null!;
    public DbSet<EngineerLoadStat> EngineerLoadStats { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User 实体配置
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CorpId).HasColumnName("corp_id").HasMaxLength(64).IsRequired();
            entity.Property(e => e.WeComUserId).HasColumnName("wecom_userid").HasMaxLength(64).IsRequired();
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
            entity.Property(e => e.Mobile).HasColumnName("mobile").HasMaxLength(20);
            entity.Property(e => e.DeptId).HasColumnName("dept_id").HasMaxLength(64);
            entity.Property(e => e.Role).HasColumnName("role").HasMaxLength(20).IsRequired();
            entity.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").IsRequired();

            entity.HasIndex(e => new { e.CorpId, e.WeComUserId }).IsUnique();
        });

        // Ticket 实体配置
        modelBuilder.Entity<Ticket>(entity =>
        {
            entity.ToTable("tickets");
            entity.HasKey(e => e.TicketId);
            entity.Property(e => e.TicketId).HasColumnName("ticket_id");
            entity.Property(e => e.TicketNo).HasColumnName("ticket_no").HasMaxLength(20).IsRequired();
            entity.Property(e => e.CustomerId).HasColumnName("customer_id").IsRequired();
            entity.Property(e => e.ProjectId).HasColumnName("project_id").IsRequired();
            entity.Property(e => e.DeviceId).HasColumnName("device_id").IsRequired();
            entity.Property(e => e.StationId).HasColumnName("station_id");
            entity.Property(e => e.CreatedByUserId).HasColumnName("created_by_user_id").IsRequired();
            entity.Property(e => e.Domain).HasColumnName("domain").HasMaxLength(1).IsRequired();
            entity.Property(e => e.StepCode).HasColumnName("step_code").HasMaxLength(20).IsRequired();
            entity.Property(e => e.StepName).HasColumnName("step_name").HasMaxLength(100);
            entity.Property(e => e.SymptomTitle).HasColumnName("symptom_title").HasMaxLength(200).IsRequired();
            entity.Property(e => e.SymptomDetail).HasColumnName("symptom_detail");
            entity.Property(e => e.ReproRate).HasColumnName("repro_rate");
            entity.Property(e => e.RebootRecovers).HasColumnName("reboot_recovers");
            entity.Property(e => e.EnvRelated).HasColumnName("env_related");
            entity.Property(e => e.SwVersion).HasColumnName("sw_version").HasMaxLength(50).IsRequired();
            entity.Property(e => e.PlcVersion).HasColumnName("plc_version").HasMaxLength(50).IsRequired();
            entity.Property(e => e.ParamVersion).HasColumnName("param_version").HasMaxLength(50).IsRequired();
            entity.Property(e => e.FactsJson).HasColumnName("facts_json").HasColumnType("jsonb");
            entity.Property(e => e.ActionsTaken).HasColumnName("actions_taken").HasColumnType("text[]");
            entity.Property(e => e.ActionsTakenNote).HasColumnName("actions_taken_note");
            entity.Property(e => e.ConfirmedAsFact).HasColumnName("confirmed_as_fact").HasDefaultValue(false);
            entity.Property(e => e.ConfirmedAt).HasColumnName("confirmed_at");
            entity.Property(e => e.AlarmCode).HasColumnName("alarm_code").HasMaxLength(50);
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(20).IsRequired().HasDefaultValue("Draft");
            entity.Property(e => e.Priority).HasColumnName("priority").HasMaxLength(5).HasDefaultValue("P3");
            entity.Property(e => e.CurrentJcCode).HasColumnName("current_jc_code").HasMaxLength(20);
            entity.Property(e => e.AssignedTo).HasColumnName("assigned_to");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").IsRequired();
            entity.Property(e => e.SubmittedAt).HasColumnName("submitted_at");
            entity.Property(e => e.ClosedAt).HasColumnName("closed_at");
            entity.Property(e => e.LocalDraftId).HasColumnName("local_draft_id").HasMaxLength(64);
            entity.Property(e => e.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(64);
            
            // 责任归因字段（HR-004）
            entity.Property(e => e.RootCause).HasColumnName("root_cause");
            entity.Property(e => e.RootResponsibility).HasColumnName("root_responsibility").HasMaxLength(50);
            entity.Property(e => e.ResponsibilityTeam).HasColumnName("responsibility_team").HasMaxLength(50);
            entity.Property(e => e.IsPreventable).HasColumnName("is_preventable");
            entity.Property(e => e.ResponsibilityNotes).HasColumnName("responsibility_notes");
            entity.Property(e => e.AttributedBy).HasColumnName("attributed_by");
            entity.Property(e => e.AttributedAt).HasColumnName("attributed_at");

            entity.HasIndex(e => e.TicketNo).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.DeviceId);
            entity.HasIndex(e => e.CreatedByUserId);
            entity.HasIndex(e => e.Domain);
            entity.HasIndex(e => e.CreatedAt);
        });

        // Attachment 实体配置
        modelBuilder.Entity<Attachment>(entity =>
        {
            entity.ToTable("attachments");
            entity.HasKey(e => e.AttachmentId);
            entity.Property(e => e.AttachmentId).HasColumnName("attachment_id");
            entity.Property(e => e.TicketId).HasColumnName("ticket_id").IsRequired();
            entity.Property(e => e.UploadedBy).HasColumnName("uploaded_by").IsRequired();
            entity.Property(e => e.FileType).HasColumnName("file_type").HasMaxLength(20).IsRequired();
            entity.Property(e => e.FileName).HasColumnName("file_name").HasMaxLength(255).IsRequired();
            entity.Property(e => e.FileKey).HasColumnName("file_key").HasMaxLength(500).IsRequired();
            entity.Property(e => e.FileSize).HasColumnName("file_size").IsRequired();
            entity.Property(e => e.MimeType).HasColumnName("mime_type").HasMaxLength(100);
            entity.Property(e => e.Sha256).HasColumnName("sha256").HasMaxLength(64);
            entity.Property(e => e.UploadStatus).HasColumnName("upload_status").HasMaxLength(20).HasDefaultValue("completed");
            entity.Property(e => e.UploadId).HasColumnName("upload_id").HasMaxLength(200);
            entity.Property(e => e.UploadedChunks).HasColumnName("uploaded_chunks").HasDefaultValue(0);
            entity.Property(e => e.TotalChunks).HasColumnName("total_chunks");
            entity.Property(e => e.Tags).HasColumnName("tags");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();

            entity.HasIndex(e => e.TicketId);
        });

        // PerformanceMetrics 实体配置
        modelBuilder.Entity<PerformanceMetrics>(entity =>
        {
            entity.ToTable("performance_metrics");
            entity.HasKey(e => e.MetricId);
            entity.Property(e => e.MetricId).HasColumnName("metric_id");
            entity.Property(e => e.EngineerId).HasColumnName("engineer_id").IsRequired();
            entity.Property(e => e.PeriodType).HasColumnName("period_type").HasMaxLength(20).IsRequired();
            entity.Property(e => e.PeriodStart).HasColumnName("period_start").IsRequired();
            entity.Property(e => e.PeriodEnd).HasColumnName("period_end").IsRequired();
            
            // 工单相关指标
            entity.Property(e => e.TotalTickets).HasColumnName("total_tickets").HasDefaultValue(0);
            entity.Property(e => e.TicketsResolved).HasColumnName("tickets_resolved").HasDefaultValue(0);
            entity.Property(e => e.TicketsPending).HasColumnName("tickets_pending").HasDefaultValue(0);
            entity.Property(e => e.AverageResolutionTime).HasColumnName("average_resolution_time").HasColumnType("interval");
            entity.Property(e => e.FirstTimeResolutionRate).HasColumnName("first_time_resolution_rate").HasColumnType("decimal(5,2)");
            
            // 响应时间指标
            entity.Property(e => e.AverageResponseTime).HasColumnName("average_response_time").HasColumnType("interval");
            entity.Property(e => e.ResponseTimeP95).HasColumnName("response_time_p95").HasColumnType("interval");
            entity.Property(e => e.OnTimeResponseRate).HasColumnName("on_time_response_rate").HasColumnType("decimal(5,2)");
            
            // 设备故障率
            entity.Property(e => e.DevicesServiced).HasColumnName("devices_serviced").HasDefaultValue(0);
            entity.Property(e => e.DeviceFailureRate).HasColumnName("device_failure_rate").HasColumnType("decimal(5,2)");
            entity.Property(e => e.RepeatFailureRate).HasColumnName("repeat_failure_rate").HasColumnType("decimal(5,2)");
            
            // 工作活动完整性
            entity.Property(e => e.WorkActivityDays).HasColumnName("work_activity_days").HasDefaultValue(0);
            entity.Property(e => e.WorkActivityCompleteness).HasColumnName("work_activity_completeness").HasColumnType("decimal(5,2)");
            
            // 工单创建质量指标
            entity.Property(e => e.TicketCreationCompleteness).HasColumnName("ticket_creation_completeness").HasColumnType("decimal(5,2)");
            entity.Property(e => e.FieldFeedbackTimelinessRate).HasColumnName("field_feedback_timeliness_rate").HasColumnType("decimal(5,2)");
            entity.Property(e => e.QuestionReplyTimelinessRate).HasColumnName("question_reply_timeliness_rate").HasColumnType("decimal(5,2)");
            
            // 问题解决能力指标
            entity.Property(e => e.VerificationPassRate).HasColumnName("verification_pass_rate").HasColumnType("decimal(5,2)");
            entity.Property(e => e.RepeatProblemRate).HasColumnName("repeat_problem_rate").HasColumnType("decimal(5,2)");
            
            // 技术诊断能力指标
            entity.Property(e => e.JudgementCardUsageAccuracy).HasColumnName("judgement_card_usage_accuracy").HasColumnType("decimal(5,2)");
            entity.Property(e => e.JudgementCardHitRate).HasColumnName("judgement_card_hit_rate").HasColumnType("decimal(5,2)");
            entity.Property(e => e.AiSuggestionAdoptionRate).HasColumnName("ai_suggestion_adoption_rate").HasColumnType("decimal(5,2)");
            entity.Property(e => e.LowConfidenceUpgradeTimeliness).HasColumnName("low_confidence_upgrade_timeliness").HasColumnType("decimal(5,2)");
            
            // 知识贡献指标
            entity.Property(e => e.JudgementCardsCreated).HasColumnName("judgement_cards_created").HasDefaultValue(0);
            entity.Property(e => e.JudgementCardQualityScore).HasColumnName("judgement_card_quality_score").HasColumnType("decimal(5,2)");
            entity.Property(e => e.JudgementCardReuseContribution).HasColumnName("judgement_card_reuse_contribution").HasColumnType("decimal(5,2)");
            entity.Property(e => e.SolutionsContributed).HasColumnName("solutions_contributed").HasDefaultValue(0);
            
            // 客户服务能力指标
            entity.Property(e => e.CustomerCommunicationTimeliness).HasColumnName("customer_communication_timeliness").HasColumnType("decimal(5,2)");
            entity.Property(e => e.CustomerCommunicationQuality).HasColumnName("customer_communication_quality").HasColumnType("decimal(5,2)");
            entity.Property(e => e.CustomerSatisfactionScore).HasColumnName("customer_satisfaction_score").HasColumnType("decimal(3,2)");
            entity.Property(e => e.CustomerFeedbackCount).HasColumnName("customer_feedback_count").HasDefaultValue(0);
            
            // 协作能力指标
            entity.Property(e => e.TeamCollaborationActivity).HasColumnName("team_collaboration_activity").HasColumnType("decimal(5,2)");
            entity.Property(e => e.KnowledgeSharingContribution).HasColumnName("knowledge_sharing_contribution").HasColumnType("decimal(5,2)");
            
            // 工作规范性指标
            entity.Property(e => e.TicketInformationCompleteness).HasColumnName("ticket_information_completeness").HasColumnType("decimal(5,2)");
            entity.Property(e => e.RootCauseAttributionCompleteness).HasColumnName("root_cause_attribution_completeness").HasColumnType("decimal(5,2)");
            
            // 综合评分
            entity.Property(e => e.OverallScore).HasColumnName("overall_score").HasColumnType("decimal(5,2)");
            entity.Property(e => e.PerformanceLevel).HasColumnName("performance_level").HasMaxLength(20);
            
            // 排名
            entity.Property(e => e.RankInTeam).HasColumnName("rank_in_team");
            entity.Property(e => e.RankInDepartment).HasColumnName("rank_in_department");
            
            // 审计字段
            entity.Property(e => e.CalculatedAt).HasColumnName("calculated_at").HasDefaultValueSql("NOW()");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");
            
            // 外键和索引
            entity.HasOne(e => e.Engineer)
                .WithMany()
                .HasForeignKey(e => e.EngineerId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasIndex(e => new { e.EngineerId, e.PeriodType, e.PeriodStart }).IsUnique();
            entity.HasIndex(e => new { e.EngineerId, e.PeriodStart }).HasDatabaseName("idx_performance_engineer");
            entity.HasIndex(e => new { e.PeriodType, e.PeriodStart }).HasDatabaseName("idx_performance_period");
            entity.HasIndex(e => e.OverallScore).HasDatabaseName("idx_performance_score");
        });

        // AiAnalysisResult 实体配置
        modelBuilder.Entity<AiAnalysisResult>(entity =>
        {
            entity.ToTable("ai_analysis_results");
            entity.HasKey(e => e.AnalysisId);
            entity.Property(e => e.AnalysisId).HasColumnName("analysis_id");
            entity.Property(e => e.AnalysisType).HasColumnName("analysis_type").HasMaxLength(50).IsRequired();
            entity.Property(e => e.AnalysisDate).HasColumnName("analysis_date").IsRequired();
            entity.Property(e => e.EngineerId).HasColumnName("engineer_id");
            entity.Property(e => e.DepartmentId).HasColumnName("department_id");
            entity.Property(e => e.Summary).HasColumnName("summary").IsRequired();
            entity.Property(e => e.KeyInsights).HasColumnName("key_insights").HasColumnType("jsonb");
            entity.Property(e => e.Suggestions).HasColumnName("suggestions").HasColumnType("jsonb");
            entity.Property(e => e.PerformanceAnalysis).HasColumnName("performance_analysis").HasColumnType("jsonb");
            entity.Property(e => e.AiModel).HasColumnName("ai_model").HasMaxLength(100);
            entity.Property(e => e.ConfidenceScore).HasColumnName("confidence_score").HasColumnType("decimal(3,2)");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            
            // 外键和索引
            entity.HasOne(e => e.Engineer)
                .WithMany()
                .HasForeignKey(e => e.EngineerId)
                .OnDelete(DeleteBehavior.SetNull);
            
            entity.HasOne(e => e.Creator)
                .WithMany()
                .HasForeignKey(e => e.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull);
            
            entity.HasIndex(e => new { e.AnalysisType, e.AnalysisDate }).HasDatabaseName("idx_ai_analysis_type");
            entity.HasIndex(e => new { e.EngineerId, e.AnalysisDate }).HasDatabaseName("idx_ai_analysis_engineer");
        });

        // JudgementCard 实体配置
        modelBuilder.Entity<JudgementCard>(entity =>
        {
            entity.ToTable("judgement_cards");
            entity.HasKey(e => e.JudgementCardId);
            entity.Property(e => e.JudgementCardId).HasColumnName("judgement_card_id");
            entity.Property(e => e.JcCode).HasColumnName("jc_code").HasMaxLength(20).IsRequired();
            entity.Property(e => e.Title).HasColumnName("title").HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Domain).HasColumnName("domain").HasMaxLength(1).IsRequired();
            entity.Property(e => e.SymptomStructure).HasColumnName("symptom_structure").HasColumnType("jsonb");
            entity.Property(e => e.TroubleshootingPath).HasColumnName("troubleshooting_path").HasColumnType("jsonb");
            entity.Property(e => e.HypothesisTemplate).HasColumnName("hypothesis_template");
            entity.Property(e => e.NextActionTemplate).HasColumnName("next_action_template");
            entity.Property(e => e.EscalationConditions).HasColumnName("escalation_conditions").HasColumnType("jsonb");
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValue("Active");
            entity.Property(e => e.Version).HasColumnName("version").HasDefaultValue(1);
            entity.Property(e => e.ParentJcId).HasColumnName("parent_jc_id");
            entity.Property(e => e.IsCurrent).HasColumnName("is_current").HasDefaultValue(true);
            entity.Property(e => e.CreatedBy).HasColumnName("created_by").IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").IsRequired();
            entity.Property(e => e.UsageCount).HasColumnName("usage_count").HasDefaultValue(0);
            entity.Property(e => e.LastUsedAt).HasColumnName("last_used_at");
            entity.Property(e => e.ApplicableSwVersions).HasColumnName("applicable_sw_versions").HasColumnType("varchar(50)[]");
            entity.Property(e => e.ApplicableHwVersions).HasColumnName("applicable_hw_versions").HasColumnType("varchar(50)[]");
            entity.Property(e => e.ExpiryDate).HasColumnName("expiry_date");
            entity.Property(e => e.IsExpired).HasColumnName("is_expired").HasDefaultValue(false);
            entity.Property(e => e.SourceTicketId).HasColumnName("source_ticket_id");
            entity.Property(e => e.VerifiedBy).HasColumnName("verified_by");
            entity.Property(e => e.LastVerifiedAt).HasColumnName("last_verified_at");
            entity.Property(e => e.VerificationCount).HasColumnName("verification_count").HasDefaultValue(0);

            entity.HasIndex(e => e.JcCode).IsUnique();
            entity.HasIndex(e => e.Domain);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.IsExpired);
            entity.HasIndex(e => e.SourceTicketId);
        });

        // Solution 实体配置
        modelBuilder.Entity<Solution>(entity =>
        {
            entity.ToTable("solutions");
            entity.HasKey(e => e.SolutionId);
            entity.Property(e => e.SolutionId).HasColumnName("solution_id");
            entity.Property(e => e.SolutionCode).HasColumnName("solution_code").HasMaxLength(50);
            entity.Property(e => e.TicketId).HasColumnName("ticket_id").IsRequired();
            entity.Property(e => e.Title).HasColumnName("title").HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.SolutionType).HasColumnName("solution_type").HasMaxLength(50).IsRequired();
            entity.Property(e => e.ReleaseType).HasColumnName("release_type").HasMaxLength(20).IsRequired();
            entity.Property(e => e.RequiredSwVersion).HasColumnName("required_sw_version").HasMaxLength(50);
            entity.Property(e => e.RequiredPlcVersion).HasColumnName("required_plc_version").HasMaxLength(50);
            entity.Property(e => e.RequiredParamVersion).HasColumnName("required_param_version").HasMaxLength(50);
            entity.Property(e => e.NewSwVersion).HasColumnName("new_sw_version").HasMaxLength(50);
            entity.Property(e => e.NewPlcVersion).HasColumnName("new_plc_version").HasMaxLength(50);
            entity.Property(e => e.NewParamVersion).HasColumnName("new_param_version").HasMaxLength(50);
            entity.Property(e => e.ChangeDetailJson).HasColumnName("change_detail_json").HasColumnType("jsonb");
            entity.Property(e => e.VerificationChecklistJson).HasColumnName("verification_checklist_json").HasColumnType("jsonb");
            entity.Property(e => e.ImplementationSteps).HasColumnName("implementation_steps");
            entity.Property(e => e.EstimatedImplementationTime).HasColumnName("estimated_implementation_time");
            entity.Property(e => e.RiskLevel).HasColumnName("risk_level").HasMaxLength(20).HasDefaultValue("medium");
            entity.Property(e => e.RiskDescription).HasColumnName("risk_description");
            entity.Property(e => e.RollbackPossible).HasColumnName("rollback_possible").HasDefaultValue(true);
            entity.Property(e => e.RollbackProcedure).HasColumnName("rollback_procedure");
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValue("Draft");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by").IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").IsRequired();
            entity.Property(e => e.PublishedAt).HasColumnName("published_at");
            entity.Property(e => e.PublishedBy).HasColumnName("published_by");
            entity.Property(e => e.ApplicableSwVersions).HasColumnName("applicable_sw_versions").HasColumnType("varchar(50)[]");
            entity.Property(e => e.ApplicableHwVersions).HasColumnName("applicable_hw_versions").HasColumnType("varchar(50)[]");
            entity.Property(e => e.ExpiryDate).HasColumnName("expiry_date");
            entity.Property(e => e.IsExpired).HasColumnName("is_expired").HasDefaultValue(false);
            entity.Property(e => e.VerifiedBy).HasColumnName("verified_by");
            entity.Property(e => e.LastVerifiedAt).HasColumnName("last_verified_at");
            entity.Property(e => e.VerificationCount).HasColumnName("verification_count").HasDefaultValue(0);

            entity.HasIndex(e => e.SolutionCode).IsUnique();
            entity.HasIndex(e => e.TicketId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.IsExpired);
        });

        // TriageNote 实体配置
        modelBuilder.Entity<TriageNote>(entity =>
        {
            entity.ToTable("triage_notes");
            entity.HasKey(e => e.TriageNoteId);
            entity.Property(e => e.TriageNoteId).HasColumnName("triage_note_id");
            entity.Property(e => e.TicketId).HasColumnName("ticket_id").IsRequired();
            entity.Property(e => e.JcCode).HasColumnName("jc_code").HasMaxLength(20);
            entity.Property(e => e.CurrentHypothesis).HasColumnName("current_hypothesis");
            entity.Property(e => e.NextAction).HasColumnName("next_action");
            entity.Property(e => e.Confidence).HasColumnName("confidence").IsRequired();
            entity.Property(e => e.EscalationRequired).HasColumnName("escalation_required").HasDefaultValue(false);
            entity.Property(e => e.EscalatedTo).HasColumnName("escalated_to");
            entity.Property(e => e.Note).HasColumnName("note");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by").IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();

            entity.HasIndex(e => e.TicketId);
            entity.HasIndex(e => e.JcCode);
        });

        // JudgementCardChangeLog 实体配置
        modelBuilder.Entity<JudgementCardChangeLog>(entity =>
        {
            entity.ToTable("judgement_card_change_logs");
            entity.HasKey(e => e.ChangeLogId);
            entity.Property(e => e.ChangeLogId).HasColumnName("change_log_id");
            entity.Property(e => e.JcCode).HasColumnName("jc_code").HasMaxLength(20).IsRequired();
            entity.Property(e => e.Version).HasColumnName("version").IsRequired();
            entity.Property(e => e.PreviousVersionId).HasColumnName("previous_version_id");
            entity.Property(e => e.ChangeReason).HasColumnName("change_reason").IsRequired();
            entity.Property(e => e.ChangeSummary).HasColumnName("change_summary");
            entity.Property(e => e.ChangedBy).HasColumnName("changed_by").IsRequired();
            entity.Property(e => e.ChangedAt).HasColumnName("changed_at").IsRequired();
            entity.Property(e => e.IsOverturned).HasColumnName("is_overturned").HasDefaultValue(false);
            entity.Property(e => e.OverturnedBy).HasColumnName("overturned_by");
            entity.Property(e => e.OverturnedAt).HasColumnName("overturned_at");

            entity.HasIndex(e => e.JcCode);
            entity.HasIndex(e => new { e.JcCode, e.Version });
        });

        // JudgementCardUsageHistory 实体配置
        modelBuilder.Entity<JudgementCardUsageHistory>(entity =>
        {
            entity.ToTable("judgement_card_usage_history");
            entity.HasKey(e => e.UsageHistoryId);
            entity.Property(e => e.UsageHistoryId).HasColumnName("usage_history_id");
            entity.Property(e => e.JcCode).HasColumnName("jc_code").HasMaxLength(20).IsRequired();
            entity.Property(e => e.JcVersion).HasColumnName("jc_version").IsRequired();
            entity.Property(e => e.TicketId).HasColumnName("ticket_id").IsRequired();
            entity.Property(e => e.UsedBy).HasColumnName("used_by").IsRequired();
            entity.Property(e => e.UsedAt).HasColumnName("used_at").IsRequired();
            entity.Property(e => e.Result).HasColumnName("result").HasMaxLength(20);
            entity.Property(e => e.Feedback).HasColumnName("feedback");
            entity.Property(e => e.IsValid).HasColumnName("is_valid").HasDefaultValue(true);

            entity.HasIndex(e => e.JcCode);
            entity.HasIndex(e => new { e.JcCode, e.JcVersion });
            entity.HasIndex(e => e.TicketId);
        });

        // DeviceChangeLog 实体配置
        modelBuilder.Entity<DeviceChangeLog>(entity =>
        {
            entity.ToTable("device_change_logs");
            entity.HasKey(e => e.ChangeId);
            entity.Property(e => e.ChangeId).HasColumnName("change_id");
            entity.Property(e => e.DeviceId).HasColumnName("device_id").IsRequired();
            entity.Property(e => e.ChangeType).HasColumnName("change_type").HasMaxLength(50).IsRequired();
            entity.Property(e => e.ChangeDate).HasColumnName("change_date").IsRequired();
            entity.Property(e => e.ChangeDetail).HasColumnName("change_detail").HasColumnType("jsonb").IsRequired();
            entity.Property(e => e.ImpactScope).HasColumnName("impact_scope").HasColumnType("jsonb");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();

            entity.HasIndex(e => e.DeviceId);
            entity.HasIndex(e => e.ChangeDate);
            entity.HasIndex(e => new { e.DeviceId, e.ChangeDate });
        });

        // DeviceConfigSnapshot 实体配置
        modelBuilder.Entity<DeviceConfigSnapshot>(entity =>
        {
            entity.ToTable("device_config_snapshots");
            entity.HasKey(e => e.SnapshotId);
            entity.Property(e => e.SnapshotId).HasColumnName("snapshot_id");
            entity.Property(e => e.DeviceId).HasColumnName("device_id").IsRequired();
            entity.Property(e => e.SnapshotType).HasColumnName("snapshot_type").HasMaxLength(20).IsRequired();
            entity.Property(e => e.SnapshotAt).HasColumnName("snapshot_at").IsRequired();
            entity.Property(e => e.ConfigJson).HasColumnName("config_json").HasColumnType("jsonb").IsRequired();
            entity.Property(e => e.StandardConfigJson).HasColumnName("standard_config_json").HasColumnType("jsonb");
            entity.Property(e => e.Differences).HasColumnName("differences").HasColumnType("jsonb");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();

            entity.HasIndex(e => e.DeviceId);
            entity.HasIndex(e => e.SnapshotAt);
            entity.HasIndex(e => new { e.DeviceId, e.SnapshotAt });
            entity.HasIndex(e => new { e.DeviceId, e.SnapshotType });
        });

        // Verification 实体配置
        modelBuilder.Entity<Verification>(entity =>
        {
            entity.ToTable("verifications");
            entity.HasKey(e => e.VerificationId);
            entity.Property(e => e.VerificationId).HasColumnName("verification_id");
            entity.Property(e => e.TicketId).HasColumnName("ticket_id").IsRequired();
            entity.Property(e => e.SolutionId).HasColumnName("solution_id");
            entity.Property(e => e.ExecutedBy).HasColumnName("executed_by").IsRequired();
            entity.Property(e => e.RunCount).HasColumnName("run_count").IsRequired();
            entity.Property(e => e.PassCount).HasColumnName("pass_count").IsRequired();
            entity.Property(e => e.FailCount).HasColumnName("fail_count").IsRequired();
            entity.Property(e => e.Result).HasColumnName("result").HasMaxLength(20).IsRequired();
            entity.Property(e => e.ChecklistResultJson).HasColumnName("checklist_result_json").HasColumnType("jsonb");
            entity.Property(e => e.EvidenceAttachmentIds).HasColumnName("evidence_attachment_ids").HasColumnType("uuid[]");
            entity.Property(e => e.Note).HasColumnName("note");
            entity.Property(e => e.VerifiedAt).HasColumnName("verified_at").IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").IsRequired();

            // 外键和索引
            entity.HasOne(e => e.Ticket)
                .WithMany()
                .HasForeignKey(e => e.TicketId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Solution)
                .WithMany()
                .HasForeignKey(e => e.SolutionId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Executor)
                .WithMany()
                .HasForeignKey(e => e.ExecutedBy)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.TicketId);
            entity.HasIndex(e => e.SolutionId);
            entity.HasIndex(e => e.ExecutedBy);
            entity.HasIndex(e => e.Result);
            entity.HasIndex(e => e.VerifiedAt);
        });

        // NotificationRule 实体配置
        modelBuilder.Entity<NotificationRule>(entity =>
        {
            entity.ToTable("notification_rules");
            entity.HasKey(e => e.RuleId);
            entity.Property(e => e.RuleId).HasColumnName("rule_id");
            entity.Property(e => e.RuleLevel).HasColumnName("rule_level").HasMaxLength(20).IsRequired();
            entity.Property(e => e.CustomerId).HasColumnName("customer_id");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.DeviceId).HasColumnName("device_id");
            entity.Property(e => e.TriggerEvent).HasColumnName("trigger_event").HasMaxLength(50).IsRequired();
            entity.Property(e => e.RecipientsConfig).HasColumnName("recipients_config").HasColumnType("jsonb").IsRequired();
            entity.Property(e => e.TemplateOverride).HasColumnName("template_override");
            entity.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.Property(e => e.CreatedBy).HasColumnName("created_by").IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").IsRequired();

            entity.HasIndex(e => new { e.RuleLevel, e.CustomerId, e.ProjectId, e.DeviceId });
            entity.HasIndex(e => e.TriggerEvent);
            entity.HasIndex(e => e.IsActive);
        });

        // NotificationLog 实体配置
        modelBuilder.Entity<NotificationLog>(entity =>
        {
            entity.ToTable("notification_logs");
            entity.HasKey(e => e.LogId);
            entity.Property(e => e.LogId).HasColumnName("log_id");
            entity.Property(e => e.TicketId).HasColumnName("ticket_id");
            entity.Property(e => e.TriggerEvent).HasColumnName("trigger_event").HasMaxLength(50).IsRequired();
            entity.Property(e => e.RuleId).HasColumnName("rule_id");
            entity.Property(e => e.Recipients).HasColumnName("recipients").HasColumnType("jsonb").IsRequired();
            entity.Property(e => e.SentCount).HasColumnName("sent_count").HasDefaultValue(0);
            entity.Property(e => e.FailedCount).HasColumnName("failed_count").HasDefaultValue(0);
            entity.Property(e => e.MessageContent).HasColumnName("message_content");
            entity.Property(e => e.SentAt).HasColumnName("sent_at").IsRequired();

            entity.HasIndex(e => e.TicketId);
            entity.HasIndex(e => e.SentAt);
        });

        // DiagnosisConversation 实体配置
        modelBuilder.Entity<DiagnosisConversation>(entity =>
        {
            entity.ToTable("diagnosis_conversations");
            entity.HasKey(e => e.ConversationId);
            entity.Property(e => e.ConversationId).HasColumnName("conversation_id");
            entity.Property(e => e.TicketId).HasColumnName("ticket_id").IsRequired();
            entity.Property(e => e.CurrentHypothesis).HasColumnName("current_hypothesis");
            entity.Property(e => e.CurrentConfidence).HasColumnName("current_confidence").HasColumnType("decimal(3,2)");
            entity.Property(e => e.ConversationRound).HasColumnName("conversation_round").HasDefaultValue(0);
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValue("active");
            entity.Property(e => e.DiagnosisPathJson).HasColumnName("diagnosis_path").HasColumnType("jsonb");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");

            // 外键和索引
            entity.HasOne(e => e.Ticket)
                .WithMany()
                .HasForeignKey(e => e.TicketId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.TicketId).HasDatabaseName("idx_diagnosis_conversations_ticket");
            entity.HasIndex(e => e.Status).HasDatabaseName("idx_diagnosis_conversations_status");
        });

        // HypothesisVerificationStep 实体配置
        modelBuilder.Entity<HypothesisVerificationStep>(entity =>
        {
            entity.ToTable("hypothesis_verification_steps");
            entity.HasKey(e => e.StepId);
            entity.Property(e => e.StepId).HasColumnName("step_id");
            entity.Property(e => e.ConversationId).HasColumnName("conversation_id").IsRequired();
            entity.Property(e => e.HypothesisId).HasColumnName("hypothesis_id").HasMaxLength(50);
            entity.Property(e => e.StepDescription).HasColumnName("step_description").IsRequired();
            entity.Property(e => e.StepType).HasColumnName("step_type").HasMaxLength(50);
            entity.Property(e => e.ExpectedResult).HasColumnName("expected_result");
            entity.Property(e => e.ActualResult).HasColumnName("actual_result");
            entity.Property(e => e.VerificationStatus).HasColumnName("verification_status").HasMaxLength(20).HasDefaultValue("pending");
            entity.Property(e => e.VerificationNotes).HasColumnName("verification_notes");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");

            // 外键和索引
            entity.HasOne(e => e.Conversation)
                .WithMany(c => c.VerificationSteps)
                .HasForeignKey(e => e.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.ConversationId).HasDatabaseName("idx_verification_steps_conversation");
            entity.HasIndex(e => e.VerificationStatus).HasDatabaseName("idx_verification_steps_status");
        });

        // ConfidenceCalibrationRecord 实体配置
        modelBuilder.Entity<ConfidenceCalibrationRecord>(entity =>
        {
            entity.ToTable("confidence_calibration_records");
            entity.HasKey(e => e.RecordId);
            entity.Property(e => e.RecordId).HasColumnName("record_id");
            entity.Property(e => e.TicketId).HasColumnName("ticket_id").IsRequired();
            entity.Property(e => e.HypothesisId).HasColumnName("hypothesis_id").HasMaxLength(50);
            entity.Property(e => e.OriginalConfidence).HasColumnName("original_confidence").HasColumnType("decimal(3,2)").IsRequired();
            entity.Property(e => e.CalibratedConfidence).HasColumnName("calibrated_confidence").HasColumnType("decimal(3,2)");
            entity.Property(e => e.ActualResult).HasColumnName("actual_result").HasMaxLength(20);
            entity.Property(e => e.CalibrationMethod).HasColumnName("calibration_method").HasMaxLength(50);
            entity.Property(e => e.CalibrationFactorsJson).HasColumnName("calibration_factors").HasColumnType("jsonb");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");

            // 外键和索引
            entity.HasOne(e => e.Ticket)
                .WithMany()
                .HasForeignKey(e => e.TicketId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.TicketId).HasDatabaseName("idx_calibration_records_ticket");
            entity.HasIndex(e => e.HypothesisId).HasDatabaseName("idx_calibration_records_hypothesis");
            entity.HasIndex(e => e.CreatedAt).HasDatabaseName("idx_calibration_records_created");
        });

        // ConfidenceCalibrationModel 实体配置
        modelBuilder.Entity<ConfidenceCalibrationModel>(entity =>
        {
            entity.ToTable("confidence_calibration_models");
            entity.HasKey(e => e.ModelId);
            entity.Property(e => e.ModelId).HasColumnName("model_id");
            entity.Property(e => e.ModelVersion).HasColumnName("model_version").HasMaxLength(20).IsRequired();
            entity.Property(e => e.ModelType).HasColumnName("model_type").HasMaxLength(50).IsRequired();
            entity.Property(e => e.ModelParametersJson).HasColumnName("model_parameters").HasColumnType("jsonb").IsRequired();
            entity.Property(e => e.Accuracy).HasColumnName("accuracy").HasColumnType("decimal(5,4)");
            entity.Property(e => e.PrecisionScore).HasColumnName("precision_score").HasColumnType("decimal(5,4)");
            entity.Property(e => e.RecallScore).HasColumnName("recall_score").HasColumnType("decimal(5,4)");
            entity.Property(e => e.F1Score).HasColumnName("f1_score").HasColumnType("decimal(5,4)");
            entity.Property(e => e.TrainingDataCount).HasColumnName("training_data_count");
            entity.Property(e => e.TrainedAt).HasColumnName("trained_at");
            entity.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(false);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");

            entity.HasIndex(e => e.IsActive).HasDatabaseName("idx_calibration_models_active");
            entity.HasIndex(e => e.ModelVersion).HasDatabaseName("idx_calibration_models_version");
        });

        // UserProfile 实体配置
        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.ToTable("user_profiles");
            entity.HasKey(e => e.ProfileId);
            entity.Property(e => e.ProfileId).HasColumnName("profile_id");
            entity.Property(e => e.UserId).HasColumnName("user_id").IsRequired();
            entity.Property(e => e.CommonFields).HasColumnName("common_fields").HasColumnType("jsonb");
            entity.Property(e => e.ExpertiseLevel).HasColumnName("expertise_level").HasMaxLength(20);
            entity.Property(e => e.ExpertiseScore).HasColumnName("expertise_score").HasColumnType("decimal(3,2)");
            entity.Property(e => e.TotalTickets).HasColumnName("total_tickets").HasDefaultValue(0);
            entity.Property(e => e.AverageCompletionTime).HasColumnName("average_completion_time");
            entity.Property(e => e.CommonMistakes).HasColumnName("common_mistakes").HasColumnType("jsonb");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").IsRequired();

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.UserId).IsUnique();
        });

        // UserFillingHistory 实体配置
        modelBuilder.Entity<UserFillingHistory>(entity =>
        {
            entity.ToTable("user_filling_history");
            entity.HasKey(e => e.HistoryId);
            entity.Property(e => e.HistoryId).HasColumnName("history_id");
            entity.Property(e => e.UserId).HasColumnName("user_id").IsRequired();
            entity.Property(e => e.TicketId).HasColumnName("ticket_id").IsRequired();
            entity.Property(e => e.FilledFields).HasColumnName("filled_fields").HasColumnType("jsonb");
            entity.Property(e => e.FillingTime).HasColumnName("filling_time");
            entity.Property(e => e.SkippedFields).HasColumnName("skipped_fields").HasColumnType("jsonb");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Ticket)
                .WithMany()
                .HasForeignKey(e => e.TicketId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.TicketId);
            entity.HasIndex(e => e.CreatedAt);
        });

        // ThresholdConfig 实体配置
        modelBuilder.Entity<ThresholdConfig>(entity =>
        {
            entity.ToTable("threshold_configs");
            entity.HasKey(e => e.ConfigId);
            entity.Property(e => e.ConfigId).HasColumnName("config_id");
            entity.Property(e => e.ConfigName).HasColumnName("config_name").HasMaxLength(100).IsRequired();
            entity.Property(e => e.ScenarioType).HasColumnName("scenario_type").HasMaxLength(50).IsRequired();
            entity.Property(e => e.ScenarioValue).HasColumnName("scenario_value").HasMaxLength(100);
            entity.Property(e => e.TimeWindowDays).HasColumnName("time_window_days").HasDefaultValue(30);
            entity.Property(e => e.TriggerCount).HasColumnName("trigger_count").HasDefaultValue(3);
            entity.Property(e => e.MatchCriteria).HasColumnName("match_criteria").HasColumnType("jsonb");
            entity.Property(e => e.TriggerRate).HasColumnName("trigger_rate").HasColumnType("decimal(5,4)");
            entity.Property(e => e.AccuracyRate).HasColumnName("accuracy_rate").HasColumnType("decimal(5,4)");
            entity.Property(e => e.FalsePositiveRate).HasColumnName("false_positive_rate").HasColumnType("decimal(5,4)");
            entity.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.Property(e => e.IsAutoOptimized).HasColumnName("is_auto_optimized").HasDefaultValue(false);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").IsRequired();

            entity.HasIndex(e => new { e.ScenarioType, e.ScenarioValue });
            entity.HasIndex(e => e.IsActive);
        });

        // ThresholdTriggerHistory 实体配置
        modelBuilder.Entity<ThresholdTriggerHistory>(entity =>
        {
            entity.ToTable("threshold_trigger_history");
            entity.HasKey(e => e.HistoryId);
            entity.Property(e => e.HistoryId).HasColumnName("history_id");
            entity.Property(e => e.ConfigId).HasColumnName("config_id").IsRequired();
            entity.Property(e => e.TicketId).HasColumnName("ticket_id").IsRequired();
            entity.Property(e => e.TriggerTime).HasColumnName("trigger_time").IsRequired();
            entity.Property(e => e.MatchedTickets).HasColumnName("matched_tickets").HasColumnType("uuid[]");
            entity.Property(e => e.ActualResult).HasColumnName("actual_result").HasMaxLength(20);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();

            entity.HasOne(e => e.Config)
                .WithMany()
                .HasForeignKey(e => e.ConfigId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Ticket)
                .WithMany()
                .HasForeignKey(e => e.TicketId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.ConfigId);
            entity.HasIndex(e => e.TicketId);
            entity.HasIndex(e => e.TriggerTime);
        });

        // KnowledgeGraphNode 实体配置
        modelBuilder.Entity<KnowledgeGraphNode>(entity =>
        {
            entity.ToTable("knowledge_graph_nodes");
            entity.HasKey(e => e.NodeId);
            entity.Property(e => e.NodeId).HasColumnName("node_id");
            entity.Property(e => e.NodeType).HasColumnName("node_type").HasMaxLength(50).IsRequired();
            entity.Property(e => e.NodeCode).HasColumnName("node_code").HasMaxLength(50).IsRequired();
            entity.Property(e => e.NodeName).HasColumnName("node_name").HasMaxLength(200).IsRequired();
            entity.Property(e => e.PropertiesJson).HasColumnName("properties").HasColumnType("jsonb");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");

            entity.HasIndex(e => e.NodeType).HasDatabaseName("idx_kg_nodes_type");
            entity.HasIndex(e => e.NodeCode).HasDatabaseName("idx_kg_nodes_code");
        });

        // KnowledgeGraphEdge 实体配置
        modelBuilder.Entity<KnowledgeGraphEdge>(entity =>
        {
            entity.ToTable("knowledge_graph_edges");
            entity.HasKey(e => e.EdgeId);
            entity.Property(e => e.EdgeId).HasColumnName("edge_id");
            entity.Property(e => e.SourceNodeId).HasColumnName("source_node_id").IsRequired();
            entity.Property(e => e.TargetNodeId).HasColumnName("target_node_id").IsRequired();
            entity.Property(e => e.EdgeType).HasColumnName("edge_type").HasMaxLength(50).IsRequired();
            entity.Property(e => e.Weight).HasColumnName("weight").HasColumnType("decimal(3,2)").HasDefaultValue(1.0m);
            entity.Property(e => e.MetadataJson).HasColumnName("metadata").HasColumnType("jsonb");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");

            // 外键和索引
            entity.HasOne(e => e.SourceNode)
                .WithMany(n => n.OutgoingEdges)
                .HasForeignKey(e => e.SourceNodeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.TargetNode)
                .WithMany(n => n.IncomingEdges)
                .HasForeignKey(e => e.TargetNodeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.SourceNodeId).HasDatabaseName("idx_kg_edges_source");
            entity.HasIndex(e => e.TargetNodeId).HasDatabaseName("idx_kg_edges_target");
            entity.HasIndex(e => e.EdgeType).HasDatabaseName("idx_kg_edges_type");
            entity.HasIndex(e => new { e.SourceNodeId, e.TargetNodeId, e.EdgeType })
                .IsUnique()
                .HasDatabaseName("idx_kg_edges_unique");
        });

        // KnowledgeVersion 实体配置
        modelBuilder.Entity<KnowledgeVersion>(entity =>
        {
            entity.ToTable("knowledge_versions");
            entity.HasKey(e => e.VersionId);
            entity.Property(e => e.VersionId).HasColumnName("version_id");
            entity.Property(e => e.KnowledgeId).HasColumnName("knowledge_id").IsRequired();
            entity.Property(e => e.KnowledgeType).HasColumnName("knowledge_type").HasMaxLength(50).IsRequired();
            entity.Property(e => e.VersionNumber).HasColumnName("version_number").HasMaxLength(20).IsRequired();
            entity.Property(e => e.VersionDescription).HasColumnName("version_description");
            entity.Property(e => e.ContentJson).HasColumnName("content").HasColumnType("jsonb").IsRequired();
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
            entity.Property(e => e.IsCurrent).HasColumnName("is_current").HasDefaultValue(false);
            entity.Property(e => e.ChangeType).HasColumnName("change_type").HasMaxLength(50);
            entity.Property(e => e.ChangeReason).HasColumnName("change_reason");
            entity.Property(e => e.ChangeSummary).HasColumnName("change_summary");

            // 外键和索引
            entity.HasOne(e => e.Creator)
                .WithMany()
                .HasForeignKey(e => e.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(e => new { e.KnowledgeId, e.KnowledgeType }).HasDatabaseName("idx_kv_knowledge");
            entity.HasIndex(e => new { e.KnowledgeId, e.KnowledgeType, e.IsCurrent }).HasDatabaseName("idx_kv_current");
            entity.HasIndex(e => e.VersionNumber).HasDatabaseName("idx_kv_version_number");
        });

        // KnowledgeVersionRelation 实体配置
        modelBuilder.Entity<KnowledgeVersionRelation>(entity =>
        {
            entity.ToTable("knowledge_version_relations");
            entity.HasKey(e => e.RelationId);
            entity.Property(e => e.RelationId).HasColumnName("relation_id");
            entity.Property(e => e.SourceVersionId).HasColumnName("source_version_id").IsRequired();
            entity.Property(e => e.TargetVersionId).HasColumnName("target_version_id").IsRequired();
            entity.Property(e => e.RelationType).HasColumnName("relation_type").HasMaxLength(50).IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");

            // 外键和索引
            entity.HasOne(e => e.SourceVersion)
                .WithMany(v => v.SourceRelations)
                .HasForeignKey(e => e.SourceVersionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.TargetVersion)
                .WithMany(v => v.TargetRelations)
                .HasForeignKey(e => e.TargetVersionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.SourceVersionId).HasDatabaseName("idx_kvr_source");
            entity.HasIndex(e => e.TargetVersionId).HasDatabaseName("idx_kvr_target");
        });

        // TicketStatusHistory 实体配置
        modelBuilder.Entity<TicketStatusHistory>(entity =>
        {
            entity.ToTable("ticket_status_history");
            entity.HasKey(e => e.HistoryId);
            entity.Property(e => e.HistoryId).HasColumnName("history_id");
            entity.Property(e => e.TicketId).HasColumnName("ticket_id").IsRequired();
            entity.Property(e => e.FromStatus).HasColumnName("from_status").HasMaxLength(20).IsRequired();
            entity.Property(e => e.ToStatus).HasColumnName("to_status").HasMaxLength(20).IsRequired();
            entity.Property(e => e.ChangeReason).HasColumnName("change_reason");
            entity.Property(e => e.ChangedBy).HasColumnName("changed_by").IsRequired();
            entity.Property(e => e.ChangedByName).HasColumnName("changed_by_name").HasMaxLength(100);
            entity.Property(e => e.ChangedAt).HasColumnName("changed_at").IsRequired();
            entity.Property(e => e.ChangeType).HasColumnName("change_type").HasMaxLength(20).HasDefaultValue("manual");
            entity.Property(e => e.RelatedEntityId).HasColumnName("related_entity_id");
            entity.Property(e => e.RelatedEntityType).HasColumnName("related_entity_type").HasMaxLength(50);
            entity.Property(e => e.Notes).HasColumnName("notes");

            entity.HasIndex(e => e.TicketId).HasDatabaseName("idx_ticket_status_history_ticket");
            entity.HasIndex(e => e.ChangedAt).HasDatabaseName("idx_ticket_status_history_changed_at");
            entity.HasIndex(e => new { e.TicketId, e.ChangedAt }).HasDatabaseName("idx_ticket_status_history_ticket_time");
        });

        // TicketTemplate 实体配置
        modelBuilder.Entity<TicketTemplate>(entity =>
        {
            entity.ToTable("ticket_templates");
            entity.HasKey(e => e.TemplateId);
            entity.Property(e => e.TemplateId).HasColumnName("template_id");
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Category).HasColumnName("category").HasMaxLength(50);
            entity.Property(e => e.CreatedByUserId).HasColumnName("created_by_user_id").IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");
            entity.Property(e => e.UsageCount).HasColumnName("usage_count").HasDefaultValue(0);
            entity.Property(e => e.LastUsedAt).HasColumnName("last_used_at");
            entity.Property(e => e.IsPublic).HasColumnName("is_public").HasDefaultValue(false);
            entity.Property(e => e.TemplateData).HasColumnName("template_data").HasColumnType("jsonb");
            entity.Property(e => e.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");

            // 外键和索引
            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(e => e.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.CreatedByUserId).HasDatabaseName("idx_tt_created_by");
            entity.HasIndex(e => e.Category).HasDatabaseName("idx_tt_category");
            entity.HasIndex(e => new { e.IsDeleted, e.IsPublic }).HasDatabaseName("idx_tt_visibility");
        });

        // CommunicationTemplate 实体配置
        modelBuilder.Entity<CommunicationTemplate>(entity =>
        {
            entity.ToTable("comm_templates");
            entity.HasKey(e => e.TemplateId);
            entity.Property(e => e.TemplateId).HasColumnName("template_id");
            entity.Property(e => e.Code).HasColumnName("code").HasMaxLength(50).IsRequired();
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
            entity.Property(e => e.Scenario).HasColumnName("scenario").HasMaxLength(50).IsRequired();
            entity.Property(e => e.ContentTemplate).HasColumnName("content_template").IsRequired();
            entity.Property(e => e.Variables).HasColumnName("variables").HasColumnType("jsonb");
            entity.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");

            entity.HasIndex(e => e.Code).IsUnique().HasDatabaseName("idx_comm_template_code");
            entity.HasIndex(e => e.Scenario).HasDatabaseName("idx_comm_template_scenario");
            entity.HasIndex(e => e.IsActive).HasDatabaseName("idx_comm_template_active");
        });

        // CustomerCommunication 实体配置
        modelBuilder.Entity<CustomerCommunication>(entity =>
        {
            entity.ToTable("customer_comms");
            entity.HasKey(e => e.CommunicationId);
            entity.Property(e => e.CommunicationId).HasColumnName("id");
            entity.Property(e => e.TicketId).HasColumnName("ticket_id").IsRequired();
            entity.Property(e => e.TemplateId).HasColumnName("template_id");
            entity.Property(e => e.Content).HasColumnName("content").IsRequired();
            entity.Property(e => e.CommunicationType).HasColumnName("comm_type").HasMaxLength(20).IsRequired();
            entity.Property(e => e.CommunicatedBy).HasColumnName("by_user_id").IsRequired();
            entity.Property(e => e.CommunicatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
            entity.Property(e => e.CustomerFeedback).HasColumnName("customer_feedback");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");

            entity.HasIndex(e => e.TicketId).HasDatabaseName("idx_comms_ticket");
            entity.HasIndex(e => e.TemplateId).HasDatabaseName("idx_comms_template");
            entity.HasIndex(e => e.CommunicatedAt).HasDatabaseName("idx_comms_created_at");
        });

        // KnowledgeVerificationHistory 实体配置
        modelBuilder.Entity<KnowledgeVerificationHistory>(entity =>
        {
            entity.ToTable("knowledge_verification_histories");
            entity.HasKey(e => e.VerificationId);
            entity.Property(e => e.VerificationId).HasColumnName("verification_id");
            entity.Property(e => e.KnowledgeId).HasColumnName("knowledge_id").IsRequired();
            entity.Property(e => e.KnowledgeType).HasColumnName("knowledge_type").HasMaxLength(50).IsRequired();
            entity.Property(e => e.VerifiedBy).HasColumnName("verified_by").IsRequired();
            entity.Property(e => e.VerifiedAt).HasColumnName("verified_at").HasDefaultValueSql("NOW()");
            entity.Property(e => e.VerificationNote).HasColumnName("verification_note");

            entity.HasOne(e => e.Verifier)
                .WithMany()
                .HasForeignKey(e => e.VerifiedBy)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(e => new { e.KnowledgeId, e.KnowledgeType }).HasDatabaseName("idx_kvh_knowledge");
            entity.HasIndex(e => e.VerifiedAt).HasDatabaseName("idx_kvh_verified_at");
        });

        // JudgementCardRelation 实体配置
        modelBuilder.Entity<JudgementCardRelation>(entity =>
        {
            entity.ToTable("judgement_card_relations");
            entity.HasKey(e => e.RelationId);
            entity.Property(e => e.RelationId).HasColumnName("relation_id");
            entity.Property(e => e.SourceJcCode).HasColumnName("source_jc_code").HasMaxLength(20).IsRequired();
            entity.Property(e => e.TargetJcCode).HasColumnName("target_jc_code").HasMaxLength(20).IsRequired();
            entity.Property(e => e.RelationType).HasColumnName("relation_type").HasMaxLength(50).IsRequired();
            entity.Property(e => e.RelationStrength).HasColumnName("relation_strength").HasColumnType("decimal(3,2)").HasDefaultValue(0.5m);
            entity.Property(e => e.Metadata).HasColumnName("metadata").HasColumnType("jsonb");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");

            entity.HasOne(e => e.SourceJudgementCard)
                .WithMany()
                .HasForeignKey(e => e.SourceJcCode)
                .HasPrincipalKey(jc => jc.JcCode)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.TargetJudgementCard)
                .WithMany()
                .HasForeignKey(e => e.TargetJcCode)
                .HasPrincipalKey(jc => jc.JcCode)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.SourceJcCode).HasDatabaseName("idx_jc_relations_source");
            entity.HasIndex(e => e.TargetJcCode).HasDatabaseName("idx_jc_relations_target");
            entity.HasIndex(e => e.RelationType).HasDatabaseName("idx_jc_relations_type");
        });

        // EngineerLoadStat 实体配置
        modelBuilder.Entity<EngineerLoadStat>(entity =>
        {
            entity.ToTable("engineer_load_stats");
            entity.HasKey(e => e.StatId);
            entity.Property(e => e.StatId).HasColumnName("stat_id");
            entity.Property(e => e.EngineerId).HasColumnName("engineer_id").IsRequired();
            entity.Property(e => e.StatDate).HasColumnName("stat_date").IsRequired();
            entity.Property(e => e.MentionedCount).HasColumnName("mentioned_count").HasDefaultValue(0);
            entity.Property(e => e.EscalationTakenCount).HasColumnName("escalation_taken_count").HasDefaultValue(0);
            entity.Property(e => e.JudgementReusedCount).HasColumnName("judgement_reused_count").HasDefaultValue(0);
            entity.Property(e => e.LowConfidenceTakenCount).HasColumnName("low_confidence_taken_count").HasDefaultValue(0);
            entity.Property(e => e.TicketsAssigned).HasColumnName("tickets_assigned").HasDefaultValue(0);
            entity.Property(e => e.TicketsClosed).HasColumnName("tickets_closed").HasDefaultValue(0);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");

            entity.HasOne(e => e.Engineer)
                .WithMany()
                .HasForeignKey(e => e.EngineerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.EngineerId, e.StatDate }).IsUnique().HasDatabaseName("idx_els_engineer_date");
            entity.HasIndex(e => e.StatDate).HasDatabaseName("idx_els_stat_date");
        });

        // Project 实体配置
        modelBuilder.Entity<Project>(entity =>
        {
            entity.ToTable("projects");
            entity.HasKey(e => e.ProjectId);
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.ProjectNo).HasColumnName("project_no").HasMaxLength(50).IsRequired();
            entity.Property(e => e.ProjectName).HasColumnName("project_name").HasMaxLength(200).IsRequired();
            entity.Property(e => e.CustomerId).HasColumnName("customer_id").IsRequired();
            entity.Property(e => e.CustomerName).HasColumnName("customer_name").HasMaxLength(200);
            entity.Property(e => e.DeviceType).HasColumnName("device_type").HasMaxLength(50);
            entity.Property(e => e.IndustryType).HasColumnName("industry_type").HasMaxLength(50);
            entity.Property(e => e.SalesAmount).HasColumnName("sales_amount").HasColumnType("decimal(18,2)");
            entity.Property(e => e.Quantity).HasColumnName("quantity").HasDefaultValue(1);
            entity.Property(e => e.OrderDate).HasColumnName("order_date");
            entity.Property(e => e.RequiredDeliveryDate).HasColumnName("required_delivery_date");
            entity.Property(e => e.ActualDeliveryDate).HasColumnName("actual_delivery_date");
            entity.Property(e => e.DeliveryDelayDays).HasColumnName("delivery_delay_days");
            entity.Property(e => e.ProjectStatus).HasColumnName("project_status").HasMaxLength(50).HasDefaultValue("进行中");
            entity.Property(e => e.ProjectManagerId).HasColumnName("project_manager_id");
            entity.Property(e => e.ProjectManagerName).HasColumnName("project_manager_name").HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");

            entity.HasIndex(e => e.ProjectNo).IsUnique().HasDatabaseName("idx_projects_project_no");
            entity.HasIndex(e => e.CustomerId).HasDatabaseName("idx_projects_customer_id");
            entity.HasIndex(e => e.ProjectStatus).HasDatabaseName("idx_projects_project_status");
        });

        // FieldProblem 实体配置
        modelBuilder.Entity<FieldProblem>(entity =>
        {
            entity.ToTable("field_problems");
            entity.HasKey(e => e.ProblemId);
            entity.Property(e => e.ProblemId).HasColumnName("problem_id");
            entity.Property(e => e.ProjectId).HasColumnName("project_id").IsRequired();
            entity.Property(e => e.ProblemSequence).HasColumnName("problem_sequence").IsRequired();
            entity.Property(e => e.ProblemCategory).HasColumnName("problem_category").HasMaxLength(50).IsRequired();
            entity.Property(e => e.ProblemDescription).HasColumnName("problem_description").IsRequired();
            entity.Property(e => e.Priority).HasColumnName("priority").HasMaxLength(10);
            entity.Property(e => e.FoundDate).HasColumnName("found_date").IsRequired();
            entity.Property(e => e.CompletedDate).HasColumnName("completed_date");
            entity.Property(e => e.ProcessingDays).HasColumnName("processing_days");
            entity.Property(e => e.PrimaryDepartment).HasColumnName("primary_department").HasMaxLength(100).IsRequired();
            entity.Property(e => e.PrimaryResponsible).HasColumnName("primary_responsible").HasMaxLength(100).IsRequired();
            entity.Property(e => e.PrimaryResponsibleId).HasColumnName("primary_responsible_id");
            entity.Property(e => e.CollaboratingDepartment).HasColumnName("collaborating_department").HasMaxLength(100);
            entity.Property(e => e.CollaboratingPerson).HasColumnName("collaborating_person").HasMaxLength(100);
            entity.Property(e => e.CollaboratingPersonId).HasColumnName("collaborating_person_id");
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(50).HasDefaultValue("待分配");
            entity.Property(e => e.Solution).HasColumnName("solution");
            entity.Property(e => e.SolutionDetails).HasColumnName("solution_details");
            entity.Property(e => e.VerificationStatus).HasColumnName("verification_status").HasMaxLength(50);
            entity.Property(e => e.CustomerFeedback).HasColumnName("customer_feedback");
            entity.Property(e => e.SatisfactionScore).HasColumnName("satisfaction_score");
            entity.Property(e => e.VerifiedAt).HasColumnName("verified_at");
            entity.Property(e => e.VerifiedBy).HasColumnName("verified_by").HasMaxLength(100);
            entity.Property(e => e.RelatedTicketId).HasColumnName("related_ticket_id");
            entity.Property(e => e.RelatedTicketNo).HasColumnName("related_ticket_no").HasMaxLength(50);
            entity.Property(e => e.KnowledgeBaseId).HasColumnName("knowledge_base_id").HasMaxLength(100);
            entity.Property(e => e.IsRepeatProblem).HasColumnName("is_repeat_problem").HasDefaultValue(false);
            entity.Property(e => e.RelatedHistoryProblemId).HasColumnName("related_history_problem_id");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");

            entity.HasOne(e => e.Project)
                .WithMany(p => p.Problems)
                .HasForeignKey(e => e.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.ProjectId, e.ProblemSequence }).IsUnique().HasDatabaseName("idx_field_problems_project_sequence");
            entity.HasIndex(e => e.ProjectId).HasDatabaseName("idx_field_problems_project_id");
            entity.HasIndex(e => e.Status).HasDatabaseName("idx_field_problems_status");
            entity.HasIndex(e => e.ProblemCategory).HasDatabaseName("idx_field_problems_category");
            entity.HasIndex(e => e.PrimaryResponsibleId).HasDatabaseName("idx_field_problems_primary_responsible_id");
        });

        // RootCauseAnalysis 实体配置
        modelBuilder.Entity<RootCauseAnalysis>(entity =>
        {
            entity.ToTable("root_cause_analyses");
            entity.HasKey(e => e.AnalysisId);
            entity.Property(e => e.AnalysisId).HasColumnName("analysis_id");
            entity.Property(e => e.ProblemId).HasColumnName("problem_id").IsRequired();
            entity.Property(e => e.Why1).HasColumnName("why1");
            entity.Property(e => e.Why2).HasColumnName("why2");
            entity.Property(e => e.Why3).HasColumnName("why3");
            entity.Property(e => e.Why4).HasColumnName("why4");
            entity.Property(e => e.Why5).HasColumnName("why5");
            entity.Property(e => e.RootCause).HasColumnName("root_cause").IsRequired();
            entity.Property(e => e.RootCauseCategory).HasColumnName("root_cause_category").HasMaxLength(50);
            entity.Property(e => e.PreventiveMeasures).HasColumnName("preventive_measures");
            entity.Property(e => e.VerificationMethod).HasColumnName("verification_method");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");
            entity.Property(e => e.AnalyzedBy).HasColumnName("analyzed_by");
            entity.Property(e => e.AnalyzedAt).HasColumnName("analyzed_at");

            entity.HasOne(e => e.Problem)
                .WithOne(p => p.RootCauseAnalysis)
                .HasForeignKey<RootCauseAnalysis>(e => e.ProblemId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.ProblemId).IsUnique().HasDatabaseName("idx_root_cause_analyses_problem_id");
        });

        // CorrectiveAction 实体配置
        modelBuilder.Entity<CorrectiveAction>(entity =>
        {
            entity.ToTable("corrective_actions");
            entity.HasKey(e => e.ActionId);
            entity.Property(e => e.ActionId).HasColumnName("action_id");
            entity.Property(e => e.ActionCode).HasColumnName("action_code").HasMaxLength(50).IsRequired();
            entity.Property(e => e.TriggerType).HasColumnName("trigger_type").HasMaxLength(50).IsRequired();
            entity.Property(e => e.TriggerRule).HasColumnName("trigger_rule").HasColumnType("jsonb");
            entity.Property(e => e.RelatedTicketIds).HasColumnName("related_ticket_ids").HasColumnType("uuid[]").IsRequired();
            entity.Property(e => e.ProblemDescription).HasColumnName("problem_description").IsRequired();
            entity.Property(e => e.RootResponsibility).HasColumnName("root_responsibility").HasMaxLength(50);
            entity.Property(e => e.ActionPlan).HasColumnName("action_plan").IsRequired();
            entity.Property(e => e.ResponsiblePersonId).HasColumnName("responsible_person_id");
            entity.Property(e => e.TargetCompletionDate).HasColumnName("target_completion_date");
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValue("open");
            entity.Property(e => e.ExecutionNotes).HasColumnName("execution_notes");
            entity.Property(e => e.CompletedAt).HasColumnName("completed_at");
            entity.Property(e => e.CompletedBy).HasColumnName("completed_by");
            entity.Property(e => e.EffectivenessCheck).HasColumnName("effectiveness_check").HasColumnType("jsonb");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");

            entity.HasIndex(e => e.ActionCode).IsUnique().HasDatabaseName("idx_corrective_action_code");
            entity.HasIndex(e => e.Status).HasDatabaseName("idx_corrective_actions_status");
            entity.HasIndex(e => e.ResponsiblePersonId).HasDatabaseName("idx_corrective_actions_responsible");
        });
    }
}

