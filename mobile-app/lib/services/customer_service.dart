import 'dart:convert';
import 'package:http/http.dart' as http;
import 'auth_service.dart';

/// 客户和设备服务
class CustomerService {
  final AuthService _authService = AuthService();
  static const String _apiBaseUrl = 'http://localhost:5000';

  /// 获取客户列表
  Future<List<CustomerDto>> getCustomers({
    String? searchTerm,
    int page = 1,
    int pageSize = 20,
  }) async {
    final queryParams = <String, String>{
      'page': page.toString(),
      'pageSize': pageSize.toString(),
    };
    if (searchTerm != null && searchTerm.isNotEmpty) {
      queryParams['searchTerm'] = searchTerm;
    }

    final uri = Uri.parse('$_apiBaseUrl/api/customers')
        .replace(queryParameters: queryParams);
    final headers = await _authService.getAuthHeaders();

    final response = await http.get(uri, headers: headers);

    if (response.statusCode != 200) {
      throw Exception('Failed to get customers');
    }

    final data = json.decode(response.body);
    final items = data['items'] as List;
    return items.map((e) => CustomerDto.fromJson(e as Map<String, dynamic>)).toList();
  }

  /// 获取客户详情
  Future<CustomerDto> getCustomer(String customerId) async {
    final url = Uri.parse('$_apiBaseUrl/api/customers/$customerId');
    final headers = await _authService.getAuthHeaders();

    final response = await http.get(url, headers: headers);

    if (response.statusCode != 200) {
      throw Exception('Failed to get customer');
    }

    final data = json.decode(response.body) as Map<String, dynamic>;
    return CustomerDto.fromJson(data);
  }

  /// 获取设备列表
  Future<List<DeviceDto>> getDevices({
    String? customerId,
    String? searchTerm,
    int page = 1,
    int pageSize = 20,
  }) async {
    final queryParams = <String, String>{
      'page': page.toString(),
      'pageSize': pageSize.toString(),
    };
    if (customerId != null) queryParams['customerId'] = customerId;
    if (searchTerm != null && searchTerm.isNotEmpty) {
      queryParams['searchTerm'] = searchTerm;
    }

    final uri = Uri.parse('$_apiBaseUrl/api/devices')
        .replace(queryParameters: queryParams);
    final headers = await _authService.getAuthHeaders();

    final response = await http.get(uri, headers: headers);

    if (response.statusCode != 200) {
      throw Exception('Failed to get devices');
    }

    final data = json.decode(response.body);
    final items = data['items'] as List;
    return items.map((e) => DeviceDto.fromJson(e as Map<String, dynamic>)).toList();
  }

  /// 获取设备详情
  Future<DeviceDto> getDevice(String deviceId) async {
    final url = Uri.parse('$_apiBaseUrl/api/devices/$deviceId');
    final headers = await _authService.getAuthHeaders();

    final response = await http.get(url, headers: headers);

    if (response.statusCode != 200) {
      throw Exception('Failed to get device');
    }

    final data = json.decode(response.body) as Map<String, dynamic>;
    return DeviceDto.fromJson(data);
  }
}

/// 客户 DTO
class CustomerDto {
  final String customerId;
  final String customerName;
  final String? contactPerson;
  final String? contactPhone;
  final String? address;
  final String? serviceLevel;

  CustomerDto({
    required this.customerId,
    required this.customerName,
    this.contactPerson,
    this.contactPhone,
    this.address,
    this.serviceLevel,
  });

  factory CustomerDto.fromJson(Map<String, dynamic> json) {
    return CustomerDto(
      customerId: json['customerId'] as String,
      customerName: json['customerName'] as String,
      contactPerson: json['contactPerson'] as String?,
      contactPhone: json['contactPhone'] as String?,
      address: json['address'] as String?,
      serviceLevel: json['serviceLevel'] as String?,
    );
  }
}

/// 设备 DTO
class DeviceDto {
  final String deviceId;
  final String customerId;
  final String deviceSn;
  final String deviceModel;
  final String? deviceName;
  final String? installLocation;
  final String? currentSwVersion;
  final String? currentPlcVersion;
  final String? currentParamVersion;

  DeviceDto({
    required this.deviceId,
    required this.customerId,
    required this.deviceSn,
    required this.deviceModel,
    this.deviceName,
    this.installLocation,
    this.currentSwVersion,
    this.currentPlcVersion,
    this.currentParamVersion,
  });

  factory DeviceDto.fromJson(Map<String, dynamic> json) {
    return DeviceDto(
      deviceId: json['deviceId'] as String,
      customerId: json['customerId'] as String,
      deviceSn: json['deviceSn'] as String,
      deviceModel: json['deviceModel'] as String,
      deviceName: json['deviceName'] as String?,
      installLocation: json['installLocation'] as String?,
      currentSwVersion: json['currentSwVersion'] as String?,
      currentPlcVersion: json['currentPlcVersion'] as String?,
      currentParamVersion: json['currentParamVersion'] as String?,
    );
  }
}
