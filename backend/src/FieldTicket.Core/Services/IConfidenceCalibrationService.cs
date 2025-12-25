using FieldTicket.Shared.Models;

namespace FieldTicket.Core.Services;

/// <summary>
/// 置信度校准服务接口
/// </summary>
public interface IConfidenceCalibrationService
{
    /// <summary>
    /// 校准置信度
    /// </summary>
    Task<CalibrateConfidenceResponse> CalibrateConfidenceAsync(CalibrateConfidenceRequest request);

    /// <summary>
    /// 训练校准模型
    /// </summary>
    Task<CalibrationModelDto> TrainCalibrationModelAsync(TrainCalibrationModelRequest request);

    /// <summary>
    /// 评估校准效果
    /// </summary>
    Task<CalibrationEvaluationDto> EvaluateCalibrationAsync(EvaluateCalibrationRequest request);

    /// <summary>
    /// 记录校准结果
    /// </summary>
    Task RecordCalibrationResultAsync(SubmitCalibrationFeedbackRequest request);

    /// <summary>
    /// 获取置信度分布
    /// </summary>
    Task<ConfidenceDistributionDto> GetConfidenceDistributionAsync(DateTime? fromDate, DateTime? toDate);

    /// <summary>
    /// 获取活跃的校准模型
    /// </summary>
    Task<CalibrationModelDto?> GetActiveModelAsync();
}

