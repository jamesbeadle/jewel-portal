# Integrations Catalogue

Single source of truth for every external or adjacent system named across the twenty-one workflows. Each workflow file references integrations by name; this catalogue is where direction, owner, and target status are decided.

**Sourced from:** [`/docs/meetings/2026-05-20-jbb-workflow-audit.md`](../meetings/2026-05-20-jbb-workflow-audit.md)

**Status:** Draft — target status (keep / replace / archive) confirmed per integration as each workflow is signed off.

---

## Legend

- **Direction:** `→ JPMS` (JPMS consumes from it) · `JPMS →` (JPMS writes to it) · `↔` (both).
- **Target status under JPMS:**
  - **Keep — core integration:** stays after rollout, JPMS depends on it.
  - **Keep — peripheral:** stays for specific use; not core.
  - **Replace:** JPMS replaces this system's role.
  - **Archive:** kept read-only for historical data.
  - **TBD:** under review.

---

## Catalogue

### Finance & accounting

| System | Direction | Workflows | Target status | Notes |
|---|---|---|---|---|
| **Xero** | ↔ | 09, 10, 11, 13 | Keep — core integration | Posting for AP; invoice creation for AR; AP/AR balances feeding cashflow. Multi-entity (BB/PS/PFP) handling TBC. |
| **Dext** | JPMS → (route) · → JPMS (capture) | 09, 13 | Keep — core integration | Invoice OCR capture. Inbox triage routes invoices here automatically. |
| **Brightpay** | JPMS → | 12 | Keep — core integration | Payroll engine. Fed from JPMS timesheets + starter/leaver. |
| **Chaser HQ** | JPMS → | 09, 10 | Keep — peripheral | Collections sequence on AR; supplier disputes on AP. |
| **Online banking** | (manual) | 09 | Keep — peripheral | Payment execution. No JPMS integration planned in phase 1. |
| **HMRC CIS** | ↔ | 08, 09 | Keep — core integration | CIS verification on subbies; status held against record; gates payment. |

### Project lifecycle tools

| System | Direction | Workflows | Target status | Notes |
|---|---|---|---|---|
| **Bluebeam** | → JPMS | 01, 02 | Keep — peripheral | Take-off quantities import into BoQ. Drawing mark-up handoff. |
| **MS Project** | (migration only) | 05 | Replace | Programme module inside JPMS takes over. |
| **Buildertrend** | (migration only) | 01, 03 | Replace | Drawing distribution and work-order contracts move into JPMS. |
| **Planyard** | (migration only) | 03 | Replace | Work-order contracting moves into JPMS. |
| **Onetrace** | → JPMS | 06 | TBD | Where relevant for site capture; keep if it offers something the JPMS site app doesn't. |
| **Dashpivot** | (migration only) | 06, 08 | Replace | Site capture + attendance + compliance move into JPMS. |
| **Monday.com** | (migration only) | 03, 08, 19 | Replace | Subbie directory + compliance + fleet move into JPMS. |
| **RAMsApp** | (migration only) | 03, 08 | Replace | RAMS template engine moves into JPMS. |

### Microsoft 365 stack

| System | Direction | Workflows | Target status | Notes |
|---|---|---|---|---|
| **Outlook** | → JPMS (inbox monitor) | 01, 04, 09, 13, 14, 15, 18 | Keep — core integration | Email remains the inbound channel; JPMS monitors specific inboxes. |
| **SharePoint** | → JPMS (migration) · archive | 01, 03, 08, 14, 18, 19, 21 | Archive | Project folders move into JPMS; SharePoint reduces to archive + corporate documents only. |
| **OneDrive** | (peripheral) | 21 | Keep — peripheral | Personal / draft work only. |
| **Teams** | (peripheral) | 16 | Keep — peripheral | Comms only; account provisioning happens via JPMS 16. |
| **M365 Admin** | JPMS → | 16, 17 | Keep — core integration | Provisioning hooks from workflow 16. |
| **Entra ID** | JPMS → | 16, 17 | Keep — core integration | Identity + group membership for account provisioning. Likely the auth backbone for internal JPMS users. |
| **Intune** | JPMS → | 16, 17 | Keep — core integration | Device enrolment on starter. |
| **Defender** | (peripheral) | 13, 17 | Keep — peripheral | Spam/phishing handled here, not in JPMS classifier. |

### External / client portals

| System | Direction | Workflows | Target status | Notes |
|---|---|---|---|---|
| **Dwellant** | JPMS → | 08, 10 | Keep — peripheral | Client-side portal upload for invoices / RAMS where required. |
| **Vantify** | JPMS → | 08, 10 | Keep — peripheral | As Dwellant. |
| **Insurance broker portals** | (manual) | 18, 19 | TBD | Broker-by-broker decision. |
| **TfL / council portals** | (manual) | 19 | Keep — peripheral | Fine payment / appeals. Manual workflow. |
| **HMRC portal (non-CIS)** | (manual) | 12, 18 | Keep — peripheral | General HMRC interactions. |

### Office & operational tools

| System | Direction | Workflows | Target status | Notes |
|---|---|---|---|---|
| **Phone system** | → JPMS (caller-ID lookup) | 14 | Keep — peripheral | Platform TBC. |
| **WhatsApp** | (legacy) | 06, 15 | Archive | One-way reads to onboard legacy users; eliminated post-rollout. |
| **Amazon** | (manual) | 15 | Keep — peripheral | Office consumables ordering. |
| **Paperstone** | (manual) | 15 | Keep — peripheral | Office consumables ordering. |
| **1Password** | (peripheral) | 16, 17 | Keep — peripheral | Vault provisioning on starter. |

### Marketing & brand

| System | Direction | Workflows | Target status | Notes |
|---|---|---|---|---|
| **Canva** | (peripheral) | 20 | Keep — peripheral | Design tooling. JPMS surfaces the consent flag and asset-library link only. |
| **Meta Business** | (peripheral) | 20 | Keep — peripheral | Publishing. |
| **LinkedIn** | (peripheral) | 20 | Keep — peripheral | Publishing. |
| **Marketing scheduling tool** | (peripheral) | 20 | TBD | Provider not selected. |

### Compliance partners (target / new)

| System | Direction | Workflows | Target status | Notes |
|---|---|---|---|---|
| **Outsourced IT helpdesk vendor** | ↔ (audit reports + tickets) | 16, 17 | TBD — vendor not selected | Target owner of workflow 17. |
| **E-signature provider** | JPMS → | 16 | TBD | DocuSign / Adobe Sign / M365 native — decision pending. |

---

## Phase-1 integration shortlist

Driven by the audit's recommended workflow order (finance → project lifecycle). The integrations on this list need to be live for phase 1:

1. **Xero** (09, 10, 11)
2. **Dext** (09, 13)
3. **Brightpay** (12)
4. **HMRC CIS** (08, 09)
5. **Outlook inbox monitoring** (09, 13, 14)
6. **Entra ID / M365 auth** (across the platform — internal users)
7. **Chaser HQ** (09, 10)
8. **Bluebeam** (02 — needed once project-lifecycle phase 2 begins)

Everything else can land in phase 2 or stay out of scope per the workflow files.
