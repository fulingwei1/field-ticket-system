import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../providers/auth_provider.dart';
import '../providers/ticket_provider.dart';

/// 首页 - 工作台
class HomePage extends StatefulWidget {
  const HomePage({super.key});

  @override
  State<HomePage> createState() => _HomePageState();
}

class _HomePageState extends State<HomePage> {
  int _selectedIndex = 0;

  @override
  void initState() {
    super.initState();
    // 初始化加载数据
    WidgetsBinding.instance.addPostFrameCallback((_) {
      _loadData();
    });
  }

  Future<void> _loadData() async {
    final ticketProvider = context.read<TicketProvider>();
    await ticketProvider.loadTickets(refresh: true);
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('现场问题反馈系统'),
        actions: [
          IconButton(
            icon: const Icon(Icons.person),
            onPressed: () => _showUserMenu(context),
          ),
        ],
      ),
      body: _buildBody(),
      floatingActionButton: _selectedIndex == 0
          ? FloatingActionButton.extended(
              onPressed: () => Navigator.pushNamed(context, '/tickets/create'),
              icon: const Icon(Icons.add),
              label: const Text('创建工单'),
            )
          : null,
      bottomNavigationBar: BottomNavigationBar(
        currentIndex: _selectedIndex,
        onTap: (index) {
          setState(() {
            _selectedIndex = index;
          });
        },
        items: const [
          BottomNavigationBarItem(
            icon: Icon(Icons.list_alt),
            label: '工单',
          ),
          BottomNavigationBarItem(
            icon: Icon(Icons.dashboard),
            label: '工作台',
          ),
          BottomNavigationBarItem(
            icon: Icon(Icons.person),
            label: '我的',
          ),
        ],
      ),
    );
  }

  Widget _buildBody() {
    switch (_selectedIndex) {
      case 0:
        return _buildTicketList();
      case 1:
        return _buildDashboard();
      case 2:
        return _buildProfile();
      default:
        return _buildTicketList();
    }
  }

  /// 工单列表
  Widget _buildTicketList() {
    return Consumer<TicketProvider>(
      builder: (context, provider, child) {
        if (provider.isLoading && provider.tickets.isEmpty) {
          return const Center(child: CircularProgressIndicator());
        }

        if (provider.error != null && provider.tickets.isEmpty) {
          return Center(
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                Text('加载失败: ${provider.error}'),
                const SizedBox(height: 16),
                ElevatedButton(
                  onPressed: _loadData,
                  child: const Text('重试'),
                ),
              ],
            ),
          );
        }

        if (provider.tickets.isEmpty) {
          return Center(
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                const Icon(Icons.inbox, size: 64, color: Colors.grey),
                const SizedBox(height: 16),
                const Text('暂无工单'),
                const SizedBox(height: 16),
                ElevatedButton(
                  onPressed: () => Navigator.pushNamed(context, '/tickets/create'),
                  child: const Text('创建第一个工单'),
                ),
              ],
            ),
          );
        }

        return RefreshIndicator(
          onRefresh: _loadData,
          child: ListView.builder(
            itemCount: provider.tickets.length + (provider.hasMore ? 1 : 0),
            itemBuilder: (context, index) {
              if (index == provider.tickets.length) {
                // 加载更多
                if (!provider.isLoading) {
                  provider.loadMore();
                }
                return const Padding(
                  padding: EdgeInsets.all(16.0),
                  child: Center(child: CircularProgressIndicator()),
                );
              }

              final ticket = provider.tickets[index];
              return _buildTicketCard(ticket);
            },
          ),
        );
      },
    );
  }

  /// 工单卡片
  Widget _buildTicketCard(TicketListItemDto ticket) {
    return Card(
      margin: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
      child: ListTile(
        title: Text(
          ticket.symptomTitle,
          maxLines: 2,
          overflow: TextOverflow.ellipsis,
        ),
        subtitle: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const SizedBox(height: 4),
            Text('${ticket.customerName} - ${ticket.deviceSn}'),
            const SizedBox(height: 4),
            Row(
              children: [
                _buildStatusChip(ticket.status),
                const SizedBox(width: 8),
                _buildPriorityChip(ticket.priority),
              ],
            ),
          ],
        ),
        trailing: const Icon(Icons.chevron_right),
        onTap: () {
          Navigator.pushNamed(context, '/tickets/${ticket.ticketId}');
        },
      ),
    );
  }

  /// 状态标签
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
      label: Text(label, style: const TextStyle(fontSize: 12)),
      backgroundColor: color.withOpacity(0.2),
      labelStyle: TextStyle(color: color),
      visualDensity: VisualDensity.compact,
    );
  }

  /// 优先级标签
  Widget _buildPriorityChip(String priority) {
    Color color;
    String label;
    switch (priority) {
      case 'P1':
        color = Colors.red;
        label = 'P1';
        break;
      case 'P2':
        color = Colors.orange;
        label = 'P2';
        break;
      case 'P3':
        color = Colors.blue;
        label = 'P3';
        break;
      default:
        color = Colors.grey;
        label = priority;
    }

    return Chip(
      label: Text(label, style: const TextStyle(fontSize: 12)),
      backgroundColor: color.withOpacity(0.2),
      labelStyle: TextStyle(color: color),
      visualDensity: VisualDensity.compact,
    );
  }

  /// 工作台
  Widget _buildDashboard() {
    return const Center(
      child: Text('工作台功能开发中...'),
    );
  }

  /// 个人中心
  Widget _buildProfile() {
    return Consumer<AuthProvider>(
      builder: (context, authProvider, child) {
        final user = authProvider.user;

        return ListView(
          children: [
            const SizedBox(height: 32),
            CircleAvatar(
              radius: 50,
              backgroundColor: Theme.of(context).colorScheme.primary,
              child: Text(
                user?.name.substring(0, 1) ?? '?',
                style: const TextStyle(fontSize: 32, color: Colors.white),
              ),
            ),
            const SizedBox(height: 16),
            Text(
              user?.name ?? '未登录',
              textAlign: TextAlign.center,
              style: const TextStyle(fontSize: 20, fontWeight: FontWeight.bold),
            ),
            const SizedBox(height: 8),
            Text(
              user?.role ?? '',
              textAlign: TextAlign.center,
              style: TextStyle(fontSize: 14, color: Colors.grey[600]),
            ),
            if (user?.mobile != null) ...[
              const SizedBox(height: 4),
              Text(
                user!.mobile!,
                textAlign: TextAlign.center,
                style: TextStyle(fontSize: 14, color: Colors.grey[600]),
              ),
            ],
            const SizedBox(height: 32),
            ListTile(
              leading: const Icon(Icons.exit_to_app),
              title: const Text('退出登录'),
              onTap: () => _handleLogout(context),
            ),
          ],
        );
      },
    );
  }

  /// 显示用户菜单
  void _showUserMenu(BuildContext context) {
    final user = context.read<AuthProvider>().user;

    showModalBottomSheet(
      context: context,
      builder: (context) {
        return SafeArea(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              ListTile(
                leading: const Icon(Icons.person),
                title: Text(user?.name ?? '未登录'),
                subtitle: Text(user?.role ?? ''),
              ),
              const Divider(),
              ListTile(
                leading: const Icon(Icons.exit_to_app),
                title: const Text('退出登录'),
                onTap: () {
                  Navigator.pop(context);
                  _handleLogout(context);
                },
              ),
            ],
          ),
        );
      },
    );
  }

  /// 处理登出
  Future<void> _handleLogout(BuildContext context) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('确认退出'),
        content: const Text('确定要退出登录吗？'),
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

    if (confirmed == true && context.mounted) {
      await context.read<AuthProvider>().logout();
      if (context.mounted) {
        Navigator.pushReplacementNamed(context, '/');
      }
    }
  }
}
