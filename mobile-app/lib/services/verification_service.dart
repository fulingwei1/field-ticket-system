import 'dart:convert';
import 'package:http/http.dart' as http;
import '../models/verification_models.dart';
import 'auth_service.dart';

/// 验证服务
class VerificationService {
  final String baseUrl;
  final AuthService authService;

  VerificationService({
    required this.baseUrl,
    required this.authService,
  });

  /// 提交验证结果
  Future<VerificationDto> submitVerification(
    String ticketId,
    SubmitVerificationRequest request,
  ) async {
    final token = await authService.getToken();
    if (token == null) {
      throw Exception('未登录');
    }

    final response = await http.post(
      Uri.parse('$baseUrl/api/verifications/tickets/$ticketId'),
      headers: {
        'Authorization': 'Bearer $token',
        'Content-Type': 'application/json',
      },
      body: jsonEncode(request.toJson()),
    );

    if (response.statusCode == 201) {
      final data = jsonDecode(utf8.decode(response.bodyBytes));
      return VerificationDto.fromJson(data);
    } else if (response.statusCode == 404) {
      throw Exception('工单不存在');
    } else if (response.statusCode == 400) {
      final error = jsonDecode(utf8.decode(response.bodyBytes));
      throw Exception(error['message'] ?? '提交验证失败');
    } else if (response.statusCode == 401) {
      throw Exception('未登录或登录已过期');
    } else {
      throw Exception('提交验证失败: ${response.statusCode}');
    }
  }

  /// 获取工单的验证历史
  Future<List<VerificationDto>> getVerificationHistory(String ticketId) async {
    final token = await authService.getToken();
    if (token == null) {
      throw Exception('未登录');
    }

    final response = await http.get(
      Uri.parse('$baseUrl/api/verifications/tickets/$ticketId'),
      headers: {
        'Authorization': 'Bearer $token',
      },
    );

    if (response.statusCode == 200) {
      final List<dynamic> data = jsonDecode(utf8.decode(response.bodyBytes));
      return data.map((json) => VerificationDto.fromJson(json)).toList();
    } else if (response.statusCode == 401) {
      throw Exception('未登录或登录已过期');
    } else {
      throw Exception('获取验证历史失败: ${response.statusCode}');
    }
  }

  /// 获取验证详情
  Future<VerificationDto?> getVerification(String verificationId) async {
    final token = await authService.getToken();
    if (token == null) {
      throw Exception('未登录');
    }

    final response = await http.get(
      Uri.parse('$baseUrl/api/verifications/$verificationId'),
      headers: {
        'Authorization': 'Bearer $token',
      },
    );

    if (response.statusCode == 200) {
      final data = jsonDecode(utf8.decode(response.bodyBytes));
      return VerificationDto.fromJson(data);
    } else if (response.statusCode == 404) {
      return null;
    } else if (response.statusCode == 401) {
      throw Exception('未登录或登录已过期');
    } else {
      throw Exception('获取验证详情失败: ${response.statusCode}');
    }
  }
}
