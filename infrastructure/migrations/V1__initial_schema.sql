CREATE TABLE "app_user" (
  "id" uuid PRIMARY KEY DEFAULT (gen_random_uuid()),
  "first_name" text NOT NULL,
  "last_name" text NOT NULL,
  "email" text UNIQUE,
  "password_hash" text,
  "created_at" timestamptz NOT NULL DEFAULT (CURRENT_TIMESTAMP),
  CHECK (length(trim(email)) > 0)
);

CREATE TABLE "role" (
  "id" uuid PRIMARY KEY DEFAULT (gen_random_uuid()),
  "name" text UNIQUE NOT NULL,
  "description" text
);

CREATE TABLE "role_permission" (
  "role_id" uuid,
  "permission" text,
  PRIMARY KEY ("role_id", "permission")
);

CREATE TABLE "user_role" (
  "user_id" uuid,
  "role_id" uuid,
  PRIMARY KEY ("user_id", "role_id")
);

CREATE TABLE "import_batch" (
  "id" uuid PRIMARY KEY DEFAULT (gen_random_uuid()),
  "source" text NOT NULL,
  "imported_by" uuid NOT NULL,
  "created_at" timestamptz NOT NULL DEFAULT (CURRENT_TIMESTAMP),
  CHECK (source IN ('mobigo', 'visma_4010', 'visma_services', 'okq8', 'autoplan'))
);

CREATE TABLE "import_batch_file" (
  "id" uuid PRIMARY KEY DEFAULT (gen_random_uuid()),
  "import_batch_id" uuid NOT NULL,
  "name" text NOT NULL,
  "file_hash" text NOT NULL
);

CREATE TABLE "mobigo_material_line" (
  "id" uuid PRIMARY KEY DEFAULT (gen_random_uuid()),
  "import_batch_id" uuid NOT NULL,
  "product_id" uuid NOT NULL,
  "mobigo_work_order_id" text NOT NULL,
  "quantity" numeric NOT NULL,
  "date" date NOT NULL,
  CHECK (quantity >= 0)
);

CREATE TABLE "sold_product" (
  "id" uuid PRIMARY KEY DEFAULT (gen_random_uuid()),
  "article_no" text UNIQUE,
  "name" text NOT NULL,
  "group_id" uuid,
  "kwh_per_year" numeric,
  "lifetime_years" numeric,
  CHECK (length(trim(name)) > 0),
  CHECK (kwh_per_year IS NULL OR kwh_per_year >= 0),
  CHECK (lifetime_years IS NULL OR lifetime_years > 0)
);

CREATE TABLE "product_group" (
  "id" uuid PRIMARY KEY DEFAULT (gen_random_uuid()),
  "name" text UNIQUE NOT NULL,
  "description" text
);

CREATE TABLE "material" (
  "id" uuid PRIMARY KEY DEFAULT (gen_random_uuid()),
  "name" text UNIQUE NOT NULL,
  "eol_treatment_id" uuid NOT NULL
);

CREATE TABLE "sold_product_material" (
  "product_id" uuid,
  "material_id" uuid,
  "weight_kg" numeric NOT NULL,
  CHECK (weight_kg >= 0),
  PRIMARY KEY ("product_id", "material_id")
);

CREATE TABLE "supplier" (
  "id" uuid PRIMARY KEY DEFAULT (gen_random_uuid()),
  "name" text UNIQUE NOT NULL,
  CHECK (length(trim(name)) > 0)
);

CREATE TABLE "purchase_category" (
  "id" uuid PRIMARY KEY DEFAULT (gen_random_uuid()),
  "name" text UNIQUE NOT NULL
);

CREATE TABLE "supplier_category_allocation" (
  "supplier_id" uuid,
  "purchase_category_id" uuid,
  "share" numeric NOT NULL,
  CHECK (share > 0 AND share <= 1),
  PRIMARY KEY ("supplier_id", "purchase_category_id")
);

CREATE TABLE "service_category" (
  "id" uuid PRIMARY KEY DEFAULT (gen_random_uuid()),
  "name" text UNIQUE NOT NULL
);

CREATE TABLE "account_category_mapping" (
  "account_no" text PRIMARY KEY,
  "service_category_id" uuid NOT NULL,
  "service_label" text
);

CREATE TABLE "purchased_good" (
  "id" uuid PRIMARY KEY DEFAULT (gen_random_uuid()),
  "supplier_id" uuid NOT NULL,
  "transaction_date" date NOT NULL,
  "value" numeric NOT NULL,
  "unit" text NOT NULL,
  "measurement_quality" text NOT NULL,
  "goods_name" text,
  "comment" text,
  "specification" text,
  "account_no" text,
  "import_batch_id" uuid,
  "source_record_id" text UNIQUE,
  "created_by" uuid NOT NULL,
  "created_at" timestamptz NOT NULL DEFAULT (CURRENT_TIMESTAMP),
  CHECK (measurement_quality IN ('measured', 'estimated')),
  CHECK (length(trim(unit)) > 0)
);

CREATE TABLE "service_spend_report" (
  "id" uuid PRIMARY KEY DEFAULT (gen_random_uuid()),
  "import_batch_id" uuid UNIQUE NOT NULL,
  "period_from" date NOT NULL,
  "period_to" date NOT NULL,
  CHECK (period_to >= period_from)
);

CREATE TABLE "purchased_service" (
  "report_id" uuid,
  "account_no" text,
  "amount_sek" numeric NOT NULL,
  PRIMARY KEY ("report_id", "account_no")
);

CREATE TABLE "facility_energy" (
  "id" uuid PRIMARY KEY DEFAULT (gen_random_uuid()),
  "city" text NOT NULL,
  "area_m2" numeric(18,6),
  "energy_carrier" text NOT NULL,
  "energy_origin" text,
  "reporting_year" integer NOT NULL,
  "energy_kwh" numeric NOT NULL,
  "measurement_quality" text NOT NULL,
  "comment" text,
  "created_by" uuid NOT NULL,
  "created_at" timestamptz NOT NULL DEFAULT (CURRENT_TIMESTAMP),
  CHECK (energy_kwh >= 0),
  CHECK (area_m2 IS NULL OR area_m2 >= 0),
  CHECK (energy_carrier IN ('el', 'fjarrvarme')),
  CHECK (measurement_quality IN ('measured', 'estimated'))
);

CREATE TABLE "vehicle" (
  "registration_number" text PRIMARY KEY NOT NULL,
  "vehicle_type" text,
  "fuel_type" text,
  "created_at" timestamptz NOT NULL DEFAULT (CURRENT_TIMESTAMP)
);

CREATE TABLE "vehicle_usage" (
  "id" uuid PRIMARY KEY DEFAULT (gen_random_uuid()),
  "registration_number" text,
  "fuel_type" text NOT NULL,
  "energy_origin" text,
  "date" date NOT NULL,
  "value" numeric NOT NULL,
  "unit" text NOT NULL,
  "measurement_quality" text NOT NULL,
  "comment" text,
  "import_batch_id" uuid,
  "source_system" text,
  "source_record_id" text,
  "created_by" uuid NOT NULL,
  "created_at" timestamptz NOT NULL DEFAULT (CURRENT_TIMESTAMP),
  CHECK (value >= 0),
  CHECK (measurement_quality IN ('measured', 'estimated'))
);

CREATE TABLE "waste" (
  "id" uuid PRIMARY KEY DEFAULT (gen_random_uuid()),
  "waste_type_id" uuid NOT NULL,
  "waste_treatment_id" uuid NOT NULL,
  "date" date NOT NULL,
  "weight_kg" numeric NOT NULL,
  "measurement_quality" text NOT NULL,
  "comment" text,
  "created_by" uuid NOT NULL,
  "created_at" timestamptz NOT NULL DEFAULT (CURRENT_TIMESTAMP),
  CHECK (weight_kg >= 0),
  CHECK (measurement_quality IN ('measured', 'estimated'))
);

CREATE TABLE "waste_treatment" (
  "id" uuid PRIMARY KEY DEFAULT (gen_random_uuid()),
  "name" text UNIQUE NOT NULL
);

CREATE TABLE "waste_type" (
  "id" uuid PRIMARY KEY DEFAULT (gen_random_uuid()),
  "name" text UNIQUE NOT NULL
);

CREATE TABLE "business_travel_flight" (
  "id" uuid PRIMARY KEY DEFAULT (gen_random_uuid()),
  "departure_iata_code" text NOT NULL,
  "destination_iata_code" text NOT NULL,
  "travel_date" date,
  "single_trips_quantity" integer NOT NULL,
  "measurement_quality" text NOT NULL,
  "comment" text,
  "travelling_user_id" uuid,
  "created_by" uuid NOT NULL,
  "created_at" timestamptz NOT NULL DEFAULT (CURRENT_TIMESTAMP),
  CHECK (single_trips_quantity > 0),
  CHECK (departure_iata_code ~ '^[A-Z]{3}$'),
  CHECK (destination_iata_code ~ '^[A-Z]{3}$'),
  CHECK (measurement_quality IN ('measured', 'estimated'))
);

CREATE TABLE "business_travel_ground" (
  "id" uuid PRIMARY KEY DEFAULT (gen_random_uuid()),
  "travel_mode" text NOT NULL,
  "country" text,
  "fuel_type" text,
  "date" date NOT NULL,
  "value" numeric NOT NULL,
  "unit" text NOT NULL,
  "measurement_quality" text NOT NULL,
  "comment" text,
  "travelling_user_id" uuid,
  "mileage_allowance" numeric(10,2),
  "created_by" uuid NOT NULL,
  "created_at" timestamptz NOT NULL DEFAULT (CURRENT_TIMESTAMP),
  CHECK (value > 0),
  CHECK (mileage_allowance IS NULL OR mileage_allowance >= 0),
  CHECK (measurement_quality IN ('measured', 'estimated'))
);

CREATE TABLE "attachment" (
  "id" uuid PRIMARY KEY DEFAULT (gen_random_uuid()),
  "file_name" text NOT NULL,
  "storage_path" text NOT NULL,
  "uploaded_by" uuid NOT NULL,
  "uploaded_at" timestamptz NOT NULL DEFAULT (CURRENT_TIMESTAMP),
  "purchased_good_id" uuid,
  "facility_energy_id" uuid,
  "vehicle_usage_id" uuid,
  "waste_id" uuid,
  "business_travel_flight_id" uuid,
  "business_travel_ground_id" uuid,
  CHECK (num_nonnulls(purchased_good_id, facility_energy_id, vehicle_usage_id, waste_id, business_travel_flight_id, business_travel_ground_id) = 1)
);

CREATE TABLE "audit_log" (
  "id" uuid PRIMARY KEY DEFAULT (gen_random_uuid()),
  "table_name" text NOT NULL,
  "record_id" text NOT NULL,
  "action" text NOT NULL,
  "old_values" jsonb,
  "new_values" jsonb,
  "changed_by" uuid,
  "changed_at" timestamptz NOT NULL DEFAULT (CURRENT_TIMESTAMP),
  CHECK (action IN ('insert', 'update', 'delete'))
);

CREATE INDEX ON "user_role" ("role_id");

CREATE INDEX ON "import_batch" ("imported_by");

CREATE INDEX ON "import_batch" ("created_at");

CREATE INDEX ON "import_batch_file" ("import_batch_id");

CREATE UNIQUE INDEX ON "import_batch_file" ("file_hash");

CREATE INDEX ON "mobigo_material_line" ("import_batch_id");

CREATE INDEX ON "mobigo_material_line" ("product_id");

CREATE INDEX ON "mobigo_material_line" ("date");

CREATE INDEX ON "mobigo_material_line" ("mobigo_work_order_id", "product_id", "date");

CREATE INDEX ON "sold_product" ("group_id");

CREATE UNIQUE INDEX sold_product_name_key_uq ON sold_product
  ((lower(regexp_replace(btrim(name), '\s+', ' ', 'g') COLLATE "und-x-icu")))
  WHERE article_no IS NULL;

CREATE INDEX ON "material" ("eol_treatment_id");

CREATE INDEX ON "sold_product_material" ("material_id");

CREATE INDEX ON "supplier_category_allocation" ("purchase_category_id");

CREATE INDEX ON "account_category_mapping" ("service_category_id");

CREATE INDEX ON "purchased_good" ("transaction_date");

CREATE INDEX ON "purchased_good" ("supplier_id");

CREATE INDEX ON "purchased_good" ("import_batch_id");

CREATE INDEX ON "purchased_good" ("created_by");

CREATE INDEX ON "service_spend_report" ("period_from", "period_to");

CREATE UNIQUE INDEX ON "facility_energy" ("city", "energy_carrier", "reporting_year");

CREATE INDEX ON "facility_energy" ("created_by");

CREATE INDEX ON "vehicle_usage" ("date");

CREATE INDEX ON "vehicle_usage" ("registration_number");

CREATE INDEX ON "vehicle_usage" ("import_batch_id");

CREATE INDEX ON "waste" ("date");

CREATE INDEX ON "waste" ("waste_type_id");

CREATE INDEX ON "waste" ("waste_treatment_id");

CREATE INDEX ON "business_travel_flight" ("travel_date");

CREATE INDEX ON "business_travel_flight" ("travelling_user_id");

CREATE INDEX ON "business_travel_ground" ("date");

CREATE INDEX ON "business_travel_ground" ("travelling_user_id");

CREATE INDEX ON "business_travel_ground" ("travel_mode");

CREATE INDEX ON "attachment" ("purchased_good_id");

CREATE INDEX ON "attachment" ("facility_energy_id");

CREATE INDEX ON "attachment" ("vehicle_usage_id");

CREATE INDEX ON "attachment" ("waste_id");

CREATE INDEX ON "attachment" ("business_travel_flight_id");

CREATE INDEX ON "attachment" ("business_travel_ground_id");

CREATE INDEX ON "audit_log" ("table_name", "record_id", "changed_at");

COMMENT ON COLUMN "app_user"."password_hash" IS 'Null means account not activated';

COMMENT ON COLUMN "role_permission"."permission" IS 'E.g. import.mobigo';

COMMENT ON COLUMN "import_batch"."source" IS 'mobigo, visma_4010, visma_services, okq8, autoplan';

COMMENT ON COLUMN "import_batch_file"."file_hash" IS 'SHA-256 för dublettskydd, förhindra att man laddar upp samma fil mer än en gång';

COMMENT ON COLUMN "mobigo_material_line"."mobigo_work_order_id" IS 'Uppdragsnummer';

COMMENT ON COLUMN "mobigo_material_line"."date" IS 'Leveransdatum, avgör rapporteringsår';

COMMENT ON COLUMN "sold_product"."kwh_per_year" IS 'Standardantagande. Null = ej satt';

COMMENT ON COLUMN "sold_product"."lifetime_years" IS 'Standardantagande. Null = ej satt';

COMMENT ON COLUMN "product_group"."name" IS 'Rapporteringsnivå för 3.11/3.12, t.ex. Fläktar';

COMMENT ON COLUMN "material"."name" IS 'T.ex. Plast';

COMMENT ON COLUMN "supplier"."name" IS 'Visma-importen matchar leverantör på namn';

COMMENT ON COLUMN "purchase_category"."name" IS 'Kategorier från PropTech för köpt varor, t.ex. Trä, Elektronik';

COMMENT ON COLUMN "supplier_category_allocation"."share" IS '0.5 = 50 procent. Summa per leverantör = 100% (FK-DI.05, kontrolleras i backend/deferred trigger)';

COMMENT ON COLUMN "service_category"."name" IS 'De 26 tjänstekategorierna, t.ex. Fastighetstjänster';

COMMENT ON COLUMN "account_category_mapping"."account_no" IS 'Visma-konto, t.ex. 5010';

COMMENT ON COLUMN "account_category_mapping"."service_label" IS 'T.ex. Lokalhyra';

COMMENT ON COLUMN "purchased_good"."value" IS 'Krediter kan vara negativa';

COMMENT ON COLUMN "purchased_good"."unit" IS 'sek, kg, st';

COMMENT ON COLUMN "purchased_good"."measurement_quality" IS 'measured/estimated';

COMMENT ON COLUMN "purchased_good"."goods_name" IS 'Vara, ofta okänd vid Visma-import';

COMMENT ON COLUMN "purchased_good"."comment" IS 'Kolumn i Atmoz-excel';

COMMENT ON COLUMN "purchased_good"."specification" IS 'Kolumn i Atmoz-excel';

COMMENT ON COLUMN "purchased_good"."account_no" IS 'Konto, t.ex. 4010';

COMMENT ON COLUMN "purchased_good"."import_batch_id" IS 'Null = manuellt registrerad';

COMMENT ON COLUMN "purchased_good"."source_record_id" IS 'Visma LevFktnr från 4010 exporten';

COMMENT ON COLUMN "service_spend_report"."period_from" IS 'Från rapportens Period-rad';

COMMENT ON COLUMN "purchased_service"."account_no" IS 'Konto så som 5010';

COMMENT ON COLUMN "purchased_service"."amount_sek" IS 'Kostnad som positivt belopp (rapportens tecken vänt)';

COMMENT ON COLUMN "facility_energy"."energy_carrier" IS 'el eller fjärrvärme';

COMMENT ON COLUMN "facility_energy"."energy_origin" IS 'Ursprung: Vattenkraft, Ospecifierat, Förnybar ospec';

COMMENT ON COLUMN "facility_energy"."energy_kwh" IS 'Nollvärde tillåtet (FK-S.01)';

COMMENT ON COLUMN "vehicle"."vehicle_type" IS 'Bil, Skåpbil';

COMMENT ON COLUMN "vehicle_usage"."fuel_type" IS 'El, Diesel, Bensin';

COMMENT ON COLUMN "vehicle_usage"."energy_origin" IS 'Vid elfordon';

COMMENT ON COLUMN "vehicle_usage"."date" IS 'Leveransdatum / fakturadatum';

COMMENT ON COLUMN "vehicle_usage"."unit" IS 'liter, kWh';

COMMENT ON COLUMN "vehicle_usage"."source_record_id" IS 'Fakturanummer';

COMMENT ON COLUMN "waste_treatment"."name" IS 'Materialåtervinning, deponi etc';

COMMENT ON COLUMN "waste_type"."name" IS 'Papper och kartong, plast, elektronik etc';

COMMENT ON COLUMN "business_travel_flight"."departure_iata_code" IS 'FK-BV.05';

COMMENT ON COLUMN "business_travel_flight"."travel_date" IS 'Behövs för avstämning mot konto 5800';

COMMENT ON COLUMN "business_travel_flight"."travelling_user_id" IS 'Den som åkte på resan (Arbetare, FK-AB.1.3)';

COMMENT ON COLUMN "business_travel_flight"."created_by" IS 'Den som loggade resan (Arbetare, FK-AB.1.3)';

COMMENT ON COLUMN "business_travel_ground"."travel_mode" IS 'Tåg, Bil, Hotell';

COMMENT ON COLUMN "business_travel_ground"."fuel_type" IS 'Null om ej relevant (t.ex. hotell)';

COMMENT ON COLUMN "business_travel_ground"."unit" IS 'km, sek, gn, kgCO2e';

COMMENT ON COLUMN "business_travel_ground"."travelling_user_id" IS 'Den som åkte på resan (Arbetare, FK-AB.1.3)';

COMMENT ON COLUMN "business_travel_ground"."mileage_allowance" IS 'SEK/mil. Null om ej relevant';

COMMENT ON COLUMN "audit_log"."record_id" IS 'Text för att strakställa stöd för både UUID och text-PKs';

COMMENT ON COLUMN "audit_log"."action" IS 'insert, update eller delete';

ALTER TABLE "role_permission" ADD FOREIGN KEY ("role_id") REFERENCES "role" ("id") ON DELETE CASCADE DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "user_role" ADD FOREIGN KEY ("user_id") REFERENCES "app_user" ("id") ON DELETE CASCADE DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "user_role" ADD FOREIGN KEY ("role_id") REFERENCES "role" ("id") ON DELETE CASCADE DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "import_batch" ADD FOREIGN KEY ("imported_by") REFERENCES "app_user" ("id") ON DELETE RESTRICT DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "import_batch_file" ADD FOREIGN KEY ("import_batch_id") REFERENCES "import_batch" ("id") ON DELETE CASCADE DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "mobigo_material_line" ADD FOREIGN KEY ("import_batch_id") REFERENCES "import_batch" ("id") ON DELETE CASCADE DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "mobigo_material_line" ADD FOREIGN KEY ("product_id") REFERENCES "sold_product" ("id") ON DELETE RESTRICT DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "sold_product" ADD FOREIGN KEY ("group_id") REFERENCES "product_group" ("id") ON DELETE RESTRICT DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "material" ADD FOREIGN KEY ("eol_treatment_id") REFERENCES "waste_treatment" ("id") ON DELETE RESTRICT DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "sold_product_material" ADD FOREIGN KEY ("product_id") REFERENCES "sold_product" ("id") ON DELETE CASCADE DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "sold_product_material" ADD FOREIGN KEY ("material_id") REFERENCES "material" ("id") ON DELETE RESTRICT DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "supplier_category_allocation" ADD FOREIGN KEY ("supplier_id") REFERENCES "supplier" ("id") ON DELETE CASCADE DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "supplier_category_allocation" ADD FOREIGN KEY ("purchase_category_id") REFERENCES "purchase_category" ("id") ON DELETE RESTRICT DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "account_category_mapping" ADD FOREIGN KEY ("service_category_id") REFERENCES "service_category" ("id") ON DELETE RESTRICT DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "purchased_good" ADD FOREIGN KEY ("supplier_id") REFERENCES "supplier" ("id") ON DELETE RESTRICT DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "purchased_good" ADD FOREIGN KEY ("import_batch_id") REFERENCES "import_batch" ("id") ON DELETE CASCADE DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "purchased_good" ADD FOREIGN KEY ("created_by") REFERENCES "app_user" ("id") ON DELETE RESTRICT DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "service_spend_report" ADD FOREIGN KEY ("import_batch_id") REFERENCES "import_batch" ("id") ON DELETE CASCADE DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "purchased_service" ADD FOREIGN KEY ("report_id") REFERENCES "service_spend_report" ("id") ON DELETE CASCADE DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "facility_energy" ADD FOREIGN KEY ("created_by") REFERENCES "app_user" ("id") ON DELETE RESTRICT DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "vehicle_usage" ADD FOREIGN KEY ("registration_number") REFERENCES "vehicle" ("registration_number") ON DELETE RESTRICT ON UPDATE CASCADE DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "vehicle_usage" ADD FOREIGN KEY ("import_batch_id") REFERENCES "import_batch" ("id") ON DELETE CASCADE DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "vehicle_usage" ADD FOREIGN KEY ("created_by") REFERENCES "app_user" ("id") ON DELETE RESTRICT DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "waste" ADD FOREIGN KEY ("waste_type_id") REFERENCES "waste_type" ("id") ON DELETE RESTRICT DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "waste" ADD FOREIGN KEY ("waste_treatment_id") REFERENCES "waste_treatment" ("id") ON DELETE RESTRICT DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "waste" ADD FOREIGN KEY ("created_by") REFERENCES "app_user" ("id") ON DELETE RESTRICT DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "business_travel_flight" ADD FOREIGN KEY ("travelling_user_id") REFERENCES "app_user" ("id") ON DELETE RESTRICT DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "business_travel_flight" ADD FOREIGN KEY ("created_by") REFERENCES "app_user" ("id") ON DELETE RESTRICT DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "business_travel_ground" ADD FOREIGN KEY ("travelling_user_id") REFERENCES "app_user" ("id") ON DELETE RESTRICT DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "business_travel_ground" ADD FOREIGN KEY ("created_by") REFERENCES "app_user" ("id") ON DELETE RESTRICT DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "attachment" ADD FOREIGN KEY ("uploaded_by") REFERENCES "app_user" ("id") ON DELETE RESTRICT DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "attachment" ADD FOREIGN KEY ("purchased_good_id") REFERENCES "purchased_good" ("id") ON DELETE CASCADE DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "attachment" ADD FOREIGN KEY ("facility_energy_id") REFERENCES "facility_energy" ("id") ON DELETE CASCADE DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "attachment" ADD FOREIGN KEY ("vehicle_usage_id") REFERENCES "vehicle_usage" ("id") ON DELETE CASCADE DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "attachment" ADD FOREIGN KEY ("waste_id") REFERENCES "waste" ("id") ON DELETE CASCADE DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "attachment" ADD FOREIGN KEY ("business_travel_flight_id") REFERENCES "business_travel_flight" ("id") ON DELETE CASCADE DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "attachment" ADD FOREIGN KEY ("business_travel_ground_id") REFERENCES "business_travel_ground" ("id") ON DELETE CASCADE DEFERRABLE INITIALLY IMMEDIATE;

ALTER TABLE "audit_log" ADD FOREIGN KEY ("changed_by") REFERENCES "app_user" ("id") ON DELETE RESTRICT DEFERRABLE INITIALLY IMMEDIATE;