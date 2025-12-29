-- 生成AI分析结果数据
-- 为最近30天生成每日总结、每周总结、团队分析等

DO $$
DECLARE
    -- 用户ID
    user_ids UUID[];
    user_id UUID;
    
    -- 分析日期
    analysis_date DATE;
    days_ago INT;
    
    -- 分析类型
    analysis_types TEXT[] := ARRAY['daily_summary', 'weekly_summary', 'team_analysis', 'scheduling_suggestion'];
    analysis_type TEXT;
    
    -- 循环变量
    i INT;
    j INT;
    
BEGIN
    RAISE NOTICE '开始生成AI分析结果数据...';
    
    -- 获取用户ID
    SELECT ARRAY_AGG(id) INTO user_ids FROM users LIMIT 10;
    IF array_length(user_ids, 1) IS NULL THEN
        RAISE NOTICE '警告: 未找到用户';
        RETURN;
    END IF;
    
    -- 生成最近30天的每日总结
    FOR days_ago IN 0..29 LOOP
        analysis_date := CURRENT_DATE - (days_ago || ' days')::INTERVAL;
        
        -- 为每个工程师生成每日总结
        FOR i IN 1..LEAST(array_length(user_ids, 1), 5) LOOP
            user_id := user_ids[i];
            
            INSERT INTO ai_analysis_results (
                analysis_id, analysis_type, analysis_date, engineer_id,
                summary, key_insights, suggestions, performance_analysis,
                ai_model, confidence_score, created_at, created_by
            )
            VALUES (
                gen_random_uuid(),
                'daily_summary',
                analysis_date,
                user_id,
                '今日工作总结：处理了' || (3 + (random() * 5)::INT)::TEXT || '个工单，主要涉及' ||
                CASE (i % 5)
                    WHEN 0 THEN '机械定位精度问题'
                    WHEN 1 THEN '电气信号异常问题'
                    WHEN 2 THEN 'PLC程序执行问题'
                    WHEN 3 THEN '测试判定逻辑问题'
                    ELSE '系统偶发故障问题'
                END || '。整体响应时间较昨日提升' || (5 + (random() * 15)::INT)::TEXT || '%，客户满意度良好。',
                jsonb_build_object(
                    'tickets_handled', (3 + (random() * 5)::INT),
                    'average_response_time', (30 + (random() * 60))::INT,
                    'resolution_rate', (85 + (random() * 10))::NUMERIC(5,2),
                    'customer_satisfaction', (4.0 + random() * 1.0)::NUMERIC(3,2),
                    'key_achievements', jsonb_build_array(
                        '成功解决' || (2 + (random() * 3)::INT)::TEXT || '个复杂问题',
                        '响应时间缩短' || (10 + (random() * 20)::INT)::TEXT || '%',
                        '客户反馈良好'
                    )
                ),
                jsonb_build_object(
                    'improvements', jsonb_build_array(
                        jsonb_build_object(
                            'area', '响应速度',
                            'suggestion', '建议进一步优化工单分诊流程',
                            'priority', 'medium'
                        ),
                        jsonb_build_object(
                            'area', '问题解决效率',
                            'suggestion', '加强与其他部门的协作沟通',
                            'priority', 'high'
                        )
                    )
                ),
                jsonb_build_object(
                    'performance_trend', 'improving',
                    'strengths', jsonb_build_array('技术能力强', '沟通能力好', '问题解决效率高'),
                    'areas_for_improvement', jsonb_build_array('可以进一步提升响应速度', '建议加强跨部门协作')
                ),
                'gemini-pro',
                (0.85 + random() * 0.1)::NUMERIC(3,2),
                analysis_date + INTERVAL '18 hours',
                user_id
            );
        END LOOP;
    END LOOP;
    
    -- 生成最近4周的每周总结
    FOR i IN 1..4 LOOP
        analysis_date := DATE_TRUNC('week', CURRENT_DATE) - ((i - 1) || ' weeks')::INTERVAL;
        
        INSERT INTO ai_analysis_results (
            analysis_id, analysis_type, analysis_date, engineer_id,
            summary, key_insights, suggestions, performance_analysis,
            ai_model, confidence_score, created_at, created_by
        )
        VALUES (
            gen_random_uuid(),
            'weekly_summary',
            analysis_date,
            NULL, -- 团队总结
            '本周团队工作总结：共处理工单' || (50 + (random() * 30)::INT)::TEXT || '个，平均响应时间' ||
            (45 + (random() * 30)::INT)::TEXT || '分钟，问题解决率' || (88 + (random() * 7)::INT)::TEXT || '%。' ||
            '主要问题集中在' || 
            CASE (i % 3)
                WHEN 0 THEN '机械定位精度和电气信号异常'
                WHEN 1 THEN 'PLC程序执行和测试判定逻辑'
                ELSE '系统级问题和环境因素影响'
            END || '。团队协作良好，建议继续保持。',
            jsonb_build_object(
                'total_tickets', (50 + (random() * 30)::INT),
                'average_response_time_minutes', (45 + (random() * 30))::INT,
                'resolution_rate', (88 + (random() * 7))::NUMERIC(5,2),
                'top_issues', jsonb_build_array(
                    jsonb_build_object('domain', 'A', 'count', (10 + (random() * 10)::INT), 'percentage', (20 + (random() * 5))::NUMERIC(5,2)),
                    jsonb_build_object('domain', 'B', 'count', (12 + (random() * 10)::INT), 'percentage', (25 + (random() * 5))::NUMERIC(5,2)),
                    jsonb_build_object('domain', 'C', 'count', (10 + (random() * 8)::INT), 'percentage', (20 + (random() * 5))::NUMERIC(5,2))
                ),
                'team_performance', jsonb_build_object(
                    'collaboration_score', (4.2 + random() * 0.6)::NUMERIC(3,2),
                    'efficiency_score', (4.0 + random() * 0.8)::NUMERIC(3,2),
                    'quality_score', (4.3 + random() * 0.5)::NUMERIC(3,2)
                )
            ),
            jsonb_build_object(
                'team_suggestions', jsonb_build_array(
                    jsonb_build_object(
                        'suggestion', '建议加强团队内部技术交流',
                        'reason', '可以提升整体问题解决效率',
                        'priority', 'high'
                    ),
                    jsonb_build_object(
                        'suggestion', '优化工单分配机制',
                        'reason', '根据工程师专长分配工单可以提高效率',
                        'priority', 'medium'
                    )
                )
            ),
            jsonb_build_object(
                'team_trends', jsonb_build_object(
                    'efficiency_trend', 'improving',
                    'quality_trend', 'stable',
                    'collaboration_trend', 'improving'
                ),
                'top_performers', jsonb_build_array(
                    user_ids[1],
                    user_ids[2]
                )
            ),
            'gemini-pro',
            (0.88 + random() * 0.08)::NUMERIC(3,2),
            analysis_date + INTERVAL '6 days' + INTERVAL '20 hours',
            user_ids[1]
        );
    END LOOP;
    
    -- 生成团队分析（每月一次）
    FOR i IN 1..3 LOOP
        analysis_date := DATE_TRUNC('month', CURRENT_DATE) - ((i - 1) || ' months')::INTERVAL;
        
        INSERT INTO ai_analysis_results (
            analysis_id, analysis_type, analysis_date, engineer_id,
            summary, key_insights, suggestions, performance_analysis,
            ai_model, confidence_score, created_at, created_by
        )
        VALUES (
            gen_random_uuid(),
            'team_analysis',
            analysis_date,
            NULL,
            '团队月度分析报告：本月团队整体表现优秀，共处理工单' || (200 + (random() * 100)::INT)::TEXT || '个，' ||
            '平均响应时间' || (40 + (random() * 20)::INT)::TEXT || '分钟，问题解决率' || (90 + (random() * 5)::INT)::TEXT || '%。' ||
            '团队协作良好，技术能力持续提升。建议继续保持当前工作节奏，重点关注复杂问题的处理效率。',
            jsonb_build_object(
                'monthly_statistics', jsonb_build_object(
                    'total_tickets', (200 + (random() * 100)::INT),
                    'average_response_time', (40 + (random() * 20))::INT,
                    'resolution_rate', (90 + (random() * 5))::NUMERIC(5,2),
                    'customer_satisfaction', (4.5 + random() * 0.4)::NUMERIC(3,2)
                ),
                'team_composition', jsonb_build_object(
                    'total_engineers', array_length(user_ids, 1),
                    'senior_engineers', (array_length(user_ids, 1) / 3)::INT,
                    'field_engineers', (array_length(user_ids, 1) * 2 / 3)::INT
                ),
                'key_metrics', jsonb_build_object(
                    'efficiency_index', (85 + (random() * 10))::NUMERIC(5,2),
                    'quality_index', (88 + (random() * 8))::NUMERIC(5,2),
                    'collaboration_index', (90 + (random() * 7))::NUMERIC(5,2)
                )
            ),
            jsonb_build_object(
                'strategic_suggestions', jsonb_build_array(
                    jsonb_build_object(
                        'area', '技术能力提升',
                        'suggestion', '建议组织定期技术培训',
                        'expected_impact', '预计提升15%问题解决效率',
                        'priority', 'high'
                    ),
                    jsonb_build_object(
                        'area', '工作流程优化',
                        'suggestion', '优化工单分诊和分配流程',
                        'expected_impact', '预计缩短20%响应时间',
                        'priority', 'medium'
                    )
                )
            ),
            jsonb_build_object(
                'team_performance', jsonb_build_object(
                    'overall_rating', 'excellent',
                    'strengths', jsonb_build_array('团队协作能力强', '技术能力扎实', '响应速度快'),
                    'improvement_areas', jsonb_build_array('复杂问题处理效率', '跨部门沟通协调')
                ),
                'individual_performances', jsonb_build_array(
                    jsonb_build_object('engineer_id', user_ids[1], 'rating', 'excellent', 'key_achievements', jsonb_build_array('处理工单数量最多', '客户满意度最高')),
                    jsonb_build_object('engineer_id', user_ids[2], 'rating', 'good', 'key_achievements', jsonb_build_array('响应速度最快', '技术能力强'))
                )
            ),
            'gemini-pro',
            (0.90 + random() * 0.07)::NUMERIC(3,2),
            analysis_date + INTERVAL '25 days' + INTERVAL '18 hours',
            user_ids[1]
        );
    END LOOP;
    
    -- 生成人员安排建议（每周一次）
    FOR i IN 1..4 LOOP
        analysis_date := DATE_TRUNC('week', CURRENT_DATE) - ((i - 1) || ' weeks')::INTERVAL;
        
        INSERT INTO ai_analysis_results (
            analysis_id, analysis_type, analysis_date, engineer_id,
            summary, key_insights, suggestions, performance_analysis,
            ai_model, confidence_score, created_at, created_by
        )
        VALUES (
            gen_random_uuid(),
            'scheduling_suggestion',
            analysis_date,
            NULL,
            '本周人员安排建议：根据工单分布和工程师专长分析，建议优化人员配置。' ||
            '预计本周工单量' || (60 + (random() * 30)::INT)::TEXT || '个，建议安排' ||
            (array_length(user_ids, 1))::TEXT || '名工程师，其中' || (array_length(user_ids, 1) / 2)::INT::TEXT || '名专注于现场服务。',
            jsonb_build_object(
                'workload_forecast', jsonb_build_object(
                    'expected_tickets', (60 + (random() * 30)::INT),
                    'peak_days', jsonb_build_array('周一', '周三', '周五'),
                    'peak_hours', jsonb_build_array('09:00-11:00', '14:00-16:00')
                ),
                'current_capacity', jsonb_build_object(
                    'total_engineers', array_length(user_ids, 1),
                    'available_hours', (array_length(user_ids, 1) * 40),
                    'utilization_rate', (75 + (random() * 15))::NUMERIC(5,2)
                )
            ),
            jsonb_build_object(
                'scheduling_suggestions', jsonb_build_array(
                    jsonb_build_object(
                        'engineer_id', user_ids[1],
                        'suggestion', '建议增加现场服务时间，减少远程支持时间',
                        'reason', '该工程师现场服务效率更高，客户满意度更好',
                        'priority', 'high',
                        'expected_improvement', '预计提升20%效率'
                    ),
                    jsonb_build_object(
                        'engineer_id', user_ids[2],
                        'suggestion', '建议专注于复杂问题处理',
                        'reason', '该工程师技术能力强，适合处理复杂问题',
                        'priority', 'medium',
                        'expected_improvement', '预计提升15%问题解决率'
                    )
                ),
                'workload_balance', jsonb_build_array(
                    jsonb_build_object(
                        'from_engineer', user_ids[1],
                        'to_engineer', user_ids[2],
                        'suggestion', '建议将部分工单从工程师A转移到工程师B',
                        'reason', '工程师B在该领域更专业',
                        'expected_improvement', '预计提升20%效率'
                    )
                )
            ),
            jsonb_build_object(
                'optimization_impact', jsonb_build_object(
                    'expected_efficiency_gain', (15 + (random() * 10))::NUMERIC(5,2),
                    'expected_response_time_reduction', (20 + (random() * 15))::INT,
                    'expected_customer_satisfaction_improvement', (5 + (random() * 5))::NUMERIC(5,2)
                )
            ),
            'gemini-pro',
            (0.87 + random() * 0.1)::NUMERIC(3,2),
            analysis_date + INTERVAL '6 days' + INTERVAL '22 hours',
            user_ids[1]
        );
    END LOOP;
    
    RAISE NOTICE 'AI分析结果数据生成完成！';
    
END $$;

-- 验证数据
SELECT 
    '每日总结' as type, COUNT(*)::TEXT as count FROM ai_analysis_results WHERE analysis_type = 'daily_summary'
UNION ALL
SELECT '每周总结', COUNT(*)::TEXT FROM ai_analysis_results WHERE analysis_type = 'weekly_summary'
UNION ALL
SELECT '团队分析', COUNT(*)::TEXT FROM ai_analysis_results WHERE analysis_type = 'team_analysis'
UNION ALL
SELECT '人员安排建议', COUNT(*)::TEXT FROM ai_analysis_results WHERE analysis_type = 'scheduling_suggestion';

