using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using SeconSbtiBackend.Models;

namespace SeconSbtiBackend.Data;

public partial class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AccountCategoryMapping> AccountCategoryMappings { get; set; }

    public virtual DbSet<AppUser> AppUsers { get; set; }

    public virtual DbSet<Attachment> Attachments { get; set; }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<BusinessTravelFlight> BusinessTravelFlights { get; set; }

    public virtual DbSet<BusinessTravelGround> BusinessTravelGrounds { get; set; }

    public virtual DbSet<FacilityEnergy> FacilityEnergies { get; set; }

    public virtual DbSet<ImportBatch> ImportBatches { get; set; }

    public virtual DbSet<ImportBatchFile> ImportBatchFiles { get; set; }

    public virtual DbSet<Material> Materials { get; set; }

    public virtual DbSet<MobigoMaterialLine> MobigoMaterialLines { get; set; }

    public virtual DbSet<ProductGroup> ProductGroups { get; set; }

    public virtual DbSet<PurchaseCategory> PurchaseCategories { get; set; }

    public virtual DbSet<PurchasedGood> PurchasedGoods { get; set; }

    public virtual DbSet<PurchasedService> PurchasedServices { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<RolePermission> RolePermissions { get; set; }

    public virtual DbSet<ServiceCategory> ServiceCategories { get; set; }

    public virtual DbSet<ServiceSpendReport> ServiceSpendReports { get; set; }

    public virtual DbSet<SoldProduct> SoldProducts { get; set; }

    public virtual DbSet<SoldProductMaterial> SoldProductMaterials { get; set; }

    public virtual DbSet<Supplier> Suppliers { get; set; }

    public virtual DbSet<SupplierCategoryAllocation> SupplierCategoryAllocations { get; set; }

    public virtual DbSet<Vehicle> Vehicles { get; set; }

    public virtual DbSet<VehicleUsage> VehicleUsages { get; set; }

    public virtual DbSet<Waste> Wastes { get; set; }

    public virtual DbSet<WasteTreatment> WasteTreatments { get; set; }

    public virtual DbSet<WasteType> WasteTypes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AccountCategoryMapping>(entity =>
        {
            entity.HasKey(e => e.AccountNo).HasName("account_category_mapping_pkey");

            entity.ToTable("account_category_mapping");

            entity.HasIndex(e => e.ServiceCategoryId, "account_category_mapping_service_category_id_idx");

            entity.Property(e => e.AccountNo)
                .HasComment("Visma-konto, t.ex. 5010")
                .HasColumnName("account_no");
            entity.Property(e => e.ServiceCategoryId).HasColumnName("service_category_id");
            entity.Property(e => e.ServiceLabel)
                .HasComment("T.ex. Lokalhyra")
                .HasColumnName("service_label");

            entity.HasOne(d => d.ServiceCategory).WithMany(p => p.AccountCategoryMappings)
                .HasForeignKey(d => d.ServiceCategoryId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("account_category_mapping_service_category_id_fkey");
        });

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("app_user_pkey");

            entity.ToTable("app_user");

            entity.HasIndex(e => e.Email, "app_user_email_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.Email).HasColumnName("email");
            entity.Property(e => e.FirstName).HasColumnName("first_name");
            entity.Property(e => e.LastName).HasColumnName("last_name");
            entity.Property(e => e.PasswordHash)
                .HasComment("Null means account not activated")
                .HasColumnName("password_hash");

            entity.HasMany(d => d.Roles).WithMany(p => p.Users)
                .UsingEntity<Dictionary<string, object>>(
                    "UserRole",
                    r => r.HasOne<Role>().WithMany()
                        .HasForeignKey("RoleId")
                        .HasConstraintName("user_role_role_id_fkey"),
                    l => l.HasOne<AppUser>().WithMany()
                        .HasForeignKey("UserId")
                        .HasConstraintName("user_role_user_id_fkey"),
                    j =>
                    {
                        j.HasKey("UserId", "RoleId").HasName("user_role_pkey");
                        j.ToTable("user_role");
                        j.HasIndex(new[] { "RoleId" }, "user_role_role_id_idx");
                        j.IndexerProperty<Guid>("UserId").HasColumnName("user_id");
                        j.IndexerProperty<Guid>("RoleId").HasColumnName("role_id");
                    });
        });

        modelBuilder.Entity<Attachment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("attachment_pkey");

            entity.ToTable("attachment");

            entity.HasIndex(e => e.BusinessTravelFlightId, "attachment_business_travel_flight_id_idx");

            entity.HasIndex(e => e.BusinessTravelGroundId, "attachment_business_travel_ground_id_idx");

            entity.HasIndex(e => e.FacilityEnergyId, "attachment_facility_energy_id_idx");

            entity.HasIndex(e => e.PurchasedGoodId, "attachment_purchased_good_id_idx");

            entity.HasIndex(e => e.VehicleUsageId, "attachment_vehicle_usage_id_idx");

            entity.HasIndex(e => e.WasteId, "attachment_waste_id_idx");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.BusinessTravelFlightId).HasColumnName("business_travel_flight_id");
            entity.Property(e => e.BusinessTravelGroundId).HasColumnName("business_travel_ground_id");
            entity.Property(e => e.FacilityEnergyId).HasColumnName("facility_energy_id");
            entity.Property(e => e.FileName).HasColumnName("file_name");
            entity.Property(e => e.PurchasedGoodId).HasColumnName("purchased_good_id");
            entity.Property(e => e.StoragePath).HasColumnName("storage_path");
            entity.Property(e => e.UploadedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("uploaded_at");
            entity.Property(e => e.UploadedBy).HasColumnName("uploaded_by");
            entity.Property(e => e.VehicleUsageId).HasColumnName("vehicle_usage_id");
            entity.Property(e => e.WasteId).HasColumnName("waste_id");

            entity.HasOne(d => d.BusinessTravelFlight).WithMany(p => p.Attachments)
                .HasForeignKey(d => d.BusinessTravelFlightId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("attachment_business_travel_flight_id_fkey");

            entity.HasOne(d => d.BusinessTravelGround).WithMany(p => p.Attachments)
                .HasForeignKey(d => d.BusinessTravelGroundId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("attachment_business_travel_ground_id_fkey");

            entity.HasOne(d => d.FacilityEnergy).WithMany(p => p.Attachments)
                .HasForeignKey(d => d.FacilityEnergyId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("attachment_facility_energy_id_fkey");

            entity.HasOne(d => d.PurchasedGood).WithMany(p => p.Attachments)
                .HasForeignKey(d => d.PurchasedGoodId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("attachment_purchased_good_id_fkey");

            entity.HasOne(d => d.UploadedByNavigation).WithMany(p => p.Attachments)
                .HasForeignKey(d => d.UploadedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("attachment_uploaded_by_fkey");

            entity.HasOne(d => d.VehicleUsage).WithMany(p => p.Attachments)
                .HasForeignKey(d => d.VehicleUsageId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("attachment_vehicle_usage_id_fkey");

            entity.HasOne(d => d.Waste).WithMany(p => p.Attachments)
                .HasForeignKey(d => d.WasteId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("attachment_waste_id_fkey");
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("audit_log_pkey");

            entity.ToTable("audit_log");

            entity.HasIndex(e => new { e.TableName, e.RecordId, e.ChangedAt }, "audit_log_table_name_record_id_changed_at_idx");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Action)
                .HasComment("insert, update eller delete")
                .HasColumnName("action");
            entity.Property(e => e.ChangedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("changed_at");
            entity.Property(e => e.ChangedBy).HasColumnName("changed_by");
            entity.Property(e => e.NewValues)
                .HasColumnType("jsonb")
                .HasColumnName("new_values");
            entity.Property(e => e.OldValues)
                .HasColumnType("jsonb")
                .HasColumnName("old_values");
            entity.Property(e => e.RecordId)
                .HasComment("Text för att strakställa stöd för både UUID och text-PKs")
                .HasColumnName("record_id");
            entity.Property(e => e.TableName).HasColumnName("table_name");

            entity.HasOne(d => d.ChangedByNavigation).WithMany(p => p.AuditLogs)
                .HasForeignKey(d => d.ChangedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("audit_log_changed_by_fkey");
        });

        modelBuilder.Entity<BusinessTravelFlight>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("business_travel_flight_pkey");

            entity.ToTable("business_travel_flight");

            entity.HasIndex(e => e.TravelDate, "business_travel_flight_travel_date_idx");

            entity.HasIndex(e => e.TravellingUserId, "business_travel_flight_travelling_user_id_idx");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Comment).HasColumnName("comment");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasComment("Den som loggade resan (Arbetare, FK-AB.1.3)")
                .HasColumnName("created_by");
            entity.Property(e => e.DepartureIataCode)
                .HasComment("FK-BV.05")
                .HasColumnName("departure_iata_code");
            entity.Property(e => e.DestinationIataCode).HasColumnName("destination_iata_code");
            entity.Property(e => e.MeasurementQuality).HasColumnName("measurement_quality");
            entity.Property(e => e.SingleTripsQuantity).HasColumnName("single_trips_quantity");
            entity.Property(e => e.TravelDate)
                .HasComment("Behövs för avstämning mot konto 5800")
                .HasColumnName("travel_date");
            entity.Property(e => e.TravellingUserId)
                .HasComment("Den som åkte på resan (Arbetare, FK-AB.1.3)")
                .HasColumnName("travelling_user_id");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.BusinessTravelFlightCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("business_travel_flight_created_by_fkey");

            entity.HasOne(d => d.TravellingUser).WithMany(p => p.BusinessTravelFlightTravellingUsers)
                .HasForeignKey(d => d.TravellingUserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("business_travel_flight_travelling_user_id_fkey");
        });

        modelBuilder.Entity<BusinessTravelGround>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("business_travel_ground_pkey");

            entity.ToTable("business_travel_ground");

            entity.HasIndex(e => e.Date, "business_travel_ground_date_idx");

            entity.HasIndex(e => e.TravelMode, "business_travel_ground_travel_mode_idx");

            entity.HasIndex(e => e.TravellingUserId, "business_travel_ground_travelling_user_id_idx");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Comment).HasColumnName("comment");
            entity.Property(e => e.Country).HasColumnName("country");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.Date).HasColumnName("date");
            entity.Property(e => e.FuelType)
                .HasComment("Null om ej relevant (t.ex. hotell)")
                .HasColumnName("fuel_type");
            entity.Property(e => e.MeasurementQuality).HasColumnName("measurement_quality");
            entity.Property(e => e.MileageAllowance)
                .HasPrecision(10, 2)
                .HasComment("SEK/mil. Null om ej relevant")
                .HasColumnName("mileage_allowance");
            entity.Property(e => e.TravelMode)
                .HasComment("Tåg, Bil, Hotell")
                .HasColumnName("travel_mode");
            entity.Property(e => e.TravellingUserId)
                .HasComment("Den som åkte på resan (Arbetare, FK-AB.1.3)")
                .HasColumnName("travelling_user_id");
            entity.Property(e => e.Unit)
                .HasComment("km, sek, gn, kgCO2e")
                .HasColumnName("unit");
            entity.Property(e => e.Value).HasColumnName("value");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.BusinessTravelGroundCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("business_travel_ground_created_by_fkey");

            entity.HasOne(d => d.TravellingUser).WithMany(p => p.BusinessTravelGroundTravellingUsers)
                .HasForeignKey(d => d.TravellingUserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("business_travel_ground_travelling_user_id_fkey");
        });

        modelBuilder.Entity<FacilityEnergy>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("facility_energy_pkey");

            entity.ToTable("facility_energy");

            entity.HasIndex(e => new { e.City, e.EnergyCarrier, e.ReportingYear }, "facility_energy_city_energy_carrier_reporting_year_idx").IsUnique();

            entity.HasIndex(e => e.CreatedBy, "facility_energy_created_by_idx");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.AreaM2)
                .HasPrecision(18, 6)
                .HasColumnName("area_m2");
            entity.Property(e => e.City).HasColumnName("city");
            entity.Property(e => e.Comment).HasColumnName("comment");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.EnergyCarrier)
                .HasComment("el eller fjärrvärme")
                .HasColumnName("energy_carrier");
            entity.Property(e => e.EnergyKwh)
                .HasComment("Nollvärde tillåtet (FK-S.01)")
                .HasColumnName("energy_kwh");
            entity.Property(e => e.EnergyOrigin)
                .HasComment("Ursprung: Vattenkraft, Ospecifierat, Förnybar ospec")
                .HasColumnName("energy_origin");
            entity.Property(e => e.MeasurementQuality).HasColumnName("measurement_quality");
            entity.Property(e => e.ReportingYear).HasColumnName("reporting_year");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.FacilityEnergies)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("facility_energy_created_by_fkey");
        });

        modelBuilder.Entity<ImportBatch>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("import_batch_pkey");

            entity.ToTable("import_batch");

            entity.HasIndex(e => e.CreatedAt, "import_batch_created_at_idx");

            entity.HasIndex(e => e.ImportedBy, "import_batch_imported_by_idx");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.ImportedBy).HasColumnName("imported_by");
            entity.Property(e => e.Source)
                .HasComment("mobigo, visma_4010, visma_services, okq8, autoplan")
                .HasColumnName("source");

            entity.HasOne(d => d.ImportedByNavigation).WithMany(p => p.ImportBatches)
                .HasForeignKey(d => d.ImportedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("import_batch_imported_by_fkey");
        });

        modelBuilder.Entity<ImportBatchFile>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("import_batch_file_pkey");

            entity.ToTable("import_batch_file");

            entity.HasIndex(e => e.FileHash, "import_batch_file_file_hash_idx").IsUnique();

            entity.HasIndex(e => e.ImportBatchId, "import_batch_file_import_batch_id_idx");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.FileHash)
                .HasComment("SHA-256 för dublettskydd, förhindra att man laddar upp samma fil mer än en gång")
                .HasColumnName("file_hash");
            entity.Property(e => e.ImportBatchId).HasColumnName("import_batch_id");
            entity.Property(e => e.Name).HasColumnName("name");

            entity.HasOne(d => d.ImportBatch).WithMany(p => p.ImportBatchFiles)
                .HasForeignKey(d => d.ImportBatchId)
                .HasConstraintName("import_batch_file_import_batch_id_fkey");
        });

        modelBuilder.Entity<Material>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("material_pkey");

            entity.ToTable("material");

            entity.HasIndex(e => e.EolTreatmentId, "material_eol_treatment_id_idx");

            entity.HasIndex(e => e.Name, "material_name_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.EolTreatmentId).HasColumnName("eol_treatment_id");
            entity.Property(e => e.Name)
                .HasComment("T.ex. Plast")
                .HasColumnName("name");

            entity.HasOne(d => d.EolTreatment).WithMany(p => p.Materials)
                .HasForeignKey(d => d.EolTreatmentId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("material_eol_treatment_id_fkey");
        });

        modelBuilder.Entity<MobigoMaterialLine>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("mobigo_material_line_pkey");

            entity.ToTable("mobigo_material_line");

            entity.HasIndex(e => e.Date, "mobigo_material_line_date_idx");

            entity.HasIndex(e => e.ImportBatchId, "mobigo_material_line_import_batch_id_idx");

            entity.HasIndex(e => new { e.MobigoWorkOrderId, e.ProductId, e.Date }, "mobigo_material_line_mobigo_work_order_id_product_id_date_idx");

            entity.HasIndex(e => e.ProductId, "mobigo_material_line_product_id_idx");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Date)
                .HasComment("Leveransdatum, avgör rapporteringsår")
                .HasColumnName("date");
            entity.Property(e => e.ImportBatchId).HasColumnName("import_batch_id");
            entity.Property(e => e.MobigoWorkOrderId)
                .HasComment("Uppdragsnummer")
                .HasColumnName("mobigo_work_order_id");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.Quantity).HasColumnName("quantity");

            entity.HasOne(d => d.ImportBatch).WithMany(p => p.MobigoMaterialLines)
                .HasForeignKey(d => d.ImportBatchId)
                .HasConstraintName("mobigo_material_line_import_batch_id_fkey");

            entity.HasOne(d => d.Product).WithMany(p => p.MobigoMaterialLines)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("mobigo_material_line_product_id_fkey");
        });

        modelBuilder.Entity<ProductGroup>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("product_group_pkey");

            entity.ToTable("product_group");

            entity.HasIndex(e => e.Name, "product_group_name_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Name)
                .HasComment("Rapporteringsnivå för 3.11/3.12, t.ex. Fläktar")
                .HasColumnName("name");
        });

        modelBuilder.Entity<PurchaseCategory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("purchase_category_pkey");

            entity.ToTable("purchase_category");

            entity.HasIndex(e => e.Name, "purchase_category_name_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Name)
                .HasComment("Kategorier från PropTech för köpt varor, t.ex. Trä, Elektronik")
                .HasColumnName("name");
        });

        modelBuilder.Entity<PurchasedGood>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("purchased_good_pkey");

            entity.ToTable("purchased_good");

            entity.HasIndex(e => e.CreatedBy, "purchased_good_created_by_idx");

            entity.HasIndex(e => e.ImportBatchId, "purchased_good_import_batch_id_idx");

            entity.HasIndex(e => e.SourceRecordId, "purchased_good_source_record_id_key").IsUnique();

            entity.HasIndex(e => e.SupplierId, "purchased_good_supplier_id_idx");

            entity.HasIndex(e => e.TransactionDate, "purchased_good_transaction_date_idx");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.AccountNo)
                .HasComment("Konto, t.ex. 4010")
                .HasColumnName("account_no");
            entity.Property(e => e.Comment)
                .HasComment("Kolumn i Atmoz-excel")
                .HasColumnName("comment");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.GoodsName)
                .HasComment("Vara, ofta okänd vid Visma-import")
                .HasColumnName("goods_name");
            entity.Property(e => e.ImportBatchId)
                .HasComment("Null = manuellt registrerad")
                .HasColumnName("import_batch_id");
            entity.Property(e => e.MeasurementQuality)
                .HasComment("measured/estimated")
                .HasColumnName("measurement_quality");
            entity.Property(e => e.SourceRecordId)
                .HasComment("Visma LevFktnr från 4010 exporten")
                .HasColumnName("source_record_id");
            entity.Property(e => e.Specification)
                .HasComment("Kolumn i Atmoz-excel")
                .HasColumnName("specification");
            entity.Property(e => e.SupplierId).HasColumnName("supplier_id");
            entity.Property(e => e.TransactionDate).HasColumnName("transaction_date");
            entity.Property(e => e.Unit)
                .HasComment("sek, kg, st")
                .HasColumnName("unit");
            entity.Property(e => e.Value)
                .HasComment("Krediter kan vara negativa")
                .HasColumnName("value");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.PurchasedGoods)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("purchased_good_created_by_fkey");

            entity.HasOne(d => d.ImportBatch).WithMany(p => p.PurchasedGoods)
                .HasForeignKey(d => d.ImportBatchId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("purchased_good_import_batch_id_fkey");

            entity.HasOne(d => d.Supplier).WithMany(p => p.PurchasedGoods)
                .HasForeignKey(d => d.SupplierId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("purchased_good_supplier_id_fkey");
        });

        modelBuilder.Entity<PurchasedService>(entity =>
        {
            entity.HasKey(e => new { e.ReportId, e.AccountNo }).HasName("purchased_service_pkey");

            entity.ToTable("purchased_service");

            entity.Property(e => e.ReportId).HasColumnName("report_id");
            entity.Property(e => e.AccountNo)
                .HasComment("Konto så som 5010")
                .HasColumnName("account_no");
            entity.Property(e => e.AmountSek)
                .HasComment("Kostnad som positivt belopp (rapportens tecken vänt)")
                .HasColumnName("amount_sek");

            entity.HasOne(d => d.Report).WithMany(p => p.PurchasedServices)
                .HasForeignKey(d => d.ReportId)
                .HasConstraintName("purchased_service_report_id_fkey");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("role_pkey");

            entity.ToTable("role");

            entity.HasIndex(e => e.Name, "role_name_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Name).HasColumnName("name");
        });

        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.HasKey(e => new { e.RoleId, e.Permission }).HasName("role_permission_pkey");

            entity.ToTable("role_permission");

            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.Permission)
                .HasComment("E.g. import.mobigo")
                .HasColumnName("permission");

            entity.HasOne(d => d.Role).WithMany(p => p.RolePermissions)
                .HasForeignKey(d => d.RoleId)
                .HasConstraintName("role_permission_role_id_fkey");
        });

        modelBuilder.Entity<ServiceCategory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("service_category_pkey");

            entity.ToTable("service_category");

            entity.HasIndex(e => e.Name, "service_category_name_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Name)
                .HasComment("De 26 tjänstekategorierna, t.ex. Fastighetstjänster")
                .HasColumnName("name");
        });

        modelBuilder.Entity<ServiceSpendReport>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("service_spend_report_pkey");

            entity.ToTable("service_spend_report");

            entity.HasIndex(e => e.ImportBatchId, "service_spend_report_import_batch_id_key").IsUnique();

            entity.HasIndex(e => new { e.PeriodFrom, e.PeriodTo }, "service_spend_report_period_from_period_to_idx");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.ImportBatchId).HasColumnName("import_batch_id");
            entity.Property(e => e.PeriodFrom)
                .HasComment("Från rapportens Period-rad")
                .HasColumnName("period_from");
            entity.Property(e => e.PeriodTo).HasColumnName("period_to");

            entity.HasOne(d => d.ImportBatch).WithOne(p => p.ServiceSpendReport)
                .HasForeignKey<ServiceSpendReport>(d => d.ImportBatchId)
                .HasConstraintName("service_spend_report_import_batch_id_fkey");
        });

        modelBuilder.Entity<SoldProduct>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("sold_product_pkey");

            entity.ToTable("sold_product");

            entity.HasIndex(e => e.ArticleNo, "sold_product_article_no_key").IsUnique();

            entity.HasIndex(e => e.GroupId, "sold_product_group_id_idx");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.ArticleNo).HasColumnName("article_no");
            entity.Property(e => e.GroupId).HasColumnName("group_id");
            entity.Property(e => e.KwhPerYear)
                .HasComment("Standardantagande. Null = ej satt")
                .HasColumnName("kwh_per_year");
            entity.Property(e => e.LifetimeYears)
                .HasComment("Standardantagande. Null = ej satt")
                .HasColumnName("lifetime_years");
            entity.Property(e => e.Name).HasColumnName("name");

            entity.HasOne(d => d.Group).WithMany(p => p.SoldProducts)
                .HasForeignKey(d => d.GroupId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("sold_product_group_id_fkey");
        });

        modelBuilder.Entity<SoldProductMaterial>(entity =>
        {
            entity.HasKey(e => new { e.ProductId, e.MaterialId }).HasName("sold_product_material_pkey");

            entity.ToTable("sold_product_material");

            entity.HasIndex(e => e.MaterialId, "sold_product_material_material_id_idx");

            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.MaterialId).HasColumnName("material_id");
            entity.Property(e => e.WeightKg).HasColumnName("weight_kg");

            entity.HasOne(d => d.Material).WithMany(p => p.SoldProductMaterials)
                .HasForeignKey(d => d.MaterialId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("sold_product_material_material_id_fkey");

            entity.HasOne(d => d.Product).WithMany(p => p.SoldProductMaterials)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("sold_product_material_product_id_fkey");
        });

        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("supplier_pkey");

            entity.ToTable("supplier");

            entity.HasIndex(e => e.Name, "supplier_name_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Name)
                .HasComment("Visma-importen matchar leverantör på namn")
                .HasColumnName("name");
        });

        modelBuilder.Entity<SupplierCategoryAllocation>(entity =>
        {
            entity.HasKey(e => new { e.SupplierId, e.PurchaseCategoryId }).HasName("supplier_category_allocation_pkey");

            entity.ToTable("supplier_category_allocation");

            entity.HasIndex(e => e.PurchaseCategoryId, "supplier_category_allocation_purchase_category_id_idx");

            entity.Property(e => e.SupplierId).HasColumnName("supplier_id");
            entity.Property(e => e.PurchaseCategoryId).HasColumnName("purchase_category_id");
            entity.Property(e => e.Share)
                .HasComment("0.5 = 50 procent. Summa per leverantör = 100% (FK-DI.05, kontrolleras i backend/deferred trigger)")
                .HasColumnName("share");

            entity.HasOne(d => d.PurchaseCategory).WithMany(p => p.SupplierCategoryAllocations)
                .HasForeignKey(d => d.PurchaseCategoryId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("supplier_category_allocation_purchase_category_id_fkey");

            entity.HasOne(d => d.Supplier).WithMany(p => p.SupplierCategoryAllocations)
                .HasForeignKey(d => d.SupplierId)
                .HasConstraintName("supplier_category_allocation_supplier_id_fkey");
        });

        modelBuilder.Entity<Vehicle>(entity =>
        {
            entity.HasKey(e => e.RegistrationNumber).HasName("vehicle_pkey");

            entity.ToTable("vehicle");

            entity.Property(e => e.RegistrationNumber).HasColumnName("registration_number");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.FuelType).HasColumnName("fuel_type");
            entity.Property(e => e.VehicleType)
                .HasComment("Bil, Skåpbil")
                .HasColumnName("vehicle_type");
        });

        modelBuilder.Entity<VehicleUsage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("vehicle_usage_pkey");

            entity.ToTable("vehicle_usage");

            entity.HasIndex(e => e.Date, "vehicle_usage_date_idx");

            entity.HasIndex(e => e.ImportBatchId, "vehicle_usage_import_batch_id_idx");

            entity.HasIndex(e => e.RegistrationNumber, "vehicle_usage_registration_number_idx");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Comment).HasColumnName("comment");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.Date)
                .HasComment("Leveransdatum / fakturadatum")
                .HasColumnName("date");
            entity.Property(e => e.EnergyOrigin)
                .HasComment("Vid elfordon")
                .HasColumnName("energy_origin");
            entity.Property(e => e.FuelType)
                .HasComment("El, Diesel, Bensin")
                .HasColumnName("fuel_type");
            entity.Property(e => e.ImportBatchId).HasColumnName("import_batch_id");
            entity.Property(e => e.MeasurementQuality).HasColumnName("measurement_quality");
            entity.Property(e => e.RegistrationNumber).HasColumnName("registration_number");
            entity.Property(e => e.SourceRecordId)
                .HasComment("Fakturanummer")
                .HasColumnName("source_record_id");
            entity.Property(e => e.SourceSystem).HasColumnName("source_system");
            entity.Property(e => e.Unit)
                .HasComment("liter, kWh")
                .HasColumnName("unit");
            entity.Property(e => e.Value).HasColumnName("value");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.VehicleUsages)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("vehicle_usage_created_by_fkey");

            entity.HasOne(d => d.ImportBatch).WithMany(p => p.VehicleUsages)
                .HasForeignKey(d => d.ImportBatchId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("vehicle_usage_import_batch_id_fkey");

            entity.HasOne(d => d.RegistrationNumberNavigation).WithMany(p => p.VehicleUsages)
                .HasForeignKey(d => d.RegistrationNumber)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("vehicle_usage_registration_number_fkey");
        });

        modelBuilder.Entity<Waste>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("waste_pkey");

            entity.ToTable("waste");

            entity.HasIndex(e => e.Date, "waste_date_idx");

            entity.HasIndex(e => e.WasteTreatmentId, "waste_waste_treatment_id_idx");

            entity.HasIndex(e => e.WasteTypeId, "waste_waste_type_id_idx");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Comment).HasColumnName("comment");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.Date).HasColumnName("date");
            entity.Property(e => e.MeasurementQuality).HasColumnName("measurement_quality");
            entity.Property(e => e.WasteTreatmentId).HasColumnName("waste_treatment_id");
            entity.Property(e => e.WasteTypeId).HasColumnName("waste_type_id");
            entity.Property(e => e.WeightKg).HasColumnName("weight_kg");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Wastes)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("waste_created_by_fkey");

            entity.HasOne(d => d.WasteTreatment).WithMany(p => p.Wastes)
                .HasForeignKey(d => d.WasteTreatmentId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("waste_waste_treatment_id_fkey");

            entity.HasOne(d => d.WasteType).WithMany(p => p.Wastes)
                .HasForeignKey(d => d.WasteTypeId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("waste_waste_type_id_fkey");
        });

        modelBuilder.Entity<WasteTreatment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("waste_treatment_pkey");

            entity.ToTable("waste_treatment");

            entity.HasIndex(e => e.Name, "waste_treatment_name_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Name)
                .HasComment("Materialåtervinning, deponi etc")
                .HasColumnName("name");
        });

        modelBuilder.Entity<WasteType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("waste_type_pkey");

            entity.ToTable("waste_type");

            entity.HasIndex(e => e.Name, "waste_type_name_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Name)
                .HasComment("Papper och kartong, plast, elektronik etc")
                .HasColumnName("name");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
