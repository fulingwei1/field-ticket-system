import 'dart:convert';
import 'package:http/http.dart' as http;
import 'package:shared_preferences/shared_preferences.dart';

/// 认证服务
class AuthService {
  static const String _apiBaseUrl = 'http://localhost:5000';
  static const String _tokenKey = 'field_ticket_token';
  static const String _refreshTokenKey = 'field_ticket_refresh_token';
  static const String _userKey = 'field_ticket_user';

  /// 获取企业微信登录URL
  Future<WeComLoginUrlResponse> getWeComLoginUrl({String? state}) async {
    final url = state != null
        ? Uri.parse('$_apiBaseUrl/api/auth/wecom/login-url?state=$state')
        : Uri.parse('$_apiBaseUrl/api/auth/wecom/login-url');

    final response = await http.get(url);

    if (response.statusCode != 200) {
      throw Exception('Failed to get WeCom login URL');
    }

    final data = json.decode(response.body) as Map<String, dynamic>;
    return WeComLoginUrlResponse.fromJson(data);
  }

  /// 处理企业微信回调
  Future<AuthResult> handleWeComCallback(String code, String? state) async {
    final url = Uri.parse('$_apiBaseUrl/api/auth/wecom/callback');
    final response = await http.post(
      url,
      headers: {'Content-Type': 'application/json'},
      body: json.encode({'code': code, 'state': state}),
    );

    if (response.statusCode != 200) {
      throw Exception('Failed to handle WeCom callback');
    }

    final data = json.decode(response.body) as Map<String, dynamic>;
    final result = AuthResult.fromJson(data);
    await _saveAuth(result);
    return result;
  }

  /// 刷新Token
  Future<AuthResult> refreshToken() async {
    final refreshToken = await getRefreshToken();
    if (refreshToken == null) {
      throw Exception('No refresh token available');
    }

    final url = Uri.parse('$_apiBaseUrl/api/auth/refresh');
    final response = await http.post(
      url,
      headers: {'Content-Type': 'application/json'},
      body: json.encode({'refreshToken': refreshToken}),
    );

    if (response.statusCode != 200) {
      await clearAuth();
      throw Exception('Failed to refresh token');
    }

    final data = json.decode(response.body) as Map<String, dynamic>;
    final result = AuthResult.fromJson(data);
    await _saveAuth(result);
    return result;
  }

  /// 获取当前用户信息
  Future<UserInfo?> getCurrentUser() async {
    final token = await getToken();
    if (token == null) {
      return null;
    }

    try {
      final url = Uri.parse('$_apiBaseUrl/api/auth/me');
      final response = await http.get(
        url,
        headers: {'Authorization': 'Bearer $token'},
      );

      if (response.statusCode == 401) {
        // Token 过期，尝试刷新
        try {
          await refreshToken();
          return getCurrentUser();
        } catch {
          await clearAuth();
          return null;
        }
      }

      if (response.statusCode != 200) {
        return null;
      }

      final data = json.decode(response.body) as Map<String, dynamic>;
      final user = UserInfo.fromJson(data);
      await _saveUser(user);
      return user;
    } catch (e) {
      print('Failed to get current user: $e');
      return null;
    }
  }

  /// 保存认证信息
  Future<void> _saveAuth(AuthResult result) async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString(_tokenKey, result.token);
    await prefs.setString(_refreshTokenKey, result.refreshToken);
    await prefs.setString(_userKey, json.encode(result.user.toJson()));
  }

  /// 保存用户信息
  Future<void> _saveUser(UserInfo user) async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString(_userKey, json.encode(user.toJson()));
  }

  /// 获取Token
  Future<String?> getToken() async {
    final prefs = await SharedPreferences.getInstance();
    return prefs.getString(_tokenKey);
  }

  /// 获取Refresh Token
  Future<String?> getRefreshToken() async {
    final prefs = await SharedPreferences.getInstance();
    return prefs.getString(_refreshTokenKey);
  }

  /// 获取用户信息
  Future<UserInfo?> getUser() async {
    final prefs = await SharedPreferences.getInstance();
    final userStr = prefs.getString(_userKey);
    if (userStr == null) {
      return null;
    }
    try {
      final data = json.decode(userStr) as Map<String, dynamic>;
      return UserInfo.fromJson(data);
    } catch {
      return null;
    }
  }

  /// 清除认证信息
  Future<void> clearAuth() async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.remove(_tokenKey);
    await prefs.remove(_refreshTokenKey);
    await prefs.remove(_userKey);
  }

  /// 检查是否已登录
  Future<bool> isAuthenticated() async {
    final token = await getToken();
    return token != null;
  }

  /// 获取认证请求头
  Future<Map<String, String>> getAuthHeaders() async {
    final token = await getToken();
    return {
      'Authorization': token != null ? 'Bearer $token' : '',
      'Content-Type': 'application/json',
    };
  }
}

/// 企业微信登录URL响应
class WeComLoginUrlResponse {
  final String url;
  final String state;

  WeComLoginUrlResponse({required this.url, required this.state});

  factory WeComLoginUrlResponse.fromJson(Map<String, dynamic> json) {
    return WeComLoginUrlResponse(
      url: json['url'] as String,
      state: json['state'] as String,
    );
  }
}

/// 认证结果
class AuthResult {
  final String token;
  final String refreshToken;
  final int expiresIn;
  final UserInfo user;

  AuthResult({
    required this.token,
    required this.refreshToken,
    required this.expiresIn,
    required this.user,
  });

  factory AuthResult.fromJson(Map<String, dynamic> json) {
    return AuthResult(
      token: json['token'] as String,
      refreshToken: json['refreshToken'] as String,
      expiresIn: json['expiresIn'] as int,
      user: UserInfo.fromJson(json['user'] as Map<String, dynamic>),
    );
  }
}

/// 用户信息
class UserInfo {
  final String id;
  final String name;
  final String? mobile;
  final String role;
  final String? deptName;

  UserInfo({
    required this.id,
    required this.name,
    this.mobile,
    required this.role,
    this.deptName,
  });

  factory UserInfo.fromJson(Map<String, dynamic> json) {
    return UserInfo(
      id: json['id'] as String,
      name: json['name'] as String,
      mobile: json['mobile'] as String?,
      role: json['role'] as String,
      deptName: json['deptName'] as String?,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'name': name,
      'mobile': mobile,
      'role': role,
      'deptName': deptName,
    };
  }
}

