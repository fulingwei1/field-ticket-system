import 'dart:convert';
import 'package:http/http.dart' as http;
import 'auth_service.dart';

/// 工单服务
class TicketService {
  final AuthService _authService = AuthService();
  static const String _apiBaseUrl = 'http://localhost:5000';

  /// 创建工单草稿
  Future<TicketDto> createDraft(CreateTicketRequest request, {String? idempotencyKey}) async {
    final url = Uri.parse('$_apiBaseUrl/api/tickets');
    final headers = await _authService.getAuthHeaders();
    if (idempotencyKey != null) {
      headers['X-Idempotency-Key'] = idempotencyKey;
    }

    final response = await http.post(
      url,
      headers: headers,
      body: json.encode(request.toJson()),
    );

    if (response.statusCode != 200) {
      throw Exception('Failed to create ticket');
    }

    final data = json.decode(response.body) as Map<String, dynamic>;
    return TicketDto.fromJson(data);
  }

  /// 更新工单草稿
  Future<TicketDto> updateDraft(String ticketId, UpdateTicketRequest request) async {
    final url = Uri.parse('$_apiBaseUrl/api/tickets/$ticketId');
    final headers = await _authService.getAuthHeaders();

    final response = await http.put(
      url,
      headers: headers,
      body: json.encode(request.toJson()),
    );

    if (response.statusCode != 200) {
      throw Exception('Failed to update ticket');
    }

    final data = json.decode(response.body) as Map<String, dynamic>;
    return TicketDto.fromJson(data);
  }

  /// 提交工单
  Future<SubmitTicketResult> submitTicket(String ticketId) async {
    final url = Uri.parse('$_apiBaseUrl/api/tickets/$ticketId/submit');
    final headers = await _authService.getAuthHeaders();

    final response = await http.post(url, headers: headers);

    if (response.statusCode == 422) {
      final data = json.decode(response.body) as Map<String, dynamic>;
      return SubmitTicketResult(
        success: false,
        ticketNo: '',
        status: '',
        errors: (data['errors'] as List?)
            ?.map((e) => ValidationError.fromJson(e as Map<String, dynamic>))
            .toList(),
      );
    }

    if (response.statusCode != 200) {
      throw Exception('Failed to submit ticket');
    }

    final data = json.decode(response.body) as Map<String, dynamic>;
    return SubmitTicketResult.fromJson(data);
  }

  /// 获取工单详情
  Future<TicketDto> getTicket(String ticketId) async {
    final url = Uri.parse('$_apiBaseUrl/api/tickets/$ticketId');
    final headers = await _authService.getAuthHeaders();

    final response = await http.get(url, headers: headers);

    if (response.statusCode != 200) {
      throw Exception('Failed to get ticket');
    }

    final data = json.decode(response.body) as Map<String, dynamic>;
    return TicketDto.fromJson(data);
  }

  /// 获取工单列表
  Future<TicketListResult> getTickets({
    List<String>? status,
    String? customerId,
    String? deviceSn,
    String? domain,
    String? priority,
    String? createdBy,
    String? dateFrom,
    String? dateTo,
    int page = 1,
    int pageSize = 20,
  }) async {
    final queryParams = <String, String>{
      'page': page.toString(),
      'pageSize': pageSize.toString(),
    };
    if (status != null) {
      for (var s in status) {
        queryParams['status'] = s;
      }
    }
    if (customerId != null) queryParams['customerId'] = customerId;
    if (deviceSn != null) queryParams['deviceSn'] = deviceSn;
    if (domain != null) queryParams['domain'] = domain;
    if (priority != null) queryParams['priority'] = priority;
    if (createdBy != null) queryParams['createdBy'] = createdBy;
    if (dateFrom != null) queryParams['dateFrom'] = dateFrom;
    if (dateTo != null) queryParams['dateTo'] = dateTo;

    final uri = Uri.parse('$_apiBaseUrl/api/tickets').replace(queryParameters: queryParams);
    final headers = await _authService.getAuthHeaders();

    final response = await http.get(uri, headers: headers);

    if (response.statusCode != 200) {
      throw Exception('Failed to get tickets');
    }

    final data = json.decode(response.body) as Map<String, dynamic>;
    return TicketListResult.fromJson(data);
  }

  /// 获取工单缺失信息分析
  Future<MissingInfoAnalysisResult> getMissingInfo(
    String ticketId,
    String? jcCode,
  ) async {
    final queryParams = <String, String>{};
    if (jcCode != null) queryParams['jcCode'] = jcCode;

    final uri = Uri.parse('$_apiBaseUrl/api/tickets/$ticketId/missing-info')
        .replace(queryParameters: queryParams);
    final headers = await _authService.getAuthHeaders();

    final response = await http.get(uri, headers: headers);

    if (response.statusCode != 200) {
      throw Exception('Failed to get missing info');
    }

    final data = json.decode(response.body) as Map<String, dynamic>;
    return MissingInfoAnalysisResult.fromJson(data);
  }

  /// 补全缺失信息
  Future<TicketDto> completeMissingInfo(
    String ticketId,
    CompleteMissingInfoRequest request,
  ) async {
    final url = Uri.parse('$_apiBaseUrl/api/tickets/$ticketId/complete-missing-info');
    final headers = await _authService.getAuthHeaders();

    final response = await http.post(
      url,
      headers: headers,
      body: json.encode(request.toJson()),
    );

    if (response.statusCode != 200) {
      throw Exception('Failed to complete missing info');
    }

    final data = json.decode(response.body) as Map<String, dynamic>;
    return TicketDto.fromJson(data);
  }
}

/// 创建工单请求
class CreateTicketRequest {
  final String? localDraftId;
  final String deviceId;
  final String? stationId;
  final String domain; // A/B/C/D/E
  final String stepCode;
  final String? stepName;
  final String symptomTitle;
  final String? symptomDetail;
  final int? reproRate;
  final bool? rebootRecovers;
  final bool? envRelated;
  final String swVersion;
  final String plcVersion;
  final String paramVersion;
  final Map<String, dynamic> factsJson;
  final List<String>? actionsTaken;
  final String? actionsTakenNote;
  final String? alarmCode;
  final bool confirmedAsFact;

  CreateTicketRequest({
    this.localDraftId,
    required this.deviceId,
    this.stationId,
    required this.domain,
    required this.stepCode,
    this.stepName,
    required this.symptomTitle,
    this.symptomDetail,
    this.reproRate,
    this.rebootRecovers,
    this.envRelated,
    required this.swVersion,
    required this.plcVersion,
    required this.paramVersion,
    required this.factsJson,
    this.actionsTaken,
    this.actionsTakenNote,
    this.alarmCode,
    required this.confirmedAsFact,
  });

  Map<String, dynamic> toJson() {
    return {
      'localDraftId': localDraftId,
      'deviceId': deviceId,
      'stationId': stationId,
      'domain': domain,
      'stepCode': stepCode,
      'stepName': stepName,
      'symptomTitle': symptomTitle,
      'symptomDetail': symptomDetail,
      'reproRate': reproRate,
      'rebootRecovers': rebootRecovers,
      'envRelated': envRelated,
      'swVersion': swVersion,
      'plcVersion': plcVersion,
      'paramVersion': paramVersion,
      'factsJson': factsJson,
      'actionsTaken': actionsTaken,
      'actionsTakenNote': actionsTakenNote,
      'alarmCode': alarmCode,
      'confirmedAsFact': confirmedAsFact,
    };
  }
}

/// 更新工单请求
class UpdateTicketRequest {
  final String? domain;
  final String? stepCode;
  final String? stepName;
  final String? symptomTitle;
  final String? symptomDetail;
  final int? reproRate;
  final bool? rebootRecovers;
  final bool? envRelated;
  final String? swVersion;
  final String? plcVersion;
  final String? paramVersion;
  final Map<String, dynamic>? factsJson;
  final List<String>? actionsTaken;
  final String? actionsTakenNote;
  final String? alarmCode;
  final bool? confirmedAsFact;

  UpdateTicketRequest({
    this.domain,
    this.stepCode,
    this.stepName,
    this.symptomTitle,
    this.symptomDetail,
    this.reproRate,
    this.rebootRecovers,
    this.envRelated,
    this.swVersion,
    this.plcVersion,
    this.paramVersion,
    this.factsJson,
    this.actionsTaken,
    this.actionsTakenNote,
    this.alarmCode,
    this.confirmedAsFact,
  });

  Map<String, dynamic> toJson() {
    final map = <String, dynamic>{};
    if (domain != null) map['domain'] = domain;
    if (stepCode != null) map['stepCode'] = stepCode;
    if (stepName != null) map['stepName'] = stepName;
    if (symptomTitle != null) map['symptomTitle'] = symptomTitle;
    if (symptomDetail != null) map['symptomDetail'] = symptomDetail;
    if (reproRate != null) map['reproRate'] = reproRate;
    if (rebootRecovers != null) map['rebootRecovers'] = rebootRecovers;
    if (envRelated != null) map['envRelated'] = envRelated;
    if (swVersion != null) map['swVersion'] = swVersion;
    if (plcVersion != null) map['plcVersion'] = plcVersion;
    if (paramVersion != null) map['paramVersion'] = paramVersion;
    if (factsJson != null) map['factsJson'] = factsJson;
    if (actionsTaken != null) map['actionsTaken'] = actionsTaken;
    if (actionsTakenNote != null) map['actionsTakenNote'] = actionsTakenNote;
    if (alarmCode != null) map['alarmCode'] = alarmCode;
    if (confirmedAsFact != null) map['confirmedAsFact'] = confirmedAsFact;
    return map;
  }
}

/// 工单DTO
class TicketDto {
  final String ticketId;
  final String ticketNo;
  final String customerId;
  final String projectId;
  final String deviceId;
  final String? stationId;
  final String createdByUserId;
  final String domain;
  final String stepCode;
  final String? stepName;
  final String symptomTitle;
  final String? symptomDetail;
  final int? reproRate;
  final bool? rebootRecovers;
  final bool? envRelated;
  final String swVersion;
  final String plcVersion;
  final String paramVersion;
  final Map<String, dynamic> factsJson;
  final List<String> actionsTaken;
  final String? actionsTakenNote;
  final String? alarmCode;
  final bool confirmedAsFact;
  final String? confirmedAt;
  final String status;
  final String priority;
  final String? currentJcCode;
  final String? assignedTo;
  final String createdAt;
  final String updatedAt;
  final String? submittedAt;
  final String? closedAt;
  final int attachmentCount;

  TicketDto({
    required this.ticketId,
    required this.ticketNo,
    required this.customerId,
    required this.projectId,
    required this.deviceId,
    this.stationId,
    required this.createdByUserId,
    required this.domain,
    required this.stepCode,
    this.stepName,
    required this.symptomTitle,
    this.symptomDetail,
    this.reproRate,
    this.rebootRecovers,
    this.envRelated,
    required this.swVersion,
    required this.plcVersion,
    required this.paramVersion,
    required this.factsJson,
    required this.actionsTaken,
    this.actionsTakenNote,
    this.alarmCode,
    required this.confirmedAsFact,
    this.confirmedAt,
    required this.status,
    required this.priority,
    this.currentJcCode,
    this.assignedTo,
    required this.createdAt,
    required this.updatedAt,
    this.submittedAt,
    this.closedAt,
    required this.attachmentCount,
  });

  factory TicketDto.fromJson(Map<String, dynamic> json) {
    return TicketDto(
      ticketId: json['ticketId'] as String,
      ticketNo: json['ticketNo'] as String,
      customerId: json['customerId'] as String,
      projectId: json['projectId'] as String,
      deviceId: json['deviceId'] as String,
      stationId: json['stationId'] as String?,
      createdByUserId: json['createdByUserId'] as String,
      domain: json['domain'] as String,
      stepCode: json['stepCode'] as String,
      stepName: json['stepName'] as String?,
      symptomTitle: json['symptomTitle'] as String,
      symptomDetail: json['symptomDetail'] as String?,
      reproRate: json['reproRate'] as int?,
      rebootRecovers: json['rebootRecovers'] as bool?,
      envRelated: json['envRelated'] as bool?,
      swVersion: json['swVersion'] as String,
      plcVersion: json['plcVersion'] as String,
      paramVersion: json['paramVersion'] as String,
      factsJson: json['factsJson'] as Map<String, dynamic>,
      actionsTaken: (json['actionsTaken'] as List?)?.map((e) => e as String).toList() ?? [],
      actionsTakenNote: json['actionsTakenNote'] as String?,
      alarmCode: json['alarmCode'] as String?,
      confirmedAsFact: json['confirmedAsFact'] as bool,
      confirmedAt: json['confirmedAt'] as String?,
      status: json['status'] as String,
      priority: json['priority'] as String,
      currentJcCode: json['currentJcCode'] as String?,
      assignedTo: json['assignedTo'] as String?,
      createdAt: json['createdAt'] as String,
      updatedAt: json['updatedAt'] as String,
      submittedAt: json['submittedAt'] as String?,
      closedAt: json['closedAt'] as String?,
      attachmentCount: json['attachmentCount'] as int,
    );
  }
}

/// 提交工单结果
class SubmitTicketResult {
  final bool success;
  final String ticketNo;
  final String status;
  final List<ValidationError>? errors;

  SubmitTicketResult({
    required this.success,
    required this.ticketNo,
    required this.status,
    this.errors,
  });

  factory SubmitTicketResult.fromJson(Map<String, dynamic> json) {
    return SubmitTicketResult(
      success: json['success'] as bool,
      ticketNo: json['ticketNo'] as String,
      status: json['status'] as String,
      errors: (json['errors'] as List?)
          ?.map((e) => ValidationError.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
  }
}

/// 校验错误
class ValidationError {
  final String field;
  final String code;
  final String message;

  ValidationError({
    required this.field,
    required this.code,
    required this.message,
  });

  factory ValidationError.fromJson(Map<String, dynamic> json) {
    return ValidationError(
      field: json['field'] as String,
      code: json['code'] as String,
      message: json['message'] as String,
    );
  }
}

/// 工单列表结果
class TicketListResult {
  final List<TicketListItemDto> items;
  final int total;
  final int page;
  final int pageSize;

  TicketListResult({
    required this.items,
    required this.total,
    required this.page,
    required this.pageSize,
  });

  factory TicketListResult.fromJson(Map<String, dynamic> json) {
    return TicketListResult(
      items: (json['items'] as List)
          .map((e) => TicketListItemDto.fromJson(e as Map<String, dynamic>))
          .toList(),
      total: json['total'] as int,
      page: json['page'] as int,
      pageSize: json['pageSize'] as int,
    );
  }
}

/// 工单列表项DTO
class TicketListItemDto {
  final String ticketId;
  final String ticketNo;
  final String customerName;
  final String deviceSn;
  final String domain;
  final String stepCode;
  final String symptomTitle;
  final String status;
  final String priority;
  final String createdByName;
  final String createdAt;
  final String? submittedAt;

  TicketListItemDto({
    required this.ticketId,
    required this.ticketNo,
    required this.customerName,
    required this.deviceSn,
    required this.domain,
    required this.stepCode,
    required this.symptomTitle,
    required this.status,
    required this.priority,
    required this.createdByName,
    required this.createdAt,
    this.submittedAt,
  });

  factory TicketListItemDto.fromJson(Map<String, dynamic> json) {
    return TicketListItemDto(
      ticketId: json['ticketId'] as String,
      ticketNo: json['ticketNo'] as String,
      customerName: json['customerName'] as String,
      deviceSn: json['deviceSn'] as String,
      domain: json['domain'] as String,
      stepCode: json['stepCode'] as String,
      symptomTitle: json['symptomTitle'] as String,
      status: json['status'] as String,
      priority: json['priority'] as String,
      createdByName: json['createdByName'] as String,
      createdAt: json['createdAt'] as String,
      submittedAt: json['submittedAt'] as String?,
    );
  }
}


