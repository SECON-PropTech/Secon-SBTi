using System.Globalization;
using System.IO.Compression;
using System.Security.Claims;
using System.Text;
using System.Xml.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SeconSbtiBackend.Controllers;
using SeconSbtiBackend.Data;
using SeconSbtiBackend.Importing.Visma4010;
using tests.IntegrationTests.PostgresTests.Infrastructure;

namespace tests.IntegrationTests.PostgresTests;

public sealed class Visma4010ImportRollbackTests(PostgresFixture fixture) : DbTestBase(fixture)
{
    [Fact]
    public async Task Failure_after_the_first_purchase_rolls_back_everything_and_allows_a_retry()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var connectionString = await Fixture.CreateDatabaseAsync();
        await PostgresFixture.ApplyMigrationsAsync(connectionString);
        var userId = await ScalarOnAsync<Guid>(connectionString,
            "INSERT INTO app_user(first_name,last_name) VALUES('Import','Tester') RETURNING id");
        var existingSupplier = await ScalarOnAsync<Guid>(connectionString,
            "INSERT INTO supplier(name) VALUES('Existing supplier') RETURNING id");
        await ExecOnAsync(connectionString, """
            INSERT INTO purchased_good(supplier_id,transaction_date,value,unit,measurement_quality,created_by)
            VALUES($1, DATE '2026-01-01', 10, 'sek', 'measured', $2)
            """, existingSupplier, userId);
        var before = await ReadSnapshotAsync(connectionString);

        // Sequences survive rollback, so this also proves that saving reached the second purchase.
        await ExecOnAsync(connectionString, """
            CREATE SEQUENCE import_test_inserts;
            CREATE FUNCTION fail_second_import_purchase() RETURNS trigger LANGUAGE plpgsql AS $$
            BEGIN
                IF nextval('import_test_inserts') = 2 THEN
                    RAISE EXCEPTION 'Forced import test failure';
                END IF;
                RETURN NEW;
            END;
            $$;
            CREATE TRIGGER fail_import_purchase BEFORE INSERT ON purchased_good
            FOR EACH ROW EXECUTE FUNCTION fail_second_import_purchase();
            """);

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString).Options;
        using var services = new ServiceCollection().AddLogging().AddControllers().Services.BuildServiceProvider();
        using var fileStream = CreateFile();
        var file = new FormFile(fileStream, 0, fileStream.Length, "file", "test.xlsx");

        await using (var context = new ApplicationDbContext(options))
        {
            var controller = CreateController(context, services, userId);
            var response = await controller.Import(file, CancellationToken.None);
            var result = Assert.IsType<ObjectResult>(response.Result);
            var problem = Assert.IsType<ProblemDetails>(result.Value);

            Assert.Equal(StatusCodes.Status500InternalServerError, result.StatusCode);
            Assert.Contains("Förhandsgranska filen igen", problem.Detail!);
            Assert.DoesNotContain("Forced import test failure", problem.Detail!);
        }

        Assert.Equal(2L, await ScalarOnAsync<long>(connectionString, "SELECT last_value FROM import_test_inserts"));
        Assert.Equal(before, await ReadSnapshotAsync(connectionString));

        await ExecOnAsync(connectionString, "DROP TRIGGER fail_import_purchase ON purchased_good");
        fileStream.Position = 0;

        await using (var context = new ApplicationDbContext(options))
        {
            var controller = CreateController(context, services, userId);
            var response = await controller.Import(file, CancellationToken.None);
            var result = Assert.IsType<ObjectResult>(response.Result);
            var imported = Assert.IsType<Visma4010ImportResponse>(result.Value);

            Assert.Equal(StatusCodes.Status201Created, result.StatusCode);
            Assert.NotNull(imported.ImportBatchId);
            Assert.Equal(2, imported.ToImport.Count);
            Assert.Equal(300m, imported.ToImport.NetSek);
        }

        var after = await ReadSnapshotAsync(connectionString);
        Assert.Equal(before.Batches + 1, after.Batches);
        Assert.Equal(before.Files + 1, after.Files);
        Assert.Equal(before.Suppliers + 1, after.Suppliers);
        Assert.Equal(before.Goods + 2, after.Goods);
        Assert.Equal(before.Total + 300m, after.Total);

        fileStream.Position = 0;
        await using (var context = new ApplicationDbContext(options))
        {
            var controller = CreateController(context, services, userId);
            var response = await controller.Import(file, CancellationToken.None);
            var result = Assert.IsType<ConflictObjectResult>(response.Result);
            var blocked = Assert.IsType<Visma4010ImportResponse>(result.Value);
            Assert.Contains(Visma4010Blockers.FileAlreadyImported, blocked.Blockers);
        }

        Assert.Equal(after, await ReadSnapshotAsync(connectionString));
    }

    private static Visma4010ImportsController CreateController(
        ApplicationDbContext context, IServiceProvider services, Guid userId)
    {
        var controller = ActivatorUtilities.CreateInstance<Visma4010ImportsController>(
            services, new Visma4010ImportService(context));
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                RequestServices = services,
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.NameIdentifier, userId.ToString())], "Test"))
            }
        };
        return controller;
    }

    private static async Task<(long Batches, long Files, long Suppliers, long Goods, decimal Total)>
        ReadSnapshotAsync(string connectionString) =>
        (
            await ScalarOnAsync<long>(connectionString, "SELECT count(*) FROM import_batch"),
            await ScalarOnAsync<long>(connectionString, "SELECT count(*) FROM import_batch_file"),
            await ScalarOnAsync<long>(connectionString, "SELECT count(*) FROM supplier"),
            await ScalarOnAsync<long>(connectionString, "SELECT count(*) FROM purchased_good"),
            await ScalarOnAsync<decimal>(connectionString, "SELECT coalesce(sum(value),0) FROM purchased_good")
        );

    // Synthetic Excel data keeps this test independent of Secon's exports and pending PR #146.
    private static MemoryStream CreateFile()
    {
        object?[][] rows =
        [
            ["Test company"],
            ["Huvudbok"],
            ["Period: 2026-01-01 - 2026-12-31"],
            ["Konto", "Namn", "Vernr", "Datum", "Text", "Debet", "Kredit", "Saldo"],
            ["4010", "Inköp"],
            [null, null, "A1", new DateTime(2026, 1, 10), "Levfkt 410001 New supplier", 100m, 0m, 100m],
            [null, null, "A2", new DateTime(2026, 1, 11), "Levfkt 410002 New supplier", 200m, 0m, 300m],
            [null, null, null, "Omslutning:", null, 300m, 0m, 300m],
            [null, null, null, "Utgående saldo:", null, 300m, 0m, 300m]
        ];
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        var sheetData = new XElement(ns + "sheetData");
        for (var rowIndex = 0; rowIndex < rows.Length; rowIndex++)
        {
            var row = new XElement(ns + "row", new XAttribute("r", rowIndex + 1));
            for (var column = 0; column < rows[rowIndex].Length; column++)
            {
                var value = rows[rowIndex][column];
                if (value is null) continue;
                var cell = new XElement(ns + "c", new XAttribute("r", $"{(char)('A' + column)}{rowIndex + 1}"));
                if (value is string text)
                {
                    cell.Add(new XAttribute("t", "inlineStr"), new XElement(ns + "is", new XElement(ns + "t", text)));
                }
                else if (value is DateTime date)
                {
                    cell.Add(new XAttribute("s", 1), new XElement(ns + "v", date.ToOADate().ToString(CultureInfo.InvariantCulture)));
                }
                else
                {
                    cell.Add(new XElement(ns + "v", Convert.ToString(value, CultureInfo.InvariantCulture)));
                }
                row.Add(cell);
            }
            sheetData.Add(row);
        }

        var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            void Add(string name, string content)
            {
                using var writer = new StreamWriter(archive.CreateEntry(name).Open());
                writer.Write(content);
            }
            Add("[Content_Types].xml", """
                <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
                  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
                  <Default Extension="xml" ContentType="application/xml"/>
                  <Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>
                  <Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
                  <Override PartName="/xl/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"/>
                </Types>
                """);
            Add("_rels/.rels", """
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
                  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/>
                </Relationships>
                """);
            Add("xl/workbook.xml", """
                <workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
                  <sheets><sheet name="Huvudbok" sheetId="1" r:id="rId1"/></sheets>
                </workbook>
                """);
            Add("xl/_rels/workbook.xml.rels", """
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
                  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/>
                  <Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/>
                </Relationships>
                """);
            Add("xl/styles.xml", """
                <styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
                  <cellXfs count="2"><xf numFmtId="0"/><xf numFmtId="14"/></cellXfs>
                </styleSheet>
                """);
            Add("xl/worksheets/sheet1.xml", new XElement(ns + "worksheet", sheetData).ToString());
        }
        stream.Position = 0;
        return stream;
    }
}
