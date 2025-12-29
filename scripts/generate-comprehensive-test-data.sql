-- 生成全面的测试数据用于统计分析
-- 基于非标自动化行业特点：汽车电子测试、锂电池EOL测试、白色家电测试、电动工具、电机驱动器、BMS测试、控制板自动化装配

DO $$
DECLARE
    -- 客户变量
    customer_ids UUID[] := ARRAY[]::UUID[];
    customer_id UUID;
    
    -- 项目变量
    project_ids UUID[] := ARRAY[]::UUID[];
    project_id UUID;
    
    -- 设备变量
    device_ids UUID[] := ARRAY[]::UUID[];
    device_id UUID;
    
    -- 工单变量
    ticket_id UUID;
    ticket_no_counter INT := 1;
    ticket_date DATE;
    ticket_date_str VARCHAR(8);
    
    -- 工程师ID（从现有用户中获取）
    engineer_ids UUID[];
    engineer_id UUID;
    
    -- 状态分布：Closed(60%), Verifying(15%), SolutionIssued(10%), Triage(10%), Submitted(5%)
    status_weights INT[] := ARRAY[60, 15, 10, 10, 5];
    statuses TEXT[] := ARRAY['Closed', 'Verifying', 'SolutionIssued', 'Triage', 'Submitted'];
    
    -- 问题域分布：A(20%), B(25%), C(20%), D(25%), E(10%)
    domain_weights INT[] := ARRAY[20, 25, 20, 25, 10];
    domains CHAR[] := ARRAY['A', 'B', 'C', 'D', 'E'];
    
    -- 优先级分布：P1(10%), P2(40%), P3(40%), P4(10%)
    priority_weights INT[] := ARRAY[10, 40, 40, 10];
    priorities TEXT[] := ARRAY['P1', 'P2', 'P3', 'P4'];
    
    -- 行业类型
    industry_types TEXT[] := ARRAY['汽车电子', '锂电池', '白色家电', '电动工具', '电机驱动', 'BMS测试', '控制板装配'];
    device_types TEXT[] := ARRAY['FCT测试设备', 'EOL测试设备', '自动化产线', '装配设备', '检测设备'];
    
    -- 步骤代码和名称
    step_codes TEXT[] := ARRAY['Step_100', 'Step_110', 'Step_120', 'Step_130', 'Step_140', 'Step_150', 'Step_200', 'Step_210', 'Step_220'];
    step_names TEXT[] := ARRAY['上料', '定位', '测试', '判定', '下料', '复位', '通信', '数据采集', '结果输出'];
    
    -- 症状标题模板
    symptom_titles TEXT[] := ARRAY[
        '定位精度偏差超出要求',
        '传感器信号异常',
        'PLC程序执行超时',
        '测试数据判定错误',
        '系统偶发故障',
        '探针接触不良',
        '电压采集精度不足',
        '通信协议不匹配',
        '测试结果重复性差',
        '环境因素影响测试'
    ];
    
    -- 循环变量
    i INT;
    j INT;
    k INT;
    days_ago INT;
    created_at TIMESTAMPTZ;
    closed_at TIMESTAMPTZ;
    status TEXT;
    domain CHAR;
    priority TEXT;
    random_weight INT;
    cumulative_weight INT;
    total_weight INT;
    
    -- 工单生成时的临时变量
    selected_device_id UUID;
    selected_engineer_id UUID;
    selected_project_id UUID;
    selected_customer_id UUID;
    
    -- 时间范围：过去90天
    start_date DATE := CURRENT_DATE - INTERVAL '90 days';
    end_date DATE := CURRENT_DATE;
    
BEGIN
    RAISE NOTICE '开始生成测试数据...';
    
    -- 1. 获取或创建客户
    SELECT ARRAY_AGG(customers.customer_id) INTO customer_ids FROM customers LIMIT 10;
    
    IF array_length(customer_ids, 1) IS NULL OR array_length(customer_ids, 1) < 5 THEN
        RAISE NOTICE '创建新客户...';
        FOR i IN 1..5 LOOP
            INSERT INTO customers (customer_id, customer_name, customer_code, industry_type, contact_person, contact_phone)
            VALUES (
                gen_random_uuid(),
                CASE i
                    WHEN 1 THEN '华为技术有限公司'
                    WHEN 2 THEN '比亚迪汽车工业有限公司'
                    WHEN 3 THEN '宁德时代新能源科技股份有限公司'
                    WHEN 4 THEN '三一重工股份有限公司'
                    WHEN 5 THEN '美的集团股份有限公司'
                END,
                'CUST' || LPAD(i::TEXT, 3, '0'),
                industry_types[(i - 1) % array_length(industry_types, 1) + 1],
                '联系人' || i,
                '13800138' || LPAD(i::TEXT, 3, '0')
            )
            RETURNING customer_id INTO customer_id;
            customer_ids := array_append(customer_ids, customer_id);
        END LOOP;
    END IF;
    
    RAISE NOTICE '客户数量: %', array_length(customer_ids, 1);
    
    -- 2. 获取或创建项目（每个客户2-3个项目）
    SELECT ARRAY_AGG(projects.project_id) INTO project_ids FROM projects LIMIT 20;
    
    IF array_length(project_ids, 1) IS NULL OR array_length(project_ids, 1) < 10 THEN
        RAISE NOTICE '创建新项目...';
        FOR i IN 1..array_length(customer_ids, 1) LOOP
            FOR j IN 1..(2 + (i % 2)) LOOP  -- 每个客户2-3个项目
                INSERT INTO projects (
                    project_id, project_no, project_name, customer_id, customer_name,
                    device_type, industry_type, sales_amount, quantity,
                    order_date, required_delivery_date, project_status
                )
                VALUES (
                    gen_random_uuid(),
                    'PRJ' || LPAD(i::TEXT, 3, '0') || '-' || LPAD(j::TEXT, 3, '0'),
                    '项目' || i || '-' || j || '自动化产线',
                    customer_ids[i],
                    (SELECT customer_name FROM customers WHERE customer_id = customer_ids[i]),
                    device_types[(i + j - 1) % array_length(device_types, 1) + 1],
                    industry_types[(i - 1) % array_length(industry_types, 1) + 1],
                    5000000.00 + (random() * 10000000),
                    20 + (random() * 80)::INT,
                    start_date - INTERVAL '30 days',
                    end_date + INTERVAL '60 days',
                    '进行中'
                )
                RETURNING project_id INTO project_id;
                project_ids := array_append(project_ids, project_id);
            END LOOP;
        END LOOP;
    END IF;
    
    RAISE NOTICE '项目数量: %', array_length(project_ids, 1);
    
    -- 3. 获取或创建设备（每个项目3-5台设备）
    SELECT ARRAY_AGG(devices.device_id) INTO device_ids FROM devices LIMIT 100;
    
    IF array_length(device_ids, 1) IS NULL OR array_length(device_ids, 1) < 30 THEN
        RAISE NOTICE '创建新设备...';
        FOR i IN 1..array_length(project_ids, 1) LOOP
            FOR j IN 1..(3 + (i % 3)) LOOP  -- 每个项目3-5台设备
                INSERT INTO devices (
                    device_id, device_sn, device_name, project_id, customer_id,
                    device_type, station_name, installation_date, device_status
                )
                VALUES (
                    gen_random_uuid(),
                    'DEV' || LPAD(i::TEXT, 3, '0') || '-' || LPAD(j::TEXT, 2, '0'),
                    '设备' || i || '-' || j,
                    project_ids[i],
                    (SELECT customer_id FROM projects WHERE project_id = project_ids[i]),
                    device_types[(i + j - 1) % array_length(device_types, 1) + 1],
                    '工位' || j,
                    start_date - INTERVAL '60 days',
                    '运行中'
                )
                RETURNING device_id INTO device_id;
                device_ids := array_append(device_ids, device_id);
            END LOOP;
        END LOOP;
    END IF;
    
    RAISE NOTICE '设备数量: %', array_length(device_ids, 1);
    
    -- 4. 获取工程师ID
    SELECT ARRAY_AGG(id) INTO engineer_ids 
    FROM users 
    WHERE role IN ('FieldEngineer', 'SeniorEngineer', 'CS', 'Engineer')
    LIMIT 10;
    
    -- 如果还是没有找到，尝试获取任何用户
    IF array_length(engineer_ids, 1) IS NULL THEN
        SELECT ARRAY_AGG(id) INTO engineer_ids FROM users LIMIT 10;
    END IF;
    
    -- 如果还是没有，创建测试用户
    IF array_length(engineer_ids, 1) IS NULL THEN
        RAISE NOTICE '创建测试工程师用户...';
        FOR i IN 1..3 LOOP
            INSERT INTO users (id, username, name, role, password_hash)
            VALUES (
                gen_random_uuid(),
                'test_engineer_' || i,
                '测试工程师' || i,
                'FieldEngineer',
                '$2a$11$dummyhash'
            )
            RETURNING id INTO selected_engineer_id;
            engineer_ids := array_append(engineer_ids, selected_engineer_id);
        END LOOP;
    END IF;
    
    RAISE NOTICE '工程师数量: %', array_length(engineer_ids, 1);
    
    -- 5. 生成工单（过去90天，每天5-15个工单）
    RAISE NOTICE '开始生成工单...';
    
    FOR days_ago IN 0..89 LOOP
        ticket_date := end_date - (days_ago || ' days')::INTERVAL;
        ticket_date_str := TO_CHAR(ticket_date, 'YYYYMMDD');
        
        -- 每天生成5-15个工单
        FOR i IN 1..(5 + (random() * 10)::INT) LOOP
            ticket_no_counter := i;
            
            -- 随机选择设备
            selected_device_id := device_ids[1 + (random() * (array_length(device_ids, 1) - 1))::INT];
            
            -- 随机选择工程师
            selected_engineer_id := engineer_ids[1 + (random() * (array_length(engineer_ids, 1) - 1))::INT];
            
            -- 获取项目和客户ID（使用表别名避免冲突）
            SELECT dev.project_id, proj.customer_id INTO selected_project_id, selected_customer_id
            FROM devices dev
            JOIN projects proj ON dev.project_id = proj.project_id
            WHERE dev.device_id = selected_device_id
            LIMIT 1;
            
            -- 根据权重随机选择状态
            total_weight := 0;
            FOREACH random_weight IN ARRAY status_weights LOOP
                total_weight := total_weight + random_weight;
            END LOOP;
            
            random_weight := (random() * total_weight)::INT;
            cumulative_weight := 0;
            status := statuses[1];
            
            FOR j IN 1..array_length(statuses, 1) LOOP
                cumulative_weight := cumulative_weight + status_weights[j];
                IF random_weight < cumulative_weight THEN
                    status := statuses[j];
                    EXIT;
                END IF;
            END LOOP;
            
            -- 根据权重随机选择问题域
            total_weight := 0;
            FOREACH random_weight IN ARRAY domain_weights LOOP
                total_weight := total_weight + random_weight;
            END LOOP;
            
            random_weight := (random() * total_weight)::INT;
            cumulative_weight := 0;
            domain := domains[1];
            
            FOR j IN 1..array_length(domains, 1) LOOP
                cumulative_weight := cumulative_weight + domain_weights[j];
                IF random_weight < cumulative_weight THEN
                    domain := domains[j];
                    EXIT;
                END IF;
            END LOOP;
            
            -- 根据权重随机选择优先级
            total_weight := 0;
            FOREACH random_weight IN ARRAY priority_weights LOOP
                total_weight := total_weight + random_weight;
            END LOOP;
            
            random_weight := (random() * total_weight)::INT;
            cumulative_weight := 0;
            priority := priorities[1];
            
            FOR j IN 1..array_length(priorities, 1) LOOP
                cumulative_weight := cumulative_weight + priority_weights[j];
                IF random_weight < cumulative_weight THEN
                    priority := priorities[j];
                    EXIT;
                END IF;
            END LOOP;
            
            -- 生成创建时间（当天随机时间）
            created_at := ticket_date + (random() * INTERVAL '1 day');
            
            -- 如果是已关闭状态，生成关闭时间（创建后1-30天）
            IF status = 'Closed' THEN
                closed_at := created_at + ((1 + random() * 29) || ' days')::INTERVAL;
            ELSE
                closed_at := NULL;
            END IF;
            
            -- 生成工单
            INSERT INTO tickets (
                ticket_id, ticket_no, customer_id, project_id,
                device_id, created_by_user_id,
                domain, step_code, step_name, symptom_title, symptom_detail,
                sw_version, plc_version, param_version, hw_version,
                repro_rate, reboot_recovers, env_related,
                facts_json, actions_taken, actions_taken_note,
                confirmed_as_fact, confirmed_at,
                status, priority,
                created_at, updated_at, closed_at
            )
            VALUES (
                gen_random_uuid(),
                'TKT' || ticket_date_str || LPAD(ticket_no_counter::TEXT, 4, '0'),
                selected_customer_id,
                selected_project_id,
                selected_device_id,
                selected_engineer_id,
                domain,
                step_codes[1 + (random() * (array_length(step_codes, 1) - 1))::INT],
                step_names[1 + (random() * (array_length(step_names, 1) - 1))::INT],
                symptom_titles[1 + (random() * (array_length(symptom_titles, 1) - 1))::INT],
                '详细描述：这是基于非标自动化行业特点生成的测试工单。问题发生在' || 
                step_names[1 + (random() * (array_length(step_names, 1) - 1))::INT] || 
                '步骤，需要进一步分析和处理。',
                'V' || (2 + (random() * 2)::INT) || '.' || (random() * 9)::INT || '.' || (random() * 9)::INT,
                'V' || (1 + (random() * 2)::INT) || '.' || (random() * 9)::INT || '.' || (random() * 9)::INT,
                'P' || TO_CHAR(ticket_date, 'YYYYMMDD'),
                'H' || (random() * 5 + 1)::INT || '.0',
                (random() * 100)::INT,
                (random() > 0.3),
                (random() > 0.5),
                jsonb_build_object(
                    'domain', domain,
                    'step', step_codes[1 + (random() * (array_length(step_codes, 1) - 1))::INT],
                    'repro_rate', (random() * 100)::INT
                ),
                ARRAY['CHECK', 'REBOOT', 'ADJUST'],
                '已执行检查、重启和调整操作',
                true,
                created_at + INTERVAL '1 hour',
                status,
                priority,
                created_at,
                COALESCE(closed_at, created_at),
                closed_at
            );
            
            ticket_no_counter := ticket_no_counter + 1;
        END LOOP;
        
        -- 每10天输出一次进度
        IF days_ago % 10 = 0 THEN
            RAISE NOTICE '已生成到 % 天的数据', days_ago;
        END IF;
    END LOOP;
    
    RAISE NOTICE '测试数据生成完成！';
    RAISE NOTICE '客户数: %', array_length(customer_ids, 1);
    RAISE NOTICE '项目数: %', array_length(project_ids, 1);
    RAISE NOTICE '设备数: %', array_length(device_ids, 1);
    RAISE NOTICE '工单数: 约 %', (90 * 10);
    
END $$;

-- 验证数据
SELECT 
    '客户数' as type, COUNT(*)::TEXT as count FROM customers
UNION ALL
SELECT '项目数', COUNT(*)::TEXT FROM projects
UNION ALL
SELECT '设备数', COUNT(*)::TEXT FROM devices
UNION ALL
SELECT '工单总数', COUNT(*)::TEXT FROM tickets
UNION ALL
SELECT '已关闭工单', COUNT(*)::TEXT FROM tickets WHERE status = 'Closed'
UNION ALL
SELECT '开放工单', COUNT(*)::TEXT FROM tickets WHERE status != 'Closed' AND status != 'Draft';

