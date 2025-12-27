import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import '../../models/verification_models.dart';
import '../../services/verification_service.dart';
import '../../services/auth_service.dart';

/// 验证结果提交页面
class VerificationPage extends StatefulWidget {
  final String ticketId;
  final String ticketNo;
  final VerificationService verificationService;

  const VerificationPage({
    super.key,
    required this.ticketId,
    required this.ticketNo,
    required this.verificationService,
  });

  @override
  State<VerificationPage> createState() => _VerificationPageState();
}

class _VerificationPageState extends State<VerificationPage> {
  final _formKey = GlobalKey<FormState>();
  final _runCountController = TextEditingController(text: '1');
  final _passCountController = TextEditingController(text: '0');
  final _failCountController = TextEditingController(text: '0');
  final _noteController = TextEditingController();

  bool _isSubmitting = false;

  @override
  void dispose() {
    _runCountController.dispose();
    _passCountController.dispose();
    _failCountController.dispose();
    _noteController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('提交验证结果'),
      ),
      body: Form(
        key: _formKey,
        child: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            // 工单信息卡片
            Card(
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      '工单编号',
                      style: TextStyle(
                        fontSize: 12,
                        color: Colors.grey[600],
                      ),
                    ),
                    const SizedBox(height: 4),
                    Text(
                      widget.ticketNo,
                      style: const TextStyle(
                        fontSize: 18,
                        fontWeight: FontWeight.bold,
                      ),
                    ),
                  ],
                ),
              ),
            ),
            const SizedBox(height: 24),

            // 说明卡片
            Card(
              color: Colors.blue[50],
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Row(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Icon(
                      Icons.info_outline,
                      color: Colors.blue[700],
                      size: 20,
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: Text(
                        '请如实填写验证结果。验证次数 = 通过次数 + 失败次数',
                        style: TextStyle(
                          fontSize: 14,
                          color: Colors.blue[900],
                        ),
                      ),
                    ),
                  ],
                ),
              ),
            ),
            const SizedBox(height: 24),

            // 验证统计
            const Text(
              '验证统计',
              style: TextStyle(
                fontSize: 16,
                fontWeight: FontWeight.bold,
              ),
            ),
            const SizedBox(height: 16),

            // 验证次数
            TextFormField(
              controller: _runCountController,
              decoration: const InputDecoration(
                labelText: '验证次数',
                hintText: '请输入验证次数',
                prefixIcon: Icon(Icons.loop),
                border: OutlinedBorder(),
              ),
              keyboardType: TextInputType.number,
              inputFormatters: [FilteringTextInputFormatter.digitsOnly],
              validator: (value) {
                if (value == null || value.isEmpty) {
                  return '请输入验证次数';
                }
                final count = int.tryParse(value);
                if (count == null || count < 1) {
                  return '验证次数必须大于0';
                }
                return null;
              },
              onChanged: _updateCounts,
            ),
            const SizedBox(height: 16),

            // 通过次数
            TextFormField(
              controller: _passCountController,
              decoration: InputDecoration(
                labelText: '通过次数',
                hintText: '请输入通过次数',
                prefixIcon: const Icon(Icons.check_circle),
                border: const OutlinedBorder(),
                suffixIcon: IconButton(
                  icon: const Icon(Icons.add),
                  onPressed: () => _incrementCount(_passCountController),
                ),
              ),
              keyboardType: TextInputType.number,
              inputFormatters: [FilteringTextInputFormatter.digitsOnly],
              validator: (value) {
                if (value == null || value.isEmpty) {
                  return '请输入通过次数';
                }
                final count = int.tryParse(value);
                if (count == null || count < 0) {
                  return '通过次数不能为负数';
                }
                return null;
              },
              onChanged: _updateCounts,
            ),
            const SizedBox(height: 16),

            // 失败次数
            TextFormField(
              controller: _failCountController,
              decoration: InputDecoration(
                labelText: '失败次数',
                hintText: '请输入失败次数',
                prefixIcon: const Icon(Icons.cancel),
                border: const OutlinedBorder(),
                suffixIcon: IconButton(
                  icon: const Icon(Icons.add),
                  onPressed: () => _incrementCount(_failCountController),
                ),
              ),
              keyboardType: TextInputType.number,
              inputFormatters: [FilteringTextInputFormatter.digitsOnly],
              validator: (value) {
                if (value == null || value.isEmpty) {
                  return '请输入失败次数';
                }
                final count = int.tryParse(value);
                if (count == null || count < 0) {
                  return '失败次数不能为负数';
                }
                return null;
              },
              onChanged: _updateCounts,
            ),
            const SizedBox(height: 24),

            // 验证备注
            const Text(
              '验证备注',
              style: TextStyle(
                fontSize: 16,
                fontWeight: FontWeight.bold,
              ),
            ),
            const SizedBox(height: 16),

            TextFormField(
              controller: _noteController,
              decoration: const InputDecoration(
                labelText: '备注（可选）',
                hintText: '请输入验证备注，如遇到的问题、观察到的现象等',
                border: OutlinedBorder(),
                alignLabelWithHint: true,
              ),
              maxLines: 5,
              maxLength: 500,
            ),
            const SizedBox(height: 32),

            // 提交按钮
            ElevatedButton(
              onPressed: _isSubmitting ? null : _submitVerification,
              style: ElevatedButton.styleFrom(
                minimumSize: const Size(double.infinity, 48),
              ),
              child: _isSubmitting
                  ? const SizedBox(
                      height: 20,
                      width: 20,
                      child: CircularProgressIndicator(strokeWidth: 2),
                    )
                  : const Text(
                      '提交验证结果',
                      style: TextStyle(fontSize: 16),
                    ),
            ),
          ],
        ),
      ),
    );
  }

  void _incrementCount(TextEditingController controller) {
    final current = int.tryParse(controller.text) ?? 0;
    controller.text = (current + 1).toString();
    _updateCounts(controller.text);
  }

  void _updateCounts(String _) {
    setState(() {
      // 自动计算验证次数
      final pass = int.tryParse(_passCountController.text) ?? 0;
      final fail = int.tryParse(_failCountController.text) ?? 0;
      final total = pass + fail;
      if (total > 0) {
        _runCountController.text = total.toString();
      }
    });
  }

  Future<void> _submitVerification() async {
    if (!_formKey.currentState!.validate()) {
      return;
    }

    final runCount = int.parse(_runCountController.text);
    final passCount = int.parse(_passCountController.text);
    final failCount = int.parse(_failCountController.text);

    // 验证：运行次数应该等于通过次数+失败次数
    if (runCount != passCount + failCount) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('验证次数必须等于通过次数加失败次数'),
          backgroundColor: Colors.red,
        ),
      );
      return;
    }

    setState(() {
      _isSubmitting = true;
    });

    try {
      final request = SubmitVerificationRequest(
        runCount: runCount,
        passCount: passCount,
        failCount: failCount,
        note: _noteController.text.trim().isEmpty ? null : _noteController.text.trim(),
      );

      await widget.verificationService.submitVerification(
        widget.ticketId,
        request,
      );

      if (!mounted) return;

      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('验证结果提交成功'),
          backgroundColor: Colors.green,
        ),
      );

      // 返回上一页，并传递结果
      Navigator.of(context).pop(true);
    } catch (e) {
      if (!mounted) return;

      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text('提交失败: $e'),
          backgroundColor: Colors.red,
        ),
      );
    } finally {
      if (mounted) {
        setState(() {
          _isSubmitting = false;
        });
      }
    }
  }
}
