/// 验证相关数据模型

/// 提交验证结果请求
class SubmitVerificationRequest {
  final String? solutionId;
  final int runCount;
  final int passCount;
  final int failCount;
  final Map<String, dynamic> checklistResult;
  final List<String> evidenceAttachmentIds;
  final String? note;

  SubmitVerificationRequest({
    this.solutionId,
    required this.runCount,
    required this.passCount,
    required this.failCount,
    this.checklistResult = const {},
    this.evidenceAttachmentIds = const [],
    this.note,
  });

  Map<String, dynamic> toJson() {
    return {
      if (solutionId != null) 'solutionId': solutionId,
      'runCount': runCount,
      'passCount': passCount,
      'failCount': failCount,
      'checklistResultJson': checklistResult,
      'evidenceAttachmentIds': evidenceAttachmentIds,
      if (note != null && note!.isNotEmpty) 'note': note,
    };
  }
}

/// 验证结果DTO
class VerificationDto {
  final String verificationId;
  final String ticketId;
  final String? solutionId;
  final String executedBy;
  final String? executedByName;
  final int runCount;
  final int passCount;
  final int failCount;
  final String result;
  final Map<String, dynamic> checklistResult;
  final List<String> evidenceAttachmentIds;
  final String? note;
  final DateTime verifiedAt;
  final DateTime createdAt;
  final DateTime updatedAt;

  VerificationDto({
    required this.verificationId,
    required this.ticketId,
    this.solutionId,
    required this.executedBy,
    this.executedByName,
    required this.runCount,
    required this.passCount,
    required this.failCount,
    required this.result,
    this.checklistResult = const {},
    this.evidenceAttachmentIds = const [],
    this.note,
    required this.verifiedAt,
    required this.createdAt,
    required this.updatedAt,
  });

  factory VerificationDto.fromJson(Map<String, dynamic> json) {
    return VerificationDto(
      verificationId: json['verificationId'] as String,
      ticketId: json['ticketId'] as String,
      solutionId: json['solutionId'] as String?,
      executedBy: json['executedBy'] as String,
      executedByName: json['executedByName'] as String?,
      runCount: json['runCount'] as int,
      passCount: json['passCount'] as int,
      failCount: json['failCount'] as int,
      result: json['result'] as String,
      checklistResult: json['checklistResultJson'] as Map<String, dynamic>? ?? {},
      evidenceAttachmentIds: (json['evidenceAttachmentIds'] as List<dynamic>?)
              ?.map((e) => e as String)
              .toList() ??
          [],
      note: json['note'] as String?,
      verifiedAt: DateTime.parse(json['verifiedAt'] as String),
      createdAt: DateTime.parse(json['createdAt'] as String),
      updatedAt: DateTime.parse(json['updatedAt'] as String),
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'verificationId': verificationId,
      'ticketId': ticketId,
      if (solutionId != null) 'solutionId': solutionId,
      'executedBy': executedBy,
      if (executedByName != null) 'executedByName': executedByName,
      'runCount': runCount,
      'passCount': passCount,
      'failCount': failCount,
      'result': result,
      'checklistResultJson': checklistResult,
      'evidenceAttachmentIds': evidenceAttachmentIds,
      if (note != null) 'note': note,
      'verifiedAt': verifiedAt.toIso8601String(),
      'createdAt': createdAt.toIso8601String(),
      'updatedAt': updatedAt.toIso8601String(),
    };
  }
}
