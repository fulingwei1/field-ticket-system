-- 为所有有工单的工程师计算2025年12月的绩效数据
-- 注意：这个脚本需要调用API来计算，这里只是准备数据

-- 获取所有有工单的工程师ID
DO $$
DECLARE
    engineer_record RECORD;
    period_start DATE := '2025-12-01';
    period_end DATE := '2025-12-31';
    period_type VARCHAR := 'monthly';
BEGIN
    -- 遍历所有有工单的工程师
    FOR engineer_record IN 
        SELECT DISTINCT created_by_user_id as engineer_id
        FROM tickets
        WHERE status != 'Draft'
        AND created_by_user_id IS NOT NULL
    LOOP
        -- 这里只是记录需要计算的工程师
        -- 实际计算需要通过API调用 PerformanceService.CalculateMetricsAsync
        RAISE NOTICE '需要为工程师 % 计算绩效 (周期: % 到 %)', 
            engineer_record.engineer_id, 
            period_start, 
            period_end;
    END LOOP;
END $$;

-- 注意：实际的绩效计算需要通过API端点 /api/performance/calculate 来完成
-- 因为计算逻辑复杂，涉及多个表的关联查询和计算




