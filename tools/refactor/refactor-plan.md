# Refactoring plan

Written by the code quality check at a score of 74.0%. These are the steps a refactor of this repository follows, in this order; a round takes the next steps from the top. The plan is measured, so a finished step is gone the next time the check runs. The facts are measured; the judgement is the round's.

## The order

**Pass 1 — Component breakout**

1. Break `jpms/Components/ProjectDetailsEditor.razor` (440 lines) into components. Component-sized blocks: lines 41–65 (25 lines, taking OnPartyChanged); lines 67–80 (14 lines, taking OnOnBehalfOfClientChanged); lines 82–103 (22 lines, taking OnOrganisationChanged); lines 134–157 (24 lines, taking OnXeroContactChanged).
2. Break `jpms/Pages/XeroAllocation.razor` (433 lines) into components. Component-sized blocks: lines 66–103 (38 lines); lines 162–177 (16 lines); lines 185–263 (79 lines); lines 280–301 (22 lines).
3. Break `jpms/Pages/AdminKpis.razor` (429 lines) into components. Component-sized blocks: lines 88–101 (14 lines); lines 102–167 (66 lines, taking OpenInControlCentre, StartEdit, StartRemove, ConfirmRemoveAsync); lines 178–204 (27 lines, taking SaveEditAsync); lines 206–229 (24 lines, taking AddPersonAsync).
4. Break `jpms/Pages/ProjectValuation.razor` (420 lines) into components. Component-sized blocks: lines 23–74 (52 lines); lines 110–146 (37 lines); lines 148–162 (15 lines); lines 165–244 (80 lines).
5. Break `jpms/Pages/SubcontractorDetail.razor` (419 lines) into components. Component-sized blocks: lines 41–84 (44 lines); lines 101–150 (50 lines); lines 154–198 (45 lines); lines 203–279 (77 lines).
6. Break `jpms/Pages/CostCodes.razor` (415 lines) into components. Component-sized blocks: lines 18–49 (32 lines); lines 103–139 (37 lines); lines 177–196 (20 lines); lines 219–275 (57 lines).
7. Break `jpms/Pages/TriageQueue.razor` (401 lines) into components. Component-sized blocks: lines 53–75 (23 lines); lines 78–90 (13 lines); lines 93–121 (29 lines); lines 125–142 (18 lines).
8. Break `jpms/Pages/ProjectVariations.razor` (400 lines) into components. Component-sized blocks: lines 27–47 (21 lines); lines 51–76 (26 lines); lines 82–94 (13 lines); lines 96–113 (18 lines).
9. Break `jpms/Features/Triage/AttachmentPicker.razor` (396 lines) into components. Component-sized blocks: lines 19–33 (15 lines); lines 35–51 (17 lines); lines 56–69 (14 lines); lines 90–134 (45 lines, taking DrawingRow).
10. Break `jpms/Components/ValuationReportTable.razor` (395 lines) into components. Component-sized blocks: lines 48–73 (26 lines); lines 86–100 (15 lines); lines 111–151 (41 lines); lines 176–187 (12 lines).
11. Break `jpms/Features/Site/Programme/ProgrammeClaimsWorkbench.razor` (385 lines) into components. Component-sized blocks: lines 22–41 (20 lines, taking RaiseNodAsync); lines 44–83 (40 lines, taking RaiseEotAsync); lines 87–130 (44 lines, taking RecordLadAsync); lines 139–169 (31 lines).
12. Break `jpms/Components/DrawingExtractionPanel.razor` (383 lines) into components. Component-sized blocks: lines 83–99 (17 lines); lines 101–114 (14 lines); lines 116–143 (28 lines, taking RevisionRows); lines 145–161 (17 lines).
13. Break `jpms/Pages/ProjectLabour.razor` (381 lines) into components. Component-sized blocks: lines 39–54 (16 lines); lines 64–135 (72 lines); lines 137–182 (46 lines); lines 185–232 (48 lines).
14. Break `jpms/Pages/DocumentControl.razor` (367 lines) into components. Component-sized blocks: lines 33–49 (17 lines); lines 54–97 (44 lines); lines 101–119 (19 lines); lines 164–243 (80 lines).
15. Break `jpms/Features/Triage/Panels/Actions/StagedRecordActionEditor.razor` (366 lines) into components. Component-sized blocks: lines 15–48 (34 lines); lines 62–87 (26 lines); lines 96–109 (14 lines); lines 112–155 (44 lines).
16. Break `jpms/Features/Triage/Panels/CorrespondenceThreadList.razor` (366 lines) into components. Component-sized blocks: lines 26–43 (18 lines, taking EmailEntry).
17. Break `jpms/Pages/PortalHome.razor` (362 lines) into components. Component-sized blocks: lines 68–135 (68 lines); lines 138–187 (50 lines); lines 189–251 (63 lines); lines 253–298 (46 lines).
18. Break `jpms/Components/FinancialsTable.razor` (358 lines) into components. Component-sized blocks: lines 5–36 (32 lines); lines 45–73 (29 lines); lines 93–108 (16 lines); lines 109–122 (14 lines).
19. Break `jpms/Components/VariationApprovePanel.razor` (347 lines) into components. Component-sized blocks: lines 12–88 (77 lines, taking AddLine, RemoveLine).
20. Break `jpms/Components/ValuationInvoicesSection.razor` (346 lines) into components. Component-sized blocks: lines 19–58 (40 lines); lines 86–99 (14 lines); lines 103–156 (54 lines); lines 165–203 (39 lines).
21. Break `jpms/Pages/Todos.razor` (342 lines) into components. Component-sized blocks: lines 52–67 (16 lines); lines 106–119 (14 lines); lines 142–153 (12 lines); lines 155–170 (16 lines).
22. Break `jpms/Pages/Registers.razor` (335 lines) into components. Component-sized blocks: lines 52–102 (51 lines, taking DeactivateAsync, DueCell); lines 104–146 (43 lines, taking SaveAsync, NamePlaceholder).
23. Break `jpms/Pages/ProjectBuildingControl.razor` (334 lines) into components. Component-sized blocks: lines 42–58 (17 lines); lines 62–88 (27 lines); lines 89–110 (22 lines); lines 117–155 (39 lines).
24. Break `jpms/Pages/ProjectWorkOrderAllocation.razor` (331 lines) into components. Component-sized blocks: lines 24–41 (18 lines); lines 71–82 (12 lines); lines 83–160 (78 lines, taking InvoicingPill); lines 174–186 (13 lines).
25. Break `jpms/Pages/WorkOrderPo.razor` (325 lines) into components. Component-sized blocks: lines 34–86 (53 lines, taking OpenEmailModal); lines 126–182 (57 lines); lines 184–201 (18 lines).
26. Break `jpms/Pages/AdminTrades.razor` (323 lines) into components. Component-sized blocks: lines 77–143 (67 lines, taking UsageCount, StartRename, ConfirmRenameAsync, StartDelete, ConfirmDeleteAsync).
27. Break `jpms/Pages/AgedPayables.razor` (322 lines) into components. Component-sized blocks: lines 20–35 (16 lines, taking ForceRefreshAsync); lines 64–80 (17 lines); lines 111–173 (63 lines, taking BucketCellClass).
28. Break `jpms/Pages/AgedReceivables.razor` (321 lines) into components. Component-sized blocks: lines 19–34 (16 lines, taking ForceRefreshAsync); lines 63–79 (17 lines); lines 110–172 (63 lines, taking BucketCellClass).
29. Break `jpms/Components/CostCentreReconciliationModal.razor` (320 lines) into components. Component-sized blocks: lines 32–64 (33 lines, taking SalesRef); lines 67–95 (29 lines); lines 98–144 (47 lines); lines 147–192 (46 lines).
30. Break `jpms/Components/ProjectTodoList.razor` (319 lines) into components. Component-sized blocks: lines 34–60 (27 lines); lines 72–97 (26 lines); lines 133–155 (23 lines); lines 158–184 (27 lines).
31. Break `jpms/Components/PurchaseOrderSheet.razor` (310 lines) into components. Component-sized blocks: lines 18–39 (22 lines); lines 43–67 (25 lines); lines 69–80 (12 lines); lines 82–150 (69 lines).
32. Break `jpms/Components/SubcontractorComplianceList.razor` (307 lines) into components. Component-sized blocks: lines 19–61 (43 lines, taking OpenEdit); lines 63–86 (24 lines); lines 93–138 (46 lines, taking CloseAdd, OnAddFileSelected, ConfirmAdd); lines 144–169 (26 lines, taking CloseEdit, ConfirmEdit).
33. Break `jpms/Pages/ProjectProgress.razor` (307 lines) into components. Component-sized blocks: lines 19–31 (13 lines); lines 48–79 (32 lines, taking Period, DeleteReportAsync); lines 83–113 (31 lines); lines 128–190 (63 lines, taking WeatherLine, DeleteUpdateAsync, DeletePhotoAsync, AddPhotosAsync).
34. Break `jpms/Pages/Clients.razor` (299 lines) into components. Component-sized blocks: lines 12–31 (20 lines, taking OpenCreate, BuildExportWorkbook); lines 49–73 (25 lines, taking OpenInvite, OpenContacts, OpenEdit); lines 79–97 (19 lines, taking CreateClientAsync); lines 99–124 (26 lines, taking SendInviteAsync).
35. Break `jpms/Pages/ProjectCashflow.razor` (299 lines) into components. Component-sized blocks: lines 65–82 (18 lines); lines 103–129 (27 lines); lines 170–184 (15 lines); lines 192–222 (31 lines).
36. Break `jpms/Components/ProjectContractPanel.razor` (297 lines) into components. Component-sized blocks: lines 45–117 (73 lines); lines 135–172 (38 lines); lines 175–218 (44 lines); lines 221–261 (41 lines).
37. Break `jpms/Components/ApprovedUserRow.razor` (295 lines) into components. Component-sized blocks: lines 21–44 (24 lines, taking SendReset, Revoke); lines 46–90 (45 lines, taking AddRole, RemoveRole); lines 92–104 (13 lines, taking ToggleRevert); lines 111–132 (22 lines, taking CopyResetLink).
38. Break `jpms/Pages/AdminSystem.razor` (292 lines) into components. Component-sized blocks: lines 55–101 (47 lines, taking PublishAsync); lines 105–154 (50 lines, taking FormatTermsSize, OnTermsFileSelected).
39. Break `jpms/Features/Sales/LeadFormModal.razor` (289 lines) into components. Component-sized blocks: lines 103–115 (13 lines, taking OnStrategyPicked); lines 116–128 (13 lines).
40. Break `jpms/Pages/ProjectArchitectInstructions.razor` (289 lines) into components. Component-sized blocks: lines 24–37 (14 lines); lines 47–59 (13 lines); lines 75–146 (72 lines); lines 153–224 (72 lines).
41. Break `jpms/Pages/RfiDashboard.razor` (286 lines) into components. Component-sized blocks: lines 20–34 (15 lines, taking StatusViewCount, StatusViewClass); lines 54–111 (58 lines, taking Date).
42. Break `jpms/Pages/Policies.razor` (285 lines) into components. Component-sized blocks: lines 44–95 (52 lines, taking SignAsync); lines 98–163 (66 lines, taking ToggleDetailAsync); lines 165–184 (20 lines, taking PublishAsync).
43. Break `jpms/Components/RequestAttachmentsPanel.razor` (280 lines) into components. Component-sized blocks: lines 15–86 (72 lines, taking LinkFor, OpenDrawingPicker, OnFilesSelected); lines 89–136 (48 lines, taking ToggleRevision, ConfirmDrawings).
44. Break `jpms/Pages/ProjectDefects.razor` (277 lines) into components. Component-sized blocks: lines 40–87 (48 lines, taking RaiseAsync); lines 103–166 (64 lines, taking SentTitle, SetStatusAsync).
45. Break `jpms/Components/MyTodosPanel.razor` (272 lines) into components. Component-sized blocks: lines 25–48 (24 lines, taking ViewTabClass, ToggleSort); lines 59–72 (14 lines); lines 75–86 (12 lines, taking SetComplete); lines 97–116 (20 lines, taking ScopeLabel, IsOverdue).
46. Break `jpms/Pages/LabourXeroMapping.razor` (272 lines) into components. Component-sized blocks: lines 52–109 (58 lines, taking SaveSiteMappingAsync); lines 111–177 (67 lines, taking SaveCodeMappingAsync).
47. Break `jpms/Components/RecipientInput.razor` (270 lines) into components. Component-sized blocks: lines 20–67 (48 lines, taking SecondLineOf, FocusInputAsync, OnInput, OnKeyDown, OnFocusOut).
48. Break `jpms/Pages/ProjectInventory.razor` (270 lines) into components. Component-sized blocks: lines 38–76 (39 lines, taking SaveAsync); lines 92–150 (59 lines, taking StartEdit, ToggleEmails).
49. Break `jpms/Components/ManualVariationForm.razor` (268 lines) into components. Component-sized blocks: lines 17–82 (66 lines).
50. Break `jpms/Pages/ProjectRequests.razor` (267 lines) into components. Component-sized blocks: lines 25–44 (20 lines); lines 58–95 (38 lines); lines 99–122 (24 lines); lines 124–137 (14 lines).
51. Break `jpms/Components/DrawingsTable.razor` (265 lines) into components. Component-sized blocks: lines 33–97 (65 lines, taking HasSubFolders, ToggleCollapse).
52. Break `jpms/Pages/ProjectVariationDetail.razor` (264 lines) into components. Component-sized blocks: lines 91–107 (17 lines); lines 121–152 (32 lines); lines 154–208 (55 lines); lines 217–239 (23 lines).
53. Break `jpms/Features/Triage/Queue/TriageMessageDetail.razor` (263 lines) into components. Component-sized blocks: lines 9–35 (27 lines); lines 41–97 (57 lines, taking ToggleAllDocumentTriage); lines 103–120 (18 lines); lines 124–137 (14 lines).
54. Break `jpms/Layout/SideNav.razor` (263 lines) into components. Component-sized blocks: lines 25–39 (15 lines); lines 51–129 (79 lines); lines 131–156 (26 lines); lines 159–212 (54 lines).
55. Break `jpms/Features/Requests/RequestOfficialFormPanel.razor` (261 lines) into components. Component-sized blocks: lines 9–28 (20 lines); lines 35–97 (63 lines); lines 100–171 (72 lines, taking Cancel, SaveAsync).
56. Break `jpms/Pages/ProjectSiteInstructions.razor` (261 lines) into components. Component-sized blocks: lines 40–72 (33 lines, taking SaveAsync); lines 88–142 (55 lines, taking StartEdit, ToggleEmails).
57. Break `jpms/Pages/ProjectBidPackageInviteDetail.razor` (260 lines) into components. Component-sized blocks: lines 44–77 (34 lines); lines 97–110 (14 lines); lines 124–145 (22 lines); lines 161–173 (13 lines).
58. Break `jpms/Pages/Workers.razor` (259 lines) into components. Component-sized blocks: lines 63–105 (43 lines); lines 114–182 (69 lines); lines 189–257 (69 lines).
59. Break `jpms/Components/RequestConversation.razor` (258 lines) into components. Component-sized blocks: lines 15–32 (18 lines); lines 48–71 (24 lines); lines 74–147 (74 lines); lines 173–222 (50 lines).
60. Break `jpms/Pages/ProjectRequestDetail.razor` (258 lines) into components. Component-sized blocks: lines 76–125 (50 lines); lines 127–164 (38 lines); lines 188–203 (16 lines); lines 205–218 (14 lines).
61. Break `jpms/Pages/ProjectCommunications.razor` (257 lines) into components. Component-sized blocks: lines 42–75 (34 lines); lines 83–98 (16 lines); lines 109–123 (15 lines); lines 150–170 (21 lines).
62. Break `jpms/Features/Site/Programme/ProgrammeWorkbench.razor` (256 lines) into components. Component-sized blocks: lines 21–58 (38 lines); lines 68–79 (12 lines); lines 88–114 (27 lines); lines 117–152 (36 lines).
63. Break `jpms/Pages/AiSkillsAdmin.razor` (256 lines) into components. Component-sized blocks: lines 32–104 (73 lines); lines 107–120 (14 lines); lines 132–188 (57 lines); lines 190–253 (64 lines).
64. Break `jpms/Pages/ProjectDrawingDetail.razor` (255 lines) into components. Component-sized blocks: lines 16–32 (17 lines); lines 35–54 (20 lines); lines 57–113 (57 lines); lines 114–145 (32 lines).
65. Break `jpms/Pages/ProjectCalendar.razor` (254 lines) into components. Component-sized blocks: lines 25–36 (12 lines); lines 54–116 (63 lines); lines 118–160 (43 lines); lines 168–238 (71 lines).
66. Break `jpms/Pages/ProjectBuildingControlInspection.razor` (248 lines) into components. Component-sized blocks: lines 43–56 (14 lines); lines 64–107 (44 lines); lines 110–139 (30 lines); lines 142–175 (34 lines).
67. Break `jpms/Pages/SalesLeads.razor` (248 lines) into components. Component-sized blocks: lines 25–37 (13 lines); lines 66–78 (13 lines, taking ChipClass); lines 93–146 (54 lines).
68. Break `jpms/Components/SearchSelect.razor` (246 lines) into components. Component-sized blocks: lines 26–62 (37 lines, taking OnInput, OnKeyDown, ItemClass).
69. Break `jpms/Features/Triage/Panels/PathwayPane.razor` (246 lines) into components. Component-sized blocks: lines 45–57 (13 lines); lines 61–75 (15 lines); lines 91–127 (37 lines); lines 139–187 (49 lines).
70. Break `jpms/Components/WorkOrderLineRecodeModal.razor` (245 lines) into components. Component-sized blocks: lines 9–72 (64 lines, taking SetRowCode, SaveAsync).
71. Break `jpms/Features/Site/Programme/RelevantEventsList.razor` (244 lines) into components. Component-sized blocks: lines 17–29 (13 lines); lines 47–77 (31 lines); lines 82–96 (15 lines, taking ToggleEmailAsync, ToggleReply); lines 97–124 (28 lines, taking EmailReplyAsync).
72. Break `jpms/Pages/AdminIntegrations.razor` (244 lines) into components. Component-sized blocks: lines 98–120 (23 lines, taking DisconnectAsync).
73. Break `jpms/Components/DropdownMenu.razor` (243 lines) into components. Component-sized blocks: lines 28–97 (70 lines, taking Toggle, Run).
74. Break `jpms/Components/SubcontractorStatementModal.razor` (243 lines) into components. Component-sized blocks: lines 36–80 (45 lines, taking MoneyExact).
75. Break `jpms/Features/Procurement/TenderSubmissionsSection.razor` (242 lines) into components. Component-sized blocks: lines 28–81 (54 lines); lines 83–116 (34 lines); lines 124–156 (33 lines); lines 159–171 (13 lines).
76. Break `jpms/Pages/XeroTransactions.razor` (242 lines) into components. Component-sized blocks: lines 12–28 (17 lines); lines 98–173 (76 lines); lines 177–191 (15 lines); lines 193–238 (46 lines).
77. Break `jpms/Pages/ProjectBidPackageInvites.razor` (241 lines) into components. Component-sized blocks: lines 13–76 (64 lines); lines 79–121 (43 lines); lines 130–148 (19 lines); lines 165–178 (14 lines).
78. Break `jpms/Features/Sales/ImagineProposal.razor` (238 lines) into components. Component-sized blocks: lines 22–33 (12 lines); lines 43–78 (36 lines, taking Toggle); lines 80–99 (20 lines, taking BarStyle); lines 109–150 (42 lines, taking AcceptAsync, DeclineAsync).
79. Break `jpms/Pages/Architects.razor` (237 lines) into components. Component-sized blocks: lines 11–30 (20 lines, taking OpenCreate, BuildExportWorkbook); lines 48–71 (24 lines, taking OpenContacts, OpenEdit); lines 77–94 (18 lines, taking CreateArchitectAsync); lines 96–113 (18 lines, taking SaveEditAsync).
80. Break `jpms/Components/UsefulInformationPanel.razor` (234 lines) into components. Component-sized blocks: lines 17–87 (71 lines, taking Matches, OpenAdd, OpenEdit); lines 91–119 (29 lines, taking Save).
81. Break `jpms/Pages/PaymentCertificates.razor` (233 lines) into components. Component-sized blocks: lines 43–120 (78 lines, taking TogglePreview, IsPdf).
82. Break `jpms/Components/ValuationStatementViewer.razor` (232 lines) into components. Component-sized blocks: lines 46–65 (20 lines); lines 73–88 (16 lines); lines 102–178 (77 lines); lines 186–203 (18 lines).
83. Break `jpms/Components/NextValuationDateEditor.razor` (231 lines) into components. Component-sized blocks: lines 54–109 (56 lines).
84. Break `jpms/Components/ProjectRetentionPanel.razor` (231 lines) into components. Component-sized blocks: lines 56–72 (17 lines); lines 76–88 (13 lines); lines 90–106 (17 lines); lines 108–143 (36 lines).
85. Break `jpms/Pages/AiActionsAdmin.razor` (231 lines) into components. Component-sized blocks: lines 50–77 (28 lines); lines 79–148 (70 lines, taking SkillChip, SkillPicker).
86. Break `jpms/Components/CostCentreCostOfSalesModal.razor` (229 lines) into components. Component-sized blocks: lines 30–44 (15 lines); lines 66–118 (53 lines); lines 119–153 (35 lines); lines 170–207 (38 lines).
87. Break `jpms/Features/Triage/Panels/EmailFinder.razor` (227 lines) into components. Component-sized blocks: lines 42–102 (61 lines, taking Toggle, LinkSelectedAsync, IsAlreadyTagged).
88. Break `jpms/Components/ClaimProgressDialog.razor` (224 lines) into components. Component-sized blocks: lines 10–69 (60 lines, taking AddPicked, Remove).
89. Break `jpms/Components/MyDayWorkspace.razor` (223 lines) into components. Component-sized blocks: lines 33–105 (73 lines, taking TodaysEntriesFor, SignInAsync, SubmitSignOutAsync); lines 108–120 (13 lines); lines 122–141 (20 lines, taking ResubmitHoursFor).
90. Break `jpms/Features/Sales/LeadImaginePanel.razor` (222 lines) into components. Component-sized blocks: lines 15–27 (13 lines, taking IssueAsync); lines 44–65 (22 lines, taking CopyAsync, DownloadAsync); lines 68–137 (70 lines, taking ToneFor, RetryAsync).
91. Break `jpms/Pages/CashForecast.razor` (217 lines) into components. Component-sized blocks: lines 98–119 (22 lines).
92. Break `jpms/Features/Triage/Queue/TaggedInboxBrowser.razor` (216 lines) into components. Component-sized blocks: lines 42–84 (43 lines, taking ClearTagFilters); lines 108–155 (48 lines).
93. Break `jpms/Components/DrawingRevisionList.razor` (214 lines) into components. Component-sized blocks: lines 21–83 (63 lines); lines 84–124 (41 lines); lines 136–150 (15 lines, taking DeletePendingAsync).
94. Break `jpms/Components/WorkOrderForm.razor` (212 lines) into components. Component-sized blocks: lines 24–48 (25 lines); lines 62–88 (27 lines); lines 91–159 (69 lines); lines 162–178 (17 lines).
95. Break `jpms/Features/Triage/Panels/PathwayActionsSection.razor` (212 lines) into components. Component-sized blocks: lines 22–82 (61 lines); lines 84–97 (14 lines); lines 100–111 (12 lines); lines 112–198 (87 lines).
96. Break `jpms/Components/WorkOrderLinkSplitModal.razor` (211 lines) into components. Component-sized blocks: lines 9–75 (67 lines, taking SetRowOrder, SaveAsync).
97. Break `jpms/Components/DrawingUploadForm.razor` (210 lines) into components. Component-sized blocks: lines 50–64 (15 lines); lines 66–138 (73 lines); lines 140–155 (16 lines); lines 157–172 (16 lines).
98. Break `jpms/Components/ProjectContractTermsDialog.razor` (210 lines) into components. Component-sized blocks: lines 24–49 (26 lines); lines 63–77 (15 lines); lines 79–98 (20 lines); lines 100–119 (20 lines).
99. Break `jpms/Components/UnpaidXeroInvoicesModal.razor` (210 lines) into components. Component-sized blocks: lines 38–89 (52 lines); lines 92–125 (34 lines).
100. Break `jpms/Features/Sales/LeadProposalsPanel.razor` (210 lines) into components. Component-sized blocks: lines 38–103 (66 lines, taking ToneFor, OpenSend, WithdrawAsync); lines 110–132 (23 lines).
101. Break `jpms/Components/TodoBoard.razor` (209 lines) into components. Component-sized blocks: lines 48–122 (75 lines, taking StripeClass).
102. Break `jpms/Features/Triage/Panels/KpiTagSection.razor` (206 lines) into components. Component-sized blocks: lines 20–32 (13 lines); lines 51–115 (65 lines, taking ToggleRow, StagedFor, StageAsync, UnstageAsync).
103. Break `jpms/Features/Sales/StrategyFormModal.razor` (205 lines) into components. Component-sized blocks: lines 74–100 (27 lines).
104. Break `jpms/Components/RequestForm.razor` (201 lines) into components. Component-sized blocks: lines 33–53 (21 lines); lines 79–122 (44 lines); lines 129–153 (25 lines); lines 158–201 (44 lines).
105. Break `jpms/Components/PackageReconciliationSection.razor` (200 lines) into components. Component-sized blocks: lines 11–26 (16 lines); lines 49–61 (13 lines); lines 62–129 (68 lines); lines 134–184 (51 lines).
106. Break `jpms/Components/PartyContactsEditor.razor` (199 lines) into components. Component-sized blocks: lines 30–78 (49 lines, taking ChangeRoutingAsync, MakePrimaryAsync); lines 82–102 (21 lines, taking AddAsync).
107. Break `jpms/Components/ValuationStatementEmailModal.razor` (199 lines) into components. Component-sized blocks: lines 13–90 (78 lines, taking EmailSnapshotAsync).
108. Break `jpms/Pages/AgentActivityLog.razor` (199 lines) into components. Component-sized blocks: lines 54–102 (49 lines, taking OutcomeLabel, OutcomeClass, FormatDuration); lines 104–117 (14 lines, taking FormatPence).
109. Break `jpms/Pages/ProjectWorkOrders.razor` (199 lines) into components. Component-sized blocks: lines 24–50 (27 lines); lines 77–101 (25 lines); lines 151–168 (18 lines).
110. Break `jpms/Features/Labour/SettlementSchedulesPanel.razor` (198 lines) into components. Component-sized blocks: lines 18–30 (13 lines); lines 57–129 (73 lines, taking BillTitle); lines 137–163 (27 lines, taking PlanPillClass).
111. Break `jpms/Components/ProjectCorrespondencePanel.razor` (197 lines) into components. Component-sized blocks: lines 52–95 (44 lines); lines 109–151 (43 lines); lines 154–194 (41 lines).
112. Break `jpms/Components/ValuationLineForm.razor` (193 lines) into components. Component-sized blocks: lines 7–32 (26 lines); lines 34–45 (12 lines); lines 47–64 (18 lines).
113. Break `jpms/Pages/ProfitSummary.razor` (193 lines) into components. Component-sized blocks: lines 99–120 (22 lines); lines 122–133 (12 lines).
114. Break `jpms/Features/Sales/ProposalFormModal.razor` (192 lines) into components. Component-sized blocks: lines 41–63 (23 lines); lines 65–86 (22 lines); lines 94–109 (16 lines).
115. Break `jpms/Components/ProgressReportForm.razor` (191 lines) into components. Component-sized blocks: lines 13–24 (12 lines); lines 47–77 (31 lines, taking Toggle).
116. Break `jpms/Features/Triage/Panels/OutboxPane.razor` (191 lines) into components. Component-sized blocks: lines 22–34 (13 lines, taking StageNewAsync, CancelComposeAsync, DisplayFrom); lines 36–103 (68 lines, taking UpdatedAsync, RemoveAsync).
117. Break `jpms/Pages/ProjectReconciliationAudit.razor` (189 lines) into components. Component-sized blocks: lines 29–102 (74 lines, taking Ago).
118. Break `jpms/Features/Procurement/InvitedSubcontractorsSection.razor` (185 lines) into components. Component-sized blocks: lines 9–34 (26 lines); lines 58–137 (80 lines, taking WebsiteHref, WebsiteLabel).
119. Break `jpms/Pages/ProjectDefectDetail.razor` (185 lines) into components. Component-sized blocks: lines 46–85 (40 lines); lines 92–150 (59 lines); lines 152–181 (30 lines).
120. Break `jpms/Features/Triage/Panels/Actions/StageTodosAction.razor` (183 lines) into components. Component-sized blocks: lines 11–62 (52 lines, taking RemoveTodoAssignee, RemoveTodoRow); lines 75–95 (21 lines).
121. Break `jpms/Features/Triage/Queue/TriageBar.razor` (183 lines) into components. Component-sized blocks: lines 16–65 (50 lines); lines 83–120 (38 lines).
122. Break `jpms/Features/Triage/TodosModal.razor` (182 lines) into components. Component-sized blocks: lines 11–60 (50 lines, taking RemoveRow); lines 73–93 (21 lines).
123. Break `jpms/Components/RequestTable.razor` (181 lines) into components. Component-sized blocks: lines 4–26 (23 lines); lines 27–105 (79 lines, taking Date).
124. Break `jpms/Features/Triage/Panels/RecordLinkSection.razor` (181 lines) into components. Component-sized blocks: lines 12–44 (33 lines, taking RecordRow, Filter).
125. Break `jpms/Pages/WeeklyCashflow.razor` (181 lines) into components. Component-sized blocks: lines 46–70 (25 lines); lines 132–148 (17 lines); lines 150–165 (16 lines).
126. Break `jpms/Features/Triage/Panels/CategoryRegisterSection.razor` (178 lines) into components. Component-sized blocks: lines 15–66 (52 lines, taking ScopeChipClass, LoadMoreAsync).
127. Break `jpms/Pages/ProjectFinancials.razor` (178 lines) into components. Component-sized blocks: lines 50–62 (13 lines); lines 63–101 (39 lines); lines 144–174 (31 lines).
128. Break `jpms/Pages/ProjectHs.razor` (176 lines) into components. Component-sized blocks: lines 27–38 (12 lines); lines 47–84 (38 lines); lines 88–124 (37 lines); lines 127–145 (19 lines).
129. Break `jpms/Features/Triage/Panels/Actions/StageRequestTransitionAction.razor` (175 lines) into components. Component-sized blocks: lines 12–37 (26 lines, taking StageAsync).
130. Break `jpms/Components/ProgressUpdateForm.razor` (174 lines) into components. Component-sized blocks: lines 38–79 (42 lines).
131. Break `jpms/Features/Cvr/CumulativePositionPanel.razor` (172 lines) into components. Component-sized blocks: lines 14–29 (16 lines); lines 42–74 (33 lines, taking Money); lines 76–125 (50 lines).
132. Break `jpms/Pages/ConnectAuthorize.razor` (172 lines) into components. Component-sized blocks: lines 19–72 (54 lines, taking DecideAsync).
133. Break `jpms/Components/ReconciliationPackageBuilderModal.razor` (171 lines) into components. Component-sized blocks: lines 25–91 (67 lines); lines 96–147 (52 lines).
134. Break `jpms/Components/PdfViewer.razor` (167 lines) into components. Component-sized blocks: lines 19–74 (56 lines).
135. Break `jpms/Features/Closeout/Detail/DefectTodosPanel.razor` (167 lines) into components. Component-sized blocks: lines 13–63 (51 lines, taking OpenAdd); lines 65–91 (27 lines, taking AddAsync).
136. Break `jpms/Components/ManualWorkOrderModal.razor` (166 lines) into components. Component-sized blocks: lines 30–79 (50 lines); lines 87–149 (63 lines).
137. Break `jpms/Pages/Subcontractors.razor` (163 lines) into components. Component-sized blocks: lines 36–56 (21 lines); lines 85–141 (57 lines).
138. Break `jpms/Features/Triage/Panels/Actions/StageKpiAction.razor` (162 lines) into components. Component-sized blocks: lines 19–46 (28 lines, taking StageAsync).
139. Break `jpms/Pages/SetPassword.razor` (161 lines) into components. Component-sized blocks: lines 27–100 (74 lines, taking HandleSubmit).
140. Break `jpms/Components/RecordAuditHistory.razor` (160 lines) into components. Component-sized blocks: lines 17–68 (52 lines, taking EventLabel).
141. Break `jpms/Pages/ProjectDrawings.razor` (160 lines) into components. Component-sized blocks: lines 14–73 (60 lines); lines 103–117 (15 lines); lines 119–134 (16 lines); lines 136–152 (17 lines).
142. Break `jpms/Components/WorkOrderAttachmentsPanel.razor` (158 lines) into components. Component-sized blocks: lines 10–73 (64 lines, taking OnFilesSelected, Remove, Size).
143. Break `jpms/Features/Cvr/RunningProfitPanel.razor` (158 lines) into components. Component-sized blocks: lines 24–71 (48 lines, taking OnFloorChangedAsync); lines 76–125 (50 lines).
144. Break `jpms/Pages/AuditTrail.razor` (158 lines) into components. Component-sized blocks: lines 33–67 (35 lines); lines 91–144 (54 lines).
145. Break `jpms/Pages/PortalWorkOrderView.razor` (157 lines) into components. Component-sized blocks: lines 16–93 (78 lines, taking AcceptAsync).
146. Break `jpms/Features/Sales/LeadHouseModelPanel.razor` (155 lines) into components. Component-sized blocks: lines 16–42 (27 lines, taking SetPhaseAsync).
147. Break `jpms/Features/Triage/Panels/Actions/StageCalendarEventAction.razor` (155 lines) into components. Component-sized blocks: lines 10–73 (64 lines, taking OnKindChanged, SetTitle).
148. Break `jpms/Features/Triage/Panels/XeroTransactionView.razor` (155 lines) into components. Component-sized blocks: lines 15–30 (16 lines); lines 34–70 (37 lines, taking IsPreviewable, PreviewAttachment); lines 72–93 (22 lines).
149. Break `jpms/Features/Triage/Queue/TaggedEmailManagePanel.razor` (155 lines) into components. Component-sized blocks: lines 15–29 (15 lines); lines 31–57 (27 lines); lines 59–117 (59 lines).
150. Break `jpms/Pages/SalesStrategies.razor` (153 lines) into components. Component-sized blocks: lines 24–36 (13 lines); lines 61–119 (59 lines).
151. Break `jpms/Components/ImageViewer.razor` (152 lines) into components. Component-sized blocks: lines 16–65 (50 lines).
152. Break `jpms/Components/ProjectPageShell.razor` (152 lines) into components. Component-sized blocks: lines 23–63 (41 lines, taking Neighbour, GoTo).
153. Break `jpms/Features/Xero/Allocation/InvoiceViewerActions.razor` (152 lines) into components. Component-sized blocks: lines 9–26 (18 lines); lines 29–48 (20 lines); lines 54–111 (58 lines).
154. Break `jpms/Pages/ProjectHsAudit.razor` (152 lines) into components. Component-sized blocks: lines 31–60 (30 lines); lines 71–119 (49 lines).
155. Break `jpms/Components/NewProjectForm.razor` (151 lines) into components. Component-sized blocks: lines 7–77 (71 lines, taking OnOrganisationChanged, Submit).
156. Break `jpms/Features/Requests/EmailDraftStagingModal.razor` (151 lines) into components. Component-sized blocks: lines 15–70 (56 lines, taking AuthorLabel); lines 73–93 (21 lines).
157. Break `jpms/Features/Hs/Audits/HsAuditSectionPanel.razor` (150 lines) into components. Component-sized blocks: lines 23–36 (14 lines); lines 37–107 (71 lines, taking Edit, ParseInt, ParseDate).
158. Break `jpms/Pages/AiConnections.razor` (150 lines) into components. Component-sized blocks: lines 17–89 (73 lines, taking ToggleShowAll, RevokeAsync).
159. Break `jpms/Features/Variations/ApprovedFiguresPanel.razor` (149 lines) into components. Component-sized blocks: lines 8–87 (80 lines, taking CancelRevise, SubmitReviseAsync).
160. Break `jpms/Components/KpiPersonPicker.razor` (148 lines) into components. Component-sized blocks: lines 16–32 (17 lines, taking OnKeyChanged, OnNewNameInput).
161. Break `jpms/Pages/Projects.razor` (147 lines) into components. Component-sized blocks: lines 11–61 (51 lines, taking BuildExportWorkbook, HandleCreated).
162. Break `jpms/Components/InviteUserForm.razor` (146 lines) into components. Component-sized blocks: lines 4–81 (78 lines, taking ToggleRole, Submit, CopyLink, Reset).
163. Break `jpms/Features/Triage/RichTextEditor.razor` (145 lines) into components. Component-sized blocks: lines 11–56 (46 lines, taking ApplyColourAsync, ExecAsync).
164. Break `jpms/Features/Requests/RequestPartyPanel.razor` (144 lines) into components. Component-sized blocks: lines 39–63 (25 lines, taking OnPartySelected); lines 65–78 (14 lines); lines 80–102 (23 lines, taking RecipientLine).
165. Break `jpms/Components/ErrorToast.razor` (140 lines) into components. Component-sized blocks: lines 21–85 (65 lines, taking SignInAgain, PagePath).
166. Break `jpms/Features/Cvr/RunningProfitTable.razor` (140 lines) into components. Component-sized blocks: lines 26–82 (57 lines); lines 83–121 (39 lines).
167. Break `jpms/Features/Triage/Panels/Actions/StageVariationDecisionAction.razor` (140 lines) into components. Component-sized blocks: lines 10–53 (44 lines, taking StageApproveAsync, StageRejectAsync).
168. Break `jpms/Features/Variations/StagedBuildUpPanel.razor` (139 lines) into components. Component-sized blocks: lines 10–37 (28 lines); lines 39–75 (37 lines, taking Close, StageAsync).
169. Break `jpms/Features/Variations/VariationDocumentPanel.razor` (139 lines) into components. Component-sized blocks: lines 10–80 (71 lines, taking CancelAsync, SaveAsync).
170. Break `jpms/Features/Xero/Allocation/AllocatedSummaryRow.razor` (138 lines) into components. Component-sized blocks: lines 13–71 (59 lines); lines 76–92 (17 lines).
171. Break `jpms/Components/CostCentreSalesLinesModal.razor` (137 lines) into components. Component-sized blocks: lines 32–85 (54 lines); lines 87–128 (42 lines).
172. Break `jpms/Features/Directory/XeroImportModal.razor` (137 lines) into components. Component-sized blocks: lines 18–30 (13 lines); lines 63–122 (60 lines).
173. Break `jpms/Features/Requests/RequestFactsEditModal.razor` (136 lines) into components. Component-sized blocks: lines 7–42 (36 lines, taking SaveAsync).
174. Break `jpms/Components/RecordTabBar.razor` (135 lines) into components. Component-sized blocks: lines 20–54 (35 lines).
175. Break `jpms/Features/Site/Programme/ProgrammeDraftReview.razor` (135 lines) into components. Component-sized blocks: lines 16–38 (23 lines); lines 45–123 (79 lines).
176. Break `jpms/Pages/LabourOverview.razor` (134 lines) into components. Component-sized blocks: lines 31–46 (16 lines).
177. Break `jpms/Features/Labour/WorkerDetailPanel.razor` (133 lines) into components. Component-sized blocks: lines 8–74 (67 lines, taking SaveAsync).
178. Break `jpms/Features/Procurement/PackageDetailsSections.razor` (133 lines) into components. Component-sized blocks: lines 8–33 (26 lines); lines 57–113 (57 lines).
179. Break `jpms/Features/Triage/Panels/Actions/StageTodoCompleteAction.razor` (132 lines) into components. Component-sized blocks: lines 11–38 (28 lines, taking StageAsync).
180. Break `jpms/Features/Xero/InvoiceDocumentPreview.razor` (129 lines) into components. Component-sized blocks: lines 30–63 (34 lines).
181. Break `jpms/Features/Requests/RequestHeaderEditModal.razor` (128 lines) into components. Component-sized blocks: lines 7–48 (42 lines, taking OnStatusChanged, SaveAsync).
182. Break `jpms/Components/RoleHome.razor` (127 lines) into components. Component-sized blocks: lines 36–50 (15 lines); lines 52–112 (61 lines).
183. Break `jpms/Features/Triage/Panels/RecordCorrespondencePanel.razor` (126 lines) into components. Component-sized blocks: lines 12–25 (14 lines); lines 41–55 (15 lines, taking OnSent).
184. Break `jpms/Components/LoadGate.razor` (125 lines) into components. Component-sized blocks: lines 31–48 (18 lines).
185. Break `jpms/Components/ProjectContractAmendmentDialog.razor` (125 lines) into components. Component-sized blocks: lines 14–57 (44 lines, taking SaveAsync).
186. Break `jpms/Pages/ProjectSettings.razor` (124 lines) into components. Component-sized blocks: lines 19–64 (46 lines, taking SiteAddress, SiteNoteSenderList).
187. Break `jpms/Components/ProjectMultiSelect.razor` (122 lines) into components. Component-sized blocks: lines 15–62 (48 lines, taking ToggleProject, SelectAll).
188. Break `jpms/Pages/SubcontractorCommunications.razor` (122 lines) into components. Component-sized blocks: lines 35–47 (13 lines); lines 69–82 (14 lines); lines 93–107 (15 lines).
189. Break `jpms/Pages/Login.razor` (121 lines) into components. Component-sized blocks: lines 14–74 (61 lines, taking HandleSubmit).
190. Break `jpms/Features/Procurement/ValuationLinePickerModal.razor` (119 lines) into components. Component-sized blocks: lines 53–111 (59 lines).
191. Break `jpms/Components/DirectoryContactForm.razor` (118 lines) into components. Component-sized blocks: lines 22–55 (34 lines); lines 65–76 (12 lines); lines 77–88 (12 lines); lines 94–105 (12 lines).
192. Break `jpms/Components/RevokedUserRow.razor` (118 lines) into components. Component-sized blocks: lines 13–74 (62 lines).
193. Break `jpms/Components/RaiseRequestDialog.razor` (116 lines) into components. Component-sized blocks: lines 12–25 (14 lines, taking Submit).
194. Break `jpms/Components/RoleOverridePrompt.razor` (116 lines) into components. Component-sized blocks: lines 18–48 (31 lines, taking Keep).
195. Break `jpms/Layout/MainLayout.razor` (116 lines) into components. Component-sized blocks: lines 7–69 (63 lines).
196. Break `jpms/Features/Cashflow/CombinedStatementCard.razor` (115 lines) into components. Component-sized blocks: lines 34–51 (18 lines).
197. Break `jpms/Features/Procurement/TenderQuoteComparisonTable.razor` (115 lines) into components. Component-sized blocks: lines 11–38 (28 lines); lines 39–67 (29 lines); lines 68–91 (24 lines).
198. Break `jpms/Features/WeeklyCashflow/CashflowBandSection.razor` (115 lines) into components. Component-sized blocks: lines 15–32 (18 lines); lines 33–86 (54 lines).
199. Break `jpms/Features/Procurement/LocalSubcontractorFinderModal.razor` (114 lines) into components. Component-sized blocks: lines 42–54 (13 lines); lines 66–109 (44 lines).
200. Break `jpms/Features/Variations/VariationDetailsCard.razor` (113 lines) into components. Component-sized blocks: lines 11–49 (39 lines, taking CancelAsync, SaveEstimateAsync).
201. Break `jpms/Features/Triage/Panels/Actions/StageTenderResponseAction.razor` (112 lines) into components. Component-sized blocks: lines 13–34 (22 lines, taking StageAsync).
202. Break `jpms/Features/Sales/SimpleMarkdown.razor` (111 lines) into components. No block was large enough to measure: read the view for its seams.
203. Break `jpms/Components/DrawingFolderPicker.razor` (109 lines) into components. Component-sized blocks: lines 13–42 (30 lines).
204. Break `jpms/Components/UpdateToast.razor` (109 lines) into components. Component-sized blocks: lines 25–53 (29 lines).
205. Break `jpms/Features/Labour/WeeklySignOffTable.razor` (109 lines) into components. Component-sized blocks: lines 10–54 (45 lines, taking PartLabel, WeekCell).
206. Break `jpms/Features/Procurement/DeleteWorkOrderModal.razor` (109 lines) into components. Component-sized blocks: lines 9–53 (45 lines, taking Close, DeleteAsync).
207. Break `jpms/Features/Procurement/TenderSubmissionModal.razor` (109 lines) into components. Component-sized blocks: lines 36–47 (12 lines); lines 71–99 (29 lines).
208. Break `jpms/Features/Triage/Panels/Actions/StageBuildingControlInspectionAction.razor` (109 lines) into components. Component-sized blocks: lines 12–52 (41 lines, taking Set).
209. Break `jpms/Features/Triage/Workspace/PanelWorkspace.razor` (109 lines) into components. Component-sized blocks: lines 13–30 (18 lines, taking ContentFor, WrapperClass).
210. Break `jpms/Components/ExportToExcelButton.razor` (107 lines) into components. Component-sized blocks: lines 13–27 (15 lines).
211. Break `jpms/Features/Labour/ChaseListPanel.razor` (106 lines) into components. Component-sized blocks: lines 7–66 (60 lines, taking StartDismiss, ConfirmDismissAsync).
212. Break `jpms/Features/Xero/Allocation/WorkOrderBillCard.razor` (106 lines) into components. Component-sized blocks: lines 7–76 (70 lines).
213. Break `jpms/Components/DrawingRevisionUploadForm.razor` (105 lines) into components. Component-sized blocks: lines 6–46 (41 lines, taking OnFileSelected, HandleUpload).
214. Break `jpms/Features/Procurement/TenderInviteComposerModal.razor` (105 lines) into components. Component-sized blocks: lines 14–105 (92 lines).
215. Break `jpms/Components/Modal.razor` (104 lines) into components. Component-sized blocks: lines 4–39 (36 lines, taking HandleOverlayClick).
216. Break `jpms/Features/Labour/WeekEntryModal.razor` (104 lines) into components. Component-sized blocks: lines 18–36 (19 lines); lines 37–95 (59 lines).
217. Break `jpms/Features/Progress/ContractorsReports/ContractorsReportPreview.razor` (104 lines) into components. Component-sized blocks: lines 8–23 (16 lines); lines 24–36 (13 lines); lines 48–62 (15 lines).
218. Break `jpms/Components/OpenRequestsPanel.razor` (103 lines) into components. Component-sized blocks: lines 15–73 (59 lines, taking Href).
219. Break `jpms/Components/ValuationClaimCorrespondenceSection.razor` (103 lines) into components. Component-sized blocks: lines 10–59 (50 lines).
220. Break `jpms/Features/Triage/Queue/ReplyComposerForm.razor` (103 lines) into components. Component-sized blocks: lines 10–65 (56 lines).
221. Break `jpms/Features/WeeklyCashflow/SupplierGroupsModal.razor` (103 lines) into components. Component-sized blocks: lines 12–41 (30 lines); lines 44–81 (38 lines).
222. Break `jpms/Pages/SalesEstimateDetail.razor` (103 lines) into components. Component-sized blocks: lines 24–103 (80 lines).
223. Break `jpms/Features/Triage/Queue/QueueInboxList.razor` (102 lines) into components. Component-sized blocks: lines 16–65 (50 lines, taking SortLinkClass).
224. Break `jpms/Features/Xero/SplitEditorForm.razor` (102 lines) into components. Component-sized blocks: lines 8–57 (50 lines, taking SetAmount).
225. Break `jpms/Components/CostCentreWorkOrdersModal.razor` (101 lines) into components. Component-sized blocks: lines 6–70 (65 lines).
226. Break `jpms/Components/Icons/ActionIcon.razor` (101 lines) into components. No block was large enough to measure: read the view for its seams.

**Pass 2 — Widget adoption**

227. Build, then adopt, `Button` where its markup is written by hand. 302 places in 169 files: jpms/Components/ApprovedUserRow.razor, jpms/Components/ApprovedUsersPanel.razor, jpms/Components/ClaimProgressDialog.razor, jpms/Components/ConversationComposer.razor +165. Each is listed with its line in audit.json under details.siteDefinition.offenders.handRolled; the route it reaches is in tools/refactor/site-definition.md.
228. Adopt `FormField` where its markup is written by hand. 144 places in 96 files: jpms/Components/ApprovedUserRow.razor, jpms/Components/ClaimProgressDialog.razor, jpms/Components/ConversationComposer.razor, jpms/Components/DirectoryContactForm.razor +92. Each is listed with its line in audit.json under details.siteDefinition.offenders.handRolled; the route it reaches is in tools/refactor/site-definition.md.
229. Adopt `SectionHeader` where its markup is written by hand. 126 places in 88 files: jpms/Components/AdminKpiPanel.razor, jpms/Components/ApprovedUsersPanel.razor, jpms/Components/ClientDefectForm.razor, jpms/Components/CostCentreReconciliationModal.razor +84. Each is listed with its line in audit.json under details.siteDefinition.offenders.handRolled; the route it reaches is in tools/refactor/site-definition.md.
230. Adopt `RecordsTable` where its markup is written by hand. 67 places in 57 files: jpms/Components/ClientCostReferencesModal.razor, jpms/Components/CostCentreCostOfSalesModal.razor, jpms/Components/CostCentreReconciliationModal.razor, jpms/Components/CostCentreSalesLinesModal.razor +53. Each is listed with its line in audit.json under details.siteDefinition.offenders.handRolled; the route it reaches is in tools/refactor/site-definition.md.
231. Build, then adopt, `KeyValueList` where its markup is written by hand. 17 places in 17 files: jpms/Components/DrawingExtractionPanel.razor, jpms/Components/ProjectContractPanel.razor, jpms/Components/PurchaseOrderSheet.razor, jpms/Components/RecordEmailPreviewPanel.razor +13. Each is listed with its line in audit.json under details.siteDefinition.offenders.handRolled; the route it reaches is in tools/refactor/site-definition.md.
232. Adopt `PageHeader` where its markup is written by hand. 13 places in 10 files: jpms/App.razor, jpms/Components/ProjectDetailView.razor, jpms/Components/PurchaseOrderSheet.razor, jpms/Components/RequestAccessView.razor +6. Each is listed with its line in audit.json under details.siteDefinition.offenders.handRolled; the route it reaches is in tools/refactor/site-definition.md.

**Pass 3 — Utility function identification**

233. Give `GetProgrammeEmailDetailEndpoint / GetProjectValuationInvoiceSummaryEndpoint / GetTodoItemByIdEndpoint / GetVariationOrderByIdEndpoint / GetVoqByRequestEndpoint / ListBidPackageEmailsEndpoint / ListBidPackageLineItemsEndpoint / ListBidPackageRecipientsEndpoint / ListBidPackagesForProjectEndpoint / ListCompanyContactsEndpoint / ListComplianceDocumentsForSubcontractorEndpoint / ListCurrentComplianceDocumentsEndpoint / ListQuotesForBidPackageEndpoint / ListRecordEmailsEndpoint / ListSchedulingEmailsEndpoint / ListSitePhotosEndpoint / ListSubcontractorsEndpoint / ListTodoActivityEndpoint / ListTodoEmailsEndpoint / ListTradesEndpoint / ListUnfiledRepliesEndpoint / ListValuationInvoicesForProjectEndpoint / ListVariationOrdersForProjectEndpoint / MatchSitePhotosEndpoint / SearchLocalSubcontractorsEndpoint` one home. The same 5-line function, word for word, is in 25 files: api/Features/Procurement/Queries/ListBidPackageEmailsEndpoint.cs, api/Features/Procurement/Queries/ListBidPackageLineItemsEndpoint.cs, api/Features/Procurement/Queries/ListBidPackageRecipientsEndpoint.cs, api/Features/Procurement/Queries/ListBidPackagesForProjectEndpoint.cs.
234. Give `EnsureContainerAsync` one home. The same 17-line function, word for word, is in 7 files: api/Features/ArchitectInstructions/Storage/ArchitectInstructionBlobStore.cs, api/Features/DocumentControl/Storage/AzureBlobDocumentControlStore.cs, api/Features/Drawings/Storage/AzureBlobDrawingStore.cs, api/Features/MailboxIntake/Sharing/AzureBlobEmailFileShareStore.cs.
235. Give `SectionHeading` one home. The same 13-line function, word for word, is in 5 files: api/Features/Commercial/Documents/CostCentreReconciliationRenderer.Helpers.cs, api/Features/Commercial/Documents/ValuationStatementRenderer.Helpers.cs, api/Features/Procurement/Documents/WorkOrderPoRenderer.Helpers.cs, api/Features/Progress/Documents/ProgressReportRenderer.cs.
236. Give `LoadSelectedAsync` one home. The same 31-line function, word for word, is in 2 files: jpms/Pages/CashForecast.razor.cs, jpms/Pages/ProfitSummary.razor.cs.
237. Give `AddGridRow` one home. The same 10-line function, word for word, is in 6 files: api/Features/Commercial/Documents/CostCentreReconciliationRenderer.Helpers.cs, api/Features/Commercial/Documents/ValuationStatementRenderer.Helpers.cs, api/Features/Procurement/Documents/WorkOrderPoRenderer.Helpers.cs, api/Features/Progress/Documents/ProgressReportRenderer.cs.
238. Give `AttachProjectContractDocumentHandler / BuildingControlAttachmentWriter / DeleteArchitectInstructionHandler / DeleteDrawingHandler / DeleteDrawingRevisionHandler / RemoveBidPackageAttachmentHandler / RemoveBuildingControlAttachmentHandler / RemoveProjectContractAmendmentHandler / RemoveRequestAttachmentHandler / RemoveWorkOrderAttachmentHandler` one home. The same 5-line function, word for word, is in 10 files: api/Features/ArchitectInstructions/ArchitectInstructionHandlers.cs, api/Features/BuildingControl/Attachments/BuildingControlAttachmentHandlers.cs, api/Features/BuildingControl/Attachments/BuildingControlAttachmentWriter.cs, api/Features/Drawings/Commands/DeleteDrawingHandler.cs.
239. Give `BluebeamStatusStore / HttpClientCostReferenceStore / HttpClientPortalStore / HttpCorrespondenceStore / HttpDocumentControlStore / HttpTodoStore / HttpUsefulInformationStore / HttpValuationInvoiceStore / HttpVariationStore` one home. The same 5-line function, word for word, is in 9 files: jpms/Services/BluebeamStatusStore.cs, jpms/Services/HttpClientCostReferenceStore.cs, jpms/Services/HttpClientPortalStore.cs, jpms/Services/HttpCorrespondenceStore.cs.
240. Give `HttpArchitectInstructionStore / HttpBidPackageAttachmentStore / HttpIntakeQueue / HttpProjectContractStore / HttpRequestAttachmentStore / HttpSitePhotoStore / HttpWorkOrderAttachmentStore` one home. The same 6-line function, word for word, is in 7 files: jpms/Features/Progress/SitePhotos/HttpSitePhotoStore.cs, jpms/Services/ArchitectInstructionStore.cs, jpms/Services/BidPackageAttachmentStore.cs, jpms/Services/HttpIntakeQueue.cs.
241. Give `CommitToBudgetAsync` one home. The same 19-line function, word for word, is in 2 files: api/Features/Variations/Commands/ApproveVariationOrderHandler.cs, api/Features/Variations/Commands/ReviseVariationOrderLinesHandler.cs.
242. Give `AzureBlobDrawingStore / AzureBlobProgressPhotoStore` one home. The same 18-line function, word for word, is in 2 files: api/Features/Drawings/Storage/AzureBlobDrawingStore.cs, api/Features/Progress/Storage/AzureBlobProgressPhotoStore.cs.
243. Give `Parse / ParseQuery` one home. The same 12-line function, word for word, is in 3 files: jpms/Pages/AdminIntegrations.razor, jpms/Pages/ConnectAuthorize.razor, worker/Bluebeam/BluebeamConnectCallback.cs.
244. Give `DeleteAsync` one home. The same 5-line function, word for word, is in 7 files: api/Features/ArchitectInstructions/Storage/ArchitectInstructionBlobStore.cs, api/Features/BuildingControl/Attachments/AzureBlobBuildingControlAttachmentStore.cs, api/Features/DocumentControl/Storage/AzureBlobDocumentControlStore.cs, api/Features/Drawings/Storage/AzureBlobDrawingStore.cs.
245. Give `ContractorsReportPhotoLoader / DeleteProgressPhotoHandler / DeleteProgressUpdateHandler / DeleteSitePhotoHandler / FileSitePhotosHandler / ProgressPhotoIntake / SitePhotoIntake` one home. The same 5-line function, word for word, is in 7 files: api/Features/Progress/Commands/DeleteProgressPhotoHandler.cs, api/Features/Progress/Commands/DeleteProgressUpdateHandler.cs, api/Features/Progress/ContractorsReports/Documents/ContractorsReportPhotoLoader.cs, api/Features/Progress/Photos/ProgressPhotoIntake.cs.
246. Give `LoadAsync` one home. The same 5-line function, word for word, is in 7 files: jpms/Services/HttpArchitectStore.cs, jpms/Services/HttpClientStore.cs, jpms/Services/HttpXeroAgedPayablesStore.cs, jpms/Services/HttpXeroAgedReceivablesStore.cs.
247. Give `DeleteDirectoryUserHandler / InviteDirectoryWriter / RemoveDirectoryUserHandler / RestoreDirectoryUserHandler / SessionManager / UpsertDirectoryUserHandler` one home. The same 5-line function, word for word, is in 6 files: api/Auth/SessionManager.cs, api/Features/Auth/InviteDirectoryWriter.cs, api/Features/Directory/Commands/DeleteDirectoryUserHandler.cs, api/Features/Directory/Commands/RemoveDirectoryUserHandler.cs.
248. Remove the 27 components and functions nothing calls. Listed in audit.json under details.orphans and details.inventory.offenders.orphans; confirm each has no caller before it goes.

**Pass 4 — Design pattern identification**

249. Complete the pattern: every handler has a endpoint. 48 of 461 lack it. Predicted: api/Features/Subcontractors/Commands/AddComplianceDocumentVersionEndpoint.cs; api/Features/Kpi/Commands/AddKpiPersonEndpoint.cs; api/Features/Xero/Ledger/AllocateSuggestedXeroLinesEndpoint.cs; api/Features/ProjectContracts/Commands/AttachProjectContractAmendmentEndpoint.cs. Find the code doing that job now and move it there; a subject that truly has no such job goes in acceptedGaps.
250. Complete the pattern: every authorisation has a handler. 13 of 235 lack it. Predicted: api/Features/Progress/ContractorsReports/Commands/ContractorsReportHandler.cs; api/Features/Drawings/Commands/DrawingFolderCommandHandler.cs; api/Features/Clients/Commands/InviteClientPortalUserHandler.cs; api/Features/Subcontractors/Commands/InviteSubcontractorPortalUserHandler.cs. Find the code doing that job now and move it there; a subject that truly has no such job goes in acceptedGaps.
251. Complete the pattern: every authorisation has a endpoint. 18 of 235 lack it. Predicted: api/Features/ProjectContracts/Commands/AttachProjectContractAmendmentEndpoint.cs; api/Features/ProjectContracts/Commands/AttachProjectContractDocumentEndpoint.cs; api/Features/Progress/ContractorsReports/Commands/ContractorsReportEndpoint.cs; api/Features/Drawings/Commands/DrawingFolderCommandEndpoint.cs. Find the code doing that job now and move it there; a subject that truly has no such job goes in acceptedGaps.
252. Complete the pattern: every authorisation has a validation. 22 of 235 lack it. Predicted: api/Features/WeeklyCashflow/Commands/ArchiveWeeklyCashflowItemValidation.cs; api/Features/Calendar/Commands/DeleteCalendarEventValidation.cs; api/Features/Progress/Commands/DeleteProgressPhotoValidation.cs; api/Features/Progress/Commands/DeleteProgressReportValidation.cs. Find the code doing that job now and move it there; a subject that truly has no such job goes in acceptedGaps.
253. Complete the pattern: every endpoint has a handler. 49 of 462 lack it. Predicted: api/Features/Portal/Commands/AcceptMyWorkOrderHandler.cs; api/Features/Progress/WhatsApp/ApplyWhatsAppWeekHandler.cs; api/Features/Connect/ApproveAuthorizationHandler.cs; api/Features/Connect/AuthorizeHandler.cs. Find the code doing that job now and move it there; a subject that truly has no such job goes in acceptedGaps.
254. Complete the pattern: every validation has a handler. 3 of 232 lack it. Predicted: api/Features/Progress/ContractorsReports/Commands/ContractorsReportHandler.cs; api/Features/Drawings/Commands/DrawingFolderCommandHandler.cs; api/Features/Xero/Ledger/Allocation/SetXeroAllocationHandler.AllocateHandler.cs. Find the code doing that job now and move it there; a subject that truly has no such job goes in acceptedGaps.
255. Complete the pattern: every validation has a authorisation. 19 of 232 lack it. Predicted: api/Features/Commercial/Commands/AddValuationLineItemAuthorisation.cs; api/Features/Site/Commands/ApplyProgrammeDraftAuthorisation.cs; api/Features/Site/Commands/DiscardProgrammeDraftAuthorisation.cs; api/Features/Site/Commands/DraftProgrammeFromValuationAuthorisation.cs. Find the code doing that job now and move it there; a subject that truly has no such job goes in acceptedGaps.
256. Complete the pattern: every validation has a endpoint. 12 of 232 lack it. Predicted: api/Features/ProjectContracts/Commands/AttachProjectContractAmendmentEndpoint.cs; api/Features/ProjectContracts/Commands/AttachProjectContractDocumentEndpoint.cs; api/Features/Progress/ContractorsReports/Commands/ContractorsReportEndpoint.cs; api/Features/Drawings/Commands/DrawingFolderCommandEndpoint.cs. Find the code doing that job now and move it there; a subject that truly has no such job goes in acceptedGaps.
257. Complete the pattern: every queries has a commands. 1 of 5 lack it. Predicted: api/Features/Subcontractors/SubcontractorTradeCommands.cs. Find the code doing that job now and move it there; a subject that truly has no such job goes in acceptedGaps.
258. Complete the pattern: every lifecycle has a razor. 1 of 4 lack it. Predicted: api/Features/Requests/Commands/RequestRazor.cs. Find the code doing that job now and move it there; a subject that truly has no such job goes in acceptedGaps.
259. Complete the pattern: every settlement has a razor. 1 of 4 lack it. Predicted: contracts/Closeout/AgreeRazor.cs. Find the code doing that job now and move it there; a subject that truly has no such job goes in acceptedGaps.
260. Complete the pattern: every export has a razor. 1 of 13 lack it. Predicted: contracts/Commercial/Export/ValuationStatementRazor.cs. Find the code doing that job now and move it there; a subject that truly has no such job goes in acceptedGaps.
261. Divide `api/Data/JpmsContext.Model.cs` (553 lines) into the units its pattern names. 1 functions; the longest is 532 lines.
262. Divide `api/Features/Sales/Documents/EstimateDocumentRenderer.cs` (507 lines) into the units its pattern names. 19 functions; the longest is 73 lines.
263. Divide `jpms/Services/Navigation/SidebarFolders.cs` (452 lines) into the units its pattern names. 1 functions; the longest is 408 lines.
264. Divide `jpms/Services/HttpLabourStore.cs` (436 lines) into the units its pattern names. 46 functions; the longest is 28 lines.
265. Divide `jpms/Components/ManualWorkOrderModal.razor.cs` (428 lines) into the units its pattern names. 13 functions; the longest is 137 lines.
266. Divide `jpms/Pages/DocumentControl.Filing.cs` (409 lines) into the units its pattern names. 22 functions; the longest is 31 lines.
267. Divide `api/Features/Ai/Tools/AiValuationInvoiceTools.cs` (405 lines) into the units its pattern names. 3 functions; the longest is 260 lines.
268. Divide `api/Features/Ai/Sources/AiFiledDocuments.cs` (394 lines) into the units its pattern names. 7 functions; the longest is 149 lines.
269. Divide `api/Features/Procurement/Commands/ExtractTenderFromMessageHandler.cs` (381 lines) into the units its pattern names. 6 functions; the longest is 129 lines.
270. Divide `api/Features/Todos/TodoBrief.cs` (379 lines) into the units its pattern names. 16 functions; the longest is 67 lines.
271. Divide `api/Features/Requests/RequestContextAssembler.cs` (375 lines) into the units its pattern names. 8 functions; the longest is 76 lines.
272. Divide `api/Features/Xero/XeroClient.Http.cs` (375 lines) into the units its pattern names. 12 functions; the longest is 49 lines.
273. Divide `api/Features/Xero/XeroClient.Reads.cs` (372 lines) into the units its pattern names. 9 functions; the longest is 47 lines.
274. Divide `api/Features/Ai/Tools/AiLabourMonthEndTools.cs` (371 lines) into the units its pattern names. 1 functions; the longest is 348 lines.
275. Divide `api/Features/MailboxIntake/Graph/MailboxGraphClient.Drafts.cs` (370 lines) into the units its pattern names. 11 functions; the longest is 90 lines.
276. Divide `jpms/Pages/TriageQueue.StagedCreate.cs` (369 lines) into the units its pattern names. 2 functions; the longest is 191 lines.
277. Divide `jpms/Pages/ProjectCommunications.razor.cs` (366 lines) into the units its pattern names. 18 functions; the longest is 39 lines.
278. Divide `api/Data/Entities/ProcurementEntities.cs` (360 lines) into the units its pattern names. 0 functions; the longest is 0 lines.
279. Divide `api/Features/Ai/Tools/AiWriteTools.cs` (358 lines) into the units its pattern names. 2 functions; the longest is 313 lines.
280. Divide `jpms/Pages/TriageQueue.Compose.cs` (356 lines) into the units its pattern names. 11 functions; the longest is 28 lines.
281. Divide `api/Features/Commercial/Documents/ValuationStatementRenderer.Sections.cs` (352 lines) into the units its pattern names. 13 functions; the longest is 54 lines.
282. Divide `api/Features/Sales/SalesEndpoints.cs` (352 lines) into the units its pattern names. 17 functions; the longest is 32 lines.
283. Divide `api/Features/Ai/Tools/AiMailboxTools.cs` (351 lines) into the units its pattern names. 4 functions; the longest is 268 lines.
284. Divide `api/Features/Ai/Tools/AiToolCatalogue.Lookup.cs` (351 lines) into the units its pattern names. 1 functions; the longest is 338 lines.
285. Divide `api/Features/Ai/Tools/Actions/SubcontractorsAndLeadsActions.Subcontractors.cs` (350 lines) into the units its pattern names. 1 functions; the longest is 331 lines.
286. Divide `jpms/Features/Triage/TriageStaging.cs` (350 lines) into the units its pattern names. 1 functions; the longest is 6 lines.
287. Divide `jpms/Components/DrawingUploadForm.razor.cs` (346 lines) into the units its pattern names. 10 functions; the longest is 80 lines.
288. Divide `api/Features/MailboxIntake/Graph/IntakeMessageReader.cs` (345 lines) into the units its pattern names. 7 functions; the longest is 110 lines.
289. Divide `api/Features/Xero/Ledger/XeroWriteBackService.cs` (342 lines) into the units its pattern names. 8 functions; the longest is 117 lines.
290. Divide `api/Features/ArchitectInstructions/ArchitectInstructionHandlers.cs` (340 lines) into the units its pattern names. 11 functions; the longest is 52 lines.
291. Divide `contracts/Xero/XeroLedger.cs` (337 lines) into the units its pattern names. 1 functions; the longest is 54 lines.
292. Divide `jpms/Components/FinancialsTable.razor.cs` (337 lines) into the units its pattern names. 11 functions; the longest is 103 lines.
293. Divide `api/Features/Commercial/Queries/GetProjectFinancialSummaryHandler.cs` (331 lines) into the units its pattern names. 1 functions; the longest is 319 lines.
294. Divide `jpms/Pages/ProjectWorkOrderAllocation.razor.cs` (331 lines) into the units its pattern names. 14 functions; the longest is 56 lines.
295. Divide `jpms/Features/Triage/MailReplyComposer.razor.cs` (323 lines) into the units its pattern names. 11 functions; the longest is 48 lines.
296. Divide `jpms/Pages/TriageQueue.Parking.cs` (322 lines) into the units its pattern names. 9 functions; the longest is 39 lines.
297. Divide `api/Data/Entities/CommercialEntities.cs` (319 lines) into the units its pattern names. 0 functions; the longest is 0 lines.
298. Divide `api/Features/Labour/Commands/WorkerLinkSlices.cs` (319 lines) into the units its pattern names. 19 functions; the longest is 57 lines.
299. Divide `api/Features/Subcontractors/Documents/SubcontractorStatementRenderer.cs` (319 lines) into the units its pattern names. 13 functions; the longest is 110 lines.
300. Divide `contracts/Ai/PageGuides/OfficePageGuides.cs` (314 lines) into the units its pattern names. 0 functions; the longest is 0 lines.
301. Divide `api/Features/Ai/Tools/AiRegisterTools.cs` (312 lines) into the units its pattern names. 1 functions; the longest is 275 lines.
302. Divide `api/Features/ArchitectInstructions/ArchitectInstructionEndpoints.cs` (312 lines) into the units its pattern names. 10 functions; the longest is 25 lines.
303. Divide `api/Features/Requests/Documents/RequestDocumentRenderer.Sections.cs` (312 lines) into the units its pattern names. 11 functions; the longest is 55 lines.
304. Divide `jpms/Pages/SubcontractorDetail.razor.cs` (311 lines) into the units its pattern names. 19 functions; the longest is 19 lines.
305. Divide `jpms/Pages/Workers.razor.cs` (310 lines) into the units its pattern names. 15 functions; the longest is 36 lines.
306. Divide `api/Features/MailboxIntake/Graph/MailboxGraphClient.Tagging.cs` (309 lines) into the units its pattern names. 10 functions; the longest is 60 lines.
307. Divide `api/Features/Labour/Commands/WorkerWeekApprovalByNameSlices.cs` (307 lines) into the units its pattern names. 14 functions; the longest is 50 lines.
308. Divide `api/Features/Documents/JewelDocumentStyle.cs` (306 lines) into the units its pattern names. 13 functions; the longest is 52 lines.
309. Divide `api/Features/Ai/Tools/AiRecordTools.Correspondence.cs` (305 lines) into the units its pattern names. 1 functions; the longest is 293 lines.
310. Divide `api/Features/Ai/Tools/AiToolCatalogue.TodoBrief.cs` (303 lines) into the units its pattern names. 5 functions; the longest is 203 lines.
311. Divide `jpms/Pages/ProjectRequestDetail.Menus.cs` (302 lines) into the units its pattern names. 12 functions; the longest is 32 lines.
312. Divide `api/Features/Progress/Documents/ProgressReportRenderer.cs` (301 lines) into the units its pattern names. 15 functions; the longest is 42 lines.
313. Divide `contracts/WeeklyCashflow/WeeklyCashflowMaths.cs` (299 lines) into the units its pattern names. 8 functions; the longest is 82 lines.
314. Divide `jpms/Components/RoleHome.razor.cs` (299 lines) into the units its pattern names. 7 functions; the longest is 23 lines.
315. Divide `api/Features/Xero/XeroClient.SitePnl.cs` (298 lines) into the units its pattern names. 9 functions; the longest is 71 lines.
316. Divide `jpms/Pages/CostCodes.razor.cs` (298 lines) into the units its pattern names. 17 functions; the longest is 44 lines.
317. Divide `api/Features/Ai/Tools/Actions/VariationsAndValuationsActions.Variations.cs` (297 lines) into the units its pattern names. 1 functions; the longest is 278 lines.
318. Divide `api/Features/Ai/Tools/Actions/VariationsAndValuationsActions.ValuationInvoices.cs` (296 lines) into the units its pattern names. 1 functions; the longest is 275 lines.
319. Divide `contracts/Commercial/CashForecastPhasing.cs` (296 lines) into the units its pattern names. 7 functions; the longest is 52 lines.
320. Divide `jpms/Features/Procurement/ProcurementRouteRegistration.cs` (295 lines) into the units its pattern names. 2 functions; the longest is 282 lines.
321. Divide `api/Features/Ai/Tools/AiSalesTools.cs` (294 lines) into the units its pattern names. 5 functions; the longest is 186 lines.
322. Divide `api/Features/Mcp/McpEndpoint.cs` (294 lines) into the units its pattern names. 10 functions; the longest is 65 lines.
323. Divide `jpms/Pages/ProjectValuation.razor.cs` (294 lines) into the units its pattern names. 8 functions; the longest is 107 lines.
324. Divide `jpms/Pages/TriageQueue.razor.cs` (294 lines) into the units its pattern names. 2 functions; the longest is 27 lines.
325. Divide `api/Features/Procurement/Commands/SuggestBidPackagesHandler.cs` (293 lines) into the units its pattern names. 5 functions; the longest is 86 lines.
326. Divide `jpms/Pages/ProjectRequestDetail.DraftVariation.cs` (292 lines) into the units its pattern names. 10 functions; the longest is 68 lines.
327. Divide `api/Features/Sales/Commands/LeadHandlers.cs` (291 lines) into the units its pattern names. 9 functions; the longest is 55 lines.
328. Divide `api/Features/Ai/Tools/Actions/LabourAndBackOfficeActions.MonthEnd.cs` (285 lines) into the units its pattern names. 1 functions; the longest is 271 lines.
329. Divide `contracts/Ai/PageGuides/SitePageGuides.cs` (282 lines) into the units its pattern names. 0 functions; the longest is 0 lines.
330. Divide `jpms/Components/ProjectTodoList.razor.cs` (276 lines) into the units its pattern names. 17 functions; the longest is 20 lines.
331. Divide `jpms/Pages/TriageQueue.ProjectMatch.cs` (275 lines) into the units its pattern names. 12 functions; the longest is 48 lines.
332. Divide `api/Features/RecordLinks/BackfillBucketsEndpoint.cs` (273 lines) into the units its pattern names. 6 functions; the longest is 155 lines.
333. Divide `jpms/Pages/ProjectBuildingControl.razor.cs` (273 lines) into the units its pattern names. 16 functions; the longest is 22 lines.
334. Divide `api/Features/Procurement/PricingScheduleWorkbook.cs` (271 lines) into the units its pattern names. 5 functions; the longest is 175 lines.
335. Divide `jpms/Components/ReconciliationPackageBuilderModal.razor.cs` (271 lines) into the units its pattern names. 9 functions; the longest is 41 lines.
336. Divide `jpms/Features/Triage/Panels/Actions/StagedRecordActionEditor.razor.cs` (271 lines) into the units its pattern names. 12 functions; the longest is 29 lines.
337. Divide `jpms/Pages/AiSkillsAdmin.razor.cs` (271 lines) into the units its pattern names. 12 functions; the longest is 39 lines.
338. Divide `jpms/Pages/ProjectCalendar.razor.cs` (270 lines) into the units its pattern names. 18 functions; the longest is 31 lines.
339. Divide `jpms/Pages/Subcontractors.razor.cs` (267 lines) into the units its pattern names. 13 functions; the longest is 21 lines.
340. Divide `jpms/Pages/XeroAllocation.Export.cs` (266 lines) into the units its pattern names. 11 functions; the longest is 117 lines.
341. Divide `api/Features/Xero/Ledger/SyncXeroLedgerHandler.cs` (264 lines) into the units its pattern names. 4 functions; the longest is 198 lines.
342. Divide `jpms/Features/Triage/AttachmentPicker.Drawings.cs` (264 lines) into the units its pattern names. 16 functions; the longest is 55 lines.
343. Divide `api/Data/JpmsContext.cs` (263 lines) into the units its pattern names. 2 functions; the longest is 4 lines.
344. Divide `jpms/Pages/XeroAllocation.SendTo.cs` (263 lines) into the units its pattern names. 14 functions; the longest is 34 lines.
345. Divide `jpms/Pages/ProjectDrawings.razor.cs` (261 lines) into the units its pattern names. 17 functions; the longest is 34 lines.
346. Divide `jpms/Pages/TriageQueue.Attachments.cs` (261 lines) into the units its pattern names. 10 functions; the longest is 34 lines.
347. Divide `api/Features/Procurement/ProcurementFeatureRegistration.cs` (260 lines) into the units its pattern names. 4 functions; the longest is 202 lines.
348. Divide `api/Features/ValuationInvoices/XeroPayments/ValuationInvoicePaymentSyncPlanner.cs` (259 lines) into the units its pattern names. 12 functions; the longest is 36 lines.
349. Divide `api/Features/Xero/XeroClient.ReadsSuppliers.cs` (259 lines) into the units its pattern names. 9 functions; the longest is 61 lines.
350. Divide `jpms/Pages/XeroTransactions.razor.cs` (259 lines) into the units its pattern names. 9 functions; the longest is 69 lines.
351. Divide `api/Features/Ai/Tools/Actions/CommercialActions.ClaimsAndValuations.cs` (255 lines) into the units its pattern names. 1 functions; the longest is 241 lines.
352. Divide `api/Features/Ai/Tools/Actions/ProcurementActions.WorkOrders.cs` (255 lines) into the units its pattern names. 1 functions; the longest is 247 lines.
353. Divide `api/Features/Labour/Queries/GetLabourOverviewSlice.cs` (254 lines) into the units its pattern names. 3 functions; the longest is 217 lines.
354. Divide `api/Features/MailboxIntake/Graph/IMailboxGraphClient.cs` (253 lines) into the units its pattern names. 1 functions; the longest is 15 lines.
355. Divide `api/Features/Ai/Tools/AiFinanceTools.cs` (252 lines) into the units its pattern names. 1 functions; the longest is 222 lines.
356. Divide `api/Features/Variations/Commands/ApproveVariationOrderHandler.cs` (252 lines) into the units its pattern names. 3 functions; the longest is 182 lines.
357. Divide `jpms/Pages/ProjectCashflow.razor.cs` (249 lines) into the units its pattern names. 2 functions; the longest is 41 lines.
358. Divide `api/Features/Sales/Imagine/ImagineRenderRunner.cs` (248 lines) into the units its pattern names. 5 functions; the longest is 153 lines.
359. Divide `contracts/Ai/ModalCatalog.Variations.cs` (248 lines) into the units its pattern names. 0 functions; the longest is 0 lines.
360. Divide `api/Features/Xero/Ledger/WorkOrderBillRecognition.Rules.cs` (247 lines) into the units its pattern names. 10 functions; the longest is 31 lines.
361. Divide `api/Features/Ai/Tools/Actions/SiteAndProgressActions.ProgressProgramme.cs` (246 lines) into the units its pattern names. 1 functions; the longest is 227 lines.
362. Divide `jpms/Pages/ProfitSummary.Running.cs` (246 lines) into the units its pattern names. 5 functions; the longest is 108 lines.
363. Divide `api/Features/Ai/Tools/Actions/SalesActions.cs` (245 lines) into the units its pattern names. 1 functions; the longest is 216 lines.
364. Divide `api/Features/Variations/Commands/ReviseVariationOrderLinesHandler.cs` (245 lines) into the units its pattern names. 3 functions; the longest is 186 lines.
365. Divide `jpms/Pages/ProjectRequestDetail.razor.cs` (245 lines) into the units its pattern names. 10 functions; the longest is 26 lines.
366. Divide `api/Features/Commercial/ValuationStatementLines.cs` (244 lines) into the units its pattern names. 6 functions; the longest is 85 lines.
367. Divide `contracts/Ai/ModalCatalog.Procurement.cs` (244 lines) into the units its pattern names. 0 functions; the longest is 0 lines.
368. Divide `jpms/Services/HttpSubcontractorStore.cs` (244 lines) into the units its pattern names. 20 functions; the longest is 28 lines.
369. Divide `api/Features/Xero/Ledger/XeroLedgerEndpoints.cs` (243 lines) into the units its pattern names. 15 functions; the longest is 25 lines.
370. Divide `api/Features/Commercial/Commands/SaveReconciliationPackageHandler.cs` (242 lines) into the units its pattern names. 1 functions; the longest is 220 lines.
371. Divide `api/Features/Labour/Commands/MonthEndByNameSlices.cs` (241 lines) into the units its pattern names. 21 functions; the longest is 21 lines.
372. Divide `api/Features/Procurement/Commands/BidPackageInviteMailAssembler.cs` (241 lines) into the units its pattern names. 6 functions; the longest is 49 lines.
373. Divide `api/Features/Requests/Documents/DocumentBranding.cs` (241 lines) into the units its pattern names. 1 functions; the longest is 8 lines.
374. Divide `jpms/Features/Requests/RequestsRouteRegistration.cs` (241 lines) into the units its pattern names. 3 functions; the longest is 217 lines.
375. Divide `api/Features/Labour/Commands/SettlementLineAndMappingSlices.cs` (240 lines) into the units its pattern names. 15 functions; the longest is 24 lines.
376. Divide `api/Features/Requests/MailboxTriageEndpoints.cs` (237 lines) into the units its pattern names. 15 functions; the longest is 31 lines.
377. Divide `jpms/Pages/ProjectDrawingDetail.razor.cs` (237 lines) into the units its pattern names. 9 functions; the longest is 30 lines.
378. Divide `jpms/Pages/ProjectValuation.Export.cs` (237 lines) into the units its pattern names. 10 functions; the longest is 77 lines.
379. Divide `jpms/Services/Navigation/DesktopNavigation.cs` (237 lines) into the units its pattern names. 5 functions; the longest is 31 lines.
380. Divide `api/Features/Places/LocalBusinessSearch.cs` (236 lines) into the units its pattern names. 7 functions; the longest is 61 lines.
381. Divide `api/Features/Sales/Commands/SalesGates.cs` (236 lines) into the units its pattern names. 14 functions; the longest is 18 lines.
382. Divide `jpms/Program.cs` (236 lines) into the units its pattern names. 0 functions; the longest is 0 lines.
383. Divide `api/Features/Xero/IXeroClient.cs` (235 lines) into the units its pattern names. 0 functions; the longest is 0 lines.
384. Divide `jpms/Components/StatusTones.cs` (235 lines) into the units its pattern names. 25 functions; the longest is 9 lines.
385. Divide `jpms/Pages/ProjectRequests.TabsAndSearch.cs` (234 lines) into the units its pattern names. 10 functions; the longest is 35 lines.
386. Divide `api/Features/Sales/Commands/ProposalHandlers.cs` (233 lines) into the units its pattern names. 7 functions; the longest is 53 lines.
387. Divide `api/Features/Xero/XeroApproval.cs` (233 lines) into the units its pattern names. 5 functions; the longest is 97 lines.
388. Divide `contracts/Models/HsAuditTemplate.cs` (231 lines) into the units its pattern names. 1 functions; the longest is 3 lines.
389. Divide `jpms/Features/Site/Programme/ProgrammeWorkbench.razor.cs` (231 lines) into the units its pattern names. 10 functions; the longest is 25 lines.
390. Divide `jpms/Components/RequestForm.razor.cs` (230 lines) into the units its pattern names. 12 functions; the longest is 48 lines.
391. Divide `jpms/Services/HttpDrawingStore.cs` (229 lines) into the units its pattern names. 20 functions; the longest is 26 lines.
392. Divide `jpms/Layout/SideNav.razor.cs` (228 lines) into the units its pattern names. 11 functions; the longest is 32 lines.
393. Divide `api/Features/Subcontractors/Commands/ConsolidateDirectoryRecordsHandler.cs` (227 lines) into the units its pattern names. 4 functions; the longest is 82 lines.
394. Divide `contracts/Models/Procurement.cs` (227 lines) into the units its pattern names. 4 functions; the longest is 66 lines.
395. Divide `jpms/Pages/AuditTrail.razor.cs` (227 lines) into the units its pattern names. 10 functions; the longest is 41 lines.
396. Divide `jpms/Pages/ProjectFinancials.razor.cs` (227 lines) into the units its pattern names. 9 functions; the longest is 27 lines.
397. Divide `jpms/Pages/XeroAllocation.razor.cs` (227 lines) into the units its pattern names. 5 functions; the longest is 15 lines.
398. Divide `api/Features/Todos/Commands/CreateTodoItemsFromMessageHandler.cs` (222 lines) into the units its pattern names. 3 functions; the longest is 187 lines.
399. Divide `api/Features/Ai/Tools/Actions/LabourAndBackOfficeActions.Timesheets.cs` (221 lines) into the units its pattern names. 1 functions; the longest is 212 lines.
400. Divide `api/Features/Todos/TodoCompletionRecordTagger.cs` (221 lines) into the units its pattern names. 5 functions; the longest is 70 lines.
401. Divide `jpms/Pages/ProfitSummary.ExportGrid.cs` (221 lines) into the units its pattern names. 6 functions; the longest is 61 lines.
402. Divide `jpms/Pages/ProjectBuildingControlInspection.razor.cs` (221 lines) into the units its pattern names. 14 functions; the longest is 26 lines.
403. Divide `api/Features/Requests/Attachments/RequestAttachmentEndpoints.cs` (220 lines) into the units its pattern names. 6 functions; the longest is 17 lines.
404. Divide `jpms/Services/AuthService.cs` (220 lines) into the units its pattern names. 12 functions; the longest is 22 lines.
405. Divide `api/Features/Ai/Tools/AiCommercialTools.Variation.cs` (219 lines) into the units its pattern names. 2 functions; the longest is 204 lines.
406. Divide `contracts/Documents/Excel/ExcelStyleRegistry.cs` (219 lines) into the units its pattern names. 9 functions; the longest is 79 lines.
407. Divide `contracts/Models/BuildingControl.cs` (219 lines) into the units its pattern names. 5 functions; the longest is 66 lines.
408. Divide `jpms/Pages/XeroAllocation.RowCoding.cs` (219 lines) into the units its pattern names. 8 functions; the longest is 27 lines.
409. Divide `api/Features/Ai/Tools/Actions/CommercialActions.Financials.cs` (217 lines) into the units its pattern names. 1 functions; the longest is 203 lines.
410. Divide `api/Features/Requests/Commands/MailboxRequestCommandHandlers.cs` (217 lines) into the units its pattern names. 4 functions; the longest is 163 lines.
411. Divide `contracts/Models/Subcontractor.cs` (217 lines) into the units its pattern names. 6 functions; the longest is 61 lines.
412. Divide `jpms/Components/RequestConversation.razor.cs` (217 lines) into the units its pattern names. 11 functions; the longest is 35 lines.
413. Divide `jpms/Features/Commercial/CommercialRouteRegistration.cs` (217 lines) into the units its pattern names. 3 functions; the longest is 195 lines.
414. Divide `jpms/Pages/XeroAllocation.Settlement.cs` (217 lines) into the units its pattern names. 9 functions; the longest is 28 lines.
415. Divide `api/Features/Drawings/Geometry/DrawingTitleBlockReader.cs` (216 lines) into the units its pattern names. 8 functions; the longest is 34 lines.
416. Divide `contracts/Models/Request.cs` (216 lines) into the units its pattern names. 5 functions; the longest is 70 lines.
417. Divide `api/Features/Ai/Tools/Actions/SiteAndProgressActions.Drawings.cs` (214 lines) into the units its pattern names. 1 functions; the longest is 190 lines.
418. Divide `api/Features/Sales/Imagine/ImagineConceptWriter.cs` (214 lines) into the units its pattern names. 6 functions; the longest is 60 lines.
419. Divide `jpms/Pages/ProjectVariationDetail.Communications.cs` (214 lines) into the units its pattern names. 9 functions; the longest is 24 lines.
420. Divide `api/Features/BuildingControl/Attachments/BuildingControlAttachmentEndpoints.cs` (213 lines) into the units its pattern names. 9 functions; the longest is 47 lines.
421. Divide `jpms/Pages/ProjectBidPackageInvites.razor.cs` (212 lines) into the units its pattern names. 10 functions; the longest is 37 lines.
422. Divide `jpms/Components/CostCentreCostOfSalesModal.razor.cs` (211 lines) into the units its pattern names. 8 functions; the longest is 32 lines.
423. Divide `jpms/Components/CostCentreSalesLinesModal.razor.cs` (211 lines) into the units its pattern names. 5 functions; the longest is 43 lines.
424. Divide `jpms/Pages/CashForecast.razor.cs` (211 lines) into the units its pattern names. 6 functions; the longest is 38 lines.
425. Divide `api/Features/Ai/Tools/AiCommercialTools.Valuation.cs` (209 lines) into the units its pattern names. 2 functions; the longest is 189 lines.
426. Divide `api/Features/Xero/SitePnl/SyncXeroSitePnlHandler.cs` (209 lines) into the units its pattern names. 2 functions; the longest is 156 lines.
427. Divide `api/Features/MailboxIntake/Graph/MailboxGraphClient.Conversations.cs` (206 lines) into the units its pattern names. 6 functions; the longest is 56 lines.
428. Divide `jpms/Pages/ProjectVariationDetail.Status.cs` (206 lines) into the units its pattern names. 13 functions; the longest is 47 lines.
429. Divide `api/Features/Ai/Tools/Actions/ProjectsAndTendersActions.BuildingControl.cs` (204 lines) into the units its pattern names. 1 functions; the longest is 183 lines.
430. Divide `api/Features/Ai/Tools/AiActionGatewayTools.cs` (204 lines) into the units its pattern names. 2 functions; the longest is 159 lines.
431. Divide `jpms/Pages/ProfitSummary.Figures.cs` (204 lines) into the units its pattern names. 5 functions; the longest is 40 lines.
432. Divide `jpms/Pages/ProjectValuation.Invoices.cs` (204 lines) into the units its pattern names. 15 functions; the longest is 22 lines.
433. Divide `contracts/Models/ProgrammeCostCentreRules.cs` (203 lines) into the units its pattern names. 3 functions; the longest is 30 lines.
434. Divide `jpms/Features/Site/Programme/ProgrammeDraftReview.razor.cs` (203 lines) into the units its pattern names. 10 functions; the longest is 28 lines.
435. Divide `jpms/Pages/TriageQueue.TaggedSearch.cs` (203 lines) into the units its pattern names. 12 functions; the longest is 52 lines.
436. Divide `api/Features/Ai/Tools/AiToolCatalogue.Masters.cs` (202 lines) into the units its pattern names. 1 functions; the longest is 188 lines.
437. Divide `contracts/Models/ValuationReport.cs` (202 lines) into the units its pattern names. 5 functions; the longest is 43 lines.
438. Divide `jpms/Features/Procurement/TenderInviteComposerModal.razor.cs` (202 lines) into the units its pattern names. 6 functions; the longest is 34 lines.
439. Divide `jpms/Pages/TodoDetail.razor.cs` (201 lines) into the units its pattern names. 11 functions; the longest is 16 lines.
440. Divide `api/Features/Xero/XeroClient.LineItems.cs` (200 lines) into the units its pattern names. 4 functions; the longest is 83 lines.
441. Divide `jpms/Features/Triage/AttachmentPicker.razor.cs` (200 lines) into the units its pattern names. 9 functions; the longest is 23 lines.
442. Divide `jpms/Pages/ProjectArchitectInstructions.razor.cs` (200 lines) into the units its pattern names. 10 functions; the longest is 41 lines.
443. Divide `api/Features/Ai/AiAttachmentReader.cs` (199 lines) into the units its pattern names. 8 functions; the longest is 25 lines.
444. Divide `api/Features/Labour/LabourFeatureRegistration.cs` (199 lines) into the units its pattern names. 1 functions; the longest is 189 lines.
445. Divide `api/Features/Ai/ClaudeClient.cs` (198 lines) into the units its pattern names. 4 functions; the longest is 66 lines.
446. Divide `api/Features/Ai/Tools/AiRecordTools.Directory.cs` (198 lines) into the units its pattern names. 1 functions; the longest is 188 lines.
447. Divide `api/Features/Ai/Tools/AiToolCatalogue.SiteWork.cs` (197 lines) into the units its pattern names. 1 functions; the longest is 182 lines.
448. Divide `jpms/Features/Triage/Panels/PathwayActionsSection.razor.cs` (197 lines) into the units its pattern names. 8 functions; the longest is 20 lines.
449. Divide `contracts/Models/SalesStrategy.cs` (195 lines) into the units its pattern names. 8 functions; the longest is 44 lines.
450. Divide `api/Features/Procurement/Commands/CreateWorkOrderFromMessageHandler.cs` (194 lines) into the units its pattern names. 2 functions; the longest is 133 lines.
451. Divide `jpms/Pages/ProfitSummary.razor.cs` (194 lines) into the units its pattern names. 7 functions; the longest is 31 lines.
452. Divide `api/Features/Requests/Queries/MailboxLiveQueryHandlers.cs` (193 lines) into the units its pattern names. 11 functions; the longest is 25 lines.
453. Divide `jpms/Pages/ProjectLabour.Approval.cs` (193 lines) into the units its pattern names. 12 functions; the longest is 41 lines.
454. Divide `api/Features/Ai/Tools/AiValuationInvoiceTools.XeroSalesInvoices.cs` (192 lines) into the units its pattern names. 1 functions; the longest is 173 lines.
455. Divide `api/Features/Labour/Commands/WorkerPlanningSlices.cs` (192 lines) into the units its pattern names. 13 functions; the longest is 27 lines.
456. Divide `jpms/Services/SessionService.cs` (192 lines) into the units its pattern names. 10 functions; the longest is 36 lines.
457. Divide `api/Features/Projects/Commands/DeleteProjectHandler.cs` (190 lines) into the units its pattern names. 3 functions; the longest is 131 lines.
458. Divide `jpms/Features/Triage/Panels/PathwayPaneConfig.cs` (190 lines) into the units its pattern names. 1 functions; the longest is 180 lines.
459. Divide `api/Features/Procurement/Queries/ResolveBidPackageTradeHandler.cs` (188 lines) into the units its pattern names. 5 functions; the longest is 60 lines.
460. Divide `jpms/Pages/PortalHome.razor.cs` (188 lines) into the units its pattern names. 7 functions; the longest is 28 lines.
461. Divide `jpms/Pages/ProjectDefectDetail.razor.cs` (188 lines) into the units its pattern names. 12 functions; the longest is 15 lines.
462. Divide `api/Features/Bluebeam/Extraction/DrawingExtractionRunner.cs` (186 lines) into the units its pattern names. 8 functions; the longest is 31 lines.
463. Divide `contracts/Commercial/Export/ValuationExportPendingSheet.cs` (186 lines) into the units its pattern names. 8 functions; the longest is 50 lines.
464. Divide `jpms/Components/ProjectContractPanel.razor.cs` (186 lines) into the units its pattern names. 10 functions; the longest is 27 lines.
465. Divide `api/Features/Ai/Tools/Actions/RequestsActions.Requests.cs` (185 lines) into the units its pattern names. 1 functions; the longest is 176 lines.
466. Divide `jpms/Pages/ProjectBidPackageInviteDetail.Invites.cs` (185 lines) into the units its pattern names. 6 functions; the longest is 56 lines.
467. Divide `jpms/Services/HttpValuationReportStore.cs` (185 lines) into the units its pattern names. 21 functions; the longest is 16 lines.
468. Divide `api/Features/Ai/Tools/Actions/AiActionSchema.cs` (184 lines) into the units its pattern names. 7 functions; the longest is 45 lines.
469. Divide `api/Features/Ai/Tools/Actions/WeeklyCashflowAndInventoryActions.cs` (183 lines) into the units its pattern names. 1 functions; the longest is 158 lines.
470. Divide `api/Features/Labour/Commands/ChaseDismissalSlices.cs` (183 lines) into the units its pattern names. 13 functions; the longest is 32 lines.
471. Divide `api/Features/Places/WebsiteContactFinder.cs` (183 lines) into the units its pattern names. 8 functions; the longest is 23 lines.
472. Divide `api/Features/Procurement/Attachments/BidPackageAttachmentEndpoints.cs` (183 lines) into the units its pattern names. 5 functions; the longest is 13 lines.
473. Divide `contracts/Models/LabourSchedule.cs` (183 lines) into the units its pattern names. 2 functions; the longest is 135 lines.
474. Divide `jpms/Components/PackageReconciliationSection.razor.cs` (183 lines) into the units its pattern names. 10 functions; the longest is 29 lines.
475. Divide `worker/Xero/XeroNightlyWorker.cs` (183 lines) into the units its pattern names. 3 functions; the longest is 58 lines.
476. Divide `jpms/Services/HttpProgressStore.cs` (182 lines) into the units its pattern names. 15 functions; the longest is 19 lines.
477. Divide `api/Features/Ai/Tools/AiToolCatalogue.Records.cs` (181 lines) into the units its pattern names. 1 functions; the longest is 168 lines.
478. Divide `api/Features/Procurement/Attachments/WorkOrderAttachmentEndpoints.cs` (181 lines) into the units its pattern names. 5 functions; the longest is 13 lines.
479. Divide `contracts/Models/AuditEvent.cs` (181 lines) into the units its pattern names. 0 functions; the longest is 0 lines.
480. Divide `jpms/Pages/TriageQueue.ApplyFiling.cs` (181 lines) into the units its pattern names. 8 functions; the longest is 32 lines.
481. Divide `api/Data/Entities/SalesEntities.cs` (180 lines) into the units its pattern names. 0 functions; the longest is 0 lines.
482. Divide `jpms/Components/ProjectCorrespondencePanel.razor.cs` (180 lines) into the units its pattern names. 9 functions; the longest is 35 lines.
483. Divide `jpms/Pages/ProjectHs.razor.cs` (180 lines) into the units its pattern names. 11 functions; the longest is 19 lines.
484. Divide `api/Features/Ai/Sources/AiSourceDocument.cs` (179 lines) into the units its pattern names. 5 functions; the longest is 52 lines.
485. Divide `api/Features/Sales/Research/StrategyResearcher.cs` (179 lines) into the units its pattern names. 1 functions; the longest is 171 lines.
486. Divide `contracts/Ai/PageGuides/FinancePageGuides.cs` (179 lines) into the units its pattern names. 0 functions; the longest is 0 lines.
487. Divide `jpms/Pages/Todos.razor.cs` (179 lines) into the units its pattern names. 7 functions; the longest is 20 lines.
488. Divide `jpms/Pages/ProjectHsAudit.razor.cs` (178 lines) into the units its pattern names. 14 functions; the longest is 17 lines.
489. Divide `api/Features/MailboxIntake/Graph/TriageCategories.cs` (177 lines) into the units its pattern names. 4 functions; the longest is 47 lines.
490. Divide `api/Features/Variations/Commands/ReturnVariationOrderToQuotingHandler.cs` (177 lines) into the units its pattern names. 2 functions; the longest is 115 lines.
491. Divide `api/Program.cs` (177 lines) into the units its pattern names. 1 functions; the longest is 14 lines.
492. Divide `api/Features/Ai/Tools/Actions/ProjectsAndTendersActions.Projects.cs` (175 lines) into the units its pattern names. 1 functions; the longest is 154 lines.
493. Divide `jpms/Services/HttpVariationStore.cs` (175 lines) into the units its pattern names. 19 functions; the longest is 16 lines.
494. Divide `api/Features/Ai/Tools/AiToolCatalogue.Procurement.cs` (173 lines) into the units its pattern names. 1 functions; the longest is 160 lines.
495. Divide `api/Features/Requests/Documents/DocumentFontResolver.cs` (173 lines) into the units its pattern names. 6 functions; the longest is 41 lines.
496. Divide `jpms/Cqrs/HttpQueryClient.cs` (173 lines) into the units its pattern names. 2 functions; the longest is 11 lines.
497. Divide `jpms/Services/HttpRequestRegister.cs` (172 lines) into the units its pattern names. 17 functions; the longest is 24 lines.
498. Divide `api/Features/Requests/Attachments/RequestAttachmentHandlers.cs` (171 lines) into the units its pattern names. 6 functions; the longest is 65 lines.
499. Divide `api/Features/Sales/SalesImagineEndpoints.cs` (171 lines) into the units its pattern names. 7 functions; the longest is 28 lines.
500. Divide `jpms/Pages/CashForecast.Forecast.cs` (170 lines) into the units its pattern names. 5 functions; the longest is 66 lines.
501. Divide `api/Features/Ai/Tools/AiToolCatalogue.Context.cs` (169 lines) into the units its pattern names. 1 functions; the longest is 156 lines.
502. Divide `api/Features/RecordLinks/Queries/ListProjectCommunicationsHandler.cs` (169 lines) into the units its pattern names. 5 functions; the longest is 71 lines.
503. Divide `jpms/Components/ValuationReportTable.VariationEdits.cs` (169 lines) into the units its pattern names. 6 functions; the longest is 32 lines.
504. Divide `api/Features/Ai/Tools/Actions/SalesActions.Estimates.cs` (168 lines) into the units its pattern names. 1 functions; the longest is 144 lines.
505. Divide `api/Features/MailboxIntake/Sharing/AzureBlobEmailFileShareStore.cs` (168 lines) into the units its pattern names. 6 functions; the longest is 45 lines.
506. Divide `contracts/Models/WeeklyCashflow.cs` (168 lines) into the units its pattern names. 4 functions; the longest is 16 lines.
507. Divide `contracts/Xero/GetXeroAgedReceivables.cs` (168 lines) into the units its pattern names. 6 functions; the longest is 20 lines.
508. Divide `jpms/Components/ValuationReportTable.razor.cs` (168 lines) into the units its pattern names. 9 functions; the longest is 30 lines.
509. Divide `jpms/Pages/ProjectVariationDetail.Menus.cs` (168 lines) into the units its pattern names. 0 functions; the longest is 0 lines.
510. Divide `jpms/Pages/SubcontractorCommunications.razor.cs` (168 lines) into the units its pattern names. 8 functions; the longest is 24 lines.
511. Divide `jpms/Pages/TriageQueue.ListReads.cs` (168 lines) into the units its pattern names. 7 functions; the longest is 40 lines.
512. Divide `api/Features/Xero/Ledger/XeroAllocationSuggester.cs` (167 lines) into the units its pattern names. 7 functions; the longest is 22 lines.
513. Divide `contracts/Xero/GetXeroAgedPayables.cs` (167 lines) into the units its pattern names. 6 functions; the longest is 20 lines.
514. Divide `jpms/Components/WorkOrderForm.Assistant.cs` (167 lines) into the units its pattern names. 7 functions; the longest is 23 lines.
515. Divide `jpms/Pages/AiActionsAdmin.razor.cs` (167 lines) into the units its pattern names. 9 functions; the longest is 34 lines.
516. Divide `api/Features/Drawings/Commands/DrawingFolderCommandEndpoints.cs` (166 lines) into the units its pattern names. 5 functions; the longest is 29 lines.
517. Divide `api/Features/Kpi/KpiEndpoints.cs` (166 lines) into the units its pattern names. 8 functions; the longest is 24 lines.
518. Divide `jpms/Components/FinancialsTable.Figures.cs` (166 lines) into the units its pattern names. 4 functions; the longest is 81 lines.
519. Divide `contracts/Documents/Excel/ExcelWorkbook.cs` (165 lines) into the units its pattern names. 2 functions; the longest is 65 lines.
520. Divide `jpms/Components/ProjectContractTermsDialog.razor.cs` (165 lines) into the units its pattern names. 3 functions; the longest is 59 lines.
521. Divide `jpms/Pages/ProjectLabour.Settlement.cs` (165 lines) into the units its pattern names. 7 functions; the longest is 63 lines.
522. Divide `jpms/Services/HttpProjectContractStore.cs` (165 lines) into the units its pattern names. 7 functions; the longest is 31 lines.
523. Divide `api/Features/MailboxIntake/MailboxIntakeOptions.cs` (164 lines) into the units its pattern names. 2 functions; the longest is 39 lines.
524. Divide `jpms/Components/ValuationInvoicesSection.Commands.cs` (164 lines) into the units its pattern names. 14 functions; the longest is 17 lines.
525. Divide `jpms/Components/ValuationStatementViewer.razor.cs` (164 lines) into the units its pattern names. 8 functions; the longest is 16 lines.
526. Divide `jpms/Pages/TriageQueue.Views.cs` (164 lines) into the units its pattern names. 8 functions; the longest is 47 lines.
527. Divide `api/Features/Ai/Tools/AiSourceTools.ReadSource.cs` (163 lines) into the units its pattern names. 5 functions; the longest is 70 lines.
528. Divide `jpms/Services/ErrorReporter.cs` (163 lines) into the units its pattern names. 9 functions; the longest is 21 lines.
529. Divide `api/Features/Requests/Recipients/RequestRecipientResolver.cs` (162 lines) into the units its pattern names. 3 functions; the longest is 95 lines.
530. Divide `jpms/Features/Drawings/DrawingsReadModel.cs` (162 lines) into the units its pattern names. 12 functions; the longest is 36 lines.
531. Divide `api/Features/Drawings/Geometry/PdfGeometryExtractor.cs` (161 lines) into the units its pattern names. 7 functions; the longest is 38 lines.
532. Divide `api/Features/Requests/RequestsFeatureRegistration.cs` (161 lines) into the units its pattern names. 2 functions; the longest is 133 lines.
533. Divide `worker/Program.cs` (161 lines) into the units its pattern names. 0 functions; the longest is 0 lines.
534. Divide `api/Features/Labour/Commands/WeekSignOffSlices.cs` (160 lines) into the units its pattern names. 9 functions; the longest is 40 lines.
535. Divide `jpms/Features/Triage/RecordLinkVocabulary.cs` (160 lines) into the units its pattern names. 5 functions; the longest is 26 lines.
536. Divide `api/Features/MailboxIntake/Graph/MailboxGraphClient.Reading.cs` (159 lines) into the units its pattern names. 9 functions; the longest is 55 lines.
537. Divide `api/Features/Ai/Tools/AiRecordTools.XeroCustomers.cs` (158 lines) into the units its pattern names. 1 functions; the longest is 132 lines.
538. Divide `api/Features/Sales/Imagine/AzureImageClient.cs` (158 lines) into the units its pattern names. 7 functions; the longest is 47 lines.
539. Divide `jpms/Features/Triage/Panels/PathwayPane.razor.cs` (158 lines) into the units its pattern names. 8 functions; the longest is 10 lines.
540. Divide `jpms/Pages/XeroAllocation.Tabs.cs` (158 lines) into the units its pattern names. 5 functions; the longest is 50 lines.
541. Divide `api/Features/Commercial/Documents/CostCentreReconciliationPdfBuilder.cs` (157 lines) into the units its pattern names. 3 functions; the longest is 105 lines.
542. Divide `contracts/Labour/ForecastRules.cs` (157 lines) into the units its pattern names. 8 functions; the longest is 19 lines.
543. Divide `jpms/Features/Procurement/TenderSubmissionModal.razor.cs` (157 lines) into the units its pattern names. 6 functions; the longest is 45 lines.
544. Divide `jpms/Pages/ProjectWorkOrders.razor.cs` (157 lines) into the units its pattern names. 3 functions; the longest is 73 lines.
545. Divide `api/Features/BuildingControl/Commands/BuildingControlCaseCommands.cs` (156 lines) into the units its pattern names. 8 functions; the longest is 60 lines.
546. Divide `api/Features/Labour/Commands/ApproveTimesheetsSlice.cs` (156 lines) into the units its pattern names. 4 functions; the longest is 110 lines.
547. Divide `api/Features/Registers/PolicyDocumentSlices.cs` (156 lines) into the units its pattern names. 9 functions; the longest is 41 lines.
548. Divide `api/Data/Entities/LabourPlanningEntities.cs` (154 lines) into the units its pattern names. 0 functions; the longest is 0 lines.
549. Divide `api/Features/Ai/Tools/Actions/LabourAndBackOfficeActions.HealthAndSafety.cs` (154 lines) into the units its pattern names. 1 functions; the longest is 144 lines.
550. Divide `api/Features/Ai/Tools/Actions/SiteAndProgressActions.CloseoutDefects.cs` (154 lines) into the units its pattern names. 1 functions; the longest is 134 lines.
551. Divide `api/Features/Procurement/Commands/UpdateManualWorkOrderHandler.cs` (154 lines) into the units its pattern names. 1 functions; the longest is 125 lines.
552. Divide `jpms/Components/ProjectRetentionPanel.razor.cs` (154 lines) into the units its pattern names. 7 functions; the longest is 18 lines.
553. Divide `api/Features/Procurement/Commands/AwardBidPackageHandler.cs` (153 lines) into the units its pattern names. 2 functions; the longest is 72 lines.
554. Divide `api/Features/DocumentControl/Commands/DocumentControlItemCommandEndpoints.cs` (152 lines) into the units its pattern names. 6 functions; the longest is 27 lines.
555. Divide `api/Features/Labour/Commands/MoveTimesheetSlice.cs` (152 lines) into the units its pattern names. 7 functions; the longest is 62 lines.
556. Divide `api/Features/Procurement/Attachments/CompanyTenderTermsStore.cs` (152 lines) into the units its pattern names. 6 functions; the longest is 24 lines.
557. Divide `jpms/Features/Labour/WeekEntryModal.razor.cs` (152 lines) into the units its pattern names. 6 functions; the longest is 45 lines.
558. Divide `jpms/Pages/ProjectLabour.razor.cs` (152 lines) into the units its pattern names. 5 functions; the longest is 26 lines.
559. Divide `api/Features/Ai/Tools/AiKpiTools.cs` (151 lines) into the units its pattern names. 1 functions; the longest is 129 lines.
560. Divide `api/Features/Commercial/CommercialFeatureRegistration.cs` (151 lines) into the units its pattern names. 1 functions; the longest is 141 lines.
561. Divide `contracts/Commercial/Export/ValuationStatementExport.cs` (151 lines) into the units its pattern names. 5 functions; the longest is 50 lines.
562. Divide `contracts/Models/Labour.cs` (151 lines) into the units its pattern names. 0 functions; the longest is 0 lines.
563. Divide `jpms/Features/Triage/Panels/Actions/SystemActionKind.cs` (151 lines) into the units its pattern names. 2 functions; the longest is 27 lines.
564. Divide `jpms/Pages/ProjectVariations.StatusChanges.cs` (151 lines) into the units its pattern names. 5 functions; the longest is 45 lines.
565. Divide `api/Features/Labour/SettlementScheduleBuilder.cs` (150 lines) into the units its pattern names. 1 functions; the longest is 134 lines.
566. Divide `contracts/Ai/ModalCatalog.Labour.cs` (150 lines) into the units its pattern names. 0 functions; the longest is 0 lines.
567. Divide `contracts/Xero/XeroSitePnl.cs` (150 lines) into the units its pattern names. 3 functions; the longest is 43 lines.
568. Divide `api/Features/RecordLinks/RecordThreadTagger.cs` (149 lines) into the units its pattern names. 4 functions; the longest is 52 lines.
569. Divide `api/Features/Xero/Ledger/LabourSupplierRecognition.cs` (149 lines) into the units its pattern names. 6 functions; the longest is 43 lines.
570. Divide `contracts/Ai/ModalCatalog.Mail.cs` (149 lines) into the units its pattern names. 0 functions; the longest is 0 lines.
571. Divide `jpms/Components/ValuationReportTable.PercentEditing.cs` (149 lines) into the units its pattern names. 7 functions; the longest is 38 lines.
572. Divide `jpms/Features/Closeout/Detail/DefectCommunicationsPanel.razor.cs` (149 lines) into the units its pattern names. 9 functions; the longest is 14 lines.
573. Divide `jpms/Features/Procurement/DraftWorkOrdersPanel.razor.cs` (149 lines) into the units its pattern names. 4 functions; the longest is 37 lines.
574. Divide `jpms/Pages/ProjectBidPackageInviteDetail.Communications.cs` (149 lines) into the units its pattern names. 6 functions; the longest is 44 lines.
575. Divide `api/Features/Hs/Audits/Commands/HsAuditEndpoints.cs` (148 lines) into the units its pattern names. 6 functions; the longest is 33 lines.
576. Divide `api/Features/Bluebeam/Extraction/DrawingExtractionResultWriter.cs` (147 lines) into the units its pattern names. 7 functions; the longest is 38 lines.
577. Divide `api/Features/Drawings/Geometry/DrawingStructureBuilder.cs` (147 lines) into the units its pattern names. 6 functions; the longest is 46 lines.
578. Divide `jpms/Features/Drawings/DrawingArchiveExpander.cs` (147 lines) into the units its pattern names. 8 functions; the longest is 29 lines.
579. Divide `api/Features/RecordLinks/Providers/WorkOrderLinkProvider.cs` (146 lines) into the units its pattern names. 4 functions; the longest is 32 lines.
580. Divide `contracts/Models/LeadEstimate.cs` (146 lines) into the units its pattern names. 6 functions; the longest is 52 lines.
581. Divide `api/Features/Ai/Tools/AiDeliveryTools.Hs.cs` (145 lines) into the units its pattern names. 6 functions; the longest is 47 lines.
582. Divide `api/Features/Connect/TokenEndpoint.cs` (145 lines) into the units its pattern names. 8 functions; the longest is 43 lines.
583. Divide `jpms/Services/HttpXeroLedgerStore.cs` (145 lines) into the units its pattern names. 15 functions; the longest is 20 lines.
584. Divide `api/Features/BuildingControl/Commands/BuildingControlInspectionCommands.cs` (143 lines) into the units its pattern names. 11 functions; the longest is 20 lines.
585. Divide `api/Features/Connect/OAuthTokenManager.cs` (143 lines) into the units its pattern names. 5 functions; the longest is 45 lines.
586. Divide `api/Features/Procurement/Queries/SearchLocalSubcontractorsHandler.cs` (143 lines) into the units its pattern names. 4 functions; the longest is 89 lines.
587. Divide `jpms/Pages/TriageQueue.ApplyPlan.cs` (143 lines) into the units its pattern names. 4 functions; the longest is 52 lines.
588. Divide `jpms/Pages/WeeklyCashflow.razor.cs` (143 lines) into the units its pattern names. 4 functions; the longest is 27 lines.
589. Divide `api/Data/Entities/PeopleEntities.cs` (142 lines) into the units its pattern names. 0 functions; the longest is 0 lines.
590. Divide `api/Features/Ai/AnthropicOptions.cs` (142 lines) into the units its pattern names. 3 functions; the longest is 64 lines.
591. Divide `api/Features/Ai/Tools/Actions/CommercialActions.Cvr.cs` (141 lines) into the units its pattern names. 1 functions; the longest is 127 lines.
592. Divide `contracts/Xero/WorkOrderBills.cs` (141 lines) into the units its pattern names. 3 functions; the longest is 31 lines.
593. Divide `api/Features/Ai/Tools/Actions/ProcurementActions.Invites.cs` (140 lines) into the units its pattern names. 1 functions; the longest is 132 lines.
594. Divide `api/Features/Bluebeam/Extraction/BluebeamMarkupParser.cs` (140 lines) into the units its pattern names. 10 functions; the longest is 22 lines.
595. Divide `api/Features/BuildingControl/Commands/CreateBuildingControlInspectionFromMessage.cs` (140 lines) into the units its pattern names. 6 functions; the longest is 30 lines.
596. Divide `api/Features/Xero/Ledger/XeroLedgerReads.cs` (140 lines) into the units its pattern names. 4 functions; the longest is 53 lines.
597. Divide `jpms/Pages/LabourOverview.razor.cs` (140 lines) into the units its pattern names. 9 functions; the longest is 28 lines.
598. Divide `api/Features/Ai/Tools/AiSourceTools.ListSources.cs` (139 lines) into the units its pattern names. 2 functions; the longest is 119 lines.
599. Divide `api/Features/Procurement/Documents/WorkOrderPoRenderer.Helpers.cs` (139 lines) into the units its pattern names. 10 functions; the longest is 25 lines.
600. Divide `api/Features/Xero/XeroClient.ReadsCash.cs` (139 lines) into the units its pattern names. 5 functions; the longest is 58 lines.
601. Divide `contracts/Commercial/ProjectDrawdown.cs` (139 lines) into the units its pattern names. 3 functions; the longest is 74 lines.
602. Divide `jpms/Features/Triage/Panels/NewEmailComposerPane.razor.cs` (139 lines) into the units its pattern names. 6 functions; the longest is 45 lines.
603. Divide `jpms/Features/WeeklyCashflow/CashflowItemModal.razor.cs` (139 lines) into the units its pattern names. 5 functions; the longest is 30 lines.
604. Divide `api/Features/Commercial/Documents/ValuationReportBillRows.cs` (138 lines) into the units its pattern names. 7 functions; the longest is 22 lines.
605. Divide `api/Features/Drawings/Commands/DrawingFolderCommandHandlers.cs` (138 lines) into the units its pattern names. 4 functions; the longest is 31 lines.
606. Divide `api/Features/Drawings/Geometry/DrawingScaleCalibrator.cs` (138 lines) into the units its pattern names. 4 functions; the longest is 39 lines.
607. Divide `api/Features/Sales/Imagine/ImagineNotifier.cs` (138 lines) into the units its pattern names. 8 functions; the longest is 20 lines.
608. Divide `jpms/Pages/Todos.Filters.cs` (138 lines) into the units its pattern names. 5 functions; the longest is 19 lines.
609. Divide `jpms/Services/UserInviteService.cs` (138 lines) into the units its pattern names. 6 functions; the longest is 23 lines.
610. Divide `api/Data/Entities/CoreEntities.cs` (137 lines) into the units its pattern names. 0 functions; the longest is 0 lines.
611. Divide `api/Features/Ai/Tools/Actions/ProcurementActions.BidPackages.cs` (137 lines) into the units its pattern names. 1 functions; the longest is 129 lines.
612. Divide `jpms/Features/Labour/LabourReadModels.cs` (137 lines) into the units its pattern names. 6 functions; the longest is 12 lines.
613. Divide `jpms/Features/Procurement/ValuationLinePickerModal.razor.cs` (137 lines) into the units its pattern names. 8 functions; the longest is 17 lines.
614. Divide `jpms/Features/WeeklyCashflow/SupplierGroupsModal.razor.cs` (137 lines) into the units its pattern names. 7 functions; the longest is 28 lines.
615. Divide `jpms/Pages/CashForecast.Export.cs` (137 lines) into the units its pattern names. 2 functions; the longest is 105 lines.
616. Divide `api/Features/Ai/Tools/AiRecordTools.Contexts.cs` (136 lines) into the units its pattern names. 1 functions; the longest is 126 lines.
617. Divide `api/Features/Sales/Commands/EstimateChangeHandlers.cs` (136 lines) into the units its pattern names. 4 functions; the longest is 57 lines.
618. Divide `api/Features/Sales/SalesFeatureRegistration.cs` (136 lines) into the units its pattern names. 1 functions; the longest is 103 lines.
619. Divide `jpms/Features/Sales/SalesReadModels.cs` (136 lines) into the units its pattern names. 8 functions; the longest is 11 lines.
620. Divide `api/Features/Commercial/Commands/SetXeroLineWorkOrderLinksHandler.cs` (135 lines) into the units its pattern names. 1 functions; the longest is 110 lines.
621. Divide `api/Features/Site/Commands/SuggestProgrammeDraftMappingsHandler.cs` (135 lines) into the units its pattern names. 6 functions; the longest is 45 lines.
622. Divide `jpms/Features/Labour/LabourRouteRegistration.cs` (135 lines) into the units its pattern names. 2 functions; the longest is 114 lines.
623. Divide `jpms/Pages/ProjectWorkOrders.Rows.cs` (135 lines) into the units its pattern names. 3 functions; the longest is 55 lines.
624. Divide `api/Features/Labour/Commands/SubmitWorkerWeekSlice.cs` (134 lines) into the units its pattern names. 3 functions; the longest is 67 lines.
625. Divide `api/Features/Subcontractors/Queries/GetSubcontractorStatementHandler.cs` (134 lines) into the units its pattern names. 2 functions; the longest is 73 lines.
626. Divide `jpms/Features/Commercial/ValuationReportReadModels.cs` (134 lines) into the units its pattern names. 5 functions; the longest is 26 lines.
627. Divide `jpms/Pages/XeroAllocation.InvoiceViewer.cs` (134 lines) into the units its pattern names. 8 functions; the longest is 24 lines.
628. Divide `api/Features/ValuationInvoices/XeroRaise/ValuationInvoiceXeroRaisePlanner.cs` (133 lines) into the units its pattern names. 6 functions; the longest is 32 lines.
629. Divide `contracts/Ai/PageGuides/ProcurementPageGuides.cs` (133 lines) into the units its pattern names. 0 functions; the longest is 0 lines.
630. Divide `jpms/Services/HttpIntakeQueue.cs` (133 lines) into the units its pattern names. 3 functions; the longest is 73 lines.
631. Divide `api/Features/ArchitectInstructions/Storage/ArchitectInstructionBlobStore.cs` (132 lines) into the units its pattern names. 6 functions; the longest is 18 lines.
632. Divide `api/Features/BuildingControl/Attachments/BuildingControlAttachmentHandlers.cs` (132 lines) into the units its pattern names. 8 functions; the longest is 35 lines.
633. Divide `api/Features/Commercial/Documents/CostCentreReconciliationRenderer.Helpers.cs` (132 lines) into the units its pattern names. 10 functions; the longest is 14 lines.
634. Divide `api/Features/Commercial/Documents/ValuationStatementPdfBuilder.cs` (132 lines) into the units its pattern names. 1 functions; the longest is 123 lines.
635. Divide `api/Features/MailboxIntake/Compose/SendMailboxEmailHandler.Compose.cs` (132 lines) into the units its pattern names. 3 functions; the longest is 28 lines.
636. Divide `api/Features/MailboxIntake/Graph/MailboxGraphClient.Snapshots.cs` (132 lines) into the units its pattern names. 3 functions; the longest is 51 lines.
637. Divide `api/Features/Registers/RegisterItemSlices.cs` (132 lines) into the units its pattern names. 9 functions; the longest is 33 lines.
638. Divide `contracts/Models/Imagine.cs` (132 lines) into the units its pattern names. 2 functions; the longest is 78 lines.
639. Divide `contracts/Models/LabourPlanning.cs` (132 lines) into the units its pattern names. 0 functions; the longest is 0 lines.
640. Divide `api/Features/Ai/Tools/AiSkillTools.cs` (131 lines) into the units its pattern names. 1 functions; the longest is 111 lines.
641. Divide `api/Features/RecordLinks/Commands/LinkMessageToRecordHandler.cs` (131 lines) into the units its pattern names. 2 functions; the longest is 90 lines.
642. Divide `api/Features/RecordLinks/Providers/ValuationClaimLinkProvider.cs` (131 lines) into the units its pattern names. 6 functions; the longest is 33 lines.
643. Divide `jpms/Pages/ProjectRequests.RowActions.cs` (131 lines) into the units its pattern names. 4 functions; the longest is 47 lines.
644. Divide `jpms/Services/HttpPortalStore.cs` (131 lines) into the units its pattern names. 9 functions; the longest is 26 lines.
645. Divide `api/Features/Commercial/PackageReconciliationCalculator.cs` (130 lines) into the units its pattern names. 1 functions; the longest is 115 lines.
646. Divide `jpms/Services/ILabourStore.cs` (130 lines) into the units its pattern names. 0 functions; the longest is 0 lines.
647. Divide `api/Features/Ai/Tools/AiCommercialTools.CostCodes.cs` (129 lines) into the units its pattern names. 1 functions; the longest is 122 lines.
648. Divide `api/Features/Labour/Commands/WorkerDayCorrectionByNameSlices.cs` (129 lines) into the units its pattern names. 8 functions; the longest is 17 lines.
649. Divide `contracts/Commercial/ValuationSummaryFigures.cs` (129 lines) into the units its pattern names. 1 functions; the longest is 106 lines.
650. Divide `jpms/Components/ValuationInvoicesSection.Forms.cs` (129 lines) into the units its pattern names. 9 functions; the longest is 30 lines.
651. Divide `jpms/Features/Procurement/LocalSubcontractorFinderModal.razor.cs` (128 lines) into the units its pattern names. 4 functions; the longest is 50 lines.
652. Divide `api/Features/Ai/AiRegistryDriftCheck.cs` (127 lines) into the units its pattern names. 2 functions; the longest is 63 lines.
653. Divide `api/Features/MailboxIntake/Compose/SendMailboxEmailHandler.Attachments.cs` (127 lines) into the units its pattern names. 3 functions; the longest is 64 lines.
654. Divide `jpms/Features/Sales/SalesRouteRegistration.cs` (127 lines) into the units its pattern names. 2 functions; the longest is 110 lines.
655. Divide `jpms/Services/HttpProcurementStore.cs` (127 lines) into the units its pattern names. 13 functions; the longest is 10 lines.
656. Divide `api/Features/Ai/Tools/AiRecordTools.Compliance.cs` (126 lines) into the units its pattern names. 1 functions; the longest is 102 lines.
657. Divide `jpms/Cqrs/HttpCommandSender.cs` (126 lines) into the units its pattern names. 3 functions; the longest is 20 lines.
658. Divide `jpms/Pages/XeroAllocation.WorkOrderBills.cs` (126 lines) into the units its pattern names. 5 functions; the longest is 31 lines.
659. Divide `api/Features/Ai/Tools/AiAuditTools.cs` (125 lines) into the units its pattern names. 3 functions; the longest is 51 lines.
660. Divide `api/Features/Subcontractors/Commands/ImportXeroSupplierHandler.cs` (125 lines) into the units its pattern names. 2 functions; the longest is 93 lines.
661. Divide `api/Features/Xero/XeroOptions.cs` (125 lines) into the units its pattern names. 1 functions; the longest is 46 lines.
662. Divide `contracts/Models/Commercial.cs` (125 lines) into the units its pattern names. 2 functions; the longest is 41 lines.
663. Divide `contracts/Models/VariationOrder.cs` (125 lines) into the units its pattern names. 4 functions; the longest is 47 lines.
664. Divide `jpms/Features/Variations/VariationsRouteRegistration.cs` (125 lines) into the units its pattern names. 1 functions; the longest is 115 lines.
665. Divide `jpms/Services/ArchitectInstructionStore.cs` (125 lines) into the units its pattern names. 3 functions; the longest is 43 lines.
666. Divide `jpms/Services/HttpValuationInvoiceStore.cs` (125 lines) into the units its pattern names. 14 functions; the longest is 15 lines.
667. Divide `api/Features/Ai/Sources/AiSourceReader.Reading.cs` (124 lines) into the units its pattern names. 4 functions; the longest is 75 lines.
668. Divide `api/Features/Ai/Tools/BidPackageContextReads.cs` (124 lines) into the units its pattern names. 5 functions; the longest is 47 lines.
669. Divide `api/Features/Progress/ContractorsReports/Documents/ContractorsReportPdfRenderer.Narrative.cs` (124 lines) into the units its pattern names. 9 functions; the longest is 16 lines.
670. Divide `contracts/Models/CalendarEvent.cs` (124 lines) into the units its pattern names. 6 functions; the longest is 18 lines.
671. Divide `jpms/Features/Procurement/SubcontractorInvitePickerModal.razor.cs` (124 lines) into the units its pattern names. 2 functions; the longest is 25 lines.
672. Divide `api/Features/Ai/Tools/AiToolCatalogue.cs` (123 lines) into the units its pattern names. 2 functions; the longest is 23 lines.
673. Divide `api/Features/Labour/Commands/SettlementCommandGates.cs` (123 lines) into the units its pattern names. 8 functions; the longest is 22 lines.
674. Divide `api/Features/Labour/Queries/GetMyLabourDaySlice.cs` (123 lines) into the units its pattern names. 3 functions; the longest is 85 lines.
675. Divide `api/Features/ProjectContracts/Storage/AzureBlobProjectContractStore.cs` (123 lines) into the units its pattern names. 8 functions; the longest is 17 lines.
676. Divide `api/Features/Xero/TrackingOptions/XeroCostCodeOptionSlices.cs` (123 lines) into the units its pattern names. 5 functions; the longest is 41 lines.
677. Divide `jpms/Components/ReportingErrorBoundary.cs` (123 lines) into the units its pattern names. 2 functions; the longest is 86 lines.
678. Divide `api/Features/Auth/PasswordResetSender.cs` (122 lines) into the units its pattern names. 6 functions; the longest is 39 lines.
679. Divide `api/Features/Commercial/Commands/SetValuationLineCostCentreHandler.cs` (122 lines) into the units its pattern names. 2 functions; the longest is 87 lines.
680. Divide `api/Features/Directory/Queries/ListEmailRecipientsHandler.cs` (122 lines) into the units its pattern names. 1 functions; the longest is 98 lines.
681. Divide `api/Features/RecordLinks/Providers/VariationOrderLinkProvider.cs` (122 lines) into the units its pattern names. 6 functions; the longest is 19 lines.
682. Divide `api/Features/Subcontractors/SubcontractorsFeatureRegistration.cs` (122 lines) into the units its pattern names. 2 functions; the longest is 92 lines.
683. Divide `api/Features/Xero/SitePnl/GetXeroSitePnlHandler.cs` (122 lines) into the units its pattern names. 3 functions; the longest is 60 lines.
684. Divide `contracts/MailboxCompose/SendMailboxEmail.cs` (122 lines) into the units its pattern names. 1 functions; the longest is 62 lines.
685. Divide `contracts/Models/ProjectContract.cs` (122 lines) into the units its pattern names. 3 functions; the longest is 68 lines.
686. Divide `jpms/Components/FinancialsTable.Export.cs` (122 lines) into the units its pattern names. 3 functions; the longest is 85 lines.
687. Divide `jpms/Pages/ProjectWorkOrders.Export.cs` (122 lines) into the units its pattern names. 1 functions; the longest is 106 lines.
688. Divide `api/Features/Ai/Tools/Actions/AiActionExecutor.cs` (121 lines) into the units its pattern names. 5 functions; the longest is 49 lines.
689. Divide `api/Features/RecordLinks/Providers/RequestLinkProvider.cs` (121 lines) into the units its pattern names. 5 functions; the longest is 28 lines.
690. Divide `api/Features/Xero/XeroFeatureRegistration.cs` (121 lines) into the units its pattern names. 1 functions; the longest is 102 lines.
691. Divide `contracts/Models/ValuationInvoice.cs` (121 lines) into the units its pattern names. 2 functions; the longest is 46 lines.
692. Divide `api/Features/BuildingControl/BuildingControlRules.cs` (120 lines) into the units its pattern names. 8 functions; the longest is 17 lines.
693. Divide `api/Features/Kpi/Commands/MarkEmailAsKpiHandler.cs` (120 lines) into the units its pattern names. 4 functions; the longest is 52 lines.
694. Divide `api/Features/Sales/Commands/StrategyHandlers.cs` (120 lines) into the units its pattern names. 5 functions; the longest is 24 lines.
695. Divide `contracts/Models/ProgrammeDraft.cs` (120 lines) into the units its pattern names. 6 functions; the longest is 20 lines.
696. Divide `jpms/Pages/ProjectBidPackageInviteDetail.Award.cs` (120 lines) into the units its pattern names. 6 functions; the longest is 19 lines.
697. Divide `worker/Bluebeam/BluebeamConnectCallback.cs` (120 lines) into the units its pattern names. 7 functions; the longest is 36 lines.
698. Divide `api/Features/Ai/Tools/Actions/ProcurementActions.Tenders.cs` (119 lines) into the units its pattern names. 1 functions; the longest is 111 lines.
699. Divide `api/Features/MailboxIntake/MailboxIntakeFeatureRegistration.cs` (119 lines) into the units its pattern names. 1 functions; the longest is 97 lines.
700. Divide `api/Features/Procurement/Commands/RecodeWorkOrderLineHandler.cs` (119 lines) into the units its pattern names. 2 functions; the longest is 88 lines.
701. Divide `api/Features/Progress/Queries/DownloadProgressReportPdfEndpoint.cs` (119 lines) into the units its pattern names. 3 functions; the longest is 9 lines.
702. Divide `api/Features/Requests/Commands/UpdateRequestDetailsHandler.cs` (119 lines) into the units its pattern names. 3 functions; the longest is 90 lines.
703. Divide `api/Features/Sales/Inbox/SalesInboxEndpoints.cs` (119 lines) into the units its pattern names. 6 functions; the longest is 17 lines.
704. Divide `api/Features/Sales/SalesEntityMapping.cs` (119 lines) into the units its pattern names. 2 functions; the longest is 104 lines.
705. Divide `jpms/Features/Todos/Detail/TodoCommunicationsPanel.razor.cs` (119 lines) into the units its pattern names. 8 functions; the longest is 15 lines.
706. Divide `api/Features/Ai/Tools/AiSitePhotoTools.cs` (118 lines) into the units its pattern names. 4 functions; the longest is 33 lines.
707. Divide `api/Features/Ai/Tools/AiTool.cs` (118 lines) into the units its pattern names. 2 functions; the longest is 87 lines.
708. Divide `api/Features/ArchitectInstructions/ArchitectInstructionSupport.cs` (118 lines) into the units its pattern names. 2 functions; the longest is 59 lines.
709. Divide `api/Features/ProjectContracts/Commands/UploadProjectContractAmendmentEndpoint.cs` (118 lines) into the units its pattern names. 2 functions; the longest is 15 lines.
710. Divide `api/Features/ValuationInvoices/XeroPayments/SyncValuationInvoicePaymentsFromXeroHandler.cs` (118 lines) into the units its pattern names. 5 functions; the longest is 42 lines.
711. Divide `jpms/Services/IIntakeQueue.cs` (118 lines) into the units its pattern names. 0 functions; the longest is 0 lines.
712. Divide `api/Features/Commercial/Queries/ListCostCentreActualCostsHandler.cs` (117 lines) into the units its pattern names. 2 functions; the longest is 91 lines.
713. Divide `api/Features/Xero/Ledger/XeroInvoiceAttachmentEndpoints.cs` (117 lines) into the units its pattern names. 5 functions; the longest is 39 lines.
714. Divide `contracts/Documents/Excel/ExcelWorkbookWriter.Worksheet.cs` (117 lines) into the units its pattern names. 2 functions; the longest is 89 lines.
715. Divide `api/Features/Ai/Tools/Actions/SubcontractorsAndLeadsActions.Contacts.cs` (116 lines) into the units its pattern names. 1 functions; the longest is 98 lines.
716. Divide `contracts/Commercial/ClaimRespread.cs` (116 lines) into the units its pattern names. 1 functions; the longest is 81 lines.
717. Divide `contracts/Models/Lead.cs` (116 lines) into the units its pattern names. 2 functions; the longest is 32 lines.
718. Divide `jpms/Services/CurrentProjectService.cs` (116 lines) into the units its pattern names. 5 functions; the longest is 14 lines.
719. Divide `jpms/Services/IDrawingStore.cs` (116 lines) into the units its pattern names. 0 functions; the longest is 0 lines.
720. Divide `api/Features/BuildingControl/Commands/BuildingControlInspectionEndpoints.cs` (115 lines) into the units its pattern names. 5 functions; the longest is 25 lines.
721. Divide `api/Features/Commercial/Commands/ReconciliationPackageEndpoints.cs` (115 lines) into the units its pattern names. 4 functions; the longest is 15 lines.
722. Divide `api/Features/Procurement/Commands/RetagWorkOrderWorkflowTagsHandler.cs` (115 lines) into the units its pattern names. 7 functions; the longest is 27 lines.
723. Divide `jpms/Components/ValuationReportTable.BulkPercent.cs` (115 lines) into the units its pattern names. 4 functions; the longest is 56 lines.
724. Divide `jpms/Features/ValuationInvoices/ValuationInvoiceXeroRaiseModal.razor.cs` (115 lines) into the units its pattern names. 8 functions; the longest is 18 lines.
725. Divide `api/Data/Entities/ValuationReportEntities.cs` (114 lines) into the units its pattern names. 0 functions; the longest is 0 lines.
726. Divide `api/Features/MailboxIntake/Compose/SendMailboxEmailHandler.cs` (114 lines) into the units its pattern names. 2 functions; the longest is 29 lines.
727. Divide `api/Features/Site/Drafts/ProgrammeDraftSuggestionPrompt.cs` (114 lines) into the units its pattern names. 2 functions; the longest is 50 lines.
728. Divide `jpms/Features/Triage/TriageEmailDisplay.cs` (114 lines) into the units its pattern names. 7 functions; the longest is 19 lines.
729. Divide `jpms/Pages/TriageQueue.ApplySends.cs` (114 lines) into the units its pattern names. 4 functions; the longest is 46 lines.
730. Divide `api/Features/Ai/Tools/Actions/LabourAndBackOfficeActions.WorkerLinks.cs` (113 lines) into the units its pattern names. 1 functions; the longest is 104 lines.
731. Divide `api/Features/DocumentControl/Commands/ExtractDocumentControlArchiveHandler.cs` (113 lines) into the units its pattern names. 5 functions; the longest is 53 lines.
732. Divide `jpms/Pages/CashForecast.Statement.cs` (113 lines) into the units its pattern names. 3 functions; the longest is 59 lines.
733. Divide `jpms/Pages/ProjectVariations.Search.cs` (113 lines) into the units its pattern names. 4 functions; the longest is 25 lines.
734. Divide `api/Data/Entities/BluebeamEntities.cs` (112 lines) into the units its pattern names. 0 functions; the longest is 0 lines.
735. Divide `api/Features/Ai/Tools/AiFinanceTools.LedgerLine.cs` (112 lines) into the units its pattern names. 3 functions; the longest is 51 lines.
736. Divide `api/Features/Labour/Commands/UnapproveTimesheetSlice.cs` (112 lines) into the units its pattern names. 6 functions; the longest is 41 lines.
737. Divide `contracts/Labour/WorkerWeekApproval.cs` (112 lines) into the units its pattern names. 0 functions; the longest is 0 lines.
738. Divide `contracts/Models/Todo.cs` (112 lines) into the units its pattern names. 3 functions; the longest is 31 lines.
739. Divide `jpms/Components/WorkOrderForm.razor.cs` (112 lines) into the units its pattern names. 6 functions; the longest is 17 lines.
740. Divide `api/Features/Bluebeam/BluebeamClient.cs` (111 lines) into the units its pattern names. 6 functions; the longest is 34 lines.
741. Divide `api/Features/Requests/Commands/MailboxMoveCommandHandlers.cs` (111 lines) into the units its pattern names. 7 functions; the longest is 23 lines.
742. Divide `api/Features/Xero/XeroClient.Attachments.cs` (111 lines) into the units its pattern names. 3 functions; the longest is 35 lines.
743. Divide `api/Features/MailboxIntake/Compose/SendMailboxEmailHandler.Draft.cs` (110 lines) into the units its pattern names. 5 functions; the longest is 25 lines.
744. Divide `contracts/Commercial/Export/ValuationReportExportWorkbook.cs` (110 lines) into the units its pattern names. 3 functions; the longest is 28 lines.
745. Divide `jpms/Features/Triage/Panels/RecordExplorerPane.razor.cs` (110 lines) into the units its pattern names. 5 functions; the longest is 17 lines.
746. Divide `jpms/Pages/ProjectBidPackageInviteDetail.Lines.cs` (110 lines) into the units its pattern names. 5 functions; the longest is 19 lines.
747. Divide `api/Data/Entities/LabourEntities.cs` (109 lines) into the units its pattern names. 0 functions; the longest is 0 lines.
748. Divide `api/Data/Entities/ProgressEntities.cs` (109 lines) into the units its pattern names. 0 functions; the longest is 0 lines.
749. Divide `api/Features/Ai/Scans/PngEncoder.cs` (109 lines) into the units its pattern names. 7 functions; the longest is 20 lines.
750. Divide `api/Features/Progress/ContractorsReports/Documents/ContractorsReportWordRenderer.Sections.cs` (109 lines) into the units its pattern names. 7 functions; the longest is 14 lines.
751. Divide `api/Features/Progress/ContractorsReports/Documents/ContractorsReportWordRenderer.cs` (109 lines) into the units its pattern names. 6 functions; the longest is 24 lines.
752. Divide `api/Features/Progress/ProgressFeatureRegistration.cs` (109 lines) into the units its pattern names. 4 functions; the longest is 49 lines.
753. Divide `api/Features/RecordLinks/Providers/SchedulingLinkProvider.cs` (109 lines) into the units its pattern names. 2 functions; the longest is 44 lines.
754. Divide `api/Features/Requests/Documents/RequestDocumentModel.cs` (109 lines) into the units its pattern names. 1 functions; the longest is 80 lines.
755. Divide `api/Features/Subcontractors/Commands/UploadComplianceDocumentFileEndpoint.cs` (109 lines) into the units its pattern names. 2 functions; the longest is 13 lines.
756. Divide `jpms/Features/Subcontractors/SubcontractorsRouteRegistration.cs` (109 lines) into the units its pattern names. 2 functions; the longest is 94 lines.
757. Divide `jpms/Pages/TriageQueue.Decisions.cs` (109 lines) into the units its pattern names. 3 functions; the longest is 10 lines.
758. Divide `api/Data/Entities/ConnectEntities.cs` (108 lines) into the units its pattern names. 0 functions; the longest is 0 lines.
759. Divide `api/Features/Ai/Sources/AiSourceReader.cs` (108 lines) into the units its pattern names. 5 functions; the longest is 34 lines.
760. Divide `api/Features/Commercial/VariationClaimRespread.cs` (108 lines) into the units its pattern names. 1 functions; the longest is 75 lines.
761. Divide `api/Features/Procurement/Documents/WorkOrderPoRenderer.Scope.cs` (108 lines) into the units its pattern names. 6 functions; the longest is 22 lines.
762. Divide `api/Features/Requests/Commands/RaiseRequestHandler.cs` (108 lines) into the units its pattern names. 2 functions; the longest is 82 lines.
763. Divide `api/Features/Requests/Documents/RequestDocumentRenderer.Helpers.cs` (108 lines) into the units its pattern names. 8 functions; the longest is 15 lines.
764. Divide `api/Features/Sales/Queries/SalesQueryHandlers.cs` (108 lines) into the units its pattern names. 4 functions; the longest is 32 lines.
765. Divide `api/Features/Variations/Commands/ReviseVariationOrderValueHandler.cs` (108 lines) into the units its pattern names. 2 functions; the longest is 83 lines.
766. Divide `jpms/Components/WorkOrderForm.Draft.cs` (108 lines) into the units its pattern names. 4 functions; the longest is 24 lines.
767. Divide `jpms/Pages/SalesEstimateDetail.razor.cs` (108 lines) into the units its pattern names. 8 functions; the longest is 17 lines.
768. Divide `jpms/Services/HttpCloseoutStore.cs` (108 lines) into the units its pattern names. 8 functions; the longest is 12 lines.
769. Divide `api/Features/Connect/RegisterClientEndpoint.cs` (107 lines) into the units its pattern names. 4 functions; the longest is 62 lines.
770. Divide `api/Features/Procurement/Commands/SetBidPackageLineItemCoverageHandler.cs` (107 lines) into the units its pattern names. 1 functions; the longest is 90 lines.
771. Divide `jpms/Features/Cvr/ProfitDisplay.cs` (107 lines) into the units its pattern names. 11 functions; the longest is 12 lines.
772. Divide `api/Features/DocumentControl/Storage/AzureBlobDocumentControlStore.cs` (106 lines) into the units its pattern names. 8 functions; the longest is 17 lines.
773. Divide `api/Features/Progress/ContractorsReports/Documents/ContractorsReportWordTables.cs` (106 lines) into the units its pattern names. 7 functions; the longest is 23 lines.
774. Divide `api/Features/ProjectContracts/Commands/UploadProjectContractDocumentEndpoint.cs` (106 lines) into the units its pattern names. 2 functions; the longest is 15 lines.
775. Divide `api/Features/Sales/Commands/ImagineHandlers.cs` (106 lines) into the units its pattern names. 4 functions; the longest is 26 lines.
776. Divide `api/Features/Xero/XeroClient.SalesInvoices.cs` (106 lines) into the units its pattern names. 3 functions; the longest is 49 lines.
777. Divide `contracts/Models/SalesProposal.cs` (106 lines) into the units its pattern names. 1 functions; the longest is 9 lines.
778. Divide `jpms/Features/Directory/ConsolidateRecordsModal.razor.cs` (106 lines) into the units its pattern names. 3 functions; the longest is 34 lines.
779. Divide `jpms/Features/Directory/XeroImportModal.razor.cs` (106 lines) into the units its pattern names. 6 functions; the longest is 16 lines.
780. Divide `jpms/Pages/WeeklyCashflow.Moving.cs` (106 lines) into the units its pattern names. 5 functions; the longest is 26 lines.
781. Divide `api/Features/Ai/Tools/AiToolCatalogue.RequestContext.cs` (105 lines) into the units its pattern names. 1 functions; the longest is 92 lines.
782. Divide `api/Features/MailboxIntake/Compose/SendMailboxEmailHandler.Outcome.cs` (105 lines) into the units its pattern names. 6 functions; the longest is 26 lines.
783. Divide `api/Features/Portal/Commands/UploadMyComplianceDocumentEndpoint.cs` (105 lines) into the units its pattern names. 2 functions; the longest is 68 lines.
784. Divide `api/Features/Procurement/Documents/WorkOrderPoRenderer.Lines.cs` (105 lines) into the units its pattern names. 6 functions; the longest is 27 lines.
785. Divide `api/Features/Sales/Commands/EstimateGates.cs` (105 lines) into the units its pattern names. 5 functions; the longest is 27 lines.
786. Divide `api/Features/Sales/Research/StrategyResearchRunner.cs` (105 lines) into the units its pattern names. 2 functions; the longest is 69 lines.
787. Divide `api/Features/Xero/Ledger/WorkOrderBillRecognition.cs` (105 lines) into the units its pattern names. 4 functions; the longest is 15 lines.
788. Divide `jpms/Features/RecordLinks/RecordLinksRouteRegistration.cs` (105 lines) into the units its pattern names. 2 functions; the longest is 90 lines.
789. Divide `api/Features/Ai/Tools/AiDeliveryTools.DocumentData.cs` (104 lines) into the units its pattern names. 3 functions; the longest is 42 lines.
790. Divide `api/Features/DocumentControl/Commands/SendAttachmentsToDocumentControlHandler.cs` (104 lines) into the units its pattern names. 2 functions; the longest is 72 lines.
791. Divide `api/Features/Xero/Ledger/WorkOrderBills/UndoWorkOrderBillApprovalHandler.cs` (104 lines) into the units its pattern names. 4 functions; the longest is 43 lines.
792. Divide `contracts/Models/Closeout.cs` (104 lines) into the units its pattern names. 2 functions; the longest is 40 lines.
793. Divide `jpms/Features/Drawings/DrawingsRouteRegistration.cs` (104 lines) into the units its pattern names. 2 functions; the longest is 91 lines.
794. Divide `api/Features/Drawings/Commands/UploadDrawingRevisionEndpoint.cs` (103 lines) into the units its pattern names. 2 functions; the longest is 17 lines.
795. Divide `api/Features/Labour/Queries/ListLabourSettlementForProjectSlice.cs` (103 lines) into the units its pattern names. 3 functions; the longest is 71 lines.
796. Divide `api/Features/Progress/SitePhotos/SitePhotoFiler.cs` (103 lines) into the units its pattern names. 5 functions; the longest is 25 lines.
797. Divide `api/Features/RecordLinks/Providers/VariationOrderQuoteLinkProvider.cs` (103 lines) into the units its pattern names. 5 functions; the longest is 23 lines.
798. Divide `api/Features/Requests/RequestsEntityMapping.cs` (103 lines) into the units its pattern names. 2 functions; the longest is 52 lines.
799. Divide `api/Features/Variations/Documents/VariationDocumentCostBreakdown.cs` (103 lines) into the units its pattern names. 2 functions; the longest is 75 lines.
800. Divide `contracts/BuildingControl/BuildingControlCommands.cs` (103 lines) into the units its pattern names. 0 functions; the longest is 0 lines.
801. Divide `api/Data/Entities/ProjectContractEntities.cs` (102 lines) into the units its pattern names. 0 functions; the longest is 0 lines.
802. Divide `api/Features/Ai/Tools/Actions/KpiActions.cs` (102 lines) into the units its pattern names. 1 functions; the longest is 79 lines.
803. Divide `api/Features/Drawings/DrawingRevisionLanding.cs` (102 lines) into the units its pattern names. 3 functions; the longest is 37 lines.
804. Divide `api/Features/Procurement/Commands/CreateManualWorkOrderHandler.cs` (102 lines) into the units its pattern names. 1 functions; the longest is 80 lines.
805. Divide `api/Features/Progress/ContractorsReports/Documents/ContractorsReportWordParts.cs` (102 lines) into the units its pattern names. 8 functions; the longest is 14 lines.
806. Divide `jpms/Pages/ProjectLabour.Corrections.cs` (102 lines) into the units its pattern names. 5 functions; the longest is 25 lines.
807. Divide `api/Features/Ai/Tools/Actions/CommercialActions.cs` (101 lines) into the units its pattern names. 1 functions; the longest is 15 lines.
808. Divide `api/Features/Ai/Tools/Actions/SiteAndProgressActions.ToDos.cs` (101 lines) into the units its pattern names. 1 functions; the longest is 82 lines.
809. Divide `api/Features/Ai/Tools/AiDeliveryTools.Programme.cs` (101 lines) into the units its pattern names. 4 functions; the longest is 26 lines.
810. Divide `api/Features/RecordLinks/Providers/DefectLinkProvider.cs` (101 lines) into the units its pattern names. 4 functions; the longest is 27 lines.
811. Divide `api/Features/Site/Drafts/ProgrammeDraftCapture.cs` (101 lines) into the units its pattern names. 2 functions; the longest is 63 lines.
812. Divide `api/Features/ValuationInvoices/Commands/CreateValuationInvoiceHandler.cs` (101 lines) into the units its pattern names. 1 functions; the longest is 73 lines.
813. Divide `contracts/Commercial/Export/ValuationExportStatementSheet.cs` (101 lines) into the units its pattern names. 5 functions; the longest is 23 lines.
814. Divide `contracts/Commercial/ListWorkOrderInvoiceSummaries.cs` (101 lines) into the units its pattern names. 1 functions; the longest is 16 lines.
815. Divide `jpms/Components/ValuationInvoicesSection.razor.cs` (101 lines) into the units its pattern names. 1 functions; the longest is 11 lines.

**Pass 5 — The sweep to zero**

816. Explanatory comment lines: 16116 to zero. Scores 0.0% at weight 4; the offenders are in audit.json under details.comments, fifty at a time.
817. Functions over the line limit: 827 to zero. Scores 59.6% at weight 8; the offenders are in audit.json under details.functionShape, fifty at a time.
818. Deeply indented lines: 3078 to zero. Scores 62.8% at weight 4; the offenders are in audit.json under details.prose, fifty at a time.
819. Else blocks: 1177 to zero. Scores 80.4% at weight 5; the offenders are in audit.json under details.functionShape, fifty at a time.
820. Duplication %: 2.34 to zero. Scores 88.3% at weight 8; the offenders are in audit.json under details.duplication, fifty at a time.
821. Conditions with calls tangled inside calls: 278 to zero. Scores 90.7% at weight 8; the offenders are in audit.json under details.conditions, fifty at a time.
822. Long member chain lines: 754 to zero. Scores 90.9% at weight 4; the offenders are in audit.json under details.prose, fifty at a time.
823. Conditions compared to a raw literal: 246 to zero. Scores 91.8% at weight 6; the offenders are in audit.json under details.conditions, fifty at a time.
824. Overlong function names: 53 to zero. Scores 93.5% at weight 4; the offenders are in audit.json under details.functionNames, fifty at a time.
825. Accessor names that want to be a property: 34 to zero. Scores 95.8% at weight 6; the offenders are in audit.json under details.accessorNames, fifty at a time.
826. Inline magic values: 134 to zero. Scores 97.6% at weight 4; the offenders are in audit.json under details.magicValues, fifty at a time.

## The detail behind the first targets

### `api/Data/JpmsContext.Model.cs` — 553 lines

| Function | Line | Lines | Imported by | Same body also in |
| --- | --- | --- | --- | --- |
| OnModelCreating | 21 | 532 | — | — |

### `api/Features/Sales/Documents/EstimateDocumentRenderer.cs` — 507 lines

| Function | Line | Lines | Imported by | Same body also in |
| --- | --- | --- | --- | --- |
| Render | 27 | 38 | — | — |
| FileName | 68 | 9 | — | — |
| AddCover | 80 | 35 | — | — |
| AddProjectBlock | 118 | 23 | — | — |
| Labelled | 142 | 12 | — | — |
| CompanyBlock | 155 | 10 | — | — |
| AddNarrative | 168 | 40 | — | — |
| Prose | 210 | 25 | — | — |
| AddChart | 238 | 50 | — | — |
| AddSections | 291 | 73 | — | — |
| AddGrandTotal | 365 | 51 | — | — |
| AddContactPage | 419 | 21 | — | — |
| ContactLine | 441 | 10 | — | — |
| SectionTitle | 454 | 9 | — | — |
| SubHeading | 464 | 10 | — | — |
| AddColumn | 475 | 5 | — | — |
| Cell | 481 | 7 | — | — |
| PropertyLine | 489 | 7 | — | — |
| ClientLine | 497 | 6 | — | — |

### `jpms/Services/Navigation/SidebarFolders.cs` — 452 lines

| Function | Line | Lines | Imported by | Same body also in |
| --- | --- | --- | --- | --- |
| SidebarFolderInfo | 45 | 408 | — | — |

### `jpms/Components/ProjectDetailsEditor.razor` — 440 lines

| Block opens with | Lines | Range | Functions that move with it |
| --- | --- | --- | --- |
| `<div>` | 25 | 41–65 | OnPartyChanged |
| `@if (partySelection.StartsWith(ArchitectPrefix, StringComparison.Ordinal))` | 14 | 67–80 | OnOnBehalfOfClientChanged |
| `<div class="grid grid-cols-1 sm:grid-cols-2 gap-4">` | 22 | 82–103 | OnOrganisationChanged |
| `<FormField Label="Xero contact"` | 24 | 134–157 | OnXeroContactChanged |
| `@if (CanDelete)` | 39 | 159–197 | DisarmDelete, DeleteAsync |

| Function | Line | Lines | Imported by | Same body also in |
| --- | --- | --- | --- | --- |
| Open | 262 | 31 | — | — |
| LoadPartiesAsync | 294 | 6 | — | — |
| LoadXeroContactsAsync | 305 | 21 | — | — |
| OnXeroContactChanged | 327 | 8 | — | — |
| OnPartyChanged | 336 | 5 | — | — |
| OnOnBehalfOfClientChanged | 342 | 10 | — | — |
| DisarmDelete | 355 | 5 | — | — |
| DeleteAsync | 361 | 20 | — | — |
| OnOrganisationChanged | 382 | 58 | — | — |

### `jpms/Services/HttpLabourStore.cs` — 436 lines

| Function | Line | Lines | Imported by | Same body also in |
| --- | --- | --- | --- | --- |
| HttpLabourStore | 32 | 28 | — | — |
| Workers | 63 | 5 | — | — |
| LoadWorkersAsync | 69 | 5 | — | — |
| AddWorkerAsync | 79 | 8 | — | — |
| UpdateWorkerAsync | 88 | 8 | — | — |
| SetWorkerSettlementIdentityAsync | 97 | 7 | — | — |
| ReconcileWorkerLinksAsync | 105 | 6 | — | — |
| DismissChaseDayAsync | 112 | 8 | — | — |
| AssignmentsFor | 121 | 5 | — | — |
| SetAssignmentAsync | 131 | 5 | — | — |
| MyDay | 137 | 5 | — | — |
| LoadMyDayAsync | 143 | 5 | — | — |
| MySignInAsync | 151 | 5 | — | — |
| MySignOutAsync | 157 | 5 | — | — |
| MyResubmitAsync | 163 | 5 | — | — |
| TimesheetsFor | 169 | 5 | — | — |
| AttendanceFor | 179 | 5 | — | — |
| AddWorkerTimesheetAsync | 189 | 6 | — | — |
| SubmitWorkerWeekAsync | 196 | 11 | — | — |
| AdjustTimesheetAsync | 208 | 6 | — | — |
| ApproveTimesheetsAsync | 215 | 8 | — | — |
| RejectTimesheetAsync | 224 | 6 | — | — |
| UnapproveTimesheetAsync | 231 | 8 | — | — |
| MoveTimesheetAsync | 240 | 12 | — | — |
| Overview | 253 | 6 | — | — |
| SetWorkerContractAsync | 264 | 5 | — | — |
| SetWorkerCisStatusAsync | 270 | 5 | — | — |
| RecordAbsenceAsync | 276 | 5 | — | — |
| RecordAbsenceRangeAsync | 282 | 24 | — | — |
| RemoveAbsenceAsync | 307 | 5 | — | — |
| SignOffWeekAsync | 315 | 5 | — | — |
| RemoveWeekSignOffAsync | 321 | 5 | — | — |
| MonthStartOf | 327 | 9 | — | — |
| AddSettlementLineAsync | 341 | 5 | — | — |
| RemoveSettlementLineAsync | 347 | 5 | — | — |
| XeroMappings | 353 | 5 | — | — |
| LoadMappingsAsync | 359 | 5 | — | — |
| SetSiteXeroMappingAsync | 367 | 5 | — | — |
| SetCostCodeXeroMappingAsync | 373 | 5 | — | — |
| RunXeroCodingAsync | 379 | 7 | — | — |
| ResetXeroCodingOutcomeAsync | 387 | 5 | — | — |
| ApproveLabourBillAsync | 393 | 6 | — | — |
| SettlementFor | 400 | 5 | — | — |
| SetTimesheetCoverAsync | 410 | 5 | — | — |
| SetTimesheetCoverForMonthAsync | 421 | 9 | — | — |
| LoadAsync | 431 | 5 | — | — |

### `jpms/Pages/XeroAllocation.razor` — 433 lines

| Block opens with | Lines | Range | Functions that move with it |
| --- | --- | --- | --- |
| `<div class="mb-4 flex items-center justify-between gap-4 flex-wrap">` | 38 | 66–103 | — |
| `@if (activeTab == XeroAllocationStatus.Unallocated && workOrderBillsTab)` | 16 | 162–177 | — |
| `@foreach (var line in Paged)` | 79 | 185–263 | — |
| `<div class="mt-3 flex items-center justify-between gap-4 flex-wrap">` | 22 | 280–301 | — |
| `<Modal IsOpen="@(splitLine is not null && viewLine is null)" Title="Split across` | 14 | 309–322 | — |
| `<SendLinesModal IsOpen="@sendToCostCentreOpen" Title="Send to cost centre"` | 13 | 328–340 | — |
| `<SendLinesModal IsOpen="@sendToProjectOpen" Title="Send to project"` | 13 | 344–356 | — |
| `<Modal IsOpen="@(viewLine is not null)" Title="Invoice document" ShowFooter="fal` | 52 | 380–431 | — |

### `jpms/Pages/AdminKpis.razor` — 429 lines

| Block opens with | Lines | Range | Functions that move with it |
| --- | --- | --- | --- |
| `@if (personFilter == "")` | 14 | 88–101 | — |
| `<RecordsTable Dense="true" MaxHeight="">` | 66 | 102–167 | OpenInControlCentre, StartEdit, StartRemove, ConfirmRemoveAsync |
| `<Modal IsOpen="editing is not null"` | 27 | 178–204 | SaveEditAsync |
| `<Modal IsOpen="addingPerson"` | 24 | 206–229 | AddPersonAsync |

| Function | Line | Lines | Imported by | Same body also in |
| --- | --- | --- | --- | --- |
| OpenInControlCentre | 271 | 9 | — | — |
| StartAddPerson | 281 | 7 | — | — |
| AddPersonAsync | 289 | 16 | — | — |
| StartEdit | 306 | 10 | — | — |
| SaveEditAsync | 317 | 19 | — | — |
| StartRemove | 337 | 6 | — | — |
| ConfirmRemoveAsync | 344 | 18 | — | — |
| OnInitializedAsync | 363 | 19 | — | — |
| OnSessionChanged | 383 | 11 | — | `jpms/Pages/AdminTrades.razor` |
| FetchAsync | 395 | 14 | — | — |
| RevalidateAsync | 410 | 5 | — | `jpms/Pages/AdminTrades.razor` |
| FetchAndRepaintAsync | 416 | 5 | — | `jpms/Pages/AdminTrades.razor` |
| Dispose | 422 | 7 | — | — |

### `jpms/Components/ManualWorkOrderModal.razor.cs` — 428 lines

| Function | Line | Lines | Imported by | Same body also in |
| --- | --- | --- | --- | --- |
| Dispose | 75 | 3 | — | — |
| OnParametersSetAsync | 79 | 37 | — | — |
| ToggleSalesLine | 146 | 7 | — | — |
| SaveAsync | 188 | 137 | — | — |
| ConfirmSaleWarningAsync | 326 | 6 | — | — |
| FindUncoveredCostCentres | 338 | 12 | — | — |
| OnAttachmentFilesSelected | 351 | 5 | — | — |
| LoadExistingAttachmentsAsync | 357 | 6 | — | — |
| RemoveExistingAttachmentAsync | 364 | 12 | — | — |
| UploadStagedAttachmentsAsync | 379 | 21 | — | — |
| FormatBytes | 401 | 2 | — | — |
| Ref | 407 | 11 | — | — |
| DescriptionFor | 420 | 9 | — | — |

### `jpms/Pages/ProjectValuation.razor` — 420 lines

| Block opens with | Lines | Range | Functions that move with it |
| --- | --- | --- | --- |
| `<SectionHeader Title="Valuation Report">` | 52 | 23–74 | — |
| `<div class="flex items-start justify-between gap-4 flex-wrap">` | 37 | 110–146 | — |
| `<ol class="flex items-center gap-1.5 text-xs flex-wrap">` | 15 | 148–162 | — |
| `@switch (StageFor(s))` | 80 | 165–244 | — |
| `<ValuationReportTable ProjectId="@ProjectId" SelectedClaim="@Selected" PreviousC` | 20 | 267–286 | — |
| `<Modal IsOpen="@showStartClaim"` | 52 | 314–365 | — |
| `<Modal IsOpen="@showRename"` | 14 | 367–380 | — |
| `<Modal IsOpen="@showDeleteClaim"` | 17 | 382–398 | — |
| `<Modal IsOpen="@showConfirmNudge"` | 18 | 400–417 | — |

### `jpms/Pages/SubcontractorDetail.razor` — 419 lines

| Block opens with | Lines | Range | Functions that move with it |
| --- | --- | --- | --- |
| `<PageHeader>` | 44 | 41–84 | — |
| `<div class="panel p-4 mb-6">` | 50 | 101–150 | — |
| `<div class="panel p-4 mb-6">` | 45 | 154–198 | — |
| `<div class="panel p-4 mb-6">` | 77 | 203–279 | — |
| `<div class="panel p-4 mb-6">` | 26 | 283–308 | — |
| `<div class="panel p-4 mb-6">` | 19 | 313–331 | — |
| `<Modal IsOpen="editOpen" Title="Edit company details" ShowFooter="true"` | 73 | 344–416 | — |
