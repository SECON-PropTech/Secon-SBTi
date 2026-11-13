-- Testdata för Scope 3.12. Kör lokalt efter databasmigreringarna.
-- Fasta ID:n och ON CONFLICT DO NOTHING gör att filen kan köras flera gånger.
--
-- Förväntat resultat för GET /api/product-materials:
--   Duschhandtag (TEST-312-001): rostfritt stål 0,30 kg och ABS-plast 0,12 kg.
--   Filterhus (TEST-312-002): ABS-plast 0,45 kg.
--
-- Duschhandtagets ID: e3120000-0000-0000-0000-000000000021
-- Förväntat resultat för GET /api/product-materials/{productId}/entries:
--   ?year=2025&month=3 : två rader, 25 st den 1 mars och 12 st den 31 mars.
--   ?year=2025         : tre rader, inklusive 10 st den 1 april.
--   ?year=2026         : en rad, 30 st den 10 mars.
--   Filterhuset       : tom lista för båda åren.

BEGIN;

INSERT INTO waste_treatment (id, name) VALUES
    ('e3120000-0000-0000-0000-000000000001', 'Materialåtervinning'),
    ('e3120000-0000-0000-0000-000000000002', 'Energiåtervinning')
ON CONFLICT DO NOTHING;

INSERT INTO material (id, name, eol_treatment_id) VALUES
    ('e3120000-0000-0000-0000-000000000011', 'Rostfritt stål',
     (SELECT id FROM waste_treatment WHERE name = 'Materialåtervinning')),
    ('e3120000-0000-0000-0000-000000000012', 'ABS-plast',
     (SELECT id FROM waste_treatment WHERE name = 'Energiåtervinning'))
ON CONFLICT DO NOTHING;

INSERT INTO sold_product (id, article_no, name) VALUES
    ('e3120000-0000-0000-0000-000000000021', 'TEST-312-001', 'Duschhandtag'),
    ('e3120000-0000-0000-0000-000000000022', 'TEST-312-002', 'Filterhus')
ON CONFLICT DO NOTHING;

INSERT INTO sold_product_material (product_id, material_id, weight_kg) VALUES
    ('e3120000-0000-0000-0000-000000000021',
     (SELECT id FROM material WHERE name = 'Rostfritt stål'), 0.30),
    ('e3120000-0000-0000-0000-000000000021',
     (SELECT id FROM material WHERE name = 'ABS-plast'), 0.12),
    ('e3120000-0000-0000-0000-000000000022',
     (SELECT id FROM material WHERE name = 'ABS-plast'), 0.45)
ON CONFLICT DO NOTHING;

-- Alla rader är manuella. Datumet avgör vilken rapporteringsperiod de tillhör.
INSERT INTO mobigo_material_line
    (id, product_id, quantity, date, measurement_quality, comment)
VALUES
    ('e3120000-0000-0000-0000-000000000041', 'e3120000-0000-0000-0000-000000000021',
     25, '2025-03-01', 'estimated', 'Uppskattat antal i början av mars'),
    ('e3120000-0000-0000-0000-000000000042', 'e3120000-0000-0000-0000-000000000021',
     12, '2025-03-31', 'measured', 'Leverans i slutet av mars'),
    ('e3120000-0000-0000-0000-000000000043', 'e3120000-0000-0000-0000-000000000021',
     10, '2025-04-01', 'measured', NULL),
    ('e3120000-0000-0000-0000-000000000044', 'e3120000-0000-0000-0000-000000000021',
     30, '2026-03-10', 'estimated', 'Uppskattat antal för 2026')
ON CONFLICT DO NOTHING;

COMMIT;