import 'dart:convert';
import 'dart:io';
import 'package:http/http.dart' as http;
import 'auth_service.dart';

/// 附件服务
class AttachmentService {
  final AuthService _authService = AuthService();
  static const String _apiBaseUrl = 'http://localhost:5000';

  /// 上传附件
  Future<AttachmentDto> uploadAttachment({
    required String ticketId,
    required File file,
    required String category,
    String? description,
    Function(double)? onProgress,
  }) async {
    final uri = Uri.parse('$_apiBaseUrl/api/tickets/$ticketId/attachments');
    final token = await _authService.getToken();

    if (token == null) {
      throw Exception('未登录');
    }

    // 创建 multipart request
    final request = http.MultipartRequest('POST', uri);
    request.headers['Authorization'] = 'Bearer $token';

    // 添加文件
    final fileStream = http.ByteStream(file.openRead());
    final fileLength = await file.length();
    final multipartFile = http.MultipartFile(
      'file',
      fileStream,
      fileLength,
      filename: file.path.split('/').last,
    );
    request.files.add(multipartFile);

    // 添加其他字段
    request.fields['category'] = category;
    if (description != null) {
      request.fields['description'] = description;
    }

    // 发送请求（带进度）
    final streamedResponse = await request.send();

    // 监听上传进度
    if (onProgress != null) {
      int bytesSent = 0;
      streamedResponse.stream.listen(
        (chunk) {
          bytesSent += chunk.length;
          onProgress(bytesSent / fileLength);
        },
      );
    }

    // 获取响应
    final response = await http.Response.fromStream(streamedResponse);

    if (response.statusCode != 200) {
      throw Exception('上传失败: ${response.body}');
    }

    final data = json.decode(response.body) as Map<String, dynamic>;
    return AttachmentDto.fromJson(data);
  }

  /// 获取工单的附件列表
  Future<List<AttachmentDto>> getAttachments(String ticketId) async {
    final url = Uri.parse('$_apiBaseUrl/api/tickets/$ticketId/attachments');
    final headers = await _authService.getAuthHeaders();

    final response = await http.get(url, headers: headers);

    if (response.statusCode != 200) {
      throw Exception('获取附件列表失败');
    }

    final data = json.decode(response.body) as List;
    return data.map((e) => AttachmentDto.fromJson(e as Map<String, dynamic>)).toList();
  }

  /// 获取附件下载URL
  String getAttachmentUrl(String ticketId, String attachmentId) {
    return '$_apiBaseUrl/api/tickets/$ticketId/attachments/$attachmentId';
  }

  /// 下载附件
  Future<File> downloadAttachment({
    required String ticketId,
    required String attachmentId,
    required String savePath,
    Function(double)? onProgress,
  }) async {
    final url = getAttachmentUrl(ticketId, attachmentId);
    final headers = await _authService.getAuthHeaders();

    final request = http.Request('GET', Uri.parse(url));
    request.headers.addAll(headers);

    final streamedResponse = await request.send();

    if (streamedResponse.statusCode != 200) {
      throw Exception('下载失败');
    }

    final file = File(savePath);
    final sink = file.openWrite();

    int bytesReceived = 0;
    final contentLength = streamedResponse.contentLength ?? 0;

    await for (var chunk in streamedResponse.stream) {
      sink.add(chunk);
      bytesReceived += chunk.length;
      if (onProgress != null && contentLength > 0) {
        onProgress(bytesReceived / contentLength);
      }
    }

    await sink.close();
    return file;
  }

  /// 删除附件
  Future<void> deleteAttachment(String ticketId, String attachmentId) async {
    final url = Uri.parse('$_apiBaseUrl/api/tickets/$ticketId/attachments/$attachmentId');
    final headers = await _authService.getAuthHeaders();

    final response = await http.delete(url, headers: headers);

    if (response.statusCode != 200) {
      throw Exception('删除附件失败');
    }
  }
}

/// 附件 DTO
class AttachmentDto {
  final String attachmentId;
  final String ticketId;
  final String fileName;
  final String filePath;
  final int fileSize;
  final String mimeType;
  final String category;
  final String? description;
  final String uploadedBy;
  final String uploadedAt;
  final String? thumbnailPath;

  AttachmentDto({
    required this.attachmentId,
    required this.ticketId,
    required this.fileName,
    required this.filePath,
    required this.fileSize,
    required this.mimeType,
    required this.category,
    this.description,
    required this.uploadedBy,
    required this.uploadedAt,
    this.thumbnailPath,
  });

  factory AttachmentDto.fromJson(Map<String, dynamic> json) {
    return AttachmentDto(
      attachmentId: json['attachmentId'] as String,
      ticketId: json['ticketId'] as String,
      fileName: json['fileName'] as String,
      filePath: json['filePath'] as String,
      fileSize: json['fileSize'] as int,
      mimeType: json['mimeType'] as String,
      category: json['category'] as String,
      description: json['description'] as String?,
      uploadedBy: json['uploadedBy'] as String,
      uploadedAt: json['uploadedAt'] as String,
      thumbnailPath: json['thumbnailPath'] as String?,
    );
  }

  /// 获取格式化的文件大小
  String get formattedFileSize {
    if (fileSize < 1024) {
      return '$fileSize B';
    } else if (fileSize < 1024 * 1024) {
      return '${(fileSize / 1024).toStringAsFixed(1)} KB';
    } else if (fileSize < 1024 * 1024 * 1024) {
      return '${(fileSize / (1024 * 1024)).toStringAsFixed(1)} MB';
    } else {
      return '${(fileSize / (1024 * 1024 * 1024)).toStringAsFixed(1)} GB';
    }
  }

  /// 判断是否是图片
  bool get isImage {
    return mimeType.startsWith('image/');
  }

  /// 判断是否是视频
  bool get isVideo {
    return mimeType.startsWith('video/');
  }

  /// 判断是否是文档
  bool get isDocument {
    return mimeType.contains('pdf') ||
        mimeType.contains('document') ||
        mimeType.contains('sheet') ||
        mimeType.contains('text');
  }
}
