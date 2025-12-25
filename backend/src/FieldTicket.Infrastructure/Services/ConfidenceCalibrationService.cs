using System.Text.Json;
using FieldTicket.Core.Services;
using FieldTicket.Domain.Entities;
using FieldTicket.Infrastructure.Data;
using FieldTicket.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FieldTicket.Infrastructure.Services;

/// <summary>
/// 置信度校准服务实现
/// </summary>
public class ConfidenceCalibrationService : IConfidenceCalibrationService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<ConfidenceCalibrationService> _logger;

    public ConfidenceCalibrationService(
        ApplicationDbContext dbContext,
        ILogger<ConfidenceCalibrationService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<CalibrateConfidenceResponse> CalibrateConfidenceAsync(CalibrateConfidenceRequest request)
    {
        _logger.LogInformation("Calibrating confidence for ticket {TicketId}, original: {OriginalConfidence}",
            request.TicketId, request.OriginalConfidence);

        // 计算校准因子
        var factors = await CalculateCalibrationFactorsAsync(request.TicketId, request.HypothesisId);

        // 使用线性校准算法
        var calibratedConfidence = ApplyLinearCalibration(request.OriginalConfidence, factors);

        // 限制置信度范围 [0, 1]
        calibratedConfidence = Math.Max(0m, Math.Min(1m, calibratedConfidence));

        return new CalibrateConfidenceResponse
        {
            OriginalConfidence = request.OriginalConfidence,
            CalibratedConfidence = calibratedConfidence,
            CalibrationMethod = "linear",
            CalibrationFactors = JsonDocument.Parse(JsonSerializer.Serialize(factors))
        };
    }

    public async Task<CalibrationModelDto> TrainCalibrationModelAsync(TrainCalibrationModelRequest request)
    {
        _logger.LogInformation("Training calibration model, type: {ModelType}, from: {FromDate}, to: {ToDate}",
            request.ModelType, request.FromDate, request.ToDate);

        // 获取训练数据
        var trainingData = await GetTrainingDataAsync(request.FromDate, request.ToDate);

        if (trainingData.Count < 10)
        {
            throw new InvalidOperationException($"训练数据不足，需要至少10条记录，当前只有 {trainingData.Count} 条");
        }

        // 根据模型类型训练
        CalibrationModelDto model;
        switch (request.ModelType.ToLower())
        {
            case "linear":
                model = await TrainLinearModelAsync(trainingData);
                break;
            default:
                throw new ArgumentException($"不支持的模型类型: {request.ModelType}");
        }

        // 将之前的模型设为非活跃
        var previousActiveModels = await _dbContext.ConfidenceCalibrationModels
            .Where(m => m.IsActive)
            .ToListAsync();

        foreach (var prevModel in previousActiveModels)
        {
            prevModel.IsActive = false;
        }

        // 保存新模型
        var modelEntity = new ConfidenceCalibrationModel
        {
            ModelId = model.ModelId,
            ModelVersion = model.ModelVersion,
            ModelType = model.ModelType,
            ModelParametersJson = model.ModelParameters ?? JsonDocument.Parse("{}"),
            Accuracy = model.Accuracy,
            PrecisionScore = model.PrecisionScore,
            RecallScore = model.RecallScore,
            F1Score = model.F1Score,
            TrainingDataCount = model.TrainingDataCount,
            TrainedAt = model.TrainedAt,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.ConfidenceCalibrationModels.Add(modelEntity);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Calibration model {ModelId} trained successfully", model.ModelId);

        return model;
    }

    public async Task<CalibrationEvaluationDto> EvaluateCalibrationAsync(EvaluateCalibrationRequest request)
    {
        _logger.LogInformation("Evaluating calibration model {ModelId}", request.ModelId);

        var model = await _dbContext.ConfidenceCalibrationModels
            .FirstOrDefaultAsync(m => m.ModelId == request.ModelId);

        if (model == null)
        {
            throw new KeyNotFoundException($"校准模型 {request.ModelId} 不存在");
        }

        // 获取测试数据
        var testData = await GetTrainingDataAsync(request.FromDate, request.ToDate);

        if (testData.Count == 0)
        {
            throw new InvalidOperationException("没有测试数据");
        }

        // 评估模型性能
        var evaluation = EvaluateModelPerformance(model, testData);

        return evaluation;
    }

    public async Task RecordCalibrationResultAsync(SubmitCalibrationFeedbackRequest request)
    {
        _logger.LogInformation("Recording calibration result for ticket {TicketId}", request.TicketId);

        var record = new ConfidenceCalibrationRecord
        {
            RecordId = Guid.NewGuid(),
            TicketId = request.TicketId,
            HypothesisId = request.HypothesisId,
            OriginalConfidence = request.OriginalConfidence,
            CalibratedConfidence = request.CalibratedConfidence,
            ActualResult = request.ActualResult,
            CalibrationMethod = "linear",
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.ConfidenceCalibrationRecords.Add(record);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Calibration result recorded: {RecordId}", record.RecordId);
    }

    public async Task<ConfidenceDistributionDto> GetConfidenceDistributionAsync(DateTime? fromDate, DateTime? toDate)
    {
        _logger.LogInformation("Getting confidence distribution from {FromDate} to {ToDate}", fromDate, toDate);

        var query = _dbContext.ConfidenceCalibrationRecords.AsQueryable();

        if (fromDate.HasValue)
        {
            query = query.Where(r => r.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(r => r.CreatedAt <= toDate.Value);
        }

        var records = await query.ToListAsync();

        var distribution = new ConfidenceDistributionDto
        {
            TotalCount = records.Count
        };

        // 按置信度范围分组
        var ranges = new Dictionary<string, int>
        {
            ["0.0-0.2"] = 0,
            ["0.2-0.4"] = 0,
            ["0.4-0.6"] = 0,
            ["0.6-0.8"] = 0,
            ["0.8-1.0"] = 0
        };

        var confidences = new List<decimal>();

        foreach (var record in records)
        {
            var confidence = record.CalibratedConfidence ?? record.OriginalConfidence;
            confidences.Add(confidence);

            if (confidence < 0.2m)
                ranges["0.0-0.2"]++;
            else if (confidence < 0.4m)
                ranges["0.2-0.4"]++;
            else if (confidence < 0.6m)
                ranges["0.4-0.6"]++;
            else if (confidence < 0.8m)
                ranges["0.6-0.8"]++;
            else
                ranges["0.8-1.0"]++;
        }

        distribution.DistributionByRange = ranges;
        distribution.AverageConfidence = confidences.Any() ? confidences.Average() : 0m;
        distribution.MedianConfidence = confidences.Any() 
            ? confidences.OrderBy(c => c).Skip(confidences.Count / 2).First() 
            : 0m;

        return distribution;
    }

    public async Task<CalibrationModelDto?> GetActiveModelAsync()
    {
        var model = await _dbContext.ConfidenceCalibrationModels
            .FirstOrDefaultAsync(m => m.IsActive);

        if (model == null)
        {
            return null;
        }

        return new CalibrationModelDto
        {
            ModelId = model.ModelId,
            ModelVersion = model.ModelVersion,
            ModelType = model.ModelType,
            ModelParameters = model.ModelParametersJson,
            Accuracy = model.Accuracy,
            PrecisionScore = model.PrecisionScore,
            RecallScore = model.RecallScore,
            F1Score = model.F1Score,
            TrainingDataCount = model.TrainingDataCount,
            TrainedAt = model.TrainedAt,
            IsActive = model.IsActive,
            CreatedAt = model.CreatedAt
        };
    }

    // 私有辅助方法

    private async Task<CalibrationFactorsDto> CalculateCalibrationFactorsAsync(Guid ticketId, string? hypothesisId)
    {
        var factors = new CalibrationFactorsDto
        {
            HistoricalAccuracy = 1.0m,
            ContextMatch = 1.0m,
            EvidenceStrength = 1.0m,
            UserFeedback = null
        };

        // 1. 计算历史准确率
        if (!string.IsNullOrEmpty(hypothesisId))
        {
            var historicalRecords = await _dbContext.ConfidenceCalibrationRecords
                .Where(r => r.HypothesisId == hypothesisId && r.ActualResult != null)
                .ToListAsync();

            if (historicalRecords.Any())
            {
                var correctCount = historicalRecords.Count(r => r.ActualResult == "correct");
                factors.HistoricalAccuracy = (decimal)correctCount / historicalRecords.Count;
            }
        }

        // 2. 计算上下文匹配度（简化实现，后续可以优化）
        factors.ContextMatch = 0.9m;

        // 3. 计算证据强度（简化实现，后续可以优化）
        factors.EvidenceStrength = 0.85m;

        return factors;
    }

    private decimal ApplyLinearCalibration(decimal originalConfidence, CalibrationFactorsDto factors)
    {
        // 线性校准公式：calibrated = original * historical_accuracy * context_match * evidence_strength
        var calibrated = originalConfidence;
        calibrated *= factors.HistoricalAccuracy;
        calibrated *= factors.ContextMatch;
        calibrated *= factors.EvidenceStrength;

        // 如果有用户反馈，进一步调整
        if (factors.UserFeedback.HasValue)
        {
            calibrated = (calibrated + factors.UserFeedback.Value) / 2;
        }

        return calibrated;
    }

    private async Task<List<CalibrationTrainingData>> GetTrainingDataAsync(DateTime? fromDate, DateTime? toDate)
    {
        var query = _dbContext.ConfidenceCalibrationRecords
            .Where(r => r.ActualResult != null && r.CalibratedConfidence != null)
            .AsQueryable();

        if (fromDate.HasValue)
        {
            query = query.Where(r => r.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(r => r.CreatedAt <= toDate.Value);
        }

        var records = await query.ToListAsync();

        return records.Select(r => new CalibrationTrainingData
        {
            OriginalConfidence = r.OriginalConfidence,
            CalibratedConfidence = r.CalibratedConfidence!.Value,
            ActualResult = r.ActualResult!,
            TicketId = r.TicketId,
            HypothesisId = r.HypothesisId
        }).ToList();
    }

    private async Task<CalibrationModelDto> TrainLinearModelAsync(List<CalibrationTrainingData> trainingData)
    {
        // 简单的线性回归模型训练
        // 这里使用最小二乘法拟合：calibrated = a * original + b

        var n = trainingData.Count;
        var sumX = trainingData.Sum(d => (double)d.OriginalConfidence);
        var sumY = trainingData.Sum(d => (double)d.CalibratedConfidence);
        var sumXY = trainingData.Sum(d => (double)d.OriginalConfidence * (double)d.CalibratedConfidence);
        var sumX2 = trainingData.Sum(d => (double)d.OriginalConfidence * (double)d.OriginalConfidence);

        var denominator = n * sumX2 - sumX * sumX;
        if (Math.Abs(denominator) < 0.0001)
        {
            throw new InvalidOperationException("无法计算线性回归参数：分母为0");
        }

        var a = (n * sumXY - sumX * sumY) / denominator;
        var b = (sumY - a * sumX) / n;

        // 评估模型性能
        var predictions = trainingData.Select(d => (decimal)(a * (double)d.OriginalConfidence + b)).ToList();
        var actuals = trainingData.Select(d => d.CalibratedConfidence).ToList();

        var accuracy = CalculateAccuracy(predictions, actuals);
        var precision = CalculatePrecision(predictions, actuals);
        var recall = CalculateRecall(predictions, actuals);
        var f1 = 2 * precision * recall / (precision + recall);

        var modelVersion = $"v{DateTime.UtcNow:yyyyMMddHHmmss}";

        var parameters = new Dictionary<string, object>
        {
            ["a"] = a,
            ["b"] = b
        };

        return new CalibrationModelDto
        {
            ModelId = Guid.NewGuid(),
            ModelVersion = modelVersion,
            ModelType = "linear",
            ModelParameters = JsonDocument.Parse(JsonSerializer.Serialize(parameters)),
            Accuracy = (decimal)accuracy,
            PrecisionScore = (decimal)precision,
            RecallScore = (decimal)recall,
            F1Score = (decimal)f1,
            TrainingDataCount = trainingData.Count,
            TrainedAt = DateTime.UtcNow,
            IsActive = false, // 将在保存时设为true
            CreatedAt = DateTime.UtcNow
        };
    }

    private CalibrationEvaluationDto EvaluateModelPerformance(
        ConfidenceCalibrationModel model,
        List<CalibrationTrainingData> testData)
    {
        // 解析模型参数
        var parameters = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
            model.ModelParametersJson.RootElement.GetRawText());

        if (parameters == null || !parameters.ContainsKey("a") || !parameters.ContainsKey("b"))
        {
            throw new InvalidOperationException("模型参数格式错误");
        }

        var a = parameters["a"].GetDouble();
        var b = parameters["b"].GetDouble();

        // 生成预测
        var predictions = testData.Select(d => (decimal)(a * (double)d.OriginalConfidence + b)).ToList();
        var actuals = testData.Select(d => d.CalibratedConfidence).ToList();

        var accuracy = CalculateAccuracy(predictions, actuals);
        var precision = CalculatePrecision(predictions, actuals);
        var recall = CalculateRecall(predictions, actuals);
        var f1 = 2 * precision * recall / (precision + recall);

        // 按置信度范围分组评估
        var metricsByRange = new Dictionary<string, decimal>
        {
            ["0.0-0.2"] = 0m,
            ["0.2-0.4"] = 0m,
            ["0.4-0.6"] = 0m,
            ["0.6-0.8"] = 0m,
            ["0.8-1.0"] = 0m
        };

        // 简化实现：计算每个范围的准确率
        foreach (var range in metricsByRange.Keys.ToList())
        {
            var rangeParts = range.Split('-');
            var min = decimal.Parse(rangeParts[0]);
            var max = decimal.Parse(rangeParts[1]);

            var rangeData = testData
                .Where((d, i) => predictions[i] >= min && predictions[i] < max)
                .ToList();

            if (rangeData.Any())
            {
                var rangePredictions = rangeData.Select((d, i) => predictions[testData.IndexOf(d)]).ToList();
                var rangeActuals = rangeData.Select(d => d.CalibratedConfidence).ToList();
                metricsByRange[range] = (decimal)CalculateAccuracy(rangePredictions, rangeActuals);
            }
        }

        return new CalibrationEvaluationDto
        {
            ModelId = model.ModelId,
            Accuracy = (decimal)accuracy,
            Precision = (decimal)precision,
            Recall = (decimal)recall,
            F1Score = (decimal)f1,
            TestDataCount = testData.Count,
            MetricsByConfidenceRange = metricsByRange
        };
    }

    private double CalculateAccuracy(List<decimal> predictions, List<decimal> actuals)
    {
        if (predictions.Count != actuals.Count || predictions.Count == 0)
        {
            return 0.0;
        }

        var errors = predictions.Zip(actuals, (p, a) => Math.Abs((double)(p - a))).ToList();
        var mae = errors.Average();
        return 1.0 - Math.Min(mae, 1.0); // 转换为准确率
    }

    private double CalculatePrecision(List<decimal> predictions, List<decimal> actuals)
    {
        // 简化实现：使用MAE的倒数作为精确度
        if (predictions.Count != actuals.Count || predictions.Count == 0)
        {
            return 0.0;
        }

        var mae = predictions.Zip(actuals, (p, a) => Math.Abs((double)(p - a))).Average();
        return Math.Max(0.0, 1.0 - mae);
    }

    private double CalculateRecall(List<decimal> predictions, List<decimal> actuals)
    {
        // 简化实现：与精确度相同
        return CalculatePrecision(predictions, actuals);
    }

    // 内部类：训练数据
    private class CalibrationTrainingData
    {
        public decimal OriginalConfidence { get; set; }
        public decimal CalibratedConfidence { get; set; }
        public string ActualResult { get; set; } = string.Empty;
        public Guid TicketId { get; set; }
        public string? HypothesisId { get; set; }
    }
}

