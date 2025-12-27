import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:intl/intl.dart';
import '../../providers/ticket_provider.dart';
import '../../services/ticket_service.dart';

/// 工单详情页面
class TicketDetailPage extends StatefulWidget {
  final String ticketId;

  const TicketDetailPage({
    super.key,
    required this.ticketId,
  });

  @override
  State<TicketDetailPage> createState() => _TicketDetailPageState();
}

class _TicketDetailPageState extends State<TicketDetailPage> {
  @override
  void initState() {
    super.initState();
    _loadTicketDetail();
  }

  Future<void> _loadTicketDetail() async {
    await context.read<TicketProvider>().loadTicketDetail(widget.ticketId);
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('工单详情'),
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh),
            onPressed: _loadTicketDetail,
          ),
        ],
      ),
      body: Consumer<TicketProvider>(
        builder: (context, provider, child) {
          if (provider.isLoading) {
            return const Center(child: CircularProgressIndicator());
          }

          if (provider.error != null) {
            return Center(
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  Text('加载失败: ${provider.error}'),
                  const SizedBox(height: 16),
                  ElevatedButton(
                    onPressed: _loadTicketDetail,
                    child: const Text('重试'),
                  ),
                ],
              ),
            );
          }

          final ticket = provider.currentTicket;
          if (ticket == null) {
            return const Center(child: Text('工单不存在'));
          }

          return RefreshIndicator(
            onRefresh: _loadTicketDetail,
            child: ListView(
              padding: const EdgeInsets.all(16),
              children: [
                _buildTicketHeader(ticket),
                const SizedBox(height: 16),
                _buildStatusSection(ticket),
                const SizedBox(height: 16),
                _buildBasicInfo(ticket),
                const SizedBox(height: 16),
                _buildProblemInfo(ticket),
                const SizedBox(height: 16),
                _buildVersionInfo(ticket),
                const SizedBox(height: 16),
                _buildCharacteristics(ticket),
                const SizedBox(height: 16),
                _buildFactsInfo(ticket),
                const SizedBox(height: 16),
                _buildTimelineSection(ticket),
                const SizedBox(height: 16),
                _buildActionButtons(ticket),
              ],
            ),
          );
        },
      ),
    );
  }

  Widget _buildTicketHeader(TicketDto ticket) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Expanded(
                  child: Text(
                    ticket.ticketNo,
                    style: const TextStyle(
                      fontSize: 20,
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                ),
                _buildPriorityChip(ticket.priority),
              ],
            ),
            const SizedBox(height: 8),
            Text(
              ticket.symptomTitle,
              style: const TextStyle(fontSize: 16),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildStatusSection(TicketDto ticket) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text(
              '工单状态',
              style: TextStyle(
                fontSize: 16,
                fontWeight: FontWeight.bold,
              ),
            ),
            const SizedBox(height: 12),
            Row(
              children: [
                _buildStatusChip(ticket.status),
                if (ticket.assignedTo != null) ...[
                  const SizedBox(width: 8),
                  Chip(
                    avatar: const Icon(Icons.person, size: 16),
                    label: Text('分配给: ${ticket.assignedTo}'),
                    visualDensity: VisualDensity.compact,
                  ),
                ],
              ],
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildBasicInfo(TicketDto ticket) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text(
              '基本信息',
              style: TextStyle(
                fontSize: 16,
                fontWeight: FontWeight.bold,
              ),
            ),
            const Divider(),
            _buildInfoRow('设备 SN', ticket.deviceId),
            _buildInfoRow('问题域', 'Domain ${ticket.domain}'),
            _buildInfoRow('步骤代码', ticket.stepCode),
            if (ticket.stepName != null)
              _buildInfoRow('步骤名称', ticket.stepName!),
            if (ticket.alarmCode != null)
              _buildInfoRow('报警代码', ticket.alarmCode!),
          ],
        ),
      ),
    );
  }

  Widget _buildProblemInfo(TicketDto ticket) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text(
              '问题描述',
              style: TextStyle(
                fontSize: 16,
                fontWeight: FontWeight.bold,
              ),
            ),
            const Divider(),
            if (ticket.symptomDetail != null) ...[
              Text(ticket.symptomDetail!),
              const SizedBox(height: 12),
            ],
            if (ticket.actionsTaken.isNotEmpty) ...[
              const Text(
                '已采取的措施:',
                style: TextStyle(fontWeight: FontWeight.w500),
              ),
              const SizedBox(height: 4),
              ...ticket.actionsTaken.map((action) => Padding(
                    padding: const EdgeInsets.only(left: 8, top: 4),
                    child: Row(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        const Text('• '),
                        Expanded(child: Text(action)),
                      ],
                    ),
                  )),
            ],
            if (ticket.actionsTakenNote != null) ...[
              const SizedBox(height: 8),
              Text('备注: ${ticket.actionsTakenNote}'),
            ],
          ],
        ),
      ),
    );
  }

  Widget _buildVersionInfo(TicketDto ticket) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text(
              '版本信息',
              style: TextStyle(
                fontSize: 16,
                fontWeight: FontWeight.bold,
              ),
            ),
            const Divider(),
            _buildInfoRow('软件版本', ticket.swVersion),
            _buildInfoRow('PLC 版本', ticket.plcVersion),
            _buildInfoRow('参数版本', ticket.paramVersion),
          ],
        ),
      ),
    );
  }

  Widget _buildCharacteristics(TicketDto ticket) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text(
              '问题特征',
              style: TextStyle(
                fontSize: 16,
                fontWeight: FontWeight.bold,
              ),
            ),
            const Divider(),
            if (ticket.reproRate != null)
              _buildInfoRow('复现率', '${ticket.reproRate}%'),
            if (ticket.rebootRecovers != null)
              _buildInfoRow('重启后恢复', ticket.rebootRecovers! ? '是' : '否'),
            if (ticket.envRelated != null)
              _buildInfoRow('环境相关', ticket.envRelated! ? '是' : '否'),
            _buildInfoRow(
              '事实已确认',
              ticket.confirmedAsFact ? '是' : '否',
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildFactsInfo(TicketDto ticket) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text(
              '事实项',
              style: TextStyle(
                fontSize: 16,
                fontWeight: FontWeight.bold,
              ),
            ),
            const Divider(),
            if (ticket.factsJson.isEmpty)
              const Text(
                '暂无事实项数据',
                style: TextStyle(color: Colors.grey),
              )
            else
              ...ticket.factsJson.entries.map((entry) {
                return Padding(
                  padding: const EdgeInsets.symmetric(vertical: 4),
                  child: Row(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      SizedBox(
                        width: 120,
                        child: Text(
                          entry.key,
                          style: const TextStyle(fontWeight: FontWeight.w500),
                        ),
                      ),
                      Expanded(
                        child: Text(entry.value.toString()),
                      ),
                    ],
                  ),
                );
              }),
          ],
        ),
      ),
    );
  }

  Widget _buildTimelineSection(TicketDto ticket) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text(
              '时间轴',
              style: TextStyle(
                fontSize: 16,
                fontWeight: FontWeight.bold,
              ),
            ),
            const Divider(),
            _buildTimelineItem(
              '创建',
              _formatDateTime(ticket.createdAt),
              Icons.create,
              Colors.blue,
            ),
            if (ticket.submittedAt != null)
              _buildTimelineItem(
                '提交',
                _formatDateTime(ticket.submittedAt!),
                Icons.send,
                Colors.orange,
              ),
            if (ticket.closedAt != null)
              _buildTimelineItem(
                '关闭',
                _formatDateTime(ticket.closedAt!),
                Icons.check_circle,
                Colors.green,
              ),
          ],
        ),
      ),
    );
  }

  Widget _buildTimelineItem(
    String title,
    String time,
    IconData icon,
    Color color,
  ) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 8),
      child: Row(
        children: [
          Icon(icon, color: color, size: 24),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  title,
                  style: const TextStyle(fontWeight: FontWeight.w500),
                ),
                Text(
                  time,
                  style: TextStyle(
                    fontSize: 12,
                    color: Colors.grey[600],
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildInfoRow(String label, String value) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 4),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SizedBox(
            width: 100,
            child: Text(
              label,
              style: TextStyle(
                color: Colors.grey[600],
                fontSize: 14,
              ),
            ),
          ),
          Expanded(
            child: Text(
              value,
              style: const TextStyle(fontSize: 14),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildStatusChip(String status) {
    Color color;
    String label;
    switch (status) {
      case 'Draft':
        color = Colors.grey;
        label = '草稿';
        break;
      case 'Submitted':
        color = Colors.blue;
        label = '已提交';
        break;
      case 'Triage':
        color = Colors.orange;
        label = '分诊中';
        break;
      case 'SolutionIssued':
        color = Colors.purple;
        label = '方案已发布';
        break;
      case 'Verifying':
        color = Colors.teal;
        label = '验证中';
        break;
      case 'Closed':
        color = Colors.green;
        label = '已关闭';
        break;
      default:
        color = Colors.grey;
        label = status;
    }

    return Chip(
      label: Text(label),
      backgroundColor: color.withOpacity(0.2),
      labelStyle: TextStyle(color: color, fontWeight: FontWeight.bold),
    );
  }

  Widget _buildPriorityChip(String priority) {
    Color color;
    switch (priority) {
      case 'P1':
        color = Colors.red;
        break;
      case 'P2':
        color = Colors.orange;
        break;
      case 'P3':
        color = Colors.blue;
        break;
      default:
        color = Colors.grey;
    }

    return Chip(
      label: Text(priority),
      backgroundColor: color.withOpacity(0.2),
      labelStyle: TextStyle(color: color, fontWeight: FontWeight.bold),
    );
  }

  Widget _buildActionButtons(TicketDto ticket) {
    final buttons = <Widget>[];

    // 根据状态显示不同的操作按钮
    if (ticket.status == 'Draft') {
      buttons.add(
        ElevatedButton.icon(
          onPressed: () => _submitTicket(ticket),
          icon: const Icon(Icons.send),
          label: const Text('提交工单'),
          style: ElevatedButton.styleFrom(
            minimumSize: const Size(double.infinity, 48),
          ),
        ),
      );
    }

    if (ticket.status == 'Submitted' || ticket.status == 'Triage') {
      buttons.add(
        OutlinedButton.icon(
          onPressed: () => _goToMissingInfo(ticket),
          icon: const Icon(Icons.quiz),
          label: const Text('问诊式补全'),
          style: OutlinedButton.styleFrom(
            minimumSize: const Size(double.infinity, 48),
          ),
        ),
      );
    }

    if (ticket.attachmentCount > 0) {
      buttons.add(
        OutlinedButton.icon(
          onPressed: () => _viewAttachments(ticket),
          icon: const Icon(Icons.attachment),
          label: Text('查看附件 (${ticket.attachmentCount})'),
          style: OutlinedButton.styleFrom(
            minimumSize: const Size(double.infinity, 48),
          ),
        ),
      );
    } else {
      buttons.add(
        OutlinedButton.icon(
          onPressed: () => _uploadAttachment(ticket),
          icon: const Icon(Icons.upload_file),
          label: const Text('上传附件'),
          style: OutlinedButton.styleFrom(
            minimumSize: const Size(double.infinity, 48),
          ),
        ),
      );
    }

    return Column(
      children: buttons
          .map((btn) => Padding(
                padding: const EdgeInsets.only(bottom: 12),
                child: btn,
              ))
          .toList(),
    );
  }

  String _formatDateTime(String dateTime) {
    try {
      final dt = DateTime.parse(dateTime);
      return DateFormat('yyyy-MM-dd HH:mm').format(dt);
    } catch (e) {
      return dateTime;
    }
  }

  Future<void> _submitTicket(TicketDto ticket) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('确认提交'),
        content: const Text('确定要提交此工单吗？'),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: const Text('取消'),
          ),
          TextButton(
            onPressed: () => Navigator.pop(context, true),
            child: const Text('确定'),
          ),
        ],
      ),
    );

    if (confirmed == true && mounted) {
      final result = await context.read<TicketProvider>().submitTicket(ticket.ticketId);
      if (mounted) {
        if (result != null && result.success) {
          ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(content: Text('工单已提交: ${result.ticketNo}')),
          );
          _loadTicketDetail();
        } else {
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(content: Text('提交失败')),
          );
        }
      }
    }
  }

  void _goToMissingInfo(TicketDto ticket) {
    Navigator.pushNamed(
      context,
      '/tickets/${ticket.ticketId}/missing-info',
    ).then((_) => _loadTicketDetail());
  }

  void _viewAttachments(TicketDto ticket) {
    ScaffoldMessenger.of(context).showSnackBar(
      const SnackBar(content: Text('附件查看功能开发中...')),
    );
  }

  void _uploadAttachment(TicketDto ticket) {
    ScaffoldMessenger.of(context).showSnackBar(
      const SnackBar(content: Text('附件上传功能开发中...')),
    );
  }
}
