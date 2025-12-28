import 'dart:io';
import 'package:flutter/material.dart';
import 'package:cached_network_image/cached_network_image.dart';
import 'package:intl/intl.dart';
import 'package:path_provider/path_provider.dart';
import '../../services/attachment_service.dart';
import '../../widgets/video_player_widget.dart';

/// 附件查看页面
class AttachmentsPage extends StatefulWidget {
  final String ticketId;

  const AttachmentsPage({
    super.key,
    required this.ticketId,
  });

  @override
  State<AttachmentsPage> createState() => _AttachmentsPageState();
}

class _AttachmentsPageState extends State<AttachmentsPage> {
  final AttachmentService _attachmentService = AttachmentService();
  List<AttachmentDto> _attachments = [];
  bool _isLoading = false;
  String? _error;

  @override
  void initState() {
    super.initState();
    _loadAttachments();
  }

  Future<void> _loadAttachments() async {
    setState(() {
      _isLoading = true;
      _error = null;
    });

    try {
      final attachments = await _attachmentService.getAttachments(widget.ticketId);
      setState(() {
        _attachments = attachments;
        _isLoading = false;
      });
    } catch (e) {
      setState(() {
        _error = e.toString();
        _isLoading = false;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('附件'),
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh),
            onPressed: _loadAttachments,
          ),
        ],
      ),
      body: _buildBody(),
    );
  }

  Widget _buildBody() {
    if (_isLoading) {
      return const Center(child: CircularProgressIndicator());
    }

    if (_error != null) {
      return Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Text('加载失败: $_error'),
            const SizedBox(height: 16),
            ElevatedButton(
              onPressed: _loadAttachments,
              child: const Text('重试'),
            ),
          ],
        ),
      );
    }

    if (_attachments.isEmpty) {
      return const Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(Icons.attach_file, size: 64, color: Colors.grey),
            SizedBox(height: 16),
            Text('暂无附件', style: TextStyle(color: Colors.grey)),
          ],
        ),
      );
    }

    return RefreshIndicator(
      onRefresh: _loadAttachments,
      child: GridView.builder(
        padding: const EdgeInsets.all(16),
        gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
          crossAxisCount: 2,
          crossAxisSpacing: 12,
          mainAxisSpacing: 12,
          childAspectRatio: 0.75,
        ),
        itemCount: _attachments.length,
        itemBuilder: (context, index) {
          return _buildAttachmentCard(_attachments[index]);
        },
      ),
    );
  }

  Widget _buildAttachmentCard(AttachmentDto attachment) {
    return Card(
      clipBehavior: Clip.antiAlias,
      child: InkWell(
        onTap: () => _viewAttachment(attachment),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Expanded(
              child: _buildThumbnail(attachment),
            ),
            Padding(
              padding: const EdgeInsets.all(8),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    attachment.fileName,
                    maxLines: 2,
                    overflow: TextOverflow.ellipsis,
                    style: const TextStyle(
                      fontSize: 12,
                      fontWeight: FontWeight.w500,
                    ),
                  ),
                  const SizedBox(height: 4),
                  Text(
                    attachment.formattedFileSize,
                    style: TextStyle(
                      fontSize: 10,
                      color: Colors.grey[600],
                    ),
                  ),
                  const SizedBox(height: 4),
                  Text(
                    _formatDateTime(attachment.uploadedAt),
                    style: TextStyle(
                      fontSize: 10,
                      color: Colors.grey[600],
                    ),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildThumbnail(AttachmentDto attachment) {
    if (attachment.isImage) {
      final imageUrl = _attachmentService.getAttachmentUrl(
        widget.ticketId,
        attachment.attachmentId,
      );

      return CachedNetworkImage(
        imageUrl: imageUrl,
        fit: BoxFit.cover,
        placeholder: (context, url) => const Center(
          child: CircularProgressIndicator(),
        ),
        errorWidget: (context, url, error) => const Center(
          child: Icon(Icons.error, color: Colors.red),
        ),
      );
    } else if (attachment.isVideo) {
      return Container(
        color: Colors.black12,
        child: const Center(
          child: Icon(Icons.play_circle_outline, size: 64, color: Colors.blue),
        ),
      );
    } else if (attachment.isDocument) {
      return Container(
        color: Colors.grey[100],
        child: const Center(
          child: Icon(Icons.description, size: 64, color: Colors.orange),
        ),
      );
    } else {
      return Container(
        color: Colors.grey[100],
        child: const Center(
          child: Icon(Icons.insert_drive_file, size: 64, color: Colors.grey),
        ),
      );
    }
  }

  Future<void> _viewAttachment(AttachmentDto attachment) async {
    if (attachment.isImage) {
      // 查看图片
      Navigator.push(
        context,
        MaterialPageRoute(
          builder: (context) => ImageViewPage(
            ticketId: widget.ticketId,
            attachment: attachment,
          ),
        ),
      );
    } else if (attachment.isVideo) {
      // 播放视频
      final videoUrl = _attachmentService.getAttachmentUrl(
        widget.ticketId,
        attachment.attachmentId,
      );
      Navigator.push(
        context,
        MaterialPageRoute(
          builder: (context) => VideoPlayerPage(
            videoUrl: videoUrl,
            title: attachment.fileName,
          ),
        ),
      );
    } else {
      // 显示附件详情和操作选项
      _showAttachmentOptions(attachment);
    }
  }

  void _showAttachmentOptions(AttachmentDto attachment) {
    showModalBottomSheet(
      context: context,
      builder: (context) {
        return SafeArea(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              ListTile(
                leading: const Icon(Icons.info),
                title: const Text('文件信息'),
                subtitle: Text(
                  '${attachment.fileName}\n'
                  '大小: ${attachment.formattedFileSize}\n'
                  '上传时间: ${_formatDateTime(attachment.uploadedAt)}',
                ),
              ),
              const Divider(),
              ListTile(
                leading: const Icon(Icons.download),
                title: const Text('下载'),
                onTap: () {
                  Navigator.pop(context);
                  _downloadAttachment(attachment);
                },
              ),
              if (attachment.isVideo)
                ListTile(
                  leading: const Icon(Icons.play_arrow),
                  title: const Text('播放'),
                  onTap: () {
                    Navigator.pop(context);
                    final videoUrl = _attachmentService.getAttachmentUrl(
                      widget.ticketId,
                      attachment.attachmentId,
                    );
                    Navigator.push(
                      context,
                      MaterialPageRoute(
                        builder: (context) => VideoPlayerPage(
                          videoUrl: videoUrl,
                          title: attachment.fileName,
                        ),
                      ),
                    );
                  },
                ),
              ListTile(
                leading: const Icon(Icons.delete, color: Colors.red),
                title: const Text('删除', style: TextStyle(color: Colors.red)),
                onTap: () {
                  Navigator.pop(context);
                  _deleteAttachment(attachment);
                },
              ),
            ],
          ),
        );
      },
    );
  }

  Future<void> _downloadAttachment(AttachmentDto attachment) async {
    try {
      // 获取下载目录
      Directory? directory;
      if (Platform.isAndroid) {
        // Android: 使用外部存储的 Downloads 目录
        directory = await getExternalStorageDirectory();
        if (directory != null) {
          // 创建 Downloads 子目录
          final downloadPath = Directory('${directory.path}/Downloads');
          if (!await downloadPath.exists()) {
            await downloadPath.create(recursive: true);
          }
          directory = downloadPath;
        }
      } else if (Platform.isIOS) {
        // iOS: 使用应用文档目录
        directory = await getApplicationDocumentsDirectory();
      }

      if (directory == null) {
        throw Exception('无法获取存储目录');
      }

      // 构建保存路径
      final savePath = '${directory.path}/${attachment.fileName}';

      // 显示下载进度对话框
      await showDialog(
        context: context,
        barrierDismissible: false,
        builder: (dialogContext) => _DownloadProgressDialog(
          fileName: attachment.fileName,
          onDownload: () async {
            return await _attachmentService.downloadAttachment(
              ticketId: widget.ticketId,
              attachmentId: attachment.attachmentId,
              savePath: savePath,
            );
          },
          savePath: savePath,
        ),
      );
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('下载失败: $e')),
        );
      }
    }
  }

  Future<void> _deleteAttachment(AttachmentDto attachment) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('确认删除'),
        content: Text('确定要删除 "${attachment.fileName}" 吗？'),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: const Text('取消'),
          ),
          TextButton(
            onPressed: () => Navigator.pop(context, true),
            child: const Text('删除', style: TextStyle(color: Colors.red)),
          ),
        ],
      ),
    );

    if (confirmed == true) {
      try {
        await _attachmentService.deleteAttachment(
          widget.ticketId,
          attachment.attachmentId,
        );
        _loadAttachments();
        if (mounted) {
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(content: Text('已删除')),
          );
        }
      } catch (e) {
        if (mounted) {
          ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(content: Text('删除失败: $e')),
          );
        }
      }
    }
  }

  String _formatDateTime(String dateTime) {
    try {
      final dt = DateTime.parse(dateTime);
      return DateFormat('yyyy-MM-dd HH:mm').format(dt);
    } catch (e) {
      return dateTime;
    }
  }
}

/// 图片查看页面
class ImageViewPage extends StatelessWidget {
  final String ticketId;
  final AttachmentDto attachment;

  const ImageViewPage({
    super.key,
    required this.ticketId,
    required this.attachment,
  });

  @override
  Widget build(BuildContext context) {
    final imageUrl = AttachmentService().getAttachmentUrl(
      ticketId,
      attachment.attachmentId,
    );

    return Scaffold(
      backgroundColor: Colors.black,
      appBar: AppBar(
        title: Text(attachment.fileName),
        backgroundColor: Colors.black,
      ),
      body: Center(
        child: InteractiveViewer(
          minScale: 0.5,
          maxScale: 4.0,
          child: CachedNetworkImage(
            imageUrl: imageUrl,
            placeholder: (context, url) => const CircularProgressIndicator(),
            errorWidget: (context, url, error) => const Icon(
              Icons.error,
              color: Colors.white,
              size: 64,
            ),
          ),
        ),
      ),
    );
  }
}

/// 下载进度对话框
class _DownloadProgressDialog extends StatefulWidget {
  final String fileName;
  final Future<File> Function() onDownload;
  final String savePath;

  const _DownloadProgressDialog({
    required this.fileName,
    required this.onDownload,
    required this.savePath,
  });

  @override
  State<_DownloadProgressDialog> createState() => _DownloadProgressDialogState();
}

class _DownloadProgressDialogState extends State<_DownloadProgressDialog> {
  double _progress = 0.0;
  bool _isDownloading = true;
  bool _hasError = false;
  String? _errorMessage;

  @override
  void initState() {
    super.initState();
    _startDownload();
  }

  Future<void> _startDownload() async {
    try {
      await widget.onDownload();

      if (mounted) {
        setState(() {
          _isDownloading = false;
          _progress = 1.0;
        });

        // 等待一小段时间显示完成状态
        await Future.delayed(const Duration(milliseconds: 500));

        if (mounted) {
          // 关闭进度对话框
          Navigator.pop(context);

          // 显示成功消息
          showDialog(
            context: context,
            builder: (context) => AlertDialog(
              title: const Text('下载成功'),
              content: Column(
                mainAxisSize: MainAxisSize.min,
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Text('文件已保存到:'),
                  const SizedBox(height: 8),
                  Text(
                    widget.savePath,
                    style: const TextStyle(fontSize: 12, color: Colors.grey),
                  ),
                ],
              ),
              actions: [
                TextButton(
                  onPressed: () => Navigator.pop(context),
                  child: const Text('确定'),
                ),
              ],
            ),
          );
        }
      }
    } catch (e) {
      if (mounted) {
        setState(() {
          _isDownloading = false;
          _hasError = true;
          _errorMessage = e.toString();
        });
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return AlertDialog(
      title: Text(_hasError ? '下载失败' : (_isDownloading ? '下载中' : '下载完成')),
      content: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Text(widget.fileName),
          const SizedBox(height: 16),
          if (_hasError)
            Column(
              children: [
                const Icon(Icons.error, color: Colors.red, size: 48),
                const SizedBox(height: 8),
                Text(
                  _errorMessage ?? '未知错误',
                  style: const TextStyle(color: Colors.red, fontSize: 12),
                  textAlign: TextAlign.center,
                ),
              ],
            )
          else ...[
            LinearProgressIndicator(value: _isDownloading ? null : 1.0),
            const SizedBox(height: 8),
            if (_isDownloading)
              const Text('正在下载...')
            else
              const Text('100%'),
          ],
        ],
      ),
      actions: [
        if (_hasError)
          TextButton(
            onPressed: () => Navigator.pop(context),
            child: const Text('关闭'),
          ),
      ],
    );
  }
}
