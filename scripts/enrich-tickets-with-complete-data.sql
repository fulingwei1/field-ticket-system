-- 为工单补充完整的数据：状态历史、客户沟通、解决方案、验证、附件等
-- 基于非标自动化行业特点生成100个完整案例

DO $$
DECLARE
    -- 工单ID列表
    ticket_ids UUID[];
    current_ticket_id UUID;
    ticket_rec RECORD;
    
    -- 用户ID
    user_ids UUID[];
    user_id UUID;
    
    -- 状态流转
    status_flow TEXT[] := ARRAY['Draft', 'Submitted', 'Triage', 'SolutionIssued', 'Verifying', 'Closed'];
    current_status TEXT;
    next_status TEXT;
    status_index INT;
    
    -- 解决方案相关
    solution_id UUID;
    solution_counter INT := 1;
    solution_year INT := EXTRACT(YEAR FROM CURRENT_DATE);
    
    -- 验证相关
    verification_id UUID;
    
    -- 附件相关
    attachment_id UUID;
    file_types TEXT[] := ARRAY['photo', 'video', 'log', 'file'];
    file_names TEXT[];
    
    -- 客户沟通相关
    communication_types TEXT[] := ARRAY['wechat', 'phone', 'email'];
    communication_templates TEXT[];
    
    -- 循环变量
    i INT;
    j INT;
    days_offset INT;
    created_time TIMESTAMPTZ;
    status_change_time TIMESTAMPTZ;
    
    -- 问题域特定的事实表模板
    facts_templates JSONB;
    
BEGIN
    RAISE NOTICE '开始为工单补充完整数据...';
    
    -- 获取前100个TKT工单
    SELECT ARRAY_AGG(t.ticket_id ORDER BY t.created_at DESC) INTO ticket_ids
    FROM (
        SELECT tkt.ticket_id, tkt.created_at FROM tickets tkt
        WHERE tkt.ticket_no LIKE 'TKT%'
        ORDER BY tkt.created_at DESC
        LIMIT 100
    ) t;
    
    RAISE NOTICE '找到 % 个工单需要补充数据', array_length(ticket_ids, 1);
    
    -- 获取用户ID
    SELECT ARRAY_AGG(id) INTO user_ids FROM users LIMIT 10;
    IF array_length(user_ids, 1) IS NULL THEN
        RAISE NOTICE '警告: 未找到用户，将使用随机UUID';
        user_ids := ARRAY[gen_random_uuid(), gen_random_uuid(), gen_random_uuid()];
    END IF;
    
    -- 为每个工单生成完整数据
    FOR i IN 1..array_length(ticket_ids, 1) LOOP
        current_ticket_id := ticket_ids[i];
        
        -- 获取工单信息
        SELECT * INTO ticket_rec FROM tickets t WHERE t.ticket_id = current_ticket_id;
        
        IF ticket_rec IS NULL THEN
            CONTINUE;
        END IF;
        
        -- 随机选择一个用户
        user_id := user_ids[1 + (random() * (array_length(user_ids, 1) - 1))::INT];
        
        -- 1. 生成状态流转历史
        current_status := 'Draft';
        status_change_time := ticket_rec.created_at;
        
        -- 根据当前状态生成历史流转
        CASE ticket_rec.status
            WHEN 'Submitted' THEN
                -- Draft -> Submitted
                INSERT INTO ticket_status_history (
                    ticket_id, from_status, to_status, change_reason,
                    changed_by, changed_by_name, changed_at, change_type, notes
                ) VALUES (
                    current_ticket_id, 'Draft', 'Submitted', '工单已提交',
                    ticket_rec.created_by_user_id, '系统', ticket_rec.created_at, 'manual',
                    '现场工程师提交工单'
                );
                current_status := 'Submitted';
                status_change_time := ticket_rec.created_at + INTERVAL '1 hour';
            
            WHEN 'Triage' THEN
                -- Draft -> Submitted -> Triage
                INSERT INTO ticket_status_history (
                    ticket_id, from_status, to_status, change_reason,
                    changed_by, changed_by_name, changed_at, change_type, notes
                ) VALUES
                (
                    current_ticket_id, 'Draft', 'Submitted', '工单已提交',
                    ticket_rec.created_by_user_id, '系统', ticket_rec.created_at, 'manual',
                    '现场工程师提交工单'
                ),
                (
                    current_ticket_id, 'Submitted', 'Triage', '进入分诊流程',
                    user_id, '高级工程师', ticket_rec.created_at + INTERVAL '2 hours', 'manual',
                    '高级工程师开始分诊'
                );
                current_status := 'Triage';
                status_change_time := ticket_rec.created_at + INTERVAL '2 hours';
            
            WHEN 'SolutionIssued' THEN
                -- Draft -> Submitted -> Triage -> SolutionIssued
                INSERT INTO ticket_status_history (
                    ticket_id, from_status, to_status, change_reason,
                    changed_by, changed_by_name, changed_at, change_type, notes
                ) VALUES
                (
                    current_ticket_id, 'Draft', 'Submitted', '工单已提交',
                    ticket_rec.created_by_user_id, '系统', ticket_rec.created_at, 'manual',
                    '现场工程师提交工单'
                ),
                (
                    current_ticket_id, 'Submitted', 'Triage', '进入分诊流程',
                    user_id, '高级工程师', ticket_rec.created_at + INTERVAL '2 hours', 'manual',
                    '高级工程师开始分诊'
                ),
                (
                    current_ticket_id, 'Triage', 'SolutionIssued', '解决方案已发布',
                    user_id, '高级工程师', ticket_rec.created_at + INTERVAL '4 hours', 'manual',
                    '已发布解决方案，等待现场验证'
                );
                current_status := 'SolutionIssued';
                status_change_time := ticket_rec.created_at + INTERVAL '4 hours';
            
            WHEN 'Verifying' THEN
                -- Draft -> Submitted -> Triage -> SolutionIssued -> Verifying
                INSERT INTO ticket_status_history (
                    ticket_id, from_status, to_status, change_reason,
                    changed_by, changed_by_name, changed_at, change_type, notes
                ) VALUES
                (
                    current_ticket_id, 'Draft', 'Submitted', '工单已提交',
                    ticket_rec.created_by_user_id, '系统', ticket_rec.created_at, 'manual',
                    '现场工程师提交工单'
                ),
                (
                    current_ticket_id, 'Submitted', 'Triage', '进入分诊流程',
                    user_id, '高级工程师', ticket_rec.created_at + INTERVAL '2 hours', 'manual',
                    '高级工程师开始分诊'
                ),
                (
                    current_ticket_id, 'Triage', 'SolutionIssued', '解决方案已发布',
                    user_id, '高级工程师', ticket_rec.created_at + INTERVAL '4 hours', 'manual',
                    '已发布解决方案'
                ),
                (
                    current_ticket_id, 'SolutionIssued', 'Verifying', '开始验证解决方案',
                    ticket_rec.created_by_user_id, '现场工程师', ticket_rec.created_at + INTERVAL '1 day', 'manual',
                    '现场工程师开始验证解决方案'
                );
                current_status := 'Verifying';
                status_change_time := ticket_rec.created_at + INTERVAL '1 day';
            
            WHEN 'Closed' THEN
                -- Draft -> Submitted -> Triage -> SolutionIssued -> Verifying -> Closed
                INSERT INTO ticket_status_history (
                    ticket_id, from_status, to_status, change_reason,
                    changed_by, changed_by_name, changed_at, change_type, notes
                ) VALUES
                (
                    current_ticket_id, 'Draft', 'Submitted', '工单已提交',
                    ticket_rec.created_by_user_id, '系统', ticket_rec.created_at, 'manual',
                    '现场工程师提交工单'
                ),
                (
                    current_ticket_id, 'Submitted', 'Triage', '进入分诊流程',
                    user_id, '高级工程师', ticket_rec.created_at + INTERVAL '2 hours', 'manual',
                    '高级工程师开始分诊'
                ),
                (
                    current_ticket_id, 'Triage', 'SolutionIssued', '解决方案已发布',
                    user_id, '高级工程师', ticket_rec.created_at + INTERVAL '4 hours', 'manual',
                    '已发布解决方案'
                ),
                (
                    current_ticket_id, 'SolutionIssued', 'Verifying', '开始验证解决方案',
                    ticket_rec.created_by_user_id, '现场工程师', ticket_rec.created_at + INTERVAL '1 day', 'manual',
                    '现场工程师开始验证解决方案'
                ),
                (
                    current_ticket_id, 'Verifying', 'Closed', '验证通过，工单已关闭',
                    ticket_rec.created_by_user_id, '现场工程师', ticket_rec.closed_at, 'manual',
                    '解决方案验证通过，问题已解决'
                );
                current_status := 'Closed';
                status_change_time := ticket_rec.closed_at;
        END CASE;
        
        -- 2. 根据问题域生成丰富的事实表数据
        CASE ticket_rec.domain
            WHEN 'A' THEN -- 机械/动作
                UPDATE tickets SET facts_json = jsonb_build_object(
                    'mechanical', jsonb_build_object(
                        'action_completed', CASE WHEN random() > 0.3 THEN 'YES' ELSE 'NO' END,
                        'position_accuracy', CASE WHEN random() > 0.4 THEN 'YES' ELSE 'NO' END,
                        'mechanical_wear', CASE WHEN random() > 0.5 THEN 'YES' ELSE 'NO' END,
                        'lubrication_status', CASE WHEN random() > 0.2 THEN 'YES' ELSE 'NO' END,
                        'position_deviation_mm', (0.1 + random() * 0.5)::NUMERIC(5,2),
                        'required_accuracy_mm', 0.2,
                        'test_failure_rate', (5 + random() * 20)::INT
                    ),
                    'electrical', jsonb_build_object(
                        'sensor_signal', CASE WHEN random() > 0.3 THEN 'YES' ELSE 'NO' END,
                        'motor_drive', CASE WHEN random() > 0.2 THEN 'YES' ELSE 'NO' END,
                        'encoder_feedback', CASE WHEN random() > 0.3 THEN 'YES' ELSE 'NO' END
                    ),
                    'environmental', jsonb_build_object(
                        'temperature', (20 + random() * 10)::INT,
                        'humidity', (50 + random() * 20)::INT,
                        'vibration', CASE WHEN random() > 0.5 THEN 'LOW' ELSE 'MEDIUM' END
                    )
                )
                WHERE tickets.ticket_id = current_ticket_id;
            
            WHEN 'B' THEN -- 电气/IO
                UPDATE tickets SET facts_json = jsonb_build_object(
                    'electrical', jsonb_build_object(
                        'voltage_reading', (220 + random() * 20)::NUMERIC(6,2),
                        'current_reading', (5 + random() * 10)::NUMERIC(6,2),
                        'sensor_status', CASE WHEN random() > 0.4 THEN 'OK' ELSE 'FAULT' END,
                        'io_signal', CASE WHEN random() > 0.3 THEN 'YES' ELSE 'NO' END,
                        'power_supply', CASE WHEN random() > 0.2 THEN 'STABLE' ELSE 'UNSTABLE' END,
                        'communication_timeout', CASE WHEN random() > 0.5 THEN 'YES' ELSE 'NO' END
                    ),
                    'mechanical', jsonb_build_object(
                        'connection_status', CASE WHEN random() > 0.3 THEN 'GOOD' ELSE 'LOOSE' END,
                        'cable_integrity', CASE WHEN random() > 0.2 THEN 'YES' ELSE 'NO' END
                    ),
                    'environmental', jsonb_build_object(
                        'temperature', (25 + random() * 10)::INT,
                        'humidity', (60 + random() * 15)::INT
                    )
                )
                WHERE tickets.ticket_id = current_ticket_id;
            
            WHEN 'C' THEN -- PLC/程序
                UPDATE tickets SET facts_json = jsonb_build_object(
                    'plc', jsonb_build_object(
                        'program_execution', CASE WHEN random() > 0.4 THEN 'NORMAL' ELSE 'TIMEOUT' END,
                        'communication_protocol', CASE 
                            WHEN random() > 0.6 THEN 'MODBUS'
                            WHEN random() > 0.3 THEN 'PROFINET'
                            ELSE 'ETHERNET/IP'
                        END,
                        'timeout_occurred', CASE WHEN random() > 0.5 THEN 'YES' ELSE 'NO' END,
                        'error_code', CASE WHEN random() > 0.6 THEN 'E' || (1000 + (random() * 100)::INT)::TEXT ELSE NULL END,
                        'cycle_time_ms', (100 + random() * 200)::INT
                    ),
                    'software', jsonb_build_object(
                        'version', ticket_rec.sw_version,
                        'crash_occurred', CASE WHEN random() > 0.7 THEN 'YES' ELSE 'NO' END,
                        'memory_usage', (60 + random() * 30)::INT
                    ),
                    'environmental', jsonb_build_object(
                        'temperature', (22 + random() * 8)::INT,
                        'network_status', CASE WHEN random() > 0.3 THEN 'STABLE' ELSE 'UNSTABLE' END
                    )
                )
                WHERE tickets.ticket_id = current_ticket_id;
            
            WHEN 'D' THEN -- 测试/判定
                UPDATE tickets SET facts_json = jsonb_build_object(
                    'testing', jsonb_build_object(
                        'test_result', CASE WHEN random() > 0.4 THEN 'PASS' ELSE 'FAIL' END,
                        'test_repeatability', CASE WHEN random() > 0.5 THEN 'GOOD' ELSE 'POOR' END,
                        'data_anomaly', CASE WHEN random() > 0.6 THEN 'YES' ELSE 'NO' END,
                        'measurement_value', (100 + random() * 200)::NUMERIC(8,2),
                        'expected_value', (100 + random() * 200)::NUMERIC(8,2),
                        'tolerance', (5 + random() * 10)::NUMERIC(5,2),
                        'false_positive_rate', (random() * 15)::NUMERIC(5,2)
                    ),
                    'judgment', jsonb_build_object(
                        'judgment_logic', CASE WHEN random() > 0.4 THEN 'CORRECT' ELSE 'ERROR' END,
                        'threshold_setting', CASE WHEN random() > 0.5 THEN 'APPROPRIATE' ELSE 'TOO_STRICT' END,
                        'calibration_status', CASE WHEN random() > 0.3 THEN 'CALIBRATED' ELSE 'NEEDS_CALIBRATION' END
                    ),
                    'environmental', jsonb_build_object(
                        'temperature', (23 + random() * 7)::INT,
                        'humidity', (55 + random() * 15)::INT,
                        'test_environment', CASE WHEN random() > 0.4 THEN 'STABLE' ELSE 'VARIABLE' END
                    )
                )
                WHERE tickets.ticket_id = current_ticket_id;
            
            WHEN 'E' THEN -- 系统/环境
                UPDATE tickets SET facts_json = jsonb_build_object(
                    'system', jsonb_build_object(
                        'system_status', CASE WHEN random() > 0.5 THEN 'NORMAL' ELSE 'DEGRADED' END,
                        'occasional_fault', CASE WHEN random() > 0.4 THEN 'YES' ELSE 'NO' END,
                        'fault_frequency', CASE 
                            WHEN random() > 0.7 THEN 'DAILY'
                            WHEN random() > 0.4 THEN 'WEEKLY'
                            ELSE 'MONTHLY'
                        END,
                        'data_sync_status', CASE WHEN random() > 0.5 THEN 'SYNCED' ELSE 'OUT_OF_SYNC' END,
                        'display_issue', CASE WHEN random() > 0.6 THEN 'YES' ELSE 'NO' END
                    ),
                    'environmental', jsonb_build_object(
                        'temperature', (24 + random() * 6)::INT,
                        'humidity', (58 + random() * 12)::INT,
                        'power_quality', CASE WHEN random() > 0.4 THEN 'GOOD' ELSE 'POOR' END,
                        'electromagnetic_interference', CASE WHEN random() > 0.6 THEN 'YES' ELSE 'NO' END,
                        'vibration_level', CASE 
                            WHEN random() > 0.6 THEN 'HIGH'
                            WHEN random() > 0.3 THEN 'MEDIUM'
                            ELSE 'LOW'
                        END
                    ),
                    'network', jsonb_build_object(
                        'network_status', CASE WHEN random() > 0.5 THEN 'STABLE' ELSE 'UNSTABLE' END,
                        'latency_ms', (10 + random() * 50)::INT,
                        'packet_loss', (random() * 5)::NUMERIC(4,2)
                    )
                )
                WHERE tickets.ticket_id = current_ticket_id;
        END CASE;
        
        -- 3. 补充已采取行动（如果为空）
        IF ticket_rec.actions_taken IS NULL OR array_length(ticket_rec.actions_taken, 1) = 0 THEN
            CASE ticket_rec.domain
                WHEN 'A' THEN
                    UPDATE tickets SET 
                        actions_taken = ARRAY['CHECK_MECHANICAL', 'MEASURE_POSITION', 'INSPECT_WEAR', 'LUBRICATE'],
                        actions_taken_note = '已检查机械结构，测量定位精度，检查磨损情况，添加润滑'
                    WHERE tickets.ticket_id = current_ticket_id;
                WHEN 'B' THEN
                    UPDATE tickets SET 
                        actions_taken = ARRAY['CHECK_VOLTAGE', 'TEST_SENSOR', 'INSPECT_CABLE', 'REBOOT_SYSTEM'],
                        actions_taken_note = '已检查电压，测试传感器信号，检查电缆连接，重启系统'
                    WHERE tickets.ticket_id = current_ticket_id;
                WHEN 'C' THEN
                    UPDATE tickets SET 
                        actions_taken = ARRAY['CHECK_PROGRAM', 'REVIEW_LOG', 'TEST_COMMUNICATION', 'UPDATE_FIRMWARE'],
                        actions_taken_note = '已检查PLC程序，查看日志，测试通信，更新固件'
                    WHERE tickets.ticket_id = current_ticket_id;
                WHEN 'D' THEN
                    UPDATE tickets SET 
                        actions_taken = ARRAY['REPEAT_TEST', 'CHECK_CALIBRATION', 'REVIEW_THRESHOLD', 'ANALYZE_DATA'],
                        actions_taken_note = '已重复测试，检查校准状态，审查判定阈值，分析测试数据'
                    WHERE tickets.ticket_id = current_ticket_id;
                WHEN 'E' THEN
                    UPDATE tickets SET 
                        actions_taken = ARRAY['CHECK_ENVIRONMENT', 'MONITOR_SYSTEM', 'REVIEW_LOGS', 'TEST_NETWORK'],
                        actions_taken_note = '已检查环境条件，监控系统状态，查看日志，测试网络连接'
                    WHERE tickets.ticket_id = current_ticket_id;
            END CASE;
        END IF;
        
        -- 4. 生成客户沟通记录（每个工单1-3条）- 如果表存在
        BEGIN
            FOR j IN 1..(1 + (random() * 2)::INT) LOOP
                INSERT INTO customer_comms (
                    id, ticket_id, content, comm_type,
                    by_user_id, communicated_at, customer_feedback, created_at
                )
                VALUES (
                    gen_random_uuid(),
                    current_ticket_id,
                    CASE j
                        WHEN 1 THEN '已与客户沟通，了解问题发生的具体情况和时间。客户反馈问题在' || 
                                    CASE WHEN random() > 0.5 THEN '上午' ELSE '下午' END || '出现，影响生产进度。'
                        WHEN 2 THEN '向客户说明问题分析进展，预计' || (1 + (random() * 3)::INT)::TEXT || '天内提供解决方案。'
                        ELSE '跟进客户反馈，确认解决方案实施效果。客户表示满意。'
                    END,
                    communication_types[1 + (random() * (array_length(communication_types, 1) - 1))::INT],
                    user_id,
                    ticket_rec.created_at + (j || ' days')::INTERVAL + (random() * INTERVAL '1 day'),
                    CASE WHEN random() > 0.5 THEN 
                        '客户对处理速度表示满意，希望尽快解决。'
                    ELSE NULL END,
                    ticket_rec.created_at + (j || ' days')::INTERVAL + (random() * INTERVAL '1 day')
                );
            END LOOP;
        EXCEPTION
            WHEN undefined_table THEN
                RAISE NOTICE 'customer_comms表不存在，跳过客户沟通记录生成';
        END;
        
        -- 5. 如果状态是SolutionIssued、Verifying或Closed，生成解决方案
        IF ticket_rec.status IN ('SolutionIssued', 'Verifying', 'Closed') THEN
            solution_id := gen_random_uuid();
            
            INSERT INTO solutions (
                solution_id, solution_code, ticket_id, title, description,
                solution_type, release_type,
                required_sw_version, required_plc_version, required_param_version,
                new_sw_version, new_plc_version, new_param_version,
                change_detail_json, verification_checklist_json,
                implementation_steps, estimated_implementation_time,
                risk_level, risk_description, rollback_possible, rollback_procedure,
                status, created_by, created_at, updated_at, published_at, published_by
            )
            VALUES (
                solution_id,
                'SOL-' || solution_year || '-' || LPAD(solution_counter::TEXT, 3, '0'),
                current_ticket_id,
                '解决方案：' || ticket_rec.symptom_title,
                '针对' || ticket_rec.symptom_title || '问题的解决方案。' ||
                CASE ticket_rec.domain
                    WHEN 'A' THEN '通过调整机械结构参数和优化定位算法来解决定位精度问题。'
                    WHEN 'B' THEN '通过更换传感器和优化电气连接来解决信号异常问题。'
                    WHEN 'C' THEN '通过修改PLC程序逻辑和优化通信协议来解决程序执行问题。'
                    WHEN 'D' THEN '通过调整测试判定逻辑和校准测试设备来解决测试判定问题。'
                    ELSE '通过优化系统配置和改善环境条件来解决系统级问题。'
                END,
                CASE ticket_rec.domain
                    WHEN 'A' THEN 'procedure'
                    WHEN 'B' THEN 'hardware_replacement'
                    WHEN 'C' THEN 'software_update'
                    WHEN 'D' THEN 'parameter_change'
                    ELSE 'procedure'
                END,
                CASE WHEN random() > 0.5 THEN 'MIXED' ELSE 'SOFTWARE' END,
                ticket_rec.sw_version,
                ticket_rec.plc_version,
                ticket_rec.param_version,
                CASE WHEN random() > 0.5 THEN 'V' || (2 + (random() * 2)::INT) || '.' || (random() * 9)::INT || '.' || (random() * 9)::INT ELSE NULL END,
                CASE WHEN random() > 0.5 THEN 'V' || (1 + (random() * 2)::INT) || '.' || (random() * 9)::INT || '.' || (random() * 9)::INT ELSE NULL END,
                CASE WHEN random() > 0.5 THEN 'P' || TO_CHAR(CURRENT_DATE, 'YYYYMMDD') ELSE NULL END,
                jsonb_build_object(
                    'changes', jsonb_build_array(
                        jsonb_build_object('type', 'parameter', 'field', 'position_tolerance', 'old_value', 0.2, 'new_value', 0.15),
                        jsonb_build_object('type', 'program', 'file', 'main.prg', 'line', 120, 'change', '优化定位算法')
                    )
                ),
                jsonb_build_object(
                    'checklist', jsonb_build_array(
                        jsonb_build_object('item', '验证定位精度', 'required', true),
                        jsonb_build_object('item', '检查机械结构', 'required', true),
                        jsonb_build_object('item', '测试运行稳定性', 'required', true)
                    )
                ),
                '1. 备份当前参数配置\n2. 更新软件版本\n3. 调整参数设置\n4. 执行测试验证\n5. 确认问题解决',
                (30 + (random() * 120)::INT),
                CASE WHEN random() > 0.7 THEN 'high' WHEN random() > 0.4 THEN 'medium' ELSE 'low' END,
                CASE WHEN random() > 0.5 THEN '需要停机维护，可能影响生产计划' ELSE NULL END,
                random() > 0.3,
                CASE WHEN random() > 0.5 THEN '如需回滚，请恢复备份的参数配置和软件版本' ELSE NULL END,
                'Published',
                user_id,
                ticket_rec.created_at + INTERVAL '4 hours',
                ticket_rec.created_at + INTERVAL '4 hours',
                ticket_rec.created_at + INTERVAL '4 hours',
                user_id
            );
            
            solution_counter := solution_counter + 1;
            
            -- 6. 如果状态是Verifying或Closed，生成验证记录
            IF ticket_rec.status IN ('Verifying', 'Closed') THEN
                INSERT INTO verifications (
                    verification_id, ticket_id, solution_id, verification_type, status,
                    executed_by, executed_at, verified_at,
                    verification_result, verification_notes,
                    checklist_results, attachments,
                    run_count, pass_count, fail_count, result,
                    checklist_result_json, evidence_attachment_ids,
                    created_at, updated_at
                )
                VALUES (
                    gen_random_uuid(),
                    current_ticket_id,
                    solution_id,
                    'solution_verification',
                    CASE WHEN ticket_rec.status = 'Closed' THEN 'completed' ELSE 'in_progress' END,
                    ticket_rec.created_by_user_id,
                    ticket_rec.created_at + INTERVAL '1 day',
                    ticket_rec.created_at + INTERVAL '1 day' + INTERVAL '2 hours',
                    CASE WHEN ticket_rec.status = 'Closed' THEN 
                        '验证通过，解决方案有效，问题已解决。'
                    ELSE 
                        '验证进行中，已执行' || (1 + (random() * 2)::INT)::TEXT || '次测试。'
                    END,
                    CASE WHEN ticket_rec.status = 'Closed' THEN 
                        '按照解决方案执行后，问题已完全解决，系统运行正常。'
                    ELSE 
                        '正在验证解决方案的有效性，需要进一步测试。'
                    END,
                    jsonb_build_object(
                        'items', jsonb_build_array(
                            jsonb_build_object('item', '验证定位精度', 'result', 'PASS', 'note', '定位精度符合要求'),
                            jsonb_build_object('item', '检查机械结构', 'result', 'PASS', 'note', '机械结构正常'),
                            jsonb_build_object('item', '测试运行稳定性', 'result', 'PASS', 'note', '运行稳定')
                        )
                    ),
                    jsonb_build_array(
                        jsonb_build_object('type', 'photo', 'url', '/attachments/verification_' || current_ticket_id::TEXT || '_1.jpg')
                    ),
                    (1 + (random() * 5)::INT),
                    CASE WHEN ticket_rec.status = 'Closed' THEN (1 + (random() * 5)::INT) ELSE (random() * 2)::INT END,
                    CASE WHEN ticket_rec.status = 'Closed' THEN 0 ELSE (random() * 1)::INT END,
                    CASE WHEN ticket_rec.status = 'Closed' THEN 'PASS' ELSE 'PARTIAL' END,
                    jsonb_build_object(
                        'verification_items', jsonb_build_array(
                            jsonb_build_object('name', '定位精度验证', 'status', 'PASS'),
                            jsonb_build_object('name', '稳定性测试', 'status', 'PASS')
                        )
                    ),
                    ARRAY[]::UUID[],
                    ticket_rec.created_at + INTERVAL '1 day',
                    ticket_rec.created_at + INTERVAL '1 day' + INTERVAL '2 hours'
                );
            END IF;
        END IF;
        
        -- 7. 生成附件（每个工单1-4个）
        FOR j IN 1..(1 + (random() * 3)::INT) LOOP
            INSERT INTO attachments (
                attachment_id, ticket_id, uploaded_by, file_type, file_name, file_key,
                file_size, mime_type, upload_status, description, created_at
            )
            VALUES (
                gen_random_uuid(),
                current_ticket_id,
                ticket_rec.created_by_user_id,
                file_types[1 + (random() * (array_length(file_types, 1) - 1))::INT],
                CASE 
                    WHEN file_types[1 + (random() * (array_length(file_types, 1) - 1))::INT] = 'photo' THEN 
                        'problem_photo_' || j || '.jpg'
                    WHEN file_types[1 + (random() * (array_length(file_types, 1) - 1))::INT] = 'video' THEN 
                        'problem_video_' || j || '.mp4'
                    WHEN file_types[1 + (random() * (array_length(file_types, 1) - 1))::INT] = 'log' THEN 
                        'system_log_' || j || '.txt'
                    ELSE 
                        'document_' || j || '.pdf'
                END,
                'tickets/' || current_ticket_id::TEXT || '/' || j || '/' || 
                CASE 
                    WHEN file_types[1 + (random() * (array_length(file_types, 1) - 1))::INT] = 'photo' THEN 'photo.jpg'
                    WHEN file_types[1 + (random() * (array_length(file_types, 1) - 1))::INT] = 'video' THEN 'video.mp4'
                    WHEN file_types[1 + (random() * (array_length(file_types, 1) - 1))::INT] = 'log' THEN 'log.txt'
                    ELSE 'doc.pdf'
                END,
                (100000 + random() * 5000000)::BIGINT,
                CASE 
                    WHEN file_types[1 + (random() * (array_length(file_types, 1) - 1))::INT] = 'photo' THEN 'image/jpeg'
                    WHEN file_types[1 + (random() * (array_length(file_types, 1) - 1))::INT] = 'video' THEN 'video/mp4'
                    WHEN file_types[1 + (random() * (array_length(file_types, 1) - 1))::INT] = 'log' THEN 'text/plain'
                    ELSE 'application/pdf'
                END,
                'completed',
                CASE j
                    WHEN 1 THEN '问题现场照片'
                    WHEN 2 THEN '系统日志文件'
                    WHEN 3 THEN '测试视频记录'
                    ELSE '相关文档'
                END,
                ticket_rec.created_at + (random() * INTERVAL '1 day')
            );
        END LOOP;
        
        -- 每10个工单输出一次进度
        IF i % 10 = 0 THEN
            RAISE NOTICE '已处理 % / % 个工单', i, array_length(ticket_ids, 1);
        END IF;
    END LOOP;
    
    RAISE NOTICE '数据补充完成！';
    
END $$;

-- 验证数据
SELECT 
    '工单总数' as type, COUNT(*)::TEXT as count FROM tickets WHERE ticket_no LIKE 'TKT%'
UNION ALL
SELECT '有状态历史的工单', COUNT(DISTINCT ticket_id)::TEXT FROM ticket_status_history WHERE ticket_id IN (SELECT ticket_id FROM tickets WHERE ticket_no LIKE 'TKT%')
UNION ALL
SELECT '有客户沟通的工单', '0'::TEXT  -- customer_comms表暂未创建
UNION ALL
SELECT '有解决方案的工单', COUNT(DISTINCT ticket_id)::TEXT FROM solutions WHERE ticket_id IN (SELECT ticket_id FROM tickets WHERE ticket_no LIKE 'TKT%')
UNION ALL
SELECT '有验证记录的工单', COUNT(DISTINCT ticket_id)::TEXT FROM verifications WHERE ticket_id IN (SELECT ticket_id FROM tickets WHERE ticket_no LIKE 'TKT%')
UNION ALL
SELECT '有附件的工单', COUNT(DISTINCT ticket_id)::TEXT FROM attachments WHERE ticket_id IN (SELECT ticket_id FROM tickets WHERE ticket_no LIKE 'TKT%');

