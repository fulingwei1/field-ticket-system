import { Route, Routes } from 'react-router-dom';
import Login from './pages/Login';
import ChangePassword from './pages/ChangePassword';
import Dashboard from './pages/dashboard/Dashboard';
import TicketList from './pages/tickets/TicketList';
import TicketKanban from './pages/tickets/TicketKanban';
import CreateTicket from './pages/tickets/CreateTicket';
import TicketDetail from './pages/tickets/TicketDetail';
import TriagePanel from './pages/tickets/TriagePanel';
import SolutionEditor from './pages/solutions/SolutionEditor';
import SolutionList from './pages/solutions/SolutionList';
import VerificationPage from './pages/verification/VerificationPage';
import MyPerformance from './pages/performance/MyPerformance';
import TeamPerformance from './pages/performance/TeamPerformance';
import AiAnalysisResults from './pages/ai-analysis/AiAnalysisResults';
import AiAnalysisDetail from './pages/ai-analysis/AiAnalysisDetail';
import ConversationalDiagnosis from './pages/diagnosis/ConversationalDiagnosis';
import CalibrationDashboard from './pages/confidence/CalibrationDashboard';
import AIAttribution from './pages/attribution/AIAttribution';
import NotificationRuleConfig from './pages/notification-rules/NotificationRuleConfig';
import KnowledgeGraphVisualization from './pages/knowledge-graph/KnowledgeGraphVisualization';
import VersionHistory from './pages/knowledge/VersionHistory';
import JudgementCardList from './pages/judgement-cards/JudgementCardList';
import JudgementCardQuality from './pages/judgement-cards/JudgementCardQuality';
import JudgementCardVersion from './pages/judgement-cards/JudgementCardVersion';
import DeviceList from './pages/devices/DeviceList';
import DeviceConfigSnapshot from './pages/devices/DeviceConfigSnapshot';
import TicketTemplateList from './pages/tickets/TicketTemplateList';
import StatisticsDashboard from './pages/statistics/Dashboard';
import CorrectiveActionList from './pages/corrective-actions/ActionList';
import CorrectiveActionDetail from './pages/corrective-actions/ActionDetail';
import EngineerLoadStats from './pages/performance/EngineerLoadStats';
import NewcomerGrowthCurve from './pages/performance/NewcomerGrowthCurve';
import QRCodeGenerator from './pages/devices/QRCodeGenerator';
import ExcelImport from './pages/projects/ExcelImport';
import ProjectList from './pages/projects/ProjectList';
import ProblemDetail from './pages/projects/ProblemDetail';
import ProblemStatistics from './pages/projects/ProblemStatistics';
import UserProfile from './pages/user-profile/UserProfile';
import ThresholdManagement from './pages/thresholds/ThresholdManagement';
import CustomerList from './pages/customers/CustomerList';
import UserManagement from './pages/users/UserManagement';
import AppLayout from './components/AppLayout';

/**
 * 路由配置
 * 
 * 使用说明：
 * 1. 在 App.tsx 或主入口文件中导入此路由配置
 * 2. 使用 <BrowserRouter> 包裹应用
 * 3. 使用此 <Routes> 组件
 * 
 * 示例：
 * ```tsx
 * import { BrowserRouter } from 'react-router-dom';
 * import AppRoutes from './routes';
 * 
 * function App() {
 *   return (
 *     <BrowserRouter>
 *       <AppRoutes />
 *     </BrowserRouter>
 *   );
 * }
 * ```
 */
import TestPage from './pages/TestPage';

export default function AppRoutes() {
  console.log('🛣️ AppRoutes component rendering...');
  
  return (
    <Routes>
      {/* 测试页面 */}
      <Route path="/test" element={<TestPage />} />
      {/* 登录页（不使用布局） */}
      <Route path="/login" element={<Login />} />
      {/* 修改密码页（不使用布局） */}
      <Route path="/change-password" element={<ChangePassword />} />

      {/* 需要布局的页面 */}
      <Route
        path="/*"
        element={
          <AppLayout>
            <Routes>
              {/* 首页 */}
              <Route path="/" element={<Dashboard />} />
              <Route path="/dashboard" element={<Dashboard />} />

              {/* 工单相关 */}
              <Route path="/tickets" element={<TicketList />} />
              <Route path="/tickets/kanban" element={<TicketKanban />} />
              <Route path="/tickets/new" element={<CreateTicket />} />
              <Route path="/tickets/create" element={<CreateTicket />} />
              <Route path="/tickets/templates" element={<TicketTemplateList />} />
              <Route path="/tickets/:ticketId" element={<TicketDetail />} />
              <Route path="/tickets/:ticketId/triage" element={<TriagePanel />} />
              
              {/* 解决方案相关 */}
              <Route path="/solutions/tickets/:ticketId" element={<SolutionList />} />
              <Route path="/tickets/:ticketId/solutions/new" element={<SolutionEditor />} />
              <Route path="/tickets/:ticketId/solutions/:solutionId" element={<SolutionEditor />} />
              
              {/* 验证相关 */}
              <Route path="/tickets/:ticketId/verification" element={<VerificationPage />} />
              
              {/* 绩效管理 */}
              <Route path="/performance/my" element={<MyPerformance />} />
              <Route path="/performance/team" element={<TeamPerformance />} />
              <Route path="/performance/load-stats" element={<EngineerLoadStats />} />
              <Route path="/performance/growth-curve" element={<NewcomerGrowthCurve />} />
              
              {/* AI分析 */}
              <Route path="/ai-analysis" element={<AiAnalysisResults />} />
              <Route path="/ai-analysis/:analysisId" element={<AiAnalysisDetail />} />
              
              {/* 多轮对话式诊断 */}
              <Route path="/tickets/:ticketId/diagnosis" element={<ConversationalDiagnosis />} />
              
              {/* 置信度校准 */}
              <Route path="/confidence/calibration" element={<CalibrationDashboard />} />
              
              {/* AI辅助归因 */}
              <Route path="/tickets/:ticketId/attribution" element={<AIAttribution />} />
              
              {/* 通知规则配置 */}
              <Route path="/notification-rules" element={<NotificationRuleConfig />} />
              
              {/* 判断卡管理 */}
              <Route path="/judgement-cards" element={<JudgementCardList />} />
              <Route path="/judgement-cards/quality" element={<JudgementCardQuality />} />
              <Route path="/judgement-cards/version" element={<JudgementCardVersion />} />

              {/* 设备管理 */}
              <Route path="/devices" element={<DeviceList />} />
              <Route path="/devices/config-snapshot" element={<DeviceConfigSnapshot />} />
              <Route path="/devices/qrcode-generator" element={<QRCodeGenerator />} />

              {/* 客户管理 */}
              <Route path="/customers" element={<CustomerList />} />

              {/* 用户管理 */}
              <Route path="/users" element={<UserManagement />} />
              
              {/* 知识图谱 */}
              <Route path="/knowledge-graph" element={<KnowledgeGraphVisualization />} />
              
              {/* 知识版本管理 */}
              <Route path="/knowledge/:knowledgeId/versions/:knowledgeType" element={<VersionHistory />} />
              <Route path="/knowledge/expired" element={<VersionHistory />} />
              
              {/* 统计分析 */}
              <Route path="/statistics" element={<StatisticsDashboard />} />
              
              {/* 项目导入 */}
              <Route path="/projects/excel-import" element={<ExcelImport />} />
              
              {/* 项目管理 */}
              <Route path="/projects" element={<ProjectList />} />
              <Route path="/projects/problems/:problemId" element={<ProblemDetail />} />
              <Route path="/projects/statistics" element={<ProblemStatistics />} />
              
              {/* 整改任务 */}
              <Route path="/corrective-actions" element={<CorrectiveActionList />} />
              <Route path="/corrective-actions/:actionId" element={<CorrectiveActionDetail />} />
              
              {/* 用户画像 */}
              <Route path="/user-profile" element={<UserProfile />} />
              
              {/* 智能阈值管理 */}
              <Route path="/thresholds" element={<ThresholdManagement />} />

              {/* 404 页面 */}
              <Route path="*" element={<div>404 - 页面未找到</div>} />
            </Routes>
          </AppLayout>
        }
      />
    </Routes>
  );
}

