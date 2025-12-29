-- 插入第21-40个非标自动化行业真实案例
-- 执行前确保已执行过 create-and-insert-industry-cases.sql

DO $$
DECLARE
    test_user_id UUID;
    test_customer_id UUID;
    test_project_id UUID;
    test_device_id UUID;
    ticket_no_base VARCHAR(20);
    case_date DATE;
    ticket_counter INT := 21;
BEGIN
    -- 获取第一个用户作为创建者
    SELECT id INTO test_user_id FROM users LIMIT 1;
    
    IF test_user_id IS NULL THEN
        RAISE EXCEPTION 'No users found. Please create users first.';
    END IF;
    
    -- 获取或创建测试客户
    SELECT customer_id INTO test_customer_id FROM customers WHERE customer_name = '汽车电子测试客户' LIMIT 1;
    
    IF test_customer_id IS NULL THEN
        INSERT INTO customers (customer_id, customer_name, industry_type, created_at, updated_at)
        VALUES (gen_random_uuid(), '汽车电子测试客户', '汽车电子', NOW(), NOW())
        RETURNING customer_id INTO test_customer_id;
    END IF;
    
    -- 获取或创建测试项目
    SELECT project_id INTO test_project_id FROM projects WHERE project_no = 'PRJ-2024-001' LIMIT 1;
    
    IF test_project_id IS NULL THEN
        INSERT INTO projects (project_id, project_no, project_name, customer_id, customer_name, industry_type, project_status, created_at, updated_at)
        VALUES (gen_random_uuid(), 'PRJ-2024-001', '汽车电子FCT测试设备项目', test_customer_id, '汽车电子测试客户', '汽车电子', '进行中', NOW(), NOW())
        RETURNING project_id INTO test_project_id;
    END IF;
    
    -- 获取或创建测试设备
    SELECT device_id INTO test_device_id FROM devices WHERE device_sn = 'DEV-2024-001' LIMIT 1;
    
    IF test_device_id IS NULL THEN
        INSERT INTO devices (device_id, device_sn, device_name, project_id, device_type, status, created_at, updated_at)
        VALUES (gen_random_uuid(), 'DEV-2024-001', '汽车电子FCT测试设备', test_project_id, 'FCT测试设备', 'active', NOW(), NOW())
        RETURNING device_id INTO test_device_id;
    END IF;
    
    -- 设置基础日期
    case_date := CURRENT_DATE;
    ticket_no_base := 'TK-' || TO_CHAR(case_date, 'YYYYMMDD');
    
    -- ========== 案例21：汽车电子测试设备 - 测试探针压力不足 ==========
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
        'A', 'Step_125', '探针压力测试',
        '测试探针接触压力不足，导致接触电阻过大',
        '在汽车电子控制板FCT测试中，测试探针需要施加足够的压力才能保证良好的电气接触。测试中发现，部分探针的接触压力不足，导致接触电阻达到100Ω以上，影响测试信号的准确性。检查探针弹簧机构和压力调节装置，发现弹簧老化导致弹力下降，压力调节机构松动。',
        70, false, false,
        'V2.3.5', 'V1.8.3', 'P20241216', 'H3.1',
        '{"mechanical":{"probe_pressure":"NO","spring_force":"NO","pressure_adjustment":"NO","mechanical_wear":"YES"},"electrical":{"contact_resistance":"NO","signal_quality":"NO"}}'::jsonb,
        ARRAY['CHECK_PRESSURE', 'ADJUST_PRESSURE', 'REPLACE_SPRING'],
        '已检查探针压力，发现压力不足；调整压力调节机构后，部分探针恢复正常，但仍有部分需要更换弹簧。',
        true, 'Submitted', 'P2',
        case_date - INTERVAL '19 days', case_date - INTERVAL '19 days'
    );
    ticket_counter := ticket_counter + 1;
    
    -- ========== 案例22：锂电池EOL测试设备 - 温度控制超调 ==========
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
        'C', 'Step_210', '温度控制',
        '温度控制过程中出现超调，温度波动超过±2℃要求',
        '在锂电池EOL测试的温度循环测试中，需要精确控制测试环境温度。测试中发现，当目标温度从25℃切换到-10℃时，实际温度会先降到-15℃（超调5℃），然后才稳定到-10℃，温度波动超过±2℃的工艺要求。检查PID控制参数，发现积分时间设置过小，导致系统响应过快，出现超调。',
        100, false, true,
        'V3.1.8', 'V2.0.2', 'P20241221', 'H4.2',
        '{"plc":{"pid_control":"NO","overshoot":"YES","temperature_control":"YES","control_parameters":"NO"},"electrical":{"heater_control":"YES","cooler_control":"YES","temperature_sensor":"YES"}}'::jsonb,
        ARRAY['CHECK_PID', 'ADJUST_PID', 'RETEST'],
        '已检查PID控制参数，发现积分时间过小；调整PID参数后，超调现象有所改善但仍需进一步优化。',
        true, 'Submitted', 'P2',
        case_date - INTERVAL '18 days', case_date - INTERVAL '18 days'
    );
    ticket_counter := ticket_counter + 1;
    
    -- ========== 案例23：白色家电测试设备 - 通信协议不匹配 ==========
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
        'C', 'Step_315', '通信协议测试',
        '与待测板通信时协议不匹配，无法建立连接',
        '在白色家电控制板测试中，上位机需要通过UART与待测板通信。测试中发现，部分待测板无法建立通信连接，检查通信协议配置，发现待测板使用的是115200波特率、8N1格式，但测试程序配置为9600波特率、8E1格式，导致通信协议不匹配。',
        100, false, false,
        'V1.9.5', 'V1.5.9', 'P20241126', 'H2.9',
        '{"plc":{"communication_protocol":"NO","baud_rate":"NO","data_format":"NO","parity":"NO"},"electrical":{"uart_connection":"YES","signal_level":"YES"}}'::jsonb,
        ARRAY['CHECK_PROTOCOL', 'MODIFY_CONFIG', 'RETEST'],
        '已检查通信协议配置，发现波特率和数据格式不匹配；修改配置后，通信连接正常。',
        true, 'Submitted', 'P2',
        case_date - INTERVAL '17 days', case_date - INTERVAL '17 days'
    );
    ticket_counter := ticket_counter + 1;
    
    -- ========== 案例24：电动工具测试设备 - 振动测试数据异常 ==========
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
        'D', 'Step_420', '振动测试',
        '振动测试数据采集异常，部分数据点丢失',
        '在电动工具电机振动测试中，需要采集振动加速度数据进行分析。测试中发现，振动数据采集过程中出现数据点丢失，采样率设定为1000Hz，但实际采集到的数据只有约800Hz，导致振动频谱分析不准确。检查数据采集程序和硬件，发现数据缓冲区溢出，导致部分数据丢失。',
        100, false, false,
        'V2.5.5', 'V1.9.1', 'P20241211', 'H3.5',
        '{"test":{"data_acquisition":"NO","sampling_rate":"NO","data_loss":"YES","buffer_overflow":"YES"},"plc":{"data_processing":"NO","buffer_management":"NO"}}'::jsonb,
        ARRAY['CHECK_BUFFER', 'INCREASE_BUFFER', 'OPTIMIZE_CODE'],
        '已检查数据采集程序，发现缓冲区溢出；增加缓冲区大小后，数据丢失问题得到解决。',
        true, 'Submitted', 'P2',
        case_date - INTERVAL '16 days', case_date - INTERVAL '16 days'
    );
    ticket_counter := ticket_counter + 1;
    
    -- ========== 案例25：电机驱动器测试设备 - 功率因数测量错误 ==========
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
        'D', 'Step_625', '功率因数测试',
        '功率因数测量结果异常，显示值超过1.0',
        '在电机驱动器效率测试中，需要测量功率因数来计算有功功率。测试中发现，功率因数测量结果异常，部分工况下显示值超过1.0（理论上不可能），导致效率计算错误。检查功率因数测量算法，发现电压和电流相位差计算存在错误，当相位差计算错误时会导致功率因数超过1.0。',
        100, false, false,
        'V3.3.2', 'V2.2.1', 'P20241229', 'H4.6',
        '{"test":{"power_factor":"NO","calculation_error":"YES","phase_measurement":"NO","data_validity":"NO"},"plc":{"algorithm":"NO","phase_calculation":"NO"}}'::jsonb,
        ARRAY['CHECK_ALGORITHM', 'VERIFY_PHASE', 'FIX_CALCULATION'],
        '已检查功率因数计算算法，发现相位差计算错误；修正算法后，功率因数测量恢复正常。',
        true, 'Submitted', 'P2',
        case_date - INTERVAL '15 days', case_date - INTERVAL '15 days'
    );
    ticket_counter := ticket_counter + 1;
    
    -- ========== 案例26：BMS测试设备 - 均衡电流控制不稳定 ==========
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
        'B', 'Step_130', '均衡电流控制',
        '电池包均衡电流控制不稳定，电流波动±20%',
        '在BMS测试设备中，需要控制均衡电流对电池包进行均衡。测试中发现，均衡电流设定为100mA，但实际电流在80mA到120mA之间波动，波动幅度达到±20%，超出±5%的精度要求。检查均衡电路和电流控制程序，发现PWM控制频率设置不当，导致电流控制不稳定。',
        100, false, true,
        'V2.8.5', 'V1.7.6', 'P20241219', 'H3.8',
        '{"electrical":{"current_control":"NO","pwm_frequency":"NO","current_sensor":"YES","control_stability":"NO"},"plc":{"pwm_control":"NO","control_loop":"YES"}}'::jsonb,
        ARRAY['CHECK_PWM', 'ADJUST_FREQUENCY', 'RETEST'],
        '已检查PWM控制，发现频率设置不当；调整PWM频率后，电流控制稳定性有所改善。',
        true, 'Submitted', 'P2',
        case_date - INTERVAL '14 days', case_date - INTERVAL '14 days'
    );
    ticket_counter := ticket_counter + 1;
    
    -- ========== 案例27：汽车电子测试设备 - 测试夹具磨损 ==========
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
        'A', 'Step_225', '测试夹具定位',
        '测试夹具定位销磨损，导致产品定位不准确',
        '在汽车电子控制板FCT测试中，使用测试夹具对产品进行定位。测试中发现，部分产品的定位不准确，检查测试夹具，发现定位销有磨损，直径从原来的5.0mm磨损到4.8mm，导致产品在夹具中的位置偏移，影响测试探针的接触精度。',
        60, false, false,
        'V2.4.2', 'V1.8.6', 'P20241213', 'H3.2',
        '{"mechanical":{"fixture_wear":"YES","positioning_pin":"NO","positioning_accuracy":"NO","mechanical_wear":"YES"},"electrical":{"probe_contact":"NO"}}'::jsonb,
        ARRAY['CHECK_FIXTURE', 'MEASURE_PIN', 'REPLACE_PIN'],
        '已检查测试夹具，发现定位销磨损；测量定位销直径，确认磨损；需要更换定位销或整个夹具。',
        true, 'Submitted', 'P2',
        case_date - INTERVAL '13 days', case_date - INTERVAL '13 days'
    );
    ticket_counter := ticket_counter + 1;
    
    -- ========== 案例28：锂电池EOL测试设备 - 数据存储失败 ==========
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
        'E', 'Step_210', '测试数据存储',
        '测试数据存储时偶发失败，数据丢失',
        '在锂电池EOL测试中，测试完成后需要将测试数据存储到数据库。测试中发现，偶发出现数据存储失败的情况，导致测试数据丢失，需要重新测试。检查数据库连接和存储程序，发现数据库连接池配置不当，当并发测试时可能出现连接超时，导致数据存储失败。',
        10, true, false,
        'V3.1.6', 'V2.0.4', 'P20241222', 'H4.3',
        '{"system":{"database_connection":"NO","connection_pool":"NO","data_storage":"NO","concurrent_access":"YES"},"plc":{"error_handling":"NO","retry_mechanism":"NO"}}'::jsonb,
        ARRAY['CHECK_DATABASE', 'INCREASE_POOL', 'ADD_RETRY'],
        '已检查数据库连接，发现连接池配置不足；增加连接池大小和添加重试机制后，数据存储失败率降低。',
        true, 'Submitted', 'P3',
        case_date - INTERVAL '12 days', case_date - INTERVAL '12 days'
    );
    ticket_counter := ticket_counter + 1;
    
    -- ========== 案例29：白色家电测试设备 - 测试序列执行错误 ==========
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
        'C', 'Step_415', '测试序列执行',
        '测试序列执行顺序错误，部分测试步骤被跳过',
        '在白色家电控制板测试中，需要按照预定的测试序列依次执行各项测试。测试中发现，部分测试步骤被跳过，导致测试不完整。检查测试序列程序，发现序列控制逻辑存在错误，当某个测试步骤失败时，程序没有正确处理，导致后续步骤执行顺序混乱。',
        30, true, false,
        'V1.8.8', 'V1.5.3', 'P20241116', 'H2.7',
        '{"plc":{"test_sequence":"NO","error_handling":"NO","step_control":"NO","program_logic":"NO"},"test":{"test_completeness":"NO","step_execution":"NO"}}'::jsonb,
        ARRAY['CHECK_SEQUENCE', 'FIX_LOGIC', 'ADD_ERROR_HANDLING'],
        '已检查测试序列程序，发现错误处理逻辑有问题；修复逻辑和添加错误处理后，测试序列执行正常。',
        true, 'Submitted', 'P2',
        case_date - INTERVAL '11 days', case_date - INTERVAL '11 days'
    );
    ticket_counter := ticket_counter + 1;
    
    -- ========== 案例30：电动工具测试设备 - 负载模拟器故障 ==========
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
        'B', 'Step_520', '负载模拟',
        '负载模拟器无法正常加载，负载电流不稳定',
        '在电动工具电机测试中，需要使用负载模拟器模拟不同负载条件。测试中发现，负载模拟器无法正常加载，设定负载电流为5A，但实际电流在3A到7A之间波动，无法稳定。检查负载模拟器和控制电路，发现负载控制MOSFET驱动电路故障，导致负载电流控制不稳定。',
        100, false, true,
        'V2.6.3', 'V1.9.6', 'P20241215', 'H3.6',
        '{"electrical":{"load_simulator":"NO","mosfet_drive":"NO","current_control":"NO","circuit_fault":"YES"},"plc":{"load_control":"YES","pwm_output":"YES"}}'::jsonb,
        ARRAY['CHECK_LOAD', 'CHECK_MOSFET', 'REPLACE_COMPONENT'],
        '已检查负载模拟器，发现MOSFET驱动电路故障；更换驱动电路元件后，负载控制恢复正常。',
        true, 'Submitted', 'P2',
        case_date - INTERVAL '10 days', case_date - INTERVAL '10 days'
    );
    ticket_counter := ticket_counter + 1;
    
    -- ========== 案例31：电机驱动器测试设备 - 编码器信号丢失 ==========
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
        'B', 'Step_725', '编码器反馈',
        '编码器反馈信号偶发丢失，导致位置控制异常',
        '在电机驱动器位置控制测试中，需要编码器反馈信号进行闭环控制。测试中发现，编码器反馈信号偶发丢失，导致位置控制异常，电机位置偏差增大。检查编码器连接和信号线，发现编码器信号线屏蔽层破损，导致信号受到干扰，偶发出现信号丢失。',
        15, true, true,
        'V3.3.8', 'V2.2.3', 'P20241230', 'H4.7',
        '{"electrical":{"encoder_signal":"NO","signal_loss":"YES","shielding":"NO","signal_interference":"YES"},"mechanical":{"cable_damage":"YES"}}'::jsonb,
        ARRAY['CHECK_ENCODER', 'CHECK_CABLE', 'REPLACE_CABLE'],
        '已检查编码器，编码器本身正常；检查信号线，发现屏蔽层破损；更换信号线后，信号丢失问题解决。',
        true, 'Submitted', 'P3',
        case_date - INTERVAL '9 days', case_date - INTERVAL '9 days'
    );
    ticket_counter := ticket_counter + 1;
    
    -- ========== 案例32：BMS测试设备 - SOC计算误差大 ==========
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
        'D', 'Step_230', 'SOC计算',
        '电池包SOC（电量状态）计算误差大，与实际电量偏差超过10%',
        '在BMS测试中，需要计算电池包的SOC（State of Charge，电量状态）。测试中发现，SOC计算值与实际电量偏差较大，最大偏差超过10%，影响电池包的使用和判定。检查SOC计算算法和输入参数，发现容量标定参数不准确，导致SOC计算基准错误。',
        100, false, true,
        'V2.9.3', 'V1.7.10', 'P20241221', 'H3.9',
        '{"test":{"soc_calculation":"NO","calculation_error":"YES","calibration":"NO","parameter_accuracy":"NO"},"plc":{"algorithm":"YES","parameter_management":"NO"}}'::jsonb,
        ARRAY['CHECK_ALGORITHM', 'RECALIBRATE', 'VERIFY_PARAMETER'],
        '已检查SOC计算算法，算法逻辑正确；检查容量标定参数，发现参数不准确；重新标定后，SOC计算精度改善。',
        true, 'Submitted', 'P2',
        case_date - INTERVAL '8 days', case_date - INTERVAL '8 days'
    );
    ticket_counter := ticket_counter + 1;
    
    -- ========== 案例33：汽车电子测试设备 - 测试时间超时 ==========
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
        'C', 'Step_330', '测试执行',
        '测试执行时间超过设定超时时间，测试被中断',
        '在汽车电子控制板FCT测试中，整个测试序列设定超时时间为300秒。测试中发现，部分测试序列执行时间超过300秒，导致测试被强制中断，测试结果不完整。检查测试程序和测试步骤，发现部分测试步骤执行时间过长，特别是通信测试和数据处理步骤，导致总时间超过超时限制。',
        40, false, false,
        'V2.4.8', 'V1.8.8', 'P20241214', 'H3.3',
        '{"plc":{"test_timeout":"YES","execution_time":"NO","time_management":"NO","program_efficiency":"MEDIUM"},"test":{"test_completeness":"NO","time_optimization":"NO"}}'::jsonb,
        ARRAY['CHECK_TIMEOUT', 'OPTIMIZE_STEPS', 'INCREASE_TIMEOUT'],
        '已检查超时设置，发现部分测试步骤耗时过长；优化测试步骤和增加超时时间后，测试完成率提高。',
        true, 'Submitted', 'P2',
        case_date - INTERVAL '7 days', case_date - INTERVAL '7 days'
    );
    ticket_counter := ticket_counter + 1;
    
    -- ========== 案例34：锂电池EOL测试设备 - 充放电效率测试异常 ==========
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
        'D', 'Step_410', '充放电效率测试',
        '充放电效率测试结果异常，效率值超过100%',
        '在锂电池EOL测试中，需要测试电池的充放电效率。测试中发现，部分电池的充放电效率显示超过100%，明显异常（理论上效率不可能超过100%）。检查效率计算程序和能量测量，发现放电能量测量存在错误，导致效率计算时分子大于分母，出现效率超过100%的异常结果。',
        100, false, false,
        'V3.1.9', 'V2.0.6', 'P20241223', 'H4.4',
        '{"test":{"efficiency_calculation":"NO","energy_measurement":"NO","data_validity":"NO","calculation_error":"YES"},"plc":{"energy_integration":"NO","calculation_formula":"NO"}}'::jsonb,
        ARRAY['CHECK_FORMULA', 'VERIFY_MEASUREMENT', 'FIX_CALCULATION'],
        '已检查效率计算公式，发现放电能量测量错误；修正能量测量后，效率计算恢复正常。',
        true, 'Submitted', 'P2',
        case_date - INTERVAL '6 days', case_date - INTERVAL '6 days'
    );
    ticket_counter := ticket_counter + 1;
    
    -- ========== 案例35：白色家电测试设备 - 继电器触点粘连 ==========
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
        'B', 'Step_515', '继电器控制',
        '测试用继电器触点粘连，无法正常断开',
        '在白色家电控制板测试中，使用继电器控制测试回路的通断。测试中发现，部分继电器触点粘连，无法正常断开，导致测试回路一直保持导通状态，影响测试结果。检查继电器，发现继电器触点有烧蚀痕迹，可能是负载电流过大或频繁开关导致触点损坏。',
        30, false, false,
        'V1.9.2', 'V1.5.10', 'P20241127', 'H2.9',
        '{"electrical":{"relay_contact":"NO","contact_sticking":"YES","contact_damage":"YES","load_current":"HIGH"},"mechanical":{"relay_wear":"YES"}}'::jsonb,
        ARRAY['CHECK_RELAY', 'INSPECT_CONTACT', 'REPLACE_RELAY'],
        '已检查继电器，发现触点粘连；检查触点，发现烧蚀痕迹；需要更换继电器或增加保护电路。',
        true, 'Submitted', 'P2',
        case_date - INTERVAL '5 days', case_date - INTERVAL '5 days'
    );
    ticket_counter := ticket_counter + 1;
    
    -- ========== 案例36：电动工具测试设备 - 测试数据同步失败 ==========
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
        'E', 'Step_625', '数据同步',
        '测试数据同步到服务器时偶发失败，数据丢失',
        '在电动工具测试设备中，测试完成后需要将测试数据同步到服务器。测试中发现，数据同步过程中偶发失败，导致测试数据丢失，需要重新测试。检查网络连接和同步程序，发现网络不稳定时可能出现连接中断，而同步程序没有重试机制，导致数据同步失败。',
        5, true, true,
        'V2.7.2', 'V2.0.1', 'P20241217', 'H3.7',
        '{"system":{"data_sync":"NO","network_connection":"NO","retry_mechanism":"NO","error_handling":"NO"},"environmental":{"network_stability":"MEDIUM"}}'::jsonb,
        ARRAY['CHECK_NETWORK', 'ADD_RETRY', 'IMPROVE_HANDLING'],
        '已检查网络连接，发现网络不稳定；添加重试机制和改善错误处理后，数据同步成功率提高。',
        true, 'Submitted', 'P3',
        case_date - INTERVAL '4 days', case_date - INTERVAL '4 days'
    );
    ticket_counter := ticket_counter + 1;
    
    -- ========== 案例37：电机驱动器测试设备 - 过压保护误触发 ==========
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
        'D', 'Step_730', '过压保护测试',
        '过压保护在正常电压下误触发，导致测试中断',
        '在电机驱动器FCT测试中，需要测试过压保护功能。测试程序设定过压保护阈值为400V，但在正常电压380V时，过压保护误触发，导致测试中断。检查保护电路和电压采样，发现电压采样电路存在噪声干扰，导致瞬时电压峰值超过阈值，触发保护。',
        20, true, true,
        'V3.4.0', 'V2.2.4', 'P20250102', 'H4.8',
        '{"test":{"protection_trigger":"YES","false_positive":"YES","voltage_measurement":"NO"},"electrical":{"voltage_sampling":"NO","noise_filter":"NO","signal_conditioning":"YES"}}'::jsonb,
        ARRAY['CHECK_THRESHOLD', 'ADD_FILTER', 'RETEST'],
        '已检查保护阈值，阈值设置正确；添加电压采样滤波后，误触发频率降低。',
        true, 'Submitted', 'P3',
        case_date - INTERVAL '3 days', case_date - INTERVAL '3 days'
    );
    ticket_counter := ticket_counter + 1;
    
    -- ========== 案例38：BMS测试设备 - 通信波特率错误 ==========
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
        'C', 'Step_335', 'CAN通信测试',
        '与BMS通信时波特率不匹配，无法建立通信',
        '在BMS测试设备中，需要通过CAN总线与BMS通信。测试中发现，无法与BMS建立通信连接，检查CAN通信配置，发现测试设备配置的波特率为500kbps，但BMS实际使用的波特率为250kbps，导致通信协议不匹配，无法建立连接。',
        100, false, false,
        'V2.9.8', 'V1.8.0', 'P20241222', 'H4.0',
        '{"plc":{"can_configuration":"NO","baud_rate":"NO","communication_protocol":"NO"},"electrical":{"can_bus":"YES","termination":"YES"}}'::jsonb,
        ARRAY['CHECK_CONFIG', 'MODIFY_BAUD_RATE', 'RETEST'],
        '已检查CAN通信配置，发现波特率不匹配；修改波特率配置后，通信连接正常。',
        true, 'Submitted', 'P2',
        case_date - INTERVAL '2 days', case_date - INTERVAL '2 days'
    );
    ticket_counter := ticket_counter + 1;
    
    -- ========== 案例39：汽车电子测试设备 - 测试结果判定错误 ==========
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
        'D', 'Step_430', '测试结果判定',
        '测试结果判定逻辑错误，合格产品被判为不合格',
        '在汽车电子控制板FCT测试中，需要对各项测试结果进行判定。测试中发现，部分合格产品被判定为不合格，检查判定逻辑，发现判定条件使用了错误的逻辑运算符，将"或"（OR）条件写成了"与"（AND）条件，导致判定结果错误。',
        100, false, false,
        'V2.5.3', 'V1.9.3', 'P20241212', 'H3.4',
        '{"test":{"judgment_logic":"NO","logical_operator":"NO","test_result":"NO","false_negative":"YES"},"plc":{"condition_check":"NO","program_logic":"NO"}}'::jsonb,
        ARRAY['CHECK_LOGIC', 'FIX_OPERATOR', 'VERIFY_RESULT'],
        '已检查判定逻辑，发现逻辑运算符错误；修正逻辑运算符后，判定结果恢复正常。',
        true, 'Submitted', 'P1',
        case_date - INTERVAL '1 day', case_date - INTERVAL '1 day'
    );
    ticket_counter := ticket_counter + 1;
    
    -- ========== 案例40：控制板自动化装配 - 视觉识别误判 ==========
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
        'A', 'Step_735', '视觉识别',
        '视觉识别系统误判，将合格元件识别为不合格',
        '在控制板自动化装配中，使用视觉识别系统检测元件是否正确安装。装配过程中发现，视觉识别系统将部分正确安装的元件识别为不合格，导致装配流程中断。检查视觉识别程序和图像处理算法，发现识别阈值设置过严格，导致误判率较高。',
        25, false, true,
        'V4.0.5', 'V2.3.2', 'P20250103', 'H5.1',
        '{"vision":{"recognition_accuracy":"NO","false_positive":"YES","threshold_setting":"NO","image_processing":"YES"},"mechanical":{"component_placement":"YES","lighting":"MEDIUM"}}'::jsonb,
        ARRAY['CHECK_VISION', 'ADJUST_THRESHOLD', 'IMPROVE_LIGHTING'],
        '已检查视觉识别系统，发现阈值设置过严格；调整识别阈值和改善光照后，误判率降低。',
        true, 'Submitted', 'P2',
        case_date, case_date
    );
    
    RAISE NOTICE 'Successfully inserted cases 21-40 (20 new cases).';
    
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






