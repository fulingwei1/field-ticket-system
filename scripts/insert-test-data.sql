-- 插入测试数据
-- 创建3-4个客户，每个客户下2-3个项目，每个项目下5-6台设备，每台设备下5-6个工单

DO $$
DECLARE
    -- 客户变量
    customer1_id UUID;
    customer2_id UUID;
    customer3_id UUID;
    customer4_id UUID;
    
    -- 项目变量（每个客户2-3个项目）
    project1_id UUID;
    project2_id UUID;
    project3_id UUID;
    project4_id UUID;
    project5_id UUID;
    project6_id UUID;
    project7_id UUID;
    project8_id UUID;
    project9_id UUID;
    project10_id UUID;
    
    -- 设备变量（每个项目5-6台设备）
    current_device_id UUID;
    
    -- 工单变量
    ticket_id UUID;
    ticket_no_counter INT := 1;
    ticket_no_base VARCHAR(20);
    
    -- 工程师ID（使用客服工程师和现场工程师）
    engineer1_id UUID := '92ca664d-cc50-4316-8177-6698cca66a3a'; -- 朱客服工程师
    engineer2_id UUID := '9f6cab03-de84-4d1a-aa78-5cadfbf14983'; -- 秦客服工程师
    engineer3_id UUID := '1cb75999-daff-422d-8d2b-0ea0209a0bc3'; -- 褚测试工程师
    engineer4_id UUID := '7482838f-7d57-43e1-999f-12e008f0dca8'; -- 卫测试工程师
    engineer5_id UUID := 'e716769e-cf49-42e2-838e-5e63cd751d9c'; -- 郑电气工程师
    engineer6_id UUID := '97b5d02b-e910-4aba-8a58-85f3a0b773d1'; -- 王电气工程师
    
    -- 高级工程师ID（用于分配和归因）
    senior_engineer1_id UUID := '01e1f5e2-bff2-443d-b7a2-84fb4dcb00ce'; -- 陈售前技术
    senior_engineer2_id UUID := 'f8b86880-c4fb-4a0c-b118-087029f6d1ae'; -- 刘售前技术
    
    -- 当前工程师索引
    current_engineer_idx INT := 1;
    current_engineer_id UUID;
    assigned_engineer_id UUID;
    attributed_engineer_id UUID;
    engineers UUID[] := ARRAY[
        engineer1_id, engineer2_id, engineer3_id, 
        engineer4_id, engineer5_id, engineer6_id
    ];
    senior_engineers UUID[] := ARRAY[senior_engineer1_id, senior_engineer2_id];
    
    -- 循环变量
    i INT;
    j INT;
    k INT;
    l INT;
    device_counter INT;
    ticket_counter INT;
    
    -- 工单编号生成
    ticket_date DATE := CURRENT_DATE;
    ticket_date_str VARCHAR(8) := TO_CHAR(ticket_date, 'YYYYMMDD');
BEGIN
    -- 1. 创建4个客户
    INSERT INTO customers (customer_id, customer_name, customer_code, industry_type, contact_person, contact_phone)
    VALUES 
        (gen_random_uuid(), '华为技术有限公司', 'CUST001', '通信设备', '张经理', '13800138001'),
        (gen_random_uuid(), '比亚迪汽车工业有限公司', 'CUST002', '汽车制造', '李经理', '13800138002'),
        (gen_random_uuid(), '宁德时代新能源科技股份有限公司', 'CUST003', '新能源', '王经理', '13800138003'),
        (gen_random_uuid(), '三一重工股份有限公司', 'CUST004', '工程机械', '赵经理', '13800138004');
    
    SELECT customer_id INTO customer1_id FROM customers WHERE customer_code = 'CUST001';
    SELECT customer_id INTO customer2_id FROM customers WHERE customer_code = 'CUST002';
    SELECT customer_id INTO customer3_id FROM customers WHERE customer_code = 'CUST003';
    SELECT customer_id INTO customer4_id FROM customers WHERE customer_code = 'CUST004';
    
    RAISE NOTICE '创建了4个客户: %, %, %, %', customer1_id, customer2_id, customer3_id, customer4_id;
    
    -- 2. 为每个客户创建2-3个项目
    -- 客户1: 3个项目
    INSERT INTO projects (project_id, project_no, project_name, customer_id, customer_name, device_type, industry_type, 
                          sales_amount, quantity, order_date, required_delivery_date, project_status)
    VALUES 
        (gen_random_uuid(), 'PRJ001-001', '华为5G基站自动化产线项目', customer1_id, '华为技术有限公司', 'PLC控制器', '通信设备', 5000000.00, 50, '2024-01-15', '2024-06-30', '进行中'),
        (gen_random_uuid(), 'PRJ001-002', '华为智能终端组装线项目', customer1_id, '华为技术有限公司', '机器人', '通信设备', 8000000.00, 30, '2024-02-01', '2024-08-31', '进行中'),
        (gen_random_uuid(), 'PRJ001-003', '华为数据中心自动化项目', customer1_id, '华为技术有限公司', '传感器', '通信设备', 12000000.00, 100, '2024-03-01', '2024-12-31', '进行中');
    
    SELECT project_id INTO project1_id FROM projects WHERE project_no = 'PRJ001-001' LIMIT 1;
    SELECT project_id INTO project2_id FROM projects WHERE project_no = 'PRJ001-002' LIMIT 1;
    SELECT project_id INTO project3_id FROM projects WHERE project_no = 'PRJ001-003' LIMIT 1;
    
    -- 客户2: 2个项目
    INSERT INTO projects (project_id, project_no, project_name, customer_id, customer_name, device_type, industry_type, 
                          sales_amount, quantity, order_date, required_delivery_date, project_status)
    VALUES 
        (gen_random_uuid(), 'PRJ002-001', '比亚迪电池包自动化产线', customer2_id, '比亚迪汽车工业有限公司', 'PLC控制器', '汽车制造', 6000000.00, 40, '2024-01-20', '2024-07-31', '进行中'),
        (gen_random_uuid(), 'PRJ002-002', '比亚迪车身焊接自动化线', customer2_id, '比亚迪汽车工业有限公司', '机器人', '汽车制造', 9000000.00, 25, '2024-02-15', '2024-09-30', '进行中');
    
    SELECT project_id INTO project4_id FROM projects WHERE project_no = 'PRJ002-001' LIMIT 1;
    SELECT project_id INTO project5_id FROM projects WHERE project_no = 'PRJ002-002' LIMIT 1;
    
    -- 客户3: 3个项目
    INSERT INTO projects (project_id, project_no, project_name, customer_id, customer_name, device_type, industry_type, 
                          sales_amount, quantity, order_date, required_delivery_date, project_status)
    VALUES 
        (gen_random_uuid(), 'PRJ003-001', '宁德时代电芯自动化产线', customer3_id, '宁德时代新能源科技股份有限公司', 'PLC控制器', '新能源', 7000000.00, 60, '2024-01-25', '2024-08-15', '进行中'),
        (gen_random_uuid(), 'PRJ003-002', '宁德时代模组自动化装配线', customer3_id, '宁德时代新能源科技股份有限公司', '机器人', '新能源', 10000000.00, 35, '2024-02-20', '2024-10-31', '进行中'),
        (gen_random_uuid(), 'PRJ003-003', '宁德时代PACK自动化产线', customer3_id, '宁德时代新能源科技股份有限公司', '传感器', '新能源', 15000000.00, 80, '2024-03-10', '2025-01-31', '进行中');
    
    SELECT project_id INTO project6_id FROM projects WHERE project_no = 'PRJ003-001' LIMIT 1;
    SELECT project_id INTO project7_id FROM projects WHERE project_no = 'PRJ003-002' LIMIT 1;
    SELECT project_id INTO project8_id FROM projects WHERE project_no = 'PRJ003-003' LIMIT 1;
    
    -- 客户4: 2个项目
    INSERT INTO projects (project_id, project_no, project_name, customer_id, customer_name, device_type, industry_type, 
                          sales_amount, quantity, order_date, required_delivery_date, project_status)
    VALUES 
        (gen_random_uuid(), 'PRJ004-001', '三一重工挖掘机自动化产线', customer4_id, '三一重工股份有限公司', 'PLC控制器', '工程机械', 8000000.00, 45, '2024-02-01', '2024-09-15', '进行中'),
        (gen_random_uuid(), 'PRJ004-002', '三一重工起重机自动化装配线', customer4_id, '三一重工股份有限公司', '机器人', '工程机械', 11000000.00, 30, '2024-02-25', '2024-11-30', '进行中');
    
    SELECT project_id INTO project9_id FROM projects WHERE project_no = 'PRJ004-001' LIMIT 1;
    SELECT project_id INTO project10_id FROM projects WHERE project_no = 'PRJ004-002' LIMIT 1;
    
    RAISE NOTICE '创建了10个项目';
    
    -- 3. 为每个项目创建5-6台设备，并为每台设备创建5-6个工单
    -- 项目1: 5台设备
    FOR i IN 1..5 LOOP
        INSERT INTO devices (device_id, device_sn, device_name, project_id, device_type, model, location, status)
        VALUES (
            gen_random_uuid(),
            'DEV-PRJ001-001-' || LPAD(CAST(i AS TEXT), 3, '0'),
            'PLC控制器-' || CAST(i AS TEXT),
            project1_id,
            'PLC控制器',
            'Siemens S7-1500',
            '产线A段-' || CAST(i AS TEXT) || '号工位',
            'active'
        )
        RETURNING device_id INTO current_device_id;
        
        -- 为每台设备创建5个工单
        FOR j IN 1..5 LOOP
            current_engineer_id := engineers[1 + ((i * 5 + j - 1) % array_length(engineers, 1))];
            ticket_no_base := 'TK-' || ticket_date_str || '-' || LPAD(CAST(ticket_no_counter AS TEXT), 4, '0');
            
            -- 根据工单序号设置不同的状态和字段
            CASE j
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
                ELSE -- 验证中
                    assigned_engineer_id := senior_engineers[2];
                    attributed_engineer_id := senior_engineers[2];
            END CASE;
            
            INSERT INTO tickets (
                ticket_id, ticket_no, customer_id, project_id, device_id, created_by_user_id,
                domain, step_code, step_name, symptom_title, symptom_detail,
                repro_rate, reboot_recovers, env_related,
                sw_version, plc_version, param_version, hw_version,
                facts_json, actions_taken, actions_taken_note,
                confirmed_as_fact, confirmed_at,
                alarm_code, status, priority,
                current_jc_code, assigned_to,
                root_cause, root_responsibility, responsibility_team, is_preventable, responsibility_notes,
                attributed_by, attributed_at,
                created_at, updated_at, submitted_at, closed_at
            )
            VALUES (
                gen_random_uuid(),
                ticket_no_base,
                customer1_id,
                project1_id,
                current_device_id,
                current_engineer_id,
                CASE (j % 5) + 1 WHEN 1 THEN 'A' WHEN 2 THEN 'B' WHEN 3 THEN 'C' WHEN 4 THEN 'D' ELSE 'E' END,
                'Step_' || CAST(100 + j * 10 AS TEXT),
                '步骤' || CAST(100 + j * 10 AS TEXT),
                '设备' || CAST(i AS TEXT) || '工单' || CAST(j AS TEXT) || '：' || CASE j 
                    WHEN 1 THEN 'PLC通信异常'
                    WHEN 2 THEN '传感器信号不稳定'
                    WHEN 3 THEN '机械臂动作超时'
                    WHEN 4 THEN '温度报警'
                    ELSE '压力传感器故障'
                END,
                CASE j
                    WHEN 1 THEN '设备在运行过程中出现通信异常，具体表现为：1. PLC与上位机通信中断，通信超时时间约30秒；2. 重启后通信恢复正常，但运行一段时间后再次出现；3. 检查网络连接正常，怀疑是程序逻辑问题或硬件故障。已记录相关日志，待进一步分析。'
                    WHEN 2 THEN '传感器信号在设备运行过程中出现不稳定现象：1. 信号值在正常范围内波动，但波动幅度超过允许范围；2. 检查传感器接线正常，无松动现象；3. 更换传感器后问题依然存在，怀疑是信号干扰或程序滤波参数设置不当。已提交技术部门分析。'
                    WHEN 3 THEN '机械臂在执行动作时出现超时故障：1. 机械臂在执行抓取动作时，动作时间超过设定值（设定5秒，实际用时8秒）；2. 检查机械臂各关节运行正常，无卡顿现象；3. 检查程序逻辑，发现路径规划可能存在问题。已分配给机械工程师处理。'
                    WHEN 4 THEN '设备温度报警，温度传感器检测到异常高温：1. 温度达到85°C，超过设定阈值80°C；2. 检查冷却系统运行正常，但冷却效果不佳；3. 检查设备负载，发现负载过高。已发布解决方案：优化冷却系统参数，降低设备负载。'
                    ELSE '压力传感器故障，检测值异常：1. 压力传感器读数始终为0，但实际压力正常；2. 检查传感器供电正常，接线正常；3. 怀疑传感器损坏或信号处理模块故障。已更换传感器，正在验证效果。'
                END,
                (j * 15)::SMALLINT,
                (j % 2 = 0),
                (j % 3 = 0),
                'V1.' || CAST(j AS TEXT) || '.0',
                'V2.' || CAST(j AS TEXT) || '.0',
                'V1.' || CAST(j + 1 AS TEXT) || '.0',
                'V' || CAST(j AS TEXT) || '.0',
                CASE j
                    WHEN 1 THEN '{"error_code": "COM_001", "timeout": 30, "restart_works": true, "network_ok": true}'::JSONB
                    WHEN 2 THEN '{"signal_range": "0-10V", "fluctuation": "±0.5V", "wiring_ok": true, "sensor_replaced": true}'::JSONB
                    WHEN 3 THEN '{"action_type": "grasp", "expected_time": 5, "actual_time": 8, "joints_ok": true}'::JSONB
                    WHEN 4 THEN '{"temperature": 85, "threshold": 80, "cooling_ok": true, "load_high": true}'::JSONB
                    ELSE '{"pressure_reading": 0, "actual_pressure": "normal", "power_ok": true, "wiring_ok": true}'::JSONB
                END,
                CASE j
                    WHEN 1 THEN ARRAY['重启设备', '检查网络连接']
                    WHEN 2 THEN ARRAY['检查传感器接线', '更换传感器', '检查信号干扰源']
                    WHEN 3 THEN ARRAY['检查机械臂关节', '检查程序逻辑', '分析路径规划']
                    WHEN 4 THEN ARRAY['检查温度传感器', '检查冷却系统', '检查设备负载', '优化冷却参数']
                    ELSE ARRAY['检查传感器供电', '检查接线', '更换传感器', '验证效果']
                END,
                CASE WHEN j > 2 THEN '已执行上述动作，问题正在处理中。' ELSE NULL END,
                j > 2,
                CASE WHEN j > 2 THEN CURRENT_TIMESTAMP - ((j - 2) || ' days')::INTERVAL ELSE NULL END,
                CASE j WHEN 1 THEN NULL WHEN 2 THEN 'ALM_SENSOR_001' WHEN 3 THEN 'ALM_ROBOT_001' WHEN 4 THEN 'ALM_TEMP_001' ELSE 'ALM_PRESS_001' END,
                CASE j WHEN 1 THEN 'Draft' WHEN 2 THEN 'Submitted' WHEN 3 THEN 'Triage' WHEN 4 THEN 'SolutionIssued' ELSE 'Verifying' END,
                CASE j WHEN 1 THEN 'P3' WHEN 2 THEN 'P2' WHEN 3 THEN 'P1' WHEN 4 THEN 'P2' ELSE 'P3' END,
                CASE j WHEN 3 THEN 'JC_MECH_001' WHEN 4 THEN 'JC_ELEC_001' WHEN 5 THEN 'JC_QUALITY_001' ELSE NULL END,
                assigned_engineer_id,
                CASE j
                    WHEN 4 THEN '设备负载过高导致发热量增大，冷却系统参数设置不当，无法及时散热。'
                    WHEN 5 THEN '压力传感器硬件故障，传感器内部元件损坏。'
                    ELSE NULL
                END,
                CASE j WHEN 4 THEN 'parameter' WHEN 5 THEN 'assembly' ELSE NULL END,
                CASE j WHEN 4 THEN '电气工程部' WHEN 5 THEN '质量部' ELSE NULL END,
                CASE WHEN j > 3 THEN (j % 2 = 0) ELSE NULL END,
                CASE WHEN j > 3 THEN '已进行根因分析，责任已明确。' ELSE NULL END,
                attributed_engineer_id,
                CASE WHEN attributed_engineer_id IS NOT NULL THEN CURRENT_TIMESTAMP - ((j - 1) || ' days')::INTERVAL ELSE NULL END,
                CURRENT_TIMESTAMP - (CAST(j AS TEXT) || ' days')::INTERVAL,
                CURRENT_TIMESTAMP - (CAST(j AS TEXT) || ' days')::INTERVAL,
                CASE WHEN j > 1 THEN CURRENT_TIMESTAMP - ((j - 1) || ' days')::INTERVAL ELSE NULL END,
                NULL
            );
            
            ticket_no_counter := ticket_no_counter + 1;
        END LOOP;
    END LOOP;
    
    -- 项目2: 6台设备
    FOR i IN 1..6 LOOP
        INSERT INTO devices (device_id, device_sn, device_name, project_id, device_type, model, location, status)
        VALUES (
            gen_random_uuid(),
            'DEV-PRJ001-002-' || LPAD(CAST(i AS TEXT), 3, '0'),
            '机器人-' || i,
            project2_id,
            '机器人',
            'ABB IRB 6700',
            '产线B段-' || CAST(i AS TEXT) || '号工位',
            'active'
        )
        RETURNING device_id INTO current_device_id;
        
        FOR j IN 1..6 LOOP
            current_engineer_id := engineers[1 + ((i * 6 + j - 1) % array_length(engineers, 1))];
            ticket_no_base := 'TK-' || ticket_date_str || '-' || LPAD(ticket_no_counter::TEXT, 4, '0');
            
            INSERT INTO tickets (
                ticket_id, ticket_no, customer_id, project_id, device_id, created_by_user_id,
                domain, step_code, step_name, symptom_title, symptom_detail,
                repro_rate, reboot_recovers, env_related,
                sw_version, plc_version, param_version, hw_version,
                status, priority, created_at, submitted_at
            )
            VALUES (
                gen_random_uuid(),
                ticket_no_base,
                customer1_id,
                project2_id,
                current_device_id,
                current_engineer_id,
                CASE (j % 5) + 1 WHEN 1 THEN 'A' WHEN 2 THEN 'B' WHEN 3 THEN 'C' WHEN 4 THEN 'D' ELSE 'E' END,
                'Step_' || (200 + j * 10),
                '步骤' || (200 + j * 10),
                '设备' || CAST(i AS TEXT) || '工单' || CAST(j AS TEXT) || '：' || CASE j 
                    WHEN 1 THEN '机器人轨迹偏差'
                    WHEN 2 THEN '末端执行器故障'
                    WHEN 3 THEN '关节电机过热'
                    WHEN 4 THEN '视觉识别错误'
                    WHEN 5 THEN '安全门异常'
                    ELSE '通信模块故障'
                END,
                '详细描述：机器人' || CAST(i AS TEXT) || '在第' || CAST(j AS TEXT) || '次运行时出现故障。',
                (j * 12)::SMALLINT,
                (j % 2 = 1),
                (j % 3 = 1),
                'V1.' || CAST(j AS TEXT) || '.0',
                'V2.' || CAST(j AS TEXT) || '.0',
                'V1.' || (j + 1) || '.0',
                'V' || CAST(j AS TEXT) || '.0',
                CASE j WHEN 1 THEN 'Draft' WHEN 2 THEN 'Submitted' WHEN 3 THEN 'Triage' WHEN 4 THEN 'SolutionIssued' WHEN 5 THEN 'Verifying' ELSE 'Closed' END,
                CASE j WHEN 1 THEN 'P1' WHEN 2 THEN 'P2' WHEN 3 THEN 'P3' ELSE 'P4' END,
                CURRENT_TIMESTAMP - (j || ' days')::INTERVAL,
                CASE WHEN j > 1 THEN CURRENT_TIMESTAMP - ((j - 1) || ' days')::INTERVAL ELSE NULL END
            );
            
            ticket_no_counter := ticket_no_counter + 1;
        END LOOP;
    END LOOP;
    
    -- 项目3: 5台设备
    FOR i IN 1..5 LOOP
        INSERT INTO devices (device_id, device_sn, device_name, project_id, device_type, model, location, status)
        VALUES (
            gen_random_uuid(),
            'DEV-PRJ001-003-' || LPAD(CAST(i AS TEXT), 3, '0'),
            '传感器-' || i,
            project3_id,
            '传感器',
            'Sick DME5000',
            '产线C段-' || CAST(i AS TEXT) || '号工位',
            'active'
        )
        RETURNING device_id INTO current_device_id;
        
        FOR j IN 1..5 LOOP
            current_engineer_id := engineers[1 + ((i * 5 + j - 1) % array_length(engineers, 1))];
            ticket_no_base := 'TK-' || ticket_date_str || '-' || LPAD(ticket_no_counter::TEXT, 4, '0');
            
            INSERT INTO tickets (
                ticket_id, ticket_no, customer_id, project_id, device_id, created_by_user_id,
                domain, step_code, step_name, symptom_title, symptom_detail,
                repro_rate, reboot_recovers, env_related,
                sw_version, plc_version, param_version, hw_version,
                status, priority, created_at, submitted_at
            )
            VALUES (
                gen_random_uuid(),
                ticket_no_base,
                customer1_id,
                project3_id,
                current_device_id,
                current_engineer_id,
                CASE (j % 5) + 1 WHEN 1 THEN 'A' WHEN 2 THEN 'B' WHEN 3 THEN 'C' WHEN 4 THEN 'D' ELSE 'E' END,
                'Step_' || (300 + j * 10),
                '步骤' || (300 + j * 10),
                '设备' || CAST(i AS TEXT) || '工单' || CAST(j AS TEXT) || '：' || CASE j 
                    WHEN 1 THEN '传感器信号丢失'
                    WHEN 2 THEN '测量精度下降'
                    WHEN 3 THEN '响应时间过长'
                    WHEN 4 THEN '环境干扰'
                    ELSE '校准失败'
                END,
                '详细描述：传感器' || CAST(i AS TEXT) || '在第' || CAST(j AS TEXT) || '次运行时出现故障。',
                (j * 18)::SMALLINT,
                (j % 2 = 0),
                (j % 3 = 0),
                'V1.' || CAST(j AS TEXT) || '.0',
                'V2.' || CAST(j AS TEXT) || '.0',
                'V1.' || (j + 1) || '.0',
                'V' || CAST(j AS TEXT) || '.0',
                CASE j WHEN 1 THEN 'Draft' WHEN 2 THEN 'Submitted' WHEN 3 THEN 'Triage' WHEN 4 THEN 'SolutionIssued' ELSE 'Verifying' END,
                CASE j WHEN 1 THEN 'P1' WHEN 2 THEN 'P2' WHEN 3 THEN 'P3' ELSE 'P4' END,
                CURRENT_TIMESTAMP - (j || ' days')::INTERVAL,
                CASE WHEN j > 1 THEN CURRENT_TIMESTAMP - ((j - 1) || ' days')::INTERVAL ELSE NULL END
            );
            
            ticket_no_counter := ticket_no_counter + 1;
        END LOOP;
    END LOOP;
    
    -- 项目4: 6台设备
    FOR i IN 1..6 LOOP
        INSERT INTO devices (device_id, device_sn, device_name, project_id, device_type, model, location, status)
        VALUES (
            gen_random_uuid(),
            'DEV-PRJ002-001-' || LPAD(CAST(i AS TEXT), 3, '0'),
            'PLC控制器-' || i,
            project4_id,
            'PLC控制器',
            'Siemens S7-1200',
            '产线D段-' || CAST(i AS TEXT) || '号工位',
            'active'
        )
        RETURNING device_id INTO current_device_id;
        
        FOR j IN 1..6 LOOP
            current_engineer_id := engineers[1 + ((i * 6 + j - 1) % array_length(engineers, 1))];
            ticket_no_base := 'TK-' || ticket_date_str || '-' || LPAD(ticket_no_counter::TEXT, 4, '0');
            
            INSERT INTO tickets (
                ticket_id, ticket_no, customer_id, project_id, device_id, created_by_user_id,
                domain, step_code, step_name, symptom_title, symptom_detail,
                repro_rate, reboot_recovers, env_related,
                sw_version, plc_version, param_version, hw_version,
                status, priority, created_at, submitted_at
            )
            VALUES (
                gen_random_uuid(),
                ticket_no_base,
                customer2_id,
                project4_id,
                current_device_id,
                current_engineer_id,
                CASE (j % 5) + 1 WHEN 1 THEN 'A' WHEN 2 THEN 'B' WHEN 3 THEN 'C' WHEN 4 THEN 'D' ELSE 'E' END,
                'Step_' || (400 + j * 10),
                '步骤' || (400 + j * 10),
                '设备' || CAST(i AS TEXT) || '工单' || CAST(j AS TEXT) || '：' || CASE j 
                    WHEN 1 THEN 'PLC程序异常'
                    WHEN 2 THEN 'IO模块故障'
                    WHEN 3 THEN '通信中断'
                    WHEN 4 THEN '电源模块报警'
                    WHEN 5 THEN '扩展模块异常'
                    ELSE '程序下载失败'
                END,
                '详细描述：PLC控制器' || CAST(i AS TEXT) || '在第' || CAST(j AS TEXT) || '次运行时出现故障。',
                (j * 14)::SMALLINT,
                (j % 2 = 1),
                (j % 3 = 1),
                'V1.' || CAST(j AS TEXT) || '.0',
                'V2.' || CAST(j AS TEXT) || '.0',
                'V1.' || (j + 1) || '.0',
                'V' || CAST(j AS TEXT) || '.0',
                CASE j WHEN 1 THEN 'Draft' WHEN 2 THEN 'Submitted' WHEN 3 THEN 'Triage' WHEN 4 THEN 'SolutionIssued' WHEN 5 THEN 'Verifying' ELSE 'Closed' END,
                CASE j WHEN 1 THEN 'P1' WHEN 2 THEN 'P2' WHEN 3 THEN 'P3' ELSE 'P4' END,
                CURRENT_TIMESTAMP - (j || ' days')::INTERVAL,
                CASE WHEN j > 1 THEN CURRENT_TIMESTAMP - ((j - 1) || ' days')::INTERVAL ELSE NULL END
            );
            
            ticket_no_counter := ticket_no_counter + 1;
        END LOOP;
    END LOOP;
    
    -- 项目5: 5台设备
    FOR i IN 1..5 LOOP
        INSERT INTO devices (device_id, device_sn, device_name, project_id, device_type, model, location, status)
        VALUES (
            gen_random_uuid(),
            'DEV-PRJ002-002-' || LPAD(CAST(i AS TEXT), 3, '0'),
            '机器人-' || i,
            project5_id,
            '机器人',
            'KUKA KR 210',
            '产线E段-' || CAST(i AS TEXT) || '号工位',
            'active'
        )
        RETURNING device_id INTO current_device_id;
        
        FOR j IN 1..5 LOOP
            current_engineer_id := engineers[1 + ((i * 5 + j - 1) % array_length(engineers, 1))];
            ticket_no_base := 'TK-' || ticket_date_str || '-' || LPAD(ticket_no_counter::TEXT, 4, '0');
            
            INSERT INTO tickets (
                ticket_id, ticket_no, customer_id, project_id, device_id, created_by_user_id,
                domain, step_code, step_name, symptom_title, symptom_detail,
                repro_rate, reboot_recovers, env_related,
                sw_version, plc_version, param_version, hw_version,
                status, priority, created_at, submitted_at
            )
            VALUES (
                gen_random_uuid(),
                ticket_no_base,
                customer2_id,
                project5_id,
                current_device_id,
                current_engineer_id,
                CASE (j % 5) + 1 WHEN 1 THEN 'A' WHEN 2 THEN 'B' WHEN 3 THEN 'C' WHEN 4 THEN 'D' ELSE 'E' END,
                'Step_' || (500 + j * 10),
                '步骤' || (500 + j * 10),
                '设备' || CAST(i AS TEXT) || '工单' || CAST(j AS TEXT) || '：' || CASE j 
                    WHEN 1 THEN '机器人碰撞检测'
                    WHEN 2 THEN '伺服电机异常'
                    WHEN 3 THEN '编码器故障'
                    WHEN 4 THEN '减速器异响'
                    ELSE '控制器过热'
                END,
                '详细描述：机器人' || CAST(i AS TEXT) || '在第' || CAST(j AS TEXT) || '次运行时出现故障。',
                (j * 16)::SMALLINT,
                (j % 2 = 0),
                (j % 3 = 0),
                'V1.' || CAST(j AS TEXT) || '.0',
                'V2.' || CAST(j AS TEXT) || '.0',
                'V1.' || (j + 1) || '.0',
                'V' || CAST(j AS TEXT) || '.0',
                CASE j WHEN 1 THEN 'Draft' WHEN 2 THEN 'Submitted' WHEN 3 THEN 'Triage' WHEN 4 THEN 'SolutionIssued' ELSE 'Verifying' END,
                CASE j WHEN 1 THEN 'P1' WHEN 2 THEN 'P2' WHEN 3 THEN 'P3' ELSE 'P4' END,
                CURRENT_TIMESTAMP - (j || ' days')::INTERVAL,
                CASE WHEN j > 1 THEN CURRENT_TIMESTAMP - ((j - 1) || ' days')::INTERVAL ELSE NULL END
            );
            
            ticket_no_counter := ticket_no_counter + 1;
        END LOOP;
    END LOOP;
    
    -- 项目6: 6台设备
    FOR i IN 1..6 LOOP
        INSERT INTO devices (device_id, device_sn, device_name, project_id, device_type, model, location, status)
        VALUES (
            gen_random_uuid(),
            'DEV-PRJ003-001-' || LPAD(CAST(i AS TEXT), 3, '0'),
            'PLC控制器-' || i,
            project6_id,
            'PLC控制器',
            'Mitsubishi FX5U',
            '产线F段-' || CAST(i AS TEXT) || '号工位',
            'active'
        )
        RETURNING device_id INTO current_device_id;
        
        FOR j IN 1..6 LOOP
            current_engineer_id := engineers[1 + ((i * 6 + j - 1) % array_length(engineers, 1))];
            ticket_no_base := 'TK-' || ticket_date_str || '-' || LPAD(ticket_no_counter::TEXT, 4, '0');
            
            INSERT INTO tickets (
                ticket_id, ticket_no, customer_id, project_id, device_id, created_by_user_id,
                domain, step_code, step_name, symptom_title, symptom_detail,
                repro_rate, reboot_recovers, env_related,
                sw_version, plc_version, param_version, hw_version,
                status, priority, created_at, submitted_at
            )
            VALUES (
                gen_random_uuid(),
                ticket_no_base,
                customer3_id,
                project6_id,
                current_device_id,
                current_engineer_id,
                CASE (j % 5) + 1 WHEN 1 THEN 'A' WHEN 2 THEN 'B' WHEN 3 THEN 'C' WHEN 4 THEN 'D' ELSE 'E' END,
                'Step_' || (600 + j * 10),
                '步骤' || (600 + j * 10),
                '设备' || CAST(i AS TEXT) || '工单' || CAST(j AS TEXT) || '：' || CASE j 
                    WHEN 1 THEN 'PLC运行异常'
                    WHEN 2 THEN '内存溢出'
                    WHEN 3 THEN '通信超时'
                    WHEN 4 THEN '输入点故障'
                    WHEN 5 THEN '输出点异常'
                    ELSE '程序扫描周期过长'
                END,
                '详细描述：PLC控制器' || CAST(i AS TEXT) || '在第' || CAST(j AS TEXT) || '次运行时出现故障。',
                (j * 13)::SMALLINT,
                (j % 2 = 1),
                (j % 3 = 1),
                'V1.' || CAST(j AS TEXT) || '.0',
                'V2.' || CAST(j AS TEXT) || '.0',
                'V1.' || (j + 1) || '.0',
                'V' || CAST(j AS TEXT) || '.0',
                CASE j WHEN 1 THEN 'Draft' WHEN 2 THEN 'Submitted' WHEN 3 THEN 'Triage' WHEN 4 THEN 'SolutionIssued' WHEN 5 THEN 'Verifying' ELSE 'Closed' END,
                CASE j WHEN 1 THEN 'P1' WHEN 2 THEN 'P2' WHEN 3 THEN 'P3' ELSE 'P4' END,
                CURRENT_TIMESTAMP - (j || ' days')::INTERVAL,
                CASE WHEN j > 1 THEN CURRENT_TIMESTAMP - ((j - 1) || ' days')::INTERVAL ELSE NULL END
            );
            
            ticket_no_counter := ticket_no_counter + 1;
        END LOOP;
    END LOOP;
    
    -- 项目7: 5台设备
    FOR i IN 1..5 LOOP
        INSERT INTO devices (device_id, device_sn, device_name, project_id, device_type, model, location, status)
        VALUES (
            gen_random_uuid(),
            'DEV-PRJ003-002-' || LPAD(CAST(i AS TEXT), 3, '0'),
            '机器人-' || i,
            project7_id,
            '机器人',
            'Fanuc M-20iA',
            '产线G段-' || CAST(i AS TEXT) || '号工位',
            'active'
        )
        RETURNING device_id INTO current_device_id;
        
        FOR j IN 1..5 LOOP
            current_engineer_id := engineers[1 + ((i * 5 + j - 1) % array_length(engineers, 1))];
            ticket_no_base := 'TK-' || ticket_date_str || '-' || LPAD(ticket_no_counter::TEXT, 4, '0');
            
            INSERT INTO tickets (
                ticket_id, ticket_no, customer_id, project_id, device_id, created_by_user_id,
                domain, step_code, step_name, symptom_title, symptom_detail,
                repro_rate, reboot_recovers, env_related,
                sw_version, plc_version, param_version, hw_version,
                status, priority, created_at, submitted_at
            )
            VALUES (
                gen_random_uuid(),
                ticket_no_base,
                customer3_id,
                project7_id,
                current_device_id,
                current_engineer_id,
                CASE (j % 5) + 1 WHEN 1 THEN 'A' WHEN 2 THEN 'B' WHEN 3 THEN 'C' WHEN 4 THEN 'D' ELSE 'E' END,
                'Step_' || (700 + j * 10),
                '步骤' || (700 + j * 10),
                '设备' || CAST(i AS TEXT) || '工单' || CAST(j AS TEXT) || '：' || CASE j 
                    WHEN 1 THEN '机器人急停触发'
                    WHEN 2 THEN '工具中心点偏移'
                    WHEN 3 THEN '负载检测异常'
                    WHEN 4 THEN '速度限制报警'
                    ELSE '坐标系错误'
                END,
                '详细描述：机器人' || CAST(i AS TEXT) || '在第' || CAST(j AS TEXT) || '次运行时出现故障。',
                (j * 17)::SMALLINT,
                (j % 2 = 0),
                (j % 3 = 0),
                'V1.' || CAST(j AS TEXT) || '.0',
                'V2.' || CAST(j AS TEXT) || '.0',
                'V1.' || (j + 1) || '.0',
                'V' || CAST(j AS TEXT) || '.0',
                CASE j WHEN 1 THEN 'Draft' WHEN 2 THEN 'Submitted' WHEN 3 THEN 'Triage' WHEN 4 THEN 'SolutionIssued' ELSE 'Verifying' END,
                CASE j WHEN 1 THEN 'P1' WHEN 2 THEN 'P2' WHEN 3 THEN 'P3' ELSE 'P4' END,
                CURRENT_TIMESTAMP - (j || ' days')::INTERVAL,
                CASE WHEN j > 1 THEN CURRENT_TIMESTAMP - ((j - 1) || ' days')::INTERVAL ELSE NULL END
            );
            
            ticket_no_counter := ticket_no_counter + 1;
        END LOOP;
    END LOOP;
    
    -- 项目8: 6台设备
    FOR i IN 1..6 LOOP
        INSERT INTO devices (device_id, device_sn, device_name, project_id, device_type, model, location, status)
        VALUES (
            gen_random_uuid(),
            'DEV-PRJ003-003-' || LPAD(CAST(i AS TEXT), 3, '0'),
            '传感器-' || i,
            project8_id,
            '传感器',
            'Keyence IV2',
            '产线H段-' || CAST(i AS TEXT) || '号工位',
            'active'
        )
        RETURNING device_id INTO current_device_id;
        
        FOR j IN 1..6 LOOP
            current_engineer_id := engineers[1 + ((i * 6 + j - 1) % array_length(engineers, 1))];
            ticket_no_base := 'TK-' || ticket_date_str || '-' || LPAD(ticket_no_counter::TEXT, 4, '0');
            
            INSERT INTO tickets (
                ticket_id, ticket_no, customer_id, project_id, device_id, created_by_user_id,
                domain, step_code, step_name, symptom_title, symptom_detail,
                repro_rate, reboot_recovers, env_related,
                sw_version, plc_version, param_version, hw_version,
                status, priority, created_at, submitted_at
            )
            VALUES (
                gen_random_uuid(),
                ticket_no_base,
                customer3_id,
                project8_id,
                current_device_id,
                current_engineer_id,
                CASE (j % 5) + 1 WHEN 1 THEN 'A' WHEN 2 THEN 'B' WHEN 3 THEN 'C' WHEN 4 THEN 'D' ELSE 'E' END,
                'Step_' || (800 + j * 10),
                '步骤' || (800 + j * 10),
                '设备' || CAST(i AS TEXT) || '工单' || CAST(j AS TEXT) || '：' || CASE j 
                    WHEN 1 THEN '传感器检测失败'
                    WHEN 2 THEN '信号强度不足'
                    WHEN 3 THEN '误检测率高'
                    WHEN 4 THEN '响应延迟'
                    WHEN 5 THEN '校准数据丢失'
                    ELSE '硬件故障'
                END,
                '详细描述：传感器' || CAST(i AS TEXT) || '在第' || CAST(j AS TEXT) || '次运行时出现故障。',
                (j * 15)::SMALLINT,
                (j % 2 = 1),
                (j % 3 = 1),
                'V1.' || CAST(j AS TEXT) || '.0',
                'V2.' || CAST(j AS TEXT) || '.0',
                'V1.' || (j + 1) || '.0',
                'V' || CAST(j AS TEXT) || '.0',
                CASE j WHEN 1 THEN 'Draft' WHEN 2 THEN 'Submitted' WHEN 3 THEN 'Triage' WHEN 4 THEN 'SolutionIssued' WHEN 5 THEN 'Verifying' ELSE 'Closed' END,
                CASE j WHEN 1 THEN 'P1' WHEN 2 THEN 'P2' WHEN 3 THEN 'P3' ELSE 'P4' END,
                CURRENT_TIMESTAMP - (j || ' days')::INTERVAL,
                CASE WHEN j > 1 THEN CURRENT_TIMESTAMP - ((j - 1) || ' days')::INTERVAL ELSE NULL END
            );
            
            ticket_no_counter := ticket_no_counter + 1;
        END LOOP;
    END LOOP;
    
    -- 项目9: 5台设备
    FOR i IN 1..5 LOOP
        INSERT INTO devices (device_id, device_sn, device_name, project_id, device_type, model, location, status)
        VALUES (
            gen_random_uuid(),
            'DEV-PRJ004-001-' || LPAD(CAST(i AS TEXT), 3, '0'),
            'PLC控制器-' || i,
            project9_id,
            'PLC控制器',
            'Omron CP1E',
            '产线I段-' || CAST(i AS TEXT) || '号工位',
            'active'
        )
        RETURNING device_id INTO current_device_id;
        
        FOR j IN 1..5 LOOP
            current_engineer_id := engineers[1 + ((i * 5 + j - 1) % array_length(engineers, 1))];
            ticket_no_base := 'TK-' || ticket_date_str || '-' || LPAD(ticket_no_counter::TEXT, 4, '0');
            
            INSERT INTO tickets (
                ticket_id, ticket_no, customer_id, project_id, device_id, created_by_user_id,
                domain, step_code, step_name, symptom_title, symptom_detail,
                repro_rate, reboot_recovers, env_related,
                sw_version, plc_version, param_version, hw_version,
                status, priority, created_at, submitted_at
            )
            VALUES (
                gen_random_uuid(),
                ticket_no_base,
                customer4_id,
                project9_id,
                current_device_id,
                current_engineer_id,
                CASE (j % 5) + 1 WHEN 1 THEN 'A' WHEN 2 THEN 'B' WHEN 3 THEN 'C' WHEN 4 THEN 'D' ELSE 'E' END,
                'Step_' || (900 + j * 10),
                '步骤' || (900 + j * 10),
                '设备' || CAST(i AS TEXT) || '工单' || CAST(j AS TEXT) || '：' || CASE j 
                    WHEN 1 THEN 'PLC死机'
                    WHEN 2 THEN '程序逻辑错误'
                    WHEN 3 THEN '数据寄存器异常'
                    WHEN 4 THEN '定时器故障'
                    ELSE '计数器溢出'
                END,
                '详细描述：PLC控制器' || CAST(i AS TEXT) || '在第' || CAST(j AS TEXT) || '次运行时出现故障。',
                (j * 19)::SMALLINT,
                (j % 2 = 0),
                (j % 3 = 0),
                'V1.' || CAST(j AS TEXT) || '.0',
                'V2.' || CAST(j AS TEXT) || '.0',
                'V1.' || (j + 1) || '.0',
                'V' || CAST(j AS TEXT) || '.0',
                CASE j WHEN 1 THEN 'Draft' WHEN 2 THEN 'Submitted' WHEN 3 THEN 'Triage' WHEN 4 THEN 'SolutionIssued' ELSE 'Verifying' END,
                CASE j WHEN 1 THEN 'P1' WHEN 2 THEN 'P2' WHEN 3 THEN 'P3' ELSE 'P4' END,
                CURRENT_TIMESTAMP - (j || ' days')::INTERVAL,
                CASE WHEN j > 1 THEN CURRENT_TIMESTAMP - ((j - 1) || ' days')::INTERVAL ELSE NULL END
            );
            
            ticket_no_counter := ticket_no_counter + 1;
        END LOOP;
    END LOOP;
    
    -- 项目10: 6台设备
    FOR i IN 1..6 LOOP
        INSERT INTO devices (device_id, device_sn, device_name, project_id, device_type, model, location, status)
        VALUES (
            gen_random_uuid(),
            'DEV-PRJ004-002-' || LPAD(CAST(i AS TEXT), 3, '0'),
            '机器人-' || i,
            project10_id,
            '机器人',
            'Yaskawa Motoman',
            '产线J段-' || CAST(i AS TEXT) || '号工位',
            'active'
        )
        RETURNING device_id INTO current_device_id;
        
        FOR j IN 1..6 LOOP
            current_engineer_id := engineers[1 + ((i * 6 + j - 1) % array_length(engineers, 1))];
            ticket_no_base := 'TK-' || ticket_date_str || '-' || LPAD(ticket_no_counter::TEXT, 4, '0');
            
            INSERT INTO tickets (
                ticket_id, ticket_no, customer_id, project_id, device_id, created_by_user_id,
                domain, step_code, step_name, symptom_title, symptom_detail,
                repro_rate, reboot_recovers, env_related,
                sw_version, plc_version, param_version, hw_version,
                status, priority, created_at, submitted_at
            )
            VALUES (
                gen_random_uuid(),
                ticket_no_base,
                customer4_id,
                project10_id,
                current_device_id,
                current_engineer_id,
                CASE (j % 5) + 1 WHEN 1 THEN 'A' WHEN 2 THEN 'B' WHEN 3 THEN 'C' WHEN 4 THEN 'D' ELSE 'E' END,
                'Step_' || (1000 + j * 10),
                '步骤' || (1000 + j * 10),
                '设备' || CAST(i AS TEXT) || '工单' || CAST(j AS TEXT) || '：' || CASE j 
                    WHEN 1 THEN '机器人程序异常'
                    WHEN 2 THEN '示教器通信失败'
                    WHEN 3 THEN '安全回路断开'
                    WHEN 4 THEN '抱闸故障'
                    WHEN 5 THEN '编码器反馈异常'
                    ELSE '伺服驱动器报警'
                END,
                '详细描述：机器人' || CAST(i AS TEXT) || '在第' || CAST(j AS TEXT) || '次运行时出现故障。',
                (j * 11)::SMALLINT,
                (j % 2 = 1),
                (j % 3 = 1),
                'V1.' || CAST(j AS TEXT) || '.0',
                'V2.' || CAST(j AS TEXT) || '.0',
                'V1.' || (j + 1) || '.0',
                'V' || CAST(j AS TEXT) || '.0',
                CASE j WHEN 1 THEN 'Draft' WHEN 2 THEN 'Submitted' WHEN 3 THEN 'Triage' WHEN 4 THEN 'SolutionIssued' WHEN 5 THEN 'Verifying' ELSE 'Closed' END,
                CASE j WHEN 1 THEN 'P1' WHEN 2 THEN 'P2' WHEN 3 THEN 'P3' ELSE 'P4' END,
                CURRENT_TIMESTAMP - (j || ' days')::INTERVAL,
                CASE WHEN j > 1 THEN CURRENT_TIMESTAMP - ((j - 1) || ' days')::INTERVAL ELSE NULL END
            );
            
            ticket_no_counter := ticket_no_counter + 1;
        END LOOP;
    END LOOP;
    
    RAISE NOTICE '测试数据创建完成！';
    RAISE NOTICE '客户数: 4';
    RAISE NOTICE '项目数: 10';
    RAISE NOTICE '设备数: 55 (5+6+5+6+5+6+5+6+5+6)';
    RAISE NOTICE '工单数: %', ticket_no_counter - 1;
END $$;

