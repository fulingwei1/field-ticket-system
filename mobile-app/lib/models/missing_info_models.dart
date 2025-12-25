/// 缺失信息分析结果
class MissingInfoAnalysisResult {
  final List<MissingInfoItem> missingInfo;
  final List<QuestionItem> questions;
  final bool hasCriticalMissing;

  MissingInfoAnalysisResult({
    required this.missingInfo,
    required this.questions,
    required this.hasCriticalMissing,
  });

  factory MissingInfoAnalysisResult.fromJson(Map<String, dynamic> json) {
    return MissingInfoAnalysisResult(
      missingInfo: (json['missingInfo'] as List?)
              ?.map((e) => MissingInfoItem.fromJson(e as Map<String, dynamic>))
              .toList() ??
          [],
      questions: (json['questions'] as List?)
              ?.map((e) => QuestionItem.fromJson(e as Map<String, dynamic>))
              .toList() ??
          [],
      hasCriticalMissing: json['hasCriticalMissing'] as bool? ?? false,
    );
  }
}

/// 缺失信息项
class MissingInfoItem {
  final String field;
  final String question;
  final String type;
  final bool required;
  final String? domain;
  final List<String>? options;
  final String? hint;

  MissingInfoItem({
    required this.field,
    required this.question,
    required this.type,
    required this.required,
    this.domain,
    this.options,
    this.hint,
  });

  factory MissingInfoItem.fromJson(Map<String, dynamic> json) {
    return MissingInfoItem(
      field: json['field'] as String,
      question: json['question'] as String,
      type: json['type'] as String,
      required: json['required'] as bool? ?? false,
      domain: json['domain'] as String?,
      options: (json['options'] as List?)?.map((e) => e as String).toList(),
      hint: json['hint'] as String?,
    );
  }
}

/// 问诊式问题项
class QuestionItem {
  final String questionId;
  final String question;
  final String type;
  final bool required;
  final List<String>? options;
  final String? hint;
  final String? field;

  QuestionItem({
    required this.questionId,
    required this.question,
    required this.type,
    required this.required,
    this.options,
    this.hint,
    this.field,
  });

  factory QuestionItem.fromJson(Map<String, dynamic> json) {
    return QuestionItem(
      questionId: json['questionId'] as String,
      question: json['question'] as String,
      type: json['type'] as String,
      required: json['required'] as bool? ?? false,
      options: (json['options'] as List?)?.map((e) => e as String).toList(),
      hint: json['hint'] as String?,
      field: json['field'] as String?,
    );
  }
}

/// 补全缺失信息请求
class CompleteMissingInfoRequest {
  final Map<String, dynamic> answers;
  final Map<String, List<String>>? attachments;

  CompleteMissingInfoRequest({
    required this.answers,
    this.attachments,
  });

  Map<String, dynamic> toJson() {
    return {
      'answers': answers,
      if (attachments != null) 'attachments': attachments,
    };
  }
}

