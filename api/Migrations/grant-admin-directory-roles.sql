-- ============================================================================
-- Grant the Administrator DIRECTORY role to the former hard-coded master admins
-- ----------------------------------------------------------------------------
-- The in-code master-admin list (contracts/Models/JpmsAdministrators.cs) has
-- been removed: administrators are now administered in the directory like any
-- other role, and role resolution expands a directory Admin role to every role.
--
-- Run this against the Azure SQL database BEFORE (or with) the deploy that
-- removes the hard-coded list, or those accounts sign in with no roles:
--
--     bash infra/run-grant-admin-directory-roles.sh
--
-- Idempotent: safe to run more than once. Creates the DirectoryUsers row only
-- if missing (never overwrites an existing display name) and the Admin role row
-- (Role 0 = Admin, contracts/Models/Role.cs) only if missing.
--
-- NOTE: sessions cache their role list — each account should sign out and back
-- in after this runs to pick the role up straight away.
-- ============================================================================

SET NOCOUNT ON;

DECLARE @admins TABLE (Email nvarchar(256), DisplayName nvarchar(256));
INSERT INTO @admins (Email, DisplayName) VALUES
    (N'james.beadle@jewelbb.co.uk',          N'James Beadle'),
    (N'nigel.reilly@jewelenterprises.co.uk', N'Nigel Reilly'),
    -- Delete this line before running if the admin.james account is defunct —
    -- without a directory row it simply signs in with no roles from now on.
    (N'admin.james@jewelenterprises.co.uk',  N'James Beadle (Admin)');

-- Directory row first: the role rows hang off the user's email ---------------
INSERT INTO [DirectoryUsers] ([Email], [DisplayName], [SubcontractorId])
SELECT a.Email, a.DisplayName, NULL
FROM @admins a
WHERE NOT EXISTS (SELECT 1 FROM [DirectoryUsers] u WHERE u.Email = a.Email);

-- The Administrator role row (Role 0 = Admin) --------------------------------
INSERT INTO [DirectoryUserRoles] ([DirectoryUserRoleId], [DirectoryUserEmail], [Role])
SELECT LOWER(REPLACE(CONVERT(nvarchar(36), NEWID()), '-', '')), a.Email, 0
FROM @admins a
WHERE NOT EXISTS (
    SELECT 1 FROM [DirectoryUserRoles] r
    WHERE r.DirectoryUserEmail = a.Email AND r.Role = 0);

-- Read back what each account now holds --------------------------------------
SELECT u.[Email], u.[DisplayName], r.[Role]
FROM [DirectoryUsers] u
LEFT JOIN [DirectoryUserRoles] r ON r.[DirectoryUserEmail] = u.[Email]
WHERE u.[Email] IN (SELECT Email FROM @admins)
ORDER BY u.[Email], r.[Role];
