import 'package:flutter/foundation.dart';
import '../services/ticket_service.dart';

/// 工单状态管理
class TicketProvider with ChangeNotifier {
  final TicketService _ticketService = TicketService();

  List<TicketListItemDto> _tickets = [];
  TicketDto? _currentTicket;
  bool _isLoading = false;
  String? _error;
  int _total = 0;
  int _currentPage = 1;
  final int _pageSize = 20;

  List<TicketListItemDto> get tickets => _tickets;
  TicketDto? get currentTicket => _currentTicket;
  bool get isLoading => _isLoading;
  String? get error => _error;
  int get total => _total;
  int get currentPage => _currentPage;
  int get pageSize => _pageSize;
  bool get hasMore => _tickets.length < _total;

  /// 加载工单列表
  Future<void> loadTickets({
    List<String>? status,
    String? customerId,
    String? deviceSn,
    String? domain,
    String? priority,
    bool refresh = false,
  }) async {
    if (_isLoading) return;

    _isLoading = true;
    if (refresh) {
      _currentPage = 1;
      _tickets = [];
    }
    notifyListeners();

    try {
      final result = await _ticketService.getTickets(
        status: status,
        customerId: customerId,
        deviceSn: deviceSn,
        domain: domain,
        priority: priority,
        page: _currentPage,
        pageSize: _pageSize,
      );

      if (refresh) {
        _tickets = result.items;
      } else {
        _tickets.addAll(result.items);
      }

      _total = result.total;
      _error = null;
    } catch (e) {
      _error = e.toString();
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  /// 加载更多
  Future<void> loadMore({
    List<String>? status,
    String? customerId,
    String? deviceSn,
    String? domain,
    String? priority,
  }) async {
    if (!hasMore || _isLoading) return;

    _currentPage++;
    await loadTickets(
      status: status,
      customerId: customerId,
      deviceSn: deviceSn,
      domain: domain,
      priority: priority,
      refresh: false,
    );
  }

  /// 加载工单详情
  Future<void> loadTicketDetail(String ticketId) async {
    _isLoading = true;
    notifyListeners();

    try {
      _currentTicket = await _ticketService.getTicket(ticketId);
      _error = null;
    } catch (e) {
      _error = e.toString();
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  /// 创建工单草稿
  Future<TicketDto?> createDraft(CreateTicketRequest request) async {
    _isLoading = true;
    _error = null;
    notifyListeners();

    try {
      final ticket = await _ticketService.createDraft(request);
      _currentTicket = ticket;
      _error = null;
      _isLoading = false;
      notifyListeners();
      return ticket;
    } catch (e) {
      _error = e.toString();
      _isLoading = false;
      notifyListeners();
      return null;
    }
  }

  /// 更新工单草稿
  Future<TicketDto?> updateDraft(String ticketId, UpdateTicketRequest request) async {
    _isLoading = true;
    _error = null;
    notifyListeners();

    try {
      final ticket = await _ticketService.updateDraft(ticketId, request);
      _currentTicket = ticket;
      _error = null;
      _isLoading = false;
      notifyListeners();
      return ticket;
    } catch (e) {
      _error = e.toString();
      _isLoading = false;
      notifyListeners();
      return null;
    }
  }

  /// 提交工单
  Future<SubmitTicketResult?> submitTicket(String ticketId) async {
    _isLoading = true;
    _error = null;
    notifyListeners();

    try {
      final result = await _ticketService.submitTicket(ticketId);
      _error = null;
      _isLoading = false;
      notifyListeners();
      return result;
    } catch (e) {
      _error = e.toString();
      _isLoading = false;
      notifyListeners();
      return null;
    }
  }

  /// 清除错误
  void clearError() {
    _error = null;
    notifyListeners();
  }

  /// 清除当前工单
  void clearCurrentTicket() {
    _currentTicket = null;
    notifyListeners();
  }
}
