using FieldTicket.Api.Endpoints;
using FieldTicket.Core.Services;
using FieldTicket.Infrastructure.Auth;
using FieldTicket.Infrastructure.Data;
using FieldTicket.Infrastructure.Services;
using FieldTicket.Infrastructure.WeCom;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Field Ticket API",
        Version = "v1",
        Description = "现场问题结构化上报系统 API"
    });

    // JWT Bearer 认证配置
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// 配置企业微信选项
builder.Services.Configure<WeComOptions>(
    builder.Configuration.GetSection(WeComOptions.SectionName));

// 配置 MinIO 选项
builder.Services.Configure<FieldTicket.Infrastructure.Storage.MinIOOptions>(
    builder.Configuration.GetSection(FieldTicket.Infrastructure.Storage.MinIOOptions.SectionName));

// 配置数据库
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions => npgsqlOptions.MigrationsAssembly("FieldTicket.Infrastructure")
    ));

// 配置 Redis
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "FieldTicket:";
});

// 配置 JWT 认证
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? throw new InvalidOperationException("JWT Secret not configured");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "field-ticket-api";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// 注册服务
builder.Services.AddHttpClient();
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddScoped<WeComUserService>();
builder.Services.AddScoped<FieldTicket.Core.Services.IWeComNotificationService, FieldTicket.Infrastructure.WeCom.WeComNotificationService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<TicketNumberService>();
builder.Services.AddScoped<SolutionNumberService>();
builder.Services.AddScoped<FieldTicket.Core.Validators.TicketValidator>();
builder.Services.AddScoped<ITicketService, FieldTicket.Infrastructure.Services.TicketService>();
builder.Services.AddScoped<ITriageService, FieldTicket.Infrastructure.Services.TriageService>();
builder.Services.AddScoped<ISolutionService, FieldTicket.Infrastructure.Services.SolutionService>();
builder.Services.AddScoped<IVerificationService, FieldTicket.Infrastructure.Services.VerificationService>();
builder.Services.AddScoped<IPerformanceService, FieldTicket.Infrastructure.Services.PerformanceService>();
builder.Services.AddScoped<IAiAnalysisService, FieldTicket.Infrastructure.Services.AiAnalysisService>();
builder.Services.AddScoped<IConversationalDiagnosisService, FieldTicket.Infrastructure.Services.ConversationalDiagnosisService>();
builder.Services.AddScoped<IMissingInfoAnalysisService, FieldTicket.Infrastructure.Services.MissingInfoAnalysisService>();
builder.Services.AddScoped<IAIDeepAnalysisService, FieldTicket.Infrastructure.Services.AIDeepAnalysisService>();

// 注册LLM服务
builder.Services.AddHttpClient<FieldTicket.Core.Services.ILLMService, FieldTicket.Infrastructure.LLM.OpenAIService>();
builder.Services.AddScoped<IConfidenceCalibrationService, FieldTicket.Infrastructure.Services.ConfidenceCalibrationService>();
builder.Services.AddScoped<IAIAttributionService, FieldTicket.Infrastructure.Services.AIAttributionService>();
builder.Services.AddScoped<IKnowledgeGraphService, FieldTicket.Infrastructure.Services.KnowledgeGraphService>();
builder.Services.AddScoped<IKnowledgeVersionService, FieldTicket.Infrastructure.Services.KnowledgeVersionService>();
builder.Services.AddScoped<IJudgementCardRecommendationService, FieldTicket.Infrastructure.Services.JudgementCardRecommendationService>();
builder.Services.AddScoped<IUserProfileService, FieldTicket.Infrastructure.Services.UserProfileService>();
builder.Services.AddScoped<ISmartThresholdService, FieldTicket.Infrastructure.Services.SmartThresholdService>();
builder.Services.AddScoped<FieldTicket.Infrastructure.Storage.IMinIOService, FieldTicket.Infrastructure.Storage.MinIOService>();
builder.Services.AddScoped<IAttachmentService, FieldTicket.Infrastructure.Services.AttachmentService>();
builder.Services.AddScoped<INotificationRuleService, FieldTicket.Infrastructure.Services.NotificationRuleService>();
builder.Services.AddScoped<IJudgementCardQualityService, FieldTicket.Infrastructure.Services.JudgementCardQualityService>();
builder.Services.AddScoped<IJudgementCardVersionService, FieldTicket.Infrastructure.Services.JudgementCardVersionService>();
builder.Services.AddScoped<IRecentChangeAssociationService, FieldTicket.Infrastructure.Services.RecentChangeAssociationService>();
builder.Services.AddScoped<IDeviceConfigSnapshotService, FieldTicket.Infrastructure.Services.DeviceConfigSnapshotService>();
builder.Services.AddScoped<IEngineerLoadStatService, FieldTicket.Infrastructure.Services.EngineerLoadStatService>();
builder.Services.AddScoped<IStatisticsService, FieldTicket.Infrastructure.Services.StatisticsService>();
builder.Services.AddScoped<IAIAssistedTriageService, FieldTicket.Infrastructure.Services.AIAssistedTriageService>();
builder.Services.AddScoped<ITicketStatusHistoryService, FieldTicket.Infrastructure.Services.TicketStatusHistoryService>();
builder.Services.AddScoped<ITicketAssociationService, FieldTicket.Infrastructure.Services.TicketAssociationService>();
builder.Services.AddScoped<ITicketTemplateService, FieldTicket.Infrastructure.Services.TicketTemplateService>();
builder.Services.AddScoped<ITicketBatchService, FieldTicket.Infrastructure.Services.TicketBatchService>();
builder.Services.AddScoped<ITicketExportService, FieldTicket.Infrastructure.Services.TicketExportService>();
builder.Services.AddScoped<IDeviceService, FieldTicket.Infrastructure.Services.DeviceService>();
builder.Services.AddScoped<ICommunicationTemplateService, FieldTicket.Infrastructure.Services.CommunicationTemplateService>();
builder.Services.AddScoped<ITicketSearchService, FieldTicket.Infrastructure.Services.TicketSearchService>();
builder.Services.AddScoped<IStatisticsExportService, FieldTicket.Infrastructure.Services.StatisticsExportService>();
builder.Services.AddScoped<IResponsibilityAttributionService, FieldTicket.Infrastructure.Services.ResponsibilityAttributionService>();
builder.Services.AddScoped<ICorrectiveActionTriggerService, FieldTicket.Infrastructure.Services.CorrectiveActionTriggerService>();
builder.Services.AddScoped<ICorrectiveActionService, FieldTicket.Infrastructure.Services.CorrectiveActionService>();
builder.Services.AddScoped<IKnowledgeValidityService, FieldTicket.Infrastructure.Services.KnowledgeValidityService>();
builder.Services.AddScoped<IKnowledgeSourceTraceService, FieldTicket.Infrastructure.Services.KnowledgeSourceTraceService>();
builder.Services.AddScoped<IJudgementCardRelationService, FieldTicket.Infrastructure.Services.JudgementCardRelationService>();
builder.Services.AddScoped<INewcomerGrowthService, FieldTicket.Infrastructure.Services.NewcomerGrowthService>();
builder.Services.AddScoped<IQRCodeService, FieldTicket.Infrastructure.Services.QRCodeService>();
builder.Services.AddScoped<IKPIAnomalyService, FieldTicket.Infrastructure.Services.KPIAnomalyService>();
builder.Services.AddScoped<IExcelImportService, FieldTicket.Infrastructure.Services.ExcelImportService>();
builder.Services.AddScoped<IRootCauseAnalysisService, FieldTicket.Infrastructure.Services.RootCauseAnalysisService>();
builder.Services.AddScoped<IProjectService, FieldTicket.Infrastructure.Services.ProjectService>();
builder.Services.AddScoped<EmployeeExportService>();
builder.Services.AddScoped<EmployeeUpdateService>();

// 配置角色映射
var roleMappingConfig = FieldTicket.Infrastructure.WeCom.RoleMappingConfig.LoadFromConfiguration(builder.Configuration);
builder.Services.AddSingleton(roleMappingConfig);

// 注册企业微信通讯录服务
builder.Services.AddScoped<FieldTicket.Core.Services.IWeComContactService, FieldTicket.Infrastructure.WeCom.WeComContactService>();

// CORS 配置
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
                builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>())
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// 映射 API 端点
app.MapAuthEndpoints();
app.MapUserEndpoints();
app.MapEmployeeImportEndpoints();
app.MapEmployeeExportEndpoints();
app.MapEmployeeUpdateEndpoints();
app.MapTicketEndpoints();
app.MapTriageEndpoints();
app.MapSolutionEndpoints();
app.MapVerificationEndpoints();
app.MapJudgementCardQualityEndpoints();
app.MapPerformanceEndpoints();
app.MapAiAnalysisEndpoints();
app.MapConversationalDiagnosisEndpoints();
app.MapConfidenceCalibrationEndpoints();
app.MapAIAttributionEndpoints();
app.MapKnowledgeGraphEndpoints();
app.MapKnowledgeVersionEndpoints();
app.MapJudgementCardRecommendationEndpoints();
app.MapUserProfileEndpoints();
app.MapSmartThresholdEndpoints();
app.MapAttachmentEndpoints();
app.MapNotificationRuleEndpoints();
app.MapJudgementCardVersionEndpoints();
app.MapRecentChangeEndpoints();
app.MapDeviceConfigSnapshotEndpoints();
app.MapStatisticsEndpoints();
app.MapWeComContactEndpoints();
app.MapAIAssistedTriageEndpoints();
app.MapTicketStatusHistoryEndpoints();
app.MapTicketAssociationEndpoints();
app.MapTicketTemplateEndpoints();
app.MapTicketBatchEndpoints();
app.MapTicketExportEndpoints();
app.MapDeviceEndpoints();
app.MapCommunicationTemplateEndpoints();
app.MapEngineerLoadStatEndpoints();
app.MapNewcomerGrowthEndpoints();
app.MapTicketSearchEndpoints();
app.MapStatisticsExportEndpoints();
app.MapResponsibilityAttributionEndpoints();
app.MapCorrectiveActionEndpoints();
app.MapKnowledgeValidityEndpoints();
app.MapKnowledgeSourceTraceEndpoints();
app.MapJudgementCardRelationEndpoints();
app.MapQRCodeEndpoints();
app.MapKPIAnomalyEndpoints();
app.MapMissingInfoConversationEndpoints();
app.MapExcelImportEndpoints();
app.MapRootCauseAnalysisEndpoints();
app.MapProjectEndpoints();
app.MapFactTableEndpoints();

// Health check
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
    .WithName("HealthCheck")
    .WithTags("Health");

app.Run();

