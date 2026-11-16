-- Test data for scope 3.11 (FK-S.05). Only for local development, NOT a migration.
-- Can be run several times: fixed ids + ON CONFLICT DO NOTHING means nothing is added twice.
--
-- Expected result for GET /api/scope311?year=2025:
--   Total 5000 kWh
--   Fläktar      4000 kWh  80 %  3 products, 1 incomplete (Fläkt C lacks kWh)
--   Ingen grupp  1000 kWh  20 %  1 product,  1 incomplete (pump lacks group)
--   Styrsystem      0 kWh   0 %  1 product,  0 incomplete (0 kWh = complete)
-- Expected result for year=2024: total 4960 kWh (Fläkt B 4000 + Gammal fläkt 960).

-- NOTE: if scope312_test_data.sql is also loaded, "Ingen grupp" shows
-- 2 products, 2 incomplete for year=2025 (Duschhandtag is added).

INSERT INTO product_group (id, name, description) VALUES
  ('a0000000-0000-0000-0000-000000000001', 'Fläktar', 'Ventilationsfläktar'),
  ('a0000000-0000-0000-0000-000000000002', 'Styrsystem', 'Styr- och reglerutrustning')
ON CONFLICT DO NOTHING;

INSERT INTO sold_product (id, article_no, name, group_id, kwh_per_year, lifetime_years, measurement_quality, comment) VALUES
  ('b0000000-0000-0000-0000-000000000001', 'TEST-FL-100', 'Fläkt A', 'a0000000-0000-0000-0000-000000000001', 50, 10, 'measured', 'Uppmätt av leverantör'),
  ('b0000000-0000-0000-0000-000000000002', 'TEST-FL-200', 'Fläkt B', 'a0000000-0000-0000-0000-000000000001', 100, 10, 'estimated', 'Uppskattat från datablad'),
  ('b0000000-0000-0000-0000-000000000003', 'TEST-FL-300', 'Fläkt C', 'a0000000-0000-0000-0000-000000000001', NULL, 10, 'estimated', NULL),
  ('b0000000-0000-0000-0000-000000000004', 'TEST-PU-100', 'Pump utan grupp', NULL, 10, 10, 'estimated', NULL),
  ('b0000000-0000-0000-0000-000000000005', 'TEST-ST-100', 'Styrbox', 'a0000000-0000-0000-0000-000000000002', 0, NULL, 'measured', 'Drar ingen el'),
  ('b0000000-0000-0000-0000-000000000006', 'TEST-FL-900', 'Gammal fläkt', 'a0000000-0000-0000-0000-000000000001', 80, 12, 'estimated', NULL)
ON CONFLICT DO NOTHING;

-- Sales (material lines). The date decides the reporting year.
INSERT INTO mobigo_material_line (id, product_id, mobigo_work_order_id, quantity, date) VALUES
  ('c0000000-0000-0000-0000-000000000001', 'b0000000-0000-0000-0000-000000000001', 'TEST-1001', 1, '2025-03-10'),  -- Fläkt A, 1 + 1 = 2 st
  ('c0000000-0000-0000-0000-000000000002', 'b0000000-0000-0000-0000-000000000001', 'TEST-1002', 1, '2025-09-22'),
  ('c0000000-0000-0000-0000-000000000003', 'b0000000-0000-0000-0000-000000000002', 'TEST-1003', 3, '2025-05-05'),  -- Fläkt B, 3 st
  ('c0000000-0000-0000-0000-000000000004', 'b0000000-0000-0000-0000-000000000003', 'TEST-1004', 5, '2025-06-15'),  -- Fläkt C, 5 st
  ('c0000000-0000-0000-0000-000000000005', 'b0000000-0000-0000-0000-000000000004', 'TEST-1005', 10, '2025-11-30'), -- Pump, 10 st
  ('c0000000-0000-0000-0000-000000000006', 'b0000000-0000-0000-0000-000000000005', 'TEST-1006', 4, '2025-02-01'),  -- Styrbox, 4 st
  ('c0000000-0000-0000-0000-000000000007', 'b0000000-0000-0000-0000-000000000002', 'TEST-0901', 4, '2024-12-31'),  -- Fläkt B, year 2024
  ('c0000000-0000-0000-0000-000000000008', 'b0000000-0000-0000-0000-000000000006', 'TEST-0902', 1, '2024-08-08')   -- Gammal fläkt, only 2024
ON CONFLICT DO NOTHING;