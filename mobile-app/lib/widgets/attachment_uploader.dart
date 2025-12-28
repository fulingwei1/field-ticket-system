import 'dart:io';
import 'package:flutter/material.dart';
import 'package:image_picker/image_picker.dart';
import 'package:file_picker/file_picker.dart';

/// 附件数据
class AttachmentData {
  final File file;
  final String category;
  String? description;
  double uploadProgress;

  AttachmentData({
    required this.file,
    required this.category,
    this.description,
    this.uploadProgress = 0.0,
  });

  String get fileName => file.path.split('/').last;

  Future<int> get fileSize => file.length();

  String get formattedFileSize {
    return file.lengthSync().toString();
  }
}

/// 附件上传器组件
class AttachmentUploader extends StatefulWidget {
  final List<AttachmentData> attachments;
  final Function(AttachmentData) onAttachmentAdded;
  final Function(AttachmentData) onAttachmentRemoved;
  final int? maxFiles;
  final int? maxFileSize; // 字节

  const AttachmentUploader({
    super.key,
    required this.attachments,
    required this.onAttachmentAdded,
    required this.onAttachmentRemoved,
    this.maxFiles = 10,
    this.maxFileSize = 50 * 1024 * 1024, // 50MB
  });

  @override
  State<AttachmentUploader> createState() => _AttachmentUploaderState();
}

class _AttachmentUploaderState extends State<AttachmentUploader> {
  final ImagePicker _imagePicker = ImagePicker();

  @override
  Widget build(BuildContext context) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                const Text(
                  '附件',
                  style: TextStyle(
                    fontSize: 16,
                    fontWeight: FontWeight.bold,
                  ),
                ),
                if (widget.maxFiles != null)
                  Text(
                    '${widget.attachments.length}/${widget.maxFiles}',
                    style: TextStyle(
                      fontSize: 12,
                      color: Colors.grey[600],
                    ),
                  ),
              ],
            ),
            const SizedBox(height: 12),
            _buildActionButtons(),
            if (widget.attachments.isNotEmpty) ...[
              const SizedBox(height: 12),
              _buildAttachmentList(),
            ],
          ],
        ),
      ),
    );
  }

  Widget _buildActionButtons() {
    final canAddMore = widget.maxFiles == null || widget.attachments.length < widget.maxFiles!;

    return Wrap(
      spacing: 8,
      runSpacing: 8,
      children: [
        ElevatedButton.icon(
          onPressed: canAddMore ? _pickImage : null,
          icon: const Icon(Icons.photo_camera, size: 20),
          label: const Text('拍照'),
          style: ElevatedButton.styleFrom(
            padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
          ),
        ),
        ElevatedButton.icon(
          onPressed: canAddMore ? _pickImageFromGallery : null,
          icon: const Icon(Icons.photo_library, size: 20),
          label: const Text('相册'),
          style: ElevatedButton.styleFrom(
            padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
          ),
        ),
        ElevatedButton.icon(
          onPressed: canAddMore ? _pickVideo : null,
          icon: const Icon(Icons.videocam, size: 20),
          label: const Text('视频'),
          style: ElevatedButton.styleFrom(
            padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
          ),
        ),
        OutlinedButton.icon(
          onPressed: canAddMore ? _pickFile : null,
          icon: const Icon(Icons.attach_file, size: 20),
          label: const Text('文件'),
          style: OutlinedButton.styleFrom(
            padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
          ),
        ),
      ],
    );
  }

  Widget _buildAttachmentList() {
    return Column(
      children: widget.attachments.map((attachment) {
        return _buildAttachmentItem(attachment);
      }).toList(),
    );
  }

  Widget _buildAttachmentItem(AttachmentData attachment) {
    return Card(
      margin: const EdgeInsets.only(bottom: 8),
      child: ListTile(
        leading: _buildFileIcon(attachment),
        title: Text(
          attachment.fileName,
          maxLines: 1,
          overflow: TextOverflow.ellipsis,
        ),
        subtitle: attachment.uploadProgress > 0 && attachment.uploadProgress < 1
            ? LinearProgressIndicator(value: attachment.uploadProgress)
            : FutureBuilder<int>(
                future: attachment.fileSize,
                builder: (context, snapshot) {
                  if (snapshot.hasData) {
                    return Text(_formatFileSize(snapshot.data!));
                  }
                  return const Text('计算中...');
                },
              ),
        trailing: IconButton(
          icon: const Icon(Icons.delete, color: Colors.red),
          onPressed: () => _removeAttachment(attachment),
        ),
      ),
    );
  }

  Widget _buildFileIcon(AttachmentData attachment) {
    final fileName = attachment.fileName.toLowerCase();

    if (fileName.endsWith('.jpg') ||
        fileName.endsWith('.jpeg') ||
        fileName.endsWith('.png') ||
        fileName.endsWith('.gif')) {
      return Image.file(
        attachment.file,
        width: 48,
        height: 48,
        fit: BoxFit.cover,
      );
    } else if (fileName.endsWith('.mp4') ||
        fileName.endsWith('.mov') ||
        fileName.endsWith('.avi')) {
      return const Icon(Icons.video_file, size: 48, color: Colors.blue);
    } else if (fileName.endsWith('.pdf')) {
      return const Icon(Icons.picture_as_pdf, size: 48, color: Colors.red);
    } else {
      return const Icon(Icons.insert_drive_file, size: 48, color: Colors.grey);
    }
  }

  Future<void> _pickImage() async {
    try {
      final XFile? image = await _imagePicker.pickImage(
        source: ImageSource.camera,
        maxWidth: 1920,
        maxHeight: 1920,
        imageQuality: 85,
      );

      if (image != null) {
        _addAttachment(File(image.path), 'photo', '现场拍照');
      }
    } catch (e) {
      _showError('拍照失败: $e');
    }
  }

  Future<void> _pickImageFromGallery() async {
    try {
      final XFile? image = await _imagePicker.pickImage(
        source: ImageSource.gallery,
        maxWidth: 1920,
        maxHeight: 1920,
        imageQuality: 85,
      );

      if (image != null) {
        _addAttachment(File(image.path), 'photo', '相册图片');
      }
    } catch (e) {
      _showError('选择图片失败: $e');
    }
  }

  Future<void> _pickVideo() async {
    try {
      final XFile? video = await _imagePicker.pickVideo(
        source: ImageSource.gallery,
        maxDuration: const Duration(minutes: 5),
      );

      if (video != null) {
        _addAttachment(File(video.path), 'video', '视频');
      }
    } catch (e) {
      _showError('选择视频失败: $e');
    }
  }

  Future<void> _pickFile() async {
    try {
      final result = await FilePicker.platform.pickFiles(
        type: FileType.any,
        allowMultiple: false,
      );

      if (result != null && result.files.single.path != null) {
        final file = File(result.files.single.path!);
        _addAttachment(file, 'document', '文档');
      }
    } catch (e) {
      _showError('选择文件失败: $e');
    }
  }

  Future<void> _addAttachment(File file, String category, String description) async {
    // 检查文件大小
    final fileSize = await file.length();
    if (widget.maxFileSize != null && fileSize > widget.maxFileSize!) {
      _showError('文件太大，最大允许 ${_formatFileSize(widget.maxFileSize!)}');
      return;
    }

    // 检查数量限制
    if (widget.maxFiles != null && widget.attachments.length >= widget.maxFiles!) {
      _showError('最多只能上传 ${widget.maxFiles} 个文件');
      return;
    }

    final attachment = AttachmentData(
      file: file,
      category: category,
      description: description,
    );

    widget.onAttachmentAdded(attachment);
  }

  void _removeAttachment(AttachmentData attachment) {
    widget.onAttachmentRemoved(attachment);
  }

  void _showError(String message) {
    if (mounted) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text(message), backgroundColor: Colors.red),
      );
    }
  }

  String _formatFileSize(int bytes) {
    if (bytes < 1024) {
      return '$bytes B';
    } else if (bytes < 1024 * 1024) {
      return '${(bytes / 1024).toStringAsFixed(1)} KB';
    } else if (bytes < 1024 * 1024 * 1024) {
      return '${(bytes / (1024 * 1024)).toStringAsFixed(1)} MB';
    } else {
      return '${(bytes / (1024 * 1024 * 1024)).toStringAsFixed(1)} GB';
    }
  }
}
