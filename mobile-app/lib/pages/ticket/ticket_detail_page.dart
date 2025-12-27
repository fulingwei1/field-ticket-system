import 'package:flutter/material.dart';

/// 工单详情页面
class TicketDetailPage extends StatelessWidget {
  final String ticketId;

  const TicketDetailPage({
    super.key,
    required this.ticketId,
  });

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('工单详情'),
      ),
      body: Center(
        child: Text('工单ID: $ticketId\n\n详情页面开发中...'),
      ),
    );
  }
}
