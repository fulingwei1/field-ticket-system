-- 创建必要的表并插入20个非标自动化行业真实案例
-- 执行顺序：先创建表（如果不存在），再插入数据

-- 1. 创建customers表（如果不存在）
CREATE TABLE IF NOT EXISTS customers (
    customer_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    customer_name VARCHAR(200) NOT NULL,
    customer_code VARCHAR(50),
    industry_type VARCHAR(50),
    contact_person VARCHAR(100),
    contact_phone VARCHAR(20),
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

-- 2. 创建devices表（如果不存在）
CREATE TABLE IF NOT EXISTS devices (
    device_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    device_sn VARCHAR(100) UNIQUE,
    device_name VARCHAR(200) NOT NULL,
    project_id UUID,
    device_type VARCHAR(50),
    model VARCHAR(100),
    location VARCHAR(200),
    status VARCHAR(50) DEFAULT 'active',
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

-- 3. 确保projects表存在（已在迁移脚本中创建）
-- 这里不做处理，假设已存在

-- 4. 插入测试数据并创建工单
DO $$
DECLARE
    test_user_id UUID;
    test_customer_id UUID;
    test_project_id UUID;
    test_device_id UUID;
    ticket_no_base VARCHAR(20);
    case_date DATE;
    ticket_counter INT := 1;
BEGIN
    -- 获取第一个用户作为创建者
    SELECT id INTO test_user_id FROM users LIMIT 1;
    
    IF test_user_id IS NULL THEN
        RAISE EXCEPTION 'No users found. Please create users first.';
    END IF;
    
    -- 创建或获取测试客户
    INSERT INTO customers (customer_id, customer_name, industry_type, created_at, updated_at)
    VALUES (gen_random_uuid(), '汽车电子测试客户', '汽车电子', NOW(), NOW())
    ON CONFLICT DO NOTHING
    RETURNING customer_id INTO test_customer_id;
    
    IF test_customer_id IS NULL THEN
        SELECT customer_id INTO test_customer_id FROM customers WHERE customer_name = '汽车电子测试客户' LIMIT 1;
    END IF;
    
    -- 创建或获取测试项目
    INSERT INTO projects (project_id, project_no, project_name, customer_id, customer_name, industry_type, project_status, created_at, updated_at)
    VALUES (gen_random_uuid(), 'PRJ-2024-001', '汽车电子FCT测试设备项目', test_customer_id, '汽车电子测试客户', '汽车电子', '进行中', NOW(), NOW())
    ON CONFLICT (project_no) DO NOTHING
    RETURNING project_id INTO test_project_id;
    
    IF test_project_id IS NULL THEN
        SELECT project_id INTO test_project_id FROM projects WHERE project_no = 'PRJ-2024-001' LIMIT 1;
    END IF;
    
    -- 创建或获取测试设备
    INSERT INTO devices (device_id, device_sn, device_name, project_id, device_type, status, created_at, updated_at)
    VALUES (gen_random_uuid(), 'DEV-2024-001', '汽车电子FCT测试设备', test_project_id, 'FCT测试设备', 'active', NOW(), NOW())
    ON CONFLICT (device_sn) DO NOTHING
    RETURNING device_id INTO test_device_id;
    
    IF test_device_id IS NULL THEN
        SELECT device_id INTO test_device_id FROM devices WHERE device_sn = 'DEV-2024-001' LIMIT 1;
    END IF;
    
    -- 设置基础日期
    case_date := CURRENT_DATE;
    ticket_no_base := 'TK-' || TO_CHAR(case_date, 'YYYYMMDD');
    
    -- ========== 案例1：汽车电子测试设备 - 定位精度偏差 ==========
    INSERT INTO tickets (
        ticket_id, ticket_no, customer_id, project_id, device_id, created_by_user_id,
        domain, step_code, step_name, symptom_title, symptom_detail,
        repro_rate, reboot_recovers, env_related,
        sw_version, plc_version, param_version, hw_version,
        facts_json, actions_taken, actions_taken_note,
        confirmed_as_fact, status, priority,
        created_at, updated_at
    ) VALUES (
        gen_random_uuid(), ticket_no_base || '-' || LPAD(ticket_counter::TEXT, 3, '0'),
        test_customer_id, test_project_id, test_device_id, test_user_id,
        'A', 'Step_120', '产品定位',
        '产品定位后X轴位置偏差0.5mm，超出±0.2mm精度要求',
        '在汽车电子控制板FCT测试中，产品通过定位机构移动到测试工位后，使用激光位移传感器检测X轴位置，发现实际位置与理论位置偏差0.5mm，超出工艺要求的±0.2mm精度范围。该偏差导致测试探针无法准确接触测试点，造成测试失败率约15%。',
        85, false, true,
        'V2.3.1', 'V1.8.2', 'P20241215', 'H3.0',
        '{"mechanical":{"action_completed":"YES","position_accuracy":"NO","mechanical_wear":"UNKNOWN","lubrication_status":"YES"},"electrical":{"sensor_signal":"YES","motor_drive":"YES","encoder_feedback":"YES"},"environmental":{"temperature":28,"humidity":65,"vibration":"LOW"}}'::jsonb,
        ARRAY['REBOOT', 'CHECK_MECHANICAL', 'CALIBRATE_SENSOR'],
        '已检查定位机构机械连接，未发现明显松动；重新校准激光位移传感器，偏差仍然存在。',
        true, 'Submitted', 'P2',
        case_date - INTERVAL '20 days', case_date - INTERVAL '20 days'
    );
    ticket_counter := ticket_counter + 1;
    
    -- ========== 案例2：锂电池EOL测试设备 - 充放电测试电流波动 ==========
    INSERT INTO tickets (
        ticket_id, ticket_no, customer_id, project_id, device_id, created_by_user_id,
        domain, step_code, step_name, symptom_title, symptom_detail,
        repro_rate, reboot_recovers, env_related,
        sw_version, plc_version, param_version, hw_version,
        facts_json, actions_taken, actions_taken_note,
        confirmed_as_fact, status, priority,
        created_at, updated_at
    ) VALUES (
        gen_random_uuid(), ticket_no_base || '-' || LPAD(ticket_counter::TEXT, 3, '0'),
        test_customer_id, test_project_id, test_device_id, test_user_id,
        'B', 'Step_205', '充放电测试',
        '充放电测试过程中电流波动±5%，超出±2%精度要求',
        '在锂电池EOL测试中，充放电测试阶段电流设定为10A，但实际电流在9.5A-10.5A之间波动，波动幅度达到±5%，超出工艺要求的±2%精度范围。该问题导致电池容量测试结果不准确，测试数据离散度大，影响产品合格判定。',
        100, false, false,
        'V3.1.5', 'V2.0.1', 'P20241220', 'H4.2',
        '{"electrical":{"power_supply_stable":"NO","current_sensor_ok":"YES","load_connection":"YES","grounding":"YES"},"plc":{"pid_control":"YES","control_loop":"YES","sampling_rate":100}}'::jsonb,
        ARRAY['CHECK_POWER', 'CHECK_SENSOR', 'ADJUST_PID'],
        '已检查充放电电源模块，电压稳定；检查电流传感器，读数正常；尝试调整PID控制参数，效果不明显。',
        true, 'Submitted', 'P2',
        case_date - INTERVAL '19 days', case_date - INTERVAL '19 days'
    );
    ticket_counter := ticket_counter + 1;
    
    -- ========== 案例3：白色家电测试设备 - 温度测试超时 ==========
    INSERT INTO tickets (
        ticket_id, ticket_no, customer_id, project_id, device_id, created_by_user_id,
        domain, step_code, step_name, symptom_title, symptom_detail,
        repro_rate, reboot_recovers, env_related,
        sw_version, plc_version, param_version, hw_version,
        facts_json, actions_taken, actions_taken_note,
        confirmed_as_fact, status, priority,
        created_at, updated_at
    ) VALUES (
        gen_random_uuid(), ticket_no_base || '-' || LPAD(ticket_counter::TEXT, 3, '0'),
        test_customer_id, test_project_id, test_device_id, test_user_id,
        'C', 'Step_310', '温度循环测试',
        '温度循环测试在达到目标温度后等待超时，测试无法继续',
        '在白色家电控制板温度循环测试中，程序设定目标温度为-10℃，当实际温度达到-10℃后，程序应该继续执行下一步，但程序一直等待，超过设定的30秒超时时间后报错。检查程序逻辑，发现温度判定条件使用了严格等于（==），而实际温度在-9.8℃到-10.2℃之间波动，导致条件永远不满足。',
        90, true, true,
        'V1.9.3', 'V1.5.7', 'P20241120', 'H2.8',
        '{"plc":{"program_logic":"NO","condition_check":"NO","timeout_setting":"YES","temperature_control":"YES"},"electrical":{"temperature_sensor":"YES","heater_control":"YES","cooler_control":"YES"}}'::jsonb,
        ARRAY['REBOOT', 'CHECK_PROGRAM', 'MODIFY_CONDITION'],
        '已重启系统，问题暂时消失但会复现；检查程序发现温度判定逻辑有问题，需要修改为范围判定。',
        true, 'Submitted', 'P2',
        case_date - INTERVAL '18 days', case_date - INTERVAL '18 days'
    );
    ticket_counter := ticket_counter + 1;
    
    -- ========== 案例4：电动工具测试设备 - 扭矩测试数据异常 ==========
    INSERT INTO tickets (
        ticket_id, ticket_no, customer_id, project_id, device_id, created_by_user_id,
        domain, step_code, step_name, symptom_title, symptom_detail,
        repro_rate, reboot_recovers, env_related,
        sw_version, plc_version, param_version, hw_version,
        facts_json, actions_taken, actions_taken_note,
        confirmed_as_fact, status, priority,
        created_at, updated_at
    ) VALUES (
        gen_random_uuid(), ticket_no_base || '-' || LPAD(ticket_counter::TEXT, 3, '0'),
        test_customer_id, test_project_id, test_device_id, test_user_id,
        'D', 'Step_415', '扭矩测试',
        '扭矩测试结果判定异常，合格产品被判为不合格',
        '在电动工具电机扭矩测试中，测试程序设定扭矩上限为50N·m，下限为45N·m。实际测试中，产品扭矩为49.8N·m，应该在合格范围内，但系统判定为不合格。检查判定逻辑，发现程序使用了"大于等于"（>=）和"小于等于"（<=）的比较，但实际代码中写成了"大于"（>）和"小于"（<），导致边界值被误判。',
        100, false, false,
        'V2.5.2', 'V1.9.0', 'P20241210', 'H3.5',
        '{"test":{"test_result":"NO","judgment_logic":"NO","boundary_value":"YES","test_data":"YES"},"plc":{"comparison_operator":"NO","test_sequence":"YES"}}'::jsonb,
        ARRAY['CHECK_LOGIC', 'VERIFY_DATA', 'RETEST'],
        '已检查判定逻辑，发现比较运算符错误；验证测试数据，数据采集正常；重新测试边界值产品，确认问题。',
        true, 'Submitted', 'P1',
        case_date - INTERVAL '17 days', case_date - INTERVAL '17 days'
    );
    ticket_counter := ticket_counter + 1;
    
    -- ========== 案例5：电机驱动器测试设备 - 通信超时 ==========
    INSERT INTO tickets (
        ticket_id, ticket_no, customer_id, project_id, device_id, created_by_user_id,
        domain, step_code, step_name, symptom_title, symptom_detail,
        repro_rate, reboot_recovers, env_related,
        sw_version, plc_version, param_version, hw_version,
        facts_json, actions_taken, actions_taken_note,
        confirmed_as_fact, status, priority,
        created_at, updated_at
    ) VALUES (
        gen_random_uuid(), ticket_no_base || '-' || LPAD(ticket_counter::TEXT, 3, '0'),
        test_customer_id, test_project_id, test_device_id, test_user_id,
        'E', 'Step_520', '通信测试',
        '与电机驱动器通信时偶发超时，重启后恢复正常',
        '在电机驱动器FCT测试中，上位机通过CAN总线与驱动器通信，偶发出现通信超时错误。超时时间设定为500ms，但实际通信延迟可能达到800ms以上。该问题出现频率不高，约5%的测试中会出现，重启系统或重新上电后通常能恢复正常。检查CAN总线终端电阻和线缆连接，未发现明显问题。',
        5, true, true,
        'V3.2.1', 'V2.1.3', 'P20241225', 'H4.5',
        '{"system":{"communication_timeout":"YES","bus_load":"HIGH","error_recovery":"YES"},"environmental":{"temperature":35,"electromagnetic":"MEDIUM","cable_length":3.5},"electrical":{"can_termination":"YES","signal_quality":"MEDIUM"}}'::jsonb,
        ARRAY['REBOOT', 'CHECK_CABLE', 'CHECK_TERMINATION'],
        '已重启系统，问题暂时消失；检查CAN总线线缆和终端电阻，连接正常；怀疑是总线负载或电磁干扰导致。',
        true, 'Submitted', 'P3',
        case_date - INTERVAL '16 days', case_date - INTERVAL '16 days'
    );
    ticket_counter := ticket_counter + 1;
    
    -- ========== 案例6：BMS测试设备 - 电压采集精度不足 ==========
    INSERT INTO tickets (
        ticket_id, ticket_no, customer_id, project_id, device_id, created_by_user_id,
        domain, step_code, step_name, symptom_title, symptom_detail,
        repro_rate, reboot_recovers, env_related,
        sw_version, plc_version, param_version, hw_version,
        facts_json, actions_taken, actions_taken_note,
        confirmed_as_fact, status, priority,
        created_at, updated_at
    ) VALUES (
        gen_random_uuid(), ticket_no_base || '-' || LPAD(ticket_counter::TEXT, 3, '0'),
        test_customer_id, test_project_id, test_device_id, test_user_id,
        'B', 'Step_125', '电压采集',
        '电池包电压采集精度不足，误差达到±50mV，超出±10mV要求',
        '在BMS（电池管理系统）测试设备中，需要对电池包各单体电压进行采集，精度要求为±10mV。实际测试中发现，采集到的电压值与标准电压表对比，误差达到±50mV，超出精度要求。该问题影响电池包SOC（电量状态）计算的准确性，可能导致电池包被误判。',
        100, false, true,
        'V2.8.3', 'V1.7.5', 'P20241218', 'H3.8',
        '{"electrical":{"adc_accuracy":"NO","reference_voltage":"YES","signal_conditioning":"YES","noise_level":"HIGH"},"test":{"calibration":"YES","measurement_range":"YES"}}'::jsonb,
        ARRAY['CALIBRATE_ADC', 'CHECK_REFERENCE', 'CHECK_NOISE'],
        '已重新校准ADC，但精度仍然不足；检查参考电压源，电压稳定；发现信号调理电路存在噪声干扰。',
        true, 'Submitted', 'P2',
        case_date - INTERVAL '15 days', case_date - INTERVAL '15 days'
    );
    ticket_counter := ticket_counter + 1;
    
    -- ========== 案例7：汽车电子测试设备 - 探针接触不良 ==========
    INSERT INTO tickets (
        ticket_id, ticket_no, customer_id, project_id, device_id, created_by_user_id,
        domain, step_code, step_name, symptom_title, symptom_detail,
        repro_rate, reboot_recovers, env_related,
        sw_version, plc_version, param_version, hw_version,
        facts_json, actions_taken, actions_taken_note,
        confirmed_as_fact, status, priority,
        created_at, updated_at
    ) VALUES (
        gen_random_uuid(), ticket_no_base || '-' || LPAD(ticket_counter::TEXT, 3, '0'),
        test_customer_id, test_project_id, test_device_id, test_user_id,
        'A', 'Step_220', '测试探针接触',
        '测试探针与PCB测试点接触不良，导致测试信号不稳定',
        '在汽车电子控制板FCT测试中，使用弹簧探针接触PCB测试点进行信号测试。测试过程中发现，部分测试点的接触电阻不稳定，在10Ω到500Ω之间波动，导致测试信号不稳定，测试结果不可靠。检查探针，发现探针头部有轻微磨损，弹簧力不足。',
        60, false, false,
        'V2.4.0', 'V1.8.5', 'P20241212', 'H3.2',
        '{"mechanical":{"probe_wear":"YES","spring_force":"NO","contact_pressure":"NO","alignment":"YES"},"electrical":{"contact_resistance":"NO","signal_stability":"NO"}}'::jsonb,
        ARRAY['CHECK_PROBE', 'CLEAN_PROBE', 'ADJUST_PRESSURE'],
        '已检查探针，发现头部磨损；清洁探针后问题略有改善但仍存在；需要更换探针或调整接触压力。',
        true, 'Submitted', 'P2',
        case_date - INTERVAL '14 days', case_date - INTERVAL '14 days'
    );
    ticket_counter := ticket_counter + 1;
    
    -- ========== 案例8：锂电池EOL测试设备 - 内阻测试重复性差 ==========
    INSERT INTO tickets (
        ticket_id, ticket_no, customer_id, project_id, device_id, created_by_user_id,
        domain, step_code, step_name, symptom_title, symptom_detail,
        repro_rate, reboot_recovers, env_related,
        sw_version, plc_version, param_version, hw_version,
        facts_json, actions_taken, actions_taken_note,
        confirmed_as_fact, status, priority,
        created_at, updated_at
    ) VALUES (
        gen_random_uuid(), ticket_no_base || '-' || LPAD(ticket_counter::TEXT, 3, '0'),
        test_customer_id, test_project_id, test_device_id, test_user_id,
        'D', 'Step_305', '内阻测试',
        '电池内阻测试重复性差，同一电池多次测试结果差异超过5%',
        '在锂电池EOL测试中，需要对电池内阻进行测试，要求重复性误差小于2%。实际测试中发现，同一电池连续测试3次，内阻值分别为12.5mΩ、13.2mΩ、12.8mΩ，差异达到5.6%，超出要求。检查测试程序，发现测试前没有充分稳定时间，电池状态不一致导致测试结果波动。',
        80, false, true,
        'V3.0.8', 'V2.0.5', 'P20241222', 'H4.3',
        '{"test":{"repeatability":"NO","stabilization_time":"NO","test_sequence":"YES","measurement_method":"YES"},"plc":{"timing_control":"NO","test_procedure":"YES"}}'::jsonb,
        ARRAY['CHECK_SEQUENCE', 'INCREASE_STABILIZATION', 'RETEST'],
        '已检查测试序列，发现稳定时间不足；增加稳定时间后，重复性有所改善但仍需优化。',
        true, 'Submitted', 'P2',
        case_date - INTERVAL '13 days', case_date - INTERVAL '13 days'
    );
    ticket_counter := ticket_counter + 1;
    
    -- ========== 案例9：白色家电测试设备 - 按键响应延迟 ==========
    INSERT INTO tickets (
        ticket_id, ticket_no, customer_id, project_id, device_id, created_by_user_id,
        domain, step_code, step_name, symptom_title, symptom_detail,
        repro_rate, reboot_recovers, env_related,
        sw_version, plc_version, param_version, hw_version,
        facts_json, actions_taken, actions_taken_note,
        confirmed_as_fact, status, priority,
        created_at, updated_at
    ) VALUES (
        gen_random_uuid(), ticket_no_base || '-' || LPAD(ticket_counter::TEXT, 3, '0'),
        test_customer_id, test_project_id, test_device_id, test_user_id,
        'C', 'Step_410', '按键功能测试',
        '按键按下后响应延迟500ms，超出200ms响应时间要求',
        '在白色家电控制板测试中，需要测试按键功能。程序设定按键按下后，系统应在200ms内响应。实际测试中发现，按键按下后，系统响应延迟达到500ms，超出要求。检查程序逻辑，发现按键扫描周期设置为100ms，但去抖时间设置为300ms，加上程序处理时间，总延迟超过要求。',
        100, false, false,
        'V1.8.5', 'V1.5.2', 'P20241115', 'H2.6',
        '{"plc":{"scan_cycle":100,"debounce_time":300,"response_time":500,"program_efficiency":"MEDIUM"},"electrical":{"key_signal":"YES","interrupt":"NO"}}'::jsonb,
        ARRAY['CHECK_PROGRAM', 'REDUCE_DEBOUNCE', 'OPTIMIZE_CODE'],
        '已检查程序，发现去抖时间过长；尝试减少去抖时间，但需要平衡防抖效果和响应速度。',
        true, 'Submitted', 'P2',
        case_date - INTERVAL '12 days', case_date - INTERVAL '12 days'
    );
    ticket_counter := ticket_counter + 1;
    
    -- ========== 案例10：电动工具测试设备 - 转速波动大 ==========
    INSERT INTO tickets (
        ticket_id, ticket_no, customer_id, project_id, device_id, created_by_user_id,
        domain, step_code, step_name, symptom_title, symptom_detail,
        repro_rate, reboot_recovers, env_related,
        sw_version, plc_version, param_version, hw_version,
        facts_json, actions_taken, actions_taken_note,
        confirmed_as_fact, status, priority,
        created_at, updated_at
    ) VALUES (
        gen_random_uuid(), ticket_no_base || '-' || LPAD(ticket_counter::TEXT, 3, '0'),
        test_customer_id, test_project_id, test_device_id, test_user_id,
        'B', 'Step_515', '转速测试',
        '电机转速波动±200rpm，超出±50rpm稳定性要求',
        '在电动工具电机转速测试中，设定目标转速为3000rpm，要求稳定性为±50rpm。实际测试中发现，转速在2800rpm到3200rpm之间波动，波动幅度达到±200rpm，超出要求。检查电机驱动器和编码器反馈，发现编码器信号存在干扰，导致转速控制不稳定。',
        100, false, true,
        'V2.6.1', 'V1.9.5', 'P20241214', 'H3.6',
        '{"electrical":{"encoder_signal":"NO","signal_interference":"YES","motor_drive":"YES","shielding":"NO"},"plc":{"speed_control":"YES","pid_tuning":"MEDIUM"}}'::jsonb,
        ARRAY['CHECK_ENCODER', 'CHECK_SHIELDING', 'ADJUST_PID'],
        '已检查编码器，信号存在干扰；检查线缆屏蔽，发现屏蔽层接地不良；需要改善屏蔽和接地。',
        true, 'Submitted', 'P2',
        case_date - INTERVAL '11 days', case_date - INTERVAL '11 days'
    );
    ticket_counter := ticket_counter + 1;
    
    -- ========== 案例11：电机驱动器测试设备 - 过流保护误触发 ==========
    INSERT INTO tickets (
        ticket_id, ticket_no, customer_id, project_id, device_id, created_by_user_id,
        domain, step_code, step_name, symptom_title, symptom_detail,
        repro_rate, reboot_recovers, env_related,
        sw_version, plc_version, param_version, hw_version,
        facts_json, actions_taken, actions_taken_note,
        confirmed_as_fact, status, priority,
        created_at, updated_at
    ) VALUES (
        gen_random_uuid(), ticket_no_base || '-' || LPAD(ticket_counter::TEXT, 3, '0'),
        test_customer_id, test_project_id, test_device_id, test_user_id,
        'D', 'Step_620', '过流保护测试',
        '过流保护在正常电流下误触发，导致测试中断',
        '在电机驱动器FCT测试中，需要测试过流保护功能。测试程序设定过流保护阈值为20A，但在正常负载电流15A时，过流保护误触发，导致测试中断。检查保护电路和判定逻辑，发现电流采样存在噪声干扰，导致瞬时电流峰值超过阈值。',
        30, true, true,
        'V3.3.0', 'V2.2.0', 'P20241228', 'H4.6',
        '{"test":{"protection_trigger":"YES","false_positive":"YES","threshold_setting":"YES"},"electrical":{"current_sampling":"NO","noise_filter":"NO","signal_conditioning":"YES"}}'::jsonb,
        ARRAY['CHECK_THRESHOLD', 'ADD_FILTER', 'RETEST'],
        '已检查保护阈值设置，阈值正确；添加电流采样滤波后，误触发频率降低但仍存在。',
        true, 'Submitted', 'P3',
        case_date - INTERVAL '10 days', case_date - INTERVAL '10 days'
    );
    ticket_counter := ticket_counter + 1;
    
    -- ========== 案例12：BMS测试设备 - 温度传感器故障 ==========
    INSERT INTO tickets (
        ticket_id, ticket_no, customer_id, project_id, device_id, created_by_user_id,
        domain, step_code, step_name, symptom_title, symptom_detail,
        repro_rate, reboot_recovers, env_related,
        sw_version, plc_version, param_version, hw_version,
        facts_json, actions_taken, actions_taken_note,
        confirmed_as_fact, status, priority,
        created_at, updated_at
    ) VALUES (
        gen_random_uuid(), ticket_no_base || '-' || LPAD(ticket_counter::TEXT, 3, '0'),
        test_customer_id, test_project_id, test_device_id, test_user_id,
        'B', 'Step_225', '温度监控',
        '温度传感器读数异常，显示-40℃固定值',
        '在BMS测试设备中，需要监控电池包温度。测试过程中发现，温度传感器读数始终显示-40℃，不随实际温度变化。检查传感器连接和信号，发现传感器信号线断路，导致读取到默认错误值。',
        100, false, false,
        'V2.9.1', 'V1.7.8', 'P20241219', 'H3.9',
        '{"electrical":{"sensor_connection":"NO","signal_wire":"NO","sensor_power":"YES","adc_reading":"YES"},"mechanical":{"wire_damage":"YES","connector":"YES"}}'::jsonb,
        ARRAY['CHECK_SENSOR', 'CHECK_WIRE', 'REPLACE_SENSOR'],
        '已检查传感器，传感器本身正常；检查信号线，发现线缆断路；需要修复或更换线缆。',
        true, 'Submitted', 'P2',
        case_date - INTERVAL '9 days', case_date - INTERVAL '9 days'
    );
    ticket_counter := ticket_counter + 1;
    
    -- ========== 案例13：汽车电子测试设备 - 程序死循环 ==========
    INSERT INTO tickets (
        ticket_id, ticket_no, customer_id, project_id, device_id, created_by_user_id,
        domain, step_code, step_name, symptom_title, symptom_detail,
        repro_rate, reboot_recovers, env_related,
        sw_version, plc_version, param_version, hw_version,
        facts_json, actions_taken, actions_taken_note,
        confirmed_as_fact, status, priority,
        created_at, updated_at
    ) VALUES (
        gen_random_uuid(), ticket_no_base || '-' || LPAD(ticket_counter::TEXT, 3, '0'),
        test_customer_id, test_project_id, test_device_id, test_user_id,
        'C', 'Step_320', '通信握手',
        '程序在通信握手阶段进入死循环，系统无响应',
        '在汽车电子控制板测试中，上位机需要与待测板进行CAN通信握手。测试过程中发现，程序在等待握手响应时进入死循环，系统无响应，需要手动重启。检查程序逻辑，发现等待循环没有超时退出机制，当待测板无响应时程序会一直等待。',
        10, true, false,
        'V2.4.5', 'V1.8.7', 'P20241213', 'H3.3',
        '{"plc":{"program_logic":"NO","timeout_handling":"NO","error_handling":"NO","loop_control":"NO"},"system":{"watchdog":"NO","recovery":"NO"}}'::jsonb,
        ARRAY['REBOOT', 'CHECK_PROGRAM', 'ADD_TIMEOUT'],
        '已重启系统恢复正常；检查程序发现缺少超时和错误处理机制；需要添加超时退出和错误处理。',
        true, 'Submitted', 'P1',
        case_date - INTERVAL '8 days', case_date - INTERVAL '8 days'
    );
    ticket_counter := ticket_counter + 1;
    
    -- ========== 案例14：锂电池EOL测试设备 - 容量测试数据异常 ==========
    INSERT INTO tickets (
        ticket_id, ticket_no, customer_id, project_id, device_id, created_by_user_id,
        domain, step_code, step_name, symptom_title, symptom_detail,
        repro_rate, reboot_recovers, env_related,
        sw_version, plc_version, param_version, hw_version,
        facts_json, actions_taken, actions_taken_note,
        confirmed_as_fact, status, priority,
        created_at, updated_at
    ) VALUES (
        gen_random_uuid(), ticket_no_base || '-' || LPAD(ticket_counter::TEXT, 3, '0'),
        test_customer_id, test_project_id, test_device_id, test_user_id,
        'D', 'Step_405', '容量测试',
        '容量测试结果与标准设备对比，偏差达到8%，超出±3%要求',
        '在锂电池EOL测试中，容量测试是关键的判定指标。测试中发现，同一电池在标准设备上测试容量为2500mAh，但在本设备上测试为2300mAh，偏差达到8%，超出±3%的精度要求。检查测试程序和电流积分算法，发现积分时间步长设置过大，导致积分误差累积。',
        100, false, false,
        'V3.1.2', 'V2.0.3', 'P20241221', 'H4.4',
        '{"test":{"measurement_accuracy":"NO","calculation_method":"NO","integration_error":"YES","calibration":"YES"},"plc":{"algorithm":"NO","time_step":"NO","sampling_rate":"MEDIUM"}}'::jsonb,
        ARRAY['CHECK_ALGORITHM', 'REDUCE_TIME_STEP', 'RECALIBRATE'],
        '已检查容量计算算法，发现时间步长问题；减小积分时间步长后，精度有所改善。',
        true, 'Submitted', 'P2',
        case_date - INTERVAL '7 days', case_date - INTERVAL '7 days'
    );
    ticket_counter := ticket_counter + 1;
    
    -- ========== 案例15：白色家电测试设备 - 显示界面乱码 ==========
    INSERT INTO tickets (
        ticket_id, ticket_no, customer_id, project_id, device_id, created_by_user_id,
        domain, step_code, step_name, symptom_title, symptom_detail,
        repro_rate, reboot_recovers, env_related,
        sw_version, plc_version, param_version, hw_version,
        facts_json, actions_taken, actions_taken_note,
        confirmed_as_fact, status, priority,
        created_at, updated_at
    ) VALUES (
        gen_random_uuid(), ticket_no_base || '-' || LPAD(ticket_counter::TEXT, 3, '0'),
        test_customer_id, test_project_id, test_device_id, test_user_id,
        'E', 'Step_510', '显示功能测试',
        'LCD显示界面偶发乱码，重启后恢复正常',
        '在白色家电控制板测试中，需要测试LCD显示功能。测试过程中偶发出现显示界面乱码，字符显示错误或位置错乱，重启系统后恢复正常。该问题出现频率约3%，无明显规律。检查显示驱动和通信，怀疑是数据通信过程中的偶发错误或内存溢出导致。',
        3, true, true,
        'V1.9.0', 'V1.5.8', 'P20241125', 'H2.9',
        '{"system":{"memory_overflow":"POSSIBLE","data_corruption":"POSSIBLE","communication_error":"POSSIBLE"},"electrical":{"display_connection":"YES","signal_quality":"MEDIUM"},"environmental":{"temperature":32,"electromagnetic":"MEDIUM"}}'::jsonb,
        ARRAY['REBOOT', 'CHECK_MEMORY', 'CHECK_COMMUNICATION'],
        '已重启系统恢复正常；检查内存使用，发现可能存在内存泄漏；需要优化内存管理。',
        true, 'Submitted', 'P3',
        case_date - INTERVAL '6 days', case_date - INTERVAL '6 days'
    );
    ticket_counter := ticket_counter + 1;
    
    -- ========== 案例16：电动工具测试设备 - 装配定位不准 ==========
    INSERT INTO tickets (
        ticket_id, ticket_no, customer_id, project_id, device_id, created_by_user_id,
        domain, step_code, step_name, symptom_title, symptom_detail,
        repro_rate, reboot_recovers, env_related,
        sw_version, plc_version, param_version, hw_version,
        facts_json, actions_taken, actions_taken_note,
        confirmed_as_fact, status, priority,
        created_at, updated_at
    ) VALUES (
        gen_random_uuid(), ticket_no_base || '-' || LPAD(ticket_counter::TEXT, 3, '0'),
        test_customer_id, test_project_id, test_device_id, test_user_id,
        'A', 'Step_615', '零件装配定位',
        '自动化装配中零件定位偏差，导致装配失败率15%',
        '在电动工具控制板自动化装配中，需要将多个电子元件精确定位到PCB上。装配过程中发现，部分零件的定位偏差达到0.3mm，导致元件无法正确插入PCB孔位，装配失败率达到15%。检查定位机构和视觉定位系统，发现视觉定位标定参数需要更新。',
        15, false, true,
        'V2.7.0', 'V2.0.0', 'P20241216', 'H3.7',
        '{"mechanical":{"positioning_accuracy":"NO","mechanical_wear":"POSSIBLE","calibration":"NO"},"vision":{"camera_calibration":"NO","lighting":"MEDIUM","recognition_accuracy":"MEDIUM"}}'::jsonb,
        ARRAY['CHECK_VISION', 'RECALIBRATE', 'ADJUST_LIGHTING'],
        '已检查视觉定位系统，发现标定参数偏差；重新标定后，定位精度有所改善。',
        true, 'Submitted', 'P2',
        case_date - INTERVAL '5 days', case_date - INTERVAL '5 days'
    );
    ticket_counter := ticket_counter + 1;
    
    -- ========== 案例17：电机驱动器测试设备 - 效率测试数据异常 ==========
    INSERT INTO tickets (
        ticket_id, ticket_no, customer_id, project_id, device_id, created_by_user_id,
        domain, step_code, step_name, symptom_title, symptom_detail,
        repro_rate, reboot_recovers, env_related,
        sw_version, plc_version, param_version, hw_version,
        facts_json, actions_taken, actions_taken_note,
        confirmed_as_fact, status, priority,
        created_at, updated_at
    ) VALUES (
        gen_random_uuid(), ticket_no_base || '-' || LPAD(ticket_counter::TEXT, 3, '0'),
        test_customer_id, test_project_id, test_device_id, test_user_id,
        'D', 'Step_720', '效率测试',
        '电机效率测试结果与理论值偏差大，测试数据异常',
        '在电机驱动器效率测试中，需要测试电机在不同负载下的效率。测试中发现，效率测试结果与理论计算值偏差较大，部分工况下效率显示超过100%，明显异常。检查功率测量和效率计算程序，发现功率因数测量存在错误，导致效率计算不准确。',
        100, false, false,
        'V3.3.5', 'V2.2.2', 'P20241229', 'H4.7',
        '{"test":{"calculation_error":"YES","power_factor":"NO","measurement_method":"YES","data_validity":"NO"},"plc":{"efficiency_formula":"NO","power_calculation":"NO"}}'::jsonb,
        ARRAY['CHECK_FORMULA', 'CHECK_POWER_FACTOR', 'VERIFY_CALCULATION'],
        '已检查效率计算公式，发现功率因数处理错误；修正功率因数测量后，效率计算恢复正常。',
        true, 'Submitted', 'P2',
        case_date - INTERVAL '4 days', case_date - INTERVAL '4 days'
    );
    ticket_counter := ticket_counter + 1;
    
    -- ========== 案例18：BMS测试设备 - 均衡功能测试失败 ==========
    INSERT INTO tickets (
        ticket_id, ticket_no, customer_id, project_id, device_id, created_by_user_id,
        domain, step_code, step_name, symptom_title, symptom_detail,
        repro_rate, reboot_recovers, env_related,
        sw_version, plc_version, param_version, hw_version,
        facts_json, actions_taken, actions_taken_note,
        confirmed_as_fact, status, priority,
        created_at, updated_at
    ) VALUES (
        gen_random_uuid(), ticket_no_base || '-' || LPAD(ticket_counter::TEXT, 3, '0'),
        test_customer_id, test_project_id, test_device_id, test_user_id,
        'C', 'Step_325', '均衡功能测试',
        '电池包均衡功能测试中，均衡控制逻辑执行错误',
        '在BMS测试中，需要测试电池包均衡功能。测试程序设定当单体电压差超过50mV时启动均衡，但实际测试中发现，均衡控制逻辑执行错误，在电压差小于50mV时也启动均衡，或者在电压差超过50mV时不启动均衡。检查程序逻辑，发现电压差计算和判定条件存在错误。',
        100, false, false,
        'V2.9.5', 'V1.7.9', 'P20241220', 'H4.0',
        '{"plc":{"control_logic":"NO","voltage_calculation":"NO","threshold_check":"NO","equilibrium_control":"NO"},"test":{"test_sequence":"YES","trigger_condition":"NO"}}'::jsonb,
        ARRAY['CHECK_LOGIC', 'VERIFY_CALCULATION', 'FIX_CONDITION'],
        '已检查均衡控制逻辑，发现电压差计算和判定条件错误；修正后，均衡功能测试正常。',
        true, 'Submitted', 'P2',
        case_date - INTERVAL '3 days', case_date - INTERVAL '3 days'
    );
    ticket_counter := ticket_counter + 1;
    
    -- ========== 案例19：汽车电子测试设备 - 绝缘测试击穿 ==========
    INSERT INTO tickets (
        ticket_id, ticket_no, customer_id, project_id, device_id, created_by_user_id,
        domain, step_code, step_name, symptom_title, symptom_detail,
        repro_rate, reboot_recovers, env_related,
        sw_version, plc_version, param_version, hw_version,
        facts_json, actions_taken, actions_taken_note,
        confirmed_as_fact, status, priority,
        created_at, updated_at
    ) VALUES (
        gen_random_uuid(), ticket_no_base || '-' || LPAD(ticket_counter::TEXT, 3, '0'),
        test_customer_id, test_project_id, test_device_id, test_user_id,
        'B', 'Step_425', '绝缘测试',
        '绝缘测试电压无法达到设定值，怀疑测试回路存在问题',
        '在汽车电子控制板绝缘测试中，需要施加500V测试电压，检查绝缘电阻。测试中发现，测试电压无法达到500V，最高只能达到350V，怀疑测试回路存在漏电或负载过大。检查测试回路和高压电源，发现测试夹具与待测板之间存在接触不良，导致测试回路阻抗异常。',
        40, false, true,
        'V2.5.0', 'V1.9.2', 'P20241211', 'H3.4',
        '{"electrical":{"high_voltage_source":"YES","test_circuit":"NO","contact_resistance":"NO","leakage_current":"YES"},"mechanical":{"fixture_contact":"NO","connection":"NO"}}'::jsonb,
        ARRAY['CHECK_FIXTURE', 'CHECK_CONTACT', 'CLEAN_CONTACT'],
        '已检查测试夹具，发现接触不良；清洁接触点后，问题有所改善但仍需优化夹具设计。',
        true, 'Submitted', 'P2',
        case_date - INTERVAL '2 days', case_date - INTERVAL '2 days'
    );
    ticket_counter := ticket_counter + 1;
    
    -- ========== 案例20：控制板自动化装配 - 贴片位置偏移 ==========
    INSERT INTO tickets (
        ticket_id, ticket_no, customer_id, project_id, device_id, created_by_user_id,
        domain, step_code, step_name, symptom_title, symptom_detail,
        repro_rate, reboot_recovers, env_related,
        sw_version, plc_version, param_version, hw_version,
        facts_json, actions_taken, actions_taken_note,
        confirmed_as_fact, status, priority,
        created_at, updated_at
    ) VALUES (
        gen_random_uuid(), ticket_no_base || '-' || LPAD(ticket_counter::TEXT, 3, '0'),
        test_customer_id, test_project_id, test_device_id, test_user_id,
        'A', 'Step_730', 'SMT贴片',
        'SMT贴片机贴片位置偏移，导致元件焊接不良',
        '在控制板自动化装配中，使用SMT贴片机进行元件贴装。贴装过程中发现，部分小尺寸元件（如0402封装）的贴装位置存在偏移，偏移量达到0.15mm，导致元件焊接后出现虚焊或短路。检查贴片机的视觉定位系统和机械精度，发现吸嘴磨损和视觉标定需要更新。',
        20, false, true,
        'V4.0.1', 'V2.3.0', 'P20250101', 'H5.0',
        '{"mechanical":{"placement_accuracy":"NO","nozzle_wear":"YES","mechanical_precision":"MEDIUM","calibration":"NO"},"vision":{"component_recognition":"YES","position_calibration":"NO","lighting":"YES"}}'::jsonb,
        ARRAY['CHECK_NOZZLE', 'RECALIBRATE_VISION', 'REPLACE_NOZZLE'],
        '已检查吸嘴，发现磨损；重新标定视觉定位系统；更换磨损吸嘴后，贴装精度有所改善。',
        true, 'Submitted', 'P2',
        case_date - INTERVAL '1 day', case_date - INTERVAL '1 day'
    );
    
    RAISE NOTICE 'Successfully inserted 20 industry cases.';
    
END $$;

-- 验证插入结果
SELECT 
    COUNT(*) as total_tickets,
    domain,
    status,
    priority
FROM tickets
WHERE ticket_no LIKE 'TK-%'
GROUP BY domain, status, priority
ORDER BY domain, status, priority;






