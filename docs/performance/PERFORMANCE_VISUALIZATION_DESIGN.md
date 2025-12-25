# 绩效管理可视化展示设计

> **日期**：2025-12-22  
> **目标**：设计全面的绩效管理可视化展示方案，包括雷达图、柱状图、折线图、热力图等

---

## 🎯 可视化需求分析

### 1. 个人绩效展示

**需求**：
- 个人各维度得分一目了然
- 与目标值、团队平均对比
- 历史趋势分析
- 薄弱环节识别

### 2. 团队管理展示

**需求**：
- 团队成员对比
- 团队整体绩效
- 人员分布情况
- 异常情况预警

### 3. 技能分析展示

**需求**：
- 技能等级分布
- 问题域处理能力
- 学习成长轨迹
- 知识掌握情况

---

## 📊 可视化组件设计

### 1. 个人绩效雷达图

**用途**：展示个人在四个核心维度的综合表现

**数据模型**：
```typescript
interface PerformanceRadar {
  engineerId: string;
  engineerName: string;
  proactivity: number;      // 主动性得分 (0-100)
  enthusiasm: number;        // 积极性得分 (0-100)
  timeliness: number;        // 及时性得分 (0-100)
  knowledgeSkills: number;   // 知识技能等级得分 (0-100)
  totalScore: number;        // 综合得分 (0-100)
  target: number;            // 目标值 (80)
  teamAverage: number;       // 团队平均 (0-100)
}
```

**实现方案**：
- 使用 `recharts` 或 `echarts` 实现雷达图
- 四个维度作为雷达图的四个轴
- 个人得分、目标值、团队平均用不同颜色显示

**展示效果**：
```
        主动性 (80)
           /\
          /  \
         /    \
   积极性(70)  及时性(90)
         \    /
          \  /
           \/
    知识技能(75)
```

---

### 2. 团队对比柱状图

**用途**：对比团队成员各维度得分，识别优秀和待改进人员

**数据模型**：
```typescript
interface TeamComparison {
  engineerId: string;
  engineerName: string;
  proactivity: number;
  enthusiasm: number;
  timeliness: number;
  knowledgeSkills: number;
  totalScore: number;
  rank: number;
  department: string;
}
```

**实现方案**：
- 使用分组柱状图
- 每个工程师一个分组
- 四个维度用不同颜色显示
- 支持排序（按总分、按维度）

**展示效果**：
```
得分
100 |     ████  ████  ████
 80 |  ████  ████  ████
 60 |  ████  ████  ████
 40 |  ████  ████  ████
 20 |  ████  ████  ████
  0 |_____________________
     张三  李四  王五
     
图例：主动性 █ 积极性 █ 及时性 █ 知识技能 █
```

---

### 3. 趋势分析折线图

**用途**：展示个人或团队绩效随时间的变化趋势

**数据模型**：
```typescript
interface TrendAnalysis {
  date: string;              // 日期 (YYYY-MM)
  proactivity: number;
  enthusiasm: number;
  timeliness: number;
  knowledgeSkills: number;
  totalScore: number;
  target: number;
  teamAverage: number;
}
```

**实现方案**：
- 使用多线折线图
- X轴：时间（月）
- Y轴：得分（0-100）
- 多条线：各维度得分、目标值、团队平均

**展示效果**：
```
得分
100 |                    ╱─── 目标值
 90 |              ╱───╱
 80 |        ╱───╱    ╱
 70 |  ╱───╱        ╱
 60 |╱            ╱
 50 |
  0 |________________________
    1月  2月  3月  4月  5月
```

---

### 4. 技能等级热力图

**用途**：展示工程师在各问题域的处理能力和技能等级

**数据模型**：
```typescript
interface SkillHeatmap {
  engineerId: string;
  engineerName: string;
  domains: {
    domain: string;          // 问题域名称
    skillLevel: number;      // 技能等级 (1-5)
    ticketCount: number;     // 处理工单数
    successRate: number;     // 成功率 (0-100)
    averageTime: number;     // 平均处理时间（小时）
  }[];
}
```

**实现方案**：
- 使用热力图
- X轴：问题域
- Y轴：工程师
- 颜色：技能等级（1-5级，颜色从浅到深）

**展示效果**：
```
问题域   机械  电气  软件  工艺  其他
工程师
张三      ████  ███   ████  ██    ███
李四      ███   ████  ███   ████  ██
王五      ████  ███   ██    ███   ████

图例：1级 █ 2级 █ 3级 █ 4级 █ 5级 █
```

---

### 5. 工作负荷分析图

**用途**：展示工程师的工作负荷分布和加班情况

**数据模型**：
```typescript
interface WorkloadAnalysis {
  engineerId: string;
  engineerName: string;
  totalTickets: number;          // 总工单数
  complexTickets: number;         // 复杂工单数
  overtimeTickets: number;        // 加班处理工单数
  averageHandlingTime: number;    // 平均处理时间（小时）
  workloadScore: number;         // 工作负荷得分 (0-100)
  workloadLevel: 'low' | 'normal' | 'high' | 'overload';
}
```

**实现方案**：
- 使用散点图或气泡图
- X轴：总工单数
- Y轴：平均处理时间
- 气泡大小：复杂工单数
- 颜色：工作负荷等级

**展示效果**：
```
平均处理时间
 24h |        ● (高负荷)
 18h |    ●
 12h |  ●   ●
  6h |●
  0h |________________
     0   10  20  30  40 总工单数
```

---

### 6. 主动性分析饼图

**用途**：展示工程师的主动行为分布

**数据模型**：
```typescript
interface ProactivityAnalysis {
  engineerId: string;
  engineerName: string;
  proactiveTicketCreation: number;  // 主动创建工单数
  proactiveFollowUp: number;        // 主动跟进工单数
  proactiveKnowledgeContribution: number;  // 主动知识贡献数
  proactiveImprovement: number;     // 主动改进建议数
  totalProactiveActions: number;    // 总主动行为数
}
```

**实现方案**：
- 使用饼图或环形图
- 四个扇区：四种主动行为
- 显示百分比和数量

**展示效果**：
```
     主动创建工单 (40%)
         ╱─────╲
        ╱       ╲
       ╱         ╲
      ╱  主动跟进  ╲ (30%)
     ╱   (20%)    ╲
    ╱               ╲
   ╱  主动改进建议    ╲
  ╱     (10%)        ╲
 ╱___________________╲
```

---

### 7. 及时性分析仪表盘

**用途**：展示工程师的及时性表现，类似仪表盘

**数据模型**：
```typescript
interface TimelinessDashboard {
  engineerId: string;
  engineerName: string;
  ticketResponseRate: number;       // 工单响应及时率 (0-100)
  followUpResponseRate: number;      // 追问回复及时性 (0-100)
  verificationTimeliness: number;   // 验证执行及时性 (0-100)
  customerCommunicationRate: number; // 客户沟通及时性 (0-100)
  overallTimeliness: number;        // 综合及时性 (0-100)
}
```

**实现方案**：
- 使用仪表盘组件
- 四个小仪表盘：各维度及时性
- 一个大仪表盘：综合及时性

**展示效果**：
```
工单响应及时率    追问回复及时性
    ┌─────┐          ┌─────┐
    │ 95% │          │ 90% │
    └─────┘          └─────┘

验证执行及时性    客户沟通及时性
    ┌─────┐          ┌─────┐
    │ 88% │          │ 92% │
    └─────┘          └─────┘

        综合及时性
        ┌─────┐
        │ 91% │
        └─────┘
```

---

### 8. 知识技能成长曲线

**用途**：展示工程师的知识技能成长轨迹

**数据模型**：
```typescript
interface KnowledgeGrowth {
  engineerId: string;
  engineerName: string;
  timeline: {
    date: string;                  // 日期
    diagnosisAccuracy: number;      // 技术诊断准确率
    judgementCardAccuracy: number;  // 判断卡使用准确率
    complexProblemSolving: number;  // 复杂问题解决能力
    knowledgeBreadth: number;       // 知识掌握广度
    knowledgeDepth: number;         // 知识掌握深度
    learningGrowth: number;         // 学习成长速度
  }[];
}
```

**实现方案**：
- 使用多线折线图
- X轴：时间（月）
- Y轴：技能得分（0-100）
- 多条线：各技能指标

**展示效果**：
```
技能得分
100 |                    ╱─── 诊断准确率
 90 |              ╱───╱    ╱─── 判断卡准确率
 80 |        ╱───╱        ╱
 70 |  ╱───╱        ╱───╱
 60 |╱            ╱
 50 |
  0 |________________________
    1月  2月  3月  4月  5月
```

---

## 🎨 UI/UX 设计

### 1. 个人绩效看板

**布局**：
```
┌─────────────────────────────────────────┐
│  个人绩效看板 - 张三                      │
├─────────────────────────────────────────┤
│  ┌─────────┐  ┌─────────┐  ┌─────────┐ │
│  │综合得分  │  │排名     │  │等级     │ │
│  │  85分   │  │ 第3名   │  │ 良好    │ │
│  └─────────┘  └─────────┘  └─────────┘ │
│                                         │
│  ┌───────────────────────────────────┐ │
│  │  绩效雷达图                         │ │
│  │        主动性 (80)                  │ │
│  │           /╲                        │ │
│  │          /  ╲                       │ │
│  │    积极性(70) 及时性(90)            │ │
│  │         ╲    /                       │ │
│  │          ╲  /                        │ │
│  │           ╲/                         │ │
│  │    知识技能(75)                      │ │
│  └───────────────────────────────────┘ │
│                                         │
│  ┌───────────────────────────────────┐ │
│  │  趋势分析                           │ │
│  │  [折线图]                           │ │
│  └───────────────────────────────────┘ │
└─────────────────────────────────────────┘
```

---

### 2. 团队绩效看板

**布局**：
```
┌─────────────────────────────────────────┐
│  团队绩效看板                           │
├─────────────────────────────────────────┤
│  ┌───────────────────────────────────┐ │
│  │  团队对比柱状图                     │ │
│  │  [分组柱状图]                       │ │
│  └───────────────────────────────────┘ │
│                                         │
│  ┌───────────────────────────────────┐ │
│  │  工作负荷分析                       │ │
│  │  [散点图/气泡图]                    │ │
│  └───────────────────────────────────┘ │
│                                         │
│  ┌───────────────────────────────────┐ │
│  │  技能等级热力图                     │ │
│  │  [热力图]                           │ │
│  └───────────────────────────────────┘ │
└─────────────────────────────────────────┘
```

---

### 3. 技能分析看板

**布局**：
```
┌─────────────────────────────────────────┐
│  技能分析看板 - 张三                    │
├─────────────────────────────────────────┤
│  ┌───────────────────────────────────┐ │
│  │  知识技能成长曲线                   │ │
│  │  [多线折线图]                       │ │
│  └───────────────────────────────────┘ │
│                                         │
│  ┌───────────────────────────────────┐ │
│  │  问题域处理能力                     │ │
│  │  机械: ████████ 80%                │ │
│  │  电气: ██████ 60%                   │ │
│  │  软件: ██████████ 100%              │ │
│  │  工艺: ████ 40%                     │ │
│  └───────────────────────────────────┘ │
└─────────────────────────────────────────┘
```

---

## 🔧 技术实现

### 1. 前端技术栈

**推荐方案**：
- **React** + **TypeScript**
- **Recharts** 或 **ECharts**（图表库）
- **Tailwind CSS**（样式）
- **shadcn/ui**（UI组件）

### 2. 数据获取

**API设计**：
```typescript
// 获取个人绩效数据
GET /api/performance/engineer/{engineerId}
Response: PerformanceRadar

// 获取团队绩效数据
GET /api/performance/team
Response: TeamComparison[]

// 获取趋势分析数据
GET /api/performance/trend/{engineerId}?from={date}&to={date}
Response: TrendAnalysis[]

// 获取技能热力图数据
GET /api/performance/skills/{engineerId}
Response: SkillHeatmap

// 获取工作负荷分析数据
GET /api/performance/workload
Response: WorkloadAnalysis[]
```

### 3. 组件实现示例

**雷达图组件**：
```typescript
import { Radar, RadarChart, PolarGrid, PolarAngleAxis, PolarRadiusAxis, ResponsiveContainer } from 'recharts';

interface PerformanceRadarProps {
  data: PerformanceRadar;
}

export function PerformanceRadarChart({ data }: PerformanceRadarProps) {
  const chartData = [
    { dimension: '主动性', score: data.proactivity, target: data.target, teamAverage: data.teamAverage },
    { dimension: '积极性', score: data.enthusiasm, target: data.target, teamAverage: data.teamAverage },
    { dimension: '及时性', score: data.timeliness, target: data.target, teamAverage: data.teamAverage },
    { dimension: '知识技能', score: data.knowledgeSkills, target: data.target, teamAverage: data.teamAverage },
  ];

  return (
    <ResponsiveContainer width="100%" height={400}>
      <RadarChart data={chartData}>
        <PolarGrid />
        <PolarAngleAxis dataKey="dimension" />
        <PolarRadiusAxis angle={90} domain={[0, 100]} />
        <Radar name="个人得分" dataKey="score" stroke="#8884d8" fill="#8884d8" fillOpacity={0.6} />
        <Radar name="目标值" dataKey="target" stroke="#82ca9d" fill="#82ca9d" fillOpacity={0.3} />
        <Radar name="团队平均" dataKey="teamAverage" stroke="#ffc658" fill="#ffc658" fillOpacity={0.3} />
      </RadarChart>
    </ResponsiveContainer>
  );
}
```

---

## 📱 移动端适配

### 1. 响应式设计

- 使用 Tailwind CSS 的响应式类
- 小屏幕：单列布局，图表简化
- 大屏幕：多列布局，完整展示

### 2. 移动端优化

- 图表支持触摸交互
- 支持横屏查看
- 数据表格支持横向滚动

---

## 🚀 实施计划

### Phase 1: 基础图表（Sprint 5）

1. 实现个人绩效雷达图
2. 实现团队对比柱状图
3. 实现趋势分析折线图

### Phase 2: 高级图表（Sprint 6）

1. 实现技能等级热力图
2. 实现工作负荷分析图
3. 实现及时性分析仪表盘

### Phase 3: 优化和增强（Sprint 7）

1. 移动端适配
2. 交互优化
3. 数据导出功能

---

## 📝 总结

### 核心优势

1. **全面性**：8种可视化图表，覆盖所有绩效维度
2. **直观性**：图表清晰，一目了然
3. **交互性**：支持筛选、排序、钻取
4. **响应式**：支持PC和移动端

### 关键图表

- **雷达图**：个人综合表现
- **柱状图**：团队对比
- **折线图**：趋势分析
- **热力图**：技能分布
- **散点图**：工作负荷
- **饼图**：主动行为分布
- **仪表盘**：及时性表现
- **成长曲线**：技能提升

---

**最后更新**：2025-12-22

