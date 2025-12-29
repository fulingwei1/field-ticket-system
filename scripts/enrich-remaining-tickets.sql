-- 为剩余的280个工单添加丰富信息
-- 基于工单状态和序号添加不同的信息

DO $$
DECLARE
    ticket_rec RECORD;
    ticket_j INT;
    project_num INT;
    device_num INT;
    current_status VARCHAR(20);
    assigned_engineer_id UUID;
    attributed_engineer_id UUID;
    senior_engineer1_id UUID := '01e1f5e2-bff2-443d-b7a2-84fb4dcb00ce'; -- 陈售前技术
    senior_engineer2_id UUID := 'f8b86880-c4fb-4a0c-b118-087029f6d1ae'; -- 刘售前技术
    senior_engineers UUID[] := ARRAY[senior_engineer1_id, senior_engineer2_id];
    ticket_counter INT := 0;
BEGIN
    -- 遍历所有没有丰富信息的工单
    FOR ticket_rec IN 
        SELECT t.ticket_id, t.ticket_no, t.status, t.priority, t.device_id,
               ROW_NUMBER() OVER (PARTITION BY t.device_id ORDER BY t.created_at) as device_ticket_seq
        FROM tickets t
        WHERE t.ticket_no LIKE 'TK-%' 
          AND (t.facts_json IS NULL OR t.facts_json::text = '{}')
        ORDER BY t.device_id, t.created_at
    LOOP
        ticket_j := ticket_rec.device_ticket_seq;
        current_status := ticket_rec.status;
        
        -- 根据工单序号设置不同的状态和字段
        CASE ticket_j
            WHEN 1 THEN -- 草稿状态
                assigned_engineer_id := NULL;
                attributed_engineer_id := NULL;
            WHEN 2 THEN -- 已提交
                assigned_engineer_id := senior_engineers[1];
                attributed_engineer_id := NULL;
            WHEN 3 THEN -- 分诊中
                assigned_engineer_id := senior_engineers[2];
                attributed_engineer_id := NULL;
            WHEN 4 THEN -- 已发布解决方案
                assigned_engineer_id := senior_engineers[1];
                attributed_engineer_id := senior_engineers[1];
            ELSE -- 验证中或已关闭
                assigned_engineer_id := senior_engineers[2];
                attributed_engineer_id := senior_engineers[2];
        END CASE;
        
        -- 更新工单信息
        UPDATE tickets SET
            facts_json = CASE ticket_j
                WHEN 1 THEN '{"error_code": "COM_001", "timeout": 30, "restart_works": true, "network_ok": true}'::JSONB
                WHEN 2 THEN '{"signal_range": "0-10V", "fluctuation": "±0.5V", "wiring_ok": true, "sensor_replaced": true}'::JSONB
                WHEN 3 THEN '{"action_type": "grasp", "expected_time": 5, "actual_time": 8, "joints_ok": true}'::JSONB
                WHEN 4 THEN '{"temperature": 85, "threshold": 80, "cooling_ok": true, "load_high": true}'::JSONB
                ELSE '{"pressure_reading": 0, "actual_pressure": "normal", "power_ok": true, "wiring_ok": true}'::JSONB
            END,
            actions_taken = CASE ticket_j
                WHEN 1 THEN ARRAY['重启设备', '检查网络连接']
                WHEN 2 THEN ARRAY['检查传感器接线', '更换传感器', '检查信号干扰源']
                WHEN 3 THEN ARRAY['检查机械臂关节', '检查程序逻辑', '分析路径规划']
                WHEN 4 THEN ARRAY['检查温度传感器', '检查冷却系统', '检查设备负载', '优化冷却参数']
                ELSE ARRAY['检查传感器供电', '检查接线', '更换传感器', '验证效果']
            END,
            actions_taken_note = CASE WHEN ticket_j > 2 THEN '已执行上述动作，问题正在处理中。' ELSE NULL END,
            confirmed_as_fact = ticket_j > 2,
            confirmed_at = CASE WHEN ticket_j > 2 THEN CURRENT_TIMESTAMP - ((ticket_j - 2) || ' days')::INTERVAL ELSE NULL END,
            alarm_code = CASE ticket_j 
                WHEN 1 THEN NULL 
                WHEN 2 THEN 'ALM_SENSOR_001' 
                WHEN 3 THEN 'ALM_ROBOT_001' 
                WHEN 4 THEN 'ALM_TEMP_001' 
                ELSE 'ALM_PRESS_001' 
            END,
            current_jc_code = CASE ticket_j 
                WHEN 3 THEN 'JC_MECH_001' 
                WHEN 4 THEN 'JC_ELEC_001' 
                WHEN 5 THEN 'JC_QUALITY_001' 
                ELSE NULL 
            END,
            assigned_to = assigned_engineer_id,
            root_cause = CASE ticket_j
                WHEN 4 THEN '设备负载过高导致发热量增大，冷却系统参数设置不当，无法及时散热。'
                WHEN 5 THEN '压力传感器硬件故障，传感器内部元件损坏。'
                ELSE NULL
            END,
            root_responsibility = CASE ticket_j 
                WHEN 4 THEN 'parameter' 
                WHEN 5 THEN 'assembly' 
                ELSE NULL 
            END,
            responsibility_team = CASE ticket_j 
                WHEN 4 THEN '电气工程部' 
                WHEN 5 THEN '质量部' 
                ELSE NULL 
            END,
            is_preventable = CASE WHEN ticket_j > 3 THEN (ticket_j % 2 = 0) ELSE NULL END,
            responsibility_notes = CASE WHEN ticket_j > 3 THEN '已进行根因分析，责任已明确。' ELSE NULL END,
            attributed_by = attributed_engineer_id,
            attributed_at = CASE WHEN attributed_engineer_id IS NOT NULL THEN CURRENT_TIMESTAMP - ((ticket_j - 1) || ' days')::INTERVAL ELSE NULL END,
            symptom_detail = CASE ticket_j
                WHEN 1 THEN symptom_detail || E'\n\n补充信息：设备在运行过程中出现通信异常，具体表现为：1. PLC与上位机通信中断，通信超时时间约30秒；2. 重启后通信恢复正常，但运行一段时间后再次出现；3. 检查网络连接正常，怀疑是程序逻辑问题或硬件故障。已记录相关日志，待进一步分析。'
                WHEN 2 THEN symptom_detail || E'\n\n补充信息：传感器信号在设备运行过程中出现不稳定现象：1. 信号值在正常范围内波动，但波动幅度超过允许范围；2. 检查传感器接线正常，无松动现象；3. 更换传感器后问题依然存在，怀疑是信号干扰或程序滤波参数设置不当。已提交技术部门分析。'
                WHEN 3 THEN symptom_detail || E'\n\n补充信息：机械臂在执行动作时出现超时故障：1. 机械臂在执行抓取动作时，动作时间超过设定值（设定5秒，实际用时8秒）；2. 检查机械臂各关节运行正常，无卡顿现象；3. 检查程序逻辑，发现路径规划可能存在问题。已分配给机械工程师处理。'
                WHEN 4 THEN symptom_detail || E'\n\n补充信息：设备温度报警，温度传感器检测到异常高温：1. 温度达到85°C，超过设定阈值80°C；2. 检查冷却系统运行正常，但冷却效果不佳；3. 检查设备负载，发现负载过高。已发布解决方案：优化冷却系统参数，降低设备负载。'
                ELSE symptom_detail || E'\n\n补充信息：压力传感器故障，检测值异常：1. 压力传感器读数始终为0，但实际压力正常；2. 检查传感器供电正常，接线正常；3. 怀疑传感器损坏或信号处理模块故障。已更换传感器，正在验证效果。'
            END
        WHERE ticket_id = ticket_rec.ticket_id;
        
        ticket_counter := ticket_counter + 1;
    END LOOP;
    
    RAISE NOTICE '已更新 % 个工单的丰富信息', ticket_counter;
END $$;


