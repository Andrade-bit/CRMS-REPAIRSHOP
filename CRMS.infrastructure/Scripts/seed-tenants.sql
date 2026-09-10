-- Run against your MASTER database (db66485) in SSMS.
-- Registers tenant a/b/c so TenantConnectionService can resolve their
-- connection strings by CompanyId at runtime.
--
-- Fill in each <...PASSWORD...> from MonsterASP.net's "Manage" button on
-- that tenant database (the key icon next to its name).

INSERT INTO dbo.Companies (CompanyCode, CompanyName, IsActive, CreateAt)
VALUES
    ('TENANT-A', 'Tenant A', 1, SYSUTCDATETIME()),
    ('TENANT-B', 'Tenant B', 1, SYSUTCDATETIME()),
    ('TENANT-C', 'Tenant C', 1, SYSUTCDATETIME());

INSERT INTO dbo.CompanyDatabases (CompanyId, ServerName, DatabaseName, ConnectionString, IsActive)
VALUES
    (
        (SELECT CompanyId FROM dbo.Companies WHERE CompanyCode = 'TENANT-A'),
        'db67075.public.databaseasp.net,1433',
        'db67075',
        'Server=db67075.public.databaseasp.net,1433;Database=db67075;User Id=db67075;Password=<TENANT_A_PASSWORD>;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;',
        1
    ),
    (
        (SELECT CompanyId FROM dbo.Companies WHERE CompanyCode = 'TENANT-B'),
        'db67077.public.databaseasp.net,1433',
        'db67077',
        'Server=db67077.public.databaseasp.net,1433;Database=db67077;User Id=db67077;Password=<TENANT_B_PASSWORD>;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;',
        1
    ),
    (
        (SELECT CompanyId FROM dbo.Companies WHERE CompanyCode = 'TENANT-C'),
        'db67078.public.databaseasp.net,1433',
        'db67078',
        'Server=db67078.public.databaseasp.net,1433;Database=db67078;User Id=db67078;Password=<TENANT_C_PASSWORD>;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;',
        1
    );
