-- Backfill device.project_id and project.customer_name using existing data.
-- 1) Fill project.customer_name from customers.
-- 2) Fill devices.project_id from recent tickets (most recent ticket per device).
-- 3) Fill remaining devices by matching device_sn to project_no.

-- 1) Projects: ensure customer_name is populated.
UPDATE projects p
SET customer_name = c.customer_name,
    updated_at = NOW()
FROM customers c
WHERE p.customer_id = c.customer_id
  AND (p.customer_name IS NULL OR p.customer_name = '');

-- 2) Devices: use latest ticket's project_id.
WITH ranked AS (
    SELECT
        t.device_id,
        t.project_id,
        ROW_NUMBER() OVER (
            PARTITION BY t.device_id
            ORDER BY t.created_at DESC
        ) AS rn
    FROM tickets t
    WHERE t.project_id IS NOT NULL
)
UPDATE devices d
SET project_id = r.project_id,
    updated_at = NOW()
FROM ranked r
WHERE d.device_id = r.device_id
  AND d.project_id IS NULL
  AND r.rn = 1;

-- 3) Devices: match device_sn contains project_no (e.g. DEV-PRJ003-001-001).
UPDATE devices d
SET project_id = p.project_id,
    updated_at = NOW()
FROM projects p
WHERE d.project_id IS NULL
  AND d.device_sn IS NOT NULL
  AND p.project_no IS NOT NULL
  AND d.device_sn ILIKE '%' || p.project_no || '%';

-- 4) Remaining devices without project_id (inspect count).
SELECT COUNT(*) AS devices_without_project
FROM devices
WHERE project_id IS NULL;
