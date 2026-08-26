/* ============================================================================
   Backfill: 17a Abbot Road — line-level client references (Valuation report)
   ----------------------------------------------------------------------------
   Stamps each live valuation line's ClientReference with the client's own
   schedule-of-works item number ("1.01" … "30.06"), derived by aligning the
   JPMS valuation report export (Valuation 14, 25/08/2026) line-by-line against
   the client's contract document "Valuation No.14 - 17a Abbot Road"
   (Phase 2 Re-Tender, 18/12/25). 236 lines mapped: 230 contract-works lines
   plus the 6 provisional sums (client section 30). Variation lines are not
   touched — they already carry their V-refs, which is how the client's own
   V-sheets are numbered.

   Lines deliberately left blank (not in the client's document):
     - "Excavation Works relating to P2 & P3"            (£1,795)
     - "Screed Plank 20mm (board) - JBB INSTALL ONLY"    (£2,070)
     - "Iso Edge (6mm x 75mm x 50m) - JBB INSTALL ONLY"  (£250)
     - "Screed Plank Adhesive (bottle) - JBB INSTALL ONLY" (£250)
     - "Screed Plank Screws (19mm) (500) - JBB INSTALL ONLY" (£250)

   Matching: trimmed Description + occurrence number (ROW_NUMBER over
   ElementType, DisplayOrder within the project's non-variation lines) — the
   same order the report prints, so duplicate descriptions ("Rubbish removal",
   "Site manager" 12wk/22wk) land on the right rows. Values (qty/rate) ride
   along as a drift check only, reported at the end, never enforced.

   REQUIRES: migration 20260826100000_AddValuationLineItemClientReference
   (adds ValuationLineItems.ClientReference) applied first.

   Safe to re-run: pure UPDATE of ClientReference to the mapped value; rows
   already carrying the mapped value are skipped. Never touches schema.
   ============================================================================ */

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @ProjectId NVARCHAR(64) = N'4ec1ad1ca3a440c69f32f46f73aea005';

/* --- Verify the project and the column exist ------------------------------- */
IF NOT EXISTS (SELECT 1 FROM dbo.Projects WHERE ProjectId = @ProjectId)
BEGIN
    RAISERROR(N'Project 4ec1ad1ca3a440c69f32f46f73aea005 (17a Abbot Road) not found in dbo.Projects. Backfill aborted.', 16, 1);
    RETURN;
END

IF COL_LENGTH('dbo.ValuationLineItems', 'ClientReference') IS NULL
BEGIN
    RAISERROR(N'dbo.ValuationLineItems.ClientReference does not exist — apply migration 20260826100000_AddValuationLineItemClientReference first. Backfill aborted.', 16, 1);
    RETURN;
END

/* --- The map: client ref per (description, occurrence) --------------------- */
CREATE TABLE #Map (
    Description      NVARCHAR(512) NOT NULL,
    Occurrence       INT           NOT NULL,
    ClientReference  NVARCHAR(64)  NOT NULL,
    ExpectedQuantity DECIMAL(18,4) NULL,   -- drift check only
    ExpectedRate     DECIMAL(18,4) NULL,   -- drift check only
    UNIQUE NONCLUSTERED (Description, Occurrence)  -- nonclustered: nvarchar(512) keys overflow a clustered index's 900-byte cap
);

INSERT INTO #Map (Description, Occurrence, ClientReference, ExpectedQuantity, ExpectedRate) VALUES
    (N'Site manager', 1, N'1.01', 12, 750),
    (N'Site manager', 2, N'1.02', 22, 750),
    (N'Project manager', 1, N'1.03', 12, 350),
    (N'Project manager', 2, N'1.04', 22, 350),
    (N'Site labour', 1, N'1.05', 12, 300),
    (N'Site labour', 2, N'1.06', 22, 300),
    (N'Hoarding & protection', 1, N'1.07', 1, 125),
    (N'Rubbish removal', 1, N'1.08', 6, 380),
    (N'Rubbish removal', 2, N'1.09', 6, 380),
    (N'Temporary toilet & welfare', 1, N'1.10', 12, 90),
    (N'Temporary toilet & welfare', 2, N'1.11', 22, 90),
    (N'Plant, lighting & machinery', 1, N'1.12', 12, 55),
    (N'Plant, lighting & machinery', 2, N'1.13', 22, 55),
    (N'Temporary plumbing & electrics', 1, N'1.14', 1, 800),
    (N'CDM', 1, N'1.15', 1, 1200),
    (N'Health, safety & welfare', 1, N'1.16', 12, 125),
    (N'Health, safety & welfare', 2, N'1.17', 22, 125),
    (N'Scaffolding', 1, N'1.18', 1, 3500),
    (N'Clean on completion', 1, N'1.19', 1, 800),
    (N'Conveyor Belt System to remove hardcore from garden', 1, N'1.20', 1, 2000),
    (N'Asbestos Survey & Report', 1, N'1.21', 1, 315),
    (N'Standing Charge - 23 June', 1, N'1.22', 1, 500),
    (N'Standing Charge - 30th June', 1, N'1.23', 1, 500),
    (N'Standing Charge - 7th July', 1, N'1.24', 1, 500),
    (N'Standing Charge - 14th July', 1, N'1.25', 1, 500),
    (N'Standing Charge - 21st July', 1, N'1.26', 1, 500),
    (N'Standing Charge - 28th July', 1, N'1.27', 1, 500),
    (N'Standing Charge - 4th August', 1, N'1.28', 1, 500),
    (N'Standing Charge - 11th August', 1, N'1.29', 1, 500),
    (N'Standing Charge - 18th August', 1, N'1.30', 1, 500),
    (N'Standing Charge - 25th August', 1, N'1.31', 1, 500),
    (N'Standing Charge - 1st Sept', 1, N'1.32', 1, 500),
    (N'Standing Charge - 8th Sept', 1, N'1.33', 1, 500),
    (N'Standing Charge - 15th Sept', 1, N'1.34', 1, 500),
    (N'Standing Charge - 22nd Sept', 1, N'1.35', 1, 500),
    (N'Standing Charge - 29th Sept', 1, N'1.36', 1, 500),
    (N'Erect temporary propping to existing construction', 1, N'2.01', 5, 125),
    (N'Cap off & drain / divert existing services', 1, N'2.02', 1, 250),
    (N'Remove redundant fixtures & fittings', 1, N'2.03', 1, 200),
    (N'Remove existing metal balcony & staircase', 1, N'2.04', 1, 1800),
    (N'Cut down existing balcony support steels', 1, N'2.05', 1, 350),
    (N'Remove external door', 1, N'2.06', 1, 55),
    (N'Demolish existing retaining wall', 1, N'2.07', 1, 600),
    (N'Grub out existing manhole', 1, N'2.08', 1, 375),
    (N'Remove existing paving to new areas', 1, N'2.09', 12, 12),
    (N'Excavate to reduce levels & remove spoil', 1, N'2.10', 2, 110),
    (N'Demolition of existing Garden Wall & Flower Bed', 1, N'2.11', 4, 350),
    (N'Rubbish removal', 3, N'2.12', 1, 380),
    (N'Removal of surface water drainage only', 1, N'2.13', 1, 350),
    (N'Erect strongboy propping to existing construction. (New opening into Sitting Area from Balcony).', 1, N'2.14', 5, 75),
    (N'Erect needle propping to existing construction. (New opening into Sitting Area from Balcony).', 1, N'2.15', 5, 90),
    (N'Removal of sitting area window opening, plaster, cut back brickwork, remove existing lintel & all associated demo work to this area.', 1, N'2.16', 3, 350),
    (N'Install protection to all existing area with in kitchen & sitting area. Protect kitchen worktops & close up / make air tight to doorways leading into house while demo is being carried out.', 1, N'2.17', 1, 600),
    (N'Remove section of lower roof', 1, N'3.01', 2, 12),
    (N'Install 12 mm WBP plywood deck with timbers', 1, N'3.02', 2, 42),
    (N'Code 4 lead flashings', 1, N'3.03', 2, 68),
    (N'Remove section of main roof', 1, N'4.01', 9, 12),
    (N'Install 12 mm WBP plywood deck with timbers', 2, N'4.02', 9, 42),
    (N'Code 4 lead flashings', 2, N'4.03', 9, 68),
    (N'Excavate & concrete in pad foundations - PAD1', 1, N'5.01', 2, 398),
    (N'Excavate & concrete in pad foundations - PAD2', 1, N'5.02', 1, 320),
    (N'Excavate & concrete in strip foundation', 1, N'5.03', 2, 290),
    (N'Excavate & concrete in pad foundations - PAD1', 2, N'5.04', 1, -398),
    (N'Excavate & concrete in pad foundations - PAD2', 2, N'5.05', 1, -160),
    (N'Excavate & concrete in strip foundation', 2, N'5.06', 1, -290),
    (N'7.5 - Unforeseen structural', 1, N'5.07', 1, -1000),
    (N'Capital Piling - SFA Open Bore Piles - REV3', 1, N'5.08', 1, 9355),
    (N'Capital Piling - RC Ground Beam - REV3', 1, N'5.09', 1, 15005),
    (N'Core Drilling 500mm holes into 200mm r/c', 1, N'5.11', 2, 885),
    (N'Trial Pit Excavation as requested', 1, N'5.12', 3, 400),
    (N'Excavate ground round proposed concrete encased steel beam & Proposed 1m2 concrete pad. Section Detail 12.', 1, N'6.01', 1, 2000),
    (N'Proposed concrete & steel to 1m2 steel reinforced concrete pad foundation to S.E detail & specification.', 1, N'6.02', 1, 500),
    (N'Supply & Install of Concrete to Steel Ground Beam.', 1, N'6.03', 1, 1750),
    (N'Dense concrete blockwork subwall supporting beam & block floor to S.E detail & Spec.', 1, N'6.04', 1, 1000),
    (N'Proposed Beam & Block concrete floor to S.E detail & specification.', 1, N'6.05', 6, 108),
    (N'65 x 215mm dense concrete blockwork laid flat', 1, N'6.06', 6, 72),
    (N'95mm wide engineering brick on edge', 1, N'6.07', 6, 72),
    (N'65mm x 100mm dense concrete external blockwork', 1, N'6.08', 6, 72),
    (N'Facing brickwork to match exisitng below timber frame walls', 1, N'6.09', 2.22, 72),
    (N'100 x 140mm Marmox Thermoblock to run full length across top of 215mm blockwork full length', 1, N'6.10', 6, 72),
    (N'210 x 222 x 215mm Manthorpe telescopic underfloor vent.', 1, N'6.11', 2, 20),
    (N'Stone paving cap facing brick to match external landing floor finish', 1, N'6.12', 6, 100),
    (N'50 mm hardcore blinded with sand', 1, N'6.13', 0, 34),
    (N'200 mm bed of concrete', 1, N'6.14', 0, 150),
    (N'Damp proof membrane', 1, N'6.15', 6, 18),
    (N'150 mm XPS insulation', 1, N'6.16', 6, 62),
    (N'Damp proof membrane / Separating Layer', 1, N'6.17', 6, 18),
    (N'75 mm sand / cement screed', 1, N'6.18', 6, 80),
    (N'Proposed 25mm perimeter insulation', 1, N'6.19', 10, 40),
    (N'20mm Cellecta Screedboard', 1, N'6.20', 1, 750),
    (N'Proposed Sphere 8 resin floor (Lower Ground Stairwell only) (TBC)', 1, N'6.21', 6, 0),
    (N'Carefully remove existing floor boards not to damage existing floor joists. Exisitng floor to remian.', 1, N'7.01', 3, 55),
    (N'Replace with - Proposed 70mm Kingspan K103 insulation fixed between existing joists.', 1, N'7.02', 3, 62),
    (N'0.3 Polythene Vapour Control Layer', 1, N'7.03', 1, 100),
    (N'18mm plywood flooring', 1, N'7.04', 3, 36),
    (N'Proposed Sphere 8 resin floor (Lower Ground Stairwell only) (TBC)', 2, N'7.05', 3, 0),
    (N'152 x 152 x 23 kg steel beams', 1, N'8.01', 460, 8),
    (N'152 x 152 x 37 kg steel beams', 1, N'8.02', 1015, 8),
    (N'152 x 152 x 30 kg steel beams', 1, N'8.03', 420, 8),
    (N'139.7 x 8 CHS steel column', 1, N'8.04', 80, 8),
    (N'100 x 100 x 8 SHS steel column', 1, N'8.05', 120, 8),
    (N'Intumescent paint to steels', 1, N'8.06', 1, 600),
    (N'Ground Beam Steel', 1, N'8.07', 1, 1250),
    (N'Balustrade Balcony Steel', 1, N'8.08', 1, 1500),
    (N'Louvre Brackets (Provisional Sum)', 1, N'8.09', 1, 1500),
    (N'50 x 150 mm timber floor joists', 1, N'9.01', 82, 32),
    (N'Structural timbers / trimmers', 1, N'9.02', 1, 755),
    (N'140mm mineral insulation laid with in Joist Lay under Internal section of joist lay only', 1, N'9.03', 20, 35),
    (N'Black mesh to underside of joists', 1, N'9.04', 21, 45),
    (N'Fix 65mm Kingspan Kooltherm K110 insulation to the underside of the joist lay', 1, N'9.05', 25, 55),
    (N'18 mm plywood deck', 1, N'10.01', 12, 36),
    (N'50mm x 50mm treated timber battens laid ontop of the 18mm plywood to the internal section of the balcony', 1, N'10.02', 12, 42),
    (N'18mm WBP Plywood to top of 50mm x 50mm batten', 1, N'10.03', 12, 42),
    (N'Timber firings to floor joists', 1, N'11.01', 11, 24),
    (N'18mm WBP Plywood deck to SE detail & Specification', 1, N'11.02', 11, 42),
    (N'Sarnifil single ply roof membrane', 1, N'11.03', 11, 175),
    (N'130mm high 50mm thick Kingspan Kooltherm K15 insulation taken up to underside of decking at door threshold to prevent thermal bridge', 1, N'11.04', 11, 30),
    (N'22mm CLADCO composite decking with timber effect, laid on 50mm(W) x 100mm(H) CLADCO recycled plastic joists cut in profile & length to account for falls underneath, @ 400mm centres. Allow for 10mm drainage gap to decking perimeter. (Allowance made under Ref 29.02)', 1, N'11.05', 0, 0),
    (N'1100 mm clear glass balustrade', 1, N'12.01', 8, 625),
    (N'1100 mm opaque glass balustrade', 1, N'12.02', 5, 658),
    (N'1100 mm clear glass balustrade', 2, N'12.03', 8, -625),
    (N'1100 mm opaque glass balustrade', 2, N'12.04', 5, -658),
    (N'1100 mm toughened glass balustrade to steps', 1, N'12.05', 1, -750),
    (N'IG Glass - Q55861 - CSA005 - 12.09.25', 1, N'12.06', 1, 47090),
    (N'50 x 140 mm timber framed external wall', 1, N'13.02', 26, 84),
    (N'140 mm Kingspan insulation between studs', 1, N'13.03', 18, 52),
    (N'12 mm plywood', 1, N'13.04', 26, 18),
    (N'Breatherable membrane', 1, N'13.05', 26, 16),
    (N'Timber battens (cross battened)', 1, N'13.06', 26, 42),
    (N'Western Red Cedar board cladding', 1, N'13.07', 36, 162),
    (N'Western Red Cedar board cladding', 2, N'13.08', 36, -162),
    (N'Millboard Anique Oak Boards', 1, N'13.09', 48, 215),
    (N'Millboard Fixtures & Fittings', 1, N'13.10', 1, 3350),
    (N'Aluminium trim', 1, N'13.11', 13, 80),
    (N'62.5mm Kingspan Kooltherm K118 insulated plasterboard w/ integral vapour control layer. Breakdown of 50mm rigid insulation & 12.5mm plasterboard', 1, N'13.13', 44, 48),
    (N'3mm Plaster Skim to Wall', 1, N'13.14', 44, 16),
    (N'White Paint Finish to Wall', 1, N'13.15', 44, 16),
    (N'50 x 150 mm timber roof joists', 1, N'14.01', 48, 32),
    (N'18 mm plywood over firings', 1, N'14.02', 11, 52),
    (N'Breatherable membrane', 2, N'14.03', 11, 16),
    (N'2 layers of 90 mm Kingspan', 1, N'14.04', 11, 78),
    (N'Sarnifil single ply roof membrane', 2, N'14.05', 11, 175),
    (N'12 mm BPG plasterboard', 1, N'14.06', 11, -40),
    (N'50 x 235 Oak Louvres @ 200mm spacing (Provisional)', 1, N'15.01', 1, 3000),
    (N'Fixtures & Fittings (Provisional)', 1, N'15.02', 1, 800),
    (N'Wealding of all support plates to take Oak Beams (Provisional)', 1, N'15.03', 1, 500),
    (N'12mm WBP Plywood board & Tyvek breathable membrane', 1, N'15.04', 11, 50),
    (N'Kingspan Nilvent Breathable membrane', 1, N'15.05', 1, 300),
    (N'50mm x 38mm treated timber vertical battens @ 400mm centres', 1, N'15.06', 11, 42),
    (N'50 x 140 mm timber framed external wall', 2, N'16.01', 12, 84),
    (N'140 mm Kingspan insulation between studs', 2, N'16.02', 12, 52),
    (N'25 mm Kingspan insulation', 1, N'16.03', 12, 26),
    (N'2 no 12 mm plywood', 1, N'16.04', 12, 36),
    (N'Breatherable membrane', 3, N'16.05', 12, 16),
    (N'Timber battens (cross battened)', 2, N'16.06', 12, 42),
    (N'Western Red Cedar board cladding', 3, N'16.07', 12, 162),
    (N'Western Red Cedar board cladding', 4, N'16.08', 12, -162),
    (N'Aluminium capping', 1, N'16.09', 21, 112),
    (N'50 x 140 mm timber framed external wall', 3, N'17.01', 6, 84),
    (N'12 mm plywood', 2, N'17.02', 6, 18),
    (N'Breatherable membrane', 4, N'17.03', 6, 16),
    (N'Timber battens (cross battened)', 3, N'17.04', 6, 42),
    (N'8 mm Swisspearl fibre cement panel', 1, N'17.05', 6, 140),
    (N'50 x 140 mm timber framed external wall', 4, N'18.01', 3, 84),
    (N'140 mm Kingspan insulation between studs', 3, N'18.02', 3, 52),
    (N'25 mm Kingspan insulation', 2, N'18.03', 3, 26),
    (N'2 no 12 mm plywood', 2, N'18.04', 3, 36),
    (N'Breatherable membrane', 5, N'18.05', 3, 16),
    (N'Timber battens (cross battened)', 4, N'18.06', 3, 42),
    (N'8 mm Swisspearl fibre cement panel', 2, N'18.07', 3, 140),
    (N'Aluminium capping', 2, N'18.08', 5, 112),
    (N'Install of internal timber wall to inclose Pocket door', 1, N'19.01', 5, 50),
    (N'Install of Pocket Door System 760mm x 2040mm Enigma sliding pocket door', 1, N'19.02', 1, 900),
    (N'Celotex insulation between walls', 1, N'19.03', 0, 48),
    (N'12.5 mm plasterboard to studwork', 1, N'19.04', 5, 15),
    (N'3 mm skim to walls', 1, N'19.05', 5, 16),
    (N'Install of Catnic Lintel to new opening with in sitting room / balcony area. Making good of all associated surrounding work inc brickwork.', 1, N'20.01', 1, 1000),
    (N'13mm wet plaster to existing brickwork (new internal balcony area) 5000mm (L) x 2100mm (H) = 10.5m2', 1, N'20.02', 11, 25),
    (N'Paint finish to existing brickwork wall once plastered 5000mm (L) x 2100mm (H) = 10.5m2', 1, N'20.03', 11, 16),
    (N'MF5 Ceiling to new internal balcony area & ground floor stair landing', 1, N'21.01', 15, 150),
    (N'100 Isosaver Spacesaver insulation laid above ceiling boards', 1, N'21.02', 15, 48),
    (N'12.5mm British Gypsum Soundbloc Plasterboard', 1, N'21.03', 15, 15),
    (N'3mm Plaster Skim to Ceiling', 1, N'21.04', 15, 16),
    (N'White Paint Finish to Ceiling', 1, N'21.05', 15, 16),
    (N'100mm block wall to Lower Ground Bedroom opening', 1, N'22.01', 5, 72),
    (N'13mm wet plaster to new single skin blockwork wall', 1, N'22.02', 5, 25),
    (N'Paint Finish to new single skin blockwork wall', 1, N'22.03', 5, 16),
    (N'Install single internal door to new blockwork wall. L/G Bedroom. Lining, Door, Ironmongary, door stops, paint / decorate?', 1, N'22.04', 1, 1000),
    (N'Upper Level: Allow for intrusive investigations into two separate 200mm square area to the current existing floor buildup. One to the marble floor and one to the timber floor. Allow for remedial works if required to reinstate screed where disturbed.', 1, N'23.01', 2, 300),
    (N'Upper Level: Allow for preparatory works to remove existing floor finish (marble and timber floor) down to existing sub-base - Screed.', 1, N'23.02', 33, 65),
    (N'Upper level: Allow for your plumber to connect their system it to the existing water supply. (JBB have not allowed for the supply & install of thermostat or manifold)', 1, N'23.04', 1, 1000),
    (N'JBB Oversee & Management Fee (Assumed 15% OH&P on JK Flooring Contract Sum)', 1, N'23.05', 1, 500),
    (N'Steel staircase', 1, N'24.01', 1, -7500),
    (N'Timber Wrapped Staircase w/ potensial handrail / ballustrade', 1, N'24.02', 1, 5000),
    (N'Underlay & Carpet (£40 per m2 supply) to New Wrapped Timber Staircase', 1, N'24.03', 1, 1000),
    (N'Make good existing finishes', 1, N'25.01', 1, 300),
    (N'Remove existing rainwater gutter', 1, N'25.02', 1, 80),
    (N'Sarnifill box scuppers', 1, N'25.03', 2, 320),
    (N'PPC aluminium hopper heads', 1, N'25.04', 4, 140),
    (N'PPC aluminium rainwater pipe', 1, N'25.05', 5, 46);

INSERT INTO #Map (Description, Occurrence, ClientReference, ExpectedQuantity, ExpectedRate) VALUES
    (N'PPC aluminium rainwater pipe', 2, N'25.06', 6, 46),
    (N'PPC aluminium rainwater pipe', 3, N'25.07', 6, 46),
    (N'PPC aluminium guttering', 1, N'25.08', 5, 44),
    (N'Rainwater outlets & rain chains from balcony', 1, N'25.09', 1, 520),
    (N'950 x 1050 mm Velfac window', 1, N'25.10', 1, 780),
    (N'810 x 2000 mm Velfac external door', 1, N'25.11', 1, 1720),
    (N'25 mm Cladco composite decking', 1, N'26.01', 21, -152),
    (N'External Finish Decking to Balcony - 25 mm Cladco composite decking', 1, N'26.02', 11, 152),
    (N'8 mm Swisspearl fibre cement panel', 3, N'26.03', 32, 140),
    (N'Prepare & decorate existing surfaces affected', 1, N'26.04', 1, 500),
    (N'Silicone mastic', 1, N'26.05', 1, 100),
    (N'Lower Level - 1No Lower Level radiator beneath the staircase - Victorian Plumbing – Urban Horizontal Radiator (Double panel, 600mm H x 608mm W, Anthracite)', 1, N'27.01', 1, 300),
    (N'All proposed plumbing connections to 1no radiators.', 1, N'27.02', 1, 550),
    (N'External LED downlights to ceilings', 1, N'28.01', 14, 125),
    (N'External double socket outlet', 1, N'28.02', 2, 135),
    (N'BWIC', 1, N'28.03', 1, 350),
    (N'Additional electrics unforeseen (Provisional)', 1, N'28.04', 1, 500),
    (N'Existing foul routes & connections to be assessed / repaired', 1, N'29.01', 1, 400),
    (N'New foul water manhole', 1, N'29.02', 1, 722),
    (N'Make connection into existing system', 1, N'29.03', 1, 250),
    (N'Connect new rainwater drains into storm system', 1, N'29.04', 1, 450),
    (N'1100 mm toughened glass balustrade to steps', 2, N'29.05', 1, 750),
    (N'Facing brick plinths with copping', 1, N'29.06', 8, 455),
    (N'Install of New Soakaway', 1, N'29.07', 0, 1400),
    (N'Install of Catchpit Chamber', 1, N'29.08', 0, 1400),
    (N'Proposed Planters', 1, N'29.09', 0, 1500),
    (N'Concrete Plinths to take rain chains', 1, N'29.10', 0, 500),
    (N'Rear Steps / Slab', 1, N'29.11', 0, 2000),
    (N'Sub base & paving slabs to external areas (no supply of slabs)', 1, N'29.12', 0, 90),
    (N'Replace Grass to West Elevation (Next to P1 & P4)', 1, N'29.13', 0, 140),
    (N'Supply of ironmongery', 1, N'30.01', 1, 500),
    (N'Unforeseen drainage', 1, N'30.02', 1, 1000),
    (N'Unforeseen landscaping', 1, N'30.03', 1, 0),
    (N'Unforeseen electrics', 1, N'30.04', 1, 500),
    (N'Unforeseen structural', 1, N'30.05', 1, 1000),
    (N'Overheads & Profits', 1, N'30.06', 1, 13662.2);

/* --- Stamp the lines ------------------------------------------------------- */
;WITH Ordered AS (
    SELECT  line.ValuationLineItemId,
            LTRIM(RTRIM(line.Description)) AS TrimmedDescription,
            line.Quantity,
            line.Rate,
            line.ClientReference,
            ROW_NUMBER() OVER (
                PARTITION BY LTRIM(RTRIM(line.Description))
                ORDER BY line.ElementType, line.DisplayOrder) AS Occurrence
    FROM    dbo.ValuationLineItems AS line
    WHERE   line.ProjectId = @ProjectId
      AND   line.ElementType IN (0, 1, 2)   -- ContractWorks, PcSum, Contingency; never variations
)
UPDATE  Ordered
SET     ClientReference = map.ClientReference
FROM    Ordered
JOIN    #Map AS map
  ON    map.Description = Ordered.TrimmedDescription
 AND    map.Occurrence  = Ordered.Occurrence
WHERE   Ordered.ClientReference <> map.ClientReference;

PRINT CONCAT('Lines stamped this run: ', @@ROWCOUNT);

/* --- Verification ---------------------------------------------------------- */
/* Map rows that found no line (empty = everything landed). */
;WITH Ordered AS (
    SELECT  LTRIM(RTRIM(line.Description)) AS TrimmedDescription,
            ROW_NUMBER() OVER (
                PARTITION BY LTRIM(RTRIM(line.Description))
                ORDER BY line.ElementType, line.DisplayOrder) AS Occurrence
    FROM    dbo.ValuationLineItems AS line
    WHERE   line.ProjectId = @ProjectId AND line.ElementType IN (0, 1, 2)
)
SELECT  map.ClientReference AS UnmatchedClientRef, map.Description
FROM    #Map AS map
LEFT JOIN Ordered
  ON    Ordered.TrimmedDescription = map.Description
 AND    Ordered.Occurrence         = map.Occurrence
WHERE   Ordered.TrimmedDescription IS NULL
ORDER BY map.ClientReference;

/* Non-variation lines still without a reference (expected: the five listed in
   the header, plus anything added since the 25/08/2026 export). */
SELECT  line.Description, line.Quantity, line.Rate, line.LineAmount
FROM    dbo.ValuationLineItems AS line
WHERE   line.ProjectId = @ProjectId
  AND   line.ElementType IN (0, 1, 2)
  AND   line.ClientReference = N''
ORDER BY line.ElementType, line.DisplayOrder;

/* Qty/rate drift between the export the map was built from and the line as it
   stands now — worth an eyeball, not an error. */
;WITH Ordered AS (
    SELECT  LTRIM(RTRIM(line.Description)) AS TrimmedDescription,
            line.Quantity, line.Rate,
            ROW_NUMBER() OVER (
                PARTITION BY LTRIM(RTRIM(line.Description))
                ORDER BY line.ElementType, line.DisplayOrder) AS Occurrence
    FROM    dbo.ValuationLineItems AS line
    WHERE   line.ProjectId = @ProjectId AND line.ElementType IN (0, 1, 2)
)
SELECT  map.ClientReference, map.Description,
        map.ExpectedQuantity, Ordered.Quantity,
        map.ExpectedRate, Ordered.Rate
FROM    #Map AS map
JOIN    Ordered
  ON    Ordered.TrimmedDescription = map.Description
 AND    Ordered.Occurrence         = map.Occurrence
WHERE   Ordered.Quantity <> map.ExpectedQuantity
   OR   Ordered.Rate     <> map.ExpectedRate
ORDER BY map.ClientReference;

DROP TABLE #Map;
