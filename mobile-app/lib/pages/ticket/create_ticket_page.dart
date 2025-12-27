import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../providers/ticket_provider.dart';
import '../../services/customer_service.dart';
import '../../services/ticket_service.dart';
import 'package:uuid/uuid.dart';

/// 创建工单页面
class CreateTicketPage extends StatefulWidget {
  const CreateTicketPage({super.key});

  @override
  State<CreateTicketPage> createState() => _CreateTicketPageState();
}

class _CreateTicketPageState extends State<CreateTicketPage> {
  final _formKey = GlobalKey<FormState>();
  final _customerService = CustomerService();

  // 表单字段
  CustomerDto? _selectedCustomer;
  DeviceDto? _selectedDevice;
  String _domain = 'A';
  final _stepCodeController = TextEditingController();
  final _symptomTitleController = TextEditingController();
  final _symptomDetailController = TextEditingController();
  final _swVersionController = TextEditingController();
  final _plcVersionController = TextEditingController();
  final _paramVersionController = TextEditingController();
  final _alarmCodeController = TextEditingController();
  int? _reproRate;
  bool? _rebootRecovers;
  bool? _envRelated;

  // 事实项
  final Map<String, dynamic> _factsJson = {};

  bool _isLoading = false;
  bool _isSavingDraft = false;

  @override
  void dispose() {
    _stepCodeController.dispose();
    _symptomTitleController.dispose();
    _symptomDetailController.dispose();
    _swVersionController.dispose();
    _plcVersionController.dispose();
    _paramVersionController.dispose();
    _alarmCodeController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('创建工单'),
        actions: [
          TextButton.icon(
            onPressed: _isLoading ? null : _saveDraft,
            icon: const Icon(Icons.save, color: Colors.white),
            label: const Text('保存草稿', style: TextStyle(color: Colors.white)),
          ),
        ],
      ),
      body: Form(
        key: _formKey,
        child: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            _buildSectionTitle('基本信息'),
            _buildCustomerSelector(),
            const SizedBox(height: 16),
            _buildDeviceSelector(),
            const SizedBox(height: 24),

            _buildSectionTitle('问题信息'),
            _buildDomainSelector(),
            const SizedBox(height: 16),
            _buildTextField(
              controller: _stepCodeController,
              label: '步骤代码',
              hint: '例如：STEP_01',
              required: true,
            ),
            const SizedBox(height: 16),
            _buildTextField(
              controller: _symptomTitleController,
              label: '问题标题',
              hint: '简要描述问题',
              required: true,
              maxLines: 2,
            ),
            const SizedBox(height: 16),
            _buildTextField(
              controller: _symptomDetailController,
              label: '问题详情',
              hint: '详细描述问题现象',
              maxLines: 5,
            ),
            const SizedBox(height: 16),
            _buildTextField(
              controller: _alarmCodeController,
              label: '报警代码',
              hint: '如有报警，请填写报警代码',
            ),
            const SizedBox(height: 24),

            _buildSectionTitle('版本信息'),
            _buildTextField(
              controller: _swVersionController,
              label: '软件版本',
              hint: '例如：v1.2.3',
              required: true,
            ),
            const SizedBox(height: 16),
            _buildTextField(
              controller: _plcVersionController,
              label: 'PLC 版本',
              hint: '例如：v2.0.1',
              required: true,
            ),
            const SizedBox(height: 16),
            _buildTextField(
              controller: _paramVersionController,
              label: '参数版本',
              hint: '例如：v1.0.0',
              required: true,
            ),
            const SizedBox(height: 24),

            _buildSectionTitle('问题特征'),
            _buildReproRateSelector(),
            const SizedBox(height: 16),
            _buildSwitchTile(
              title: '重启后是否恢复',
              value: _rebootRecovers,
              onChanged: (value) => setState(() => _rebootRecovers = value),
            ),
            const SizedBox(height: 8),
            _buildSwitchTile(
              title: '是否与环境相关',
              value: _envRelated,
              onChanged: (value) => setState(() => _envRelated = value),
            ),
            const SizedBox(height: 24),

            _buildSectionTitle('事实项（选填）'),
            _buildFactsSection(),
            const SizedBox(height: 32),

            _buildSubmitButton(),
            const SizedBox(height: 16),
          ],
        ),
      ),
    );
  }

  Widget _buildSectionTitle(String title) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 12),
      child: Text(
        title,
        style: const TextStyle(
          fontSize: 18,
          fontWeight: FontWeight.bold,
        ),
      ),
    );
  }

  Widget _buildCustomerSelector() {
    return Card(
      child: ListTile(
        leading: const Icon(Icons.business),
        title: Text(_selectedCustomer?.customerName ?? '选择客户'),
        subtitle: _selectedCustomer != null
            ? Text(_selectedCustomer!.contactPerson ?? '')
            : const Text('必填'),
        trailing: const Icon(Icons.chevron_right),
        onTap: _selectCustomer,
      ),
    );
  }

  Widget _buildDeviceSelector() {
    return Card(
      child: ListTile(
        leading: const Icon(Icons.devices),
        title: Text(_selectedDevice?.deviceSn ?? '选择设备'),
        subtitle: _selectedDevice != null
            ? Text('${_selectedDevice!.deviceModel} - ${_selectedDevice!.installLocation ?? ""}')
            : const Text('必填 - 请先选择客户'),
        trailing: const Icon(Icons.chevron_right),
        onTap: _selectedCustomer != null ? _selectDevice : null,
        enabled: _selectedCustomer != null,
      ),
    );
  }

  Widget _buildDomainSelector() {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text(
              '问题域 *',
              style: TextStyle(fontSize: 16, fontWeight: FontWeight.w500),
            ),
            const SizedBox(height: 12),
            Wrap(
              spacing: 8,
              children: ['A', 'B', 'C', 'D', 'E'].map((domain) {
                return ChoiceChip(
                  label: Text('问题域 $domain'),
                  selected: _domain == domain,
                  onSelected: (selected) {
                    if (selected) {
                      setState(() => _domain = domain);
                    }
                  },
                );
              }).toList(),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildTextField({
    required TextEditingController controller,
    required String label,
    String? hint,
    bool required = false,
    int maxLines = 1,
  }) {
    return TextFormField(
      controller: controller,
      decoration: InputDecoration(
        labelText: required ? '$label *' : label,
        hintText: hint,
        border: const OutlineInputBorder(),
      ),
      maxLines: maxLines,
      validator: required
          ? (value) {
              if (value == null || value.trim().isEmpty) {
                return '请填写$label';
              }
              return null;
            }
          : null,
    );
  }

  Widget _buildReproRateSelector() {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text(
              '复现率',
              style: TextStyle(fontSize: 16, fontWeight: FontWeight.w500),
            ),
            const SizedBox(height: 12),
            Wrap(
              spacing: 8,
              children: [
                for (int rate in [10, 30, 50, 70, 90, 100])
                  ChoiceChip(
                    label: Text('$rate%'),
                    selected: _reproRate == rate,
                    onSelected: (selected) {
                      setState(() => _reproRate = selected ? rate : null);
                    },
                  ),
              ],
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildSwitchTile({
    required String title,
    required bool? value,
    required ValueChanged<bool?> onChanged,
  }) {
    return Card(
      child: SwitchListTile(
        title: Text(title),
        value: value ?? false,
        tristate: true,
        onChanged: onChanged,
        subtitle: value == null
            ? const Text('未设置')
            : Text(value ? '是' : '否'),
      ),
    );
  }

  Widget _buildFactsSection() {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text('根据问题域和步骤，系统会提示需要填写的事实项'),
            const SizedBox(height: 8),
            const Text(
              '提示：提交后可以通过"问诊式补全"功能添加详细的事实项',
              style: TextStyle(fontSize: 12, color: Colors.grey),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildSubmitButton() {
    return ElevatedButton(
      onPressed: _isLoading ? null : _submitTicket,
      style: ElevatedButton.styleFrom(
        minimumSize: const Size(double.infinity, 50),
      ),
      child: _isLoading
          ? const SizedBox(
              height: 20,
              width: 20,
              child: CircularProgressIndicator(strokeWidth: 2),
            )
          : const Text('提交工单', style: TextStyle(fontSize: 16)),
    );
  }

  // 选择客户
  Future<void> _selectCustomer() async {
    final customers = await _customerService.getCustomers();

    if (!mounted) return;

    final selected = await showDialog<CustomerDto>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('选择客户'),
        content: SizedBox(
          width: double.maxFinite,
          child: ListView.builder(
            shrinkWrap: true,
            itemCount: customers.length,
            itemBuilder: (context, index) {
              final customer = customers[index];
              return ListTile(
                title: Text(customer.customerName),
                subtitle: Text(customer.contactPerson ?? ''),
                onTap: () => Navigator.pop(context, customer),
              );
            },
          ),
        ),
      ),
    );

    if (selected != null) {
      setState(() {
        _selectedCustomer = selected;
        _selectedDevice = null; // 重置设备选择
      });
    }
  }

  // 选择设备
  Future<void> _selectDevice() async {
    if (_selectedCustomer == null) return;

    final devices = await _customerService.getDevices(
      customerId: _selectedCustomer!.customerId,
    );

    if (!mounted) return;

    final selected = await showDialog<DeviceDto>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('选择设备'),
        content: SizedBox(
          width: double.maxFinite,
          child: ListView.builder(
            shrinkWrap: true,
            itemCount: devices.length,
            itemBuilder: (context, index) {
              final device = devices[index];
              return ListTile(
                title: Text(device.deviceSn),
                subtitle: Text('${device.deviceModel} - ${device.installLocation ?? ""}'),
                onTap: () => Navigator.pop(context, device),
              );
            },
          ),
        ),
      ),
    );

    if (selected != null) {
      setState(() {
        _selectedDevice = selected;
        // 自动填充版本信息
        if (selected.currentSwVersion != null) {
          _swVersionController.text = selected.currentSwVersion!;
        }
        if (selected.currentPlcVersion != null) {
          _plcVersionController.text = selected.currentPlcVersion!;
        }
        if (selected.currentParamVersion != null) {
          _paramVersionController.text = selected.currentParamVersion!;
        }
      });
    }
  }

  // 保存草稿
  Future<void> _saveDraft() async {
    if (_selectedDevice == null) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('请先选择客户和设备')),
      );
      return;
    }

    setState(() => _isSavingDraft = true);

    try {
      final request = CreateTicketRequest(
        localDraftId: const Uuid().v4(),
        deviceId: _selectedDevice!.deviceId,
        domain: _domain,
        stepCode: _stepCodeController.text.trim().isEmpty
            ? 'DRAFT'
            : _stepCodeController.text.trim(),
        symptomTitle: _symptomTitleController.text.trim().isEmpty
            ? '草稿'
            : _symptomTitleController.text.trim(),
        symptomDetail: _symptomDetailController.text.trim().isEmpty
            ? null
            : _symptomDetailController.text.trim(),
        reproRate: _reproRate,
        rebootRecovers: _rebootRecovers,
        envRelated: _envRelated,
        swVersion: _swVersionController.text.trim().isEmpty
            ? 'unknown'
            : _swVersionController.text.trim(),
        plcVersion: _plcVersionController.text.trim().isEmpty
            ? 'unknown'
            : _plcVersionController.text.trim(),
        paramVersion: _paramVersionController.text.trim().isEmpty
            ? 'unknown'
            : _paramVersionController.text.trim(),
        factsJson: _factsJson,
        alarmCode: _alarmCodeController.text.trim().isEmpty
            ? null
            : _alarmCodeController.text.trim(),
        confirmedAsFact: false,
      );

      final ticket = await context.read<TicketProvider>().createDraft(request);

      if (mounted && ticket != null) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('草稿已保存')),
        );
        Navigator.pop(context);
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('保存失败: $e')),
        );
      }
    } finally {
      if (mounted) {
        setState(() => _isSavingDraft = false);
      }
    }
  }

  // 提交工单
  Future<void> _submitTicket() async {
    if (!_formKey.currentState!.validate()) {
      return;
    }

    if (_selectedDevice == null) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('请选择客户和设备')),
      );
      return;
    }

    setState(() => _isLoading = true);

    try {
      // 1. 先创建草稿
      final request = CreateTicketRequest(
        localDraftId: const Uuid().v4(),
        deviceId: _selectedDevice!.deviceId,
        domain: _domain,
        stepCode: _stepCodeController.text.trim(),
        symptomTitle: _symptomTitleController.text.trim(),
        symptomDetail: _symptomDetailController.text.trim().isEmpty
            ? null
            : _symptomDetailController.text.trim(),
        reproRate: _reproRate,
        rebootRecovers: _rebootRecovers,
        envRelated: _envRelated,
        swVersion: _swVersionController.text.trim(),
        plcVersion: _plcVersionController.text.trim(),
        paramVersion: _paramVersionController.text.trim(),
        factsJson: _factsJson,
        alarmCode: _alarmCodeController.text.trim().isEmpty
            ? null
            : _alarmCodeController.text.trim(),
        confirmedAsFact: true,
      );

      final ticket = await context.read<TicketProvider>().createDraft(request);

      if (ticket == null) {
        throw Exception('创建草稿失败');
      }

      // 2. 提交工单
      final result = await context.read<TicketProvider>().submitTicket(ticket.ticketId);

      if (mounted) {
        if (result != null && result.success) {
          ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(content: Text('工单已提交: ${result.ticketNo}')),
          );
          Navigator.pop(context);
        } else if (result != null && result.errors != null) {
          // 显示验证错误
          final errors = result.errors!.map((e) => e.message).join('\n');
          ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(content: Text('提交失败:\n$errors')),
          );
        } else {
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(content: Text('提交失败，请重试')),
          );
        }
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('提交失败: $e')),
        );
      }
    } finally {
      if (mounted) {
        setState(() => _isLoading = false);
      }
    }
  }
}
