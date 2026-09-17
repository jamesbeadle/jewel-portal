# Refactoring plan

Written by the code quality check at a score of 75.3%. These are the steps a refactor of this repository follows, in this order; a round takes the next steps from the top. The plan is measured, so a finished step is gone the next time the check runs. The facts are measured; the judgement is the round's.

## The order

**Pass 1 — Component breakout**

1. Break `jpms/Components/ProjectDetailsEditor.razor` (440 lines) into components. Component-sized blocks: lines 41–65 (25 lines, taking OnPartyChanged); lines 67–80 (14 lines, taking OnOnBehalfOfClientChanged); lines 82–103 (22 lines, taking OnOrganisationChanged); lines 134–157 (24 lines, taking OnXeroContactChanged).
2. Break `jpms/Pages/XeroAllocation.razor` (435 lines) into components. Component-sized blocks: lines 66–103 (38 lines); lines 162–177 (16 lines); lines 191–209 (19 lines); lines 212–232 (21 lines).
3. Break `jpms/Pages/AdminKpis.razor` (429 lines) into components. Component-sized blocks: lines 88–101 (14 lines); lines 102–167 (66 lines, taking OpenInControlCentre, StartEdit, StartRemove, ConfirmRemoveAsync); lines 178–204 (27 lines, taking SaveEditAsync); lines 206–229 (24 lines, taking AddPersonAsync).
4. Break `jpms/Pages/SubcontractorDetail.razor` (419 lines) into components. Component-sized blocks: lines 41–84 (44 lines); lines 101–150 (50 lines); lines 154–198 (45 lines); lines 203–279 (77 lines).
5. Break `jpms/Pages/ProjectVariations.razor` (417 lines) into components. Component-sized blocks: lines 27–47 (21 lines); lines 51–76 (26 lines); lines 82–94 (13 lines); lines 96–113 (18 lines).
6. Break `jpms/Pages/CostCodes.razor` (415 lines) into components. Component-sized blocks: lines 18–49 (32 lines); lines 103–139 (37 lines); lines 177–196 (20 lines); lines 219–275 (57 lines).
7. Break `jpms/Pages/ProjectValuation.razor` (405 lines) into components. Component-sized blocks: lines 23–74 (52 lines); lines 110–126 (17 lines); lines 128–142 (15 lines); lines 145–224 (80 lines).
8. Break `jpms/Pages/TriageQueue.razor` (403 lines) into components. Component-sized blocks: lines 53–75 (23 lines); lines 78–90 (13 lines); lines 93–123 (31 lines); lines 127–144 (18 lines).
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
19. Break `jpms/Components/ValuationInvoicesSection.razor` (347 lines) into components. Component-sized blocks: lines 19–58 (40 lines); lines 86–99 (14 lines); lines 103–157 (55 lines); lines 166–204 (39 lines).
20. Break `jpms/Components/VariationApprovePanel.razor` (347 lines) into components. Component-sized blocks: lines 12–88 (77 lines, taking AddLine, RemoveLine).
21. Break `jpms/Pages/Todos.razor` (342 lines) into components. Component-sized blocks: lines 52–67 (16 lines); lines 106–119 (14 lines); lines 142–153 (12 lines); lines 155–170 (16 lines).
22. Break `jpms/Pages/Registers.razor` (335 lines) into components. Component-sized blocks: lines 52–102 (51 lines, taking DeactivateAsync, DueCell); lines 104–146 (43 lines, taking SaveAsync, NamePlaceholder).
23. Break `jpms/Pages/ProjectBuildingControl.razor` (334 lines) into components. Component-sized blocks: lines 42–58 (17 lines); lines 62–88 (27 lines); lines 89–110 (22 lines); lines 117–155 (39 lines).
24. Break `jpms/Pages/ProjectWorkOrderAllocation.razor` (331 lines) into components. Component-sized blocks: lines 24–41 (18 lines); lines 71–82 (12 lines); lines 83–160 (78 lines, taking InvoicingPill); lines 174–186 (13 lines).
25. Break `jpms/Pages/AdminTrades.razor` (323 lines) into components. Component-sized blocks: lines 77–143 (67 lines, taking UsageCount, StartRename, ConfirmRenameAsync, StartDelete, ConfirmDeleteAsync).
26. Break `jpms/Pages/AgedPayables.razor` (322 lines) into components. Component-sized blocks: lines 20–35 (16 lines, taking ForceRefreshAsync); lines 64–80 (17 lines); lines 111–173 (63 lines, taking BucketCellClass).
27. Break `jpms/Pages/WorkOrderPo.razor` (322 lines) into components. Component-sized blocks: lines 33–85 (53 lines, taking OpenEmailModal); lines 125–181 (57 lines); lines 183–198 (16 lines).
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
50. Break `jpms/Pages/ProjectRequests.razor` (266 lines) into components. Component-sized blocks: lines 25–44 (20 lines); lines 58–95 (38 lines); lines 99–122 (24 lines); lines 124–137 (14 lines).
51. Break `jpms/Components/DrawingsTable.razor` (265 lines) into components. Component-sized blocks: lines 33–97 (65 lines, taking HasSubFolders, ToggleCollapse).
52. Break `jpms/Layout/SideNav.razor` (264 lines) into components. Component-sized blocks: lines 25–39 (15 lines); lines 51–130 (80 lines); lines 132–157 (26 lines); lines 160–213 (54 lines).
53. Break `jpms/Features/Triage/Queue/TriageMessageDetail.razor` (263 lines) into components. Component-sized blocks: lines 9–35 (27 lines); lines 41–97 (57 lines, taking ToggleAllDocumentTriage); lines 103–120 (18 lines); lines 124–137 (14 lines).
54. Break `jpms/Features/Requests/RequestOfficialFormPanel.razor` (261 lines) into components. Component-sized blocks: lines 9–28 (20 lines); lines 35–97 (63 lines); lines 100–171 (72 lines, taking Cancel, SaveAsync).
55. Break `jpms/Pages/ProjectSiteInstructions.razor` (261 lines) into components. Component-sized blocks: lines 40–72 (33 lines, taking SaveAsync); lines 88–142 (55 lines, taking StartEdit, ToggleEmails).
56. Break `jpms/Pages/ProjectVariationDetail.razor` (260 lines) into components. Component-sized blocks: lines 91–107 (17 lines); lines 121–152 (32 lines); lines 154–208 (55 lines); lines 217–239 (23 lines).
57. Break `jpms/Pages/Workers.razor` (259 lines) into components. Component-sized blocks: lines 63–105 (43 lines); lines 114–182 (69 lines); lines 189–257 (69 lines).
58. Break `jpms/Pages/ProjectBidPackageInviteDetail.razor` (258 lines) into components. Component-sized blocks: lines 44–77 (34 lines); lines 97–110 (14 lines); lines 124–145 (22 lines); lines 161–173 (13 lines).
59. Break `jpms/Pages/ProjectCommunications.razor` (257 lines) into components. Component-sized blocks: lines 42–75 (34 lines); lines 83–98 (16 lines); lines 109–123 (15 lines); lines 150–170 (21 lines).
60. Break `jpms/Features/Site/Programme/ProgrammeWorkbench.razor` (256 lines) into components. Component-sized blocks: lines 21–58 (38 lines); lines 68–79 (12 lines); lines 88–114 (27 lines); lines 117–152 (36 lines).
61. Break `jpms/Pages/AiSkillsAdmin.razor` (256 lines) into components. Component-sized blocks: lines 32–104 (73 lines); lines 107–120 (14 lines); lines 132–188 (57 lines); lines 190–253 (64 lines).
62. Break `jpms/Pages/ProjectDrawingDetail.razor` (255 lines) into components. Component-sized blocks: lines 16–32 (17 lines); lines 35–54 (20 lines); lines 57–113 (57 lines); lines 114–145 (32 lines).
63. Break `jpms/Pages/ProjectCalendar.razor` (254 lines) into components. Component-sized blocks: lines 25–36 (12 lines); lines 54–116 (63 lines); lines 118–160 (43 lines); lines 168–238 (71 lines).
64. Break `jpms/Pages/ProjectRequestDetail.razor` (254 lines) into components. Component-sized blocks: lines 75–124 (50 lines); lines 126–163 (38 lines); lines 187–202 (16 lines); lines 215–234 (20 lines).
65. Break `jpms/Components/RequestConversation.razor` (250 lines) into components. Component-sized blocks: lines 15–32 (18 lines); lines 48–71 (24 lines); lines 74–147 (74 lines); lines 173–214 (42 lines).
66. Break `jpms/Pages/ProjectBuildingControlInspection.razor` (248 lines) into components. Component-sized blocks: lines 43–56 (14 lines); lines 64–107 (44 lines); lines 110–139 (30 lines); lines 142–175 (34 lines).
67. Break `jpms/Pages/SalesLeads.razor` (248 lines) into components. Component-sized blocks: lines 25–37 (13 lines); lines 66–78 (13 lines, taking ChipClass); lines 93–146 (54 lines).
68. Break `jpms/Features/Triage/Panels/PathwayPane.razor` (246 lines) into components. Component-sized blocks: lines 45–57 (13 lines); lines 61–75 (15 lines); lines 91–127 (37 lines); lines 139–187 (49 lines).
69. Break `jpms/Components/WorkOrderLineRecodeModal.razor` (245 lines) into components. Component-sized blocks: lines 9–72 (64 lines, taking SetRowCode, SaveAsync).
70. Break `jpms/Pages/AdminIntegrations.razor` (244 lines) into components. Component-sized blocks: lines 98–120 (23 lines, taking DisconnectAsync).
71. Break `jpms/Features/Procurement/TenderSubmissionsSection.razor` (242 lines) into components. Component-sized blocks: lines 28–81 (54 lines); lines 83–116 (34 lines); lines 124–156 (33 lines); lines 159–171 (13 lines).
72. Break `jpms/Pages/XeroTransactions.razor` (242 lines) into components. Component-sized blocks: lines 12–28 (17 lines); lines 98–173 (76 lines); lines 177–191 (15 lines); lines 193–238 (46 lines).
73. Break `jpms/Pages/ProjectBidPackageInvites.razor` (241 lines) into components. Component-sized blocks: lines 13–76 (64 lines); lines 79–121 (43 lines); lines 130–148 (19 lines); lines 165–178 (14 lines).
74. Break `jpms/Components/RequestTable.razor` (240 lines) into components. Component-sized blocks: lines 4–26 (23 lines); lines 27–102 (76 lines, taking ToggleStatusMenu, Date); lines 110–133 (24 lines, taking PickStatus, StatusHint).
75. Break `jpms/Features/Sales/ImagineProposal.razor` (238 lines) into components. Component-sized blocks: lines 22–33 (12 lines); lines 43–78 (36 lines, taking Toggle); lines 80–99 (20 lines, taking BarStyle); lines 109–150 (42 lines, taking AcceptAsync, DeclineAsync).
76. Break `jpms/Pages/Architects.razor` (237 lines) into components. Component-sized blocks: lines 11–30 (20 lines, taking OpenCreate, BuildExportWorkbook); lines 48–71 (24 lines, taking OpenContacts, OpenEdit); lines 77–94 (18 lines, taking CreateArchitectAsync); lines 96–113 (18 lines, taking SaveEditAsync).
77. Break `jpms/Components/UsefulInformationPanel.razor` (234 lines) into components. Component-sized blocks: lines 17–87 (71 lines, taking Matches, OpenAdd, OpenEdit); lines 91–119 (29 lines, taking Save).
78. Break `jpms/Features/Site/Programme/RelevantEventsList.razor` (233 lines) into components. Component-sized blocks: lines 17–29 (13 lines); lines 47–77 (31 lines); lines 82–96 (15 lines, taking ToggleEmailAsync, ToggleReply); lines 97–117 (21 lines, taking CreateReplyDraftAsync).
79. Break `jpms/Pages/PaymentCertificates.razor` (233 lines) into components. Component-sized blocks: lines 43–120 (78 lines, taking TogglePreview, IsPdf).
80. Break `jpms/Components/NextValuationDateEditor.razor` (231 lines) into components. Component-sized blocks: lines 54–109 (56 lines).
81. Break `jpms/Components/ProjectRetentionPanel.razor` (231 lines) into components. Component-sized blocks: lines 56–72 (17 lines); lines 76–88 (13 lines); lines 90–106 (17 lines); lines 108–143 (36 lines).
82. Break `jpms/Pages/AiActionsAdmin.razor` (231 lines) into components. Component-sized blocks: lines 50–77 (28 lines); lines 79–148 (70 lines, taking SkillChip, SkillPicker).
83. Break `jpms/Components/CostCentreCostOfSalesModal.razor` (229 lines) into components. Component-sized blocks: lines 30–44 (15 lines); lines 66–118 (53 lines); lines 119–153 (35 lines); lines 170–207 (38 lines).
84. Break `jpms/Components/SubcontractorStatementModal.razor` (227 lines) into components. Component-sized blocks: lines 35–79 (45 lines, taking MoneyExact); lines 89–101 (13 lines).
85. Break `jpms/Features/Triage/Panels/EmailFinder.razor` (227 lines) into components. Component-sized blocks: lines 42–102 (61 lines, taking Toggle, LinkSelectedAsync, IsAlreadyTagged).
86. Break `jpms/Components/ValuationSnapshotViewer.razor` (226 lines) into components. Component-sized blocks: lines 40–59 (20 lines); lines 67–82 (16 lines); lines 96–172 (77 lines); lines 180–197 (18 lines).
87. Break `jpms/Components/ClaimProgressDialog.razor` (224 lines) into components. Component-sized blocks: lines 10–69 (60 lines, taking AddPicked, Remove).
88. Break `jpms/Components/MyDayWorkspace.razor` (223 lines) into components. Component-sized blocks: lines 33–105 (73 lines, taking TodaysEntriesFor, SignInAsync, SubmitSignOutAsync); lines 108–120 (13 lines); lines 122–141 (20 lines, taking ResubmitHoursFor).
89. Break `jpms/Features/Sales/LeadImaginePanel.razor` (222 lines) into components. Component-sized blocks: lines 15–27 (13 lines, taking IssueAsync); lines 44–65 (22 lines, taking CopyAsync, DownloadAsync); lines 68–137 (70 lines, taking ToneFor, RetryAsync).
90. Break `jpms/Pages/CashForecast.razor` (217 lines) into components. Component-sized blocks: lines 98–119 (22 lines).
91. Break `jpms/Components/DrawingRevisionList.razor` (214 lines) into components. Component-sized blocks: lines 21–83 (63 lines); lines 84–124 (41 lines); lines 136–150 (15 lines, taking DeletePendingAsync).
92. Break `jpms/Components/ValuationSnapshotsSection.razor` (213 lines) into components. Component-sized blocks: lines 9–30 (22 lines, taking BuildExportWorkbook); lines 48–108 (61 lines, taking DeleteAsync); lines 111–126 (16 lines, taking TakeAsync).
93. Break `jpms/Components/WorkOrderForm.razor` (212 lines) into components. Component-sized blocks: lines 24–48 (25 lines); lines 62–88 (27 lines); lines 91–159 (69 lines); lines 162–178 (17 lines).
94. Break `jpms/Features/Triage/Panels/PathwayActionsSection.razor` (212 lines) into components. Component-sized blocks: lines 22–82 (61 lines); lines 84–97 (14 lines); lines 100–111 (12 lines); lines 112–198 (87 lines).
95. Break `jpms/Components/WorkOrderLinkSplitModal.razor` (211 lines) into components. Component-sized blocks: lines 9–75 (67 lines, taking SetRowOrder, SaveAsync).
96. Break `jpms/Components/DrawingUploadForm.razor` (210 lines) into components. Component-sized blocks: lines 50–64 (15 lines); lines 66–138 (73 lines); lines 140–155 (16 lines); lines 157–172 (16 lines).
97. Break `jpms/Components/ProjectContractTermsDialog.razor` (210 lines) into components. Component-sized blocks: lines 24–49 (26 lines); lines 63–77 (15 lines); lines 79–98 (20 lines); lines 100–119 (20 lines).
98. Break `jpms/Components/UnpaidXeroInvoicesModal.razor` (210 lines) into components. Component-sized blocks: lines 38–89 (52 lines); lines 92–125 (34 lines).
99. Break `jpms/Features/Sales/LeadProposalsPanel.razor` (210 lines) into components. Component-sized blocks: lines 38–103 (66 lines, taking ToneFor, OpenSend, WithdrawAsync); lines 110–132 (23 lines).
100. Break `jpms/Components/TodoBoard.razor` (209 lines) into components. Component-sized blocks: lines 48–122 (75 lines, taking StripeClass).
101. Break `jpms/Components/DropdownMenu.razor` (207 lines) into components. Component-sized blocks: lines 21–87 (67 lines, taking Toggle, Run).
102. Break `jpms/Features/Triage/Queue/TaggedInboxBrowser.razor` (207 lines) into components. Component-sized blocks: lines 42–81 (40 lines); lines 105–152 (48 lines).
103. Break `jpms/Features/Triage/Panels/KpiTagSection.razor` (206 lines) into components. Component-sized blocks: lines 20–32 (13 lines); lines 51–115 (65 lines, taking ToggleRow, StagedFor, StageAsync, UnstageAsync).
104. Break `jpms/Features/Sales/StrategyFormModal.razor` (205 lines) into components. Component-sized blocks: lines 74–100 (27 lines).
105. Break `jpms/Components/RequestForm.razor` (201 lines) into components. Component-sized blocks: lines 33–53 (21 lines); lines 79–122 (44 lines); lines 129–153 (25 lines); lines 158–201 (44 lines).
106. Break `jpms/Components/PackageReconciliationSection.razor` (200 lines) into components. Component-sized blocks: lines 11–26 (16 lines); lines 49–61 (13 lines); lines 62–129 (68 lines); lines 134–184 (51 lines).
107. Break `jpms/Components/PartyContactsEditor.razor` (199 lines) into components. Component-sized blocks: lines 30–78 (49 lines, taking ChangeRoutingAsync, MakePrimaryAsync); lines 82–102 (21 lines, taking AddAsync).
108. Break `jpms/Pages/AgentActivityLog.razor` (199 lines) into components. Component-sized blocks: lines 54–102 (49 lines, taking OutcomeLabel, OutcomeClass, FormatDuration); lines 104–117 (14 lines, taking FormatPence).
109. Break `jpms/Pages/ProjectWorkOrders.razor` (199 lines) into components. Component-sized blocks: lines 24–50 (27 lines); lines 77–101 (25 lines); lines 151–168 (18 lines).
110. Break `jpms/Features/Labour/SettlementSchedulesPanel.razor` (198 lines) into components. Component-sized blocks: lines 18–30 (13 lines); lines 57–129 (73 lines, taking BillTitle); lines 137–163 (27 lines, taking PlanPillClass).
111. Break `jpms/Components/ProjectCorrespondencePanel.razor` (197 lines) into components. Component-sized blocks: lines 52–95 (44 lines); lines 109–151 (43 lines); lines 154–194 (41 lines).
112. Break `jpms/Components/SearchSelect.razor` (196 lines) into components. Component-sized blocks: lines 19–55 (37 lines, taking OnInput, OnKeyDown, ItemClass).
113. Break `jpms/Components/ValuationLineForm.razor` (193 lines) into components. Component-sized blocks: lines 7–32 (26 lines); lines 34–45 (12 lines); lines 47–64 (18 lines).
114. Break `jpms/Pages/ProfitSummary.razor` (193 lines) into components. Component-sized blocks: lines 99–120 (22 lines); lines 122–133 (12 lines).
115. Break `jpms/Components/ValuationSnapshotEmailModal.razor` (192 lines) into components. Component-sized blocks: lines 11–90 (80 lines, taking CreateDraftAsync).
116. Break `jpms/Features/Sales/ProposalFormModal.razor` (192 lines) into components. Component-sized blocks: lines 41–63 (23 lines); lines 65–86 (22 lines); lines 94–109 (16 lines).
117. Break `jpms/Components/ProgressReportForm.razor` (191 lines) into components. Component-sized blocks: lines 13–24 (12 lines); lines 47–77 (31 lines, taking Toggle).
118. Break `jpms/Features/Triage/Panels/OutboxPane.razor` (191 lines) into components. Component-sized blocks: lines 22–34 (13 lines, taking StageNewAsync, CancelComposeAsync, DisplayFrom); lines 36–103 (68 lines, taking UpdatedAsync, RemoveAsync).
119. Break `jpms/Pages/ProjectReconciliationAudit.razor` (189 lines) into components. Component-sized blocks: lines 29–102 (74 lines, taking Ago).
120. Break `jpms/Features/Procurement/InvitedSubcontractorsSection.razor` (185 lines) into components. Component-sized blocks: lines 9–34 (26 lines); lines 58–137 (80 lines, taking WebsiteHref, WebsiteLabel).
121. Break `jpms/Pages/ProjectDefectDetail.razor` (185 lines) into components. Component-sized blocks: lines 46–85 (40 lines); lines 92–150 (59 lines); lines 152–181 (30 lines).
122. Break `jpms/Features/Triage/Panels/Actions/StageTodosAction.razor` (183 lines) into components. Component-sized blocks: lines 11–62 (52 lines, taking RemoveTodoAssignee, RemoveTodoRow); lines 75–95 (21 lines).
123. Break `jpms/Features/Triage/Queue/TriageBar.razor` (183 lines) into components. Component-sized blocks: lines 16–65 (50 lines); lines 83–120 (38 lines).
124. Break `jpms/Features/Triage/TodosModal.razor` (182 lines) into components. Component-sized blocks: lines 11–60 (50 lines, taking RemoveRow); lines 73–93 (21 lines).
125. Break `jpms/Features/Triage/Panels/RecordLinkSection.razor` (181 lines) into components. Component-sized blocks: lines 12–44 (33 lines, taking RecordRow, Filter).
126. Break `jpms/Pages/WeeklyCashflow.razor` (181 lines) into components. Component-sized blocks: lines 46–70 (25 lines); lines 132–148 (17 lines); lines 150–165 (16 lines).
127. Break `jpms/Features/Triage/Panels/CategoryRegisterSection.razor` (178 lines) into components. Component-sized blocks: lines 15–66 (52 lines, taking ScopeChipClass, LoadMoreAsync).
128. Break `jpms/Pages/ProjectFinancials.razor` (178 lines) into components. Component-sized blocks: lines 50–62 (13 lines); lines 63–101 (39 lines); lines 144–174 (31 lines).
129. Break `jpms/Pages/ProjectHs.razor` (176 lines) into components. Component-sized blocks: lines 27–38 (12 lines); lines 47–84 (38 lines); lines 88–124 (37 lines); lines 127–145 (19 lines).
130. Break `jpms/Features/Triage/Panels/Actions/StageRequestTransitionAction.razor` (175 lines) into components. Component-sized blocks: lines 12–37 (26 lines, taking StageAsync).
131. Break `jpms/Components/ProgressUpdateForm.razor` (174 lines) into components. Component-sized blocks: lines 38–79 (42 lines).
132. Break `jpms/Features/Cvr/CumulativePositionPanel.razor` (172 lines) into components. Component-sized blocks: lines 14–29 (16 lines); lines 42–74 (33 lines, taking Money); lines 76–125 (50 lines).
133. Break `jpms/Pages/ConnectAuthorize.razor` (172 lines) into components. Component-sized blocks: lines 19–72 (54 lines, taking DecideAsync).
134. Break `jpms/Components/ReconciliationPackageBuilderModal.razor` (171 lines) into components. Component-sized blocks: lines 25–91 (67 lines); lines 96–147 (52 lines).
135. Break `jpms/Components/PdfViewer.razor` (167 lines) into components. Component-sized blocks: lines 19–74 (56 lines).
136. Break `jpms/Features/Closeout/Detail/DefectTodosPanel.razor` (167 lines) into components. Component-sized blocks: lines 13–63 (51 lines, taking OpenAdd); lines 65–91 (27 lines, taking AddAsync).
137. Break `jpms/Components/ManualWorkOrderModal.razor` (166 lines) into components. Component-sized blocks: lines 30–79 (50 lines); lines 87–149 (63 lines).
138. Break `jpms/Pages/ProjectValuationSnapshots.razor` (165 lines) into components. Component-sized blocks: lines 44–104 (61 lines, taking InvoiceFor).
139. Break `jpms/Pages/Subcontractors.razor` (163 lines) into components. Component-sized blocks: lines 36–56 (21 lines); lines 85–141 (57 lines).
140. Break `jpms/Features/Triage/Panels/Actions/StageKpiAction.razor` (162 lines) into components. Component-sized blocks: lines 19–46 (28 lines, taking StageAsync).
141. Break `jpms/Pages/SetPassword.razor` (161 lines) into components. Component-sized blocks: lines 27–100 (74 lines, taking HandleSubmit).
142. Break `jpms/Components/RecordAuditHistory.razor` (160 lines) into components. Component-sized blocks: lines 17–68 (52 lines, taking EventLabel).
143. Break `jpms/Pages/ProjectDrawings.razor` (160 lines) into components. Component-sized blocks: lines 14–73 (60 lines); lines 103–117 (15 lines); lines 119–134 (16 lines); lines 136–152 (17 lines).
144. Break `jpms/Components/WorkOrderAttachmentsPanel.razor` (158 lines) into components. Component-sized blocks: lines 10–73 (64 lines, taking OnFilesSelected, Remove, Size).
145. Break `jpms/Features/Cvr/RunningProfitPanel.razor` (158 lines) into components. Component-sized blocks: lines 24–71 (48 lines, taking OnFloorChangedAsync); lines 76–125 (50 lines).
146. Break `jpms/Pages/AuditTrail.razor` (158 lines) into components. Component-sized blocks: lines 33–67 (35 lines); lines 91–144 (54 lines).
147. Break `jpms/Pages/PortalWorkOrderView.razor` (157 lines) into components. Component-sized blocks: lines 16–93 (78 lines, taking AcceptAsync).
148. Break `jpms/Features/Sales/LeadHouseModelPanel.razor` (155 lines) into components. Component-sized blocks: lines 16–42 (27 lines, taking SetPhaseAsync).
149. Break `jpms/Features/Triage/Panels/Actions/StageCalendarEventAction.razor` (155 lines) into components. Component-sized blocks: lines 10–73 (64 lines, taking OnKindChanged, SetTitle).
150. Break `jpms/Features/Triage/Panels/XeroTransactionView.razor` (155 lines) into components. Component-sized blocks: lines 15–30 (16 lines); lines 34–70 (37 lines, taking IsPreviewable, PreviewAttachment); lines 72–93 (22 lines).
151. Break `jpms/Features/Triage/Queue/TaggedEmailManagePanel.razor` (155 lines) into components. Component-sized blocks: lines 15–29 (15 lines); lines 31–57 (27 lines); lines 59–117 (59 lines).
152. Break `jpms/Pages/SalesStrategies.razor` (153 lines) into components. Component-sized blocks: lines 24–36 (13 lines); lines 61–119 (59 lines).
153. Break `jpms/Components/ImageViewer.razor` (152 lines) into components. Component-sized blocks: lines 16–65 (50 lines).
154. Break `jpms/Components/ProjectPageShell.razor` (152 lines) into components. Component-sized blocks: lines 23–63 (41 lines, taking Neighbour, GoTo).
155. Break `jpms/Features/Xero/Allocation/InvoiceViewerActions.razor` (152 lines) into components. Component-sized blocks: lines 9–26 (18 lines); lines 29–48 (20 lines); lines 54–111 (58 lines).
156. Break `jpms/Pages/ProjectHsAudit.razor` (152 lines) into components. Component-sized blocks: lines 31–60 (30 lines); lines 71–119 (49 lines).
157. Break `jpms/Components/NewProjectForm.razor` (151 lines) into components. Component-sized blocks: lines 7–77 (71 lines, taking OnOrganisationChanged, Submit).
158. Break `jpms/Features/Hs/Audits/HsAuditSectionPanel.razor` (150 lines) into components. Component-sized blocks: lines 23–36 (14 lines); lines 37–107 (71 lines, taking Edit, ParseInt, ParseDate).
159. Break `jpms/Pages/AiConnections.razor` (150 lines) into components. Component-sized blocks: lines 17–89 (73 lines, taking ToggleShowAll, RevokeAsync).
160. Break `jpms/Features/Variations/ApprovedFiguresPanel.razor` (149 lines) into components. Component-sized blocks: lines 8–87 (80 lines, taking CancelRevise, SubmitReviseAsync).
161. Break `jpms/Components/KpiPersonPicker.razor` (148 lines) into components. Component-sized blocks: lines 16–32 (17 lines, taking OnKeyChanged, OnNewNameInput).
162. Break `jpms/Pages/Projects.razor` (147 lines) into components. Component-sized blocks: lines 11–61 (51 lines, taking BuildExportWorkbook, HandleCreated).
163. Break `jpms/Components/InviteUserForm.razor` (146 lines) into components. Component-sized blocks: lines 4–81 (78 lines, taking ToggleRole, Submit, CopyLink, Reset).
164. Break `jpms/Features/Triage/RichTextEditor.razor` (145 lines) into components. Component-sized blocks: lines 11–53 (43 lines, taking ApplyColourAsync, ExecAsync).
165. Break `jpms/Features/Requests/RequestPartyPanel.razor` (144 lines) into components. Component-sized blocks: lines 39–63 (25 lines, taking OnPartySelected); lines 65–78 (14 lines); lines 80–102 (23 lines, taking RecipientLine).
166. Break `jpms/Components/ErrorToast.razor` (140 lines) into components. Component-sized blocks: lines 21–85 (65 lines, taking SignInAgain, PagePath).
167. Break `jpms/Features/Cvr/RunningProfitTable.razor` (140 lines) into components. Component-sized blocks: lines 26–82 (57 lines); lines 83–121 (39 lines).
168. Break `jpms/Features/Triage/Panels/Actions/StageVariationDecisionAction.razor` (140 lines) into components. Component-sized blocks: lines 10–53 (44 lines, taking StageApproveAsync, StageRejectAsync).
169. Break `jpms/Features/Variations/StagedBuildUpPanel.razor` (139 lines) into components. Component-sized blocks: lines 10–37 (28 lines); lines 39–75 (37 lines, taking Close, StageAsync).
170. Break `jpms/Features/Variations/VariationDocumentPanel.razor` (139 lines) into components. Component-sized blocks: lines 10–80 (71 lines, taking CancelAsync, SaveAsync).
171. Break `jpms/Features/Xero/Allocation/AllocatedSummaryRow.razor` (138 lines) into components. Component-sized blocks: lines 13–71 (59 lines); lines 76–92 (17 lines).
172. Break `jpms/Components/CostCentreSalesLinesModal.razor` (137 lines) into components. Component-sized blocks: lines 32–85 (54 lines); lines 87–128 (42 lines).
173. Break `jpms/Features/Directory/XeroImportModal.razor` (137 lines) into components. Component-sized blocks: lines 18–30 (13 lines); lines 63–122 (60 lines).
174. Break `jpms/Features/Requests/RequestFactsEditModal.razor` (136 lines) into components. Component-sized blocks: lines 7–42 (36 lines, taking SaveAsync).
175. Break `jpms/Components/RecordTabBar.razor` (135 lines) into components. Component-sized blocks: lines 20–54 (35 lines).
176. Break `jpms/Features/Site/Programme/ProgrammeDraftReview.razor` (135 lines) into components. Component-sized blocks: lines 16–38 (23 lines); lines 45–123 (79 lines).
177. Break `jpms/Pages/LabourOverview.razor` (134 lines) into components. Component-sized blocks: lines 31–46 (16 lines).
178. Break `jpms/Features/Labour/WorkerDetailPanel.razor` (133 lines) into components. Component-sized blocks: lines 8–74 (67 lines, taking SaveAsync).
179. Break `jpms/Features/Procurement/PackageDetailsSections.razor` (133 lines) into components. Component-sized blocks: lines 8–33 (26 lines); lines 57–113 (57 lines).
180. Break `jpms/Features/Triage/Panels/Actions/StageTodoCompleteAction.razor` (132 lines) into components. Component-sized blocks: lines 11–38 (28 lines, taking StageAsync).
181. Break `jpms/Features/Xero/InvoiceDocumentPreview.razor` (129 lines) into components. Component-sized blocks: lines 30–63 (34 lines).
182. Break `jpms/Features/Requests/RequestHeaderEditModal.razor` (128 lines) into components. Component-sized blocks: lines 7–48 (42 lines, taking OnStatusChanged, SaveAsync).
183. Break `jpms/Components/RoleHome.razor` (127 lines) into components. Component-sized blocks: lines 36–50 (15 lines); lines 52–112 (61 lines).
184. Break `jpms/Features/Triage/Panels/RecordCorrespondencePanel.razor` (126 lines) into components. Component-sized blocks: lines 12–25 (14 lines); lines 41–55 (15 lines, taking OnSent).
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
195. Break `jpms/Features/Requests/EmailDraftStagingModal.razor` (116 lines) into components. Component-sized blocks: lines 8–87 (80 lines, taking AuthorLabel).
196. Break `jpms/Layout/MainLayout.razor` (116 lines) into components. Component-sized blocks: lines 7–69 (63 lines).
197. Break `jpms/Features/Cashflow/CombinedStatementCard.razor` (115 lines) into components. Component-sized blocks: lines 34–51 (18 lines).
198. Break `jpms/Features/Procurement/TenderQuoteComparisonTable.razor` (115 lines) into components. Component-sized blocks: lines 11–38 (28 lines); lines 39–67 (29 lines); lines 68–91 (24 lines).
199. Break `jpms/Features/WeeklyCashflow/CashflowBandSection.razor` (115 lines) into components. Component-sized blocks: lines 15–32 (18 lines); lines 33–86 (54 lines).
200. Break `jpms/Features/Procurement/LocalSubcontractorFinderModal.razor` (114 lines) into components. Component-sized blocks: lines 42–54 (13 lines); lines 66–109 (44 lines).
201. Break `jpms/Features/Variations/VariationDetailsCard.razor` (113 lines) into components. Component-sized blocks: lines 11–49 (39 lines, taking CancelAsync, SaveEstimateAsync).
202. Break `jpms/Features/Triage/Panels/Actions/StageTenderResponseAction.razor` (112 lines) into components. Component-sized blocks: lines 13–34 (22 lines, taking StageAsync).
203. Break `jpms/Features/Sales/SimpleMarkdown.razor` (111 lines) into components. No block was large enough to measure: read the view for its seams.
204. Break `jpms/Components/DrawingFolderPicker.razor` (109 lines) into components. Component-sized blocks: lines 13–42 (30 lines).
205. Break `jpms/Components/UpdateToast.razor` (109 lines) into components. Component-sized blocks: lines 25–53 (29 lines).
206. Break `jpms/Features/Labour/WeeklySignOffTable.razor` (109 lines) into components. Component-sized blocks: lines 10–54 (45 lines, taking PartLabel, WeekCell).
207. Break `jpms/Features/Procurement/DeleteWorkOrderModal.razor` (109 lines) into components. Component-sized blocks: lines 9–53 (45 lines, taking Close, DeleteAsync).
208. Break `jpms/Features/Procurement/TenderSubmissionModal.razor` (109 lines) into components. Component-sized blocks: lines 36–47 (12 lines); lines 71–99 (29 lines).
209. Break `jpms/Features/Triage/Panels/Actions/StageBuildingControlInspectionAction.razor` (109 lines) into components. Component-sized blocks: lines 12–52 (41 lines, taking Set).
210. Break `jpms/Features/Triage/Workspace/PanelWorkspace.razor` (109 lines) into components. Component-sized blocks: lines 13–30 (18 lines, taking ContentFor, WrapperClass).
211. Break `jpms/Components/ExportToExcelButton.razor` (107 lines) into components. Component-sized blocks: lines 13–27 (15 lines).
212. Break `jpms/Features/Labour/ChaseListPanel.razor` (106 lines) into components. Component-sized blocks: lines 7–66 (60 lines, taking StartDismiss, ConfirmDismissAsync).
213. Break `jpms/Features/Xero/Allocation/WorkOrderBillCard.razor` (106 lines) into components. Component-sized blocks: lines 7–76 (70 lines).
214. Break `jpms/Components/DrawingRevisionUploadForm.razor` (105 lines) into components. Component-sized blocks: lines 6–46 (41 lines, taking OnFileSelected, HandleUpload).
215. Break `jpms/Components/LoadGate.razor` (105 lines) into components. Component-sized blocks: lines 31–48 (18 lines).
216. Break `jpms/Components/Modal.razor` (104 lines) into components. Component-sized blocks: lines 4–39 (36 lines, taking HandleOverlayClick).
217. Break `jpms/Features/Labour/WeekEntryModal.razor` (104 lines) into components. Component-sized blocks: lines 18–36 (19 lines); lines 37–95 (59 lines).
218. Break `jpms/Features/Progress/ContractorsReports/ContractorsReportPreview.razor` (104 lines) into components. Component-sized blocks: lines 8–23 (16 lines); lines 24–36 (13 lines); lines 48–62 (15 lines).
219. Break `jpms/Components/OpenRequestsPanel.razor` (103 lines) into components. Component-sized blocks: lines 15–73 (59 lines, taking Href).
220. Break `jpms/Components/ValuationClaimCorrespondenceSection.razor` (103 lines) into components. Component-sized blocks: lines 10–59 (50 lines).
221. Break `jpms/Features/Triage/Queue/ReplyComposerForm.razor` (103 lines) into components. Component-sized blocks: lines 10–65 (56 lines).
222. Break `jpms/Features/WeeklyCashflow/SupplierGroupsModal.razor` (103 lines) into components. Component-sized blocks: lines 12–41 (30 lines); lines 44–81 (38 lines).
223. Break `jpms/Features/Triage/Queue/QueueInboxList.razor` (102 lines) into components. Component-sized blocks: lines 16–65 (50 lines, taking SortLinkClass).
224. Break `jpms/Features/Xero/SplitEditorForm.razor` (102 lines) into components. Component-sized blocks: lines 8–57 (50 lines, taking SetAmount).
225. Break `jpms/Components/CostCentreWorkOrdersModal.razor` (101 lines) into components. Component-sized blocks: lines 6–70 (65 lines).
226. Break `jpms/Components/Icons/ActionIcon.razor` (101 lines) into components. No block was large enough to measure: read the view for its seams.
227. Break `jpms/Features/Xero/Allocation/LabourLineRow.razor` (101 lines) into components. Component-sized blocks: lines 7–77 (71 lines).

**Pass 2 — Utility function identification**

228. Give `HandleAsync` one home. Declared in 523 files: api/Features/AccessRequests/Commands/ResolveAccessRequestHandler.cs, api/Features/AccessRequests/Commands/SubmitAccessRequestHandler.cs, api/Features/AccessRequests/Queries/ListPendingAccessRequestsHandler.cs, api/Features/Ai/Queries/ListAgentActivityHandler.cs.
229. Give `Run` one home. Declared in 509 files: api/Features/AccessRequests/Commands/ResolveAccessRequestEndpoint.cs, api/Features/AccessRequests/Commands/SubmitAccessRequestEndpoint.cs, api/Features/AccessRequests/Queries/ListPendingAccessRequestsEndpoint.cs, api/Features/Ai/Queries/ListAgentActivityEndpoint.cs.
230. Give `Check` one home. Declared in 265 files: api/Features/AccessRequests/Commands/ResolveAccessRequestValidation.cs, api/Features/AccessRequests/Commands/SubmitAccessRequestValidation.cs, api/Features/Ai/Skills/SaveAiSkillReferenceValidation.cs, api/Features/Ai/Skills/SaveAiSkillValidation.cs.
231. Give `Allows` one home. Declared in 153 files: api/Features/AccessRequests/Commands/ResolveAccessRequestAuthorisation.cs, api/Features/AccessRequests/Commands/SubmitAccessRequestAuthorisation.cs, api/Features/Ai/Skills/SaveAiActionSkillsAuthorisation.cs, api/Features/Ai/Skills/SaveAiSkillAuthorisation.cs.
232. Give `LoadAsync` one home. Declared in 64 files: api/Features/Ai/Tools/AiSourceTools.Opening.cs, api/Features/Progress/ContractorsReports/Documents/ContractorsReportPhotoLoader.cs, api/Features/Site/Drafts/ProgrammeDraftClaimCentres.cs, jpms/Components/MyTodosPanel.razor.
233. Give `ToModel` one home. Declared in 53 files: api/Features/AccessRequests/AccessRequestEntityMapping.cs, api/Features/ArchitectInstructions/ArchitectInstructionSupport.cs, api/Features/Architects/ArchitectEntityMapping.cs, api/Features/Bluebeam/Extraction/DrawingExtractionMapping.cs.
234. Give `RefreshAsync` one home. Declared in 50 files: api/Features/Bluebeam/BluebeamClient.cs, api/Features/Bluebeam/BluebeamTokenService.cs, api/Features/Connect/OAuthTokenManager.cs, api/Features/Connect/TokenEndpoint.cs.
235. Give `SaveAsync` one home. Declared in 43 files: api/Features/Procurement/Attachments/CompanyTenderTermsStore.cs, api/Features/Sales/Imagine/IImagineImageStore.cs, jpms/Components/ClientCostReferencesModal.razor, jpms/Components/DrawingDetailsEditor.razor.
236. Give `Build` one home. Declared in 36 files: api/Features/Ai/AiImageToolResult.cs, api/Features/Ai/Tools/Actions/CalendarActions.cs, api/Features/Ai/Tools/Actions/CommercialActions.cs, api/Features/Ai/Tools/Actions/KpiActions.cs.
237. Give `BuildExportWorkbook` one home. Declared in 28 files: jpms/Components/CostCentreSalesLinesModal.razor.cs, jpms/Components/FinancialsTable.Export.cs, jpms/Components/PackageReconciliationSection.razor.cs, jpms/Components/ValuationInvoicesSection.Export.cs.
238. Give `DeleteAsync` one home. Declared in 26 files: api/Features/ArchitectInstructions/Storage/ArchitectInstructionBlobStore.cs, api/Features/BuildingControl/Attachments/AzureBlobBuildingControlAttachmentStore.cs, api/Features/DocumentControl/Storage/AzureBlobDocumentControlStore.cs, api/Features/Drawings/Storage/AzureBlobDrawingStore.cs.
239. Give `FindAsync` one home. Declared in 25 files: api/Features/Commercial/Queries/SupplierBillFinder.cs, api/Features/Places/WebsiteContactFinder.cs, api/Features/Procurement/BidPackageEmailDispositionStore.cs, api/Features/Procurement/Commands/UncoveredCostCentres.cs.
240. Give `ForProjectAsync` one home. Declared in 23 files: api/Features/Commercial/WorkOrderLinkSlices.cs, api/Features/Commercial/WorkOrderPaidPositions.cs, api/Features/RecordLinks/Providers/BidPackageInviteLinkProvider.cs, api/Features/RecordLinks/Providers/BuildingControlCaseLinkProvider.cs.
241. Give `static` one home. Declared in 21 files: api/Features/Ai/AiAttachmentReader.cs, api/Features/Labour/Commands/WeekSignOffSlices.cs, api/Features/Labour/Commands/WorkerWeekApprovalByNameSlices.cs, api/Features/Labour/Commands/XeroCoding/ReissueChoice.cs.
242. Give `OpenAsync` one home. Declared in 21 files: api/Features/Ai/Sources/AiFiledDocuments.cs, api/Features/Ai/Tools/AiSourceTools.Opening.cs, api/Features/ArchitectInstructions/Storage/ArchitectInstructionBlobStore.cs, api/Features/BuildingControl/Attachments/AzureBlobBuildingControlAttachmentStore.cs.
243. Give `Open` a home of its own, out of `jpms/Components/ProjectDetailsEditor.razor`. 193 other files use it: api/Auth/InviteEmailBody.cs, api/Auth/PasswordResetEmailBody.cs, api/Data/Entities/CommercialEntities.cs, api/Data/Entities/TodoEntities.cs.
244. Give `DeleteAsync` a home of its own, out of `jpms/Components/ProjectDetailsEditor.razor`. 49 other files use it: api/Features/ArchitectInstructions/ArchitectInstructionHandlers.cs, api/Features/BuildingControl/Attachments/BuildingControlAttachmentHandlers.cs, api/Features/BuildingControl/Attachments/IBuildingControlAttachmentStore.cs, api/Features/DocumentControl/Storage/IDocumentControlBlobStore.cs.
245. Give `StartEdit` a home of its own, out of `jpms/Pages/AdminKpis.razor`. 3 other files use it: jpms/Components/ValuationReportTable.razor, jpms/Pages/ProjectLabour.razor, jpms/Pages/Workers.razor.
246. Give `SaveEditAsync` a home of its own, out of `jpms/Pages/AdminKpis.razor`. 4 other files use it: jpms/Components/ValuationInvoicesSection.razor, jpms/Pages/CostCodes.razor, jpms/Pages/ProjectDefectDetail.razor, jpms/Pages/ProjectLabour.razor.
247. Give `OnInitializedAsync` a home of its own, out of `jpms/Pages/AdminKpis.razor`. 26 other files use it: jpms/App.razor, jpms/Components/ExpiringDocumentsPanel.razor, jpms/Components/PackageReconciliationSection.razor.cs, jpms/Components/ProjectCorrespondencePanel.razor.cs.
248. Give `OnSessionChanged` a home of its own, out of `jpms/Pages/AdminKpis.razor`. 1 other files use it: jpms/Components/RoleOverridePrompt.razor.
249. Give `FetchAsync` a home of its own, out of `jpms/Pages/AdminKpis.razor`. 1 other files use it: jpms/Services/HttpPortalStore.cs.
250. Give `Dispose` a home of its own, out of `jpms/Pages/AdminKpis.razor`. 73 other files use it: api/Features/Bluebeam/BluebeamClient.cs, api/Features/Progress/WhatsApp/WhatsAppExportArchive.cs, jpms/App.razor, jpms/Components/AdminKpiPanel.razor.
251. Give `PrefillNod` a home of its own, out of `jpms/Features/Site/Programme/ProgrammeClaimsWorkbench.razor`. 1 other files use it: jpms/Pages/ProjectProgramme.razor.cs.
252. Give `ToggleForm` a home of its own, out of `jpms/Features/Site/Programme/ProgrammeClaimsWorkbench.razor`. 2 other files use it: jpms/Components/ApprovedUsersPanel.razor, jpms/Features/Site/Programme/ProgrammeWorkbench.razor.
253. Give `OnParametersSetAsync` a home of its own, out of `jpms/Components/DrawingExtractionPanel.razor`. 2 other files use it: jpms/Components/RequestConversation.razor.cs, jpms/Features/Triage/Panels/RecordLinkSection.razor.
254. Give `RefreshAsync` a home of its own, out of `jpms/Components/DrawingExtractionPanel.razor`. 139 other files use it: api/Features/Bluebeam/IBluebeamClient.cs, api/Features/Bluebeam/NullBluebeamClient.cs, api/Features/ValuationInvoices/Commands/CreateValuationInvoiceHandler.cs, api/Features/ValuationInvoices/Commands/DeleteValuationInvoiceHandler.cs.
255. Give `OtherTags` a home of its own, out of `jpms/Features/Triage/Panels/CorrespondenceThreadList.razor`. 2 other files use it: jpms/Features/Todos/Detail/TodoEmailCard.razor, jpms/Features/Triage/Panels/EmailFinder.razor.
256. Give `AttachmentUrl` a home of its own, out of `jpms/Features/Triage/Panels/CorrespondenceThreadList.razor`. 6 other files use it: jpms/Components/RequestConversation.razor, jpms/Components/RequestConversation.razor.cs, jpms/Features/Triage/Panels/XeroTransactionView.razor, jpms/Features/Triage/Queue/TriageMessageDetail.razor.
257. Give `SnapshotLines` a home of its own, out of `jpms/Components/VariationApprovePanel.razor`. 1 other files use it: jpms/Components/ManualVariationForm.razor.
258. Give `ReplaceLines` a home of its own, out of `jpms/Components/VariationApprovePanel.razor`. 1 other files use it: jpms/Components/ManualVariationForm.razor.
259. Give `OnInitialized` a home of its own, out of `jpms/Components/VariationApprovePanel.razor`. 18 other files use it: jpms/App.razor, jpms/Components/ApprovedSessionGate.razor, jpms/Components/ApprovedUsersPanel.razor, jpms/Components/DrawingFolderPicker.razor.
260. Give `OnInitializedAsync` a home of its own, out of `jpms/Components/VariationApprovePanel.razor`. 26 other files use it: jpms/App.razor, jpms/Components/ExpiringDocumentsPanel.razor, jpms/Components/PackageReconciliationSection.razor.cs, jpms/Components/ProjectCorrespondencePanel.razor.cs.
261. Give `ResetLines` a home of its own, out of `jpms/Components/VariationApprovePanel.razor`. 1 other files use it: jpms/Components/ManualVariationForm.razor.
262. Give `AddLine` a home of its own, out of `jpms/Components/VariationApprovePanel.razor`. 6 other files use it: api/Features/Procurement/Commands/AwardBidPackageHandler.cs, contracts/Commercial/Export/ValuationExportStatementSheet.cs, contracts/Commercial/Export/ValuationExportVariationSheets.cs, jpms/Features/Procurement/TenderSubmissionModal.razor.
263. Give `RemoveLine` a home of its own, out of `jpms/Components/VariationApprovePanel.razor`. 1 other files use it: jpms/Features/Sales/EstimateSectionEditor.razor.
264. Give `LineAmount` a home of its own, out of `jpms/Components/VariationApprovePanel.razor`. 65 other files use it: api/Data/Entities/ValuationReportEntities.cs, api/Features/Ai/Tools/AiCommercialTools.Valuation.cs, api/Features/Ai/Tools/AiCommercialTools.Variation.cs, api/Features/Ai/Tools/AiValuationInvoiceTools.cs.
265. Give `TryBuildRequest` a home of its own, out of `jpms/Components/VariationApprovePanel.razor`. 1 other files use it: jpms/Components/ManualVariationForm.razor.
266. Give `readonly` a home of its own, out of `jpms/Pages/Registers.razor`. 1695 other files use it: api/Auth/AuthEnums.cs, api/Auth/AzureEmailInviteNotifier.cs, api/Auth/LoggingInviteNotifier.cs, api/Auth/SessionCookie.cs.
267. Give `OnInitializedAsync` a home of its own, out of `jpms/Pages/Registers.razor`. 26 other files use it: jpms/App.razor, jpms/Components/ExpiringDocumentsPanel.razor, jpms/Components/PackageReconciliationSection.razor.cs, jpms/Components/ProjectCorrespondencePanel.razor.cs.
268. Give `RefreshAsync` a home of its own, out of `jpms/Pages/Registers.razor`. 139 other files use it: api/Features/Bluebeam/IBluebeamClient.cs, api/Features/Bluebeam/NullBluebeamClient.cs, api/Features/ValuationInvoices/Commands/CreateValuationInvoiceHandler.cs, api/Features/ValuationInvoices/Commands/DeleteValuationInvoiceHandler.cs.
269. Give `StartEdit` a home of its own, out of `jpms/Pages/Registers.razor`. 3 other files use it: jpms/Components/ValuationReportTable.razor, jpms/Pages/ProjectLabour.razor, jpms/Pages/Workers.razor.
270. Give `SaveAsync` a home of its own, out of `jpms/Pages/Registers.razor`. 29 other files use it: api/Features/Procurement/Attachments/CompanyTenderTermsEndpoints.cs, api/Features/Sales/Imagine/ImaginePublicService.Submit.cs, api/Features/Sales/Imagine/ImagineRenderRunner.cs, jpms/Components/ApprovedUserRow.razor.
271. Give `KindLabel` a home of its own, out of `jpms/Pages/Registers.razor`. 5 other files use it: api/Features/Commercial/Documents/ValuationReportBillRows.cs, api/Features/Commercial/Documents/ValuationReportSnapshotRenderer.Sections.cs, jpms/Features/Requests/EmailDraftStagingModal.razor, jpms/Features/Triage/MailReplyComposer.razor.cs.
272. Give `BuildExportWorkbook` a home of its own, out of `jpms/Pages/Registers.razor`. 26 other files use it: jpms/Components/CostCentreSalesLinesModal.razor, jpms/Components/FinancialsTable.razor, jpms/Components/PackageReconciliationSection.razor, jpms/Components/ValuationInvoicesSection.razor.
273. Give `AddTradeAsync` a home of its own, out of `jpms/Pages/AdminTrades.razor`. 3 other files use it: jpms/Components/DirectoryContactForm.razor.cs, jpms/Pages/SubcontractorDetail.razor.cs, jpms/Services/ISubcontractorStore.cs.
274. Give `StartRename` a home of its own, out of `jpms/Pages/AdminTrades.razor`. 2 other files use it: jpms/Pages/ProjectVariationDetail.Menus.cs, jpms/Pages/ProjectVariationDetail.RequestRepair.cs.
275. Give `ConfirmDeleteAsync` a home of its own, out of `jpms/Pages/AdminTrades.razor`. 2 other files use it: jpms/Pages/Workers.razor, jpms/Pages/Workers.razor.cs.
276. Give `OnInitializedAsync` a home of its own, out of `jpms/Pages/AdminTrades.razor`. 26 other files use it: jpms/App.razor, jpms/Components/ExpiringDocumentsPanel.razor, jpms/Components/PackageReconciliationSection.razor.cs, jpms/Components/ProjectCorrespondencePanel.razor.cs.
277. Give `OnSessionChanged` a home of its own, out of `jpms/Pages/AdminTrades.razor`. 1 other files use it: jpms/Components/RoleOverridePrompt.razor.
278. Give `FetchAsync` a home of its own, out of `jpms/Pages/AdminTrades.razor`. 1 other files use it: jpms/Services/HttpPortalStore.cs.
279. Give `Dispose` a home of its own, out of `jpms/Pages/AdminTrades.razor`. 73 other files use it: api/Features/Bluebeam/BluebeamClient.cs, api/Features/Progress/WhatsApp/WhatsAppExportArchive.cs, jpms/App.razor, jpms/Components/AdminKpiPanel.razor.
280. Give `OnInitializedAsync` a home of its own, out of `jpms/Pages/AgedPayables.razor`. 26 other files use it: jpms/App.razor, jpms/Components/ExpiringDocumentsPanel.razor, jpms/Components/PackageReconciliationSection.razor.cs, jpms/Components/ProjectCorrespondencePanel.razor.cs.
281. Give `ForceRefreshAsync` a home of its own, out of `jpms/Pages/AgedPayables.razor`. 2 other files use it: jpms/Pages/WeeklyCashflow.razor, jpms/Pages/XeroTransactions.razor.
282. Give `OpenEmailModal` a home of its own, out of `jpms/Pages/WorkOrderPo.razor`. 1 other files use it: jpms/Pages/ProjectRequestDetail.razor.
283. Give `OnInitializedAsync` a home of its own, out of `jpms/Pages/WorkOrderPo.razor`. 26 other files use it: jpms/App.razor, jpms/Components/ExpiringDocumentsPanel.razor, jpms/Components/PackageReconciliationSection.razor.cs, jpms/Components/ProjectCorrespondencePanel.razor.cs.
284. Give `Dispose` a home of its own, out of `jpms/Pages/WorkOrderPo.razor`. 73 other files use it: api/Features/Bluebeam/BluebeamClient.cs, api/Features/Progress/WhatsApp/WhatsAppExportArchive.cs, jpms/App.razor, jpms/Components/AdminKpiPanel.razor.
285. Give `OnInitializedAsync` a home of its own, out of `jpms/Pages/AgedReceivables.razor`. 26 other files use it: jpms/App.razor, jpms/Components/ExpiringDocumentsPanel.razor, jpms/Components/PackageReconciliationSection.razor.cs, jpms/Components/ProjectCorrespondencePanel.razor.cs.
286. Give `ForceRefreshAsync` a home of its own, out of `jpms/Pages/AgedReceivables.razor`. 2 other files use it: jpms/Pages/WeeklyCashflow.razor, jpms/Pages/XeroTransactions.razor.
287. Give `OnParametersSetAsync` a home of its own, out of `jpms/Components/CostCentreReconciliationModal.razor`. 2 other files use it: jpms/Components/RequestConversation.razor.cs, jpms/Features/Triage/Panels/RecordLinkSection.razor.
288. Give `OpenAdd` a home of its own, out of `jpms/Components/SubcontractorComplianceList.razor`. 7 other files use it: jpms/Components/ProjectTodoList.razor, jpms/Features/WeeklyCashflow/CashflowItemModal.razor, jpms/Pages/CostCodes.razor, jpms/Pages/ProjectCalendar.razor.
289. Give `CloseAdd` a home of its own, out of `jpms/Components/SubcontractorComplianceList.razor`. 2 other files use it: jpms/Components/ProjectTodoList.razor, jpms/Pages/Todos.razor.
290. Give `OpenEdit` a home of its own, out of `jpms/Components/SubcontractorComplianceList.razor`. 11 other files use it: jpms/Components/PackageReconciliationSection.razor, jpms/Components/ValuationInvoicesSection.Menu.cs, jpms/Components/ValuationInvoicesSection.razor.cs, jpms/Features/WeeklyCashflow/CashflowItemModal.razor.
291. Give `CloseEdit` a home of its own, out of `jpms/Components/SubcontractorComplianceList.razor`. 5 other files use it: jpms/Pages/ProjectCalendar.razor, jpms/Pages/ProjectCalendar.razor.cs, jpms/Pages/ProjectDefectDetail.razor, jpms/Pages/SubcontractorDetail.razor.
292. Give `OnInitializedAsync` a home of its own, out of `jpms/Pages/ProjectProgress.razor`. 26 other files use it: jpms/App.razor, jpms/Components/ExpiringDocumentsPanel.razor, jpms/Components/PackageReconciliationSection.razor.cs, jpms/Components/ProjectCorrespondencePanel.razor.cs.
293. Give `Period` a home of its own, out of `jpms/Pages/ProjectProgress.razor`. 21 other files use it: api/Features/Commercial/Commands/AddClaimPeriodValidation.cs, api/Features/Commercial/Documents/ValuationReportBillColumns.cs, api/Features/Commercial/Documents/ValuationReportSnapshotRenderer.Sections.cs, api/Features/Progress/ContractorsReports/Documents/ContractorsReportPdfRenderer.cs.
294. Give `DeleteReportAsync` a home of its own, out of `jpms/Pages/ProjectProgress.razor`. 1 other files use it: jpms/Services/IProgressStore.cs.
295. Give `DeleteUpdateAsync` a home of its own, out of `jpms/Pages/ProjectProgress.razor`. 1 other files use it: jpms/Services/IProgressStore.cs.
296. Give `DeletePhotoAsync` a home of its own, out of `jpms/Pages/ProjectProgress.razor`. 1 other files use it: jpms/Services/IProgressStore.cs.
297. Give `AddPhotosAsync` a home of its own, out of `jpms/Pages/ProjectProgress.razor`. 1 other files use it: jpms/Services/IProgressStore.cs.
298. Give `RunAsync` a home of its own, out of `jpms/Pages/ProjectProgress.razor`. 7 other files use it: api/Features/Ai/Tools/AiActionGatewayTools.cs, api/Program.cs, jpms/Program.cs, worker/Bluebeam/DrawingExtractionWorker.cs.
299. Give `OnInitializedAsync` a home of its own, out of `jpms/Pages/Clients.razor`. 26 other files use it: jpms/App.razor, jpms/Components/ExpiringDocumentsPanel.razor, jpms/Components/PackageReconciliationSection.razor.cs, jpms/Components/ProjectCorrespondencePanel.razor.cs.
300. Give `ReloadAsync` a home of its own, out of `jpms/Pages/Clients.razor`. 15 other files use it: api/Features/Bluebeam/BluebeamTokenService.cs, jpms/Components/ValuationInvoicesSection.Commands.cs, jpms/Components/ValuationInvoicesSection.Forms.cs, jpms/Pages/ProjectValuation.Invoices.cs.
301. Give `OpenCreate` a home of its own, out of `jpms/Pages/Clients.razor`. 1 other files use it: jpms/Components/PackageReconciliationSection.razor.
302. Give `SendInviteAsync` a home of its own, out of `jpms/Pages/Clients.razor`. 2 other files use it: api/Auth/IInviteNotifier.cs, api/Features/Auth/UserInviter.cs.
303. Give `OpenEdit` a home of its own, out of `jpms/Pages/Clients.razor`. 11 other files use it: jpms/Components/PackageReconciliationSection.razor, jpms/Components/ValuationInvoicesSection.Menu.cs, jpms/Components/ValuationInvoicesSection.razor.cs, jpms/Features/WeeklyCashflow/CashflowItemModal.razor.
304. Give `SaveEditAsync` a home of its own, out of `jpms/Pages/Clients.razor`. 4 other files use it: jpms/Components/ValuationInvoicesSection.razor, jpms/Pages/CostCodes.razor, jpms/Pages/ProjectDefectDetail.razor, jpms/Pages/ProjectLabour.razor.
305. Give `BuildExportWorkbook` a home of its own, out of `jpms/Pages/Clients.razor`. 26 other files use it: jpms/Components/CostCentreSalesLinesModal.razor, jpms/Components/FinancialsTable.razor, jpms/Components/PackageReconciliationSection.razor, jpms/Components/ValuationInvoicesSection.razor.
306. Give `OnParametersSet` a home of its own, out of `jpms/Components/ApprovedUserRow.razor`. 12 other files use it: jpms/Components/DrawingFolderOptions.razor, jpms/Components/DrawingsTable.razor, jpms/Components/KpiPersonPicker.razor, jpms/Components/ValuationReportTable.BulkPercent.cs.
307. Give `Revoke` a home of its own, out of `jpms/Components/ApprovedUserRow.razor`. 2 other files use it: api/Features/Directory/Commands/DeleteDirectoryUserHandler.cs, api/Features/Directory/Commands/RemoveDirectoryUserValidation.cs.
308. Give `OnInitializedAsync` a home of its own, out of `jpms/Pages/AdminSystem.razor`. 26 other files use it: jpms/App.razor, jpms/Components/ExpiringDocumentsPanel.razor, jpms/Components/PackageReconciliationSection.razor.cs, jpms/Components/ProjectCorrespondencePanel.razor.cs.
309. Give `OnSessionChanged` a home of its own, out of `jpms/Pages/AdminSystem.razor`. 1 other files use it: jpms/Components/RoleOverridePrompt.razor.
310. Give `PublishAsync` a home of its own, out of `jpms/Pages/AdminSystem.razor`. 1 other files use it: jpms/Services/ISystemStore.cs.
311. Give `Dispose` a home of its own, out of `jpms/Pages/AdminSystem.razor`. 73 other files use it: api/Features/Bluebeam/BluebeamClient.cs, api/Features/Progress/WhatsApp/WhatsAppExportArchive.cs, jpms/App.razor, jpms/Components/AdminKpiPanel.razor.
312. Give `OnInitialized` a home of its own, out of `jpms/Features/Sales/LeadFormModal.razor`. 18 other files use it: jpms/App.razor, jpms/Components/ApprovedSessionGate.razor, jpms/Components/ApprovedUsersPanel.razor, jpms/Components/DrawingFolderPicker.razor.
313. Give `OnParametersSet` a home of its own, out of `jpms/Features/Sales/LeadFormModal.razor`. 12 other files use it: jpms/Components/DrawingFolderOptions.razor, jpms/Components/DrawingsTable.razor, jpms/Components/KpiPersonPicker.razor, jpms/Components/ValuationReportTable.BulkPercent.cs.
314. Give `Cancel` a home of its own, out of `jpms/Features/Sales/LeadFormModal.razor`. 114 other files use it: api/Features/Commercial/Commands/RemoveValuationLineItemHandler.cs, api/Features/Commercial/Commands/ReopenValuationClaimHandler.cs, api/Features/Procurement/Commands/DeleteBidPackageHandler.cs, api/Features/Procurement/Commands/DeleteDraftWorkOrderHandler.cs.
315. Give `SaveAsync` a home of its own, out of `jpms/Features/Sales/LeadFormModal.razor`. 29 other files use it: api/Features/Procurement/Attachments/CompanyTenderTermsEndpoints.cs, api/Features/Sales/Imagine/ImaginePublicService.Submit.cs, api/Features/Sales/Imagine/ImagineRenderRunner.cs, jpms/Components/ApprovedUserRow.razor.
316. Give `StatusViewCount` a home of its own, out of `jpms/Pages/RfiDashboard.razor`. 1 other files use it: jpms/Pages/ProjectRequests.razor.
317. Give `StatusViewLabel` a home of its own, out of `jpms/Pages/RfiDashboard.razor`. 1 other files use it: jpms/Pages/ProjectRequests.razor.
318. Give `StatusViewClass` a home of its own, out of `jpms/Pages/RfiDashboard.razor`. 1 other files use it: jpms/Pages/ProjectRequests.razor.
319. Give `Date` a home of its own, out of `jpms/Pages/RfiDashboard.razor`. 256 other files use it: api/Data/Entities/CalendarEventEntities.cs, api/Data/Entities/LabourPlanningEntities.cs, api/Data/Entities/XeroLedgerLineEntity.cs, api/Data/JpmsContext.Model.cs.
320. Give `Reload` a home of its own, out of `jpms/Pages/RfiDashboard.razor`. 16 other files use it: jpms/Components/ErrorToast.razor, jpms/Features/Procurement/SupplierAccountModal.razor, jpms/Pages/ClientPortalHome.razor, jpms/Pages/PortalHome.razor.cs.
321. Give `OnInitializedAsync` a home of its own, out of `jpms/Pages/RfiDashboard.razor`. 26 other files use it: jpms/App.razor, jpms/Components/ExpiringDocumentsPanel.razor, jpms/Components/PackageReconciliationSection.razor.cs, jpms/Components/ProjectCorrespondencePanel.razor.cs.
322. Give `Dispose` a home of its own, out of `jpms/Pages/RfiDashboard.razor`. 73 other files use it: api/Features/Bluebeam/BluebeamClient.cs, api/Features/Progress/WhatsApp/WhatsAppExportArchive.cs, jpms/App.razor, jpms/Components/AdminKpiPanel.razor.
323. Give `OnInitializedAsync` a home of its own, out of `jpms/Pages/Policies.razor`. 26 other files use it: jpms/App.razor, jpms/Components/ExpiringDocumentsPanel.razor, jpms/Components/PackageReconciliationSection.razor.cs, jpms/Components/ProjectCorrespondencePanel.razor.cs.
324. Give `Dispose` a home of its own, out of `jpms/Pages/Policies.razor`. 73 other files use it: api/Features/Bluebeam/BluebeamClient.cs, api/Features/Progress/WhatsApp/WhatsAppExportArchive.cs, jpms/App.razor, jpms/Components/AdminKpiPanel.razor.
325. Give `PublishAsync` a home of its own, out of `jpms/Pages/Policies.razor`. 1 other files use it: jpms/Services/ISystemStore.cs.
326. Give `OnParametersSetAsync` a home of its own, out of `jpms/Components/RequestAttachmentsPanel.razor`. 2 other files use it: jpms/Components/RequestConversation.razor.cs, jpms/Features/Triage/Panels/RecordLinkSection.razor.
327. Give `LoadAsync` a home of its own, out of `jpms/Components/RequestAttachmentsPanel.razor`. 18 other files use it: api/Features/Ai/Tools/AiValuationInvoiceTools.cs, api/Features/Commercial/Documents/ValuationReportSnapshotPdfBuilder.cs, api/Features/MailboxIntake/Graph/MailboxGraphClient.Tagging.cs, api/Features/Progress/ContractorsReports/Documents/ContractorsReportBuilder.cs.
328. Give `OpenDrawingPicker` a home of its own, out of `jpms/Components/RequestAttachmentsPanel.razor`. 1 other files use it: jpms/Components/RequestForm.razor.
329. Give `ToggleRevision` a home of its own, out of `jpms/Components/RequestAttachmentsPanel.razor`. 1 other files use it: jpms/Components/RequestForm.razor.
330. Give `ConfirmDrawings` a home of its own, out of `jpms/Components/RequestAttachmentsPanel.razor`. 1 other files use it: jpms/Pages/ProjectBidPackageInviteDetail.razor.
331. Give `OnFilesSelected` a home of its own, out of `jpms/Components/RequestAttachmentsPanel.razor`. 4 other files use it: jpms/Components/DrawingUploadForm.razor, jpms/Components/RequestForm.razor, jpms/Features/Procurement/PackageDocumentsSection.razor, jpms/Pages/ProjectBidPackageInviteDetail.razor.
332. Give `Remove` a home of its own, out of `jpms/Components/RequestAttachmentsPanel.razor`. 214 other files use it: api/Features/AccessRequests/Commands/ResolveAccessRequestHandler.cs, api/Features/Ai/Skills/SaveAiActionSkillsHandler.cs, api/Features/ArchitectInstructions/ArchitectInstructionHandlers.cs, api/Features/Bluebeam/BluebeamStatusEndpoints.cs.
333. Give `OnInitializedAsync` a home of its own, out of `jpms/Pages/ProjectDefects.razor`. 26 other files use it: jpms/App.razor, jpms/Components/ExpiringDocumentsPanel.razor, jpms/Components/PackageReconciliationSection.razor.cs, jpms/Components/ProjectCorrespondencePanel.razor.cs.
334. Give `OnParametersSetAsync` a home of its own, out of `jpms/Pages/ProjectDefects.razor`. 2 other files use it: jpms/Components/RequestConversation.razor.cs, jpms/Features/Triage/Panels/RecordLinkSection.razor.
335. Give `LoadAsync` a home of its own, out of `jpms/Pages/ProjectDefects.razor`. 18 other files use it: api/Features/Ai/Tools/AiValuationInvoiceTools.cs, api/Features/Commercial/Documents/ValuationReportSnapshotPdfBuilder.cs, api/Features/MailboxIntake/Graph/MailboxGraphClient.Tagging.cs, api/Features/Progress/ContractorsReports/Documents/ContractorsReportBuilder.cs.
336. Give `RaiseAsync` a home of its own, out of `jpms/Pages/ProjectDefects.razor`. 9 other files use it: api/Features/BuildingControl/Commands/BuildingControlInspectionCommands.cs, api/Features/BuildingControl/Commands/CreateBuildingControlInspectionFromMessage.cs, api/Features/Calendar/Commands/CreateCalendarEventFromMessageHandler.cs, api/Features/Calendar/Commands/CreateCalendarEventHandler.cs.
337. Give `SetStatusAsync` a home of its own, out of `jpms/Pages/ProjectDefects.razor`. 7 other files use it: jpms/Pages/ProjectBuildingControlInspection.razor, jpms/Pages/ProjectDefectDetail.razor, jpms/Pages/ProjectHs.razor, jpms/Pages/ProjectVariationDetail.Status.cs.
338. Give `Dispose` a home of its own, out of `jpms/Pages/ProjectDefects.razor`. 73 other files use it: api/Features/Bluebeam/BluebeamClient.cs, api/Features/Progress/WhatsApp/WhatsAppExportArchive.cs, jpms/App.razor, jpms/Components/AdminKpiPanel.razor.
339. Give `OnInitializedAsync` a home of its own, out of `jpms/Components/MyTodosPanel.razor`. 26 other files use it: jpms/App.razor, jpms/Components/ExpiringDocumentsPanel.razor, jpms/Components/PackageReconciliationSection.razor.cs, jpms/Components/ProjectCorrespondencePanel.razor.cs.
340. Give `LoadAsync` a home of its own, out of `jpms/Components/MyTodosPanel.razor`. 18 other files use it: api/Features/Ai/Tools/AiValuationInvoiceTools.cs, api/Features/Commercial/Documents/ValuationReportSnapshotPdfBuilder.cs, api/Features/MailboxIntake/Graph/MailboxGraphClient.Tagging.cs, api/Features/Progress/ContractorsReports/Documents/ContractorsReportBuilder.cs.
341. Give `SetComplete` a home of its own, out of `jpms/Components/MyTodosPanel.razor`. 2 other files use it: jpms/Components/ProjectTodoList.razor, jpms/Pages/Todos.razor.
342. Give `ScopeLabel` a home of its own, out of `jpms/Components/MyTodosPanel.razor`. 6 other files use it: jpms/Features/Todos/Detail/TodoFactsPanel.razor, jpms/Features/Todos/Detail/TodoScopeFact.razor, jpms/Pages/TodoDetail.razor, jpms/Pages/Todos.Add.cs.
343. Give `MatchesRoleFilter` a home of its own, out of `jpms/Components/MyTodosPanel.razor`. 1 other files use it: jpms/Components/ProjectTodoList.razor.
344. Give `ViewTabClass` a home of its own, out of `jpms/Components/MyTodosPanel.razor`. 6 other files use it: jpms/Components/ProjectTodoList.razor, jpms/Components/ProjectTodoList.razor.cs, jpms/Pages/DocumentControl.Display.cs, jpms/Pages/DocumentControl.razor.
345. Give `ToggleSort` a home of its own, out of `jpms/Components/MyTodosPanel.razor`. 2 other files use it: jpms/Components/ProjectTodoList.razor, jpms/Pages/Todos.razor.
346. Give `IsOverdue` a home of its own, out of `jpms/Components/MyTodosPanel.razor`. 22 other files use it: api/Features/Requests/Documents/RequestDocumentModel.cs, api/Features/Requests/Documents/RequestDocumentRenderer.Sections.cs, contracts/Xero/GetXeroAgedPayables.cs, contracts/Xero/GetXeroAgedReceivables.cs.
347. Give `OnInitializedAsync` a home of its own, out of `jpms/Pages/LabourXeroMapping.razor`. 26 other files use it: jpms/App.razor, jpms/Components/ExpiringDocumentsPanel.razor, jpms/Components/PackageReconciliationSection.razor.cs, jpms/Components/ProjectCorrespondencePanel.razor.cs.
348. Give `Dispose` a home of its own, out of `jpms/Pages/LabourXeroMapping.razor`. 73 other files use it: api/Features/Bluebeam/BluebeamClient.cs, api/Features/Progress/WhatsApp/WhatsAppExportArchive.cs, jpms/App.razor, jpms/Components/AdminKpiPanel.razor.
349. Give `RefreshAsync` a home of its own, out of `jpms/Pages/LabourXeroMapping.razor`. 139 other files use it: api/Features/Bluebeam/IBluebeamClient.cs, api/Features/Bluebeam/NullBluebeamClient.cs, api/Features/ValuationInvoices/Commands/CreateValuationInvoiceHandler.cs, api/Features/ValuationInvoices/Commands/DeleteValuationInvoiceHandler.cs.
350. Give `OnParametersSet` a home of its own, out of `jpms/Components/RecipientInput.razor`. 12 other files use it: jpms/Components/DrawingFolderOptions.razor, jpms/Components/DrawingsTable.razor, jpms/Components/KpiPersonPicker.razor, jpms/Components/ValuationReportTable.BulkPercent.cs.
351. Give `OnKeyDown` a home of its own, out of `jpms/Components/RecipientInput.razor`. 2 other files use it: jpms/Components/InlineValueEditor.razor, jpms/Components/ValuationReportTable.razor.
352. Give `PickAsync` a home of its own, out of `jpms/Components/RecipientInput.razor`. 2 other files use it: jpms/Components/ProjectSelect.razor, jpms/Components/ProjectSelect.razor.cs.
353. Give `RemoveAsync` a home of its own, out of `jpms/Components/RecipientInput.razor`. 10 other files use it: jpms/Components/ApprovedUserRow.razor, jpms/Components/ManualWorkOrderModal.razor.cs, jpms/Components/PackageReconciliationSection.razor, jpms/Components/PartyContactsEditor.razor.
354. Give `Close` a home of its own, out of `jpms/Components/RecipientInput.razor`. 58 other files use it: api/Features/Drawings/Geometry/PdfGeometryExtractor.cs, api/Features/Labour/Commands/SettlementLineAndMappingSlices.cs, api/Features/Requests/Commands/MergeRequestsHandler.cs, contracts/Ai/PageGuides/SitePageGuides.cs.
355. Give `OnInitializedAsync` a home of its own, out of `jpms/Pages/ProjectInventory.razor`. 26 other files use it: jpms/App.razor, jpms/Components/ExpiringDocumentsPanel.razor, jpms/Components/PackageReconciliationSection.razor.cs, jpms/Components/ProjectCorrespondencePanel.razor.cs.
356. Give `OnParametersSetAsync` a home of its own, out of `jpms/Pages/ProjectInventory.razor`. 2 other files use it: jpms/Components/RequestConversation.razor.cs, jpms/Features/Triage/Panels/RecordLinkSection.razor.
357. Give `LoadAsync` a home of its own, out of `jpms/Pages/ProjectInventory.razor`. 18 other files use it: api/Features/Ai/Tools/AiValuationInvoiceTools.cs, api/Features/Commercial/Documents/ValuationReportSnapshotPdfBuilder.cs, api/Features/MailboxIntake/Graph/MailboxGraphClient.Tagging.cs, api/Features/Progress/ContractorsReports/Documents/ContractorsReportBuilder.cs.
358. Give `ToggleForm` a home of its own, out of `jpms/Pages/ProjectInventory.razor`. 2 other files use it: jpms/Components/ApprovedUsersPanel.razor, jpms/Features/Site/Programme/ProgrammeWorkbench.razor.
359. Give `StartEdit` a home of its own, out of `jpms/Pages/ProjectInventory.razor`. 3 other files use it: jpms/Components/ValuationReportTable.razor, jpms/Pages/ProjectLabour.razor, jpms/Pages/Workers.razor.
360. Give `SaveAsync` a home of its own, out of `jpms/Pages/ProjectInventory.razor`. 29 other files use it: api/Features/Procurement/Attachments/CompanyTenderTermsEndpoints.cs, api/Features/Sales/Imagine/ImaginePublicService.Submit.cs, api/Features/Sales/Imagine/ImagineRenderRunner.cs, jpms/Components/ApprovedUserRow.razor.
361. Give `Draft` a home of its own, out of `jpms/Components/ManualVariationForm.razor`. 199 other files use it: api/Data/JpmsContext.Model.cs, api/Data/JpmsContext.cs, api/Features/Ai/Tools/Actions/CommercialActions.ClaimsAndValuations.cs, api/Features/Ai/Tools/Actions/LabourAndBackOfficeActions.HealthAndSafety.cs.
362. Give `Set` a home of its own, out of `jpms/Components/ManualVariationForm.razor`. 97 other files use it: api/Auth/InviteEmailBody.cs, api/Auth/SessionCookie.cs, api/Data/Entities/ConnectEntities.cs, api/Data/Entities/CoreEntities.cs.
363. Give `SuggestNumber` a home of its own, out of `jpms/Components/ManualVariationForm.razor`. 1 other files use it: jpms/Components/AddManualVariationDialog.razor.
364. Give `Reset` a home of its own, out of `jpms/Components/ManualVariationForm.razor`. 34 other files use it: api/Auth/AuthEnums.cs, api/Auth/PasswordResetEmailBody.cs, api/Data/Entities/AuthEntities.cs, api/Features/Ai/Tools/Actions/CommercialActions.XeroAllocation.cs.
365. Give `ShowError` a home of its own, out of `jpms/Components/ManualVariationForm.razor`. 3 other files use it: jpms/Components/AddManualVariationDialog.razor, jpms/Components/RaiseRequestDialog.razor, jpms/Pages/Subcontractors.razor.cs.
366. Give `TrySubmitAsync` a home of its own, out of `jpms/Components/ManualVariationForm.razor`. 6 other files use it: jpms/Components/AddManualVariationDialog.razor, jpms/Components/RaiseRequestDialog.razor, jpms/Components/RequestForm.razor, jpms/Features/Triage/Panels/Actions/StageDirectoryContactAction.razor.
367. Give `IndentStyle` a home of its own, out of `jpms/Components/DrawingsTable.razor`. 1 other files use it: jpms/Features/Triage/AttachmentPicker.razor.
368. Give `OpenDrawing` a home of its own, out of `jpms/Components/DrawingsTable.razor`. 1 other files use it: jpms/Components/ProjectTodoList.razor.
369. Give `OnParametersSet` a home of its own, out of `jpms/Features/Requests/RequestOfficialFormPanel.razor`. 12 other files use it: jpms/Components/DrawingFolderOptions.razor, jpms/Components/DrawingsTable.razor, jpms/Components/KpiPersonPicker.razor, jpms/Components/ValuationReportTable.BulkPercent.cs.
370. Give `Seed` a home of its own, out of `jpms/Features/Requests/RequestOfficialFormPanel.razor`. 6 other files use it: api/Features/Architects/Commands/CreateArchitectHandler.cs, api/Features/Clients/Commands/CreateClientHandler.cs, api/Features/Commercial/Commands/StartValuationClaimHandler.cs, jpms/Components/AddManualVariationDialog.razor.
371. Give `Cancel` a home of its own, out of `jpms/Features/Requests/RequestOfficialFormPanel.razor`. 114 other files use it: api/Features/Commercial/Commands/RemoveValuationLineItemHandler.cs, api/Features/Commercial/Commands/ReopenValuationClaimHandler.cs, api/Features/Procurement/Commands/DeleteBidPackageHandler.cs, api/Features/Procurement/Commands/DeleteDraftWorkOrderHandler.cs.
372. Give `SaveAsync` a home of its own, out of `jpms/Features/Requests/RequestOfficialFormPanel.razor`. 29 other files use it: api/Features/Procurement/Attachments/CompanyTenderTermsEndpoints.cs, api/Features/Sales/Imagine/ImaginePublicService.Submit.cs, api/Features/Sales/Imagine/ImagineRenderRunner.cs, jpms/Components/ApprovedUserRow.razor.
373. Give `OnInitializedAsync` a home of its own, out of `jpms/Pages/ProjectSiteInstructions.razor`. 26 other files use it: jpms/App.razor, jpms/Components/ExpiringDocumentsPanel.razor, jpms/Components/PackageReconciliationSection.razor.cs, jpms/Components/ProjectCorrespondencePanel.razor.cs.
374. Give `OnParametersSetAsync` a home of its own, out of `jpms/Pages/ProjectSiteInstructions.razor`. 2 other files use it: jpms/Components/RequestConversation.razor.cs, jpms/Features/Triage/Panels/RecordLinkSection.razor.
375. Give `LoadAsync` a home of its own, out of `jpms/Pages/ProjectSiteInstructions.razor`. 18 other files use it: api/Features/Ai/Tools/AiValuationInvoiceTools.cs, api/Features/Commercial/Documents/ValuationReportSnapshotPdfBuilder.cs, api/Features/MailboxIntake/Graph/MailboxGraphClient.Tagging.cs, api/Features/Progress/ContractorsReports/Documents/ContractorsReportBuilder.cs.
376. Give `ToggleForm` a home of its own, out of `jpms/Pages/ProjectSiteInstructions.razor`. 2 other files use it: jpms/Components/ApprovedUsersPanel.razor, jpms/Features/Site/Programme/ProgrammeWorkbench.razor.
377. Give `StartEdit` a home of its own, out of `jpms/Pages/ProjectSiteInstructions.razor`. 3 other files use it: jpms/Components/ValuationReportTable.razor, jpms/Pages/ProjectLabour.razor, jpms/Pages/Workers.razor.
378. Give `SaveAsync` a home of its own, out of `jpms/Pages/ProjectSiteInstructions.razor`. 29 other files use it: api/Features/Procurement/Attachments/CompanyTenderTermsEndpoints.cs, api/Features/Sales/Imagine/ImaginePublicService.Submit.cs, api/Features/Sales/Imagine/ImagineRenderRunner.cs, jpms/Components/ApprovedUserRow.razor.
379. Give `ChipClass` a home of its own, out of `jpms/Pages/SalesLeads.razor`. 2 other files use it: jpms/Features/Xero/Allocation/BucketChipStrip.razor, jpms/Pages/SubcontractorCommunications.razor.
380. Give `OnInitializedAsync` a home of its own, out of `jpms/Pages/SalesLeads.razor`. 26 other files use it: jpms/App.razor, jpms/Components/ExpiringDocumentsPanel.razor, jpms/Components/PackageReconciliationSection.razor.cs, jpms/Components/ProjectCorrespondencePanel.razor.cs.
381. Give `LoadAsync` a home of its own, out of `jpms/Pages/SalesLeads.razor`. 18 other files use it: api/Features/Ai/Tools/AiValuationInvoiceTools.cs, api/Features/Commercial/Documents/ValuationReportSnapshotPdfBuilder.cs, api/Features/MailboxIntake/Graph/MailboxGraphClient.Tagging.cs, api/Features/Progress/ContractorsReports/Documents/ContractorsReportBuilder.cs.
382. Give `Dispose` a home of its own, out of `jpms/Pages/SalesLeads.razor`. 73 other files use it: api/Features/Bluebeam/BluebeamClient.cs, api/Features/Progress/WhatsApp/WhatsAppExportArchive.cs, jpms/App.razor, jpms/Components/AdminKpiPanel.razor.
383. Give `OnParametersSet` a home of its own, out of `jpms/Components/WorkOrderLineRecodeModal.razor`. 12 other files use it: jpms/Components/DrawingFolderOptions.razor, jpms/Components/DrawingsTable.razor, jpms/Components/KpiPersonPicker.razor, jpms/Components/ValuationReportTable.BulkPercent.cs.
384. Give `Parse` a home of its own, out of `jpms/Components/WorkOrderLineRecodeModal.razor`. 48 other files use it: api/Features/Ai/Tools/AiCommercialTools.Variation.cs, api/Features/Bluebeam/BluebeamClient.Sessions.cs, api/Features/Bluebeam/BluebeamClient.cs, api/Features/Bluebeam/Extraction/DrawingExtractionResultWriter.cs.
385. Give `SaveAsync` a home of its own, out of `jpms/Components/WorkOrderLineRecodeModal.razor`. 29 other files use it: api/Features/Procurement/Attachments/CompanyTenderTermsEndpoints.cs, api/Features/Sales/Imagine/ImaginePublicService.Submit.cs, api/Features/Sales/Imagine/ImagineRenderRunner.cs, jpms/Components/ApprovedUserRow.razor.
386. Give `MoneyExact` a home of its own, out of `jpms/Components/WorkOrderLineRecodeModal.razor`. 6 other files use it: jpms/Components/CostCentreCostOfSalesModal.razor, jpms/Features/Procurement/WorkOrderGroupRow.razor, jpms/Features/Procurement/WorkOrderLineRow.razor, jpms/Features/Procurement/WorkOrdersTable.razor.
387. Give `OnInitializedAsync` a home of its own, out of `jpms/Pages/AdminIntegrations.razor`. 26 other files use it: jpms/App.razor, jpms/Components/ExpiringDocumentsPanel.razor, jpms/Components/PackageReconciliationSection.razor.cs, jpms/Components/ProjectCorrespondencePanel.razor.cs.
388. Give `DisconnectAsync` a home of its own, out of `jpms/Pages/AdminIntegrations.razor`. 1 other files use it: jpms/Services/BluebeamStatusStore.cs.
389. Give `OnSessionChanged` a home of its own, out of `jpms/Pages/AdminIntegrations.razor`. 1 other files use it: jpms/Components/RoleOverridePrompt.razor.
390. Give `Dispose` a home of its own, out of `jpms/Pages/AdminIntegrations.razor`. 73 other files use it: api/Features/Bluebeam/BluebeamClient.cs, api/Features/Progress/WhatsApp/WhatsAppExportArchive.cs, jpms/App.razor, jpms/Components/AdminKpiPanel.razor.
391. Give `PickStatus` a home of its own, out of `jpms/Components/RequestTable.razor`. 1 other files use it: jpms/Pages/ProjectVariations.razor.
392. Give `Open` a home of its own, out of `jpms/Components/RequestTable.razor`. 193 other files use it: api/Auth/InviteEmailBody.cs, api/Auth/PasswordResetEmailBody.cs, api/Data/Entities/CommercialEntities.cs, api/Data/Entities/TodoEntities.cs.
393. Give `Date` a home of its own, out of `jpms/Components/RequestTable.razor`. 256 other files use it: api/Data/Entities/CalendarEventEntities.cs, api/Data/Entities/LabourPlanningEntities.cs, api/Data/Entities/XeroLedgerLineEntity.cs, api/Data/JpmsContext.Model.cs.
394. Give `OnParametersSet` a home of its own, out of `jpms/Features/Sales/ImagineProposal.razor`. 12 other files use it: jpms/Components/DrawingFolderOptions.razor, jpms/Components/DrawingsTable.razor, jpms/Components/KpiPersonPicker.razor, jpms/Components/ValuationReportTable.BulkPercent.cs.
395. Give `Toggle` a home of its own, out of `jpms/Features/Sales/ImagineProposal.razor`. 11 other files use it: jpms/Components/Checkbox.razor, jpms/Components/ValuationClaimCorrespondenceSection.razor, jpms/Components/ValuationInvoicesSection.razor, jpms/Components/ValuationInvoicesSection.razor.cs.
396. Give `BarStyle` a home of its own, out of `jpms/Features/Sales/ImagineProposal.razor`. 3 other files use it: jpms/Features/Site/Programme/ProgrammeExtensionRows.razor, jpms/Features/Site/Programme/ProgrammeGanttChart.razor, jpms/Features/Site/Programme/ProgrammePushBar.razor.
397. Give `OnInitializedAsync` a home of its own, out of `jpms/Pages/Architects.razor`. 26 other files use it: jpms/App.razor, jpms/Components/ExpiringDocumentsPanel.razor, jpms/Components/PackageReconciliationSection.razor.cs, jpms/Components/ProjectCorrespondencePanel.razor.cs.
398. Give `ReloadAsync` a home of its own, out of `jpms/Pages/Architects.razor`. 15 other files use it: api/Features/Bluebeam/BluebeamTokenService.cs, jpms/Components/ValuationInvoicesSection.Commands.cs, jpms/Components/ValuationInvoicesSection.Forms.cs, jpms/Pages/ProjectValuation.Invoices.cs.
399. Give `OpenCreate` a home of its own, out of `jpms/Pages/Architects.razor`. 1 other files use it: jpms/Components/PackageReconciliationSection.razor.
400. Give `OpenEdit` a home of its own, out of `jpms/Pages/Architects.razor`. 11 other files use it: jpms/Components/PackageReconciliationSection.razor, jpms/Components/ValuationInvoicesSection.Menu.cs, jpms/Components/ValuationInvoicesSection.razor.cs, jpms/Features/WeeklyCashflow/CashflowItemModal.razor.
401. Give `SaveEditAsync` a home of its own, out of `jpms/Pages/Architects.razor`. 4 other files use it: jpms/Components/ValuationInvoicesSection.razor, jpms/Pages/CostCodes.razor, jpms/Pages/ProjectDefectDetail.razor, jpms/Pages/ProjectLabour.razor.
402. Give `BuildExportWorkbook` a home of its own, out of `jpms/Pages/Architects.razor`. 26 other files use it: jpms/Components/CostCentreSalesLinesModal.razor, jpms/Components/FinancialsTable.razor, jpms/Components/PackageReconciliationSection.razor, jpms/Components/ValuationInvoicesSection.razor.
403. Give `LoadAsync` a home of its own, out of `jpms/Components/UsefulInformationPanel.razor`. 18 other files use it: api/Features/Ai/Tools/AiValuationInvoiceTools.cs, api/Features/Commercial/Documents/ValuationReportSnapshotPdfBuilder.cs, api/Features/MailboxIntake/Graph/MailboxGraphClient.Tagging.cs, api/Features/Progress/ContractorsReports/Documents/ContractorsReportBuilder.cs.
404. Give `Matches` a home of its own, out of `jpms/Components/UsefulInformationPanel.razor`. 53 other files use it: api/Features/Ai/AgentActivityLog.cs, api/Features/Ai/Tools/Actions/LabourAndBackOfficeActions.MonthEnd.cs, api/Features/Ai/Tools/Actions/ProcurementActions.Tenders.cs, api/Features/Ai/Tools/AiCommercialTools.SupplierAccount.cs.
405. Give `OpenAdd` a home of its own, out of `jpms/Components/UsefulInformationPanel.razor`. 7 other files use it: jpms/Components/ProjectTodoList.razor, jpms/Features/WeeklyCashflow/CashflowItemModal.razor, jpms/Pages/CostCodes.razor, jpms/Pages/ProjectCalendar.razor.
406. Give `OpenEdit` a home of its own, out of `jpms/Components/UsefulInformationPanel.razor`. 11 other files use it: jpms/Components/PackageReconciliationSection.razor, jpms/Components/ValuationInvoicesSection.Menu.cs, jpms/Components/ValuationInvoicesSection.razor.cs, jpms/Features/WeeklyCashflow/CashflowItemModal.razor.
407. Give `Save` a home of its own, out of `jpms/Components/UsefulInformationPanel.razor`. 128 other files use it: api/Features/Ai/Sources/AiSourceReader.cs, api/Features/Ai/Tools/AiCommercialTools.Variation.cs, api/Features/Ai/Tools/AiCommercialTools.cs, api/Features/Ai/Tools/AiEmailTools.cs.
408. Give `Delete` a home of its own, out of `jpms/Components/UsefulInformationPanel.razor`. 48 other files use it: api/Features/Bluebeam/BluebeamClient.Sessions.cs, api/Features/MailboxIntake/Graph/MailboxGraphClient.Drafts.cs, api/Features/Sales/SalesRoles.cs, contracts/Ai/PageGuides/CommercialPageGuides.cs.
409. Give `Run` a home of its own, out of `jpms/Components/UsefulInformationPanel.razor`. 18 other files use it: api/Features/Ai/ClaudeClient.cs, api/Features/Ai/Tools/Actions/LabourAndBackOfficeActions.WorkerLinks.cs, api/Features/Connect/RegisterClientEndpoint.cs, api/Features/MailboxIntake/MailboxIntakeOptions.cs.
410. Give `CreateReplyDraftAsync` a home of its own, out of `jpms/Features/Site/Programme/RelevantEventsList.razor`. 8 other files use it: api/Features/MailboxIntake/Compose/SendMailboxEmailHandler.Draft.cs, api/Features/MailboxIntake/Graph/IMailboxGraphClient.cs, api/Features/MailboxIntake/Graph/NullMailboxGraphClient.cs, api/Features/Procurement/Commands/PrepareWorkOrderReplyDraftHandler.cs.
411. Give `OnInitializedAsync` a home of its own, out of `jpms/Pages/PaymentCertificates.razor`. 26 other files use it: jpms/App.razor, jpms/Components/ExpiringDocumentsPanel.razor, jpms/Components/PackageReconciliationSection.razor.cs, jpms/Components/ProjectCorrespondencePanel.razor.cs.
412. Give `OnProjectFilterChanged` a home of its own, out of `jpms/Pages/PaymentCertificates.razor`. 1 other files use it: jpms/Pages/DocumentControl.razor.
413. Give `IsPdf` a home of its own, out of `jpms/Pages/PaymentCertificates.razor`. 9 other files use it: api/Features/Bluebeam/Extraction/QueueDrawingExtractionHandler.cs, api/Features/Bluebeam/Extraction/QueueProjectDrawingExtractionsHandler.cs, jpms/Features/DocumentControl/DocumentControlDisplay.cs, jpms/Features/DocumentControl/DocumentPreview.razor.
414. Give `Open` a home of its own, out of `jpms/Components/NextValuationDateEditor.razor`. 193 other files use it: api/Auth/InviteEmailBody.cs, api/Auth/PasswordResetEmailBody.cs, api/Data/Entities/CommercialEntities.cs, api/Data/Entities/TodoEntities.cs.
415. Give `Close` a home of its own, out of `jpms/Components/NextValuationDateEditor.razor`. 58 other files use it: api/Features/Drawings/Geometry/PdfGeometryExtractor.cs, api/Features/Labour/Commands/SettlementLineAndMappingSlices.cs, api/Features/Requests/Commands/MergeRequestsHandler.cs, contracts/Ai/PageGuides/SitePageGuides.cs.
416. Give `SendAsync` a home of its own, out of `jpms/Components/NextValuationDateEditor.razor`. 144 other files use it: api/Auth/AzureEmailInviteNotifier.cs, api/Features/Ai/ClaudeClient.cs, api/Features/Ai/Scans/AzureVisionOcr.cs, api/Features/Auth/ForgotPasswordEndpoint.cs.
417. Give `OnParametersSetAsync` a home of its own, out of `jpms/Components/SubcontractorStatementModal.razor`. 2 other files use it: jpms/Components/RequestConversation.razor.cs, jpms/Features/Triage/Panels/RecordLinkSection.razor.
418. Give `CreateDraftAsync` a home of its own, out of `jpms/Components/SubcontractorStatementModal.razor`. 12 other files use it: api/Features/Commercial/Commands/PrepareValuationReportSnapshotEmailDraftHandler.cs, api/Features/MailboxIntake/Compose/SendMailboxEmailHandler.Draft.cs, api/Features/MailboxIntake/Graph/IMailboxGraphClient.cs, api/Features/MailboxIntake/Graph/NullMailboxGraphClient.cs.
419. Give `MoneyExact` a home of its own, out of `jpms/Components/SubcontractorStatementModal.razor`. 6 other files use it: jpms/Components/CostCentreCostOfSalesModal.razor, jpms/Features/Procurement/WorkOrderGroupRow.razor, jpms/Features/Procurement/WorkOrderLineRow.razor, jpms/Features/Procurement/WorkOrdersTable.razor.
420. Give `SearchAsync` a home of its own, out of `jpms/Features/Triage/Panels/EmailFinder.razor`. 8 other files use it: api/Features/MailboxIntake/Graph/IMailboxGraphClient.cs, api/Features/MailboxIntake/Graph/NullMailboxGraphClient.cs, api/Features/Procurement/Queries/SearchLocalSubcontractorsHandler.cs, api/Features/RecordLinks/Queries/ListProjectCommunicationsHandler.cs.
421. Give `Toggle` a home of its own, out of `jpms/Features/Triage/Panels/EmailFinder.razor`. 11 other files use it: jpms/Components/Checkbox.razor, jpms/Components/ValuationClaimCorrespondenceSection.razor, jpms/Components/ValuationInvoicesSection.razor, jpms/Components/ValuationInvoicesSection.razor.cs.
422. Give `OnInitialized` a home of its own, out of `jpms/Components/ClaimProgressDialog.razor`. 18 other files use it: jpms/App.razor, jpms/Components/ApprovedSessionGate.razor, jpms/Components/ApprovedUsersPanel.razor, jpms/Components/DrawingFolderPicker.razor.
423. Give `OnParametersSet` a home of its own, out of `jpms/Components/ClaimProgressDialog.razor`. 12 other files use it: jpms/Components/DrawingFolderOptions.razor, jpms/Components/DrawingsTable.razor, jpms/Components/KpiPersonPicker.razor, jpms/Components/ValuationReportTable.BulkPercent.cs.
424. Give `Dispose` a home of its own, out of `jpms/Components/ClaimProgressDialog.razor`. 73 other files use it: api/Features/Bluebeam/BluebeamClient.cs, api/Features/Progress/WhatsApp/WhatsAppExportArchive.cs, jpms/App.razor, jpms/Components/AdminKpiPanel.razor.
425. Give `Remove` a home of its own, out of `jpms/Components/ClaimProgressDialog.razor`. 214 other files use it: api/Features/AccessRequests/Commands/ResolveAccessRequestHandler.cs, api/Features/Ai/Skills/SaveAiActionSkillsHandler.cs, api/Features/ArchitectInstructions/ArchitectInstructionHandlers.cs, api/Features/Bluebeam/BluebeamStatusEndpoints.cs.
426. Give `Publish` a home of its own, out of `jpms/Components/ClaimProgressDialog.razor`. 3 other files use it: contracts/Ai/PageGuides/OfficePageGuides.cs, jpms/Pages/AdminSystem.razor, jpms/Pages/Policies.razor.
427. Give `OnInitialized` a home of its own, out of `jpms/Components/MyDayWorkspace.razor`. 18 other files use it: jpms/App.razor, jpms/Components/ApprovedSessionGate.razor, jpms/Components/ApprovedUsersPanel.razor, jpms/Components/DrawingFolderPicker.razor.
428. Give `IssueAsync` a home of its own, out of `jpms/Features/Sales/LeadImaginePanel.razor`. 3 other files use it: jpms/Features/ValuationInvoices/ValuationInvoiceXeroRaiseModal.razor.cs, jpms/Pages/ProjectHsAudit.razor, jpms/Services/IValuationInvoiceStore.cs.
429. Give `RetryAsync` a home of its own, out of `jpms/Features/Sales/LeadImaginePanel.razor`. 1 other files use it: api/Features/Xero/Ledger/RetryXeroWriteBackHandler.cs.
430. Give `DownloadAsync` a home of its own, out of `jpms/Features/Sales/LeadImaginePanel.razor`. 1 other files use it: jpms/Components/ExportToExcelButton.razor.
431. Give `TakeAsync` a home of its own, out of `jpms/Components/ValuationSnapshotsSection.razor`. 4 other files use it: api/Features/Progress/Commands/CreateProgressUpdateWithPhotosEndpoint.cs, api/Features/Progress/Photos/ProgressPhotoBatches.cs, api/Features/Progress/SitePhotos/UploadSitePhotosEndpoint.cs, api/Features/Progress/WhatsApp/WhatsAppWeekWriter.cs.
432. Give `DeleteAsync` a home of its own, out of `jpms/Components/ValuationSnapshotsSection.razor`. 49 other files use it: api/Features/ArchitectInstructions/ArchitectInstructionHandlers.cs, api/Features/BuildingControl/Attachments/BuildingControlAttachmentHandlers.cs, api/Features/BuildingControl/Attachments/IBuildingControlAttachmentStore.cs, api/Features/DocumentControl/Storage/IDocumentControlBlobStore.cs.
433. Give `BuildExportWorkbook` a home of its own, out of `jpms/Components/ValuationSnapshotsSection.razor`. 26 other files use it: jpms/Components/CostCentreSalesLinesModal.razor, jpms/Components/FinancialsTable.razor, jpms/Components/PackageReconciliationSection.razor, jpms/Components/ValuationInvoicesSection.razor.
434. Give `OnParametersSet` a home of its own, out of `jpms/Components/WorkOrderLinkSplitModal.razor`. 12 other files use it: jpms/Components/DrawingFolderOptions.razor, jpms/Components/DrawingsTable.razor, jpms/Components/KpiPersonPicker.razor, jpms/Components/ValuationReportTable.BulkPercent.cs.
435. Give `Parse` a home of its own, out of `jpms/Components/WorkOrderLinkSplitModal.razor`. 48 other files use it: api/Features/Ai/Tools/AiCommercialTools.Variation.cs, api/Features/Bluebeam/BluebeamClient.Sessions.cs, api/Features/Bluebeam/BluebeamClient.cs, api/Features/Bluebeam/Extraction/DrawingExtractionResultWriter.cs.
436. Give `SaveAsync` a home of its own, out of `jpms/Components/WorkOrderLinkSplitModal.razor`. 29 other files use it: api/Features/Procurement/Attachments/CompanyTenderTermsEndpoints.cs, api/Features/Sales/Imagine/ImaginePublicService.Submit.cs, api/Features/Sales/Imagine/ImagineRenderRunner.cs, jpms/Components/ApprovedUserRow.razor.
437. Give `MoneyExact` a home of its own, out of `jpms/Components/WorkOrderLinkSplitModal.razor`. 6 other files use it: jpms/Components/CostCentreCostOfSalesModal.razor, jpms/Features/Procurement/WorkOrderGroupRow.razor, jpms/Features/Procurement/WorkOrderLineRow.razor, jpms/Features/Procurement/WorkOrdersTable.razor.
438. Give `BuildWorkbook` a home of its own, out of `jpms/Components/UnpaidXeroInvoicesModal.razor`. 34 other files use it: jpms/Components/CostCentreSalesLinesModal.razor, jpms/Components/ExportToExcelButton.razor, jpms/Components/FinancialsTable.razor, jpms/Components/PackageReconciliationSection.razor.
439. Give `MoneyExact` a home of its own, out of `jpms/Components/UnpaidXeroInvoicesModal.razor`. 6 other files use it: jpms/Components/CostCentreCostOfSalesModal.razor, jpms/Features/Procurement/WorkOrderGroupRow.razor, jpms/Features/Procurement/WorkOrderLineRow.razor, jpms/Features/Procurement/WorkOrdersTable.razor.
440. Give `OpenEditor` a home of its own, out of `jpms/Features/Sales/LeadProposalsPanel.razor`. 1 other files use it: jpms/Features/WeeklyCashflow/SupplierGroupsModal.razor.
441. Give `SendAsync` a home of its own, out of `jpms/Features/Sales/LeadProposalsPanel.razor`. 144 other files use it: api/Auth/AzureEmailInviteNotifier.cs, api/Features/Ai/ClaudeClient.cs, api/Features/Ai/Scans/AzureVisionOcr.cs, api/Features/Auth/ForgotPasswordEndpoint.cs.
442. Give `ItemsFor` a home of its own, out of `jpms/Components/TodoBoard.razor`. 1 other files use it: jpms/Services/Navigation/PageContext.cs.
443. Give `Item` a home of its own, out of `jpms/Components/DropdownMenu.razor`. 62 other files use it: api/Data/Entities/RequestItemEntity.cs, api/Features/Ai/Tools/AiToolCatalogue.TodoBrief.cs, api/Features/Hs/Audits/Commands/UpdateHsAuditItems.cs, api/Features/Hs/Audits/HsAuditRules.cs.
444. Give `DisposeAsync` a home of its own, out of `jpms/Components/DropdownMenu.razor`. 1 other files use it: jpms/Features/Triage/RichTextEditor.razor.
445. Give `Toggle` a home of its own, out of `jpms/Components/DropdownMenu.razor`. 11 other files use it: jpms/Components/Checkbox.razor, jpms/Components/ValuationClaimCorrespondenceSection.razor, jpms/Components/ValuationInvoicesSection.razor, jpms/Components/ValuationInvoicesSection.razor.cs.
446. Give `Close` a home of its own, out of `jpms/Components/DropdownMenu.razor`. 58 other files use it: api/Features/Drawings/Geometry/PdfGeometryExtractor.cs, api/Features/Labour/Commands/SettlementLineAndMappingSlices.cs, api/Features/Requests/Commands/MergeRequestsHandler.cs, contracts/Ai/PageGuides/SitePageGuides.cs.
447. Give `Run` a home of its own, out of `jpms/Components/DropdownMenu.razor`. 18 other files use it: api/Features/Ai/ClaudeClient.cs, api/Features/Ai/Tools/Actions/LabourAndBackOfficeActions.WorkerLinks.cs, api/Features/Connect/RegisterClientEndpoint.cs, api/Features/MailboxIntake/MailboxIntakeOptions.cs.
448. Give `LoadAsync` a home of its own, out of `jpms/Features/Triage/Panels/KpiTagSection.razor`. 18 other files use it: api/Features/Ai/Tools/AiValuationInvoiceTools.cs, api/Features/Commercial/Documents/ValuationReportSnapshotPdfBuilder.cs, api/Features/MailboxIntake/Graph/MailboxGraphClient.Tagging.cs, api/Features/Progress/ContractorsReports/Documents/ContractorsReportBuilder.cs.
449. Give `Dispose` a home of its own, out of `jpms/Features/Triage/Panels/KpiTagSection.razor`. 73 other files use it: api/Features/Bluebeam/BluebeamClient.cs, api/Features/Progress/WhatsApp/WhatsAppExportArchive.cs, jpms/App.razor, jpms/Components/AdminKpiPanel.razor.
450. Give `OnInitialized` a home of its own, out of `jpms/Features/Sales/StrategyFormModal.razor`. 18 other files use it: jpms/App.razor, jpms/Components/ApprovedSessionGate.razor, jpms/Components/ApprovedUsersPanel.razor, jpms/Components/DrawingFolderPicker.razor.
451. Give `OnParametersSet` a home of its own, out of `jpms/Features/Sales/StrategyFormModal.razor`. 12 other files use it: jpms/Components/DrawingFolderOptions.razor, jpms/Components/DrawingsTable.razor, jpms/Components/KpiPersonPicker.razor, jpms/Components/ValuationReportTable.BulkPercent.cs.
452. Give `Cancel` a home of its own, out of `jpms/Features/Sales/StrategyFormModal.razor`. 114 other files use it: api/Features/Commercial/Commands/RemoveValuationLineItemHandler.cs, api/Features/Commercial/Commands/ReopenValuationClaimHandler.cs, api/Features/Procurement/Commands/DeleteBidPackageHandler.cs, api/Features/Procurement/Commands/DeleteDraftWorkOrderHandler.cs.
453. Give `SaveAsync` a home of its own, out of `jpms/Features/Sales/StrategyFormModal.razor`. 29 other files use it: api/Features/Procurement/Attachments/CompanyTenderTermsEndpoints.cs, api/Features/Sales/Imagine/ImaginePublicService.Submit.cs, api/Features/Sales/Imagine/ImagineRenderRunner.cs, jpms/Components/ApprovedUserRow.razor.
454. Give `Open` a home of its own, out of `jpms/Components/PartyContactsEditor.razor`. 193 other files use it: api/Auth/InviteEmailBody.cs, api/Auth/PasswordResetEmailBody.cs, api/Data/Entities/CommercialEntities.cs, api/Data/Entities/TodoEntities.cs.
455. Give `Close` a home of its own, out of `jpms/Components/PartyContactsEditor.razor`. 58 other files use it: api/Features/Drawings/Geometry/PdfGeometryExtractor.cs, api/Features/Labour/Commands/SettlementLineAndMappingSlices.cs, api/Features/Requests/Commands/MergeRequestsHandler.cs, contracts/Ai/PageGuides/SitePageGuides.cs.
456. Give `ReloadAsync` a home of its own, out of `jpms/Components/PartyContactsEditor.razor`. 15 other files use it: api/Features/Bluebeam/BluebeamTokenService.cs, jpms/Components/ValuationInvoicesSection.Commands.cs, jpms/Components/ValuationInvoicesSection.Forms.cs, jpms/Pages/ProjectValuation.Invoices.cs.
457. Give `AddAsync` a home of its own, out of `jpms/Components/PartyContactsEditor.razor`. 14 other files use it: api/Features/Ai/Tools/AiProgressPhotoTools.cs, api/Features/Progress/Commands/AddProgressPhotosEndpoint.cs, jpms/Components/ProjectTodoList.razor.cs, jpms/Components/UsefulInformationPanel.razor.
458. Give `OnInitializedAsync` a home of its own, out of `jpms/Pages/AgentActivityLog.razor`. 26 other files use it: jpms/App.razor, jpms/Components/ExpiringDocumentsPanel.razor, jpms/Components/PackageReconciliationSection.razor.cs, jpms/Components/ProjectCorrespondencePanel.razor.cs.
459. Give `LoadAsync` a home of its own, out of `jpms/Pages/AgentActivityLog.razor`. 18 other files use it: api/Features/Ai/Tools/AiValuationInvoiceTools.cs, api/Features/Commercial/Documents/ValuationReportSnapshotPdfBuilder.cs, api/Features/MailboxIntake/Graph/MailboxGraphClient.Tagging.cs, api/Features/Progress/ContractorsReports/Documents/ContractorsReportBuilder.cs.
460. Give `FilterClass` a home of its own, out of `jpms/Pages/AgentActivityLog.razor`. 1 other files use it: jpms/Pages/ProjectRequests.razor.
461. Give `OpenAsync` a home of its own, out of `jpms/Components/SearchSelect.razor`. 51 other files use it: api/Features/Ai/Tools/AiSourceTools.Bytes.cs, api/Features/Ai/Tools/AiSourceTools.FindInSource.cs, api/Features/Ai/Tools/AiSourceTools.ReadSource.cs, api/Features/ArchitectInstructions/ArchitectInstructionEndpoints.cs.
462. Give `OnKeyDown` a home of its own, out of `jpms/Components/SearchSelect.razor`. 2 other files use it: jpms/Components/InlineValueEditor.razor, jpms/Components/ValuationReportTable.razor.
463. Give `Close` a home of its own, out of `jpms/Components/SearchSelect.razor`. 58 other files use it: api/Features/Drawings/Geometry/PdfGeometryExtractor.cs, api/Features/Labour/Commands/SettlementLineAndMappingSlices.cs, api/Features/Requests/Commands/MergeRequestsHandler.cs, contracts/Ai/PageGuides/SitePageGuides.cs.
464. Give `OnInitialized` a home of its own, out of `jpms/Components/ValuationLineForm.razor`. 18 other files use it: jpms/App.razor, jpms/Components/ApprovedSessionGate.razor, jpms/Components/ApprovedUsersPanel.razor, jpms/Components/DrawingFolderPicker.razor.
465. Give `OnParametersSet` a home of its own, out of `jpms/Components/ValuationLineForm.razor`. 12 other files use it: jpms/Components/DrawingFolderOptions.razor, jpms/Components/DrawingsTable.razor, jpms/Components/KpiPersonPicker.razor, jpms/Components/ValuationReportTable.BulkPercent.cs.
466. Give `SaveAsync` a home of its own, out of `jpms/Components/ValuationLineForm.razor`. 29 other files use it: api/Features/Procurement/Attachments/CompanyTenderTermsEndpoints.cs, api/Features/Sales/Imagine/ImaginePublicService.Submit.cs, api/Features/Sales/Imagine/ImagineRenderRunner.cs, jpms/Components/ApprovedUserRow.razor.
467. Give `Reset` a home of its own, out of `jpms/Components/ValuationLineForm.razor`. 34 other files use it: api/Auth/AuthEnums.cs, api/Auth/PasswordResetEmailBody.cs, api/Data/Entities/AuthEntities.cs, api/Features/Ai/Tools/Actions/CommercialActions.XeroAllocation.cs.
468. Give `OnParametersSet` a home of its own, out of `jpms/Components/ValuationSnapshotEmailModal.razor`. 12 other files use it: jpms/Components/DrawingFolderOptions.razor, jpms/Components/DrawingsTable.razor, jpms/Components/KpiPersonPicker.razor, jpms/Components/ValuationReportTable.BulkPercent.cs.
469. Give `CreateDraftAsync` a home of its own, out of `jpms/Components/ValuationSnapshotEmailModal.razor`. 12 other files use it: api/Features/Commercial/Commands/PrepareValuationReportSnapshotEmailDraftHandler.cs, api/Features/MailboxIntake/Compose/SendMailboxEmailHandler.Draft.cs, api/Features/MailboxIntake/Graph/IMailboxGraphClient.cs, api/Features/MailboxIntake/Graph/NullMailboxGraphClient.cs.
470. Give `BuildDefaultBody` a home of its own, out of `jpms/Components/ValuationSnapshotEmailModal.razor`. 1 other files use it: jpms/Components/SubcontractorStatementModal.razor.
471. Give `OnParametersSet` a home of its own, out of `jpms/Features/Sales/ProposalFormModal.razor`. 12 other files use it: jpms/Components/DrawingFolderOptions.razor, jpms/Components/DrawingsTable.razor, jpms/Components/KpiPersonPicker.razor, jpms/Components/ValuationReportTable.BulkPercent.cs.
472. Give `Cancel` a home of its own, out of `jpms/Features/Sales/ProposalFormModal.razor`. 114 other files use it: api/Features/Commercial/Commands/RemoveValuationLineItemHandler.cs, api/Features/Commercial/Commands/ReopenValuationClaimHandler.cs, api/Features/Procurement/Commands/DeleteBidPackageHandler.cs, api/Features/Procurement/Commands/DeleteDraftWorkOrderHandler.cs.
473. Give `SaveAsync` a home of its own, out of `jpms/Features/Sales/ProposalFormModal.razor`. 29 other files use it: api/Features/Procurement/Attachments/CompanyTenderTermsEndpoints.cs, api/Features/Sales/Imagine/ImaginePublicService.Submit.cs, api/Features/Sales/Imagine/ImagineRenderRunner.cs, jpms/Components/ApprovedUserRow.razor.
474. Give `OnParametersSet` a home of its own, out of `jpms/Components/ProgressReportForm.razor`. 12 other files use it: jpms/Components/DrawingFolderOptions.razor, jpms/Components/DrawingsTable.razor, jpms/Components/KpiPersonPicker.razor, jpms/Components/ValuationReportTable.BulkPercent.cs.
475. Give `Toggle` a home of its own, out of `jpms/Components/ProgressReportForm.razor`. 11 other files use it: jpms/Components/Checkbox.razor, jpms/Components/ValuationClaimCorrespondenceSection.razor, jpms/Components/ValuationInvoicesSection.razor, jpms/Components/ValuationInvoicesSection.razor.cs.
476. Give `RemoveAsync` a home of its own, out of `jpms/Features/Triage/Panels/OutboxPane.razor`. 10 other files use it: jpms/Components/ApprovedUserRow.razor, jpms/Components/ManualWorkOrderModal.razor.cs, jpms/Components/PackageReconciliationSection.razor, jpms/Components/PartyContactsEditor.razor.
477. Give `DisplayFrom` a home of its own, out of `jpms/Features/Triage/Panels/OutboxPane.razor`. 5 other files use it: jpms/Features/Triage/Queue/TriageBar.razor, jpms/Features/Triage/Queue/TriageEmailRow.razor, jpms/Features/Triage/Queue/TriageMessageDetail.razor, jpms/Features/Triage/TriageEmailDisplay.cs.
478. Give `OnInitializedAsync` a home of its own, out of `jpms/Pages/ProjectReconciliationAudit.razor`. 26 other files use it: jpms/App.razor, jpms/Components/ExpiringDocumentsPanel.razor, jpms/Components/PackageReconciliationSection.razor.cs, jpms/Components/ProjectCorrespondencePanel.razor.cs.
479. Give `LoadAsync` a home of its own, out of `jpms/Pages/ProjectReconciliationAudit.razor`. 18 other files use it: api/Features/Ai/Tools/AiValuationInvoiceTools.cs, api/Features/Commercial/Documents/ValuationReportSnapshotPdfBuilder.cs, api/Features/MailboxIntake/Graph/MailboxGraphClient.Tagging.cs, api/Features/Progress/ContractorsReports/Documents/ContractorsReportBuilder.cs.
480. Give `Ago` a home of its own, out of `jpms/Pages/ProjectReconciliationAudit.razor`. 3 other files use it: jpms/Features/Procurement/SupplierAccountModal.razor, jpms/Pages/AuditTrail.razor, jpms/Pages/ProjectWorkOrders.razor.
481. Give `WebsiteHref` a home of its own, out of `jpms/Features/Procurement/InvitedSubcontractorsSection.razor`. 2 other files use it: jpms/Pages/SubcontractorDetail.razor, jpms/Pages/SubcontractorDetail.razor.cs.
482. Give `OnInitialized` a home of its own, out of `jpms/Features/Triage/Panels/Actions/StageTodosAction.razor`. 18 other files use it: jpms/App.razor, jpms/Components/ApprovedSessionGate.razor, jpms/Components/ApprovedUsersPanel.razor, jpms/Components/DrawingFolderPicker.razor.
483. Give `OnParametersSet` a home of its own, out of `jpms/Features/Triage/TodosModal.razor`. 12 other files use it: jpms/Components/DrawingFolderOptions.razor, jpms/Components/DrawingsTable.razor, jpms/Components/KpiPersonPicker.razor, jpms/Components/ValuationReportTable.BulkPercent.cs.
484. Give `AssigneeLabel` a home of its own, out of `jpms/Features/Triage/TodosModal.razor`. 1 other files use it: api/Features/Ai/Tools/AiToolCatalogue.TodoBrief.cs.
485. Give `CloseAsync` a home of its own, out of `jpms/Features/Triage/TodosModal.razor`. 7 other files use it: jpms/Features/Procurement/TenderInviteComposerModal.razor, jpms/Features/Triage/Panels/Actions/StageRequestTransitionAction.razor, jpms/Features/Triage/Panels/PathwayPane.razor, jpms/Pages/ProjectHsAudit.razor.
486. Give `RowClass` a home of its own, out of `jpms/Features/Triage/Panels/RecordLinkSection.razor`. 3 other files use it: jpms/Components/ProjectSelect.razor, jpms/Features/DocumentControl/DocumentListItem.razor, jpms/Features/Triage/Queue/TriageEmailRow.razor.
487. Give `Filter` a home of its own, out of `jpms/Features/Triage/Panels/RecordLinkSection.razor`. 20 other files use it: api/Features/Ai/Tools/AiCommercialTools.Valuation.cs, api/Features/Ai/Tools/AiDeliveryTools.Hs.cs, api/Features/Ai/Tools/AiKpiTools.cs, api/Features/Ai/Tools/AiMailboxTools.cs.
488. Give `Score` a home of its own, out of `jpms/Features/Triage/Panels/RecordLinkSection.razor`. 13 other files use it: api/Data/Entities/CrmEntities.cs, api/Data/Entities/HsAuditEntities.cs, api/Data/JpmsContext.Model.cs, api/Features/Ai/Tools/AiDeliveryTools.Hs.cs.
489. Give `LoadMoreAsync` a home of its own, out of `jpms/Features/Triage/Panels/CategoryRegisterSection.razor`. 7 other files use it: jpms/Components/RecordAuditHistory.razor, jpms/Pages/AuditTrail.razor, jpms/Pages/AuditTrail.razor.cs, jpms/Pages/ProjectCommunications.razor.
490. Give `OnParametersSet` a home of its own, out of `jpms/Features/Triage/Panels/Actions/StageRequestTransitionAction.razor`. 12 other files use it: jpms/Components/DrawingFolderOptions.razor, jpms/Components/DrawingsTable.razor, jpms/Components/KpiPersonPicker.razor, jpms/Components/ValuationReportTable.BulkPercent.cs.
491. Give `static` a home of its own, out of `jpms/Features/Triage/Panels/Actions/StageRequestTransitionAction.razor`. 1654 other files use it: api/Auth/AuthEnums.cs, api/Auth/AuthTokens.cs, api/Auth/InviteEmailBody.cs, api/Auth/PasswordHasher.cs.
492. Give `OnFilesSelected` a home of its own, out of `jpms/Components/ProgressUpdateForm.razor`. 4 other files use it: jpms/Components/DrawingUploadForm.razor, jpms/Components/RequestForm.razor, jpms/Features/Procurement/PackageDocumentsSection.razor, jpms/Pages/ProjectBidPackageInviteDetail.razor.
493. Give `Money` a home of its own, out of `jpms/Features/Cvr/CumulativePositionPanel.razor`. 122 other files use it: api/Data/Entities/SalesEntities.cs, api/Features/Ai/Tools/Actions/LabourAndBackOfficeActions.Rates.cs, api/Features/Ai/Tools/Actions/VariationsAndValuationsActions.ValuationInvoices.cs, api/Features/Commercial/Documents/CostCentreReconciliationRenderer.Header.cs.
494. Give `OnInitializedAsync` a home of its own, out of `jpms/Pages/ConnectAuthorize.razor`. 26 other files use it: jpms/App.razor, jpms/Components/ExpiringDocumentsPanel.razor, jpms/Components/PackageReconciliationSection.razor.cs, jpms/Components/ProjectCorrespondencePanel.razor.cs.
495. Give `DecideAsync` a home of its own, out of `jpms/Pages/ConnectAuthorize.razor`. 1 other files use it: jpms/Features/Procurement/DraftWorkOrdersPanel.razor.
496. Give `DisposeAsync` a home of its own, out of `jpms/Components/PdfViewer.razor`. 1 other files use it: jpms/Features/Triage/RichTextEditor.razor.
497. Give `OnInitializedAsync` a home of its own, out of `jpms/Features/Closeout/Detail/DefectTodosPanel.razor`. 26 other files use it: jpms/App.razor, jpms/Components/ExpiringDocumentsPanel.razor, jpms/Components/PackageReconciliationSection.razor.cs, jpms/Components/ProjectCorrespondencePanel.razor.cs.
498. Give `LoadAsync` a home of its own, out of `jpms/Features/Closeout/Detail/DefectTodosPanel.razor`. 18 other files use it: api/Features/Ai/Tools/AiValuationInvoiceTools.cs, api/Features/Commercial/Documents/ValuationReportSnapshotPdfBuilder.cs, api/Features/MailboxIntake/Graph/MailboxGraphClient.Tagging.cs, api/Features/Progress/ContractorsReports/Documents/ContractorsReportBuilder.cs.
499. Give `OpenAdd` a home of its own, out of `jpms/Features/Closeout/Detail/DefectTodosPanel.razor`. 7 other files use it: jpms/Components/ProjectTodoList.razor, jpms/Features/WeeklyCashflow/CashflowItemModal.razor, jpms/Pages/CostCodes.razor, jpms/Pages/ProjectCalendar.razor.
500. Give `CloseAdd` a home of its own, out of `jpms/Features/Closeout/Detail/DefectTodosPanel.razor`. 2 other files use it: jpms/Components/ProjectTodoList.razor, jpms/Pages/Todos.razor.
501. Give `AddAsync` a home of its own, out of `jpms/Features/Closeout/Detail/DefectTodosPanel.razor`. 14 other files use it: api/Features/Ai/Tools/AiProgressPhotoTools.cs, api/Features/Progress/Commands/AddProgressPhotosEndpoint.cs, jpms/Components/ProjectTodoList.razor.cs, jpms/Components/UsefulInformationPanel.razor.
502. Give `OpenSnapshotEmail` a home of its own, out of `jpms/Pages/ProjectValuationSnapshots.razor`. 1 other files use it: jpms/Pages/ProjectValuation.razor.
503. Give `InvoiceFor` a home of its own, out of `jpms/Pages/ProjectValuationSnapshots.razor`. 1 other files use it: jpms/Pages/ProjectValuation.Invoices.cs.
504. Give `OnInitialized` a home of its own, out of `jpms/Features/Triage/Panels/Actions/StageKpiAction.razor`. 18 other files use it: jpms/App.razor, jpms/Components/ApprovedSessionGate.razor, jpms/Components/ApprovedUsersPanel.razor, jpms/Components/DrawingFolderPicker.razor.
505. Give `Match` a home of its own, out of `jpms/Features/Triage/Panels/Actions/StageKpiAction.razor`. 27 other files use it: api/Features/Ai/Tools/AiCommercialTools.Variation.cs, api/Features/Ai/Tools/AiCommercialTools.cs, api/Features/Ai/Tools/AiRecordTools.XeroCustomers.cs, api/Features/Ai/Tools/AiSalesTools.cs.
506. Give `OnInitializedAsync` a home of its own, out of `jpms/Pages/SetPassword.razor`. 26 other files use it: jpms/App.razor, jpms/Components/ExpiringDocumentsPanel.razor, jpms/Components/PackageReconciliationSection.razor.cs, jpms/Components/ProjectCorrespondencePanel.razor.cs.
507. Give `OnParametersSetAsync` a home of its own, out of `jpms/Components/RecordAuditHistory.razor`. 2 other files use it: jpms/Components/RequestConversation.razor.cs, jpms/Features/Triage/Panels/RecordLinkSection.razor.
508. Give `LoadAsync` a home of its own, out of `jpms/Components/RecordAuditHistory.razor`. 18 other files use it: api/Features/Ai/Tools/AiValuationInvoiceTools.cs, api/Features/Commercial/Documents/ValuationReportSnapshotPdfBuilder.cs, api/Features/MailboxIntake/Graph/MailboxGraphClient.Tagging.cs, api/Features/Progress/ContractorsReports/Documents/ContractorsReportBuilder.cs.
509. Give `EventLabel` a home of its own, out of `jpms/Components/RecordAuditHistory.razor`. 3 other files use it: jpms/Components/ValuationInvoicesSection.razor, jpms/Pages/AuditTrail.razor, jpms/Pages/AuditTrail.razor.cs.
510. Give `OnParametersSetAsync` a home of its own, out of `jpms/Components/WorkOrderAttachmentsPanel.razor`. 2 other files use it: jpms/Components/RequestConversation.razor.cs, jpms/Features/Triage/Panels/RecordLinkSection.razor.
511. Give `LoadAsync` a home of its own, out of `jpms/Components/WorkOrderAttachmentsPanel.razor`. 18 other files use it: api/Features/Ai/Tools/AiValuationInvoiceTools.cs, api/Features/Commercial/Documents/ValuationReportSnapshotPdfBuilder.cs, api/Features/MailboxIntake/Graph/MailboxGraphClient.Tagging.cs, api/Features/Progress/ContractorsReports/Documents/ContractorsReportBuilder.cs.
512. Give `OnFilesSelected` a home of its own, out of `jpms/Components/WorkOrderAttachmentsPanel.razor`. 4 other files use it: jpms/Components/DrawingUploadForm.razor, jpms/Components/RequestForm.razor, jpms/Features/Procurement/PackageDocumentsSection.razor, jpms/Pages/ProjectBidPackageInviteDetail.razor.
513. Give `Remove` a home of its own, out of `jpms/Components/WorkOrderAttachmentsPanel.razor`. 214 other files use it: api/Features/AccessRequests/Commands/ResolveAccessRequestHandler.cs, api/Features/Ai/Skills/SaveAiActionSkillsHandler.cs, api/Features/ArchitectInstructions/ArchitectInstructionHandlers.cs, api/Features/Bluebeam/BluebeamStatusEndpoints.cs.
514. Give `Size` a home of its own, out of `jpms/Components/WorkOrderAttachmentsPanel.razor`. 70 other files use it: api/Data/Entities/SalesEntities.cs, api/Features/Ai/Sources/AiFiledDocuments.cs, api/Features/Ai/Tools/AiRecordTools.Correspondence.cs, api/Features/Ai/Tools/AiSourceTools.EmailAttachments.cs.
515. Give `triage` a home of its own, out of `jpms/Pages/AuditTrail.razor`. 275 other files use it: api/Data/Entities/AuditEventEntity.cs, api/Data/Entities/CalendarEventEntities.cs, api/Data/Entities/DocumentControlEntities.cs, api/Data/Entities/TodoEntities.cs.
516. Give `OnInitializedAsync` a home of its own, out of `jpms/Pages/PortalWorkOrderView.razor`. 26 other files use it: jpms/App.razor, jpms/Components/ExpiringDocumentsPanel.razor, jpms/Components/PackageReconciliationSection.razor.cs, jpms/Components/ProjectCorrespondencePanel.razor.cs.
517. Give `DisposeAsync` a home of its own, out of `jpms/Features/Sales/LeadHouseModelPanel.razor`. 1 other files use it: jpms/Features/Triage/RichTextEditor.razor.
518. Give `OnInitialized` a home of its own, out of `jpms/Features/Triage/Panels/Actions/StageCalendarEventAction.razor`. 18 other files use it: jpms/App.razor, jpms/Components/ApprovedSessionGate.razor, jpms/Components/ApprovedUsersPanel.razor, jpms/Components/DrawingFolderPicker.razor.
519. Give `OnParametersSet` a home of its own, out of `jpms/Features/Triage/Panels/Actions/StageCalendarEventAction.razor`. 12 other files use it: jpms/Components/DrawingFolderOptions.razor, jpms/Components/DrawingsTable.razor, jpms/Components/KpiPersonPicker.razor, jpms/Components/ValuationReportTable.BulkPercent.cs.
520. Give `OnKindChanged` a home of its own, out of `jpms/Features/Triage/Panels/Actions/StageCalendarEventAction.razor`. 2 other files use it: jpms/Features/Triage/Panels/PathwayActionsSection.razor, jpms/Pages/ProjectCalendar.razor.
521. Give `Set` a home of its own, out of `jpms/Features/Triage/Panels/Actions/StageCalendarEventAction.razor`. 97 other files use it: api/Auth/InviteEmailBody.cs, api/Auth/SessionCookie.cs, api/Data/Entities/ConnectEntities.cs, api/Data/Entities/CoreEntities.cs.
522. Give `NotifyCreate` a home of its own, out of `jpms/Features/Triage/Panels/Actions/StageCalendarEventAction.razor`. 1 other files use it: jpms/Features/Triage/Panels/Actions/StagedRecordActionEditor.razor.
523. Give `OnInitializedAsync` a home of its own, out of `jpms/Features/Triage/Panels/XeroTransactionView.razor`. 26 other files use it: jpms/App.razor, jpms/Components/ExpiringDocumentsPanel.razor, jpms/Components/PackageReconciliationSection.razor.cs, jpms/Components/ProjectCorrespondencePanel.razor.cs.
524. Give `IsPreviewable` a home of its own, out of `jpms/Features/Triage/Panels/XeroTransactionView.razor`. 3 other files use it: jpms/Features/Triage/Panels/CorrespondenceThreadList.razor, jpms/Features/Triage/Queue/TriageMessageDetail.razor, jpms/Features/Triage/TriageEmailDisplay.cs.
525. Give `Money` a home of its own, out of `jpms/Features/Triage/Panels/XeroTransactionView.razor`. 122 other files use it: api/Data/Entities/SalesEntities.cs, api/Features/Ai/Tools/Actions/LabourAndBackOfficeActions.Rates.cs, api/Features/Ai/Tools/Actions/VariationsAndValuationsActions.ValuationInvoices.cs, api/Features/Commercial/Documents/CostCentreReconciliationRenderer.Header.cs.
526. Give `OnInitializedAsync` a home of its own, out of `jpms/Pages/SalesStrategies.razor`. 26 other files use it: jpms/App.razor, jpms/Components/ExpiringDocumentsPanel.razor, jpms/Components/PackageReconciliationSection.razor.cs, jpms/Components/ProjectCorrespondencePanel.razor.cs.
527. Give `DisposeAsync` a home of its own, out of `jpms/Components/ImageViewer.razor`. 1 other files use it: jpms/Features/Triage/RichTextEditor.razor.
528. Give `OnInitializedAsync` a home of its own, out of `jpms/Components/ProjectPageShell.razor`. 26 other files use it: jpms/App.razor, jpms/Components/ExpiringDocumentsPanel.razor, jpms/Components/PackageReconciliationSection.razor.cs, jpms/Components/ProjectCorrespondencePanel.razor.cs.
529. Give `Dispose` a home of its own, out of `jpms/Components/ProjectPageShell.razor`. 73 other files use it: api/Features/Bluebeam/BluebeamClient.cs, api/Features/Progress/WhatsApp/WhatsAppExportArchive.cs, jpms/App.razor, jpms/Components/AdminKpiPanel.razor.
530. Give `OnInitializedAsync` a home of its own, out of `jpms/Components/NewProjectForm.razor`. 26 other files use it: jpms/App.razor, jpms/Components/ExpiringDocumentsPanel.razor, jpms/Components/PackageReconciliationSection.razor.cs, jpms/Components/ProjectCorrespondencePanel.razor.cs.
531. Give `Submit` a home of its own, out of `jpms/Components/NewProjectForm.razor`. 6 other files use it: api/Features/Sales/Imagine/ImaginePublicService.cs, api/Features/ValuationInvoices/ValuationInvoicesFeatureRegistration.cs, jpms/Components/MyDayWorkspace.razor, jpms/Components/RequestAccessView.razor.
532. Give `Edit` a home of its own, out of `jpms/Features/Hs/Audits/HsAuditSectionPanel.razor`. 100 other files use it: api/Data/Entities/PeopleEntities.cs, api/Data/Entities/ProcurementEntities.cs, api/Features/Ai/Tools/AiToolCatalogue.Procurement.cs, api/Features/Labour/Commands/DeleteWorkerSlice.cs.
533. Give `SaveAsync` a home of its own, out of `jpms/Features/Hs/Audits/HsAuditSectionPanel.razor`. 29 other files use it: api/Features/Procurement/Attachments/CompanyTenderTermsEndpoints.cs, api/Features/Sales/Imagine/ImaginePublicService.Submit.cs, api/Features/Sales/Imagine/ImagineRenderRunner.cs, jpms/Components/ApprovedUserRow.razor.
534. Give `ParseInt` a home of its own, out of `jpms/Features/Hs/Audits/HsAuditSectionPanel.razor`. 1 other files use it: jpms/Features/Sales/ProposalFormModal.razor.
535. Give `ParseDate` a home of its own, out of `jpms/Features/Hs/Audits/HsAuditSectionPanel.razor`. 13 other files use it: jpms/Components/ProjectTodoList.razor.cs, jpms/Features/Requests/RequestFactsEditModal.razor, jpms/Features/Requests/RequestHeaderEditModal.razor, jpms/Features/Sales/EstimateFigures.cs.
536. Give `OnInitializedAsync` a home of its own, out of `jpms/Pages/AiConnections.razor`. 26 other files use it: jpms/App.razor, jpms/Components/ExpiringDocumentsPanel.razor, jpms/Components/PackageReconciliationSection.razor.cs, jpms/Components/ProjectCorrespondencePanel.razor.cs.
537. Give `LoadAsync` a home of its own, out of `jpms/Pages/AiConnections.razor`. 18 other files use it: api/Features/Ai/Tools/AiValuationInvoiceTools.cs, api/Features/Commercial/Documents/ValuationReportSnapshotPdfBuilder.cs, api/Features/MailboxIntake/Graph/MailboxGraphClient.Tagging.cs, api/Features/Progress/ContractorsReports/Documents/ContractorsReportBuilder.cs.
538. Give `RevokeAsync` a home of its own, out of `jpms/Pages/AiConnections.razor`. 1 other files use it: api/Features/Auth/LogoutEndpoint.cs.
539. Give `OnParametersSet` a home of its own, out of `jpms/Features/Variations/ApprovedFiguresPanel.razor`. 12 other files use it: jpms/Components/DrawingFolderOptions.razor, jpms/Components/DrawingsTable.razor, jpms/Components/KpiPersonPicker.razor, jpms/Components/ValuationReportTable.BulkPercent.cs.
540. Give `OnInitialized` a home of its own, out of `jpms/Components/KpiPersonPicker.razor`. 18 other files use it: jpms/App.razor, jpms/Components/ApprovedSessionGate.razor, jpms/Components/ApprovedUsersPanel.razor, jpms/Components/DrawingFolderPicker.razor.
541. Give `Repaint` a home of its own, out of `jpms/Components/KpiPersonPicker.razor`. 3 other files use it: jpms/Features/Procurement/LocalSubcontractorFinderModal.razor.cs, jpms/Layout/MainLayout.razor, jpms/Pages/ProjectRequestDetail.razor.cs.
542. Give `Parse` a home of its own, out of `jpms/Components/KpiPersonPicker.razor`. 48 other files use it: api/Features/Ai/Tools/AiCommercialTools.Variation.cs, api/Features/Bluebeam/BluebeamClient.Sessions.cs, api/Features/Bluebeam/BluebeamClient.cs, api/Features/Bluebeam/Extraction/DrawingExtractionResultWriter.cs.
543. Give `Dispose` a home of its own, out of `jpms/Components/KpiPersonPicker.razor`. 73 other files use it: api/Features/Bluebeam/BluebeamClient.cs, api/Features/Progress/WhatsApp/WhatsAppExportArchive.cs, jpms/App.razor, jpms/Components/AdminKpiPanel.razor.
544. Give `BuildExportWorkbook` a home of its own, out of `jpms/Pages/Projects.razor`. 26 other files use it: jpms/Components/CostCentreSalesLinesModal.razor, jpms/Components/FinancialsTable.razor, jpms/Components/PackageReconciliationSection.razor, jpms/Components/ValuationInvoicesSection.razor.
545. Give `OnInitializedAsync` a home of its own, out of `jpms/Pages/Projects.razor`. 26 other files use it: jpms/App.razor, jpms/Components/ExpiringDocumentsPanel.razor, jpms/Components/PackageReconciliationSection.razor.cs, jpms/Components/ProjectCorrespondencePanel.razor.cs.
546. Give `Dispose` a home of its own, out of `jpms/Pages/Projects.razor`. 73 other files use it: api/Features/Bluebeam/BluebeamClient.cs, api/Features/Progress/WhatsApp/WhatsAppExportArchive.cs, jpms/App.razor, jpms/Components/AdminKpiPanel.razor.
547. Give `Submit` a home of its own, out of `jpms/Components/InviteUserForm.razor`. 6 other files use it: api/Features/Sales/Imagine/ImaginePublicService.cs, api/Features/ValuationInvoices/ValuationInvoicesFeatureRegistration.cs, jpms/Components/MyDayWorkspace.razor, jpms/Components/RequestAccessView.razor.
548. Give `Reset` a home of its own, out of `jpms/Components/InviteUserForm.razor`. 34 other files use it: api/Auth/AuthEnums.cs, api/Auth/PasswordResetEmailBody.cs, api/Data/Entities/AuthEntities.cs, api/Features/Ai/Tools/Actions/CommercialActions.XeroAllocation.cs.
549. Give `OnParametersSetAsync` a home of its own, out of `jpms/Features/Triage/RichTextEditor.razor`. 2 other files use it: jpms/Components/RequestConversation.razor.cs, jpms/Features/Triage/Panels/RecordLinkSection.razor.
550. Give `readonly` a home of its own, out of `jpms/Features/Triage/RichTextEditor.razor`. 1695 other files use it: api/Auth/AuthEnums.cs, api/Auth/AzureEmailInviteNotifier.cs, api/Auth/LoggingInviteNotifier.cs, api/Auth/SessionCookie.cs.
551. Give `Refresh` a home of its own, out of `jpms/Components/ErrorToast.razor`. 95 other files use it: api/Data/Entities/ConnectEntities.cs, api/Features/Bluebeam/BluebeamTokenService.cs, api/Features/Connect/OAuthDefaults.cs, api/Features/Connect/OAuthTokenManager.cs.
552. Give `Copy` a home of its own, out of `jpms/Components/ErrorToast.razor`. 14 other files use it: api/Features/Commercial/Commands/StartValuationClaimHandler.cs, contracts/Ai/PageGuides/SitePageGuides.cs, jpms/Components/ApprovedUserRow.razor, jpms/Components/InviteUserForm.razor.
553. Give `OnParametersSetAsync` a home of its own, out of `jpms/Features/Triage/Panels/Actions/StageVariationDecisionAction.razor`. 2 other files use it: jpms/Components/RequestConversation.razor.cs, jpms/Features/Triage/Panels/RecordLinkSection.razor.
554. Give `OnParametersSet` a home of its own, out of `jpms/Features/Variations/StagedBuildUpPanel.razor`. 12 other files use it: jpms/Components/DrawingFolderOptions.razor, jpms/Components/DrawingsTable.razor, jpms/Components/KpiPersonPicker.razor, jpms/Components/ValuationReportTable.BulkPercent.cs.
555. Give `Seed` a home of its own, out of `jpms/Features/Variations/StagedBuildUpPanel.razor`. 6 other files use it: api/Features/Architects/Commands/CreateArchitectHandler.cs, api/Features/Clients/Commands/CreateClientHandler.cs, api/Features/Commercial/Commands/StartValuationClaimHandler.cs, jpms/Components/AddManualVariationDialog.razor.
556. Give `Close` a home of its own, out of `jpms/Features/Variations/StagedBuildUpPanel.razor`. 58 other files use it: api/Features/Drawings/Geometry/PdfGeometryExtractor.cs, api/Features/Labour/Commands/SettlementLineAndMappingSlices.cs, api/Features/Requests/Commands/MergeRequestsHandler.cs, contracts/Ai/PageGuides/SitePageGuides.cs.
557. Give `OnParametersSet` a home of its own, out of `jpms/Features/Variations/VariationDocumentPanel.razor`. 12 other files use it: jpms/Components/DrawingFolderOptions.razor, jpms/Components/DrawingsTable.razor, jpms/Components/KpiPersonPicker.razor, jpms/Components/ValuationReportTable.BulkPercent.cs.
558. Give `Seed` a home of its own, out of `jpms/Features/Variations/VariationDocumentPanel.razor`. 6 other files use it: api/Features/Architects/Commands/CreateArchitectHandler.cs, api/Features/Clients/Commands/CreateClientHandler.cs, api/Features/Commercial/Commands/StartValuationClaimHandler.cs, jpms/Components/AddManualVariationDialog.razor.
559. Give `CancelAsync` a home of its own, out of `jpms/Features/Variations/VariationDocumentPanel.razor`. 3 other files use it: jpms/Components/ValuationInvoicesSection.Menu.cs, jpms/Pages/ProjectWorkOrders.razor, jpms/Services/IValuationInvoiceStore.cs.
560. Give `SaveAsync` a home of its own, out of `jpms/Features/Variations/VariationDocumentPanel.razor`. 29 other files use it: api/Features/Procurement/Attachments/CompanyTenderTermsEndpoints.cs, api/Features/Sales/Imagine/ImaginePublicService.Submit.cs, api/Features/Sales/Imagine/ImagineRenderRunner.cs, jpms/Components/ApprovedUserRow.razor.
561. Give `OnParametersSet` a home of its own, out of `jpms/Features/Requests/RequestFactsEditModal.razor`. 12 other files use it: jpms/Components/DrawingFolderOptions.razor, jpms/Components/DrawingsTable.razor, jpms/Components/KpiPersonPicker.razor, jpms/Components/ValuationReportTable.BulkPercent.cs.
562. Give `SaveAsync` a home of its own, out of `jpms/Features/Requests/RequestFactsEditModal.razor`. 29 other files use it: api/Features/Procurement/Attachments/CompanyTenderTermsEndpoints.cs, api/Features/Sales/Imagine/ImaginePublicService.Submit.cs, api/Features/Sales/Imagine/ImagineRenderRunner.cs, jpms/Components/ApprovedUserRow.razor.
563. Give `OnInitialized` a home of its own, out of `jpms/Features/Labour/WorkerDetailPanel.razor`. 18 other files use it: jpms/App.razor, jpms/Components/ApprovedSessionGate.razor, jpms/Components/ApprovedUsersPanel.razor, jpms/Components/DrawingFolderPicker.razor.
564. Give `SaveAsync` a home of its own, out of `jpms/Features/Labour/WorkerDetailPanel.razor`. 29 other files use it: api/Features/Procurement/Attachments/CompanyTenderTermsEndpoints.cs, api/Features/Sales/Imagine/ImaginePublicService.Submit.cs, api/Features/Sales/Imagine/ImagineRenderRunner.cs, jpms/Components/ApprovedUserRow.razor.
565. Give `OnParametersSetAsync` a home of its own, out of `jpms/Features/Triage/Panels/Actions/StageTodoCompleteAction.razor`. 2 other files use it: jpms/Components/RequestConversation.razor.cs, jpms/Features/Triage/Panels/RecordLinkSection.razor.
566. Give `OnParametersSetAsync` a home of its own, out of `jpms/Features/Xero/InvoiceDocumentPreview.razor`. 2 other files use it: jpms/Components/RequestConversation.razor.cs, jpms/Features/Triage/Panels/RecordLinkSection.razor.
567. Give `IsPreviewable` a home of its own, out of `jpms/Features/Xero/InvoiceDocumentPreview.razor`. 3 other files use it: jpms/Features/Triage/Panels/CorrespondenceThreadList.razor, jpms/Features/Triage/Queue/TriageMessageDetail.razor, jpms/Features/Triage/TriageEmailDisplay.cs.
568. Give `ChipClass` a home of its own, out of `jpms/Features/Xero/InvoiceDocumentPreview.razor`. 2 other files use it: jpms/Features/Xero/Allocation/BucketChipStrip.razor, jpms/Pages/SubcontractorCommunications.razor.
569. Give `OnParametersSet` a home of its own, out of `jpms/Features/Requests/RequestHeaderEditModal.razor`. 12 other files use it: jpms/Components/DrawingFolderOptions.razor, jpms/Components/DrawingsTable.razor, jpms/Components/KpiPersonPicker.razor, jpms/Components/ValuationReportTable.BulkPercent.cs.
570. Give `OnStatusChanged` a home of its own, out of `jpms/Features/Requests/RequestHeaderEditModal.razor`. 2 other files use it: jpms/Components/RequestForm.razor, jpms/Components/RequestForm.razor.cs.
571. Give `SaveAsync` a home of its own, out of `jpms/Features/Requests/RequestHeaderEditModal.razor`. 29 other files use it: api/Features/Procurement/Attachments/CompanyTenderTermsEndpoints.cs, api/Features/Sales/Imagine/ImaginePublicService.Submit.cs, api/Features/Sales/Imagine/ImagineRenderRunner.cs, jpms/Components/ApprovedUserRow.razor.
572. Give `StartCompose` a home of its own, out of `jpms/Features/Triage/Panels/RecordCorrespondencePanel.razor`. 4 other files use it: jpms/Pages/ProjectBuildingControlInspection.razor, jpms/Pages/ProjectCommunications.razor, jpms/Pages/SubcontractorCommunications.razor, jpms/Pages/SubcontractorCommunications.razor.cs.
573. Give `OnSent` a home of its own, out of `jpms/Features/Triage/Panels/RecordCorrespondencePanel.razor`. 15 other files use it: jpms/Features/Closeout/Detail/DefectCommunicationsPanel.razor, jpms/Features/Closeout/Detail/DefectCommunicationsPanel.razor.cs, jpms/Features/Closeout/Detail/DefectEmailComposer.razor, jpms/Features/Procurement/TenderInviteComposerModal.razor.cs.
574. Give `OnInitialized` a home of its own, out of `jpms/Components/ProjectContractAmendmentDialog.razor`. 18 other files use it: jpms/App.razor, jpms/Components/ApprovedSessionGate.razor, jpms/Components/ApprovedUsersPanel.razor, jpms/Components/DrawingFolderPicker.razor.
575. Give `SaveAsync` a home of its own, out of `jpms/Components/ProjectContractAmendmentDialog.razor`. 29 other files use it: api/Features/Procurement/Attachments/CompanyTenderTermsEndpoints.cs, api/Features/Sales/Imagine/ImaginePublicService.Submit.cs, api/Features/Sales/Imagine/ImagineRenderRunner.cs, jpms/Components/ApprovedUserRow.razor.
576. Give `AsOffset` a home of its own, out of `jpms/Components/ProjectContractAmendmentDialog.razor`. 1 other files use it: jpms/Components/ProjectContractTermsDialog.razor.cs.
577. Give `SiteAddress` a home of its own, out of `jpms/Pages/ProjectSettings.razor`. 7 other files use it: api/Data/Entities/CoreEntities.cs, api/Features/Ai/Tools/AiToolCatalogue.Lookup.Sales.cs, api/Features/RecordLinks/Providers/LeadLinkProvider.cs, api/Features/Sales/Commands/LeadHandlers.cs.
578. Give `IsLiveJob` a home of its own, out of `jpms/Components/ProjectMultiSelect.razor`. 1 other files use it: jpms/Pages/CashForecast.Assumptions.cs.
579. Give `OnInitialized` a home of its own, out of `jpms/Pages/Login.razor`. 18 other files use it: jpms/App.razor, jpms/Components/ApprovedSessionGate.razor, jpms/Components/ApprovedUsersPanel.razor, jpms/Components/DrawingFolderPicker.razor.
580. Give `Restore` a home of its own, out of `jpms/Components/RevokedUserRow.razor`. 18 other files use it: api/Features/Ai/Tools/Actions/ProcurementActions.Tenders.cs, api/Features/Clients/Commands/ClientPortalInviter.cs, api/Features/DocumentControl/Commands/DiscardDocumentControlItemHandler.cs, api/Features/Procurement/BidPackageEmailDispositionStore.cs.
581. Give `Delete` a home of its own, out of `jpms/Components/RevokedUserRow.razor`. 48 other files use it: api/Features/Bluebeam/BluebeamClient.Sessions.cs, api/Features/MailboxIntake/Graph/MailboxGraphClient.Drafts.cs, api/Features/Sales/SalesRoles.cs, contracts/Ai/PageGuides/CommercialPageGuides.cs.
582. Give `OnParametersSet` a home of its own, out of `jpms/Components/RaiseRequestDialog.razor`. 12 other files use it: jpms/Components/DrawingFolderOptions.razor, jpms/Components/DrawingsTable.razor, jpms/Components/KpiPersonPicker.razor, jpms/Components/ValuationReportTable.BulkPercent.cs.
583. Give `Submit` a home of its own, out of `jpms/Components/RaiseRequestDialog.razor`. 6 other files use it: api/Features/Sales/Imagine/ImaginePublicService.cs, api/Features/ValuationInvoices/ValuationInvoicesFeatureRegistration.cs, jpms/Components/MyDayWorkspace.razor, jpms/Components/RequestAccessView.razor.
584. Give `SendAsync` a home of its own, out of `jpms/Components/RaiseRequestDialog.razor`. 144 other files use it: api/Auth/AzureEmailInviteNotifier.cs, api/Features/Ai/ClaudeClient.cs, api/Features/Ai/Scans/AzureVisionOcr.cs, api/Features/Auth/ForgotPasswordEndpoint.cs.
585. Give `OnInitialized` a home of its own, out of `jpms/Components/RoleOverridePrompt.razor`. 18 other files use it: jpms/App.razor, jpms/Components/ApprovedSessionGate.razor, jpms/Components/ApprovedUsersPanel.razor, jpms/Components/DrawingFolderPicker.razor.
586. Give `OnRevertDue` a home of its own, out of `jpms/Components/RoleOverridePrompt.razor`. 1 other files use it: jpms/Services/SessionService.cs.
587. Give `Tick` a home of its own, out of `jpms/Components/RoleOverridePrompt.razor`. 17 other files use it: api/Features/BuildingControl/Attachments/BuildingControlAttachmentHandlers.cs, jpms/Components/ManualWorkOrderModal.razor, jpms/Components/ReconciliationPackageBuilderModal.razor, jpms/Components/WorkOrderForm.razor.
588. Give `Keep` a home of its own, out of `jpms/Components/RoleOverridePrompt.razor`. 37 other files use it: api/Features/Ai/AiRoles.cs, api/Features/Procurement/Commands/ExtractTenderFromMessageHandler.cs, api/Features/RecordLinks/Commands/PrepareProgrammeReplyDraftHandler.cs, api/Features/Requests/Recipients/RequestRecipientResolver.cs.
589. Give `Close` a home of its own, out of `jpms/Components/RoleOverridePrompt.razor`. 58 other files use it: api/Features/Drawings/Geometry/PdfGeometryExtractor.cs, api/Features/Labour/Commands/SettlementLineAndMappingSlices.cs, api/Features/Requests/Commands/MergeRequestsHandler.cs, contracts/Ai/PageGuides/SitePageGuides.cs.
590. Give `Dispose` a home of its own, out of `jpms/Components/RoleOverridePrompt.razor`. 73 other files use it: api/Features/Bluebeam/BluebeamClient.cs, api/Features/Progress/WhatsApp/WhatsAppExportArchive.cs, jpms/App.razor, jpms/Components/AdminKpiPanel.razor.
591. Give `AuthorLabel` a home of its own, out of `jpms/Features/Requests/EmailDraftStagingModal.razor`. 1 other files use it: jpms/Components/RequestConversation.razor.
592. Give `OnInitialized` a home of its own, out of `jpms/Layout/MainLayout.razor`. 18 other files use it: jpms/App.razor, jpms/Components/ApprovedSessionGate.razor, jpms/Components/ApprovedUsersPanel.razor, jpms/Components/DrawingFolderPicker.razor.
593. Give `Dispose` a home of its own, out of `jpms/Layout/MainLayout.razor`. 73 other files use it: api/Features/Bluebeam/BluebeamClient.cs, api/Features/Progress/WhatsApp/WhatsAppExportArchive.cs, jpms/App.razor, jpms/Components/AdminKpiPanel.razor.
594. Give `OnParametersSet` a home of its own, out of `jpms/Features/Variations/VariationDetailsCard.razor`. 12 other files use it: jpms/Components/DrawingFolderOptions.razor, jpms/Components/DrawingsTable.razor, jpms/Components/KpiPersonPicker.razor, jpms/Components/ValuationReportTable.BulkPercent.cs.
595. Give `CancelAsync` a home of its own, out of `jpms/Features/Variations/VariationDetailsCard.razor`. 3 other files use it: jpms/Components/ValuationInvoicesSection.Menu.cs, jpms/Pages/ProjectWorkOrders.razor, jpms/Services/IValuationInvoiceStore.cs.
596. Give `OnParametersSetAsync` a home of its own, out of `jpms/Features/Triage/Panels/Actions/StageTenderResponseAction.razor`. 2 other files use it: jpms/Components/RequestConversation.razor.cs, jpms/Features/Triage/Panels/RecordLinkSection.razor.
597. Give `Inline` a home of its own, out of `jpms/Features/Sales/SimpleMarkdown.razor`. 9 other files use it: api/Features/Drawings/Queries/DownloadDrawingRevisionFileEndpoint.cs, api/Features/MailboxIntake/Compose/SendMailboxEmailHandler.Body.cs, api/Features/MailboxIntake/Graph/IntakeMessageReader.cs, api/Features/MailboxIntake/Sharing/EmailAttachmentPlanner.cs.
598. Give `Render` a home of its own, out of `jpms/Features/Sales/SimpleMarkdown.razor`. 35 other files use it: api/Features/Ai/Scans/ScannedPdfReading.cs, api/Features/Ai/Tools/AiSourceTools.ReadSource.cs, api/Features/Ai/Tools/AiValuationInvoiceTools.cs, api/Features/Commercial/Documents/CostCentreReconciliationPdfBuilder.cs.
599. Give `ResolveFolderAsync` a home of its own, out of `jpms/Components/DrawingFolderPicker.razor`. 2 other files use it: jpms/Components/DrawingUploadForm.razor.cs, jpms/Pages/DocumentControl.Filing.cs.
600. Give `Reset` a home of its own, out of `jpms/Components/DrawingFolderPicker.razor`. 34 other files use it: api/Auth/AuthEnums.cs, api/Auth/PasswordResetEmailBody.cs, api/Data/Entities/AuthEntities.cs, api/Features/Ai/Tools/Actions/CommercialActions.XeroAllocation.cs.
601. Give `OnParametersSet` a home of its own, out of `jpms/Components/DrawingFolderPicker.razor`. 12 other files use it: jpms/Components/DrawingFolderOptions.razor, jpms/Components/DrawingsTable.razor, jpms/Components/KpiPersonPicker.razor, jpms/Components/ValuationReportTable.BulkPercent.cs.
602. Give `Refresh` a home of its own, out of `jpms/Components/UpdateToast.razor`. 95 other files use it: api/Data/Entities/ConnectEntities.cs, api/Features/Bluebeam/BluebeamTokenService.cs, api/Features/Connect/OAuthDefaults.cs, api/Features/Connect/OAuthTokenManager.cs.
603. Give `DisposeAsync` a home of its own, out of `jpms/Components/UpdateToast.razor`. 1 other files use it: jpms/Features/Triage/RichTextEditor.razor.
604. Give `PartLabel` a home of its own, out of `jpms/Features/Labour/WeeklySignOffTable.razor`. 3 other files use it: api/Features/Ai/Sources/AiSourceDocument.cs, api/Features/Ai/Tools/AiSourceTools.FindInSource.cs, api/Features/Ai/Tools/AiSourceTools.ReadSource.cs.
605. Give `Open` a home of its own, out of `jpms/Features/Procurement/DeleteWorkOrderModal.razor`. 193 other files use it: api/Auth/InviteEmailBody.cs, api/Auth/PasswordResetEmailBody.cs, api/Data/Entities/CommercialEntities.cs, api/Data/Entities/TodoEntities.cs.
606. Give `Close` a home of its own, out of `jpms/Features/Procurement/DeleteWorkOrderModal.razor`. 58 other files use it: api/Features/Drawings/Geometry/PdfGeometryExtractor.cs, api/Features/Labour/Commands/SettlementLineAndMappingSlices.cs, api/Features/Requests/Commands/MergeRequestsHandler.cs, contracts/Ai/PageGuides/SitePageGuides.cs.
607. Give `DeleteAsync` a home of its own, out of `jpms/Features/Procurement/DeleteWorkOrderModal.razor`. 49 other files use it: api/Features/ArchitectInstructions/ArchitectInstructionHandlers.cs, api/Features/BuildingControl/Attachments/BuildingControlAttachmentHandlers.cs, api/Features/BuildingControl/Attachments/IBuildingControlAttachmentStore.cs, api/Features/DocumentControl/Storage/IDocumentControlBlobStore.cs.
608. Give `OnInitialized` a home of its own, out of `jpms/Features/Triage/Panels/Actions/StageBuildingControlInspectionAction.razor`. 18 other files use it: jpms/App.razor, jpms/Components/ApprovedSessionGate.razor, jpms/Components/ApprovedUsersPanel.razor, jpms/Components/DrawingFolderPicker.razor.
609. Give `OnParametersSet` a home of its own, out of `jpms/Features/Triage/Panels/Actions/StageBuildingControlInspectionAction.razor`. 12 other files use it: jpms/Components/DrawingFolderOptions.razor, jpms/Components/DrawingsTable.razor, jpms/Components/KpiPersonPicker.razor, jpms/Components/ValuationReportTable.BulkPercent.cs.
610. Give `Set` a home of its own, out of `jpms/Features/Triage/Panels/Actions/StageBuildingControlInspectionAction.razor`. 97 other files use it: api/Auth/InviteEmailBody.cs, api/Auth/SessionCookie.cs, api/Data/Entities/ConnectEntities.cs, api/Data/Entities/CoreEntities.cs.
611. Give `NotifyCreate` a home of its own, out of `jpms/Features/Triage/Panels/Actions/StageBuildingControlInspectionAction.razor`. 1 other files use it: jpms/Features/Triage/Panels/Actions/StagedRecordActionEditor.razor.
612. Give `DisposeAsync` a home of its own, out of `jpms/Features/Triage/Workspace/PanelWorkspace.razor`. 1 other files use it: jpms/Features/Triage/RichTextEditor.razor.
613. Give `OnFileSelected` a home of its own, out of `jpms/Components/DrawingRevisionUploadForm.razor`. 1 other files use it: jpms/Components/ProjectContractPanel.razor.
614. Give `HandleUpload` a home of its own, out of `jpms/Components/DrawingRevisionUploadForm.razor`. 2 other files use it: jpms/Components/DrawingUploadForm.razor, jpms/Pages/PortalHome.razor.
615. Give `ExtractRevisionLabel` a home of its own, out of `jpms/Components/DrawingRevisionUploadForm.razor`. 1 other files use it: jpms/Components/DrawingUploadForm.razor.cs.
616. Give `OnParametersSet` a home of its own, out of `jpms/Components/Modal.razor`. 12 other files use it: jpms/Components/DrawingFolderOptions.razor, jpms/Components/DrawingsTable.razor, jpms/Components/KpiPersonPicker.razor, jpms/Components/ValuationReportTable.BulkPercent.cs.
617. Give `Href` a home of its own, out of `jpms/Components/OpenRequestsPanel.razor`. 24 other files use it: jpms/Components/AdminHome.razor, jpms/Components/DropdownMenu.razor, jpms/Components/ImageViewer.razor, jpms/Components/PdfViewer.razor.
618. Give `OnInitializedAsync` a home of its own, out of `jpms/Components/OpenRequestsPanel.razor`. 26 other files use it: jpms/App.razor, jpms/Components/ExpiringDocumentsPanel.razor, jpms/Components/PackageReconciliationSection.razor.cs, jpms/Components/ProjectCorrespondencePanel.razor.cs.
619. Give `OnParametersSetAsync` a home of its own, out of `jpms/Components/ValuationClaimCorrespondenceSection.razor`. 2 other files use it: jpms/Components/RequestConversation.razor.cs, jpms/Features/Triage/Panels/RecordLinkSection.razor.
620. Give `LoadEmailsAsync` a home of its own, out of `jpms/Components/ValuationClaimCorrespondenceSection.razor`. 2 other files use it: jpms/Pages/ProjectProgramme.razor.cs, jpms/Pages/ProjectVariationDetail.razor.
621. Give `PathFor` a home of its own, out of `jpms/Components/Icons/ActionIcon.razor`. 4 other files use it: jpms/Cqrs/CommandRouteTable.cs, jpms/Cqrs/HttpCommandSender.cs, jpms/Cqrs/HttpQueryClient.cs, jpms/Cqrs/QueryRouteTable.cs.
622. Remove the 27 components and functions nothing calls. Listed in audit.json under details.orphans and details.inventory.offenders.orphans; confirm each has no caller before it goes.

**Pass 3 — Design pattern identification**

623. Complete the pattern: every handler has a endpoint. 48 of 462 lack it. Predicted: api/Features/Subcontractors/Commands/AddComplianceDocumentVersionEndpoint.cs; api/Features/Kpi/Commands/AddKpiPersonEndpoint.cs; api/Features/Xero/Ledger/AllocateSuggestedXeroLinesEndpoint.cs; api/Features/ProjectContracts/Commands/AttachProjectContractAmendmentEndpoint.cs. Find the code doing that job now and move it there; a subject that truly has no such job goes in acceptedGaps.
624. Complete the pattern: every authorisation has a handler. 13 of 233 lack it. Predicted: api/Features/Progress/ContractorsReports/Commands/ContractorsReportHandler.cs; api/Features/Drawings/Commands/DrawingFolderCommandHandler.cs; api/Features/Clients/Commands/InviteClientPortalUserHandler.cs; api/Features/Subcontractors/Commands/InviteSubcontractorPortalUserHandler.cs. Find the code doing that job now and move it there; a subject that truly has no such job goes in acceptedGaps.
625. Complete the pattern: every authorisation has a endpoint. 18 of 233 lack it. Predicted: api/Features/ProjectContracts/Commands/AttachProjectContractAmendmentEndpoint.cs; api/Features/ProjectContracts/Commands/AttachProjectContractDocumentEndpoint.cs; api/Features/Progress/ContractorsReports/Commands/ContractorsReportEndpoint.cs; api/Features/Drawings/Commands/DrawingFolderCommandEndpoint.cs. Find the code doing that job now and move it there; a subject that truly has no such job goes in acceptedGaps.
626. Complete the pattern: every authorisation has a validation. 22 of 233 lack it. Predicted: api/Features/WeeklyCashflow/Commands/ArchiveWeeklyCashflowItemValidation.cs; api/Features/Calendar/Commands/DeleteCalendarEventValidation.cs; api/Features/Progress/Commands/DeleteProgressPhotoValidation.cs; api/Features/Progress/Commands/DeleteProgressReportValidation.cs. Find the code doing that job now and move it there; a subject that truly has no such job goes in acceptedGaps.
627. Complete the pattern: every endpoint has a handler. 48 of 462 lack it. Predicted: api/Features/Portal/Commands/AcceptMyWorkOrderHandler.cs; api/Features/Progress/WhatsApp/ApplyWhatsAppWeekHandler.cs; api/Features/Connect/ApproveAuthorizationHandler.cs; api/Features/Connect/AuthorizeHandler.cs. Find the code doing that job now and move it there; a subject that truly has no such job goes in acceptedGaps.
628. Complete the pattern: every validation has a handler. 3 of 231 lack it. Predicted: api/Features/Progress/ContractorsReports/Commands/ContractorsReportHandler.cs; api/Features/Drawings/Commands/DrawingFolderCommandHandler.cs; api/Features/Xero/Ledger/Allocation/SetXeroAllocationHandler.AllocateHandler.cs. Find the code doing that job now and move it there; a subject that truly has no such job goes in acceptedGaps.
629. Complete the pattern: every validation has a authorisation. 20 of 231 lack it. Predicted: api/Features/Commercial/Commands/AddValuationLineItemAuthorisation.cs; api/Features/Site/Commands/ApplyProgrammeDraftAuthorisation.cs; api/Features/Site/Commands/DiscardProgrammeDraftAuthorisation.cs; api/Features/Site/Commands/DraftProgrammeFromValuationAuthorisation.cs. Find the code doing that job now and move it there; a subject that truly has no such job goes in acceptedGaps.
630. Complete the pattern: every validation has a endpoint. 12 of 231 lack it. Predicted: api/Features/ProjectContracts/Commands/AttachProjectContractAmendmentEndpoint.cs; api/Features/ProjectContracts/Commands/AttachProjectContractDocumentEndpoint.cs; api/Features/Progress/ContractorsReports/Commands/ContractorsReportEndpoint.cs; api/Features/Drawings/Commands/DrawingFolderCommandEndpoint.cs. Find the code doing that job now and move it there; a subject that truly has no such job goes in acceptedGaps.
631. Complete the pattern: every queries has a commands. 1 of 5 lack it. Predicted: api/Features/Subcontractors/SubcontractorTradeCommands.cs. Find the code doing that job now and move it there; a subject that truly has no such job goes in acceptedGaps.
632. Complete the pattern: every settlement has a razor. 1 of 4 lack it. Predicted: contracts/Closeout/AgreeRazor.cs. Find the code doing that job now and move it there; a subject that truly has no such job goes in acceptedGaps.
633. Complete the pattern: every export has a razor. 1 of 13 lack it. Predicted: contracts/Commercial/Export/ValuationSnapshotRazor.cs. Find the code doing that job now and move it there; a subject that truly has no such job goes in acceptedGaps.
634. Divide `api/Data/JpmsContext.Model.cs` (557 lines) into the units its pattern names. 1 functions; the longest is 536 lines.
635. Divide `api/Features/Sales/Documents/EstimateDocumentRenderer.cs` (507 lines) into the units its pattern names. 19 functions; the longest is 73 lines.
636. Divide `worker/MailboxIntake/Graph/GraphMailClient.cs` (475 lines) into the units its pattern names. 19 functions; the longest is 59 lines.
637. Divide `jpms/Services/Navigation/SidebarFolders.cs` (454 lines) into the units its pattern names. 1 functions; the longest is 410 lines.
638. Divide `jpms/Services/HttpLabourStore.cs` (436 lines) into the units its pattern names. 46 functions; the longest is 28 lines.
639. Divide `jpms/Components/ManualWorkOrderModal.razor.cs` (428 lines) into the units its pattern names. 13 functions; the longest is 137 lines.
640. Divide `jpms/Pages/DocumentControl.Filing.cs` (409 lines) into the units its pattern names. 22 functions; the longest is 31 lines.
641. Divide `api/Features/Ai/Sources/AiFiledDocuments.cs` (394 lines) into the units its pattern names. 7 functions; the longest is 149 lines.
642. Divide `api/Features/Procurement/Commands/ExtractTenderFromMessageHandler.cs` (381 lines) into the units its pattern names. 6 functions; the longest is 129 lines.
643. Divide `api/Features/Todos/TodoBrief.cs` (379 lines) into the units its pattern names. 16 functions; the longest is 67 lines.
644. Divide `api/Features/Requests/RequestContextAssembler.cs` (375 lines) into the units its pattern names. 8 functions; the longest is 76 lines.
645. Divide `api/Features/Xero/XeroClient.Http.cs` (375 lines) into the units its pattern names. 12 functions; the longest is 49 lines.
646. Divide `api/Features/Xero/XeroClient.Reads.cs` (372 lines) into the units its pattern names. 9 functions; the longest is 47 lines.
647. Divide `api/Features/Ai/Tools/AiLabourMonthEndTools.cs` (371 lines) into the units its pattern names. 1 functions; the longest is 348 lines.
648. Divide `api/Features/MailboxIntake/Graph/MailboxGraphClient.Drafts.cs` (370 lines) into the units its pattern names. 11 functions; the longest is 90 lines.
649. Divide `jpms/Pages/TriageQueue.StagedCreate.cs` (369 lines) into the units its pattern names. 2 functions; the longest is 191 lines.
650. Divide `jpms/Pages/ProjectCommunications.razor.cs` (368 lines) into the units its pattern names. 18 functions; the longest is 39 lines.
651. Divide `api/Data/Entities/ProcurementEntities.cs` (360 lines) into the units its pattern names. 0 functions; the longest is 0 lines.
652. Divide `api/Features/Ai/Tools/AiWriteTools.cs` (358 lines) into the units its pattern names. 2 functions; the longest is 313 lines.
653. Divide `jpms/Pages/TriageQueue.Compose.cs` (356 lines) into the units its pattern names. 11 functions; the longest is 28 lines.
654. Divide `api/Features/Sales/SalesEndpoints.cs` (352 lines) into the units its pattern names. 17 functions; the longest is 32 lines.
655. Divide `api/Features/Ai/Tools/AiMailboxTools.cs` (351 lines) into the units its pattern names. 4 functions; the longest is 268 lines.
656. Divide `api/Features/Ai/Tools/AiToolCatalogue.Lookup.cs` (351 lines) into the units its pattern names. 1 functions; the longest is 338 lines.
657. Divide `jpms/Features/Triage/TriageStaging.cs` (350 lines) into the units its pattern names. 1 functions; the longest is 6 lines.
658. Divide `api/Features/Commercial/Documents/ValuationReportSnapshotRenderer.Sections.cs` (348 lines) into the units its pattern names. 13 functions; the longest is 54 lines.
659. Divide `jpms/Components/DrawingUploadForm.razor.cs` (346 lines) into the units its pattern names. 10 functions; the longest is 80 lines.
660. Divide `api/Features/MailboxIntake/Graph/IntakeMessageReader.cs` (345 lines) into the units its pattern names. 7 functions; the longest is 110 lines.
661. Divide `api/Features/Xero/Ledger/XeroWriteBackService.cs` (342 lines) into the units its pattern names. 8 functions; the longest is 117 lines.
662. Divide `api/Features/ArchitectInstructions/ArchitectInstructionHandlers.cs` (340 lines) into the units its pattern names. 11 functions; the longest is 52 lines.
663. Divide `contracts/Xero/XeroLedger.cs` (337 lines) into the units its pattern names. 1 functions; the longest is 54 lines.
664. Divide `jpms/Components/FinancialsTable.razor.cs` (337 lines) into the units its pattern names. 11 functions; the longest is 103 lines.
665. Divide `api/Features/Ai/Tools/AiValuationInvoiceTools.cs` (331 lines) into the units its pattern names. 1 functions; the longest is 306 lines.
666. Divide `api/Features/Commercial/Queries/GetProjectFinancialSummaryHandler.cs` (331 lines) into the units its pattern names. 1 functions; the longest is 319 lines.
667. Divide `jpms/Pages/ProjectWorkOrderAllocation.razor.cs` (331 lines) into the units its pattern names. 14 functions; the longest is 56 lines.
668. Divide `api/Features/Ai/Tools/Actions/SubcontractorsAndLeadsActions.Subcontractors.cs` (325 lines) into the units its pattern names. 1 functions; the longest is 306 lines.
669. Divide `jpms/Features/Triage/MailReplyComposer.razor.cs` (323 lines) into the units its pattern names. 11 functions; the longest is 48 lines.
670. Divide `jpms/Pages/TriageQueue.Parking.cs` (322 lines) into the units its pattern names. 9 functions; the longest is 39 lines.
671. Divide `api/Data/Entities/CommercialEntities.cs` (319 lines) into the units its pattern names. 0 functions; the longest is 0 lines.
672. Divide `api/Features/Labour/Commands/WorkerLinkSlices.cs` (319 lines) into the units its pattern names. 19 functions; the longest is 57 lines.
673. Divide `api/Features/Subcontractors/Documents/SubcontractorStatementRenderer.cs` (319 lines) into the units its pattern names. 13 functions; the longest is 110 lines.
674. Divide `contracts/Ai/PageGuides/OfficePageGuides.cs` (314 lines) into the units its pattern names. 0 functions; the longest is 0 lines.
675. Divide `api/Features/Ai/Tools/AiRegisterTools.cs` (312 lines) into the units its pattern names. 1 functions; the longest is 275 lines.
676. Divide `api/Features/ArchitectInstructions/ArchitectInstructionEndpoints.cs` (312 lines) into the units its pattern names. 10 functions; the longest is 25 lines.
677. Divide `api/Features/Requests/Documents/RequestDocumentRenderer.Sections.cs` (312 lines) into the units its pattern names. 11 functions; the longest is 55 lines.
678. Divide `jpms/Pages/SubcontractorDetail.razor.cs` (311 lines) into the units its pattern names. 19 functions; the longest is 19 lines.
679. Divide `jpms/Pages/Workers.razor.cs` (310 lines) into the units its pattern names. 15 functions; the longest is 36 lines.
680. Divide `api/Features/MailboxIntake/Graph/MailboxGraphClient.Tagging.cs` (309 lines) into the units its pattern names. 10 functions; the longest is 60 lines.
681. Divide `api/Features/Labour/Commands/WorkerWeekApprovalByNameSlices.cs` (307 lines) into the units its pattern names. 14 functions; the longest is 50 lines.
682. Divide `api/Features/Documents/JewelDocumentStyle.cs` (306 lines) into the units its pattern names. 13 functions; the longest is 52 lines.
683. Divide `api/Features/Ai/Tools/AiRecordTools.Correspondence.cs` (305 lines) into the units its pattern names. 1 functions; the longest is 293 lines.
684. Divide `api/Features/Ai/Tools/AiToolCatalogue.TodoBrief.cs` (303 lines) into the units its pattern names. 5 functions; the longest is 203 lines.
685. Divide `jpms/Pages/ProjectRequestDetail.Menus.cs` (302 lines) into the units its pattern names. 12 functions; the longest is 32 lines.
686. Divide `api/Features/Progress/Documents/ProgressReportRenderer.cs` (301 lines) into the units its pattern names. 15 functions; the longest is 42 lines.
687. Divide `jpms/Pages/ProjectValuation.razor.cs` (300 lines) into the units its pattern names. 9 functions; the longest is 106 lines.
688. Divide `contracts/WeeklyCashflow/WeeklyCashflowMaths.cs` (299 lines) into the units its pattern names. 8 functions; the longest is 82 lines.
689. Divide `jpms/Components/RoleHome.razor.cs` (299 lines) into the units its pattern names. 7 functions; the longest is 23 lines.
690. Divide `jpms/Features/Procurement/ProcurementRouteRegistration.cs` (299 lines) into the units its pattern names. 2 functions; the longest is 286 lines.
691. Divide `api/Features/Xero/XeroClient.SitePnl.cs` (298 lines) into the units its pattern names. 9 functions; the longest is 71 lines.
692. Divide `jpms/Pages/CostCodes.razor.cs` (298 lines) into the units its pattern names. 17 functions; the longest is 44 lines.
693. Divide `api/Features/Ai/Tools/Actions/VariationsAndValuationsActions.Variations.cs` (297 lines) into the units its pattern names. 1 functions; the longest is 278 lines.
694. Divide `api/Features/Ai/Tools/Actions/VariationsAndValuationsActions.ValuationInvoices.cs` (296 lines) into the units its pattern names. 1 functions; the longest is 275 lines.
695. Divide `contracts/Commercial/CashForecastPhasing.cs` (296 lines) into the units its pattern names. 7 functions; the longest is 52 lines.
696. Divide `api/Features/Ai/Tools/AiSalesTools.cs` (294 lines) into the units its pattern names. 5 functions; the longest is 186 lines.
697. Divide `api/Features/Mcp/McpEndpoint.cs` (294 lines) into the units its pattern names. 10 functions; the longest is 65 lines.
698. Divide `jpms/Pages/TriageQueue.razor.cs` (294 lines) into the units its pattern names. 2 functions; the longest is 27 lines.
699. Divide `jpms/Pages/ProjectRequestDetail.DraftVariation.cs` (292 lines) into the units its pattern names. 10 functions; the longest is 68 lines.
700. Divide `api/Features/Sales/Commands/LeadHandlers.cs` (291 lines) into the units its pattern names. 9 functions; the longest is 55 lines.
701. Divide `api/Features/Ai/Tools/Actions/LabourAndBackOfficeActions.MonthEnd.cs` (285 lines) into the units its pattern names. 1 functions; the longest is 271 lines.
702. Divide `contracts/Ai/PageGuides/SitePageGuides.cs` (282 lines) into the units its pattern names. 0 functions; the longest is 0 lines.
703. Divide `api/Features/Procurement/Commands/SuggestBidPackagesHandler.cs` (279 lines) into the units its pattern names. 5 functions; the longest is 72 lines.
704. Divide `jpms/Components/ProjectTodoList.razor.cs` (276 lines) into the units its pattern names. 17 functions; the longest is 20 lines.
705. Divide `api/Features/Requests/Commands/PrepareRequestEmailDraftHandler.cs` (275 lines) into the units its pattern names. 5 functions; the longest is 134 lines.
706. Divide `jpms/Pages/TriageQueue.ProjectMatch.cs` (275 lines) into the units its pattern names. 12 functions; the longest is 48 lines.
707. Divide `jpms/Pages/ProjectBuildingControl.razor.cs` (273 lines) into the units its pattern names. 16 functions; the longest is 22 lines.
708. Divide `api/Features/RecordLinks/BackfillBucketsEndpoint.cs` (272 lines) into the units its pattern names. 6 functions; the longest is 155 lines.
709. Divide `api/Features/Ai/Tools/Actions/ProcurementActions.WorkOrders.cs` (271 lines) into the units its pattern names. 1 functions; the longest is 263 lines.
710. Divide `api/Features/Procurement/PricingScheduleWorkbook.cs` (271 lines) into the units its pattern names. 5 functions; the longest is 175 lines.
711. Divide `jpms/Components/ReconciliationPackageBuilderModal.razor.cs` (271 lines) into the units its pattern names. 9 functions; the longest is 41 lines.
712. Divide `jpms/Features/Triage/Panels/Actions/StagedRecordActionEditor.razor.cs` (271 lines) into the units its pattern names. 12 functions; the longest is 29 lines.
713. Divide `jpms/Pages/AiSkillsAdmin.razor.cs` (271 lines) into the units its pattern names. 12 functions; the longest is 39 lines.
714. Divide `jpms/Pages/ProjectCalendar.razor.cs` (270 lines) into the units its pattern names. 18 functions; the longest is 31 lines.
715. Divide `jpms/Pages/Subcontractors.razor.cs` (267 lines) into the units its pattern names. 13 functions; the longest is 21 lines.
716. Divide `jpms/Pages/XeroAllocation.Export.cs` (266 lines) into the units its pattern names. 11 functions; the longest is 117 lines.
717. Divide `api/Features/Xero/Ledger/SyncXeroLedgerHandler.cs` (264 lines) into the units its pattern names. 4 functions; the longest is 198 lines.
718. Divide `jpms/Features/Triage/AttachmentPicker.Drawings.cs` (264 lines) into the units its pattern names. 16 functions; the longest is 55 lines.
719. Divide `api/Data/JpmsContext.cs` (263 lines) into the units its pattern names. 2 functions; the longest is 4 lines.
720. Divide `jpms/Pages/XeroAllocation.SendTo.cs` (263 lines) into the units its pattern names. 14 functions; the longest is 34 lines.
721. Divide `api/Features/Procurement/ProcurementFeatureRegistration.cs` (261 lines) into the units its pattern names. 4 functions; the longest is 203 lines.
722. Divide `jpms/Pages/ProjectDrawings.razor.cs` (261 lines) into the units its pattern names. 17 functions; the longest is 34 lines.
723. Divide `jpms/Pages/TriageQueue.Attachments.cs` (261 lines) into the units its pattern names. 10 functions; the longest is 34 lines.
724. Divide `api/Features/ValuationInvoices/XeroPayments/ValuationInvoicePaymentSyncPlanner.cs` (259 lines) into the units its pattern names. 12 functions; the longest is 36 lines.
725. Divide `api/Features/Xero/XeroClient.ReadsSuppliers.cs` (259 lines) into the units its pattern names. 9 functions; the longest is 61 lines.
726. Divide `jpms/Pages/XeroTransactions.razor.cs` (259 lines) into the units its pattern names. 9 functions; the longest is 69 lines.
727. Divide `api/Features/Labour/Queries/GetLabourOverviewSlice.cs` (254 lines) into the units its pattern names. 3 functions; the longest is 217 lines.
728. Divide `api/Features/Ai/Tools/Actions/CommercialActions.ClaimsAndValuations.cs` (253 lines) into the units its pattern names. 1 functions; the longest is 239 lines.
729. Divide `api/Features/MailboxIntake/Graph/IMailboxGraphClient.cs` (253 lines) into the units its pattern names. 1 functions; the longest is 15 lines.
730. Divide `api/Features/Ai/Tools/AiFinanceTools.cs` (252 lines) into the units its pattern names. 1 functions; the longest is 222 lines.
731. Divide `api/Features/Variations/Commands/ApproveVariationOrderHandler.cs` (252 lines) into the units its pattern names. 3 functions; the longest is 182 lines.
732. Divide `jpms/Pages/ProjectCashflow.razor.cs` (249 lines) into the units its pattern names. 2 functions; the longest is 41 lines.
733. Divide `api/Features/Sales/Imagine/ImagineRenderRunner.cs` (248 lines) into the units its pattern names. 5 functions; the longest is 153 lines.
734. Divide `contracts/Ai/ModalCatalog.Variations.cs` (248 lines) into the units its pattern names. 0 functions; the longest is 0 lines.
735. Divide `api/Features/Xero/Ledger/WorkOrderBillRecognition.Rules.cs` (247 lines) into the units its pattern names. 10 functions; the longest is 31 lines.
736. Divide `api/Features/Ai/Tools/Actions/SiteAndProgressActions.ProgressProgramme.cs` (246 lines) into the units its pattern names. 1 functions; the longest is 227 lines.
737. Divide `jpms/Pages/ProfitSummary.Running.cs` (246 lines) into the units its pattern names. 5 functions; the longest is 108 lines.
738. Divide `api/Features/Ai/Tools/Actions/SalesActions.cs` (245 lines) into the units its pattern names. 1 functions; the longest is 216 lines.
739. Divide `api/Features/Variations/Commands/ReviseVariationOrderLinesHandler.cs` (245 lines) into the units its pattern names. 3 functions; the longest is 186 lines.
740. Divide `contracts/Ai/ModalCatalog.Procurement.cs` (244 lines) into the units its pattern names. 0 functions; the longest is 0 lines.
741. Divide `jpms/Services/HttpSubcontractorStore.cs` (244 lines) into the units its pattern names. 20 functions; the longest is 28 lines.
742. Divide `api/Features/Xero/Ledger/XeroLedgerEndpoints.cs` (243 lines) into the units its pattern names. 15 functions; the longest is 25 lines.
743. Divide `api/Features/Commercial/Commands/SaveReconciliationPackageHandler.cs` (242 lines) into the units its pattern names. 1 functions; the longest is 220 lines.
744. Divide `api/Features/Labour/Commands/MonthEndByNameSlices.cs` (241 lines) into the units its pattern names. 21 functions; the longest is 21 lines.
745. Divide `api/Features/Procurement/Commands/BidPackageInviteMailAssembler.cs` (241 lines) into the units its pattern names. 6 functions; the longest is 49 lines.
746. Divide `api/Features/Requests/Documents/DocumentBranding.cs` (241 lines) into the units its pattern names. 1 functions; the longest is 8 lines.
747. Divide `jpms/Pages/ProjectRequestDetail.razor.cs` (241 lines) into the units its pattern names. 10 functions; the longest is 26 lines.
748. Divide `api/Features/Labour/Commands/SettlementLineAndMappingSlices.cs` (240 lines) into the units its pattern names. 15 functions; the longest is 24 lines.
749. Divide `api/Features/Requests/MailboxTriageEndpoints.cs` (237 lines) into the units its pattern names. 15 functions; the longest is 31 lines.
750. Divide `jpms/Pages/ProjectDrawingDetail.razor.cs` (237 lines) into the units its pattern names. 9 functions; the longest is 30 lines.
751. Divide `jpms/Pages/ProjectValuation.Export.cs` (237 lines) into the units its pattern names. 10 functions; the longest is 77 lines.
752. Divide `jpms/Services/Navigation/DesktopNavigation.cs` (237 lines) into the units its pattern names. 5 functions; the longest is 31 lines.
753. Divide `api/Features/Places/LocalBusinessSearch.cs` (236 lines) into the units its pattern names. 7 functions; the longest is 61 lines.
754. Divide `api/Features/Sales/Commands/SalesGates.cs` (236 lines) into the units its pattern names. 14 functions; the longest is 18 lines.
755. Divide `jpms/Program.cs` (236 lines) into the units its pattern names. 0 functions; the longest is 0 lines.
756. Divide `api/Features/Xero/IXeroClient.cs` (235 lines) into the units its pattern names. 0 functions; the longest is 0 lines.
757. Divide `jpms/Components/StatusTones.cs` (235 lines) into the units its pattern names. 25 functions; the longest is 9 lines.
758. Divide `jpms/Pages/ProjectRequests.TabsAndSearch.cs` (234 lines) into the units its pattern names. 10 functions; the longest is 35 lines.
759. Divide `api/Features/Sales/Commands/ProposalHandlers.cs` (233 lines) into the units its pattern names. 7 functions; the longest is 53 lines.
760. Divide `api/Features/Xero/XeroApproval.cs` (233 lines) into the units its pattern names. 5 functions; the longest is 97 lines.
761. Divide `jpms/Pages/XeroAllocation.RowCoding.cs` (232 lines) into the units its pattern names. 9 functions; the longest is 27 lines.
762. Divide `contracts/Models/HsAuditTemplate.cs` (231 lines) into the units its pattern names. 1 functions; the longest is 3 lines.
763. Divide `jpms/Features/Commercial/CommercialRouteRegistration.cs` (231 lines) into the units its pattern names. 3 functions; the longest is 208 lines.
764. Divide `jpms/Features/Site/Programme/ProgrammeWorkbench.razor.cs` (231 lines) into the units its pattern names. 10 functions; the longest is 25 lines.
765. Divide `jpms/Components/RequestForm.razor.cs` (230 lines) into the units its pattern names. 12 functions; the longest is 48 lines.
766. Divide `api/Features/Commercial/ValuationReportSnapshotCapture.cs` (229 lines) into the units its pattern names. 3 functions; the longest is 24 lines.
767. Divide `jpms/Services/HttpDrawingStore.cs` (229 lines) into the units its pattern names. 20 functions; the longest is 26 lines.
768. Divide `jpms/Layout/SideNav.razor.cs` (228 lines) into the units its pattern names. 11 functions; the longest is 32 lines.
769. Divide `api/Features/Subcontractors/Commands/ConsolidateDirectoryRecordsHandler.cs` (227 lines) into the units its pattern names. 4 functions; the longest is 82 lines.
770. Divide `contracts/Models/Procurement.cs` (227 lines) into the units its pattern names. 4 functions; the longest is 66 lines.
771. Divide `jpms/Pages/AuditTrail.razor.cs` (227 lines) into the units its pattern names. 10 functions; the longest is 41 lines.
772. Divide `jpms/Pages/ProjectFinancials.razor.cs` (227 lines) into the units its pattern names. 9 functions; the longest is 27 lines.
773. Divide `jpms/Pages/XeroAllocation.razor.cs` (227 lines) into the units its pattern names. 5 functions; the longest is 15 lines.
774. Divide `jpms/Features/Requests/RequestsRouteRegistration.cs` (224 lines) into the units its pattern names. 2 functions; the longest is 211 lines.
775. Divide `worker/MailboxIntake/Actions/MailboxActionWorker.cs` (223 lines) into the units its pattern names. 4 functions; the longest is 135 lines.
776. Divide `api/Features/Todos/Commands/CreateTodoItemsFromMessageHandler.cs` (222 lines) into the units its pattern names. 3 functions; the longest is 187 lines.
777. Divide `api/Features/Ai/Tools/Actions/LabourAndBackOfficeActions.Timesheets.cs` (221 lines) into the units its pattern names. 1 functions; the longest is 212 lines.
778. Divide `api/Features/Todos/TodoCompletionRecordTagger.cs` (221 lines) into the units its pattern names. 5 functions; the longest is 70 lines.
779. Divide `jpms/Pages/ProfitSummary.ExportGrid.cs` (221 lines) into the units its pattern names. 6 functions; the longest is 61 lines.
780. Divide `jpms/Pages/ProjectBuildingControlInspection.razor.cs` (221 lines) into the units its pattern names. 14 functions; the longest is 26 lines.
781. Divide `api/Features/Requests/Attachments/RequestAttachmentEndpoints.cs` (220 lines) into the units its pattern names. 6 functions; the longest is 17 lines.
782. Divide `jpms/Services/AuthService.cs` (220 lines) into the units its pattern names. 12 functions; the longest is 22 lines.
783. Divide `api/Features/Ai/Tools/AiCommercialTools.Variation.cs` (219 lines) into the units its pattern names. 2 functions; the longest is 204 lines.
784. Divide `contracts/Documents/Excel/ExcelStyleRegistry.cs` (219 lines) into the units its pattern names. 9 functions; the longest is 79 lines.
785. Divide `contracts/Models/BuildingControl.cs` (219 lines) into the units its pattern names. 5 functions; the longest is 66 lines.
786. Divide `jpms/Services/HttpValuationReportStore.cs` (218 lines) into the units its pattern names. 24 functions; the longest is 19 lines.
787. Divide `api/Features/Ai/Tools/Actions/CommercialActions.Financials.cs` (217 lines) into the units its pattern names. 1 functions; the longest is 203 lines.
788. Divide `api/Features/Requests/Commands/MailboxRequestCommandHandlers.cs` (217 lines) into the units its pattern names. 4 functions; the longest is 163 lines.
789. Divide `contracts/Models/Subcontractor.cs` (217 lines) into the units its pattern names. 6 functions; the longest is 61 lines.
790. Divide `jpms/Pages/XeroAllocation.Settlement.cs` (217 lines) into the units its pattern names. 9 functions; the longest is 28 lines.
791. Divide `api/Features/Drawings/Geometry/DrawingTitleBlockReader.cs` (216 lines) into the units its pattern names. 8 functions; the longest is 34 lines.
792. Divide `contracts/Models/Request.cs` (216 lines) into the units its pattern names. 5 functions; the longest is 70 lines.
793. Divide `api/Features/Ai/Tools/Actions/SiteAndProgressActions.Drawings.cs` (214 lines) into the units its pattern names. 1 functions; the longest is 190 lines.
794. Divide `api/Features/Sales/Imagine/ImagineConceptWriter.cs` (214 lines) into the units its pattern names. 6 functions; the longest is 60 lines.
795. Divide `jpms/Pages/ProjectVariationDetail.Communications.cs` (214 lines) into the units its pattern names. 9 functions; the longest is 24 lines.
796. Divide `api/Features/BuildingControl/Attachments/BuildingControlAttachmentEndpoints.cs` (213 lines) into the units its pattern names. 9 functions; the longest is 47 lines.
797. Divide `jpms/Components/RequestConversation.razor.cs` (212 lines) into the units its pattern names. 11 functions; the longest is 35 lines.
798. Divide `jpms/Pages/ProjectBidPackageInvites.razor.cs` (212 lines) into the units its pattern names. 10 functions; the longest is 37 lines.
799. Divide `contracts/Models/ValuationReport.cs` (211 lines) into the units its pattern names. 5 functions; the longest is 40 lines.
800. Divide `jpms/Components/CostCentreCostOfSalesModal.razor.cs` (211 lines) into the units its pattern names. 8 functions; the longest is 32 lines.
801. Divide `jpms/Components/CostCentreSalesLinesModal.razor.cs` (211 lines) into the units its pattern names. 5 functions; the longest is 43 lines.
802. Divide `jpms/Pages/CashForecast.razor.cs` (211 lines) into the units its pattern names. 6 functions; the longest is 38 lines.
803. Divide `api/Features/Ai/Tools/AiCommercialTools.Valuation.cs` (209 lines) into the units its pattern names. 2 functions; the longest is 189 lines.
804. Divide `api/Features/Xero/SitePnl/SyncXeroSitePnlHandler.cs` (209 lines) into the units its pattern names. 2 functions; the longest is 156 lines.
805. Divide `api/Features/MailboxIntake/Graph/MailboxGraphClient.Conversations.cs` (206 lines) into the units its pattern names. 6 functions; the longest is 56 lines.
806. Divide `jpms/Pages/ProjectVariationDetail.Status.cs` (206 lines) into the units its pattern names. 13 functions; the longest is 47 lines.
807. Divide `jpms/Pages/ProjectValuation.Invoices.cs` (205 lines) into the units its pattern names. 15 functions; the longest is 22 lines.
808. Divide `jpms/Pages/TriageQueue.TaggedSearch.cs` (205 lines) into the units its pattern names. 12 functions; the longest is 52 lines.
809. Divide `api/Features/Ai/Tools/Actions/ProjectsAndTendersActions.BuildingControl.cs` (204 lines) into the units its pattern names. 1 functions; the longest is 183 lines.
810. Divide `api/Features/Ai/Tools/AiActionGatewayTools.cs` (204 lines) into the units its pattern names. 2 functions; the longest is 159 lines.
811. Divide `jpms/Pages/ProfitSummary.Figures.cs` (204 lines) into the units its pattern names. 5 functions; the longest is 40 lines.
812. Divide `contracts/Models/ProgrammeCostCentreRules.cs` (203 lines) into the units its pattern names. 3 functions; the longest is 30 lines.
813. Divide `jpms/Features/Site/Programme/ProgrammeDraftReview.razor.cs` (203 lines) into the units its pattern names. 10 functions; the longest is 28 lines.
814. Divide `api/Features/Ai/Tools/AiToolCatalogue.Masters.cs` (202 lines) into the units its pattern names. 1 functions; the longest is 188 lines.
815. Divide `jpms/Pages/TodoDetail.razor.cs` (201 lines) into the units its pattern names. 11 functions; the longest is 16 lines.
816. Divide `api/Features/Xero/XeroClient.LineItems.cs` (200 lines) into the units its pattern names. 4 functions; the longest is 83 lines.
817. Divide `jpms/Pages/ProjectArchitectInstructions.razor.cs` (200 lines) into the units its pattern names. 10 functions; the longest is 41 lines.
818. Divide `api/Features/Ai/AiAttachmentReader.cs` (199 lines) into the units its pattern names. 8 functions; the longest is 25 lines.
819. Divide `api/Features/Labour/LabourFeatureRegistration.cs` (199 lines) into the units its pattern names. 1 functions; the longest is 189 lines.
820. Divide `api/Features/Ai/ClaudeClient.cs` (198 lines) into the units its pattern names. 4 functions; the longest is 66 lines.
821. Divide `api/Features/Ai/Tools/AiRecordTools.Directory.cs` (198 lines) into the units its pattern names. 1 functions; the longest is 188 lines.
822. Divide `api/Features/Ai/Tools/AiToolCatalogue.SiteWork.cs` (197 lines) into the units its pattern names. 1 functions; the longest is 182 lines.
823. Divide `jpms/Features/Triage/Panels/PathwayActionsSection.razor.cs` (197 lines) into the units its pattern names. 8 functions; the longest is 20 lines.
824. Divide `worker/Program.cs` (197 lines) into the units its pattern names. 0 functions; the longest is 0 lines.
825. Divide `jpms/Features/Triage/AttachmentPicker.razor.cs` (196 lines) into the units its pattern names. 9 functions; the longest is 19 lines.
826. Divide `contracts/Models/SalesStrategy.cs` (195 lines) into the units its pattern names. 8 functions; the longest is 44 lines.
827. Divide `api/Features/Procurement/Commands/CreateWorkOrderFromMessageHandler.cs` (194 lines) into the units its pattern names. 2 functions; the longest is 133 lines.
828. Divide `jpms/Pages/ProfitSummary.razor.cs` (194 lines) into the units its pattern names. 7 functions; the longest is 31 lines.
829. Divide `api/Features/Projects/Commands/DeleteProjectHandler.cs` (193 lines) into the units its pattern names. 3 functions; the longest is 134 lines.
830. Divide `api/Features/Requests/Queries/MailboxLiveQueryHandlers.cs` (193 lines) into the units its pattern names. 11 functions; the longest is 25 lines.
831. Divide `jpms/Pages/ProjectLabour.Approval.cs` (193 lines) into the units its pattern names. 12 functions; the longest is 41 lines.
832. Divide `api/Features/Ai/Tools/AiValuationInvoiceTools.XeroSalesInvoices.cs` (192 lines) into the units its pattern names. 1 functions; the longest is 173 lines.
833. Divide `api/Features/Labour/Commands/WorkerPlanningSlices.cs` (192 lines) into the units its pattern names. 13 functions; the longest is 27 lines.
834. Divide `jpms/Features/Procurement/TenderInviteComposerModal.razor.cs` (192 lines) into the units its pattern names. 6 functions; the longest is 34 lines.
835. Divide `jpms/Features/Triage/Panels/PathwayPaneConfig.cs` (192 lines) into the units its pattern names. 1 functions; the longest is 182 lines.
836. Divide `jpms/Services/SessionService.cs` (192 lines) into the units its pattern names. 10 functions; the longest is 36 lines.
837. Divide `api/Features/Procurement/Queries/ResolveBidPackageTradeHandler.cs` (188 lines) into the units its pattern names. 5 functions; the longest is 60 lines.
838. Divide `jpms/Pages/PortalHome.razor.cs` (188 lines) into the units its pattern names. 7 functions; the longest is 28 lines.
839. Divide `jpms/Pages/ProjectDefectDetail.razor.cs` (188 lines) into the units its pattern names. 12 functions; the longest is 15 lines.
840. Divide `api/Features/Bluebeam/Extraction/DrawingExtractionRunner.cs` (186 lines) into the units its pattern names. 8 functions; the longest is 31 lines.
841. Divide `contracts/Commercial/Export/ValuationExportPendingSheet.cs` (186 lines) into the units its pattern names. 8 functions; the longest is 50 lines.
842. Divide `jpms/Components/ProjectContractPanel.razor.cs` (186 lines) into the units its pattern names. 10 functions; the longest is 27 lines.
843. Divide `api/Features/Ai/Tools/Actions/RequestsActions.Requests.cs` (185 lines) into the units its pattern names. 1 functions; the longest is 176 lines.
844. Divide `jpms/Pages/ProjectBidPackageInviteDetail.Invites.cs` (185 lines) into the units its pattern names. 6 functions; the longest is 56 lines.
845. Divide `api/Features/Ai/Tools/Actions/AiActionSchema.cs` (184 lines) into the units its pattern names. 7 functions; the longest is 45 lines.
846. Divide `api/Features/Ai/Tools/Actions/WeeklyCashflowAndInventoryActions.cs` (183 lines) into the units its pattern names. 1 functions; the longest is 158 lines.
847. Divide `api/Features/Labour/Commands/ChaseDismissalSlices.cs` (183 lines) into the units its pattern names. 13 functions; the longest is 32 lines.
848. Divide `api/Features/Places/WebsiteContactFinder.cs` (183 lines) into the units its pattern names. 8 functions; the longest is 23 lines.
849. Divide `api/Features/Procurement/Attachments/BidPackageAttachmentEndpoints.cs` (183 lines) into the units its pattern names. 5 functions; the longest is 13 lines.
850. Divide `contracts/Models/LabourSchedule.cs` (183 lines) into the units its pattern names. 2 functions; the longest is 135 lines.
851. Divide `jpms/Components/PackageReconciliationSection.razor.cs` (183 lines) into the units its pattern names. 10 functions; the longest is 29 lines.
852. Divide `worker/Xero/XeroNightlyWorker.cs` (183 lines) into the units its pattern names. 3 functions; the longest is 58 lines.
853. Divide `jpms/Services/HttpProgressStore.cs` (182 lines) into the units its pattern names. 15 functions; the longest is 19 lines.
854. Divide `api/Features/Ai/Tools/AiToolCatalogue.Records.cs` (181 lines) into the units its pattern names. 1 functions; the longest is 168 lines.
855. Divide `api/Features/Procurement/Attachments/WorkOrderAttachmentEndpoints.cs` (181 lines) into the units its pattern names. 5 functions; the longest is 13 lines.
856. Divide `contracts/Models/AuditEvent.cs` (181 lines) into the units its pattern names. 0 functions; the longest is 0 lines.
857. Divide `jpms/Pages/TriageQueue.ApplyFiling.cs` (181 lines) into the units its pattern names. 8 functions; the longest is 32 lines.
858. Divide `api/Data/Entities/SalesEntities.cs` (180 lines) into the units its pattern names. 0 functions; the longest is 0 lines.
859. Divide `jpms/Components/ProjectCorrespondencePanel.razor.cs` (180 lines) into the units its pattern names. 9 functions; the longest is 35 lines.
860. Divide `jpms/Pages/ProjectHs.razor.cs` (180 lines) into the units its pattern names. 11 functions; the longest is 19 lines.
861. Divide `api/Features/Ai/Sources/AiSourceDocument.cs` (179 lines) into the units its pattern names. 5 functions; the longest is 52 lines.
862. Divide `api/Features/Sales/Research/StrategyResearcher.cs` (179 lines) into the units its pattern names. 1 functions; the longest is 171 lines.
863. Divide `contracts/Ai/PageGuides/FinancePageGuides.cs` (179 lines) into the units its pattern names. 0 functions; the longest is 0 lines.
864. Divide `jpms/Pages/Todos.razor.cs` (179 lines) into the units its pattern names. 7 functions; the longest is 20 lines.
865. Divide `jpms/Pages/ProjectHsAudit.razor.cs` (178 lines) into the units its pattern names. 14 functions; the longest is 17 lines.
866. Divide `api/Features/MailboxIntake/Graph/TriageCategories.cs` (177 lines) into the units its pattern names. 4 functions; the longest is 47 lines.
867. Divide `api/Program.cs` (177 lines) into the units its pattern names. 1 functions; the longest is 14 lines.
868. Divide `api/Features/Ai/Tools/Actions/ProjectsAndTendersActions.Projects.cs` (175 lines) into the units its pattern names. 1 functions; the longest is 154 lines.
869. Divide `api/Features/Variations/Commands/ReturnVariationOrderToQuotingHandler.cs` (175 lines) into the units its pattern names. 2 functions; the longest is 114 lines.
870. Divide `jpms/Services/HttpVariationStore.cs` (175 lines) into the units its pattern names. 19 functions; the longest is 16 lines.
871. Divide `api/Features/Ai/Tools/AiToolCatalogue.Procurement.cs` (173 lines) into the units its pattern names. 1 functions; the longest is 160 lines.
872. Divide `api/Features/Requests/Documents/DocumentFontResolver.cs` (173 lines) into the units its pattern names. 6 functions; the longest is 41 lines.
873. Divide `jpms/Cqrs/HttpQueryClient.cs` (173 lines) into the units its pattern names. 2 functions; the longest is 11 lines.
874. Divide `jpms/Services/HttpRequestRegister.cs` (172 lines) into the units its pattern names. 17 functions; the longest is 24 lines.
875. Divide `api/Features/Requests/Attachments/RequestAttachmentHandlers.cs` (171 lines) into the units its pattern names. 6 functions; the longest is 65 lines.
876. Divide `api/Features/Sales/SalesImagineEndpoints.cs` (171 lines) into the units its pattern names. 7 functions; the longest is 28 lines.
877. Divide `jpms/Pages/CashForecast.Forecast.cs` (170 lines) into the units its pattern names. 5 functions; the longest is 66 lines.
878. Divide `api/Features/Ai/Tools/AiToolCatalogue.Context.cs` (169 lines) into the units its pattern names. 1 functions; the longest is 156 lines.
879. Divide `api/Features/RecordLinks/Queries/ListProjectCommunicationsHandler.cs` (169 lines) into the units its pattern names. 5 functions; the longest is 71 lines.
880. Divide `jpms/Components/ValuationReportTable.VariationEdits.cs` (169 lines) into the units its pattern names. 6 functions; the longest is 32 lines.
881. Divide `api/Features/MailboxIntake/Sharing/AzureBlobEmailFileShareStore.cs` (168 lines) into the units its pattern names. 6 functions; the longest is 45 lines.
882. Divide `contracts/Models/WeeklyCashflow.cs` (168 lines) into the units its pattern names. 4 functions; the longest is 16 lines.
883. Divide `contracts/Xero/GetXeroAgedReceivables.cs` (168 lines) into the units its pattern names. 6 functions; the longest is 20 lines.
884. Divide `jpms/Components/ValuationReportTable.razor.cs` (168 lines) into the units its pattern names. 9 functions; the longest is 30 lines.
885. Divide `jpms/Pages/SubcontractorCommunications.razor.cs` (168 lines) into the units its pattern names. 8 functions; the longest is 24 lines.
886. Divide `jpms/Pages/TriageQueue.ListReads.cs` (168 lines) into the units its pattern names. 7 functions; the longest is 40 lines.
887. Divide `api/Features/Xero/Ledger/XeroAllocationSuggester.cs` (167 lines) into the units its pattern names. 7 functions; the longest is 22 lines.
888. Divide `contracts/Xero/GetXeroAgedPayables.cs` (167 lines) into the units its pattern names. 6 functions; the longest is 20 lines.
889. Divide `jpms/Components/WorkOrderForm.Assistant.cs` (167 lines) into the units its pattern names. 7 functions; the longest is 23 lines.
890. Divide `jpms/Pages/AiActionsAdmin.razor.cs` (167 lines) into the units its pattern names. 9 functions; the longest is 34 lines.
891. Divide `api/Features/Drawings/Commands/DrawingFolderCommandEndpoints.cs` (166 lines) into the units its pattern names. 5 functions; the longest is 29 lines.
892. Divide `api/Features/Kpi/KpiEndpoints.cs` (166 lines) into the units its pattern names. 8 functions; the longest is 24 lines.
893. Divide `jpms/Components/FinancialsTable.Figures.cs` (166 lines) into the units its pattern names. 4 functions; the longest is 81 lines.
894. Divide `api/Features/Sales/Inbox/SalesInboxHandlers.cs` (165 lines) into the units its pattern names. 8 functions; the longest is 26 lines.
895. Divide `contracts/Documents/Excel/ExcelWorkbook.cs` (165 lines) into the units its pattern names. 2 functions; the longest is 65 lines.
896. Divide `jpms/Components/ProjectContractTermsDialog.razor.cs` (165 lines) into the units its pattern names. 3 functions; the longest is 59 lines.
897. Divide `jpms/Components/ValuationInvoicesSection.Commands.cs` (165 lines) into the units its pattern names. 14 functions; the longest is 17 lines.
898. Divide `jpms/Pages/ProjectLabour.Settlement.cs` (165 lines) into the units its pattern names. 7 functions; the longest is 63 lines.
899. Divide `jpms/Pages/TriageQueue.Views.cs` (165 lines) into the units its pattern names. 8 functions; the longest is 47 lines.
900. Divide `jpms/Services/HttpProjectContractStore.cs` (165 lines) into the units its pattern names. 7 functions; the longest is 31 lines.
901. Divide `api/Features/MailboxIntake/MailboxIntakeOptions.cs` (164 lines) into the units its pattern names. 2 functions; the longest is 39 lines.
902. Divide `api/Features/Ai/Tools/AiSourceTools.ReadSource.cs` (163 lines) into the units its pattern names. 5 functions; the longest is 70 lines.
903. Divide `jpms/Services/ErrorReporter.cs` (163 lines) into the units its pattern names. 9 functions; the longest is 21 lines.
904. Divide `api/Features/Requests/Recipients/RequestRecipientResolver.cs` (162 lines) into the units its pattern names. 3 functions; the longest is 95 lines.
905. Divide `jpms/Features/Drawings/DrawingsReadModel.cs` (162 lines) into the units its pattern names. 12 functions; the longest is 36 lines.
906. Divide `api/Features/Drawings/Geometry/PdfGeometryExtractor.cs` (161 lines) into the units its pattern names. 7 functions; the longest is 38 lines.
907. Divide `jpms/Components/ValuationSnapshotViewer.razor.cs` (161 lines) into the units its pattern names. 8 functions; the longest is 15 lines.
908. Divide `jpms/Features/Triage/RecordLinkVocabulary.cs` (161 lines) into the units its pattern names. 5 functions; the longest is 26 lines.
909. Divide `api/Features/Labour/Commands/WeekSignOffSlices.cs` (160 lines) into the units its pattern names. 9 functions; the longest is 40 lines.
910. Divide `api/Features/MailboxIntake/Graph/MailboxGraphClient.Reading.cs` (159 lines) into the units its pattern names. 9 functions; the longest is 55 lines.
911. Divide `jpms/Features/Commercial/ValuationReportReadModels.cs` (159 lines) into the units its pattern names. 6 functions; the longest is 26 lines.
912. Divide `jpms/Pages/ProjectVariationDetail.Menus.cs` (159 lines) into the units its pattern names. 0 functions; the longest is 0 lines.
913. Divide `jpms/Pages/XeroAllocation.Tabs.cs` (159 lines) into the units its pattern names. 5 functions; the longest is 51 lines.
914. Divide `api/Features/Ai/Tools/AiRecordTools.XeroCustomers.cs` (158 lines) into the units its pattern names. 1 functions; the longest is 132 lines.
915. Divide `api/Features/Sales/Imagine/AzureImageClient.cs` (158 lines) into the units its pattern names. 7 functions; the longest is 47 lines.
916. Divide `jpms/Features/Triage/Panels/PathwayPane.razor.cs` (158 lines) into the units its pattern names. 8 functions; the longest is 10 lines.
917. Divide `api/Features/Commercial/Documents/CostCentreReconciliationPdfBuilder.cs` (157 lines) into the units its pattern names. 3 functions; the longest is 105 lines.
918. Divide `api/Features/Requests/RequestsFeatureRegistration.cs` (157 lines) into the units its pattern names. 2 functions; the longest is 129 lines.
919. Divide `contracts/Labour/ForecastRules.cs` (157 lines) into the units its pattern names. 8 functions; the longest is 19 lines.
920. Divide `jpms/Features/Procurement/TenderSubmissionModal.razor.cs` (157 lines) into the units its pattern names. 6 functions; the longest is 45 lines.
921. Divide `jpms/Pages/ProjectWorkOrders.razor.cs` (157 lines) into the units its pattern names. 3 functions; the longest is 73 lines.
922. Divide `api/Features/BuildingControl/Commands/BuildingControlCaseCommands.cs` (156 lines) into the units its pattern names. 8 functions; the longest is 60 lines.
923. Divide `api/Features/Labour/Commands/ApproveTimesheetsSlice.cs` (156 lines) into the units its pattern names. 4 functions; the longest is 110 lines.
924. Divide `api/Features/Registers/PolicyDocumentSlices.cs` (156 lines) into the units its pattern names. 9 functions; the longest is 41 lines.
925. Divide `api/Data/Entities/LabourPlanningEntities.cs` (154 lines) into the units its pattern names. 0 functions; the longest is 0 lines.
926. Divide `api/Features/Ai/Tools/Actions/LabourAndBackOfficeActions.HealthAndSafety.cs` (154 lines) into the units its pattern names. 1 functions; the longest is 144 lines.
927. Divide `api/Features/Ai/Tools/Actions/SiteAndProgressActions.CloseoutDefects.cs` (154 lines) into the units its pattern names. 1 functions; the longest is 134 lines.
928. Divide `api/Features/Procurement/Commands/UpdateManualWorkOrderHandler.cs` (154 lines) into the units its pattern names. 1 functions; the longest is 125 lines.
929. Divide `jpms/Components/ProjectRetentionPanel.razor.cs` (154 lines) into the units its pattern names. 7 functions; the longest is 18 lines.
930. Divide `api/Features/Procurement/Commands/AwardBidPackageHandler.cs` (153 lines) into the units its pattern names. 2 functions; the longest is 72 lines.
931. Divide `api/Features/Commercial/CommercialFeatureRegistration.cs` (152 lines) into the units its pattern names. 1 functions; the longest is 142 lines.
932. Divide `api/Features/Commercial/Documents/ValuationReportSnapshotPdfBuilder.cs` (152 lines) into the units its pattern names. 1 functions; the longest is 144 lines.
933. Divide `api/Features/DocumentControl/Commands/DocumentControlItemCommandEndpoints.cs` (152 lines) into the units its pattern names. 6 functions; the longest is 27 lines.
934. Divide `api/Features/Labour/Commands/MoveTimesheetSlice.cs` (152 lines) into the units its pattern names. 7 functions; the longest is 62 lines.
935. Divide `api/Features/Procurement/Attachments/CompanyTenderTermsStore.cs` (152 lines) into the units its pattern names. 6 functions; the longest is 24 lines.
936. Divide `jpms/Features/Labour/WeekEntryModal.razor.cs` (152 lines) into the units its pattern names. 6 functions; the longest is 45 lines.
937. Divide `jpms/Pages/ProjectLabour.razor.cs` (152 lines) into the units its pattern names. 5 functions; the longest is 26 lines.
938. Divide `api/Features/Ai/Tools/AiKpiTools.cs` (151 lines) into the units its pattern names. 1 functions; the longest is 129 lines.
939. Divide `contracts/Commercial/Export/ValuationSnapshotExport.cs` (151 lines) into the units its pattern names. 5 functions; the longest is 50 lines.
940. Divide `contracts/Models/Labour.cs` (151 lines) into the units its pattern names. 0 functions; the longest is 0 lines.
941. Divide `jpms/Features/Triage/Panels/Actions/SystemActionKind.cs` (151 lines) into the units its pattern names. 2 functions; the longest is 27 lines.
942. Divide `api/Features/Labour/SettlementScheduleBuilder.cs` (150 lines) into the units its pattern names. 1 functions; the longest is 134 lines.
943. Divide `contracts/Ai/ModalCatalog.Labour.cs` (150 lines) into the units its pattern names. 0 functions; the longest is 0 lines.
944. Divide `contracts/Xero/XeroSitePnl.cs` (150 lines) into the units its pattern names. 3 functions; the longest is 43 lines.
945. Divide `api/Features/RecordLinks/RecordThreadTagger.cs` (149 lines) into the units its pattern names. 4 functions; the longest is 52 lines.
946. Divide `api/Features/Xero/Ledger/LabourSupplierRecognition.cs` (149 lines) into the units its pattern names. 6 functions; the longest is 43 lines.
947. Divide `contracts/Ai/ModalCatalog.Mail.cs` (149 lines) into the units its pattern names. 0 functions; the longest is 0 lines.
948. Divide `jpms/Components/ValuationReportTable.PercentEditing.cs` (149 lines) into the units its pattern names. 7 functions; the longest is 38 lines.
949. Divide `jpms/Features/Closeout/Detail/DefectCommunicationsPanel.razor.cs` (149 lines) into the units its pattern names. 9 functions; the longest is 14 lines.
950. Divide `jpms/Features/Procurement/DraftWorkOrdersPanel.razor.cs` (149 lines) into the units its pattern names. 4 functions; the longest is 37 lines.
951. Divide `jpms/Pages/ProjectBidPackageInviteDetail.Communications.cs` (149 lines) into the units its pattern names. 6 functions; the longest is 44 lines.
952. Divide `api/Features/Hs/Audits/Commands/HsAuditEndpoints.cs` (148 lines) into the units its pattern names. 6 functions; the longest is 33 lines.
953. Divide `api/Features/Bluebeam/Extraction/DrawingExtractionResultWriter.cs` (147 lines) into the units its pattern names. 7 functions; the longest is 38 lines.
954. Divide `api/Features/Drawings/Geometry/DrawingStructureBuilder.cs` (147 lines) into the units its pattern names. 6 functions; the longest is 46 lines.
955. Divide `jpms/Features/Drawings/DrawingArchiveExpander.cs` (147 lines) into the units its pattern names. 8 functions; the longest is 29 lines.
956. Divide `jpms/Pages/ProjectVariations.StatusChanges.cs` (147 lines) into the units its pattern names. 6 functions; the longest is 52 lines.
957. Divide `api/Features/RecordLinks/Providers/WorkOrderLinkProvider.cs` (146 lines) into the units its pattern names. 4 functions; the longest is 32 lines.
958. Divide `contracts/Models/LeadEstimate.cs` (146 lines) into the units its pattern names. 6 functions; the longest is 52 lines.
959. Divide `api/Features/Ai/Tools/Actions/SalesActions.Estimates.cs` (145 lines) into the units its pattern names. 1 functions; the longest is 118 lines.
960. Divide `api/Features/Ai/Tools/AiDeliveryTools.Hs.cs` (145 lines) into the units its pattern names. 6 functions; the longest is 47 lines.
961. Divide `api/Features/Connect/TokenEndpoint.cs` (145 lines) into the units its pattern names. 8 functions; the longest is 43 lines.
962. Divide `jpms/Services/HttpXeroLedgerStore.cs` (145 lines) into the units its pattern names. 15 functions; the longest is 20 lines.
963. Divide `api/Features/BuildingControl/Commands/BuildingControlInspectionCommands.cs` (143 lines) into the units its pattern names. 11 functions; the longest is 20 lines.
964. Divide `api/Features/Connect/OAuthTokenManager.cs` (143 lines) into the units its pattern names. 5 functions; the longest is 45 lines.
965. Divide `api/Features/Procurement/Queries/SearchLocalSubcontractorsHandler.cs` (143 lines) into the units its pattern names. 4 functions; the longest is 89 lines.
966. Divide `jpms/Pages/TriageQueue.ApplyPlan.cs` (143 lines) into the units its pattern names. 4 functions; the longest is 52 lines.
967. Divide `jpms/Pages/WeeklyCashflow.razor.cs` (143 lines) into the units its pattern names. 4 functions; the longest is 27 lines.
968. Divide `api/Data/Entities/PeopleEntities.cs` (142 lines) into the units its pattern names. 0 functions; the longest is 0 lines.
969. Divide `api/Features/Ai/AnthropicOptions.cs` (142 lines) into the units its pattern names. 3 functions; the longest is 64 lines.
970. Divide `api/Features/Ai/Tools/Actions/CommercialActions.Cvr.cs` (141 lines) into the units its pattern names. 1 functions; the longest is 127 lines.
971. Divide `contracts/Xero/WorkOrderBills.cs` (141 lines) into the units its pattern names. 3 functions; the longest is 31 lines.
972. Divide `api/Features/Bluebeam/Extraction/BluebeamMarkupParser.cs` (140 lines) into the units its pattern names. 10 functions; the longest is 22 lines.
973. Divide `api/Features/BuildingControl/Commands/CreateBuildingControlInspectionFromMessage.cs` (140 lines) into the units its pattern names. 6 functions; the longest is 30 lines.
974. Divide `api/Features/Xero/Ledger/XeroLedgerReads.cs` (140 lines) into the units its pattern names. 4 functions; the longest is 53 lines.
975. Divide `jpms/Pages/LabourOverview.razor.cs` (140 lines) into the units its pattern names. 9 functions; the longest is 28 lines.
976. Divide `api/Features/Ai/Tools/AiSourceTools.ListSources.cs` (139 lines) into the units its pattern names. 2 functions; the longest is 119 lines.
977. Divide `api/Features/Procurement/Documents/WorkOrderPoRenderer.Helpers.cs` (139 lines) into the units its pattern names. 10 functions; the longest is 25 lines.
978. Divide `api/Features/RecordLinks/Providers/ValuationClaimLinkProvider.cs` (139 lines) into the units its pattern names. 7 functions; the longest is 23 lines.
979. Divide `api/Features/Sales/Commands/EstimateChangeHandlers.cs` (139 lines) into the units its pattern names. 4 functions; the longest is 57 lines.
980. Divide `api/Features/Xero/XeroClient.ReadsCash.cs` (139 lines) into the units its pattern names. 5 functions; the longest is 58 lines.
981. Divide `contracts/Commercial/ProjectDrawdown.cs` (139 lines) into the units its pattern names. 3 functions; the longest is 74 lines.
982. Divide `jpms/Features/Triage/Panels/NewEmailComposerPane.razor.cs` (139 lines) into the units its pattern names. 6 functions; the longest is 45 lines.
983. Divide `jpms/Features/WeeklyCashflow/CashflowItemModal.razor.cs` (139 lines) into the units its pattern names. 5 functions; the longest is 30 lines.
984. Divide `api/Features/Commercial/Documents/ValuationReportBillRows.cs` (138 lines) into the units its pattern names. 7 functions; the longest is 22 lines.
985. Divide `api/Features/Drawings/Commands/DrawingFolderCommandHandlers.cs` (138 lines) into the units its pattern names. 4 functions; the longest is 31 lines.
986. Divide `api/Features/Drawings/Geometry/DrawingScaleCalibrator.cs` (138 lines) into the units its pattern names. 4 functions; the longest is 39 lines.
987. Divide `api/Features/RecordLinks/Providers/ValuationReportSnapshotLinkProvider.cs` (138 lines) into the units its pattern names. 7 functions; the longest is 24 lines.
988. Divide `api/Features/Sales/Imagine/ImagineNotifier.cs` (138 lines) into the units its pattern names. 8 functions; the longest is 20 lines.
989. Divide `jpms/Pages/Todos.Filters.cs` (138 lines) into the units its pattern names. 5 functions; the longest is 19 lines.
990. Divide `jpms/Services/UserInviteService.cs` (138 lines) into the units its pattern names. 6 functions; the longest is 23 lines.
991. Divide `api/Data/Entities/CoreEntities.cs` (137 lines) into the units its pattern names. 0 functions; the longest is 0 lines.
992. Divide `api/Features/Ai/Tools/Actions/ProcurementActions.BidPackages.cs` (137 lines) into the units its pattern names. 1 functions; the longest is 129 lines.
993. Divide `jpms/Features/Labour/LabourReadModels.cs` (137 lines) into the units its pattern names. 6 functions; the longest is 12 lines.
994. Divide `jpms/Features/Procurement/ValuationLinePickerModal.razor.cs` (137 lines) into the units its pattern names. 8 functions; the longest is 17 lines.
995. Divide `jpms/Features/WeeklyCashflow/SupplierGroupsModal.razor.cs` (137 lines) into the units its pattern names. 7 functions; the longest is 28 lines.
996. Divide `jpms/Pages/CashForecast.Export.cs` (137 lines) into the units its pattern names. 2 functions; the longest is 105 lines.
997. Divide `api/Features/Ai/Tools/AiRecordTools.Contexts.cs` (136 lines) into the units its pattern names. 1 functions; the longest is 126 lines.
998. Divide `jpms/Features/Sales/SalesReadModels.cs` (136 lines) into the units its pattern names. 8 functions; the longest is 11 lines.
999. Divide `api/Features/Commercial/Commands/SetXeroLineWorkOrderLinksHandler.cs` (135 lines) into the units its pattern names. 1 functions; the longest is 110 lines.
1000. Divide `api/Features/Requests/Commands/ReplyInThreadFromMessageHandler.cs` (135 lines) into the units its pattern names. 3 functions; the longest is 90 lines.
1001. Divide `api/Features/Site/Commands/SuggestProgrammeDraftMappingsHandler.cs` (135 lines) into the units its pattern names. 6 functions; the longest is 45 lines.
1002. Divide `jpms/Features/Labour/LabourRouteRegistration.cs` (135 lines) into the units its pattern names. 2 functions; the longest is 114 lines.
1003. Divide `jpms/Pages/ProjectWorkOrders.Rows.cs` (135 lines) into the units its pattern names. 3 functions; the longest is 55 lines.
1004. Divide `api/Features/Labour/Commands/SubmitWorkerWeekSlice.cs` (134 lines) into the units its pattern names. 3 functions; the longest is 67 lines.
1005. Divide `api/Features/Subcontractors/Queries/GetSubcontractorStatementHandler.cs` (134 lines) into the units its pattern names. 2 functions; the longest is 73 lines.
1006. Divide `jpms/Pages/XeroAllocation.InvoiceViewer.cs` (134 lines) into the units its pattern names. 8 functions; the longest is 24 lines.
1007. Divide `api/Features/Sales/SalesFeatureRegistration.cs` (133 lines) into the units its pattern names. 1 functions; the longest is 100 lines.
1008. Divide `api/Features/ValuationInvoices/XeroRaise/ValuationInvoiceXeroRaisePlanner.cs` (133 lines) into the units its pattern names. 6 functions; the longest is 32 lines.
1009. Divide `contracts/Ai/PageGuides/ProcurementPageGuides.cs` (133 lines) into the units its pattern names. 0 functions; the longest is 0 lines.
1010. Divide `jpms/Services/HttpIntakeQueue.cs` (133 lines) into the units its pattern names. 3 functions; the longest is 73 lines.
1011. Divide `api/Features/ArchitectInstructions/Storage/ArchitectInstructionBlobStore.cs` (132 lines) into the units its pattern names. 6 functions; the longest is 18 lines.
1012. Divide `api/Features/BuildingControl/Attachments/BuildingControlAttachmentHandlers.cs` (132 lines) into the units its pattern names. 8 functions; the longest is 35 lines.
1013. Divide `api/Features/Commercial/Documents/CostCentreReconciliationRenderer.Helpers.cs` (132 lines) into the units its pattern names. 10 functions; the longest is 14 lines.
1014. Divide `api/Features/MailboxIntake/Compose/SendMailboxEmailHandler.Compose.cs` (132 lines) into the units its pattern names. 3 functions; the longest is 28 lines.
1015. Divide `api/Features/MailboxIntake/Graph/MailboxGraphClient.Snapshots.cs` (132 lines) into the units its pattern names. 3 functions; the longest is 51 lines.
1016. Divide `api/Features/Registers/RegisterItemSlices.cs` (132 lines) into the units its pattern names. 9 functions; the longest is 33 lines.
1017. Divide `contracts/Models/Imagine.cs` (132 lines) into the units its pattern names. 2 functions; the longest is 78 lines.
1018. Divide `contracts/Models/LabourPlanning.cs` (132 lines) into the units its pattern names. 0 functions; the longest is 0 lines.
1019. Divide `api/Features/Ai/Tools/AiSkillTools.cs` (131 lines) into the units its pattern names. 1 functions; the longest is 111 lines.
1020. Divide `api/Features/RecordLinks/Commands/LinkMessageToRecordHandler.cs` (131 lines) into the units its pattern names. 2 functions; the longest is 90 lines.
1021. Divide `jpms/Pages/ProjectRequests.RowActions.cs` (131 lines) into the units its pattern names. 4 functions; the longest is 47 lines.
1022. Divide `jpms/Services/HttpPortalStore.cs` (131 lines) into the units its pattern names. 9 functions; the longest is 26 lines.
1023. Divide `api/Features/Commercial/PackageReconciliationCalculator.cs` (130 lines) into the units its pattern names. 1 functions; the longest is 115 lines.
1024. Divide `jpms/Services/ILabourStore.cs` (130 lines) into the units its pattern names. 0 functions; the longest is 0 lines.
1025. Divide `worker/MailboxIntake/Graph/IGraphMailClient.cs` (130 lines) into the units its pattern names. 1 functions; the longest is 59 lines.
1026. Divide `api/Features/Ai/Tools/AiCommercialTools.CostCodes.cs` (129 lines) into the units its pattern names. 1 functions; the longest is 122 lines.
1027. Divide `api/Features/Labour/Commands/WorkerDayCorrectionByNameSlices.cs` (129 lines) into the units its pattern names. 8 functions; the longest is 17 lines.
1028. Divide `api/Features/Procurement/Commands/SendWorkOrderPoEmailHandler.cs` (129 lines) into the units its pattern names. 3 functions; the longest is 90 lines.
1029. Divide `contracts/Commercial/ValuationSummaryFigures.cs` (129 lines) into the units its pattern names. 1 functions; the longest is 106 lines.
1030. Divide `jpms/Components/ValuationInvoicesSection.Forms.cs` (129 lines) into the units its pattern names. 9 functions; the longest is 30 lines.
1031. Divide `jpms/Features/Procurement/LocalSubcontractorFinderModal.razor.cs` (128 lines) into the units its pattern names. 4 functions; the longest is 50 lines.
1032. Divide `jpms/Services/HttpProcurementStore.cs` (127 lines) into the units its pattern names. 13 functions; the longest is 10 lines.
1033. Divide `api/Data/Entities/ValuationReportEntities.cs` (126 lines) into the units its pattern names. 0 functions; the longest is 0 lines.
1034. Divide `api/Features/Ai/AiRegistryDriftCheck.cs` (126 lines) into the units its pattern names. 2 functions; the longest is 63 lines.
1035. Divide `api/Features/Ai/Tools/AiRecordTools.Compliance.cs` (126 lines) into the units its pattern names. 1 functions; the longest is 102 lines.
1036. Divide `jpms/Cqrs/HttpCommandSender.cs` (126 lines) into the units its pattern names. 3 functions; the longest is 20 lines.
1037. Divide `jpms/Pages/XeroAllocation.WorkOrderBills.cs` (126 lines) into the units its pattern names. 5 functions; the longest is 31 lines.
1038. Divide `api/Features/Ai/Tools/AiAuditTools.cs` (125 lines) into the units its pattern names. 3 functions; the longest is 51 lines.
1039. Divide `api/Features/Subcontractors/Commands/ImportXeroSupplierHandler.cs` (125 lines) into the units its pattern names. 2 functions; the longest is 93 lines.
1040. Divide `api/Features/Xero/XeroOptions.cs` (125 lines) into the units its pattern names. 1 functions; the longest is 46 lines.
1041. Divide `contracts/Models/Commercial.cs` (125 lines) into the units its pattern names. 2 functions; the longest is 41 lines.
1042. Divide `jpms/Services/ArchitectInstructionStore.cs` (125 lines) into the units its pattern names. 3 functions; the longest is 43 lines.
1043. Divide `jpms/Services/HttpValuationInvoiceStore.cs` (125 lines) into the units its pattern names. 14 functions; the longest is 15 lines.
1044. Divide `api/Features/Ai/Sources/AiSourceReader.Reading.cs` (124 lines) into the units its pattern names. 4 functions; the longest is 75 lines.
1045. Divide `api/Features/Ai/Tools/BidPackageContextReads.cs` (124 lines) into the units its pattern names. 5 functions; the longest is 47 lines.
1046. Divide `api/Features/Progress/ContractorsReports/Documents/ContractorsReportPdfRenderer.Narrative.cs` (124 lines) into the units its pattern names. 9 functions; the longest is 16 lines.
1047. Divide `contracts/Models/CalendarEvent.cs` (124 lines) into the units its pattern names. 6 functions; the longest is 18 lines.
1048. Divide `jpms/Features/Procurement/SubcontractorInvitePickerModal.razor.cs` (124 lines) into the units its pattern names. 2 functions; the longest is 25 lines.
1049. Divide `jpms/Features/Sales/SalesRouteRegistration.cs` (124 lines) into the units its pattern names. 2 functions; the longest is 107 lines.
1050. Divide `api/Features/Labour/Commands/SettlementCommandGates.cs` (123 lines) into the units its pattern names. 8 functions; the longest is 22 lines.
1051. Divide `api/Features/Labour/Queries/GetMyLabourDaySlice.cs` (123 lines) into the units its pattern names. 3 functions; the longest is 85 lines.
1052. Divide `api/Features/ProjectContracts/Storage/AzureBlobProjectContractStore.cs` (123 lines) into the units its pattern names. 8 functions; the longest is 17 lines.
1053. Divide `api/Features/Sales/Inbox/ISalesMailbox.cs` (123 lines) into the units its pattern names. 5 functions; the longest is 20 lines.
1054. Divide `api/Features/Xero/TrackingOptions/XeroCostCodeOptionSlices.cs` (123 lines) into the units its pattern names. 5 functions; the longest is 41 lines.
1055. Divide `jpms/Components/ReportingErrorBoundary.cs` (123 lines) into the units its pattern names. 2 functions; the longest is 86 lines.
1056. Divide `api/Features/Ai/Tools/AiToolCatalogue.cs` (122 lines) into the units its pattern names. 2 functions; the longest is 23 lines.
1057. Divide `api/Features/Auth/PasswordResetSender.cs` (122 lines) into the units its pattern names. 6 functions; the longest is 39 lines.
1058. Divide `api/Features/Commercial/Commands/SetValuationLineCostCentreHandler.cs` (122 lines) into the units its pattern names. 2 functions; the longest is 87 lines.
1059. Divide `api/Features/Directory/Queries/ListEmailRecipientsHandler.cs` (122 lines) into the units its pattern names. 1 functions; the longest is 98 lines.
1060. Divide `api/Features/RecordLinks/Providers/VariationOrderLinkProvider.cs` (122 lines) into the units its pattern names. 6 functions; the longest is 19 lines.
1061. Divide `api/Features/Xero/SitePnl/GetXeroSitePnlHandler.cs` (122 lines) into the units its pattern names. 3 functions; the longest is 60 lines.
1062. Divide `contracts/MailboxCompose/SendMailboxEmail.cs` (122 lines) into the units its pattern names. 1 functions; the longest is 62 lines.
1063. Divide `contracts/Models/ProjectContract.cs` (122 lines) into the units its pattern names. 3 functions; the longest is 68 lines.
1064. Divide `jpms/Components/FinancialsTable.Export.cs` (122 lines) into the units its pattern names. 3 functions; the longest is 85 lines.
1065. Divide `jpms/Pages/ProjectWorkOrders.Export.cs` (122 lines) into the units its pattern names. 1 functions; the longest is 106 lines.
1066. Divide `api/Features/Ai/Tools/Actions/AiActionExecutor.cs` (121 lines) into the units its pattern names. 5 functions; the longest is 49 lines.
1067. Divide `api/Features/RecordLinks/Commands/PrepareProgrammeReplyDraftHandler.cs` (121 lines) into the units its pattern names. 3 functions; the longest is 69 lines.
1068. Divide `api/Features/RecordLinks/Providers/RequestLinkProvider.cs` (121 lines) into the units its pattern names. 5 functions; the longest is 28 lines.
1069. Divide `api/Features/Xero/XeroFeatureRegistration.cs` (121 lines) into the units its pattern names. 1 functions; the longest is 102 lines.
1070. Divide `contracts/Models/ValuationInvoice.cs` (121 lines) into the units its pattern names. 2 functions; the longest is 47 lines.
1071. Divide `jpms/Features/Variations/VariationsRouteRegistration.cs` (121 lines) into the units its pattern names. 1 functions; the longest is 111 lines.
1072. Divide `api/Features/BuildingControl/BuildingControlRules.cs` (120 lines) into the units its pattern names. 8 functions; the longest is 17 lines.
1073. Divide `api/Features/Kpi/Commands/MarkEmailAsKpiHandler.cs` (120 lines) into the units its pattern names. 4 functions; the longest is 52 lines.
1074. Divide `api/Features/Sales/Commands/StrategyHandlers.cs` (120 lines) into the units its pattern names. 5 functions; the longest is 24 lines.
1075. Divide `api/Features/ValuationInvoices/Commands/CreateValuationInvoiceHandler.cs` (120 lines) into the units its pattern names. 2 functions; the longest is 85 lines.
1076. Divide `contracts/Models/ProgrammeDraft.cs` (120 lines) into the units its pattern names. 6 functions; the longest is 20 lines.
1077. Divide `worker/Bluebeam/BluebeamConnectCallback.cs` (120 lines) into the units its pattern names. 7 functions; the longest is 36 lines.
1078. Divide `api/Features/Ai/Tools/Actions/ProcurementActions.Tenders.cs` (119 lines) into the units its pattern names. 1 functions; the longest is 111 lines.
1079. Divide `api/Features/Procurement/Commands/RecodeWorkOrderLineHandler.cs` (119 lines) into the units its pattern names. 2 functions; the longest is 88 lines.
1080. Divide `api/Features/Progress/Queries/DownloadProgressReportPdfEndpoint.cs` (119 lines) into the units its pattern names. 3 functions; the longest is 9 lines.
1081. Divide `api/Features/Requests/Commands/UpdateRequestDetailsHandler.cs` (119 lines) into the units its pattern names. 3 functions; the longest is 90 lines.
1082. Divide `api/Features/Sales/Inbox/SalesInboxEndpoints.cs` (119 lines) into the units its pattern names. 6 functions; the longest is 17 lines.
1083. Divide `api/Features/Sales/SalesEntityMapping.cs` (119 lines) into the units its pattern names. 2 functions; the longest is 104 lines.
1084. Divide `jpms/Features/Todos/Detail/TodoCommunicationsPanel.razor.cs` (119 lines) into the units its pattern names. 8 functions; the longest is 15 lines.
1085. Divide `api/Features/Ai/Tools/AiSitePhotoTools.cs` (118 lines) into the units its pattern names. 4 functions; the longest is 33 lines.
1086. Divide `api/Features/Ai/Tools/AiTool.cs` (118 lines) into the units its pattern names. 2 functions; the longest is 87 lines.
1087. Divide `api/Features/ArchitectInstructions/ArchitectInstructionSupport.cs` (118 lines) into the units its pattern names. 2 functions; the longest is 59 lines.
1088. Divide `api/Features/ProjectContracts/Commands/UploadProjectContractAmendmentEndpoint.cs` (118 lines) into the units its pattern names. 2 functions; the longest is 15 lines.
1089. Divide `api/Features/Subcontractors/SubcontractorsFeatureRegistration.cs` (118 lines) into the units its pattern names. 2 functions; the longest is 88 lines.
1090. Divide `api/Features/ValuationInvoices/XeroPayments/SyncValuationInvoicePaymentsFromXeroHandler.cs` (118 lines) into the units its pattern names. 5 functions; the longest is 42 lines.
1091. Divide `jpms/Services/IIntakeQueue.cs` (118 lines) into the units its pattern names. 0 functions; the longest is 0 lines.
1092. Divide `api/Features/Commercial/Queries/ListCostCentreActualCostsHandler.cs` (117 lines) into the units its pattern names. 2 functions; the longest is 91 lines.
1093. Divide `api/Features/Xero/Ledger/XeroInvoiceAttachmentEndpoints.cs` (117 lines) into the units its pattern names. 5 functions; the longest is 39 lines.
1094. Divide `contracts/Documents/Excel/ExcelWorkbookWriter.Worksheet.cs` (117 lines) into the units its pattern names. 2 functions; the longest is 89 lines.
1095. Divide `api/Features/Ai/Tools/Actions/SubcontractorsAndLeadsActions.Contacts.cs` (116 lines) into the units its pattern names. 1 functions; the longest is 98 lines.
1096. Divide `contracts/Commercial/ClaimRespread.cs` (116 lines) into the units its pattern names. 1 functions; the longest is 81 lines.
1097. Divide `contracts/Models/Lead.cs` (116 lines) into the units its pattern names. 2 functions; the longest is 32 lines.
1098. Divide `jpms/Services/CurrentProjectService.cs` (116 lines) into the units its pattern names. 5 functions; the longest is 14 lines.
1099. Divide `jpms/Services/IDrawingStore.cs` (116 lines) into the units its pattern names. 0 functions; the longest is 0 lines.
1100. Divide `api/Features/BuildingControl/Commands/BuildingControlInspectionEndpoints.cs` (115 lines) into the units its pattern names. 5 functions; the longest is 25 lines.
1101. Divide `api/Features/Commercial/Commands/ReconciliationPackageEndpoints.cs` (115 lines) into the units its pattern names. 4 functions; the longest is 15 lines.
1102. Divide `api/Features/Procurement/Commands/RetagWorkOrderWorkflowTagsHandler.cs` (115 lines) into the units its pattern names. 7 functions; the longest is 27 lines.
1103. Divide `jpms/Components/ValuationReportTable.BulkPercent.cs` (115 lines) into the units its pattern names. 4 functions; the longest is 56 lines.
1104. Divide `jpms/Features/ValuationInvoices/ValuationInvoiceXeroRaiseModal.razor.cs` (115 lines) into the units its pattern names. 8 functions; the longest is 18 lines.
1105. Divide `api/Features/MailboxIntake/Compose/SendMailboxEmailHandler.cs` (114 lines) into the units its pattern names. 2 functions; the longest is 29 lines.
1106. Divide `api/Features/Site/Drafts/ProgrammeDraftSuggestionPrompt.cs` (114 lines) into the units its pattern names. 2 functions; the longest is 50 lines.
1107. Divide `contracts/Models/VariationOrder.cs` (114 lines) into the units its pattern names. 4 functions; the longest is 47 lines.
1108. Divide `jpms/Features/Triage/TriageEmailDisplay.cs` (114 lines) into the units its pattern names. 7 functions; the longest is 19 lines.
1109. Divide `jpms/Pages/TriageQueue.ApplySends.cs` (114 lines) into the units its pattern names. 4 functions; the longest is 46 lines.
1110. Divide `api/Features/Ai/Tools/Actions/LabourAndBackOfficeActions.WorkerLinks.cs` (113 lines) into the units its pattern names. 1 functions; the longest is 104 lines.
1111. Divide `api/Features/DocumentControl/Commands/ExtractDocumentControlArchiveHandler.cs` (113 lines) into the units its pattern names. 5 functions; the longest is 53 lines.
1112. Divide `api/Features/MailboxIntake/Compose/SendMailboxEmailHandler.Attachments.cs` (113 lines) into the units its pattern names. 3 functions; the longest is 64 lines.
1113. Divide `jpms/Pages/CashForecast.Statement.cs` (113 lines) into the units its pattern names. 3 functions; the longest is 59 lines.
1114. Divide `jpms/Pages/ProjectVariations.Search.cs` (113 lines) into the units its pattern names. 4 functions; the longest is 25 lines.
1115. Divide `api/Data/Entities/BluebeamEntities.cs` (112 lines) into the units its pattern names. 0 functions; the longest is 0 lines.
1116. Divide `api/Features/Ai/Tools/AiFinanceTools.LedgerLine.cs` (112 lines) into the units its pattern names. 3 functions; the longest is 51 lines.
1117. Divide `api/Features/Labour/Commands/UnapproveTimesheetSlice.cs` (112 lines) into the units its pattern names. 6 functions; the longest is 41 lines.
1118. Divide `api/Features/MailboxIntake/Compose/ComposeHtmlPipeline.cs` (112 lines) into the units its pattern names. 2 functions; the longest is 39 lines.
1119. Divide `contracts/Labour/WorkerWeekApproval.cs` (112 lines) into the units its pattern names. 0 functions; the longest is 0 lines.
1120. Divide `contracts/Models/Todo.cs` (112 lines) into the units its pattern names. 3 functions; the longest is 31 lines.
1121. Divide `jpms/Components/WorkOrderForm.razor.cs` (112 lines) into the units its pattern names. 6 functions; the longest is 17 lines.
1122. Divide `api/Features/Bluebeam/BluebeamClient.cs` (111 lines) into the units its pattern names. 6 functions; the longest is 34 lines.
1123. Divide `api/Features/Requests/Commands/MailboxMoveCommandHandlers.cs` (111 lines) into the units its pattern names. 7 functions; the longest is 23 lines.
1124. Divide `api/Features/Xero/XeroClient.Attachments.cs` (111 lines) into the units its pattern names. 3 functions; the longest is 35 lines.
1125. Divide `api/Features/MailboxIntake/Compose/SendMailboxEmailHandler.Draft.cs` (110 lines) into the units its pattern names. 5 functions; the longest is 25 lines.
1126. Divide `contracts/Commercial/Export/ValuationReportExportWorkbook.cs` (110 lines) into the units its pattern names. 3 functions; the longest is 28 lines.
1127. Divide `jpms/Features/Triage/Panels/RecordExplorerPane.razor.cs` (110 lines) into the units its pattern names. 5 functions; the longest is 17 lines.
1128. Divide `jpms/Pages/ProjectBidPackageInviteDetail.Award.cs` (110 lines) into the units its pattern names. 4 functions; the longest is 27 lines.
1129. Divide `jpms/Pages/ProjectBidPackageInviteDetail.Lines.cs` (110 lines) into the units its pattern names. 5 functions; the longest is 19 lines.
1130. Divide `api/Data/Entities/LabourEntities.cs` (109 lines) into the units its pattern names. 0 functions; the longest is 0 lines.
1131. Divide `api/Data/Entities/ProgressEntities.cs` (109 lines) into the units its pattern names. 0 functions; the longest is 0 lines.
1132. Divide `api/Features/Ai/Scans/PngEncoder.cs` (109 lines) into the units its pattern names. 7 functions; the longest is 20 lines.
1133. Divide `api/Features/Progress/ContractorsReports/Documents/ContractorsReportWordRenderer.Sections.cs` (109 lines) into the units its pattern names. 7 functions; the longest is 14 lines.
1134. Divide `api/Features/Progress/ContractorsReports/Documents/ContractorsReportWordRenderer.cs` (109 lines) into the units its pattern names. 6 functions; the longest is 24 lines.
1135. Divide `api/Features/Progress/ProgressFeatureRegistration.cs` (109 lines) into the units its pattern names. 4 functions; the longest is 49 lines.
1136. Divide `api/Features/RecordLinks/Providers/SchedulingLinkProvider.cs` (109 lines) into the units its pattern names. 2 functions; the longest is 44 lines.
1137. Divide `api/Features/Subcontractors/Commands/UploadComplianceDocumentFileEndpoint.cs` (109 lines) into the units its pattern names. 2 functions; the longest is 13 lines.
1138. Divide `jpms/Features/Subcontractors/SubcontractorsRouteRegistration.cs` (109 lines) into the units its pattern names. 2 functions; the longest is 94 lines.
1139. Divide `jpms/Pages/TriageQueue.Decisions.cs` (109 lines) into the units its pattern names. 3 functions; the longest is 10 lines.
1140. Divide `api/Data/Entities/ConnectEntities.cs` (108 lines) into the units its pattern names. 0 functions; the longest is 0 lines.
1141. Divide `api/Features/Ai/Sources/AiSourceReader.cs` (108 lines) into the units its pattern names. 5 functions; the longest is 34 lines.
1142. Divide `api/Features/Ai/Tools/Actions/ProcurementActions.Invites.cs` (108 lines) into the units its pattern names. 1 functions; the longest is 100 lines.
1143. Divide `api/Features/Procurement/Documents/WorkOrderPoRenderer.Scope.cs` (108 lines) into the units its pattern names. 6 functions; the longest is 22 lines.
1144. Divide `api/Features/Requests/Commands/RaiseRequestHandler.cs` (108 lines) into the units its pattern names. 2 functions; the longest is 82 lines.
1145. Divide `api/Features/Requests/Documents/RequestDocumentRenderer.Helpers.cs` (108 lines) into the units its pattern names. 8 functions; the longest is 15 lines.
1146. Divide `api/Features/Sales/Commands/EstimateGates.cs` (108 lines) into the units its pattern names. 5 functions; the longest is 27 lines.
1147. Divide `api/Features/Sales/Queries/SalesQueryHandlers.cs` (108 lines) into the units its pattern names. 4 functions; the longest is 32 lines.
1148. Divide `api/Features/Variations/Commands/ReviseVariationOrderValueHandler.cs` (108 lines) into the units its pattern names. 2 functions; the longest is 83 lines.
1149. Divide `jpms/Components/WorkOrderForm.Draft.cs` (108 lines) into the units its pattern names. 4 functions; the longest is 24 lines.
1150. Divide `jpms/Services/HttpCloseoutStore.cs` (108 lines) into the units its pattern names. 8 functions; the longest is 12 lines.
1151. Divide `api/Features/Connect/RegisterClientEndpoint.cs` (107 lines) into the units its pattern names. 4 functions; the longest is 62 lines.
1152. Divide `api/Features/Procurement/Commands/PrepareWorkOrderReplyDraftHandler.cs` (107 lines) into the units its pattern names. 2 functions; the longest is 78 lines.
1153. Divide `api/Features/Procurement/Commands/SetBidPackageLineItemCoverageHandler.cs` (107 lines) into the units its pattern names. 1 functions; the longest is 90 lines.
1154. Divide `contracts/Ai/PageGuides/CommercialPageGuides.cs` (107 lines) into the units its pattern names. 0 functions; the longest is 0 lines.
1155. Divide `jpms/Features/Cvr/ProfitDisplay.cs` (107 lines) into the units its pattern names. 11 functions; the longest is 12 lines.
1156. Divide `api/Features/DocumentControl/Storage/AzureBlobDocumentControlStore.cs` (106 lines) into the units its pattern names. 8 functions; the longest is 17 lines.
1157. Divide `api/Features/MailboxIntake/MailboxIntakeFeatureRegistration.cs` (106 lines) into the units its pattern names. 1 functions; the longest is 83 lines.
1158. Divide `api/Features/Progress/ContractorsReports/Documents/ContractorsReportWordTables.cs` (106 lines) into the units its pattern names. 7 functions; the longest is 23 lines.
1159. Divide `api/Features/ProjectContracts/Commands/UploadProjectContractDocumentEndpoint.cs` (106 lines) into the units its pattern names. 2 functions; the longest is 15 lines.
1160. Divide `api/Features/Sales/Commands/ImagineHandlers.cs` (106 lines) into the units its pattern names. 4 functions; the longest is 26 lines.
1161. Divide `api/Features/Xero/XeroClient.SalesInvoices.cs` (106 lines) into the units its pattern names. 3 functions; the longest is 49 lines.
1162. Divide `contracts/Models/SalesProposal.cs` (106 lines) into the units its pattern names. 1 functions; the longest is 9 lines.
1163. Divide `jpms/Features/Directory/ConsolidateRecordsModal.razor.cs` (106 lines) into the units its pattern names. 3 functions; the longest is 34 lines.
1164. Divide `jpms/Features/Directory/XeroImportModal.razor.cs` (106 lines) into the units its pattern names. 6 functions; the longest is 16 lines.
1165. Divide `jpms/Pages/WeeklyCashflow.Moving.cs` (106 lines) into the units its pattern names. 5 functions; the longest is 26 lines.
1166. Divide `api/Features/Ai/Tools/AiToolCatalogue.RequestContext.cs` (105 lines) into the units its pattern names. 1 functions; the longest is 92 lines.
1167. Divide `api/Features/MailboxIntake/Compose/SendMailboxEmailHandler.Outcome.cs` (105 lines) into the units its pattern names. 6 functions; the longest is 26 lines.
1168. Divide `api/Features/Portal/Commands/UploadMyComplianceDocumentEndpoint.cs` (105 lines) into the units its pattern names. 2 functions; the longest is 68 lines.
1169. Divide `api/Features/Procurement/Documents/WorkOrderPoRenderer.Lines.cs` (105 lines) into the units its pattern names. 6 functions; the longest is 27 lines.
1170. Divide `api/Features/Sales/Research/StrategyResearchRunner.cs` (105 lines) into the units its pattern names. 2 functions; the longest is 69 lines.
1171. Divide `api/Features/Xero/Ledger/WorkOrderBillRecognition.cs` (105 lines) into the units its pattern names. 4 functions; the longest is 15 lines.
1172. Divide `jpms/Features/RecordLinks/RecordLinksRouteRegistration.cs` (105 lines) into the units its pattern names. 2 functions; the longest is 90 lines.
1173. Divide `api/Features/Ai/Tools/AiDeliveryTools.DocumentData.cs` (104 lines) into the units its pattern names. 3 functions; the longest is 42 lines.
1174. Divide `api/Features/DocumentControl/Commands/SendAttachmentsToDocumentControlHandler.cs` (104 lines) into the units its pattern names. 2 functions; the longest is 72 lines.
1175. Divide `api/Features/Requests/Documents/RequestDocumentModel.cs` (104 lines) into the units its pattern names. 1 functions; the longest is 75 lines.
1176. Divide `api/Features/Xero/Ledger/WorkOrderBills/UndoWorkOrderBillApprovalHandler.cs` (104 lines) into the units its pattern names. 4 functions; the longest is 43 lines.
1177. Divide `contracts/Models/Closeout.cs` (104 lines) into the units its pattern names. 2 functions; the longest is 40 lines.
1178. Divide `jpms/Features/Drawings/DrawingsRouteRegistration.cs` (104 lines) into the units its pattern names. 2 functions; the longest is 91 lines.
1179. Divide `api/Features/Drawings/Commands/UploadDrawingRevisionEndpoint.cs` (103 lines) into the units its pattern names. 2 functions; the longest is 17 lines.
1180. Divide `api/Features/Labour/Queries/ListLabourSettlementForProjectSlice.cs` (103 lines) into the units its pattern names. 3 functions; the longest is 71 lines.
1181. Divide `api/Features/Procurement/Commands/SendBidPackageInviteHandler.cs` (103 lines) into the units its pattern names. 3 functions; the longest is 63 lines.
1182. Divide `api/Features/Progress/SitePhotos/SitePhotoFiler.cs` (103 lines) into the units its pattern names. 5 functions; the longest is 25 lines.
1183. Divide `api/Features/RecordLinks/Providers/VariationOrderQuoteLinkProvider.cs` (103 lines) into the units its pattern names. 5 functions; the longest is 23 lines.
1184. Divide `api/Features/Requests/RequestsEntityMapping.cs` (103 lines) into the units its pattern names. 2 functions; the longest is 52 lines.
1185. Divide `api/Features/Variations/Documents/VariationDocumentCostBreakdown.cs` (103 lines) into the units its pattern names. 2 functions; the longest is 75 lines.
1186. Divide `contracts/BuildingControl/BuildingControlCommands.cs` (103 lines) into the units its pattern names. 0 functions; the longest is 0 lines.
1187. Divide `jpms/Components/ValuationInvoicesSection.razor.cs` (103 lines) into the units its pattern names. 1 functions; the longest is 12 lines.
1188. Divide `api/Data/Entities/ProjectContractEntities.cs` (102 lines) into the units its pattern names. 0 functions; the longest is 0 lines.
1189. Divide `api/Features/Ai/Tools/Actions/CommercialActions.cs` (102 lines) into the units its pattern names. 1 functions; the longest is 15 lines.
1190. Divide `api/Features/Ai/Tools/Actions/KpiActions.cs` (102 lines) into the units its pattern names. 1 functions; the longest is 79 lines.
1191. Divide `api/Features/Drawings/DrawingRevisionLanding.cs` (102 lines) into the units its pattern names. 3 functions; the longest is 37 lines.
1192. Divide `api/Features/Procurement/Commands/CreateManualWorkOrderHandler.cs` (102 lines) into the units its pattern names. 1 functions; the longest is 80 lines.
1193. Divide `api/Features/Progress/ContractorsReports/Documents/ContractorsReportWordParts.cs` (102 lines) into the units its pattern names. 8 functions; the longest is 14 lines.
1194. Divide `jpms/Pages/ProjectLabour.Corrections.cs` (102 lines) into the units its pattern names. 5 functions; the longest is 25 lines.
1195. Divide `api/Features/Ai/Tools/Actions/SiteAndProgressActions.ToDos.cs` (101 lines) into the units its pattern names. 1 functions; the longest is 82 lines.
1196. Divide `api/Features/Ai/Tools/AiDeliveryTools.Programme.cs` (101 lines) into the units its pattern names. 4 functions; the longest is 26 lines.
1197. Divide `api/Features/RecordLinks/Providers/DefectLinkProvider.cs` (101 lines) into the units its pattern names. 4 functions; the longest is 27 lines.
1198. Divide `api/Features/Site/Drafts/ProgrammeDraftCapture.cs` (101 lines) into the units its pattern names. 2 functions; the longest is 63 lines.
1199. Divide `contracts/Commercial/Export/ValuationExportStatementSheet.cs` (101 lines) into the units its pattern names. 5 functions; the longest is 23 lines.
1200. Divide `contracts/Commercial/ListWorkOrderInvoiceSummaries.cs` (101 lines) into the units its pattern names. 1 functions; the longest is 16 lines.

**Pass 4 — The sweep to zero**

1201. Explanatory comment lines: 16311 to zero. Scores 0.0% at weight 4; the offenders are in audit.json under details.comments, fifty at a time.
1202. Functions over the line limit: 836 to zero. Scores 58.9% at weight 8; the offenders are in audit.json under details.functionShape, fifty at a time.
1203. Long member chain lines: 3165 to zero. Scores 61.7% at weight 4; the offenders are in audit.json under details.prose, fifty at a time.
1204. Deeply indented lines: 3125 to zero. Scores 62.2% at weight 4; the offenders are in audit.json under details.prose, fifty at a time.
1205. Else blocks: 1186 to zero. Scores 80.3% at weight 5; the offenders are in audit.json under details.functionShape, fifty at a time.
1206. Duplication %: 2.34 to zero. Scores 88.3% at weight 8; the offenders are in audit.json under details.duplication, fifty at a time.
1207. Conditions with calls tangled inside calls: 278 to zero. Scores 90.8% at weight 8; the offenders are in audit.json under details.conditions, fifty at a time.
1208. Conditions compared to a raw literal: 246 to zero. Scores 91.8% at weight 6; the offenders are in audit.json under details.conditions, fifty at a time.
1209. Overlong function names: 58 to zero. Scores 92.9% at weight 4; the offenders are in audit.json under details.functionNames, fifty at a time.
1210. Accessor names that want to be a property: 34 to zero. Scores 95.8% at weight 6; the offenders are in audit.json under details.accessorNames, fifty at a time.
1211. Inline magic values: 134 to zero. Scores 97.6% at weight 4; the offenders are in audit.json under details.magicValues, fifty at a time.

## The detail behind the first targets

### `api/Data/JpmsContext.Model.cs` — 557 lines

| Function | Line | Lines | Used by other files | Also declared in |
| --- | --- | --- | --- | --- |
| OnModelCreating | 21 | 536 | — | — |

### `api/Features/Sales/Documents/EstimateDocumentRenderer.cs` — 507 lines

| Function | Line | Lines | Used by other files | Also declared in |
| --- | --- | --- | --- | --- |
| Render | 27 | 38 | `api/Features/Ai/Scans/ScannedPdfReading.cs`, `api/Features/Ai/Tools/AiSourceTools.ReadSource.cs`, `api/Features/Ai/Tools/AiValuationInvoiceTools.cs`, `api/Features/Commercial/Documents/CostCentreReconciliationPdfBuilder.cs` +31 | `api/Features/Ai/Scans/ScannedPdfPages.cs`, `api/Features/Commercial/Documents/CostCentreReconciliationRenderer.cs`, `api/Features/Commercial/Documents/ProjectSupplierAccountRenderer.cs`, `api/Features/Procurement/Documents/WorkOrderPoRenderer.cs` +8 |
| FileName | 68 | 9 | `api/Data/Entities/ArchitectInstructionEntities.cs`, `api/Data/Entities/BuildingControlEntities.cs`, `api/Data/Entities/CoreEntities.cs`, `api/Data/Entities/DocumentControlEntities.cs` +219 | — |
| AddCover | 80 | 35 | — | — |
| AddProjectBlock | 118 | 23 | — | — |
| Labelled | 142 | 12 | `api/Features/Commercial/Documents/ValuationReportSnapshotPdfBuilder.cs` | — |
| CompanyBlock | 155 | 10 | — | — |
| AddNarrative | 168 | 40 | `api/Features/Variations/Documents/VariationDocumentRenderer.cs` | `api/Features/Progress/ContractorsReports/Documents/ContractorsReportPdfRenderer.cs`, `api/Features/Progress/ContractorsReports/Documents/ContractorsReportWordRenderer.cs`, `api/Features/Progress/Documents/ProgressReportRenderer.cs`, `api/Features/Variations/Documents/VariationDocumentSections.cs` |
| Prose | 210 | 25 | `contracts/Ai/PageGuides/PageGuide.cs` | — |
| AddChart | 238 | 50 | — | — |
| AddSections | 291 | 73 | — | — |
| AddGrandTotal | 365 | 51 | — | `contracts/Commercial/Export/ValuationExportPendingSheet.cs` |
| AddContactPage | 419 | 21 | — | — |
| ContactLine | 441 | 10 | `jpms/Pages/SubcontractorDetail.razor`, `jpms/Pages/SubcontractorDetail.razor.cs` | — |
| SectionTitle | 454 | 9 | — | — |
| SubHeading | 464 | 10 | `api/Features/Procurement/Documents/WorkOrderPoRenderer.Scope.cs` | `api/Features/Procurement/Documents/WorkOrderPoRenderer.Helpers.cs` |
| AddColumn | 475 | 5 | `api/Features/Commercial/Documents/CostCentreReconciliationRenderer.Header.cs`, `api/Features/Commercial/Documents/CostCentreReconciliationRenderer.Helpers.cs`, `api/Features/Commercial/Documents/CostCentreReconciliationRenderer.Summary.cs`, `api/Features/Commercial/Documents/ProjectSupplierAccountRenderer.Header.cs` +15 | — |
| Cell | 481 | 7 | `api/Features/Ai/Sources/AiSourceReader.Workbook.cs`, `api/Features/Ai/Tools/AiWeeklyCashflowGridTool.Shape.cs`, `api/Features/Commercial/Documents/CostCentreReconciliationRenderer.Helpers.cs`, `api/Features/Commercial/Documents/ProjectSupplierAccountRenderer.Cells.cs` +14 | `api/Features/Progress/ContractorsReports/Documents/ContractorsReportWordTables.cs` |
| PropertyLine | 489 | 7 | — | — |
| ClientLine | 497 | 6 | — | — |

### `worker/MailboxIntake/Graph/GraphMailClient.cs` — 475 lines

| Function | Line | Lines | Used by other files | Also declared in |
| --- | --- | --- | --- | --- |
| GraphMailClient | 27 | 11 | `worker/Program.cs` | — |
| GetDeltaPageAsync | 41 | 33 | `worker/MailboxIntake/Graph/IGraphMailClient.cs` | `worker/MailboxIntake/Graph/NullGraphMailClient.cs` |
| GetMessageAsync | 75 | 11 | `worker/MailboxIntake/Graph/IGraphMailClient.cs` | `worker/MailboxIntake/Graph/NullGraphMailClient.cs` |
| ListInboxMessageIdentitiesAsync | 87 | 33 | `worker/MailboxIntake/Graph/IGraphMailClient.cs` | `worker/MailboxIntake/Graph/NullGraphMailClient.cs` |
| MoveMessageAsync | 121 | 13 | `worker/MailboxIntake/Graph/IGraphMailClient.cs` | `worker/MailboxIntake/Graph/NullGraphMailClient.cs` |
| GetMessageParentFolderIdAsync | 135 | 11 | `worker/MailboxIntake/Graph/IGraphMailClient.cs` | `worker/MailboxIntake/Graph/NullGraphMailClient.cs` |
| GetFolderIdAsync | 147 | 11 | `worker/MailboxIntake/Graph/IGraphMailClient.cs` | `worker/MailboxIntake/Graph/NullGraphMailClient.cs` |
| FindMessageIdByInternetMessageIdAsync | 159 | 27 | `worker/MailboxIntake/Graph/IGraphMailClient.cs` | `worker/MailboxIntake/Graph/NullGraphMailClient.cs` |
| EnsureFolderAsync | 187 | 38 | `worker/MailboxIntake/Graph/IGraphMailClient.cs` | `worker/MailboxIntake/Graph/NullGraphMailClient.cs` |
| CreateDraftAsync | 232 | 50 | `api/Features/Commercial/Commands/PrepareValuationReportSnapshotEmailDraftHandler.cs`, `api/Features/MailboxIntake/Compose/SendMailboxEmailHandler.Draft.cs`, `api/Features/MailboxIntake/Graph/IMailboxGraphClient.cs`, `api/Features/MailboxIntake/Graph/NullMailboxGraphClient.cs` +8 | `api/Features/MailboxIntake/Graph/MailboxGraphClient.Drafts.cs`, `jpms/Components/SubcontractorStatementModal.razor`, `jpms/Components/ValuationSnapshotEmailModal.razor`, `worker/MailboxIntake/Graph/NullGraphMailClient.cs` |
| CreateSubscriptionAsync | 283 | 15 | `worker/MailboxIntake/Graph/IGraphMailClient.cs` | `worker/MailboxIntake/Graph/NullGraphMailClient.cs` |
| RenewSubscriptionAsync | 299 | 8 | `worker/MailboxIntake/Graph/IGraphMailClient.cs` | `worker/MailboxIntake/Graph/NullGraphMailClient.cs` |
| DeleteSubscriptionAsync | 308 | 6 | `worker/MailboxIntake/Graph/IGraphMailClient.cs` | `worker/MailboxIntake/Graph/NullGraphMailClient.cs` |
| ReadSubscriptionAsync | 315 | 11 | — | — |
| SendAsync | 333 | 59 | `api/Auth/AzureEmailInviteNotifier.cs`, `api/Features/Ai/ClaudeClient.cs`, `api/Features/Ai/Scans/AzureVisionOcr.cs`, `api/Features/Auth/ForgotPasswordEndpoint.cs` +140 | `api/Features/Auth/PasswordResetSender.cs`, `api/Features/Bluebeam/BluebeamClient.Sessions.cs`, `api/Features/MailboxIntake/Graph/MailboxGraphClient.Http.cs`, `jpms/Components/AddManualVariationDialog.razor` +5 |
| RetryDelay | 393 | 17 | — | — |
| SafeReadAsync | 413 | 5 | — | — |
| ParseMessage | 422 | 47 | — | — |
| GraphRequestException | 474 | 1 | — | — |

### `jpms/Services/Navigation/SidebarFolders.cs` — 454 lines

| Function | Line | Lines | Used by other files | Also declared in |
| --- | --- | --- | --- | --- |
| SidebarFolderInfo | 45 | 410 | — | — |

### `jpms/Components/ProjectDetailsEditor.razor` — 440 lines

| Block opens with | Lines | Range | Functions that move with it |
| --- | --- | --- | --- |
| `<div>` | 25 | 41–65 | OnPartyChanged |
| `@if (partySelection.StartsWith(ArchitectPrefix, StringComparison.Ordinal))` | 14 | 67–80 | OnOnBehalfOfClientChanged |
| `<div class="grid grid-cols-1 sm:grid-cols-2 gap-4">` | 22 | 82–103 | OnOrganisationChanged |
| `<FormField Label="Xero contact"` | 24 | 134–157 | OnXeroContactChanged |
| `@if (CanDelete)` | 39 | 159–197 | DisarmDelete, DeleteAsync |

| Function | Line | Lines | Used by other files | Also declared in |
| --- | --- | --- | --- | --- |
| Open | 262 | 31 | `api/Auth/InviteEmailBody.cs`, `api/Auth/PasswordResetEmailBody.cs`, `api/Data/Entities/CommercialEntities.cs`, `api/Data/Entities/TodoEntities.cs` +189 | `api/Features/Progress/Photos/ProgressPhotoPreparation.cs`, `jpms/Components/NextValuationDateEditor.razor`, `jpms/Components/PartyContactsEditor.razor`, `jpms/Components/RequestTable.razor` +13 |
| LoadPartiesAsync | 294 | 6 | — | — |
| LoadXeroContactsAsync | 305 | 21 | — | — |
| OnXeroContactChanged | 327 | 8 | — | — |
| OnPartyChanged | 336 | 5 | — | — |
| OnOnBehalfOfClientChanged | 342 | 10 | — | — |
| DisarmDelete | 355 | 5 | — | — |
| DeleteAsync | 361 | 20 | `api/Features/ArchitectInstructions/ArchitectInstructionHandlers.cs`, `api/Features/BuildingControl/Attachments/BuildingControlAttachmentHandlers.cs`, `api/Features/BuildingControl/Attachments/IBuildingControlAttachmentStore.cs`, `api/Features/DocumentControl/Storage/IDocumentControlBlobStore.cs` +45 | `api/Features/ArchitectInstructions/Storage/ArchitectInstructionBlobStore.cs`, `api/Features/BuildingControl/Attachments/AzureBlobBuildingControlAttachmentStore.cs`, `api/Features/DocumentControl/Storage/AzureBlobDocumentControlStore.cs`, `api/Features/Drawings/Storage/AzureBlobDrawingStore.cs` +21 |
| OnOrganisationChanged | 382 | 58 | — | `jpms/Components/NewProjectForm.razor` |

### `jpms/Services/HttpLabourStore.cs` — 436 lines

| Function | Line | Lines | Used by other files | Also declared in |
| --- | --- | --- | --- | --- |
| HttpLabourStore | 32 | 28 | `jpms/Program.cs` | — |
| Workers | 63 | 5 | `api/Data/Entities/LabourEntities.cs`, `api/Data/JpmsContext.cs`, `api/Features/Ai/Tools/Actions/LabourAndBackOfficeActions.MonthEnd.cs`, `api/Features/Ai/Tools/Actions/LabourAndBackOfficeActions.Timesheets.cs` +77 | — |
| LoadWorkersAsync | 69 | 5 | — | — |
| AddWorkerAsync | 79 | 8 | `jpms/Pages/Workers.razor.cs`, `jpms/Services/ILabourStore.cs` | — |
| UpdateWorkerAsync | 88 | 8 | `jpms/Pages/Workers.razor.cs`, `jpms/Services/ILabourStore.cs` | — |
| SetWorkerSettlementIdentityAsync | 97 | 7 | `jpms/Pages/Workers.razor.cs`, `jpms/Pages/XeroAllocation.Settlement.cs`, `jpms/Services/ILabourStore.cs` | — |
| ReconcileWorkerLinksAsync | 105 | 6 | `jpms/Pages/Workers.razor.cs`, `jpms/Services/ILabourStore.cs` | — |
| DismissChaseDayAsync | 112 | 8 | `jpms/Features/Labour/ChaseListPanel.razor`, `jpms/Services/ILabourStore.cs` | — |
| AssignmentsFor | 121 | 5 | `jpms/Pages/ProjectLabour.Settlement.cs`, `jpms/Pages/ProjectLabour.razor`, `jpms/Services/ILabourStore.cs` | — |
| SetAssignmentAsync | 131 | 5 | `jpms/Pages/ProjectLabour.Settlement.cs`, `jpms/Services/ILabourStore.cs` | — |
| MyDay | 137 | 5 | `jpms/Components/MyDayWorkspace.razor`, `jpms/Services/ILabourStore.cs` | — |
| LoadMyDayAsync | 143 | 5 | — | — |
| MySignInAsync | 151 | 5 | `jpms/Components/MyDayWorkspace.razor`, `jpms/Services/ILabourStore.cs` | — |
| MySignOutAsync | 157 | 5 | `jpms/Components/MyDayWorkspace.razor`, `jpms/Services/ILabourStore.cs` | — |
| MyResubmitAsync | 163 | 5 | `jpms/Components/MyDayWorkspace.razor`, `jpms/Services/ILabourStore.cs` | — |
| TimesheetsFor | 169 | 5 | `jpms/Pages/ProjectLabour.Approval.cs`, `jpms/Services/ICommercialStore.cs`, `jpms/Services/ILabourStore.cs` | `jpms/Services/HttpCommercialStore.Resourcing.cs` |
| AttendanceFor | 179 | 5 | `jpms/Features/Progress/ContractorsReports/ContractorsReportAttendanceTable.razor`, `jpms/Features/Progress/ContractorsReports/ContractorsReportDraft.cs`, `jpms/Pages/ProjectLabour.Settlement.cs`, `jpms/Pages/ProjectLabour.razor` +2 | `jpms/Services/HttpHsRegister.cs` |
| AddWorkerTimesheetAsync | 189 | 6 | `jpms/Pages/ProjectLabour.ManualEntry.cs`, `jpms/Services/ILabourStore.cs` | — |
| SubmitWorkerWeekAsync | 196 | 11 | `jpms/Features/Labour/WeekEntryModal.razor.cs`, `jpms/Services/ILabourStore.cs` | — |
| AdjustTimesheetAsync | 208 | 6 | `jpms/Pages/ProjectLabour.Approval.cs`, `jpms/Services/ILabourStore.cs` | — |
| ApproveTimesheetsAsync | 215 | 8 | `jpms/Pages/ProjectLabour.Approval.cs`, `jpms/Services/ILabourStore.cs` | — |
| RejectTimesheetAsync | 224 | 6 | `jpms/Pages/ProjectLabour.Approval.cs`, `jpms/Services/ILabourStore.cs` | — |
| UnapproveTimesheetAsync | 231 | 8 | `jpms/Pages/ProjectLabour.Corrections.cs`, `jpms/Services/ILabourStore.cs` | — |
| MoveTimesheetAsync | 240 | 12 | `jpms/Pages/ProjectLabour.Corrections.cs`, `jpms/Services/ILabourStore.cs` | — |
| Overview | 253 | 6 | `api/Data/Entities/LabourPlanningEntities.cs`, `api/Data/Entities/RegisterEntities.cs`, `api/Data/Entities/TodoEntities.cs`, `api/Features/Labour/Commands/XeroCoding/RunXeroCodingEndpoint.cs` +22 | — |
| SetWorkerContractAsync | 264 | 5 | `jpms/Features/Labour/WorkerDetailPanel.razor`, `jpms/Services/ILabourStore.cs` | — |
| SetWorkerCisStatusAsync | 270 | 5 | `jpms/Features/Labour/WorkerDetailPanel.razor`, `jpms/Services/ILabourStore.cs` | — |
| RecordAbsenceAsync | 276 | 5 | `jpms/Services/ILabourStore.cs` | — |
| RecordAbsenceRangeAsync | 282 | 24 | `jpms/Features/Labour/AbsenceModal.razor`, `jpms/Services/ILabourStore.cs` | — |
| RemoveAbsenceAsync | 307 | 5 | `jpms/Services/ILabourStore.cs` | — |
| SignOffWeekAsync | 315 | 5 | `jpms/Pages/LabourOverview.razor.cs`, `jpms/Services/ILabourStore.cs` | — |
| RemoveWeekSignOffAsync | 321 | 5 | `jpms/Pages/LabourOverview.razor.cs`, `jpms/Services/ILabourStore.cs` | — |
| MonthStartOf | 327 | 9 | `api/Features/Labour/Commands/TimesheetCorrectionGuards.cs`, `api/Features/Labour/Commands/WeekSignOffSlices.cs`, `contracts/Labour/ForecastRules.cs` | — |
| AddSettlementLineAsync | 341 | 5 | `jpms/Features/Labour/SettlementLineModal.razor`, `jpms/Services/ILabourStore.cs` | — |
| RemoveSettlementLineAsync | 347 | 5 | `jpms/Pages/LabourOverview.razor`, `jpms/Services/ILabourStore.cs` | `jpms/Pages/LabourOverview.Settlement.cs` |
| XeroMappings | 353 | 5 | `jpms/Pages/LabourXeroMapping.razor`, `jpms/Services/ILabourStore.cs` | — |
| LoadMappingsAsync | 359 | 5 | — | — |
| SetSiteXeroMappingAsync | 367 | 5 | `jpms/Pages/LabourXeroMapping.razor`, `jpms/Services/ILabourStore.cs` | — |
| SetCostCodeXeroMappingAsync | 373 | 5 | `jpms/Pages/LabourXeroMapping.razor`, `jpms/Services/ILabourStore.cs` | — |
| RunXeroCodingAsync | 379 | 7 | `jpms/Pages/LabourOverview.Settlement.cs`, `jpms/Services/ILabourStore.cs` | — |
| ResetXeroCodingOutcomeAsync | 387 | 5 | `jpms/Features/Labour/CodingResetModal.razor`, `jpms/Services/ILabourStore.cs` | — |
| ApproveLabourBillAsync | 393 | 6 | `jpms/Features/Labour/LabourBillApproveDialog.razor`, `jpms/Services/ILabourStore.cs` | — |
| SettlementFor | 400 | 5 | `jpms/Pages/ProjectLabour.Settlement.cs`, `jpms/Pages/ProjectLabour.razor`, `jpms/Services/ICloseoutStore.cs`, `jpms/Services/ILabourStore.cs` | `jpms/Services/HttpCloseoutStore.cs` |
| SetTimesheetCoverAsync | 410 | 5 | `jpms/Pages/ProjectLabour.Settlement.cs`, `jpms/Services/ILabourStore.cs` | — |
| SetTimesheetCoverForMonthAsync | 421 | 9 | `jpms/Pages/XeroAllocation.Settlement.cs`, `jpms/Services/ILabourStore.cs` | — |
| LoadAsync | 431 | 5 | `api/Features/Ai/Tools/AiValuationInvoiceTools.cs`, `api/Features/Commercial/Documents/ValuationReportSnapshotPdfBuilder.cs`, `api/Features/MailboxIntake/Graph/MailboxGraphClient.Tagging.cs`, `api/Features/Progress/ContractorsReports/Documents/ContractorsReportBuilder.cs` +14 | `api/Features/Ai/Tools/AiSourceTools.Opening.cs`, `api/Features/Progress/ContractorsReports/Documents/ContractorsReportPhotoLoader.cs`, `api/Features/Site/Drafts/ProgrammeDraftClaimCentres.cs`, `jpms/Components/MyTodosPanel.razor` +59 |

### `jpms/Pages/XeroAllocation.razor` — 435 lines

| Block opens with | Lines | Range | Functions that move with it |
| --- | --- | --- | --- |
| `<div class="mb-4 flex items-center justify-between gap-4 flex-wrap">` | 38 | 66–103 | — |
| `@if (activeTab == XeroAllocationStatus.Unallocated && workOrderBillsTab)` | 16 | 162–177 | — |
| `@if (activeTab == XeroAllocationStatus.Unallocated && labourTab)` | 19 | 191–209 | — |
| `<QueueLineRow @key="line.XeroLedgerLineId"` | 21 | 212–232 | — |
| `<DisputedLineRow @key="line.XeroLedgerLineId"` | 14 | 235–248 | — |
| `<AllocatedSummaryRow @key="line.XeroLedgerLineId"` | 14 | 251–264 | — |
| `<div class="mt-3 flex items-center justify-between gap-4 flex-wrap">` | 22 | 282–303 | — |
| `<Modal IsOpen="@(splitLine is not null && viewLine is null)" Title="Split across` | 14 | 311–324 | — |
| `<SendLinesModal IsOpen="@sendToCostCentreOpen" Title="Send to cost centre"` | 13 | 330–342 | — |
| `<SendLinesModal IsOpen="@sendToProjectOpen" Title="Send to project"` | 13 | 346–358 | — |
| `<Modal IsOpen="@(viewLine is not null)" Title="Invoice document" ShowFooter="fal` | 52 | 382–433 | — |

### `jpms/Pages/AdminKpis.razor` — 429 lines

| Block opens with | Lines | Range | Functions that move with it |
| --- | --- | --- | --- |
| `@if (personFilter == "")` | 14 | 88–101 | — |
| `<RecordsTable Dense="true" MaxHeight="">` | 66 | 102–167 | OpenInControlCentre, StartEdit, StartRemove, ConfirmRemoveAsync |
| `<Modal IsOpen="editing is not null"` | 27 | 178–204 | SaveEditAsync |
| `<Modal IsOpen="addingPerson"` | 24 | 206–229 | AddPersonAsync |

| Function | Line | Lines | Used by other files | Also declared in |
| --- | --- | --- | --- | --- |
| OpenInControlCentre | 271 | 9 | — | — |
| StartAddPerson | 281 | 7 | — | — |
| AddPersonAsync | 289 | 16 | — | — |
| StartEdit | 306 | 10 | `jpms/Components/ValuationReportTable.razor`, `jpms/Pages/ProjectLabour.razor`, `jpms/Pages/Workers.razor` | `jpms/Components/ValuationReportTable.PercentEditing.cs`, `jpms/Pages/ProjectInventory.razor`, `jpms/Pages/ProjectLabour.Approval.cs`, `jpms/Pages/ProjectSiteInstructions.razor` +2 |
| SaveEditAsync | 317 | 19 | `jpms/Components/ValuationInvoicesSection.razor`, `jpms/Pages/CostCodes.razor`, `jpms/Pages/ProjectDefectDetail.razor`, `jpms/Pages/ProjectLabour.razor` | `jpms/Components/ValuationInvoicesSection.Forms.cs`, `jpms/Pages/Architects.razor`, `jpms/Pages/Clients.razor`, `jpms/Pages/CostCodes.razor.cs` +2 |
| StartRemove | 337 | 6 | — | — |
| ConfirmRemoveAsync | 344 | 18 | — | — |
| OnInitializedAsync | 363 | 19 | `jpms/App.razor`, `jpms/Components/ExpiringDocumentsPanel.razor`, `jpms/Components/PackageReconciliationSection.razor.cs`, `jpms/Components/ProjectCorrespondencePanel.razor.cs` +22 | `jpms/Components/AdminKpiPanel.razor`, `jpms/Components/ApprovedSessionGate.razor`, `jpms/Components/ClientCostReferencesModal.razor`, `jpms/Components/MyTodosPanel.razor` +103 |
| OnSessionChanged | 383 | 11 | `jpms/Components/RoleOverridePrompt.razor` | `jpms/Pages/AdminIntegrations.razor`, `jpms/Pages/AdminSystem.razor`, `jpms/Pages/AdminTrades.razor` |
| FetchAsync | 395 | 14 | `jpms/Services/HttpPortalStore.cs` | `jpms/Pages/AdminTrades.razor` |
| RevalidateAsync | 410 | 5 | — | `jpms/Pages/AdminRevokedUsers.razor`, `jpms/Pages/AdminSystem.razor`, `jpms/Pages/AdminTrades.razor`, `jpms/Pages/AdminUsers.razor` |
| FetchAndRepaintAsync | 416 | 5 | — | `jpms/Pages/AdminTrades.razor` |
| Dispose | 422 | 7 | `api/Features/Bluebeam/BluebeamClient.cs`, `api/Features/Progress/WhatsApp/WhatsAppExportArchive.cs`, `jpms/App.razor`, `jpms/Components/AdminKpiPanel.razor` +69 | `jpms/Components/AdminHome.razor`, `jpms/Components/AdminStatsRow.razor`, `jpms/Components/ClaimProgressDialog.razor`, `jpms/Components/ExpiringDocumentsPanel.razor` +53 |

### `jpms/Components/ManualWorkOrderModal.razor.cs` — 428 lines

| Function | Line | Lines | Used by other files | Also declared in |
| --- | --- | --- | --- | --- |
| Dispose | 75 | 3 | `api/Features/Bluebeam/BluebeamClient.cs`, `api/Features/Progress/WhatsApp/WhatsAppExportArchive.cs`, `jpms/App.razor`, `jpms/Components/AdminKpiPanel.razor` +69 | `jpms/Components/AdminHome.razor`, `jpms/Components/AdminStatsRow.razor`, `jpms/Components/ClaimProgressDialog.razor`, `jpms/Components/ExpiringDocumentsPanel.razor` +53 |
| OnParametersSetAsync | 79 | 37 | `jpms/Components/RequestConversation.razor.cs`, `jpms/Features/Triage/Panels/RecordLinkSection.razor` | `jpms/Components/CostCentreCostOfSalesModal.razor.cs`, `jpms/Components/CostCentreReconciliationModal.razor`, `jpms/Components/DrawingExtractionPanel.razor`, `jpms/Components/ReconciliationPackageBuilderModal.razor.cs` +39 |
| ToggleSalesLine | 146 | 7 | `jpms/Components/ManualWorkOrderModal.razor` | — |
| SaveAsync | 188 | 137 | `api/Features/Procurement/Attachments/CompanyTenderTermsEndpoints.cs`, `api/Features/Sales/Imagine/ImaginePublicService.Submit.cs`, `api/Features/Sales/Imagine/ImagineRenderRunner.cs`, `jpms/Components/ApprovedUserRow.razor` +25 | `api/Features/Procurement/Attachments/CompanyTenderTermsStore.cs`, `api/Features/Sales/Imagine/IImagineImageStore.cs`, `jpms/Components/ClientCostReferencesModal.razor`, `jpms/Components/DrawingDetailsEditor.razor` +38 |
| ConfirmSaleWarningAsync | 326 | 6 | `jpms/Components/ManualWorkOrderModal.razor` | — |
| FindUncoveredCostCentres | 338 | 12 | — | — |
| OnAttachmentFilesSelected | 351 | 5 | `jpms/Components/ManualWorkOrderModal.razor`, `jpms/Pages/ProjectBidPackageInviteDetail.razor` | `jpms/Pages/ProjectBidPackageInviteDetail.Documents.cs` |
| LoadExistingAttachmentsAsync | 357 | 6 | — | — |
| RemoveExistingAttachmentAsync | 364 | 12 | `jpms/Components/ManualWorkOrderModal.razor` | — |
| UploadStagedAttachmentsAsync | 379 | 21 | — | — |
| FormatBytes | 401 | 2 | `jpms/Components/ManualWorkOrderModal.razor` | — |
| Ref | 407 | 11 | `api/Data/Entities/RequestItemEntity.cs`, `api/Features/Ai/Tools/AiToolCatalogue.TodoBrief.cs`, `api/Features/Requests/Documents/RequestDocumentModel.cs`, `api/Features/Todos/TodoCompletionRecordTagger.cs` +21 | — |
| DescriptionFor | 420 | 9 | `jpms/Components/CostCentreReconciliationModal.razor`, `jpms/Components/CostCentreSalesLinesModal.razor`, `jpms/Components/ManualWorkOrderModal.razor` | `api/Features/ValuationInvoices/XeroRaise/ValuationInvoiceXeroRaisePlanner.cs`, `contracts/Commercial/Export/ValuationExportBillRows.cs`, `jpms/Components/CostCentreSalesLinesModal.razor.cs` |

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
