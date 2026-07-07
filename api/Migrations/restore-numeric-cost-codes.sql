-- ⛔ DO NOT RUN — WITHDRAWN 2026-07-07 (second decision): the JBB Cost Code
-- Master (trade-prefixed) is confirmed as the master. This restore script
-- would push everything back onto the retired numeric codes. Audit only.
THROW 50000, 'WITHDRAWN: do not run restore-numeric-cost-codes.sql. The JBB trade-prefixed master is confirmed.', 1;
GO

-- ============================================================================
-- RESTORE: revert the cost-code master to the numbered Jewel list (00001..00137)
-- ----------------------------------------------------------------------------
-- Corrects the erroneous v2 migration that moved the master onto trade-prefixed
-- codes (PRELIMS-*, ENABLE-*, ...). The numbered list in CostCodes_20260707.xlsx
-- is the master. This script:
--   1. re-seeds and re-activates the 137 numeric codes
--   2. retires every non-numeric code (trade-prefixed, JBB-*, strays)
--   3. reverts ValuationLineItems.CostCode and XeroLedgerLines.CostCenterCode
--      via the exact inverse of the v2 mapping (1:1, so allocations made while
--      the prefixed codes were live revert cleanly; ENABLE-GRB "Grab loads",
--      which had no numeric counterpart, reverts to 00131 Rubbish clearance)
--
-- Idempotent and transactional; amounts are never touched. Safe to re-run.
-- ============================================================================

SET XACT_ABORT ON;
BEGIN TRANSACTION;

DECLARE @valTotalBefore decimal(38,4) = (SELECT SUM([LineAmount]) FROM [dbo].[ValuationLineItems]);

-- 1. Re-seed the numeric master ------------------------------------------------
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
    UPDATE SET target.[Code] = source.[Code], target.[Name] = source.[Name],
               target.[SortOrder] = source.[SortOrder], target.[IsActive] = 1
WHEN NOT MATCHED BY TARGET THEN
    INSERT ([CostCenterId], [Code], [Name], [SortOrder], [IsActive])
    VALUES (source.[CostCenterId], source.[Code], source.[Name], source.[SortOrder], 1);

-- 2. Retire everything that is not a numeric master code ------------------------
UPDATE [dbo].[CostCenters]
SET [IsActive] = 0
WHERE [Code] NOT LIKE N'00[0-9][0-9][0-9]';

-- 3. Revert valuation lines and Xero allocations (prefixed -> numeric) ----------
DECLARE @map TABLE ([OldCode] nvarchar(32) PRIMARY KEY, [NewCode] nvarchar(32));
INSERT INTO @map ([OldCode], [NewCode]) VALUES
    (N'CARP-1FX', N'00041'),
    (N'CARP-2FX', N'00042'),
    (N'CARP-CUT', N'00039'),
    (N'CARP-DOR', N'00044'),
    (N'CARP-JNR', N'00045'),
    (N'CARP-KIT', N'00043'),
    (N'CARP-TRS', N'00040'),
    (N'CARP-WRD', N'00046'),
    (N'DEC-FIR', N'00103'),
    (N'DEC-STD', N'00082'),
    (N'DEC-WLP', N'00083'),
    (N'ELE-ALM', N'00087'),
    (N'ELE-AV', N'00095'),
    (N'ELE-CCT', N'00085'),
    (N'ELE-ENT', N'00086'),
    (N'ELE-EVC', N'00094'),
    (N'ELE-FIR', N'00096'),
    (N'ELE-SPE', N'00073'),
    (N'ELE-STD', N'00072'),
    (N'ENABLE-ASB', N'00114'),
    (N'ENABLE-COR', N'00112'),
    (N'ENABLE-DEM', N'00018'),
    (N'ENABLE-GRB', N'00131'),
    (N'ENABLE-SKP', N'00131'),
    (N'ENABLE-STS', N'00019'),
    (N'EXT-MCP', N'00110'),
    (N'EXT-STC', N'00107'),
    (N'EXT-STC-COP', N'00105'),
    (N'EXT-TIC', N'00106'),
    (N'EXTW-BBQ', N'00118'),
    (N'EXTW-DEK', N'00108'),
    (N'EXTW-FEN', N'00115'),
    (N'EXTW-LND', N'00117'),
    (N'EXTW-PAV', N'00113'),
    (N'EXTW-SHD', N'00119'),
    (N'EXTW-TRF', N'00116'),
    (N'FIRE-PSV', N'00097'),
    (N'FIRE-STP', N'00098'),
    (N'FLR-CPT', N'00069'),
    (N'FLR-LVT', N'00070'),
    (N'FLR-SCR', N'00067'),
    (N'FLR-SLF', N'00071'),
    (N'FLR-WD', N'00068'),
    (N'HAND-CLE', N'00129'),
    (N'HAND-CLI', N'00130'),
    (N'HAND-MKT', N'00133'),
    (N'HAND-MSC', N'00134'),
    (N'HAND-PHO', N'00132'),
    (N'HAND-SPE', N'00135'),
    (N'INT-COV', N'00066'),
    (N'INT-INC', N'00058'),
    (N'INT-INF', N'00059'),
    (N'INT-INW', N'00057'),
    (N'INT-MFC', N'00055'),
    (N'INT-MGW', N'00056'),
    (N'INT-PDD', N'00061'),
    (N'INT-PLB', N'00060'),
    (N'INT-PLS', N'00062'),
    (N'INT-RDR', N'00063'),
    (N'INT-SPP', N'00065'),
    (N'INT-SPR', N'00064'),
    (N'MASON-BRK', N'00028'),
    (N'MASON-STN', N'00029'),
    (N'MEC-AC', N'00089'),
    (N'MEC-BLR', N'00090'),
    (N'MEC-DRN', N'00024'),
    (N'MEC-FUL', N'00091'),
    (N'MEC-HTS', N'00088'),
    (N'MEC-PLM', N'00074'),
    (N'MEC-PLS', N'00075'),
    (N'MEC-SOL', N'00092'),
    (N'MEC-UFH', N'00093'),
    (N'MEC-VNT', N'00111'),
    (N'NOM-CON', N'00136'),
    (N'NOM-SUP', N'00137'),
    (N'PRELIMS-HRD', N'00001'),
    (N'PRELIMS-HSC', N'00006'),
    (N'PRELIMS-HSO', N'00005'),
    (N'PRELIMS-LAB', N'00015'),
    (N'PRELIMS-PMG', N'00017'),
    (N'PRELIMS-PRO', N'00008'),
    (N'PRELIMS-SEC', N'00003'),
    (N'PRELIMS-SET', N'00002'),
    (N'PRELIMS-SMG', N'00016'),
    (N'PRELIMS-TMP', N'00109'),
    (N'PRELIMS-WC', N'00007'),
    (N'PRELIMS-WEL', N'00004'),
    (N'PRELIMS-WPR', N'00009'),
    (N'ROOF-FLT', N'00033'),
    (N'ROOF-FSM', N'00037'),
    (N'ROOF-FSU', N'00035'),
    (N'ROOF-GRM', N'00038'),
    (N'ROOF-GRU', N'00036'),
    (N'ROOF-LED', N'00034'),
    (N'ROOF-RFR', N'00030'),
    (N'ROOF-TLN', N'00031'),
    (N'ROOF-TLO', N'00032'),
    (N'SCAFF-STD', N'00020'),
    (N'SPEC-GAZ', N'00102'),
    (N'SPEC-LFT', N'00099'),
    (N'SPEC-POO', N'00100'),
    (N'SPEC-SPA', N'00101'),
    (N'STAIR-GLS', N'00049'),
    (N'STAIR-MTL', N'00047'),
    (N'STAIR-TIM', N'00048'),
    (N'STR-DSL', N'00011'),
    (N'STR-GRL', N'00014'),
    (N'STR-MRL', N'00013'),
    (N'STR-MSH', N'00012'),
    (N'STR-STL', N'00010'),
    (N'SUB-CON', N'00027'),
    (N'SUB-DRN', N'00023'),
    (N'SUB-EXC', N'00021'),
    (N'SUB-GWK', N'00022'),
    (N'SUB-PIL', N'00025'),
    (N'SUB-UND', N'00026'),
    (N'SUP-APP', N'00126'),
    (N'SUP-DOR', N'00123'),
    (N'SUP-FRN', N'00127'),
    (N'SUP-IRO', N'00128'),
    (N'SUP-KIT', N'00125'),
    (N'SUP-SAN', N'00122'),
    (N'SUP-TIL', N'00124'),
    (N'TIL-MRB', N'00078'),
    (N'TIL-STD', N'00076'),
    (N'TIL-STN', N'00077'),
    (N'UTIL-STD', N'00120'),
    (N'UTIL-TRN', N'00121'),
    (N'WDR-ALU', N'00052'),
    (N'WDR-GAR', N'00104'),
    (N'WDR-INT', N'00053'),
    (N'WDR-SPG', N'00054'),
    (N'WDR-TIM', N'00050'),
    (N'WDR-UPV', N'00051'),
    (N'WIN-BLD', N'00084'),
    (N'WPF-DMP', N'00081'),
    (N'WPF-EXT', N'00080'),
    (N'WPF-INT', N'00079');

UPDATE v SET v.[CostCode] = m.[NewCode]
FROM [dbo].[ValuationLineItems] v JOIN @map m ON v.[CostCode] = m.[OldCode];

UPDATE x SET x.[CostCenterCode] = m.[NewCode]
FROM [dbo].[XeroLedgerLines] x JOIN @map m ON x.[CostCenterCode] = m.[OldCode];

-- 4. Verify ---------------------------------------------------------------------
DECLARE @valTotalAfter decimal(38,4) = (SELECT SUM([LineAmount]) FROM [dbo].[ValuationLineItems]);
IF @valTotalBefore <> @valTotalAfter
BEGIN
    ROLLBACK TRANSACTION;
    THROW 50001, 'Valuation totals changed during cost-code restore -- rolled back.', 1;
END;

COMMIT TRANSACTION;

-- Post-restore report ------------------------------------------------------------
SELECT 'Valuation lines NOT on numeric codes (should be 0)' AS [Check], COUNT(*) AS [Count]
FROM [dbo].[ValuationLineItems] WHERE [CostCode] <> N'' AND [CostCode] NOT LIKE N'00[0-9][0-9][0-9]'
UNION ALL
SELECT 'Xero allocations NOT on numeric codes (should be 0)', COUNT(*)
FROM [dbo].[XeroLedgerLines] WHERE [CostCenterCode] IS NOT NULL AND [CostCenterCode] NOT LIKE N'00[0-9][0-9][0-9]'
UNION ALL
SELECT 'Active cost centres (must be 137)', COUNT(*)
FROM [dbo].[CostCenters] WHERE [IsActive] = 1;

-- Any lines the report above flags:
SELECT [ValuationLineItemId] AS [Id], [CostCode] AS [Code], N'valuation' AS [Source]
FROM [dbo].[ValuationLineItems] WHERE [CostCode] <> N'' AND [CostCode] NOT LIKE N'00[0-9][0-9][0-9]'
UNION ALL
SELECT [XeroLedgerLineId], [CostCenterCode], N'xero ledger'
FROM [dbo].[XeroLedgerLines] WHERE [CostCenterCode] IS NOT NULL AND [CostCenterCode] NOT LIKE N'00[0-9][0-9][0-9]';
