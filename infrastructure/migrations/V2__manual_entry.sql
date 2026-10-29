-- V2__manual_entry.sql
ALTER TABLE sold_product
  ADD COLUMN measurement_quality text NOT NULL DEFAULT 'estimated'
    CHECK (measurement_quality IN ('measured', 'estimated')),
  ADD COLUMN comment text;

ALTER TABLE mobigo_material_line
  ALTER COLUMN import_batch_id DROP NOT NULL,
  ALTER COLUMN mobigo_work_order_id DROP NOT NULL,
  ADD COLUMN measurement_quality text NOT NULL DEFAULT 'measured'
    CHECK (measurement_quality IN ('measured', 'estimated')),
  ADD COLUMN comment text,
  ADD CHECK (import_batch_id IS NULL OR mobigo_work_order_id IS NOT NULL);

ALTER TABLE service_spend_report
  ALTER COLUMN import_batch_id DROP NOT NULL;

ALTER TABLE purchased_service
  ADD COLUMN measurement_quality text NOT NULL DEFAULT 'measured'
    CHECK (measurement_quality IN ('measured', 'estimated')),
  ADD COLUMN comment text;