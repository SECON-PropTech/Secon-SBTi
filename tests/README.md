# API-tests

Automated tests for the Secon SBTi backend (`SeconSbtiBackend`).

The tests are split into two groups with different requirements:

| Group | What it tests | Needs Docker? |
|---|---|---|
| **Unit / controller / service tests** | Controllers, services, DTO validation, calculations, auth | No (uses EF Core in-memory database) |
| **PostgreSQL integration tests** | The real SQL schema, constraints, migrations, EF model vs. schema, backup/restore | **Yes** (Testcontainers) |

## Prerequisites

### Always required

- [.NET SDK](https://dotnet.microsoft.com/download) matching the backend project (check `TargetFramework` in `SeconSbtiBackend.csproj`)
- Git

### Required for the PostgreSQL integration tests

1. **Docker** installed and **running** (Docker Desktop on Windows/macOS, Docker Engine on Linux).
   Verify with:
   ```bash
   docker info
   ```
2. Internet access the first time, so Docker can pull the `postgres:16-alpine` image (and the Testcontainers helper image). Later runs use the cached image.

That is all. You do **not** need to install PostgreSQL locally: even the backup/restore test runs `pg_dump` and `pg_restore` inside the container, so it works the same on Windows, macOS, Linux and CI.

## How the PostgreSQL tests work

`PostgresFixture` (shared by all tests in the `"db"` collection) does the following:

1. Starts a throw-away **PostgreSQL 16 container** (`postgres:16-alpine`) with Testcontainers.
2. Creates an isolated database named `test_<guid>` inside the container.
3. Applies every `V<number>__*.sql` file from the `migrations` folder next to the test binaries, in version order (V1, then V2, ...).
4. Hands the connection string to the tests through `DbTestBase`.
5. When all tests are done, drops the databases and removes the container.

Because the schema is created by the **same SQL migrations used in production**, these tests verify the real constraints (CHECK, UNIQUE, foreign keys, cascades) and that the EF Core model in `ApplicationDbContext` matches the database.

You do **not** need to start PostgreSQL yourself or configure a connection string for the tests. The container uses its own random port, user `postgres` and password `postgres`, and does not touch your local development database.

### Backup and restore test

`BackupRestoreTests` creates a custom-format dump with `pg_dump -Fc`, restores it into a new database with `pg_restore`, and checks that the data is still there. Both tools are executed **inside the container** through `PostgresFixture.RunInContainerAsync`, so they always match the server version and the dump file never leaves the container. Both exit codes are asserted, so a failed dump or restore fails the test with the tool's own error message.


### Adding a new migration

Add `V3__something.sql` to `infrastructure/migrations/`. It is picked up automatically (the name must match `V<number>__<name>.sql`). Remember to update the EF model (`db-scaffold-command.txt`) and the tests if the schema changes. `EfModelMatchesSchemaTests` will fail if the model and schema drift apart.

## Running the tests

All commands are run from the `backend/` folder (where the solution file is), or from `tests`.

### Everything

```bash
dotnet test
```

### Only tests that do NOT need Docker

```bash
dotnet test --filter "FullyQualifiedName!~PostgresTests"
```

### Only the PostgreSQL / Testcontainers tests

```bash
dotnet test --filter "FullyQualifiedName~PostgresTests"
```

### A single test class or test

```bash
dotnet test --filter "FullyQualifiedName~PurchasedGoodTests"
dotnet test --filter "FullyQualifiedName~Source_record_id_is_unique_and_negative_values_are_allowed"
```

### More output while debugging

```bash
dotnet test --logger "console;verbosity=detailed"
```

In Visual Studio you can also use **Test Explorer** (Docker must be running before you run the PostgreSQL tests). In VS Code/Rider use the built-in test runner.

## Troubleshooting

| Problem | Likely cause / fix |
|---|---|
| `Docker is either not running or misconfigured` | Start Docker Desktop and wait until it says it is running, then run again. |
| First PostgreSQL test run is very slow | Docker is pulling `postgres:16-alpine`. Later runs are fast. |
| `No migration files found in ...` | The migrations are not copied to the output folder. Check the `<Content>` item in the test `.csproj` (see above). |
| `pg_dump failed` / `pg_restore failed` | Read the error message in the assertion; it comes from the tool inside the container. Check that the migrations were applied and the container is healthy. |
| Containers left over after a crashed run | Testcontainers cleans up automatically (Ryuk). If something remains: `docker ps -a` and `docker rm -f <id>`. |
| Tests fail with `EF model/schema drift` | The migrations and `ApplicationDbContext` / models are out of sync. Update the model (re-scaffold) or add a migration. |
| Corporate network/VPN blocks the image pull | Pull the image manually first: `docker pull postgres:16-alpine`. |

## Notes for writing new tests

- **Database-constraint tests**: inherit from `DbTestBase`, use the `Insert...Async` helpers and `AssertRejectedAsync(CheckViolation | UniqueViolation | ForeignKeyViolation, sql, args)`. Use `Unique("prefix")` for names so tests do not collide, since tests share one database.
- **Controller/service tests**: use `TestDatabase.Create()` for an isolated in-memory database. No Docker needed.
- The in-memory provider does **not** enforce CHECK constraints, unique indexes from raw SQL or foreign keys the way PostgreSQL does. If the behaviour depends on the real database, write it as a PostgreSQL test.
- To run a PostgreSQL command inside the container from a test, use `Fixture.RunInContainerAsync("command", "arg1", ...)` and assert on `ExitCode`.
