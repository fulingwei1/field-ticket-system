import 'package:flutter/material.dart';
import '../../services/ticket_service.dart';
import '../../models/missing_info_models.dart';

/// 问诊式补全缺失信息页面
class MissingInfoQuestionnairePage extends StatefulWidget {
  final String ticketId;
  final String? jcCode;

  const MissingInfoQuestionnairePage({
    Key? key,
    required this.ticketId,
    this.jcCode,
  }) : super(key: key);

  @override
  State<MissingInfoQuestionnairePage> createState() =>
      _MissingInfoQuestionnairePageState();
}

class _MissingInfoQuestionnairePageState
    extends State<MissingInfoQuestionnairePage> {
  final TicketService _ticketService = TicketService();
  final _formKey = GlobalKey<FormState>();
  
  List<QuestionItem> _questions = [];
  bool _hasCriticalMissing = false;
  bool _loading = true;
  bool _submitting = false;
  Map<String, dynamic> _answers = {};

  @override
  void initState() {
    super.initState();
    _loadMissingInfo();
  }

  /// 加载缺失信息
  Future<void> _loadMissingInfo() async {
    try {
      setState(() => _loading = true);
      
      final result = await _ticketService.getMissingInfo(
        widget.ticketId,
        widget.jcCode,
      );
      
      setState(() {
        _questions = result.questions;
        _hasCriticalMissing = result.hasCriticalMissing;
        _loading = false;
      });

      // 初始化答案
      _answers = {};
      for (var question in _questions) {
        if (question.type == 'yes_no') {
          _answers[question.questionId] = null;
        } else if (question.type == 'number') {
          _answers[question.questionId] = null;
        } else if (question.type == 'text') {
          _answers[question.questionId] = '';
        } else if (question.type == 'select') {
          _answers[question.questionId] = null;
        }
      }
    } catch (e) {
      setState(() => _loading = false);
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('加载失败: $e')),
        );
      }
    }
  }

  /// 提交补全信息
  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) {
      return;
    }

    setState(() => _submitting = true);

    try {
      final request = CompleteMissingInfoRequest(
        answers: _answers,
      );

      await _ticketService.completeMissingInfo(widget.ticketId, request);

      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('补全成功')),
        );
        Navigator.of(context).pop(true);
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('提交失败: $e')),
        );
      }
    } finally {
      if (mounted) {
        setState(() => _submitting = false);
      }
    }
  }

  /// 跳过
  void _skip() {
    if (_hasCriticalMissing) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('有关键信息缺失，无法跳过')),
      );
      return;
    }

    showDialog(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('确认跳过'),
        content: const Text('确定要跳过信息补全吗？'),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(context).pop(),
            child: const Text('取消'),
          ),
          TextButton(
            onPressed: () {
              Navigator.of(context).pop();
              Navigator.of(context).pop();
            },
            child: const Text('确定'),
          ),
        ],
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('信息补全'),
        actions: [
          if (_hasCriticalMissing)
            const Padding(
              padding: EdgeInsets.all(16.0),
              child: Center(
                child: Text(
                  '有关键信息缺失',
                  style: TextStyle(color: Colors.red, fontSize: 12),
                ),
              ),
            ),
        ],
      ),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : _questions.isEmpty
              ? Center(
                  child: Column(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      const Icon(Icons.check_circle, color: Colors.green, size: 64),
                      const SizedBox(height: 16),
                      const Text('没有缺失信息，可以直接提交工单'),
                    ],
                  ),
                )
              : Form(
                  key: _formKey,
                  child: Column(
                    children: [
                      if (_hasCriticalMissing)
                        Container(
                          width: double.infinity,
                          padding: const EdgeInsets.all(16),
                          color: Colors.orange.shade50,
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              const Text(
                                '检测到关键信息缺失',
                                style: TextStyle(
                                  fontWeight: FontWeight.bold,
                                  color: Colors.orange,
                                ),
                              ),
                              const SizedBox(height: 8),
                              const Text(
                                '为了更准确地诊断问题，请补充以下关键信息',
                                style: TextStyle(fontSize: 12),
                              ),
                            ],
                          ),
                        ),
                      Expanded(
                        child: ListView.builder(
                          padding: const EdgeInsets.all(16),
                          itemCount: _questions.length,
                          itemBuilder: (context, index) {
                            final question = _questions[index];
                            return _buildQuestionWidget(question);
                          },
                        ),
                      ),
                      Container(
                        padding: const EdgeInsets.all(16),
                        decoration: BoxDecoration(
                          color: Colors.white,
                          boxShadow: [
                            BoxShadow(
                              color: Colors.black.withOpacity(0.1),
                              blurRadius: 4,
                              offset: const Offset(0, -2),
                            ),
                          ],
                        ),
                        child: Row(
                          children: [
                            Expanded(
                              child: OutlinedButton(
                                onPressed: _hasCriticalMissing ? null : _skip,
                                child: const Text('跳过'),
                              ),
                            ),
                            const SizedBox(width: 16),
                            Expanded(
                              flex: 2,
                              child: ElevatedButton(
                                onPressed: _submitting ? null : _submit,
                                child: _submitting
                                    ? const SizedBox(
                                        width: 20,
                                        height: 20,
                                        child: CircularProgressIndicator(
                                          strokeWidth: 2,
                                        ),
                                      )
                                    : const Text('完成补全'),
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

  /// 构建问题输入组件
  Widget _buildQuestionWidget(QuestionItem question) {
    return Card(
      margin: const EdgeInsets.only(bottom: 16),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Expanded(
                  child: Text(
                    question.question,
                    style: const TextStyle(
                      fontSize: 16,
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                ),
                if (question.required)
                  Container(
                    padding: const EdgeInsets.symmetric(
                      horizontal: 8,
                      vertical: 4,
                    ),
                    decoration: BoxDecoration(
                      color: Colors.red.shade50,
                      borderRadius: BorderRadius.circular(4),
                    ),
                    child: const Text(
                      '必填',
                      style: TextStyle(
                        color: Colors.red,
                        fontSize: 12,
                      ),
                    ),
                  ),
              ],
            ),
            if (question.hint != null) ...[
              const SizedBox(height: 8),
              Text(
                question.hint!,
                style: TextStyle(
                  fontSize: 12,
                  color: Colors.grey.shade600,
                ),
              ),
            ],
            const SizedBox(height: 16),
            _buildAnswerInput(question),
          ],
        ),
      ),
    );
  }

  /// 构建答案输入组件
  Widget _buildAnswerInput(QuestionItem question) {
    switch (question.type) {
      case 'yes_no':
        return Row(
          children: [
            Expanded(
              child: RadioListTile<String>(
                title: const Text('是'),
                value: 'yes',
                groupValue: _answers[question.questionId] as String?,
                onChanged: (value) {
                  setState(() {
                    _answers[question.questionId] = value;
                  });
                },
              ),
            ),
            Expanded(
              child: RadioListTile<String>(
                title: const Text('否'),
                value: 'no',
                groupValue: _answers[question.questionId] as String?,
                onChanged: (value) {
                  setState(() {
                    _answers[question.questionId] = value;
                  });
                },
              ),
            ),
            Expanded(
              child: RadioListTile<String>(
                title: const Text('不清楚'),
                value: 'unknown',
                groupValue: _answers[question.questionId] as String?,
                onChanged: (value) {
                  setState(() {
                    _answers[question.questionId] = value;
                  });
                },
              ),
            ),
          ],
        );

      case 'number':
        return TextFormField(
          keyboardType: TextInputType.number,
          decoration: const InputDecoration(
            hintText: '请输入数字',
            border: OutlineInputBorder(),
          ),
          validator: question.required
              ? (value) {
                  if (value == null || value.isEmpty) {
                    return '请输入';
                  }
                  return null;
                }
              : null,
          onSaved: (value) {
            if (value != null && value.isNotEmpty) {
              _answers[question.questionId] = int.tryParse(value);
            }
          },
        );

      case 'text':
        return TextFormField(
          maxLines: 4,
          decoration: const InputDecoration(
            hintText: '请输入文本',
            border: OutlineInputBorder(),
          ),
          validator: question.required
              ? (value) {
                  if (value == null || value.isEmpty) {
                    return '请输入';
                  }
                  return null;
                }
              : null,
          onSaved: (value) {
            _answers[question.questionId] = value ?? '';
          },
        );

      case 'select':
        return DropdownButtonFormField<String>(
          decoration: const InputDecoration(
            hintText: '请选择',
            border: OutlineInputBorder(),
          ),
          items: question.options
              ?.map((option) => DropdownMenuItem(
                    value: option,
                    child: Text(option),
                  ))
              .toList(),
          validator: question.required
              ? (value) {
                  if (value == null || value.isEmpty) {
                    return '请选择';
                  }
                  return null;
                }
              : null,
          onChanged: (value) {
            setState(() {
              _answers[question.questionId] = value;
            });
          },
        );

      case 'file':
        return OutlinedButton.icon(
          onPressed: () {
            // TODO: 实现文件上传
            ScaffoldMessenger.of(context).showSnackBar(
              const SnackBar(content: Text('文件上传功能待实现')),
            );
          },
          icon: const Icon(Icons.upload_file),
          label: const Text('上传文件'),
        );

      default:
        return const SizedBox.shrink();
    }
  }
}

