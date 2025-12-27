import 'package:flutter/foundation.dart';
import '../services/auth_service.dart';

/// 认证状态管理
class AuthProvider with ChangeNotifier {
  final AuthService _authService = AuthService();

  UserInfo? _user;
  bool _isLoading = false;
  String? _error;

  UserInfo? get user => _user;
  bool get isLoading => _isLoading;
  String? get error => _error;
  bool get isAuthenticated => _user != null;

  /// 初始化 - 检查登录状态
  Future<void> initialize() async {
    _isLoading = true;
    notifyListeners();

    try {
      _user = await _authService.getCurrentUser();
      _error = null;
    } catch (e) {
      _error = e.toString();
      _user = null;
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  /// 企业微信登录
  Future<bool> loginWithWeCom(String code, String? state) async {
    _isLoading = true;
    _error = null;
    notifyListeners();

    try {
      final result = await _authService.handleWeComCallback(code, state);
      _user = result.user;
      _error = null;
      _isLoading = false;
      notifyListeners();
      return true;
    } catch (e) {
      _error = e.toString();
      _user = null;
      _isLoading = false;
      notifyListeners();
      return false;
    }
  }

  /// 刷新Token
  Future<bool> refresh() async {
    try {
      final result = await _authService.refreshToken();
      _user = result.user;
      _error = null;
      notifyListeners();
      return true;
    } catch (e) {
      _error = e.toString();
      _user = null;
      notifyListeners();
      return false;
    }
  }

  /// 登出
  Future<void> logout() async {
    await _authService.clearAuth();
    _user = null;
    _error = null;
    notifyListeners();
  }

  /// 清除错误
  void clearError() {
    _error = null;
    notifyListeners();
  }
}
