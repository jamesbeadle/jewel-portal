-- ============================================================================
-- Seed the GLOBAL cost-center master  (v2.1 -- JBB Cost Code Master)
-- ----------------------------------------------------------------------------
-- One shared cost-center list used by every project's Financials tab, the
-- Valuation Report CODE column and the Xero cost-allocation queue. v2.1
-- replaces the interim numeric master (00001..00137) with the canonical JBB
-- Cost Code Master (source workbook: JBB_CostCode_Master, v2.1, owner Nigel
-- Reilly): trade-prefixed codes (PRELIMS-HRD, ENABLE-DEM, ...), 138 codes
-- across 25 parent trades. Every code = one subcontractor / bid package.
-- If a code is not in the master it is NOT a cost code -- no reactivating.
--
-- This script OWNS the [CostCenters] table for BOOTSTRAP purposes: it creates
-- the table if missing and seeds it. Run against an EMPTY database only.
--
-- ⚠ For an EXISTING database use migrate-cost-centers-v2.sql instead: it
-- seeds these same codes, retires everything else, AND remaps valuation lines
-- and Xero ledger allocations off the old numeric codes.
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
    (N'cc-prelims-hrd', N'PRELIMS-HRD', N'Hoarding', 10),
    (N'cc-prelims-set', N'PRELIMS-SET', N'Site set up', 20),
    (N'cc-prelims-sec', N'PRELIMS-SEC', N'Site temp security', 30),
    (N'cc-prelims-wel', N'PRELIMS-WEL', N'Welfare', 40),
    (N'cc-prelims-hso', N'PRELIMS-HSO', N'Health and Safety ongoing', 50),
    (N'cc-prelims-hsc', N'PRELIMS-HSC', N'Health & Safety CDM', 60),
    (N'cc-prelims-wc', N'PRELIMS-WC', N'Temp toilet', 70),
    (N'cc-prelims-pro', N'PRELIMS-PRO', N'General protection', 80),
    (N'cc-prelims-wpr', N'PRELIMS-WPR', N'Weather protection', 90),
    (N'cc-prelims-lab', N'PRELIMS-LAB', N'Labour', 100),
    (N'cc-prelims-smg', N'PRELIMS-SMG', N'Site manager', 110),
    (N'cc-prelims-pmg', N'PRELIMS-PMG', N'Project manager', 120),
    (N'cc-prelims-tmp', N'PRELIMS-TMP', N'Temporary works', 130),
    (N'cc-enable-dem', N'ENABLE-DEM', N'Demolition', 140),
    (N'cc-enable-sts', N'ENABLE-STS', N'Structural support', 150),
    (N'cc-enable-asb', N'ENABLE-ASB', N'Asbestos removal', 160),
    (N'cc-enable-cor', N'ENABLE-COR', N'Core drilling', 170),
    (N'cc-enable-skp', N'ENABLE-SKP', N'Skips', 180),
    (N'cc-enable-grb', N'ENABLE-GRB', N'Grab loads', 190),
    (N'cc-scaff-std', N'SCAFF-STD', N'Scaffolding', 200),
    (N'cc-sub-exc', N'SUB-EXC', N'Excavation', 210),
    (N'cc-sub-gwk', N'SUB-GWK', N'Groundworks', 220),
    (N'cc-sub-drn', N'SUB-DRN', N'Drainage Below ground', 230),
    (N'cc-sub-pil', N'SUB-PIL', N'Piling', 240),
    (N'cc-sub-und', N'SUB-UND', N'Under pining', 250),
    (N'cc-sub-con', N'SUB-CON', N'Concrete', 260),
    (N'cc-str-stl', N'STR-STL', N'Structural steel', 270),
    (N'cc-str-dsl', N'STR-DSL', N'Designer steel', 280),
    (N'cc-str-msh', N'STR-MSH', N'Mesh Steel works', 290),
    (N'cc-mason-brk', N'MASON-BRK', N'Masonry brickworks', 300),
    (N'cc-mason-stn', N'MASON-STN', N'Masonry stonework', 310),
    (N'cc-ext-stc', N'EXT-STC', N'External stone cladding', 320),
    (N'cc-ext-tic', N'EXT-TIC', N'External timber cladding', 330),
    (N'cc-ext-stc-cop', N'EXT-STC-COP', N'Stone copings and cappings', 340),
    (N'cc-ext-mcp', N'EXT-MCP', N'Metal capping and coping', 350),
    (N'cc-roof-rfr', N'ROOF-RFR', N'Roofer', 360),
    (N'cc-roof-tln', N'ROOF-TLN', N'Roofing tile new', 370),
    (N'cc-roof-tlo', N'ROOF-TLO', N'Roofing tile old', 380),
    (N'cc-roof-flt', N'ROOF-FLT', N'Roofing flat membrane', 390),
    (N'cc-roof-led', N'ROOF-LED', N'Roofing lead works', 400),
    (N'cc-roof-fsu', N'ROOF-FSU', N'Facia and soffit UPVC', 410),
    (N'cc-roof-fsm', N'ROOF-FSM', N'Facia and soffit Metal', 420),
    (N'cc-roof-gru', N'ROOF-GRU', N'Gutters and rainwater UPVC', 430),
    (N'cc-roof-grm', N'ROOF-GRM', N'Gutters and rainwater Metal', 440),
    (N'cc-carp-cut', N'CARP-CUT', N'Carpentry cut Roofing', 450),
    (N'cc-carp-trs', N'CARP-TRS', N'Carpentry truss roofing', 460),
    (N'cc-carp-1fx', N'CARP-1FX', N'Carpentry 1st fix', 470),
    (N'cc-carp-2fx', N'CARP-2FX', N'Carpentry 2nd fix', 480),
    (N'cc-carp-kit', N'CARP-KIT', N'Carpentry kitchens', 490),
    (N'cc-carp-dor', N'CARP-DOR', N'Internal Doors & linings', 500),
    (N'cc-carp-jnr', N'CARP-JNR', N'Joinery', 510),
    (N'cc-carp-wrd', N'CARP-WRD', N'Wardrobes', 520),
    (N'cc-stair-mtl', N'STAIR-MTL', N'Stairs metal', 530),
    (N'cc-stair-tim', N'STAIR-TIM', N'Stairs timber', 540),
    (N'cc-stair-gls', N'STAIR-GLS', N'Stairs glass', 550),
    (N'cc-wdr-tim', N'WDR-TIM', N'Timber windows and doors', 560),
    (N'cc-wdr-upv', N'WDR-UPV', N'UPVC window and doors', 570),
    (N'cc-wdr-alu', N'WDR-ALU', N'Aluminium window and doors', 580),
    (N'cc-wdr-gar', N'WDR-GAR', N'Garage doors', 590),
    (N'cc-int-mfc', N'INT-MFC', N'MF ceilings', 600),
    (N'cc-int-mgw', N'INT-MGW', N'Metal and Gypliner walls', 610),
    (N'cc-int-inw', N'INT-INW', N'Insulation walls', 620),
    (N'cc-int-inc', N'INT-INC', N'Insulation ceilings', 630),
    (N'cc-int-inf', N'INT-INF', N'Insulation floors', 640),
    (N'cc-int-plb', N'INT-PLB', N'Plaster Boarding', 650),
    (N'cc-int-pdd', N'INT-PDD', N'Plaster boarding Dot & dab', 660),
    (N'cc-int-pls', N'INT-PLS', N'Plastering', 670),
    (N'cc-int-rdr', N'INT-RDR', N'Rendering', 680),
    (N'cc-int-spr', N'INT-SPR', N'Spray plaster and render', 690),
    (N'cc-int-spp', N'INT-SPP', N'Specialist plastering', 700),
    (N'cc-int-cov', N'INT-COV', N'Coving', 710),
    (N'cc-flr-scr', N'FLR-SCR', N'Screed', 720),
    (N'cc-flr-slf', N'FLR-SLF', N'Floor finish self levelling', 730),
    (N'cc-flr-wd', N'FLR-WD', N'Floor finish timber', 740),
    (N'cc-flr-cpt', N'FLR-CPT', N'Floor finish carpet', 750),
    (N'cc-flr-lvt', N'FLR-LVT', N'Floor finish LVT/Lino', 760),
    (N'cc-til-std', N'TIL-STD', N'Tiling', 770),
    (N'cc-til-stn', N'TIL-STN', N'Tiling Stone', 780),
    (N'cc-til-mrb', N'TIL-MRB', N'Tiling marble', 790),
    (N'cc-wpf-int', N'WPF-INT', N'Tanking internal', 800),
    (N'cc-wpf-ext', N'WPF-EXT', N'Tanking external', 810),
    (N'cc-mec-drn', N'MEC-DRN', N'Drainage internal', 820),
    (N'cc-mec-plm', N'MEC-PLM', N'Plumber', 830),
    (N'cc-mec-hts', N'MEC-HTS', N'Heat Source', 840),
    (N'cc-mec-ac', N'MEC-AC', N'Air con', 850),
    (N'cc-mec-blr', N'MEC-BLR', N'Boilers', 860),
    (N'cc-mec-ful', N'MEC-FUL', N'Gas, electric or oil boilers', 870),
    (N'cc-mec-sol', N'MEC-SOL', N'Solar panels', 880),
    (N'cc-mec-ufh', N'MEC-UFH', N'Under-floor heating', 890),
    (N'cc-mec-vnt', N'MEC-VNT', N'Ventilation and extract', 900),
    (N'cc-ele-std', N'ELE-STD', N'Electrician', 910),
    (N'cc-ele-evc', N'ELE-EVC', N'EV car chargers', 920),
    (N'cc-ele-av', N'ELE-AV', N'Audio visual and sound systems', 930),
    (N'cc-ele-cct', N'ELE-CCT', N'CCTV', 940),
    (N'cc-ele-ent', N'ELE-ENT', N'Entry systems', 950),
    (N'cc-ele-alm', N'ELE-ALM', N'Alarms and security', 960),
    (N'cc-ele-fir', N'ELE-FIR', N'Fire and smoke alarms', 970),
    (N'cc-fire-psv', N'FIRE-PSV', N'Passive fire protection', 980),
    (N'cc-fire-stp', N'FIRE-STP', N'Fire stopping', 990),
    (N'cc-dec-std', N'DEC-STD', N'Decorating', 1000),
    (N'cc-dec-wlp', N'DEC-WLP', N'Wallpapering', 1010),
    (N'cc-str-mrl', N'STR-MRL', N'Metal Railings and balustrade', 1020),
    (N'cc-str-grl', N'STR-GRL', N'Glass railing and balustrade', 1030),
    (N'cc-wdr-int', N'WDR-INT', N'Internal specialist window and doors', 1040),
    (N'cc-wdr-spg', N'WDR-SPG', N'Specialist glazing and screens', 1050),
    (N'cc-wpf-dmp', N'WPF-DMP', N'Damp proofing specialist', 1060),
    (N'cc-mec-pls', N'MEC-PLS', N'Specialist plumbing', 1070),
    (N'cc-ele-spe', N'ELE-SPE', N'Specialist electrician', 1080),
    (N'cc-dec-fir', N'DEC-FIR', N'Fires, chimney and stoves', 1090),
    (N'cc-spec-lft', N'SPEC-LFT', N'Lifts and hoists', 1100),
    (N'cc-spec-poo', N'SPEC-POO', N'Swimming pools', 1110),
    (N'cc-spec-spa', N'SPEC-SPA', N'Spa and hot tubs', 1120),
    (N'cc-spec-gaz', N'SPEC-GAZ', N'Gazebo, carports and awning', 1130),
    (N'cc-extw-dek', N'EXTW-DEK', N'Decking', 1140),
    (N'cc-extw-pav', N'EXTW-PAV', N'Paving and roads', 1150),
    (N'cc-extw-fen', N'EXTW-FEN', N'Fencing', 1160),
    (N'cc-extw-trf', N'EXTW-TRF', N'Turfing', 1170),
    (N'cc-extw-lnd', N'EXTW-LND', N'Landscaping', 1180),
    (N'cc-extw-bbq', N'EXTW-BBQ', N'BBQ', 1190),
    (N'cc-extw-shd', N'EXTW-SHD', N'Sheds and out houses', 1200),
    (N'cc-util-std', N'UTIL-STD', N'Utilities', 1210),
    (N'cc-util-trn', N'UTIL-TRN', N'Trenching, bore holes and services', 1220),
    (N'cc-win-bld', N'WIN-BLD', N'Blinds and curtains', 1230),
    (N'cc-sup-san', N'SUP-SAN', N'Sanitary supply', 1240),
    (N'cc-sup-dor', N'SUP-DOR', N'Door supply', 1250),
    (N'cc-sup-til', N'SUP-TIL', N'Tiling supply', 1260),
    (N'cc-sup-kit', N'SUP-KIT', N'Kitchen supply', 1270),
    (N'cc-sup-app', N'SUP-APP', N'Appliance and machinery supply', 1280),
    (N'cc-sup-frn', N'SUP-FRN', N'Furniture supply', 1290),
    (N'cc-sup-iro', N'SUP-IRO', N'Ironmongery supply', 1300),
    (N'cc-hand-cle', N'HAND-CLE', N'Cleaning external', 1310),
    (N'cc-hand-cli', N'HAND-CLI', N'Cleaning internal', 1320),
    (N'cc-hand-pho', N'HAND-PHO', N'Photography', 1330),
    (N'cc-hand-mkt', N'HAND-MKT', N'Marketing', 1340),
    (N'cc-hand-msc', N'HAND-MSC', N'Misc', 1350),
    (N'cc-hand-spe', N'HAND-SPE', N'Specialist', 1360),
    (N'cc-nom-con', N'NOM-CON', N'Nominated contractor', 1370),
    (N'cc-nom-sup', N'NOM-SUP', N'Nominated supplier', 1380)
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
