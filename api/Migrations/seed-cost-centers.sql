-- ============================================================================
-- Seed the GLOBAL cost-center master  (v2 -- Jewel master cost codes)
-- ----------------------------------------------------------------------------
-- One shared cost-center list used by every project's Financials tab and the
-- Valuation Report CODE column. v2 replaces the original JBB-* NRM2 element
-- buckets with the granular Jewel master cost codes (00001..00137) agreed for
-- cost-code reconciliation (source workbook: CostCodes_20260707.xlsx). These
-- codes also match the "Cost Code" tracking category options used in Xero, so
-- purchase invoices can be matched against valuation lines per code.
--
-- This script OWNS the [CostCenters] table for BOOTSTRAP purposes: it creates
-- the table if missing and seeds it. Run against an EMPTY database only.
--
-- ⚠ For an EXISTING database use migrate-valuation-cost-codes.sql instead:
-- it seeds these same codes, retires the old JBB-* rows, AND remaps every
-- seeded ValuationLineItem to its new code. Re-running THIS script on a live
-- database retires every code added or renamed through the app (the NOT
-- MATCHED BY SOURCE clause) -- do not do that.
--
-- Idempotent: rows are matched on CostCenterId; Code / Name / SortOrder are
-- refreshed and the row re-activated on a re-run. Any cost centre NOT in the
-- list below is deactivated (IsActive = 0), never deleted.
-- ============================================================================

IF OBJECT_ID(N'[dbo].[CostCenters]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[CostCenters] (
        [CostCenterId] nvarchar(64)  NOT NULL,
        [Code]         nvarchar(32)  NOT NULL,
        [Name]         nvarchar(256) NOT NULL,
        [SortOrder]    int           NOT NULL,
        [IsActive]     bit           NOT NULL CONSTRAINT [DF_CostCenters_IsActive] DEFAULT (1),
        CONSTRAINT [PK_CostCenters] PRIMARY KEY ([CostCenterId])
    );
END;
GO

MERGE INTO [dbo].[CostCenters] AS target
USING (VALUES
    (N'cc-00001', N'00001', N'Hoarding', 10),
    (N'cc-00002', N'00002', N'Site set up', 20),
    (N'cc-00003', N'00003', N'Site temp security', 30),
    (N'cc-00004', N'00004', N'Welfare', 40),
    (N'cc-00005', N'00005', N'Health and Safety ongoing', 50),
    (N'cc-00006', N'00006', N'Health & Safety CDM', 60),
    (N'cc-00007', N'00007', N'Temp toilet', 70),
    (N'cc-00008', N'00008', N'General protection', 80),
    (N'cc-00009', N'00009', N'Weather protection', 90),
    (N'cc-00010', N'00010', N'Structural steel', 100),
    (N'cc-00011', N'00011', N'Designer steel', 110),
    (N'cc-00012', N'00012', N'Mesh Steel works', 120),
    (N'cc-00013', N'00013', N'Metal Railings and balustrade', 130),
    (N'cc-00014', N'00014', N'Glass railing and balustrade', 140),
    (N'cc-00015', N'00015', N'Labour', 150),
    (N'cc-00016', N'00016', N'Site manager', 160),
    (N'cc-00017', N'00017', N'Project manager', 170),
    (N'cc-00018', N'00018', N'Demolition', 180),
    (N'cc-00019', N'00019', N'Structural support', 190),
    (N'cc-00020', N'00020', N'Scaffolding', 200),
    (N'cc-00021', N'00021', N'Excavation', 210),
    (N'cc-00022', N'00022', N'Groundworks', 220),
    (N'cc-00023', N'00023', N'Drainage Below ground', 230),
    (N'cc-00024', N'00024', N'Drainage internal', 240),
    (N'cc-00025', N'00025', N'Piling', 250),
    (N'cc-00026', N'00026', N'Under pining', 260),
    (N'cc-00027', N'00027', N'Concrete', 270),
    (N'cc-00028', N'00028', N'Masonry brickworks', 280),
    (N'cc-00029', N'00029', N'Masonry stonework', 290),
    (N'cc-00030', N'00030', N'Roofer', 300),
    (N'cc-00031', N'00031', N'Roofing tile new', 310),
    (N'cc-00032', N'00032', N'Roofing tile old', 320),
    (N'cc-00033', N'00033', N'Roofing flat membrane', 330),
    (N'cc-00034', N'00034', N'Roofing lead works', 340),
    (N'cc-00035', N'00035', N'Facia and soffit UPVC', 350),
    (N'cc-00036', N'00036', N'Gutters and rainwater UPVC', 360),
    (N'cc-00037', N'00037', N'Facia and soffit Metal', 370),
    (N'cc-00038', N'00038', N'Gutters and rainwater Metal', 380),
    (N'cc-00039', N'00039', N'Carpentry cut Roofing', 390),
    (N'cc-00040', N'00040', N'Carpentry truss roofing', 400),
    (N'cc-00041', N'00041', N'Carpentry 1st fix', 410),
    (N'cc-00042', N'00042', N'Carpentry 2nd fix', 420),
    (N'cc-00043', N'00043', N'Carpentry kitchens', 430),
    (N'cc-00044', N'00044', N'Internal Doors & linings', 440),
    (N'cc-00045', N'00045', N'Joinery', 450),
    (N'cc-00046', N'00046', N'Wardrobes', 460),
    (N'cc-00047', N'00047', N'Stairs metal', 470),
    (N'cc-00048', N'00048', N'Stairs timber', 480),
    (N'cc-00049', N'00049', N'Stairs glass', 490),
    (N'cc-00050', N'00050', N'Timber windows and doors', 500),
    (N'cc-00051', N'00051', N'UPVC window and doors', 510),
    (N'cc-00052', N'00052', N'Aluminium window and doors', 520),
    (N'cc-00053', N'00053', N'Internal specialist window and doors', 530),
    (N'cc-00054', N'00054', N'Specialist glazing', 540),
    (N'cc-00055', N'00055', N'MF ceilings', 550),
    (N'cc-00056', N'00056', N'Metal and Gypliner walls', 560),
    (N'cc-00057', N'00057', N'Insulation walls', 570),
    (N'cc-00058', N'00058', N'Insulation ceilings', 580),
    (N'cc-00059', N'00059', N'Insulation floors', 590),
    (N'cc-00060', N'00060', N'Plaster Boarding', 600),
    (N'cc-00061', N'00061', N'Plaster boarding Dot & dab', 610),
    (N'cc-00062', N'00062', N'Plastering', 620),
    (N'cc-00063', N'00063', N'Rendering', 630),
    (N'cc-00064', N'00064', N'Spray plaster and render', 640),
    (N'cc-00065', N'00065', N'Specialist plastering', 650),
    (N'cc-00066', N'00066', N'Coving', 660),
    (N'cc-00067', N'00067', N'Screed', 670),
    (N'cc-00068', N'00068', N'Floor finish timber', 680),
    (N'cc-00069', N'00069', N'Floor finish carpet', 690),
    (N'cc-00070', N'00070', N'Floor finish LVT/Lino', 700),
    (N'cc-00071', N'00071', N'Floor finish self levelling', 710),
    (N'cc-00072', N'00072', N'Electrician', 720),
    (N'cc-00073', N'00073', N'Specialist electrician', 730),
    (N'cc-00074', N'00074', N'Plumber', 740),
    (N'cc-00075', N'00075', N'Specialist plumbing', 750),
    (N'cc-00076', N'00076', N'Tiling', 760),
    (N'cc-00077', N'00077', N'Tiling Stone', 770),
    (N'cc-00078', N'00078', N'Tiling marble', 780),
    (N'cc-00079', N'00079', N'Tanking internal', 790),
    (N'cc-00080', N'00080', N'Tanking external', 800),
    (N'cc-00081', N'00081', N'Damp proofing specialist', 810),
    (N'cc-00082', N'00082', N'Decorating', 820),
    (N'cc-00083', N'00083', N'Wallpapering', 830),
    (N'cc-00084', N'00084', N'Blinds and curtains', 840),
    (N'cc-00085', N'00085', N'CCTV', 850),
    (N'cc-00086', N'00086', N'Entry systems', 860),
    (N'cc-00087', N'00087', N'Alarms and security', 870),
    (N'cc-00088', N'00088', N'Heat Source', 880),
    (N'cc-00089', N'00089', N'Air con', 890),
    (N'cc-00090', N'00090', N'Boilers', 900),
    (N'cc-00091', N'00091', N'Gas, electric or oil boilers', 910),
    (N'cc-00092', N'00092', N'Solar panels', 920),
    (N'cc-00093', N'00093', N'Under-floor heating', 930),
    (N'cc-00094', N'00094', N'EV car chargers', 940),
    (N'cc-00095', N'00095', N'Audio visual and sound systems', 950),
    (N'cc-00096', N'00096', N'Fire and smoke alarms', 960),
    (N'cc-00097', N'00097', N'Passive fire protection', 970),
    (N'cc-00098', N'00098', N'Fire stopping', 980),
    (N'cc-00099', N'00099', N'Lifts and hoists', 990),
    (N'cc-00100', N'00100', N'Swimming pools', 1000),
    (N'cc-00101', N'00101', N'Spa and hot tubs', 1010),
    (N'cc-00102', N'00102', N'Gazebo, carports and awning', 1020),
    (N'cc-00103', N'00103', N'Fires, chimney and stoves', 1030),
    (N'cc-00104', N'00104', N'Garage doors', 1040),
    (N'cc-00105', N'00105', N'Stone copings and cappings', 1050),
    (N'cc-00106', N'00106', N'External timber cladding', 1060),
    (N'cc-00107', N'00107', N'External stone cladding', 1070),
    (N'cc-00108', N'00108', N'Decking', 1080),
    (N'cc-00109', N'00109', N'Temporary works', 1090),
    (N'cc-00110', N'00110', N'Metal capping and coping', 1100),
    (N'cc-00111', N'00111', N'Ventilation and extract', 1110),
    (N'cc-00112', N'00112', N'Core drilling', 1120),
    (N'cc-00113', N'00113', N'Paving and roads', 1130),
    (N'cc-00114', N'00114', N'Asbestos removal', 1140),
    (N'cc-00115', N'00115', N'Fencing', 1150),
    (N'cc-00116', N'00116', N'Turfing', 1160),
    (N'cc-00117', N'00117', N'Landscaping', 1170),
    (N'cc-00118', N'00118', N'BBQ', 1180),
    (N'cc-00119', N'00119', N'Sheds and out houses', 1190),
    (N'cc-00120', N'00120', N'Utilities', 1200),
    (N'cc-00121', N'00121', N'Trenching, bore holes and services', 1210),
    (N'cc-00122', N'00122', N'Sanitary supply', 1220),
    (N'cc-00123', N'00123', N'Door supply', 1230),
    (N'cc-00124', N'00124', N'Tiling supply', 1240),
    (N'cc-00125', N'00125', N'Kitchen supply', 1250),
    (N'cc-00126', N'00126', N'Appliance and machinery supply', 1260),
    (N'cc-00127', N'00127', N'Furniture supply', 1270),
    (N'cc-00128', N'00128', N'Ironmongery supply', 1280),
    (N'cc-00129', N'00129', N'Cleaning external', 1290),
    (N'cc-00130', N'00130', N'Cleaning internal', 1300),
    (N'cc-00131', N'00131', N'Rubbish clearance', 1310),
    (N'cc-00132', N'00132', N'Photography', 1320),
    (N'cc-00133', N'00133', N'Marketing', 1330),
    (N'cc-00134', N'00134', N'Misc', 1340),
    (N'cc-00135', N'00135', N'Specialist', 1350),
    (N'cc-00136', N'00136', N'Nominated contractor', 1360),
    (N'cc-00137', N'00137', N'Nominated supplier', 1370)
) AS source ([CostCenterId], [Code], [Name], [SortOrder])
ON target.[CostCenterId] = source.[CostCenterId]
WHEN MATCHED THEN
    UPDATE SET
        target.[Code]      = source.[Code],
        target.[Name]      = source.[Name],
        target.[SortOrder] = source.[SortOrder],
        target.[IsActive]  = 1
WHEN NOT MATCHED BY TARGET THEN
    INSERT ([CostCenterId], [Code], [Name], [SortOrder], [IsActive])
    VALUES (source.[CostCenterId], source.[Code], source.[Name], source.[SortOrder], 1)
WHEN NOT MATCHED BY SOURCE THEN
    -- Any cost centre no longer in the master list above is retired, not deleted.
    UPDATE SET target.[IsActive] = 0;
GO

SELECT [SortOrder], [Code], [Name], [IsActive]
FROM [dbo].[CostCenters]
ORDER BY [SortOrder];
