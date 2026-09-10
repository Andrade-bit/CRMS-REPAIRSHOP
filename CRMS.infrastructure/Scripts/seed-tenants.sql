-- Run against your MASTER database (db66485) in SSMS.
--
-- Tenant DB credentials themselves are now hardcoded in CRMS.api's
-- appsettings.json ("TenantCredentials" section), not stored in a table —
-- see TenantConnectionService.cs. All this script needs to do is register
-- each tenant's Company row, so a logged-in Admin's CompanyId claim can be
-- resolved to a CompanyCode, which is the key TenantConnectionService looks
-- up in that config section. CompanyCode below must match the
-- "TenantCredentials:<code>" key exactly (TenantA / TenantB / TenantC).

INSERT INTO dbo.Companies (CompanyCode, CompanyName, IsActive, CreateAt)
VALUES
    ('TenantA', 'Tenant A', 1, SYSUTCDATETIME()),
    ('TenantB', 'Tenant B', 1, SYSUTCDATETIME()),
    ('TenantC', 'Tenant C', 1, SYSUTCDATETIME());

-- Next step: create each tenant's Admin account via POST /api/auth/register-temp,
-- passing "companyId" as the CompanyId returned for that row above (or query
-- SELECT CompanyId, CompanyCode FROM dbo.Companies to find it) — that's what
-- ties that Admin's login to this tenant automatically.
