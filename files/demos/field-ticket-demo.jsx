import React, { useState } from 'react';

// 演示数据
const demoTickets = [
  {
    id: 'TK-20251218-001',
    customer: '某新能源汽车公司',
    device: 'BMS-EOL-2024-0156',
    domain: 'C',
    domainName: 'PLC/程序',
    step: 'Step_120_Clamp_Check',
    symptom: '夹具到位后程序超时，设备不继续动作',
    status: 'Triage',
    priority: 'P1',
    swVersion: 'v2.1.3',
    plcVersion: 'v1.2.6',
    paramVersion: 'v3.4',
    reproRate: 100,
    createdAt: '2024-12-18 09:30',
    creator: '张工',
    facts: {
      mechanical: { action_completed: 'YES', position_reliable: 'YES', jam_or_noise: 'NO' },
      electrical: { sensor_physical_ok: 'YES', plc_io_changes: 'NO', similar_points_ok: 'YES' },
      plc: { stuck_step_code: 'Step_120', stuck_fixed: 'YES', manual_single_step_pass: 'YES', alarm_code: 'E001' }
    },
    jcCode: 'JC-017'
  },
  {
    id: 'TK-20251218-002',
    customer: '某白色家电集团',
    device: 'PCB-TEST-2024-0089',
    domain: 'D',
    domainName: '测试/判定',
    step: 'Step_045_Insulation',
    symptom: '绝缘电阻测试值波动大，同一产品重复测试结果不一致',
    status: 'Submitted',
    priority: 'P2',
    swVersion: 'v1.8.5',
    plcVersion: 'v2.0.1',
    paramVersion: 'v1.2',
    reproRate: 70,
    createdAt: '2024-12-18 10:15',
    creator: '李工',
    facts: {
      test: { same_unit_repeat_consistent: 'NO', swap_unit_recovers: 'YES', near_limits: 'YES', fail_item: '绝缘电阻', value: '4.8MΩ', ll: '5.0MΩ', ul: '500MΩ' },
      environment: { repro_rate: 70, reboot_recovers: 'NO', env_related: 'YES' }
    }
  },
  {
    id: 'TK-20251217-008',
    customer: '某动力电池制造商',
    device: 'PACK-EOL-2024-0234',
    domain: 'B',
    domainName: '电气/IO',
    step: 'Step_080_HVIL_Check',
    symptom: '高压互锁检测偶发报警，重启后恢复',
    status: 'SolutionIssued',
    priority: 'P2',
    swVersion: 'v3.2.0',
    plcVersion: 'v1.5.2',
    paramVersion: 'v2.1',
    reproRate: 30,
    createdAt: '2024-12-17 14:20',
    creator: '王工',
    facts: {
      electrical: { sensor_physical_ok: 'YES', plc_io_changes: 'YES', similar_points_ok: 'YES' },
      environment: { repro_rate: 30, reboot_recovers: 'YES', env_related: 'NO' }
    },
    jcCode: 'JC-023',
    solution: 'SOL-2025-031'
  }
];

const demoJudgementCards = [
  {
    code: 'JC-017',
    version: '1.2',
    title: '夹具到位信号与程序判定一致性',
    domains: ['C'],
    steps: ['Step_120', 'Step_130', 'Step_140'],
    keywords: ['夹具', '到位', '超时', 'IO', '滤波'],
    target: '判断夹具物理到位后，PLC是否正确识别到位信号',
    keyFactors: ['物理到位状态', 'IO信号变化', '程序步骤位置', '报警代码'],
    decisionBoundary: [
      { condition: '机械OK + 传感器OK + IO无变化 + 固定步骤', conclusion: '程序逻辑/IO判定窗口问题', confidence: 'high' },
      { condition: '机械OK + 传感器异常', conclusion: '传感器故障或安装问题', confidence: 'high' },
      { condition: '机械动作未完成', conclusion: '机械卡滞或气压不足', confidence: 'medium' }
    ],
    failureModes: [
      { mode: 'IO去抖不足', symptom: '信号抖动导致误判', hint: '增加滤波时间至50-100ms' },
      { mode: '超时窗口过短', symptom: '正常动作也超时', hint: '延长超时时间300→800ms' },
      { mode: '传感器安装松动', symptom: '到位信号不稳定', hint: '检查传感器固定和感应距离' }
    ],
    usageCount: 47,
    successRate: 94
  },
  {
    code: 'JC-023',
    version: '2.0',
    title: '高压互锁(HVIL)检测异常',
    domains: ['B', 'C'],
    steps: ['Step_080', 'Step_085'],
    keywords: ['HVIL', '互锁', '高压', '偶发', '接触'],
    target: '判断高压互锁回路检测异常的根本原因',
    keyFactors: ['互锁回路阻值', '接插件状态', '检测电压', '环境因素'],
    decisionBoundary: [
      { condition: '偶发 + 重启恢复 + 阻值临界', conclusion: '接插件接触不良或阻值漂移', confidence: 'high' },
      { condition: '固定报警 + 阻值异常', conclusion: '互锁回路断路或短路', confidence: 'high' },
      { condition: '偶发 + 温度相关', conclusion: '温度导致阻值变化超限', confidence: 'medium' }
    ],
    failureModes: [
      { mode: '接插件接触电阻增大', symptom: '偶发报警，振动时加剧', hint: '检查接插件端子，必要时更换' },
      { mode: '检测阈值设置过严', symptom: '边界值频繁报警', hint: '调整检测上限阈值+10%' },
      { mode: '线束老化', symptom: '阻值缓慢上升', hint: '检查线束绝缘，测量回路阻值' }
    ],
    usageCount: 32,
    successRate: 91
  },
  {
    code: 'JC-031',
    version: '1.0',
    title: '绝缘电阻测试值波动',
    domains: ['D'],
    steps: ['Step_045', 'Step_046'],
    keywords: ['绝缘', '电阻', '波动', '重复性', 'IR'],
    target: '判断绝缘电阻测试结果不稳定的原因',
    keyFactors: ['测试值分布', '环境湿度', '探针接触', '测试电压'],
    decisionBoundary: [
      { condition: '波动大 + 换产品恢复 + 接近限值', conclusion: '产品本身绝缘性能边界', confidence: 'medium' },
      { condition: '波动大 + 换产品不恢复', conclusion: '测试系统问题（探针/仪器）', confidence: 'high' },
      { condition: '波动 + 湿度相关', conclusion: '环境湿度影响测量', confidence: 'high' }
    ],
    failureModes: [
      { mode: '探针接触不良', symptom: '测试值随机跳变', hint: '清洁探针，检查压力和对位' },
      { mode: '测试电压不稳', symptom: '测试值整体偏移', hint: '校准仪器，检查高压模块' },
      { mode: '环境湿度过高', symptom: '雨天/潮湿时波动加剧', hint: '控制环境湿度<60%RH' }
    ],
    usageCount: 28,
    successRate: 88
  }
];

const demoSolutions = [
  {
    code: 'SOL-2025-031',
    ticketId: 'TK-20251217-008',
    jcCode: 'JC-023',
    title: 'HVIL检测阈值优化',
    diagnosis: '接插件接触电阻增大导致检测值偶发超限',
    changes: [
      { type: 'PARAMETER', location: 'HVIL检测上限', before: '100Ω', after: '120Ω', reason: '适应接触电阻正常波动范围' },
      { type: 'PLC_LOGIC', location: 'Step_080 检测逻辑', before: '单次检测', after: '3次检测取中值', reason: '过滤偶发干扰' }
    ],
    releaseType: 'PARAM',
    releaseVersion: 'v2.2',
    verification: {
      precheck: ['确认设备停止', '记录当前参数'],
      steps: [
        { id: 'S1', text: '下载参数 v2.2', required: true },
        { id: 'S2', text: '连续运行50次HVIL检测', required: true },
        { id: 'S3', text: '记录检测值分布', required: false }
      ],
      acceptance: '50次检测无误报警',
      rollback: '恢复参数 v2.1'
    },
    status: 'Published',
    publishedAt: '2024-12-17 16:30'
  }
];

// 状态颜色映射
const statusColors = {
  Draft: { bg: 'bg-gray-100', text: 'text-gray-600', label: '草稿' },
  Submitted: { bg: 'bg-blue-100', text: 'text-blue-700', label: '待分诊' },
  Triage: { bg: 'bg-amber-100', text: 'text-amber-700', label: '分诊中' },
  SolutionIssued: { bg: 'bg-purple-100', text: 'text-purple-700', label: '已出方案' },
  Verifying: { bg: 'bg-cyan-100', text: 'text-cyan-700', label: '验证中' },
  Closed: { bg: 'bg-green-100', text: 'text-green-700', label: '已关闭' }
};

const priorityColors = {
  P1: 'bg-red-500',
  P2: 'bg-orange-500',
  P3: 'bg-yellow-500',
  P4: 'bg-gray-400'
};

const domainInfo = {
  A: { name: '机械/动作', icon: '⚙️', color: 'bg-slate-500' },
  B: { name: '电气/IO', icon: '⚡', color: 'bg-yellow-500' },
  C: { name: 'PLC/程序', icon: '💻', color: 'bg-blue-500' },
  D: { name: '测试/判定', icon: '📊', color: 'bg-green-500' },
  E: { name: '环境/偶发', icon: '🌡️', color: 'bg-purple-500' }
};

// 主应用
export default function FieldTicketDemo() {
  const [activeTab, setActiveTab] = useState('dashboard');
  const [selectedTicket, setSelectedTicket] = useState(null);
  const [selectedJC, setSelectedJC] = useState(null);
  const [showCreateWizard, setShowCreateWizard] = useState(false);
  const [wizardStep, setWizardStep] = useState(1);

  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-900 via-slate-800 to-slate-900">
      {/* 顶部导航 */}
      <nav className="bg-slate-800/80 backdrop-blur-xl border-b border-slate-700/50 sticky top-0 z-50">
        <div className="max-w-7xl mx-auto px-4">
          <div className="flex items-center justify-between h-16">
            <div className="flex items-center space-x-3">
              <div className="w-10 h-10 rounded-xl bg-gradient-to-br from-cyan-400 to-blue-500 flex items-center justify-center shadow-lg shadow-cyan-500/20">
                <span className="text-white font-bold text-lg">JK</span>
              </div>
              <div>
                <h1 className="text-white font-semibold tracking-tight">现场问题结构化上报系统</h1>
                <p className="text-slate-400 text-xs">Field Issue Tracking System</p>
              </div>
            </div>
            
            <div className="flex items-center space-x-2">
              {[
                { id: 'dashboard', label: '工作台', icon: '📊' },
                { id: 'tickets', label: '工单', icon: '📋' },
                { id: 'judgement', label: '判断卡', icon: '🎯' },
                { id: 'solutions', label: '解决方案', icon: '💡' }
              ].map(tab => (
                <button
                  key={tab.id}
                  onClick={() => { setActiveTab(tab.id); setSelectedTicket(null); setSelectedJC(null); }}
                  className={`px-4 py-2 rounded-lg text-sm font-medium transition-all duration-200 ${
                    activeTab === tab.id
                      ? 'bg-cyan-500/20 text-cyan-400 shadow-lg shadow-cyan-500/10'
                      : 'text-slate-400 hover:text-white hover:bg-slate-700/50'
                  }`}
                >
                  <span className="mr-2">{tab.icon}</span>
                  {tab.label}
                </button>
              ))}
            </div>

            <div className="flex items-center space-x-4">
              <button 
                onClick={() => setShowCreateWizard(true)}
                className="px-4 py-2 bg-gradient-to-r from-cyan-500 to-blue-500 text-white rounded-lg text-sm font-medium hover:shadow-lg hover:shadow-cyan-500/25 transition-all duration-200 hover:-translate-y-0.5"
              >
                + 新建工单
              </button>
              <div className="flex items-center space-x-2">
                <div className="w-8 h-8 rounded-full bg-gradient-to-br from-emerald-400 to-cyan-400 flex items-center justify-center text-white text-sm font-medium">
                  李
                </div>
                <span className="text-slate-300 text-sm">李工程师</span>
              </div>
            </div>
          </div>
        </div>
      </nav>

      {/* 主内容区 */}
      <main className="max-w-7xl mx-auto px-4 py-6">
        {activeTab === 'dashboard' && <Dashboard tickets={demoTickets} jcs={demoJudgementCards} onSelectTicket={(t) => { setSelectedTicket(t); setActiveTab('tickets'); }} />}
        {activeTab === 'tickets' && !selectedTicket && <TicketList tickets={demoTickets} onSelect={setSelectedTicket} />}
        {activeTab === 'tickets' && selectedTicket && <TicketDetail ticket={selectedTicket} jcs={demoJudgementCards} solutions={demoSolutions} onBack={() => setSelectedTicket(null)} />}
        {activeTab === 'judgement' && !selectedJC && <JudgementCardList cards={demoJudgementCards} onSelect={setSelectedJC} />}
        {activeTab === 'judgement' && selectedJC && <JudgementCardDetail card={selectedJC} onBack={() => setSelectedJC(null)} />}
        {activeTab === 'solutions' && <SolutionList solutions={demoSolutions} />}
      </main>

      {/* 创建工单向导 */}
      {showCreateWizard && (
        <CreateWizard 
          step={wizardStep} 
          onStepChange={setWizardStep} 
          onClose={() => { setShowCreateWizard(false); setWizardStep(1); }} 
        />
      )}
    </div>
  );
}

// 工作台
function Dashboard({ tickets, jcs, onSelectTicket }) {
  const statusCounts = {
    Submitted: tickets.filter(t => t.status === 'Submitted').length,
    Triage: tickets.filter(t => t.status === 'Triage').length,
    SolutionIssued: tickets.filter(t => t.status === 'SolutionIssued').length,
    Verifying: tickets.filter(t => t.status === 'Verifying').length
  };

  return (
    <div className="space-y-6">
      {/* 统计卡片 */}
      <div className="grid grid-cols-4 gap-4">
        {[
          { label: '待分诊', count: statusCounts.Submitted, color: 'from-blue-500 to-blue-600', icon: '📥' },
          { label: '分诊中', count: statusCounts.Triage, color: 'from-amber-500 to-orange-500', icon: '🔍' },
          { label: '待验证', count: statusCounts.SolutionIssued, color: 'from-purple-500 to-pink-500', icon: '✅' },
          { label: '判断卡总数', count: jcs.length, color: 'from-emerald-500 to-cyan-500', icon: '🎯' }
        ].map((stat, i) => (
          <div key={i} className="relative overflow-hidden rounded-2xl bg-slate-800/50 backdrop-blur border border-slate-700/50 p-5">
            <div className={`absolute top-0 right-0 w-24 h-24 bg-gradient-to-br ${stat.color} opacity-10 rounded-full -translate-y-8 translate-x-8`} />
            <div className="relative">
              <span className="text-2xl">{stat.icon}</span>
              <p className="text-slate-400 text-sm mt-2">{stat.label}</p>
              <p className={`text-3xl font-bold mt-1 bg-gradient-to-r ${stat.color} bg-clip-text text-transparent`}>
                {stat.count}
              </p>
            </div>
          </div>
        ))}
      </div>

      {/* 看板视图 */}
      <div className="grid grid-cols-4 gap-4">
        {['Submitted', 'Triage', 'SolutionIssued', 'Verifying'].map(status => (
          <div key={status} className="bg-slate-800/30 rounded-2xl border border-slate-700/50 overflow-hidden">
            <div className={`px-4 py-3 border-b border-slate-700/50 ${statusColors[status].bg}`}>
              <h3 className={`font-medium ${statusColors[status].text}`}>
                {statusColors[status].label}
                <span className="ml-2 text-sm opacity-70">
                  ({tickets.filter(t => t.status === status).length})
                </span>
              </h3>
            </div>
            <div className="p-3 space-y-3 max-h-96 overflow-y-auto">
              {tickets.filter(t => t.status === status).map(ticket => (
                <div
                  key={ticket.id}
                  onClick={() => onSelectTicket(ticket)}
                  className="bg-slate-800/50 rounded-xl p-3 cursor-pointer hover:bg-slate-700/50 transition-all duration-200 hover:-translate-y-0.5 hover:shadow-lg border border-slate-700/50"
                >
                  <div className="flex items-start justify-between mb-2">
                    <span className="text-xs text-slate-500 font-mono">{ticket.id}</span>
                    <span className={`w-2 h-2 rounded-full ${priorityColors[ticket.priority]}`} />
                  </div>
                  <p className="text-white text-sm font-medium line-clamp-2 mb-2">{ticket.symptom}</p>
                  <div className="flex items-center justify-between">
                    <span className={`text-xs px-2 py-0.5 rounded ${domainInfo[ticket.domain].color} text-white`}>
                      {domainInfo[ticket.domain].icon} {ticket.domain}
                    </span>
                    <span className="text-xs text-slate-500">{ticket.customer.slice(0, 6)}...</span>
                  </div>
                </div>
              ))}
              {tickets.filter(t => t.status === status).length === 0 && (
                <div className="text-center py-8 text-slate-500 text-sm">暂无工单</div>
              )}
            </div>
          </div>
        ))}
      </div>

      {/* 高频判断卡 */}
      <div className="bg-slate-800/30 rounded-2xl border border-slate-700/50 p-5">
        <h3 className="text-white font-semibold mb-4 flex items-center">
          <span className="mr-2">🎯</span>
          高频使用判断卡 Top 3
        </h3>
        <div className="grid grid-cols-3 gap-4">
          {jcs.slice(0, 3).map((jc, i) => (
            <div key={jc.code} className="bg-slate-800/50 rounded-xl p-4 border border-slate-700/50">
              <div className="flex items-center justify-between mb-3">
                <span className="text-cyan-400 font-mono text-sm">{jc.code}</span>
                <span className="text-emerald-400 text-sm">{jc.successRate}% 成功率</span>
              </div>
              <h4 className="text-white font-medium mb-2">{jc.title}</h4>
              <div className="flex items-center justify-between text-sm">
                <span className="text-slate-400">使用 {jc.usageCount} 次</span>
                <div className="flex space-x-1">
                  {jc.domains.map(d => (
                    <span key={d} className={`w-6 h-6 rounded flex items-center justify-center text-xs ${domainInfo[d].color} text-white`}>
                      {d}
                    </span>
                  ))}
                </div>
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}

// 工单列表
function TicketList({ tickets, onSelect }) {
  const [filter, setFilter] = useState('all');
  
  const filtered = filter === 'all' ? tickets : tickets.filter(t => t.status === filter);

  return (
    <div className="space-y-4">
      {/* 筛选器 */}
      <div className="flex items-center space-x-2 bg-slate-800/30 rounded-xl p-2 border border-slate-700/50">
        {['all', 'Submitted', 'Triage', 'SolutionIssued', 'Verifying', 'Closed'].map(f => (
          <button
            key={f}
            onClick={() => setFilter(f)}
            className={`px-4 py-2 rounded-lg text-sm font-medium transition-all ${
              filter === f
                ? 'bg-cyan-500/20 text-cyan-400'
                : 'text-slate-400 hover:text-white hover:bg-slate-700/50'
            }`}
          >
            {f === 'all' ? '全部' : statusColors[f]?.label}
          </button>
        ))}
      </div>

      {/* 列表 */}
      <div className="space-y-3">
        {filtered.map(ticket => (
          <div
            key={ticket.id}
            onClick={() => onSelect(ticket)}
            className="bg-slate-800/50 rounded-2xl p-5 cursor-pointer hover:bg-slate-700/50 transition-all duration-200 border border-slate-700/50 hover:border-cyan-500/30 hover:shadow-lg hover:shadow-cyan-500/5"
          >
            <div className="flex items-start justify-between">
              <div className="flex-1">
                <div className="flex items-center space-x-3 mb-2">
                  <span className="text-cyan-400 font-mono text-sm">{ticket.id}</span>
                  <span className={`px-2 py-0.5 rounded text-xs ${statusColors[ticket.status].bg} ${statusColors[ticket.status].text}`}>
                    {statusColors[ticket.status].label}
                  </span>
                  <span className={`w-2 h-2 rounded-full ${priorityColors[ticket.priority]}`} title={ticket.priority} />
                </div>
                <h3 className="text-white font-medium mb-2">{ticket.symptom}</h3>
                <div className="flex items-center space-x-4 text-sm text-slate-400">
                  <span>{ticket.customer}</span>
                  <span>•</span>
                  <span>{ticket.device}</span>
                  <span>•</span>
                  <span>{ticket.step}</span>
                </div>
              </div>
              <div className="text-right">
                <span className={`inline-flex items-center px-3 py-1 rounded-lg text-sm ${domainInfo[ticket.domain].color} text-white`}>
                  {domainInfo[ticket.domain].icon} {domainInfo[ticket.domain].name}
                </span>
                <p className="text-slate-500 text-xs mt-2">{ticket.createdAt}</p>
              </div>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}

// 工单详情
function TicketDetail({ ticket, jcs, solutions, onBack }) {
  const relatedJC = jcs.find(j => j.code === ticket.jcCode);
  const relatedSol = solutions.find(s => s.code === ticket.solution);
  const recommendedJCs = jcs.filter(j => j.domains.includes(ticket.domain)).slice(0, 3);

  return (
    <div className="space-y-6">
      {/* 返回按钮 */}
      <button onClick={onBack} className="text-slate-400 hover:text-white flex items-center space-x-2 transition-colors">
        <span>←</span>
        <span>返回列表</span>
      </button>

      {/* 头部信息 */}
      <div className="bg-slate-800/50 rounded-2xl p-6 border border-slate-700/50">
        <div className="flex items-start justify-between mb-4">
          <div>
            <div className="flex items-center space-x-3 mb-2">
              <span className="text-cyan-400 font-mono">{ticket.id}</span>
              <span className={`px-3 py-1 rounded-lg text-sm ${statusColors[ticket.status].bg} ${statusColors[ticket.status].text}`}>
                {statusColors[ticket.status].label}
              </span>
              <span className={`px-2 py-1 rounded text-xs text-white ${priorityColors[ticket.priority]}`}>
                {ticket.priority}
              </span>
            </div>
            <h2 className="text-xl text-white font-semibold">{ticket.symptom}</h2>
          </div>
          <span className={`px-4 py-2 rounded-xl text-white ${domainInfo[ticket.domain].color}`}>
            {domainInfo[ticket.domain].icon} {domainInfo[ticket.domain].name}
          </span>
        </div>

        <div className="grid grid-cols-4 gap-4 mt-6">
          {[
            { label: '客户', value: ticket.customer },
            { label: '设备SN', value: ticket.device },
            { label: '步骤', value: ticket.step },
            { label: '复现率', value: `${ticket.reproRate}%` }
          ].map((item, i) => (
            <div key={i} className="bg-slate-900/50 rounded-xl p-3">
              <p className="text-slate-500 text-xs mb-1">{item.label}</p>
              <p className="text-white text-sm font-medium">{item.value}</p>
            </div>
          ))}
        </div>

        <div className="grid grid-cols-3 gap-4 mt-4">
          <div className="bg-slate-900/50 rounded-xl p-3">
            <p className="text-slate-500 text-xs mb-1">软件版本</p>
            <p className="text-cyan-400 font-mono text-sm">{ticket.swVersion}</p>
          </div>
          <div className="bg-slate-900/50 rounded-xl p-3">
            <p className="text-slate-500 text-xs mb-1">PLC版本</p>
            <p className="text-cyan-400 font-mono text-sm">{ticket.plcVersion}</p>
          </div>
          <div className="bg-slate-900/50 rounded-xl p-3">
            <p className="text-slate-500 text-xs mb-1">参数版本</p>
            <p className="text-cyan-400 font-mono text-sm">{ticket.paramVersion}</p>
          </div>
        </div>
      </div>

      <div className="grid grid-cols-3 gap-6">
        {/* 事实表 */}
        <div className="col-span-2 bg-slate-800/50 rounded-2xl p-6 border border-slate-700/50">
          <h3 className="text-white font-semibold mb-4 flex items-center">
            <span className="mr-2">📋</span>
            现场事实表
          </h3>
          <div className="space-y-4">
            {Object.entries(ticket.facts).map(([category, items]) => (
              <div key={category} className="bg-slate-900/50 rounded-xl p-4">
                <h4 className="text-slate-300 text-sm font-medium mb-3 capitalize">
                  {category === 'mechanical' && '⚙️ 机械/动作'}
                  {category === 'electrical' && '⚡ 电气/IO'}
                  {category === 'plc' && '💻 PLC/程序'}
                  {category === 'test' && '📊 测试/判定'}
                  {category === 'environment' && '🌡️ 环境/复现'}
                </h4>
                <div className="grid grid-cols-2 gap-2">
                  {Object.entries(items).map(([key, value]) => (
                    <div key={key} className="flex items-center justify-between py-1 px-2 bg-slate-800/50 rounded">
                      <span className="text-slate-400 text-xs">{key.replace(/_/g, ' ')}</span>
                      <span className={`text-xs font-medium px-2 py-0.5 rounded ${
                        value === 'YES' ? 'bg-emerald-500/20 text-emerald-400' :
                        value === 'NO' ? 'bg-red-500/20 text-red-400' :
                        'bg-slate-600/50 text-slate-300'
                      }`}>
                        {value}
                      </span>
                    </div>
                  ))}
                </div>
              </div>
            ))}
          </div>
        </div>

        {/* 推荐判断卡 */}
        <div className="bg-slate-800/50 rounded-2xl p-6 border border-slate-700/50">
          <h3 className="text-white font-semibold mb-4 flex items-center">
            <span className="mr-2">🎯</span>
            推荐判断卡
          </h3>
          <div className="space-y-3">
            {recommendedJCs.map((jc, i) => (
              <div 
                key={jc.code} 
                className={`rounded-xl p-4 border transition-all ${
                  jc.code === ticket.jcCode 
                    ? 'bg-cyan-500/10 border-cyan-500/50' 
                    : 'bg-slate-900/50 border-slate-700/50 hover:border-slate-600'
                }`}
              >
                <div className="flex items-center justify-between mb-2">
                  <span className="text-cyan-400 font-mono text-sm">{jc.code}</span>
                  <span className="text-xs text-slate-400">匹配度 {95 - i * 10}%</span>
                </div>
                <p className="text-white text-sm font-medium mb-2">{jc.title}</p>
                {jc.code === ticket.jcCode && (
                  <span className="text-xs text-cyan-400">✓ 已关联</span>
                )}
              </div>
            ))}
          </div>

          {/* 已关联的判断卡详情 */}
          {relatedJC && (
            <div className="mt-6 pt-6 border-t border-slate-700/50">
              <h4 className="text-slate-300 text-sm font-medium mb-3">判定边界</h4>
              <div className="space-y-2">
                {relatedJC.decisionBoundary.slice(0, 2).map((db, i) => (
                  <div key={i} className="bg-slate-900/50 rounded-lg p-3 text-xs">
                    <p className="text-slate-400 mb-1">如果：{db.condition}</p>
                    <p className="text-emerald-400">→ {db.conclusion}</p>
                  </div>
                ))}
              </div>
            </div>
          )}
        </div>
      </div>

      {/* 解决方案 */}
      {relatedSol && (
        <div className="bg-gradient-to-r from-purple-500/10 to-pink-500/10 rounded-2xl p-6 border border-purple-500/20">
          <div className="flex items-center justify-between mb-4">
            <h3 className="text-white font-semibold flex items-center">
              <span className="mr-2">💡</span>
              解决方案 {relatedSol.code}
            </h3>
            <span className="px-3 py-1 bg-purple-500/20 text-purple-400 rounded-lg text-sm">
              {relatedSol.releaseType} {relatedSol.releaseVersion}
            </span>
          </div>
          <p className="text-slate-300 mb-4">{relatedSol.diagnosis}</p>
          <div className="space-y-2">
            {relatedSol.changes.map((change, i) => (
              <div key={i} className="bg-slate-900/50 rounded-xl p-4 flex items-start space-x-4">
                <span className={`px-2 py-1 rounded text-xs ${
                  change.type === 'PARAMETER' ? 'bg-cyan-500/20 text-cyan-400' : 'bg-amber-500/20 text-amber-400'
                }`}>
                  {change.type}
                </span>
                <div className="flex-1">
                  <p className="text-white text-sm">{change.location}</p>
                  <div className="flex items-center space-x-2 mt-1 text-xs">
                    <span className="text-red-400 line-through">{change.before}</span>
                    <span className="text-slate-500">→</span>
                    <span className="text-emerald-400">{change.after}</span>
                  </div>
                  <p className="text-slate-500 text-xs mt-1">{change.reason}</p>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* 时间线 */}
      <div className="bg-slate-800/50 rounded-2xl p-6 border border-slate-700/50">
        <h3 className="text-white font-semibold mb-4 flex items-center">
          <span className="mr-2">📅</span>
          处理时间线
        </h3>
        <div className="relative pl-6 border-l-2 border-slate-700 space-y-4">
          {[
            { time: ticket.createdAt, action: `${ticket.creator} 提交工单`, type: 'create' },
            { time: '10:15', action: '李工 开始分诊', type: 'triage' },
            { time: '10:20', action: `关联判断卡 ${ticket.jcCode}`, type: 'link' },
            ...(relatedSol ? [{ time: relatedSol.publishedAt?.split(' ')[1], action: `发布解决方案 ${relatedSol.code}`, type: 'solution' }] : [])
          ].map((event, i) => (
            <div key={i} className="relative">
              <div className={`absolute -left-8 w-4 h-4 rounded-full border-2 ${
                event.type === 'create' ? 'bg-blue-500 border-blue-400' :
                event.type === 'solution' ? 'bg-purple-500 border-purple-400' :
                'bg-slate-600 border-slate-500'
              }`} />
              <div className="bg-slate-900/50 rounded-lg p-3">
                <span className="text-slate-500 text-xs">{event.time}</span>
                <p className="text-slate-300 text-sm mt-1">{event.action}</p>
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}

// 判断卡列表
function JudgementCardList({ cards, onSelect }) {
  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h2 className="text-white text-xl font-semibold">判断卡库</h2>
        <button className="px-4 py-2 bg-cyan-500/20 text-cyan-400 rounded-lg text-sm hover:bg-cyan-500/30 transition-colors">
          + 新建判断卡
        </button>
      </div>

      <div className="grid grid-cols-2 gap-4">
        {cards.map(card => (
          <div
            key={card.code}
            onClick={() => onSelect(card)}
            className="bg-slate-800/50 rounded-2xl p-6 cursor-pointer hover:bg-slate-700/50 transition-all duration-200 border border-slate-700/50 hover:border-cyan-500/30 hover:shadow-lg"
          >
            <div className="flex items-start justify-between mb-4">
              <div>
                <span className="text-cyan-400 font-mono">{card.code}</span>
                <span className="text-slate-500 text-sm ml-2">v{card.version}</span>
              </div>
              <div className="flex space-x-1">
                {card.domains.map(d => (
                  <span key={d} className={`w-7 h-7 rounded-lg flex items-center justify-center text-sm ${domainInfo[d].color} text-white`}>
                    {d}
                  </span>
                ))}
              </div>
            </div>
            <h3 className="text-white font-semibold text-lg mb-3">{card.title}</h3>
            <p className="text-slate-400 text-sm mb-4 line-clamp-2">{card.target}</p>
            <div className="flex items-center justify-between pt-4 border-t border-slate-700/50">
              <div className="flex items-center space-x-4 text-sm">
                <span className="text-slate-400">
                  使用 <span className="text-white">{card.usageCount}</span> 次
                </span>
                <span className="text-emerald-400">
                  {card.successRate}% 成功
                </span>
              </div>
              <div className="flex flex-wrap gap-1">
                {card.keywords.slice(0, 3).map(kw => (
                  <span key={kw} className="px-2 py-0.5 bg-slate-700/50 text-slate-400 rounded text-xs">
                    {kw}
                  </span>
                ))}
              </div>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}

// 判断卡详情
function JudgementCardDetail({ card, onBack }) {
  return (
    <div className="space-y-6">
      <button onClick={onBack} className="text-slate-400 hover:text-white flex items-center space-x-2 transition-colors">
        <span>←</span>
        <span>返回列表</span>
      </button>

      <div className="bg-slate-800/50 rounded-2xl p-6 border border-slate-700/50">
        <div className="flex items-start justify-between mb-6">
          <div>
            <div className="flex items-center space-x-3 mb-2">
              <span className="text-cyan-400 font-mono text-lg">{card.code}</span>
              <span className="px-2 py-1 bg-slate-700 text-slate-300 rounded text-sm">v{card.version}</span>
            </div>
            <h2 className="text-2xl text-white font-semibold">{card.title}</h2>
          </div>
          <div className="flex space-x-2">
            {card.domains.map(d => (
              <span key={d} className={`px-3 py-2 rounded-xl text-white ${domainInfo[d].color}`}>
                {domainInfo[d].icon} {domainInfo[d].name}
              </span>
            ))}
          </div>
        </div>

        <div className="bg-slate-900/50 rounded-xl p-4 mb-6">
          <h4 className="text-slate-400 text-sm mb-2">判断目标</h4>
          <p className="text-white">{card.target}</p>
        </div>

        <div className="grid grid-cols-2 gap-4 mb-6">
          <div className="bg-slate-900/50 rounded-xl p-4">
            <h4 className="text-slate-400 text-sm mb-2">适用步骤</h4>
            <div className="flex flex-wrap gap-2">
              {card.steps.map(s => (
                <span key={s} className="px-2 py-1 bg-cyan-500/20 text-cyan-400 rounded text-sm font-mono">{s}</span>
              ))}
            </div>
          </div>
          <div className="bg-slate-900/50 rounded-xl p-4">
            <h4 className="text-slate-400 text-sm mb-2">关键词</h4>
            <div className="flex flex-wrap gap-2">
              {card.keywords.map(kw => (
                <span key={kw} className="px-2 py-1 bg-slate-700 text-slate-300 rounded text-sm">{kw}</span>
              ))}
            </div>
          </div>
        </div>
      </div>

      {/* 判定边界 */}
      <div className="bg-slate-800/50 rounded-2xl p-6 border border-slate-700/50">
        <h3 className="text-white font-semibold mb-4 flex items-center">
          <span className="mr-2">🎯</span>
          判定边界
        </h3>
        <div className="space-y-3">
          {card.decisionBoundary.map((db, i) => (
            <div key={i} className="bg-slate-900/50 rounded-xl p-4 border-l-4 border-cyan-500">
              <div className="flex items-center justify-between mb-2">
                <span className="text-slate-400 text-sm">条件</span>
                <span className={`px-2 py-0.5 rounded text-xs ${
                  db.confidence === 'high' ? 'bg-emerald-500/20 text-emerald-400' : 'bg-amber-500/20 text-amber-400'
                }`}>
                  {db.confidence === 'high' ? '高置信' : '中置信'}
                </span>
              </div>
              <p className="text-white mb-2">{db.condition}</p>
              <p className="text-emerald-400 font-medium">→ {db.conclusion}</p>
            </div>
          ))}
        </div>
      </div>

      {/* 典型失效模式 */}
      <div className="bg-slate-800/50 rounded-2xl p-6 border border-slate-700/50">
        <h3 className="text-white font-semibold mb-4 flex items-center">
          <span className="mr-2">⚠️</span>
          典型失效模式
        </h3>
        <div className="grid grid-cols-3 gap-4">
          {card.failureModes.map((fm, i) => (
            <div key={i} className="bg-slate-900/50 rounded-xl p-4">
              <h4 className="text-amber-400 font-medium mb-2">{fm.mode}</h4>
              <p className="text-slate-400 text-sm mb-3">{fm.symptom}</p>
              <div className="pt-3 border-t border-slate-700">
                <p className="text-xs text-slate-500 mb-1">处置建议</p>
                <p className="text-emerald-400 text-sm">{fm.hint}</p>
              </div>
            </div>
          ))}
        </div>
      </div>

      {/* 统计数据 */}
      <div className="grid grid-cols-3 gap-4">
        <div className="bg-gradient-to-br from-cyan-500/10 to-blue-500/10 rounded-2xl p-6 border border-cyan-500/20">
          <p className="text-slate-400 text-sm">使用次数</p>
          <p className="text-3xl font-bold text-cyan-400 mt-2">{card.usageCount}</p>
        </div>
        <div className="bg-gradient-to-br from-emerald-500/10 to-green-500/10 rounded-2xl p-6 border border-emerald-500/20">
          <p className="text-slate-400 text-sm">成功率</p>
          <p className="text-3xl font-bold text-emerald-400 mt-2">{card.successRate}%</p>
        </div>
        <div className="bg-gradient-to-br from-purple-500/10 to-pink-500/10 rounded-2xl p-6 border border-purple-500/20">
          <p className="text-slate-400 text-sm">关联SOL数</p>
          <p className="text-3xl font-bold text-purple-400 mt-2">12</p>
        </div>
      </div>
    </div>
  );
}

// 解决方案列表
function SolutionList({ solutions }) {
  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h2 className="text-white text-xl font-semibold">解决方案库</h2>
        <div className="text-slate-400 text-sm">共 {solutions.length} 个方案</div>
      </div>

      {solutions.map(sol => (
        <div key={sol.code} className="bg-slate-800/50 rounded-2xl p-6 border border-slate-700/50">
          <div className="flex items-start justify-between mb-4">
            <div>
              <div className="flex items-center space-x-3 mb-2">
                <span className="text-purple-400 font-mono">{sol.code}</span>
                <span className="px-2 py-1 bg-emerald-500/20 text-emerald-400 rounded text-sm">
                  已发布
                </span>
              </div>
              <h3 className="text-white font-semibold text-lg">{sol.title}</h3>
            </div>
            <div className="text-right">
              <span className="px-3 py-1 bg-cyan-500/20 text-cyan-400 rounded-lg text-sm">
                {sol.releaseType} {sol.releaseVersion}
              </span>
              <p className="text-slate-500 text-xs mt-2">{sol.publishedAt}</p>
            </div>
          </div>

          <p className="text-slate-400 mb-4">{sol.diagnosis}</p>

          <div className="bg-slate-900/50 rounded-xl p-4 mb-4">
            <h4 className="text-slate-400 text-sm mb-3">修改内容</h4>
            <div className="space-y-2">
              {sol.changes.map((change, i) => (
                <div key={i} className="flex items-center space-x-4 text-sm">
                  <span className={`px-2 py-0.5 rounded text-xs ${
                    change.type === 'PARAMETER' ? 'bg-cyan-500/20 text-cyan-400' : 'bg-amber-500/20 text-amber-400'
                  }`}>
                    {change.type}
                  </span>
                  <span className="text-slate-300">{change.location}</span>
                  <span className="text-red-400 line-through">{change.before}</span>
                  <span className="text-slate-500">→</span>
                  <span className="text-emerald-400">{change.after}</span>
                </div>
              ))}
            </div>
          </div>

          <div className="flex items-center justify-between pt-4 border-t border-slate-700/50">
            <div className="flex items-center space-x-4 text-sm text-slate-400">
              <span>关联工单: <span className="text-cyan-400">{sol.ticketId}</span></span>
              <span>关联判断卡: <span className="text-purple-400">{sol.jcCode}</span></span>
            </div>
            <button className="px-4 py-2 bg-slate-700 text-slate-300 rounded-lg text-sm hover:bg-slate-600 transition-colors">
              查看详情
            </button>
          </div>
        </div>
      ))}
    </div>
  );
}

// 创建工单向导
function CreateWizard({ step, onStepChange, onClose }) {
  const [selectedDomain, setSelectedDomain] = useState('');
  const [facts, setFacts] = useState({});

  return (
    <div className="fixed inset-0 bg-black/70 backdrop-blur-sm flex items-center justify-center z-50">
      <div className="bg-slate-800 rounded-3xl w-full max-w-2xl max-h-[90vh] overflow-hidden shadow-2xl border border-slate-700">
        {/* 头部 */}
        <div className="bg-slate-900/50 px-6 py-4 border-b border-slate-700 flex items-center justify-between">
          <div>
            <h2 className="text-white font-semibold">新建工单</h2>
            <p className="text-slate-400 text-sm">Step {step} / 4</p>
          </div>
          <button onClick={onClose} className="text-slate-400 hover:text-white p-2 rounded-lg hover:bg-slate-700 transition-colors">
            ✕
          </button>
        </div>

        {/* 进度条 */}
        <div className="px-6 py-3 bg-slate-900/30">
          <div className="flex items-center space-x-2">
            {[1, 2, 3, 4].map(s => (
              <React.Fragment key={s}>
                <div className={`w-8 h-8 rounded-full flex items-center justify-center text-sm font-medium transition-all ${
                  s <= step ? 'bg-cyan-500 text-white' : 'bg-slate-700 text-slate-500'
                }`}>
                  {s < step ? '✓' : s}
                </div>
                {s < 4 && <div className={`flex-1 h-1 rounded ${s < step ? 'bg-cyan-500' : 'bg-slate-700'}`} />}
              </React.Fragment>
            ))}
          </div>
          <div className="flex justify-between mt-2 text-xs text-slate-500">
            <span>设备信息</span>
            <span>问题描述</span>
            <span>事实确认</span>
            <span>上传证据</span>
          </div>
        </div>

        {/* 内容区 */}
        <div className="p-6 max-h-[60vh] overflow-y-auto">
          {step === 1 && (
            <div className="space-y-4">
              <div className="bg-gradient-to-r from-cyan-500/10 to-blue-500/10 rounded-xl p-4 border border-cyan-500/20 flex items-center space-x-4">
                <div className="w-12 h-12 bg-cyan-500/20 rounded-xl flex items-center justify-center">
                  <span className="text-2xl">📷</span>
                </div>
                <div>
                  <p className="text-cyan-400 font-medium">扫描设备二维码</p>
                  <p className="text-slate-400 text-sm">自动填充设备和客户信息</p>
                </div>
              </div>

              <div className="text-center text-slate-500 text-sm py-2">或手动选择</div>

              <div className="space-y-4">
                <div>
                  <label className="block text-slate-400 text-sm mb-2">客户</label>
                  <select className="w-full bg-slate-900 border border-slate-700 rounded-xl px-4 py-3 text-white focus:border-cyan-500 focus:outline-none">
                    <option>某新能源汽车公司</option>
                    <option>某白色家电集团</option>
                    <option>某动力电池制造商</option>
                  </select>
                </div>
                <div>
                  <label className="block text-slate-400 text-sm mb-2">设备SN</label>
                  <input 
                    type="text" 
                    placeholder="如: BMS-EOL-2024-0156"
                    className="w-full bg-slate-900 border border-slate-700 rounded-xl px-4 py-3 text-white placeholder-slate-600 focus:border-cyan-500 focus:outline-none"
                  />
                </div>
                <div className="grid grid-cols-3 gap-4">
                  <div>
                    <label className="block text-slate-400 text-sm mb-2">软件版本</label>
                    <input type="text" defaultValue="v2.1.3" className="w-full bg-slate-900 border border-slate-700 rounded-xl px-4 py-3 text-cyan-400 font-mono focus:border-cyan-500 focus:outline-none" />
                  </div>
                  <div>
                    <label className="block text-slate-400 text-sm mb-2">PLC版本</label>
                    <input type="text" defaultValue="v1.2.6" className="w-full bg-slate-900 border border-slate-700 rounded-xl px-4 py-3 text-cyan-400 font-mono focus:border-cyan-500 focus:outline-none" />
                  </div>
                  <div>
                    <label className="block text-slate-400 text-sm mb-2">参数版本</label>
                    <input type="text" defaultValue="v3.4" className="w-full bg-slate-900 border border-slate-700 rounded-xl px-4 py-3 text-cyan-400 font-mono focus:border-cyan-500 focus:outline-none" />
                  </div>
                </div>
              </div>
            </div>
          )}

          {step === 2 && (
            <div className="space-y-6">
              <div>
                <label className="block text-slate-400 text-sm mb-3">问题域（必选一个）</label>
                <div className="grid grid-cols-5 gap-2">
                  {Object.entries(domainInfo).map(([key, info]) => (
                    <button
                      key={key}
                      onClick={() => setSelectedDomain(key)}
                      className={`p-4 rounded-xl border transition-all ${
                        selectedDomain === key
                          ? `${info.color} border-transparent text-white`
                          : 'bg-slate-900 border-slate-700 text-slate-400 hover:border-slate-600'
                      }`}
                    >
                      <span className="text-2xl block mb-2">{info.icon}</span>
                      <span className="text-xs">{key}</span>
                    </button>
                  ))}
                </div>
              </div>

              <div>
                <label className="block text-slate-400 text-sm mb-2">步骤号</label>
                <input 
                  type="text" 
                  placeholder="如: Step_120_Clamp_Check"
                  className="w-full bg-slate-900 border border-slate-700 rounded-xl px-4 py-3 text-white placeholder-slate-600 focus:border-cyan-500 focus:outline-none font-mono"
                />
              </div>

              <div>
                <label className="block text-slate-400 text-sm mb-2">现象一句话描述</label>
                <textarea 
                  rows={3}
                  placeholder="简要描述现象，不要包含判断"
                  className="w-full bg-slate-900 border border-slate-700 rounded-xl px-4 py-3 text-white placeholder-slate-600 focus:border-cyan-500 focus:outline-none resize-none"
                />
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-slate-400 text-sm mb-2">复现率</label>
                  <div className="flex items-center space-x-2">
                    <input type="number" defaultValue="100" min="0" max="100" className="w-24 bg-slate-900 border border-slate-700 rounded-xl px-4 py-3 text-white focus:border-cyan-500 focus:outline-none" />
                    <span className="text-slate-400">%</span>
                  </div>
                </div>
                <div>
                  <label className="block text-slate-400 text-sm mb-2">重启后恢复？</label>
                  <div className="flex space-x-4 mt-3">
                    <label className="flex items-center space-x-2 cursor-pointer">
                      <input type="radio" name="reboot" className="w-4 h-4 text-cyan-500" />
                      <span className="text-slate-300">是</span>
                    </label>
                    <label className="flex items-center space-x-2 cursor-pointer">
                      <input type="radio" name="reboot" className="w-4 h-4 text-cyan-500" defaultChecked />
                      <span className="text-slate-300">否</span>
                    </label>
                  </div>
                </div>
              </div>
            </div>
          )}

          {step === 3 && (
            <div className="space-y-4">
              <p className="text-slate-400 text-sm mb-4">请根据现场实际情况勾选，不确定的选 NA</p>
              
              {[
                { title: '⚙️ 机械/动作', items: ['动作完成', '到位可靠', '卡滞/异音', '人工辅助恢复'] },
                { title: '⚡ 电气/IO', items: ['传感器物理OK', 'PLC IO有变化', '同类点位OK'] },
                { title: '💻 PLC/程序', items: ['固定卡在此步', '手动可通过'] }
              ].map((section, i) => (
                <div key={i} className="bg-slate-900/50 rounded-xl p-4">
                  <h4 className="text-slate-300 text-sm font-medium mb-3">{section.title}</h4>
                  <div className="space-y-2">
                    {section.items.map((item, j) => (
                      <div key={j} className="flex items-center justify-between py-2 px-3 bg-slate-800/50 rounded-lg">
                        <span className="text-slate-400 text-sm">{item}</span>
                        <div className="flex space-x-2">
                          {['YES', 'NO', 'NA'].map(val => (
                            <button
                              key={val}
                              className={`px-3 py-1 rounded text-xs font-medium transition-all ${
                                val === 'YES' ? 'bg-emerald-500/20 text-emerald-400 hover:bg-emerald-500/30' :
                                val === 'NO' ? 'bg-red-500/20 text-red-400 hover:bg-red-500/30' :
                                'bg-slate-700 text-slate-400 hover:bg-slate-600'
                              }`}
                            >
                              {val}
                            </button>
                          ))}
                        </div>
                      </div>
                    ))}
                  </div>
                </div>
              ))}
            </div>
          )}

          {step === 4 && (
            <div className="space-y-6">
              <div className="grid grid-cols-2 gap-4">
                {[
                  { icon: '📷', label: '拍照', desc: '拍摄现场照片' },
                  { icon: '🎬', label: '视频', desc: '录制现象视频' },
                  { icon: '📄', label: '日志', desc: '上传PLC日志' },
                  { icon: '📁', label: '文件', desc: '其他文件' }
                ].map((item, i) => (
                  <button 
                    key={i}
                    className="bg-slate-900/50 rounded-xl p-6 border-2 border-dashed border-slate-700 hover:border-cyan-500/50 transition-all text-center group"
                  >
                    <span className="text-4xl block mb-2 group-hover:scale-110 transition-transform">{item.icon}</span>
                    <p className="text-white font-medium">{item.label}</p>
                    <p className="text-slate-500 text-xs mt-1">{item.desc}</p>
                  </button>
                ))}
              </div>

              <div className="bg-slate-900/50 rounded-xl p-4">
                <h4 className="text-slate-400 text-sm mb-3">已上传</h4>
                <div className="space-y-2">
                  <div className="flex items-center justify-between py-2 px-3 bg-slate-800/50 rounded-lg">
                    <div className="flex items-center space-x-3">
                      <span>📷</span>
                      <span className="text-slate-300 text-sm">io_screen.jpg</span>
                      <span className="text-slate-500 text-xs">(1.2MB)</span>
                    </div>
                    <button className="text-red-400 text-sm hover:text-red-300">删除</button>
                  </div>
                </div>
              </div>

              <label className="flex items-start space-x-3 cursor-pointer bg-amber-500/10 rounded-xl p-4 border border-amber-500/20">
                <input type="checkbox" className="w-5 h-5 mt-0.5 rounded border-slate-600 text-cyan-500" />
                <div>
                  <p className="text-amber-400 font-medium">我确认以上为现场事实</p>
                  <p className="text-slate-400 text-sm">不包含个人判断和猜测</p>
                </div>
              </label>
            </div>
          )}
        </div>

        {/* 底部按钮 */}
        <div className="px-6 py-4 bg-slate-900/50 border-t border-slate-700 flex items-center justify-between">
          <button
            onClick={() => step > 1 && onStepChange(step - 1)}
            className={`px-6 py-2 rounded-xl text-sm font-medium transition-all ${
              step > 1 ? 'bg-slate-700 text-white hover:bg-slate-600' : 'bg-slate-800 text-slate-600 cursor-not-allowed'
            }`}
            disabled={step === 1}
          >
            上一步
          </button>
          <button
            onClick={() => step < 4 ? onStepChange(step + 1) : onClose()}
            className="px-6 py-2 bg-gradient-to-r from-cyan-500 to-blue-500 text-white rounded-xl text-sm font-medium hover:shadow-lg hover:shadow-cyan-500/25 transition-all"
          >
            {step < 4 ? '下一步' : '提交工单'}
          </button>
        </div>
      </div>
    </div>
  );
}
