-- 为所有工单添加符合非标自动化行业特点的详细信息
-- 基于问题域（A/B/C/D/E）和项目类型生成不同的行业特定信息

DO $$
DECLARE
    ticket_rec RECORD;
    device_rec RECORD;
    project_rec RECORD;
    customer_rec RECORD;
    
    -- 行业特定的症状描述模板
    symptom_detail_text TEXT;
    facts_json_text TEXT;
    actions_taken_array TEXT[];
    root_cause_text TEXT;
    symptom_title_text VARCHAR(200);
    step_name_text VARCHAR(100);
    env_description_text TEXT;
    
    -- 计数器
    updated_count INT := 0;
    domain_char CHAR(1);
    device_type_text VARCHAR(50);
    industry_type_text VARCHAR(50);
    
BEGIN
    -- 遍历所有工单
    FOR ticket_rec IN 
        SELECT t.ticket_id, t.ticket_no, t.domain, t.step_code, t.symptom_title, 
               t.device_id, t.project_id, t.customer_id,
               ROW_NUMBER() OVER (PARTITION BY t.device_id ORDER BY t.created_at) as device_ticket_seq
        FROM tickets t
        WHERE t.ticket_no LIKE 'TK-%'
        ORDER BY t.device_id, t.created_at
    LOOP
        domain_char := ticket_rec.domain;
        
        -- 获取设备、项目、客户信息
        SELECT d.device_type, p.device_type as project_device_type, p.industry_type, c.industry_type as customer_industry
        INTO device_type_text, device_type_text, industry_type_text, industry_type_text
        FROM devices d
        JOIN projects p ON d.project_id = p.project_id
        JOIN customers c ON p.customer_id = c.customer_id
        WHERE d.device_id = ticket_rec.device_id;
        
        -- 根据问题域生成不同的行业特定信息
        CASE domain_char
            WHEN 'A' THEN -- 机械/动作问题
                CASE (ticket_rec.device_ticket_seq % 5)
                    WHEN 1 THEN
                        symptom_title_text := '产品定位后X轴位置偏差' || (0.3 + (ticket_rec.device_ticket_seq * 0.1))::TEXT || 'mm，超出±0.2mm精度要求';
                        symptom_detail_text := '在' || COALESCE(industry_type_text, '自动化产线') || '测试中，产品通过定位机构移动到测试工位后，使用激光位移传感器检测X轴位置，发现实际位置与理论位置偏差' || (0.3 + (ticket_rec.device_ticket_seq * 0.1))::TEXT || 'mm，超出工艺要求的±0.2mm精度范围。该偏差导致测试探针无法准确接触测试点，造成测试失败率约' || (10 + ticket_rec.device_ticket_seq * 2)::TEXT || '%。检查定位机构机械结构，发现导轨存在轻微磨损，可能导致定位精度下降。同时检查伺服电机编码器反馈，发现反馈值正常，怀疑是机械传动链存在间隙或磨损。';
                        facts_json_text := json_build_object(
                            'mechanical', json_build_object(
                                'action_completed', 'YES',
                                'position_accuracy', 'NO',
                                'mechanical_wear', 'YES',
                                'lubrication_status', 'UNKNOWN',
                                'guide_rail_wear', 'YES',
                                'transmission_clearance', 'UNKNOWN'
                            ),
                            'electrical', json_build_object(
                                'sensor_signal', 'YES',
                                'motor_drive', 'YES',
                                'encoder_feedback', 'YES',
                                'servo_status', 'NORMAL'
                            ),
                            'environmental', json_build_object(
                                'temperature', 25 + (ticket_rec.device_ticket_seq % 5),
                                'humidity', 60 + (ticket_rec.device_ticket_seq % 10),
                                'vibration', 'LOW'
                            ),
                            'measurement', json_build_object(
                                'theoretical_position', 100.0,
                                'actual_position', 100.0 + (0.3 + ticket_rec.device_ticket_seq * 0.1),
                                'tolerance', 0.2,
                                'deviation', 0.3 + ticket_rec.device_ticket_seq * 0.1
                            )
                        )::TEXT;
                        actions_taken_array := ARRAY['检查定位机构机械结构', '检查导轨磨损情况', '检查伺服电机编码器反馈', '检查机械传动链间隙', '使用激光位移传感器复测位置精度'];
                        root_cause_text := '定位机构导轨存在磨损，导致定位精度下降。同时机械传动链存在间隙，在长期运行后间隙增大，影响定位精度。';
                    WHEN 2 THEN
                        symptom_title_text := '测试探针接触不良，接触电阻' || (50 + ticket_rec.device_ticket_seq * 10)::TEXT || 'mΩ，超出' || (30 + ticket_rec.device_ticket_seq * 5)::TEXT || 'mΩ要求';
                        symptom_detail_text := '在' || COALESCE(industry_type_text, '自动化产线') || 'FCT测试中，测试探针接触被测产品测试点时，测量接触电阻为' || (50 + ticket_rec.device_ticket_seq * 10)::TEXT || 'mΩ，超出工艺要求的' || (30 + ticket_rec.device_ticket_seq * 5)::TEXT || 'mΩ。该问题导致测试信号不稳定，测试数据异常，测试通过率下降约' || (8 + ticket_rec.device_ticket_seq)::TEXT || '%。检查探针表面，发现存在氧化层和污垢；检查探针压力，发现压力不足，可能因弹簧老化导致。同时检查产品测试点表面，发现部分测试点存在氧化。';
                        facts_json_text := json_build_object(
                            'mechanical', json_build_object(
                                'probe_contact', 'NO',
                                'probe_pressure', 'INSUFFICIENT',
                                'probe_wear', 'YES',
                                'spring_aging', 'YES'
                            ),
                            'electrical', json_build_object(
                                'contact_resistance', 50 + ticket_rec.device_ticket_seq * 10,
                                'required_resistance', 30 + ticket_rec.device_ticket_seq * 5,
                                'signal_stability', 'NO',
                                'test_point_oxidation', 'YES'
                            ),
                            'environmental', json_build_object(
                                'probe_contamination', 'YES',
                                'oxidation_level', 'MEDIUM',
                                'cleaning_frequency', 'LOW'
                            )
                        )::TEXT;
                        actions_taken_array := ARRAY['检查探针表面氧化情况', '检查探针压力', '清洁探针表面', '检查产品测试点表面', '更换老化弹簧', '调整探针压力'];
                        root_cause_text := '探针表面存在氧化层和污垢，同时探针弹簧老化导致压力不足，无法有效接触测试点，造成接触电阻过大。';
                    WHEN 3 THEN
                        symptom_title_text := '装配定位不准，Y轴偏差' || (0.4 + ticket_rec.device_ticket_seq * 0.15)::TEXT || 'mm';
                        symptom_detail_text := '在' || COALESCE(industry_type_text, '自动化产线') || '装配过程中，产品通过定位机构移动到装配工位后，使用视觉系统检测Y轴位置，发现实际位置与理论位置偏差' || (0.4 + ticket_rec.device_ticket_seq * 0.15)::TEXT || 'mm，超出工艺要求的±0.3mm精度范围。该偏差导致装配元件无法准确安装到目标位置，造成装配不良率约' || (12 + ticket_rec.device_ticket_seq * 2)::TEXT || '%。检查定位机构，发现Y轴导轨存在磨损；检查视觉系统标定，发现标定参数可能不准确；检查产品定位夹具，发现夹具存在松动。';
                        facts_json_text := json_build_object(
                            'mechanical', json_build_object(
                                'position_accuracy', 'NO',
                                'guide_rail_wear', 'YES',
                                'fixture_loose', 'YES',
                                'assembly_accuracy', 'NO'
                            ),
                            'vision', json_build_object(
                                'calibration_accurate', 'UNKNOWN',
                                'detection_working', 'YES',
                                'position_measurement', 'YES'
                            ),
                            'environmental', json_build_object(
                                'temperature', 24 + (ticket_rec.device_ticket_seq % 4),
                                'vibration', 'MEDIUM'
                            )
                        )::TEXT;
                        actions_taken_array := ARRAY['检查Y轴导轨磨损情况', '检查视觉系统标定参数', '检查产品定位夹具', '重新标定视觉系统', '紧固定位夹具'];
                        root_cause_text := 'Y轴导轨存在磨损，同时定位夹具松动，导致定位精度下降。视觉系统标定参数可能不准确，需要重新标定。';
                    WHEN 4 THEN
                        symptom_title_text := 'SMT贴片位置偏移，X轴偏移' || (0.25 + ticket_rec.device_ticket_seq * 0.1)::TEXT || 'mm，Y轴偏移' || (0.2 + ticket_rec.device_ticket_seq * 0.08)::TEXT || 'mm';
                        symptom_detail_text := '在' || COALESCE(industry_type_text, '自动化产线') || 'SMT贴片过程中，贴片机将元件贴装到PCB板上时，使用AOI检测发现元件位置偏移，X轴偏移' || (0.25 + ticket_rec.device_ticket_seq * 0.1)::TEXT || 'mm，Y轴偏移' || (0.2 + ticket_rec.device_ticket_seq * 0.08)::TEXT || 'mm，超出工艺要求的±0.15mm精度范围。该偏移导致元件焊接不良，造成焊接不良率约' || (5 + ticket_rec.device_ticket_seq)::TEXT || '%。检查贴片机机械结构，发现X/Y轴导轨存在磨损；检查贴片头，发现吸嘴存在磨损；检查视觉定位系统，发现定位精度下降。';
                        facts_json_text := json_build_object(
                            'mechanical', json_build_object(
                                'placement_accuracy', 'NO',
                                'x_axis_offset', 0.25 + ticket_rec.device_ticket_seq * 0.1,
                                'y_axis_offset', 0.2 + ticket_rec.device_ticket_seq * 0.08,
                                'tolerance', 0.15,
                                'guide_rail_wear', 'YES',
                                'nozzle_wear', 'YES'
                            ),
                            'vision', json_build_object(
                                'aoi_detection', 'YES',
                                'position_accuracy', 'NO',
                                'calibration_status', 'UNKNOWN'
                            ),
                            'quality', json_build_object(
                                'soldering_defect_rate', 5 + ticket_rec.device_ticket_seq,
                                'component_placement_ok', 'NO'
                            )
                        )::TEXT;
                        actions_taken_array := ARRAY['检查X/Y轴导轨磨损', '检查贴片头吸嘴磨损', '检查视觉定位系统', '重新校准贴片机', '更换磨损吸嘴'];
                        root_cause_text := 'X/Y轴导轨存在磨损，同时贴片头吸嘴磨损，导致贴片位置精度下降。视觉定位系统精度下降，需要重新校准。';
                    ELSE
                        symptom_title_text := '测试夹具磨损，定位精度下降' || (0.35 + ticket_rec.device_ticket_seq * 0.12)::TEXT || 'mm';
                        symptom_detail_text := '在' || COALESCE(industry_type_text, '自动化产线') || '测试过程中，使用测试夹具定位产品时，发现定位精度下降，偏差达到' || (0.35 + ticket_rec.device_ticket_seq * 0.12)::TEXT || 'mm，超出工艺要求的±0.25mm精度范围。该偏差导致测试探针无法准确接触测试点，造成测试失败率约' || (15 + ticket_rec.device_ticket_seq * 2)::TEXT || '%。检查测试夹具，发现定位销存在磨损；检查夹具定位面，发现存在磨损和划痕；检查夹具夹紧力，发现夹紧力不足，可能因气缸老化导致。';
                        facts_json_text := json_build_object(
                            'mechanical', json_build_object(
                                'fixture_wear', 'YES',
                                'positioning_pin_wear', 'YES',
                                'positioning_surface_wear', 'YES',
                                'clamping_force', 'INSUFFICIENT',
                                'cylinder_aging', 'YES'
                            ),
                            'quality', json_build_object(
                                'position_accuracy', 'NO',
                                'deviation', 0.35 + ticket_rec.device_ticket_seq * 0.12,
                                'tolerance', 0.25,
                                'test_failure_rate', 15 + ticket_rec.device_ticket_seq * 2
                            )
                        )::TEXT;
                        actions_taken_array := ARRAY['检查测试夹具磨损', '检查定位销磨损', '检查夹具定位面', '检查夹具夹紧力', '检查气缸状态', '更换磨损定位销'];
                        root_cause_text := '测试夹具定位销和定位面存在磨损，同时夹具气缸老化导致夹紧力不足，影响定位精度。';
                END CASE;
                step_name_text := '产品定位';
                
            WHEN 'B' THEN -- 电气/IO问题
                CASE (ticket_rec.device_ticket_seq % 5)
                    WHEN 1 THEN
                        symptom_title_text := '充放电测试电流波动' || (5 + ticket_rec.device_ticket_seq * 2)::TEXT || '%，超出±3%要求';
                        symptom_detail_text := '在' || COALESCE(industry_type_text, '锂电池EOL测试') || '充放电测试中，测试电流设定值为' || (10 + ticket_rec.device_ticket_seq)::TEXT || 'A，实际测量电流波动范围' || (5 + ticket_rec.device_ticket_seq * 2)::TEXT || '%，超出工艺要求的±3%范围。该波动导致测试数据不稳定，测试重复性差，测试通过率下降约' || (10 + ticket_rec.device_ticket_seq)::TEXT || '%。检查电流源，发现电流源输出不稳定；检查电流采集电路，发现采集精度不足；检查负载连接，发现连接电阻存在变化。';
                        facts_json_text := json_build_object(
                            'electrical', json_build_object(
                                'current_setpoint', 10 + ticket_rec.device_ticket_seq,
                                'current_fluctuation', 5 + ticket_rec.device_ticket_seq * 2,
                                'required_fluctuation', 3,
                                'current_source_stable', 'NO',
                                'acquisition_accuracy', 'INSUFFICIENT',
                                'load_connection_resistance', 'VARIABLE'
                            ),
                            'test', json_build_object(
                                'data_stability', 'NO',
                                'repeatability', 'POOR',
                                'test_pass_rate', 90 - ticket_rec.device_ticket_seq
                            )
                        )::TEXT;
                        actions_taken_array := ARRAY['检查电流源输出稳定性', '检查电流采集电路', '检查负载连接', '校准电流源', '检查采集电路精度'];
                        root_cause_text := '电流源输出不稳定，同时电流采集电路精度不足，导致测试电流波动超出要求范围。';
                    WHEN 2 THEN
                        symptom_title_text := '电压采集精度不足，误差' || (0.05 + ticket_rec.device_ticket_seq * 0.01)::TEXT || 'V，超出±0.02V要求';
                        symptom_detail_text := '在' || COALESCE(industry_type_text, '自动化产线') || '电压测试中，使用电压采集模块测量电池电压，设定值为' || (3.7 + ticket_rec.device_ticket_seq * 0.1)::TEXT || 'V，实际测量误差' || (0.05 + ticket_rec.device_ticket_seq * 0.01)::TEXT || 'V，超出工艺要求的±0.02V精度范围。该误差导致测试数据不准确，测试判定错误，测试通过率下降约' || (8 + ticket_rec.device_ticket_seq)::TEXT || '%。检查电压采集模块，发现采集精度不足；检查采集电路，发现存在温度漂移；检查校准参数，发现校准参数可能不准确。';
                        facts_json_text := json_build_object(
                            'electrical', json_build_object(
                                'voltage_setpoint', 3.7 + ticket_rec.device_ticket_seq * 0.1,
                                'measurement_error', 0.05 + ticket_rec.device_ticket_seq * 0.01,
                                'required_accuracy', 0.02,
                                'acquisition_accuracy', 'INSUFFICIENT',
                                'temperature_drift', 'YES',
                                'calibration_accurate', 'UNKNOWN'
                            ),
                            'test', json_build_object(
                                'data_accuracy', 'NO',
                                'judgment_error', 'YES',
                                'test_pass_rate', 92 - ticket_rec.device_ticket_seq
                            )
                        )::TEXT;
                        actions_taken_array := ARRAY['检查电压采集模块精度', '检查采集电路温度漂移', '检查校准参数', '重新校准电压采集模块', '检查温度补偿'];
                        root_cause_text := '电压采集模块精度不足，同时采集电路存在温度漂移，导致电压测量误差超出要求范围。';
                    WHEN 3 THEN
                        symptom_title_text := '温度传感器故障，读数异常波动' || (3 + ticket_rec.device_ticket_seq)::TEXT || '°C';
                        symptom_detail_text := '在' || COALESCE(industry_type_text, '自动化产线') || '温度测试中，使用温度传感器测量产品温度，发现温度读数异常波动，波动范围' || (3 + ticket_rec.device_ticket_seq)::TEXT || '°C，超出工艺要求的±1°C稳定性范围。该波动导致温度控制不稳定，测试数据异常，测试通过率下降约' || (12 + ticket_rec.device_ticket_seq)::TEXT || '%。检查温度传感器，发现传感器信号不稳定；检查传感器接线，发现存在接触不良；检查传感器供电，发现供电电压波动。';
                        facts_json_text := json_build_object(
                            'electrical', json_build_object(
                                'sensor_signal_stable', 'NO',
                                'temperature_fluctuation', 3 + ticket_rec.device_ticket_seq,
                                'required_stability', 1,
                                'wiring_ok', 'NO',
                                'power_voltage_stable', 'NO'
                            ),
                            'test', json_build_object(
                                'temperature_control_stable', 'NO',
                                'data_abnormal', 'YES',
                                'test_pass_rate', 88 - ticket_rec.device_ticket_seq
                            )
                        )::TEXT;
                        actions_taken_array := ARRAY['检查温度传感器信号', '检查传感器接线', '检查传感器供电', '更换温度传感器', '检查供电稳定性'];
                        root_cause_text := '温度传感器信号不稳定，同时传感器接线存在接触不良，供电电压波动，导致温度读数异常波动。';
                    WHEN 4 THEN
                        symptom_title_text := '编码器信号丢失，位置反馈异常';
                        symptom_detail_text := '在' || COALESCE(industry_type_text, '自动化产线') || '运动控制中，使用编码器反馈电机位置，发现编码器信号间歇性丢失，导致位置反馈异常，运动控制精度下降。该问题导致定位不准确，动作执行失败，设备停机率约' || (5 + ticket_rec.device_ticket_seq)::TEXT || '%。检查编码器，发现编码器信号输出不稳定；检查编码器接线，发现存在接触不良；检查编码器供电，发现供电电压不足；检查编码器安装，发现安装松动。';
                        facts_json_text := json_build_object(
                            'electrical', json_build_object(
                                'encoder_signal_stable', 'NO',
                                'signal_loss', 'YES',
                                'position_feedback_ok', 'NO',
                                'wiring_ok', 'NO',
                                'power_voltage_sufficient', 'NO',
                                'installation_loose', 'YES'
                            ),
                            'mechanical', json_build_object(
                                'position_accuracy', 'NO',
                                'motion_control_accuracy', 'NO',
                                'action_execution_ok', 'NO'
                            ),
                            'system', json_build_object(
                                'equipment_downtime_rate', 5 + ticket_rec.device_ticket_seq
                            )
                        )::TEXT;
                        actions_taken_array := ARRAY['检查编码器信号输出', '检查编码器接线', '检查编码器供电', '检查编码器安装', '紧固编码器安装', '检查信号线屏蔽'];
                        root_cause_text := '编码器信号输出不稳定，同时编码器接线存在接触不良，安装松动，导致编码器信号间歇性丢失。';
                    ELSE
                        symptom_title_text := '继电器触点粘连，无法正常断开';
                        symptom_detail_text := '在' || COALESCE(industry_type_text, '自动化产线') || '控制电路中，使用继电器控制负载通断，发现继电器触点粘连，无法正常断开，导致负载无法断电。该问题导致设备无法正常停止，存在安全隐患，设备停机率约' || (8 + ticket_rec.device_ticket_seq)::TEXT || '%。检查继电器，发现触点存在烧蚀和粘连；检查继电器负载，发现负载电流过大；检查继电器选型，发现继电器容量不足。';
                        facts_json_text := json_build_object(
                            'electrical', json_build_object(
                                'relay_contact_ok', 'NO',
                                'contact_welding', 'YES',
                                'contact_burn', 'YES',
                                'load_current', 'EXCESSIVE',
                                'relay_capacity', 'INSUFFICIENT'
                            ),
                            'safety', json_build_object(
                                'equipment_stop_ok', 'NO',
                                'safety_hazard', 'YES'
                            ),
                            'system', json_build_object(
                                'equipment_downtime_rate', 8 + ticket_rec.device_ticket_seq
                            )
                        )::TEXT;
                        actions_taken_array := ARRAY['检查继电器触点', '检查继电器负载电流', '检查继电器选型', '更换继电器', '检查负载电流', '重新选型继电器'];
                        root_cause_text := '继电器触点存在烧蚀和粘连，同时继电器容量不足，无法承受负载电流，导致触点粘连无法正常断开。';
                END CASE;
                step_name_text := '电气测试';
                
            WHEN 'C' THEN -- PLC/程序问题
                CASE (ticket_rec.device_ticket_seq % 5)
                    WHEN 1 THEN
                        symptom_title_text := '温度测试超时，超时时间' || (30 + ticket_rec.device_ticket_seq * 5)::TEXT || '秒';
                        symptom_detail_text := '在' || COALESCE(industry_type_text, '自动化产线') || '温度测试中，PLC程序执行温度测试流程时，发现测试超时，超时时间' || (30 + ticket_rec.device_ticket_seq * 5)::TEXT || '秒，超出设定的' || (20 + ticket_rec.device_ticket_seq * 3)::TEXT || '秒超时限制。该超时导致测试流程中断，测试失败，测试通过率下降约' || (15 + ticket_rec.device_ticket_seq)::TEXT || '%。检查PLC程序逻辑，发现超时处理逻辑存在问题；检查温度控制，发现温度上升速度慢；检查测试流程，发现流程设计不合理。';
                        facts_json_text := json_build_object(
                            'plc', json_build_object(
                                'timeout_handling', 'NO',
                                'timeout_occurred', 'YES',
                                'timeout_duration', 30 + ticket_rec.device_ticket_seq * 5,
                                'timeout_limit', 20 + ticket_rec.device_ticket_seq * 3,
                                'program_logic_ok', 'NO'
                            ),
                            'control', json_build_object(
                                'temperature_rise_speed', 'SLOW',
                                'temperature_control_ok', 'UNKNOWN'
                            ),
                            'process', json_build_object(
                                'process_design_reasonable', 'NO',
                                'test_interrupted', 'YES'
                            )
                        )::TEXT;
                        actions_taken_array := ARRAY['检查PLC程序超时处理逻辑', '检查温度控制', '检查温度上升速度', '检查测试流程设计', '优化超时处理逻辑'];
                        root_cause_text := 'PLC程序超时处理逻辑存在问题，同时温度上升速度慢，测试流程设计不合理，导致测试超时。';
                    WHEN 2 THEN
                        symptom_title_text := '程序死循环，CPU占用率100%';
                        symptom_detail_text := '在' || COALESCE(industry_type_text, '自动化产线') || '运行过程中，PLC程序执行时发现程序进入死循环，CPU占用率达到100%，导致PLC无法响应其他任务，设备无法正常运行。该问题导致设备停机，影响生产，设备停机率约' || (20 + ticket_rec.device_ticket_seq * 3)::TEXT || '%。检查PLC程序逻辑，发现循环条件判断错误；检查程序流程，发现缺少退出条件；检查程序变量，发现变量值异常。';
                        facts_json_text := json_build_object(
                            'plc', json_build_object(
                                'dead_loop', 'YES',
                                'cpu_usage', 100,
                                'program_logic_ok', 'NO',
                                'loop_condition_correct', 'NO',
                                'exit_condition_exists', 'NO'
                            ),
                            'system', json_build_object(
                                'plc_responsive', 'NO',
                                'equipment_running', 'NO',
                                'equipment_downtime_rate', 20 + ticket_rec.device_ticket_seq * 3
                            ),
                            'variables', json_build_object(
                                'variable_values_normal', 'NO'
                            )
                        )::TEXT;
                        actions_taken_array := ARRAY['检查PLC程序循环逻辑', '检查循环条件判断', '检查退出条件', '检查程序变量', '修复程序逻辑', '重启PLC'];
                        root_cause_text := 'PLC程序循环条件判断错误，缺少退出条件，导致程序进入死循环，CPU占用率达到100%。';
                    WHEN 3 THEN
                        symptom_title_text := '通信协议不匹配，通信失败率' || (25 + ticket_rec.device_ticket_seq * 3)::TEXT || '%';
                        symptom_detail_text := '在' || COALESCE(industry_type_text, '自动化产线') || '通信中，PLC与上位机通信时，发现通信协议不匹配，通信失败率达到' || (25 + ticket_rec.device_ticket_seq * 3)::TEXT || '%，导致数据无法正常传输，设备无法正常控制。该问题导致设备控制异常，数据采集失败，设备停机率约' || (10 + ticket_rec.device_ticket_seq)::TEXT || '%。检查通信协议配置，发现协议版本不匹配；检查通信参数，发现波特率、数据位等参数不一致；检查通信程序，发现程序实现不正确。';
                        facts_json_text := json_build_object(
                            'communication', json_build_object(
                                'protocol_match', 'NO',
                                'communication_failure_rate', 25 + ticket_rec.device_ticket_seq * 3,
                                'protocol_version_match', 'NO',
                                'baud_rate_match', 'UNKNOWN',
                                'data_bits_match', 'UNKNOWN',
                                'program_implementation_correct', 'NO'
                            ),
                            'system', json_build_object(
                                'data_transmission_ok', 'NO',
                                'equipment_control_ok', 'NO',
                                'data_acquisition_ok', 'NO',
                                'equipment_downtime_rate', 10 + ticket_rec.device_ticket_seq
                            )
                        )::TEXT;
                        actions_taken_array := ARRAY['检查通信协议配置', '检查协议版本', '检查通信参数', '检查波特率设置', '检查通信程序', '统一通信协议'];
                        root_cause_text := '通信协议版本不匹配，同时通信参数（波特率、数据位等）不一致，通信程序实现不正确，导致通信失败。';
                    WHEN 4 THEN
                        symptom_title_text := '测试序列执行错误，步骤' || (3 + ticket_rec.device_ticket_seq % 5)::TEXT || '未执行';
                        symptom_detail_text := '在' || COALESCE(industry_type_text, '自动化产线') || '测试中，PLC程序执行测试序列时，发现步骤' || (3 + ticket_rec.device_ticket_seq % 5)::TEXT || '未执行，导致测试流程不完整，测试数据不完整。该问题导致测试结果不准确，测试判定错误，测试通过率下降约' || (18 + ticket_rec.device_ticket_seq * 2)::TEXT || '%。检查PLC程序逻辑，发现测试序列执行逻辑存在问题；检查步骤跳转条件，发现条件判断错误；检查程序流程，发现流程设计不合理。';
                        facts_json_text := json_build_object(
                            'plc', json_build_object(
                                'test_sequence_execution_ok', 'NO',
                                'step_executed', 'NO',
                                'step_number', 3 + ticket_rec.device_ticket_seq % 5,
                                'execution_logic_ok', 'NO',
                                'step_jump_condition_correct', 'NO',
                                'process_design_reasonable', 'NO'
                            ),
                            'test', json_build_object(
                                'test_flow_complete', 'NO',
                                'test_data_complete', 'NO',
                                'test_result_accurate', 'NO',
                                'judgment_correct', 'NO',
                                'test_pass_rate', 82 - ticket_rec.device_ticket_seq * 2
                            )
                        )::TEXT;
                        actions_taken_array := ARRAY['检查测试序列执行逻辑', '检查步骤跳转条件', '检查程序流程', '修复程序逻辑', '完善测试序列'];
                        root_cause_text := 'PLC程序测试序列执行逻辑存在问题，步骤跳转条件判断错误，导致测试步骤未执行，测试流程不完整。';
                    ELSE
                        symptom_title_text := '通信波特率错误，通信超时' || (5 + ticket_rec.device_ticket_seq)::TEXT || '秒';
                        symptom_detail_text := '在' || COALESCE(industry_type_text, '自动化产线') || '通信中，PLC与设备通信时，发现通信波特率设置错误，导致通信超时，超时时间' || (5 + ticket_rec.device_ticket_seq)::TEXT || '秒，超出设定的' || (3 + ticket_rec.device_ticket_seq)::TEXT || '秒超时限制。该超时导致通信失败，数据无法传输，设备无法正常控制。检查通信配置，发现波特率设置不正确；检查通信参数，发现其他参数也可能不匹配；检查通信程序，发现程序实现可能存在问题。';
                        facts_json_text := json_build_object(
                            'communication', json_build_object(
                                'baud_rate_correct', 'NO',
                                'communication_timeout', 'YES',
                                'timeout_duration', 5 + ticket_rec.device_ticket_seq,
                                'timeout_limit', 3 + ticket_rec.device_ticket_seq,
                                'other_parameters_match', 'UNKNOWN',
                                'program_implementation_ok', 'UNKNOWN'
                            ),
                            'system', json_build_object(
                                'communication_ok', 'NO',
                                'data_transmission_ok', 'NO',
                                'equipment_control_ok', 'NO'
                            )
                        )::TEXT;
                        actions_taken_array := ARRAY['检查通信波特率设置', '检查通信参数', '检查通信程序', '修正波特率设置', '统一通信参数'];
                        root_cause_text := '通信波特率设置错误，导致通信超时，通信失败，数据无法传输。检查通信配置发现波特率参数与设备要求不匹配，同时通信程序中的超时处理机制可能存在问题，无法及时检测和处理通信异常。';
                END CASE;
                step_name_text := '程序控制';
                
            WHEN 'D' THEN -- 测试/判定问题
                CASE (ticket_rec.device_ticket_seq % 5)
                    WHEN 1 THEN
                        symptom_title_text := '扭矩测试数据异常，重复性差，CV值' || (8 + ticket_rec.device_ticket_seq * 2)::TEXT || '%';
                        symptom_detail_text := '在' || COALESCE(industry_type_text, '自动化产线') || '扭矩测试中，对同一产品进行多次扭矩测试，发现测试数据重复性差，变异系数（CV值）达到' || (8 + ticket_rec.device_ticket_seq * 2)::TEXT || '%，超出工艺要求的5%范围。该问题导致测试数据不可靠，测试判定不准确，测试通过率下降约' || (12 + ticket_rec.device_ticket_seq * 2)::TEXT || '%。检查扭矩传感器，发现传感器精度不足；检查测试条件，发现测试条件不稳定；检查测试程序，发现测试程序逻辑可能存在问题。';
                        facts_json_text := json_build_object(
                            'test', json_build_object(
                                'torque_data_repeatability', 'POOR',
                                'cv_value', 8 + ticket_rec.device_ticket_seq * 2,
                                'required_cv', 5,
                                'data_reliable', 'NO',
                                'judgment_accurate', 'NO',
                                'test_pass_rate', 88 - ticket_rec.device_ticket_seq * 2
                            ),
                            'sensor', json_build_object(
                                'torque_sensor_accuracy', 'INSUFFICIENT'
                            ),
                            'condition', json_build_object(
                                'test_condition_stable', 'NO'
                            ),
                            'program', json_build_object(
                                'test_program_logic_ok', 'UNKNOWN'
                            )
                        )::TEXT;
                        actions_taken_array := ARRAY['检查扭矩传感器精度', '检查测试条件稳定性', '检查测试程序逻辑', '校准扭矩传感器', '稳定测试条件'];
                        root_cause_text := '扭矩传感器精度不足，同时测试条件不稳定，导致测试数据重复性差，CV值超出要求范围。';
                    WHEN 2 THEN
                        symptom_title_text := '内阻测试重复性差，标准差' || (0.5 + ticket_rec.device_ticket_seq * 0.1)::TEXT || 'mΩ';
                        symptom_detail_text := '在' || COALESCE(industry_type_text, '锂电池EOL测试') || '内阻测试中，对同一电池进行多次内阻测试，发现测试数据重复性差，标准差达到' || (0.5 + ticket_rec.device_ticket_seq * 0.1)::TEXT || 'mΩ，超出工艺要求的0.3mΩ范围。该问题导致测试数据不可靠，测试判定不准确，测试通过率下降约' || (10 + ticket_rec.device_ticket_seq * 2)::TEXT || '%。检查内阻测试仪，发现测试仪精度不足；检查测试探针接触，发现接触不稳定；检查测试程序，发现测试程序逻辑可能存在问题。';
                        facts_json_text := json_build_object(
                            'test', json_build_object(
                                'resistance_data_repeatability', 'POOR',
                                'standard_deviation', 0.5 + ticket_rec.device_ticket_seq * 0.1,
                                'required_std', 0.3,
                                'data_reliable', 'NO',
                                'judgment_accurate', 'NO',
                                'test_pass_rate', 90 - ticket_rec.device_ticket_seq * 2
                            ),
                            'equipment', json_build_object(
                                'resistance_tester_accuracy', 'INSUFFICIENT'
                            ),
                            'mechanical', json_build_object(
                                'probe_contact_stable', 'NO'
                            ),
                            'program', json_build_object(
                                'test_program_logic_ok', 'UNKNOWN'
                            )
                        )::TEXT;
                        actions_taken_array := ARRAY['检查内阻测试仪精度', '检查测试探针接触', '检查测试程序逻辑', '校准内阻测试仪', '改善探针接触'];
                        root_cause_text := '内阻测试仪精度不足，同时测试探针接触不稳定，导致测试数据重复性差，标准差超出要求范围。';
                    WHEN 3 THEN
                        symptom_title_text := '过流保护误触发，触发电流' || (15 + ticket_rec.device_ticket_seq * 2)::TEXT || 'A';
                        symptom_detail_text := '在' || COALESCE(industry_type_text, '自动化产线') || '测试中，过流保护功能误触发，触发电流' || (15 + ticket_rec.device_ticket_seq * 2)::TEXT || 'A，低于设定的' || (20 + ticket_rec.device_ticket_seq * 2)::TEXT || 'A保护阈值，导致正常测试被中断，测试失败。该问题导致测试通过率下降约' || (8 + ticket_rec.device_ticket_seq)::TEXT || '%。检查过流保护逻辑，发现保护判定逻辑存在问题；检查电流采集，发现采集精度不足；检查保护参数，发现保护参数设置不合理。';
                        facts_json_text := json_build_object(
                            'protection', json_build_object(
                                'overcurrent_protection_triggered', 'YES',
                                'false_trigger', 'YES',
                                'trigger_current', 15 + ticket_rec.device_ticket_seq * 2,
                                'protection_threshold', 20 + ticket_rec.device_ticket_seq * 2,
                                'protection_logic_ok', 'NO',
                                'parameter_setting_reasonable', 'NO'
                            ),
                            'acquisition', json_build_object(
                                'current_acquisition_accuracy', 'INSUFFICIENT'
                            ),
                            'test', json_build_object(
                                'test_interrupted', 'YES',
                                'test_failed', 'YES',
                                'test_pass_rate', 92 - ticket_rec.device_ticket_seq
                            )
                        )::TEXT;
                        actions_taken_array := ARRAY['检查过流保护逻辑', '检查电流采集精度', '检查保护参数设置', '优化保护逻辑', '校准电流采集'];
                        root_cause_text := '过流保护判定逻辑存在问题，同时电流采集精度不足，保护参数设置不合理，导致过流保护误触发。';
                    WHEN 4 THEN
                        symptom_title_text := '容量测试数据异常，偏差' || (5 + ticket_rec.device_ticket_seq)::TEXT || '%';
                        symptom_detail_text := '在' || COALESCE(industry_type_text, '锂电池EOL测试') || '容量测试中，测试容量数据异常，与标准值偏差' || (5 + ticket_rec.device_ticket_seq)::TEXT || '%，超出工艺要求的±2%范围。该偏差导致测试判定错误，合格产品被判定为不合格，或不合格产品被判定为合格，测试准确率下降约' || (15 + ticket_rec.device_ticket_seq * 2)::TEXT || '%。检查容量测试仪，发现测试仪精度不足；检查测试条件，发现测试条件不稳定；检查测试程序，发现测试程序逻辑可能存在问题。';
                        facts_json_text := json_build_object(
                            'test', json_build_object(
                                'capacity_data_abnormal', 'YES',
                                'deviation', 5 + ticket_rec.device_ticket_seq,
                                'required_accuracy', 2,
                                'judgment_error', 'YES',
                                'test_accuracy', 85 - ticket_rec.device_ticket_seq * 2
                            ),
                            'equipment', json_build_object(
                                'capacity_tester_accuracy', 'INSUFFICIENT'
                            ),
                            'condition', json_build_object(
                                'test_condition_stable', 'NO'
                            ),
                            'program', json_build_object(
                                'test_program_logic_ok', 'UNKNOWN'
                            )
                        )::TEXT;
                        actions_taken_array := ARRAY['检查容量测试仪精度', '检查测试条件稳定性', '检查测试程序逻辑', '校准容量测试仪', '稳定测试条件'];
                        root_cause_text := '容量测试仪精度不足，同时测试条件不稳定，导致容量测试数据异常，偏差超出要求范围。';
                    ELSE
                        symptom_title_text := '测试结果判定错误，误判率' || (8 + ticket_rec.device_ticket_seq)::TEXT || '%';
                        symptom_detail_text := '在' || COALESCE(industry_type_text, '自动化产线') || '测试中，测试结果判定逻辑存在问题，误判率达到' || (8 + ticket_rec.device_ticket_seq)::TEXT || '%，导致合格产品被判定为不合格，或不合格产品被判定为合格。该问题导致测试准确率下降，影响产品质量控制。检查判定逻辑，发现判定条件设置不合理；检查判定参数，发现参数阈值设置错误；检查判定程序，发现程序实现可能存在问题。';
                        facts_json_text := json_build_object(
                            'judgment', json_build_object(
                                'judgment_logic_ok', 'NO',
                                'false_positive_rate', 8 + ticket_rec.device_ticket_seq,
                                'judgment_condition_reasonable', 'NO',
                                'parameter_threshold_correct', 'NO',
                                'program_implementation_ok', 'UNKNOWN'
                            ),
                            'quality', json_build_object(
                                'test_accuracy', 'NO',
                                'quality_control_affected', 'YES'
                            )
                        )::TEXT;
                        actions_taken_array := ARRAY['检查判定逻辑', '检查判定条件', '检查判定参数阈值', '检查判定程序', '优化判定逻辑', '修正参数阈值'];
                        root_cause_text := '测试结果判定逻辑存在问题，判定条件设置不合理，参数阈值设置错误，导致测试结果判定错误，误判率过高。';
                END CASE;
                step_name_text := '测试判定';
                
            ELSE -- 'E' 系统/环境问题
                CASE (ticket_rec.device_ticket_seq % 5)
                    WHEN 1 THEN
                        symptom_title_text := '通信超时，超时时间' || (30 + ticket_rec.device_ticket_seq * 5)::TEXT || '秒';
                        symptom_detail_text := '在' || COALESCE(industry_type_text, '自动化产线') || '运行过程中，系统与设备通信时，发现通信超时，超时时间' || (30 + ticket_rec.device_ticket_seq * 5)::TEXT || '秒，超出设定的' || (20 + ticket_rec.device_ticket_seq * 3)::TEXT || '秒超时限制。该超时导致数据无法正常传输，设备无法正常控制，设备停机率约' || (5 + ticket_rec.device_ticket_seq)::TEXT || '%。检查网络连接，发现网络连接不稳定；检查通信协议，发现协议可能不匹配；检查通信程序，发现程序实现可能存在问题；检查设备状态，发现设备可能异常。';
                        facts_json_text := json_build_object(
                            'communication', json_build_object(
                                'timeout_occurred', 'YES',
                                'timeout_duration', 30 + ticket_rec.device_ticket_seq * 5,
                                'timeout_limit', 20 + ticket_rec.device_ticket_seq * 3,
                                'network_connection_stable', 'NO',
                                'protocol_match', 'UNKNOWN',
                                'program_implementation_ok', 'UNKNOWN'
                            ),
                            'equipment', json_build_object(
                                'equipment_status_normal', 'UNKNOWN'
                            ),
                            'system', json_build_object(
                                'data_transmission_ok', 'NO',
                                'equipment_control_ok', 'NO',
                                'equipment_downtime_rate', 5 + ticket_rec.device_ticket_seq
                            )
                        )::TEXT;
                        actions_taken_array := ARRAY['检查网络连接', '检查通信协议', '检查通信程序', '检查设备状态', '优化网络连接', '检查通信超时设置'];
                        root_cause_text := '网络连接不稳定，同时通信协议可能不匹配，通信程序实现可能存在问题，导致通信超时。';
                    WHEN 2 THEN
                        symptom_title_text := '显示界面乱码，无法正常显示';
                        symptom_detail_text := '在' || COALESCE(industry_type_text, '自动化产线') || '运行过程中，上位机显示界面出现乱码，无法正常显示设备状态和测试数据，导致操作人员无法正常监控设备运行状态。该问题影响设备操作和监控，设备停机率约' || (3 + ticket_rec.device_ticket_seq)::TEXT || '%。检查显示程序，发现程序编码可能存在问题；检查字体设置，发现字体可能不支持；检查系统编码，发现系统编码设置可能不正确；检查数据格式，发现数据格式可能异常。';
                        facts_json_text := json_build_object(
                            'display', json_build_object(
                                'display_normal', 'NO',
                                'garbled_text', 'YES',
                                'program_encoding_ok', 'UNKNOWN',
                                'font_supported', 'UNKNOWN',
                                'system_encoding_correct', 'UNKNOWN',
                                'data_format_normal', 'UNKNOWN'
                            ),
                            'system', json_build_object(
                                'monitoring_ok', 'NO',
                                'operation_affected', 'YES',
                                'equipment_downtime_rate', 3 + ticket_rec.device_ticket_seq
                            )
                        )::TEXT;
                        actions_taken_array := ARRAY['检查显示程序编码', '检查字体设置', '检查系统编码设置', '检查数据格式', '修复程序编码', '调整系统编码'];
                        root_cause_text := '显示程序编码存在问题，同时系统编码设置可能不正确，导致显示界面出现乱码，无法正常显示。';
                    WHEN 3 THEN
                        symptom_title_text := '数据存储失败，存储错误率' || (12 + ticket_rec.device_ticket_seq * 2)::TEXT || '%';
                        symptom_detail_text := '在' || COALESCE(industry_type_text, '自动化产线') || '运行过程中，测试数据存储时，发现数据存储失败，存储错误率达到' || (12 + ticket_rec.device_ticket_seq * 2)::TEXT || '%，导致测试数据丢失，无法追溯。该问题影响数据管理和追溯，数据完整性下降。检查存储设备，发现存储设备可能异常；检查存储程序，发现程序实现可能存在问题；检查存储空间，发现存储空间可能不足；检查数据格式，发现数据格式可能异常。';
                        facts_json_text := json_build_object(
                            'storage', json_build_object(
                                'storage_ok', 'NO',
                                'storage_error_rate', 12 + ticket_rec.device_ticket_seq * 2,
                                'storage_device_normal', 'UNKNOWN',
                                'program_implementation_ok', 'UNKNOWN',
                                'storage_space_sufficient', 'UNKNOWN',
                                'data_format_normal', 'UNKNOWN'
                            ),
                            'data', json_build_object(
                                'data_lost', 'YES',
                                'data_traceable', 'NO',
                                'data_integrity', 'NO'
                            )
                        )::TEXT;
                        actions_taken_array := ARRAY['检查存储设备', '检查存储程序', '检查存储空间', '检查数据格式', '修复存储程序', '清理存储空间'];
                        root_cause_text := '存储设备可能异常，同时存储程序实现可能存在问题，存储空间可能不足，导致数据存储失败。';
                    WHEN 4 THEN
                        symptom_title_text := '测试数据同步失败，同步错误率' || (10 + ticket_rec.device_ticket_seq * 2)::TEXT || '%';
                        symptom_detail_text := '在' || COALESCE(industry_type_text, '自动化产线') || '运行过程中，测试数据同步到服务器时，发现数据同步失败，同步错误率达到' || (10 + ticket_rec.device_ticket_seq * 2)::TEXT || '%，导致数据无法及时同步，数据不一致。该问题影响数据管理和追溯，数据完整性下降。检查网络连接，发现网络连接可能不稳定；检查同步程序，发现程序实现可能存在问题；检查服务器状态，发现服务器可能异常；检查数据格式，发现数据格式可能异常。';
                        facts_json_text := json_build_object(
                            'sync', json_build_object(
                                'sync_ok', 'NO',
                                'sync_error_rate', 10 + ticket_rec.device_ticket_seq * 2,
                                'network_connection_stable', 'UNKNOWN',
                                'program_implementation_ok', 'UNKNOWN',
                                'server_status_normal', 'UNKNOWN',
                                'data_format_normal', 'UNKNOWN'
                            ),
                            'data', json_build_object(
                                'data_synced', 'NO',
                                'data_consistent', 'NO',
                                'data_integrity', 'NO'
                            )
                        )::TEXT;
                        actions_taken_array := ARRAY['检查网络连接', '检查同步程序', '检查服务器状态', '检查数据格式', '优化网络连接', '修复同步程序'];
                        root_cause_text := '网络连接可能不稳定，同时同步程序实现可能存在问题，服务器可能异常，导致测试数据同步失败。';
                    ELSE
                        symptom_title_text := '偶发故障，故障频率' || (2 + ticket_rec.device_ticket_seq % 3)::TEXT || '次/天';
                        symptom_detail_text := '在' || COALESCE(industry_type_text, '自动化产线') || '运行过程中，设备出现偶发故障，故障频率' || (2 + ticket_rec.device_ticket_seq % 3)::TEXT || '次/天，故障现象不固定，有时表现为通信异常，有时表现为动作异常，有时表现为数据异常。该问题导致设备运行不稳定，影响生产效率，设备停机率约' || (3 + ticket_rec.device_ticket_seq % 3)::TEXT || '%。检查设备状态，发现设备状态可能异常；检查环境因素，发现环境因素可能影响；检查系统配置，发现系统配置可能不合理；检查程序逻辑，发现程序逻辑可能存在问题。';
                        facts_json_text := json_build_object(
                            'system', json_build_object(
                                'intermittent_fault', 'YES',
                                'fault_frequency', 2 + ticket_rec.device_ticket_seq % 3,
                                'fault_symptom_fixed', 'NO',
                                'equipment_status_normal', 'UNKNOWN',
                                'system_configuration_reasonable', 'UNKNOWN',
                                'program_logic_ok', 'UNKNOWN'
                            ),
                            'environmental', json_build_object(
                                'environmental_factors_affect', 'UNKNOWN'
                            ),
                            'production', json_build_object(
                                'equipment_stable', 'NO',
                                'production_efficiency_affected', 'YES',
                                'equipment_downtime_rate', 3 + ticket_rec.device_ticket_seq % 3
                            )
                        )::TEXT;
                        actions_taken_array := ARRAY['检查设备状态', '检查环境因素', '检查系统配置', '检查程序逻辑', '优化系统配置', '改善环境条件'];
                        root_cause_text := '设备状态可能异常，同时环境因素可能影响，系统配置可能不合理，导致设备出现偶发故障。';
                END CASE;
                step_name_text := '系统运行';
        END CASE;
        
        -- 生成环境描述
        env_description_text := CASE 
            WHEN ticket_rec.domain IN ('A', 'B') THEN 
                '环境温度：' || (24 + (ticket_rec.device_ticket_seq % 5))::TEXT || '°C，相对湿度：' || (60 + (ticket_rec.device_ticket_seq % 10))::TEXT || '%，振动：低，无异常环境因素。'
            WHEN ticket_rec.domain = 'C' THEN
                '运行环境正常，无异常环境因素影响。'
            WHEN ticket_rec.domain = 'D' THEN
                '测试环境：温度' || (23 + (ticket_rec.device_ticket_seq % 4))::TEXT || '°C，湿度' || (55 + (ticket_rec.device_ticket_seq % 8))::TEXT || '%，测试条件基本稳定。'
            ELSE
                '系统运行环境：温度' || (25 + (ticket_rec.device_ticket_seq % 3))::TEXT || '°C，湿度' || (65 + (ticket_rec.device_ticket_seq % 5))::TEXT || '%，网络环境正常。'
        END;
        
        -- 更新工单信息
        UPDATE tickets SET
            symptom_title = symptom_title_text,
            symptom_detail = symptom_detail_text,
            step_name = step_name_text,
            facts_json = facts_json_text::JSONB,
            actions_taken = actions_taken_array,
            actions_taken_note = CASE 
                WHEN array_length(actions_taken_array, 1) > 0 THEN 
                    '已执行上述动作，问题正在处理中。现场工程师已记录相关日志和测试数据，待技术部门进一步分析。'
                ELSE NULL 
            END,
            env_related = CASE ticket_rec.domain WHEN 'A' THEN TRUE WHEN 'B' THEN TRUE WHEN 'D' THEN TRUE ELSE FALSE END,
            root_cause = root_cause_text
        WHERE ticket_id = ticket_rec.ticket_id;
        
        updated_count := updated_count + 1;
    END LOOP;
    
    RAISE NOTICE '已更新 % 个工单的行业特定详细信息', updated_count;
END $$;

