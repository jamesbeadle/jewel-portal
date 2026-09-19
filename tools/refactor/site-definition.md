# Site definition

Written by the code quality check on 2026-09-19 from the views themselves — never by hand, so it is never stale. Each route is what a user sees there, in the widget notation; each component of the site is defined the same way.

**116 routes, 525 components of the site, 34 catalogue widgets.** 1,669 widget usages and 669 pieces of markup written by hand where a widget should be, in 293 views.

**Notation.** `a, b` stacked top to bottom · `[ a b ]` side by side · `(3) a` three of them, `(n) a` one per item · `?when: a` shown on a condition · `( a | b )` one or the other · `Widget{ … }` a catalogue widget with its content · `Part=( … )` one of the site's own components opened out · `⚠table→RecordsTable` markup written by hand where the named widget should be.

## Written by hand where a widget should be

| Should be | Written by hand | Where |
| --- | --- | --- |
| `Button` | 302 in 169 files | `jpms/Components/ApprovedUserRow.razor`, `jpms/Components/ApprovedUsersPanel.razor`, `jpms/Components/ClaimProgressDialog.razor`, `jpms/Components/ConversationComposer.razor`, `jpms/Components/ConversationMessageCard.razor`, `jpms/Components/DirectoryContactForm.razor` +163 |
| `FormField` | 144 in 96 files | `jpms/Components/ApprovedUserRow.razor`, `jpms/Components/ClaimProgressDialog.razor`, `jpms/Components/ConversationComposer.razor`, `jpms/Components/DirectoryContactForm.razor`, `jpms/Components/DrawingFolderPicker.razor`, `jpms/Components/DrawingRevisionLabelEditor.razor` +90 |
| `SectionHeader` | 126 in 88 files | `jpms/Components/AdminKpiPanel.razor`, `jpms/Components/ApprovedUsersPanel.razor`, `jpms/Components/ClientDefectForm.razor`, `jpms/Components/CostCentreReconciliationModal.razor`, `jpms/Components/DrawingDetailsEditor.razor`, `jpms/Components/DrawingRevisionList.razor` +82 |
| `RecordsTable` | 67 in 57 files | `jpms/Components/ClientCostReferencesModal.razor`, `jpms/Components/CostCentreCostOfSalesModal.razor`, `jpms/Components/CostCentreReconciliationModal.razor`, `jpms/Components/CostCentreSalesLinesModal.razor`, `jpms/Components/CostCentreWorkOrdersModal.razor`, `jpms/Components/DrawingExtractionPanel.razor` +51 |
| `KeyValueList` | 17 in 17 files | `jpms/Components/DrawingExtractionPanel.razor`, `jpms/Components/ProjectContractPanel.razor`, `jpms/Components/PurchaseOrderSheet.razor`, `jpms/Components/RecordEmailPreviewPanel.razor`, `jpms/Components/RequestAccessView.razor`, `jpms/Components/ValuationStatementViewer.razor` +11 |
| `PageHeader` | 13 in 10 files | `jpms/App.razor`, `jpms/Components/ProjectDetailView.razor`, `jpms/Components/PurchaseOrderSheet.razor`, `jpms/Components/RequestAccessView.razor`, `jpms/Features/Xero/Allocation/AllocationPageHeader.razor`, `jpms/Pages/ConnectAuthorize.razor` +4 |

## Routes

### `/`
`jpms/Pages/Login.razor`

```
JewelIcon, ⚠h1→PageHeader, (2) ⚠input→FormField
```

### `/admin/ai-actions`
`jpms/Pages/AiActionsAdmin.razor`

```
Page{ PageHeader, LoadGate{ ( ⚠input→FormField, ?OrphanedAttachments.Count > 0: (n) ⚠button→Button, (n) ( ⚠button→Button, ?IsExpanded(area): (n) ⚠button→Button ) ) } }
```

### `/admin/integrations`
`jpms/Pages/AdminIntegrations.razor`

```
Page{ ( WorkspaceSectionNav, PageHeader, Panel ), ( ?status.LastRefreshError is { Le…: Notice, ?!confirmingDisconnect: ⚠button→Button ), ?actionError is not null: Notice }
```

### `/admin/kpis`
`jpms/Pages/AdminKpis.razor`

```
Page{ ( WorkspaceSectionNav, PageHeader, Panel{ SearchSelect, ?actionError is not null: Notice, ?actionNote is not null: Notice, ( ?personFilter == "": (n) ⚠button→Button, RecordsTable ) }, Modal{ ?editing is not null: ( ?formError is not null: Notice, FormField{ KpiPersonPicker=( ( ?!Ready: ⚠select→FormField | SearchSelect ), ?key == NewKey: ⚠input→FormField ) }, ⚠textarea→FormField ) }, Modal{ ?formError is not null: Notice, FormField } ) }
```

### `/admin/skills`
`jpms/Pages/AiSkillsAdmin.razor`

```
Page{ PageHeader, ( ?editing is null: LoadGate{ ( ?skills!.Count == 0: EmptyState | ⚠table→RecordsTable ) } | ( [ ⚠input→FormField ⚠select→FormField ], ⚠input→FormField, (2) ⚠textarea→FormField, ?!isNew: ( ⚠h2→SectionHeader, ?references.Count > 0: (n) ⚠button→Button, ( [ (2) ⚠input→FormField ], ⚠input→FormField, ⚠textarea→FormField ) ) ) ) }
```

### `/admin/system`
`jpms/Pages/AdminSystem.razor`

```
Page{ ( WorkspaceSectionNav, PageHeader, LoadGate{ (2) Panel }, ?terms.Configured: InputFile ) }
```

### `/admin/trades`
`jpms/Pages/AdminTrades.razor`

```
Page{ ( WorkspaceSectionNav, PageHeader, Panel{ ⚠input→FormField, ?actionError is not null: Notice, ?actionNote is not null: Notice, ( ?TradesModel.Current is { Count:…: EmptyState | RecordsTable{ (n) ?renamingId == trade.TradeId: ⚠input→FormField } ) } ) }
```

### `/admin/users`
`jpms/Pages/AdminUsers.razor`

```
Page{ ( WorkspaceSectionNav, PageHeader, ApprovedUsersPanel=( [ ⚠h2→SectionHeader ⚠button→Button ], ?showForm: InviteUserForm=( ( ?outcome?.Success == true && out…: ( ⚠input→FormField, [ (2) ⚠button→Button ] ) | (2) FormField ) ), LoadGate{ (n) ApprovedUserRow=( (2) ⚠button→Button, [ (n) ?CanEdit: ⚠button→Button ?CanEdit && AddableRoles.Count >…: ⚠select→FormField ?busy: JewelIcon ], ?resetLink is not null: ⚠input→FormField ) } ) ) }
```

### `/admin/users/revoked`
`jpms/Pages/AdminRevokedUsers.razor`

```
Page{ ( WorkspaceSectionNav, PageHeader, RevokedUsersPanel=( ⚠h2→SectionHeader, LoadGate{ (n) RevokedUserRow=( [ ?busy: JewelIcon (2) ⚠button→Button ], Modal ) } ) ) }
```

### `/agents/activity`
`jpms/Pages/AgentActivityLog.razor`

```
Page{ PageHeader, [ (2) ⚠button→Button ], LoadGate{ ⚠table→RecordsTable } }
```

### `/architects`
`jpms/Pages/Architects.razor`

```
Page{ PageHeader{ ExportToExcelButton }, ?loadError is not null: Notice, LoadGate{ ( ?architects.Count == 0: EmptyState | RecordsTable{ (n) (2) ⚠button→Button } ) }, PartyContactsEditor=( Modal{ ?error is not null: Notice, ( ?contacts.Count == 0: EmptyState | ⚠table→RecordsTable ), [ (4) FormField ] } ), (2) Modal{ ?formError is not null: Notice, FormField, [ (2) FormField ] } }
```

### `/audit`
`jpms/Pages/AuditTrail.razor`

```
Page{ ( ?!CanAccess: PageHeader | ( PageHeader, [ ⚠button→Button (n) ⚠button→Button (2) ⚠select→FormField ], ?loadError is not null: Notice, LoadGate{ RecordsTable{ (n) ?!string.IsNullOrEmpty(item.Path…: Pill } } ) ) }
```

### `/client`
`jpms/Pages/ClientPortalHome.razor`

```
Page{ ( ?!CanAccess: PageHeader | ?!HasLinkedRecord: PageHeader | ( PageHeader, ?loadError is not null: Notice, LoadGate{ ⚠h2→SectionHeader, ClientRequestList=( ( ?Requests.Count == 0: EmptyState | RecordsTable ) ), ⚠h2→SectionHeader, ClientVariationList=( ( ?Orders.Count == 0: EmptyState | RecordsTable ) ) } ) ) }
```

### `/client/requests/{RequestId}`
`jpms/Pages/ClientRequestView.razor`

```
Page{ LoadGate{ ( ?record is null: PageHeader | ( PageHeader, ?!string.IsNullOrWhiteSpace(reco…: Panel, ?!string.IsNullOrWhiteSpace(reco…: Panel, ClientConversationPanel=( Panel{ LoadGate{ (n) ConversationMessageCard=( ?ShowVisibility && IsInternal: Pill, ?CanReply: ⚠button→Button, ?Entry.Replies.Count > 0: (n) ConversationMessageCard ), ConversationComposer=( ?error is not null: Notice, ?ReplyingToAuthor is not null: ⚠button→Button, ⚠textarea→FormField ) } } ) ) ) } }
```

### `/client/variations/{VariationOrderId}`
`jpms/Pages/ClientVariationView.razor`

```
Page{ LoadGate{ ( ?record is null: PageHeader | ( PageHeader, ?!string.IsNullOrWhiteSpace(reco…: Panel, ClientConversationPanel=( Panel{ LoadGate{ (n) ConversationMessageCard=( ?ShowVisibility && IsInternal: Pill, ?CanReply: ⚠button→Button, ?Entry.Replies.Count > 0: (n) ConversationMessageCard ), ConversationComposer=( ?error is not null: Notice, ?ReplyingToAuthor is not null: ⚠button→Button, ⚠textarea→FormField ) } } ) ) ) } }
```

### `/clients`
`jpms/Pages/Clients.razor`

```
Page{ PageHeader{ ExportToExcelButton }, ?loadError is not null: Notice, LoadGate{ ( ?clients.Count == 0: EmptyState | RecordsTable{ (n) (3) ⚠button→Button } ) }, PartyContactsEditor=( Modal{ ?error is not null: Notice, ( ?contacts.Count == 0: EmptyState | ⚠table→RecordsTable ), [ (4) FormField ] } ), Modal{ ?formError is not null: Notice, FormField, [ (2) FormField ] }, Modal{ ?inviteError is not null: Notice, ?inviteResult is not null: Notice }, Modal{ ?formError is not null: Notice, FormField, [ (2) FormField ] } }
```

### `/connect/authorize`
`jpms/Pages/ConnectAuthorize.razor`

```
( JewelIcon, LoadGate{ ( ⚠h1→PageHeader, ?approveError is not null: Notice ) } )
```

### `/control-centre`
`jpms/Pages/TriageQueue.razor`

```
Page{ ( LoadGate{ ( ?view == QueueView.Active && sel…: TriageBar=( [ ProjectSelect=( DropdownMenu{ ?AllowNone: ⚠button→Button } ) ( (2) TriageDecisionRow=( [ (2) ⚠button→Button ] ), ?ThreadTags is { Count: > 0 } in…: TriageDecisionRow=( [ (2) ⚠button→Button ] ) ) ], ?ActionError is not null: Notice, ?DiscardArmed: Notice, ⚠button→Button ) | ?view == QueueView.Active && que…: OutboxOnlyBar=( ?ActionError is not null: Notice ) ), PanelWorkspace=( (2) PanelRail ){ EmailMirrorPane=( TriageMessageDetail=( ⚠h2→SectionHeader, ?Detail is not null && Detail.At…: ( ?ShowDocumentTriageTick && Ticka…: ⚠button→Button, (n) ?!string.IsNullOrEmpty(attachmen…: ?TriageEmailDisplay.IsPreviewabl…: ⚠button→Button ), ?SentReplies.Count > 0: (n) ⚠button→Button, ⚠button→Button, (n) ⚠button→Button ) ), RecordExplorerPane=( ( ?openRecord is { } record: RecordDocumentView=( ⚠button→Button, ⚠h2→SectionHeader, ?StatusLabel is { Length: > 0 } …: Pill, LoadGate, ?Record.Type == RecordType.Reque…: RecordAttachmentList=( ?items is { Count: > 0 } attachm…: (n) ?PreviewFor(attachment) is { } p…: ⚠button→Button ), RecordEmailList=( ?emails.Count > 0: CorrespondenceThreadList ) ) | ( [ (2) ⚠select→FormField ], SearchInput, LoadGate{ (n) ⚠button→Button } ) ) ), PreviewPane=( ( ⚠button→Button, ( ?Request.IsPdf: PdfViewer=( Toolbar{ ?pageCount > 1: ( (2) ToolbarButton, ToolbarDivider ), (4) ToolbarButton, ToolbarDivider, (2) ToolbarButton, ?DownloadUrl is not null: ( ToolbarDivider, ToolbarButton ) }, LoadGate ) | ImageViewer=( Toolbar{ (5) ToolbarButton, ToolbarDivider, (2) ToolbarButton, ?DownloadUrl is not null: ( ToolbarDivider, ToolbarButton ) }, LoadGate ) ) ) ), XeroExplorerPane=( ?openTransaction is { } transact…: XeroTransactionView=( ⚠button→Button, ⚠h2→SectionHeader, ?Transaction.HasAttachments: (n) ?IsPreviewable(attachment): ⚠button→Button ), ( ?loadFailed: Notice | LoadGate ), ?snapshot.Error is { } error: Notice, [ SearchInput ⚠select→FormField ], ( ?FilteredTransactions.Count == 0: EmptyState | (n) ⚠button→Button ) ), NewEmailComposerPane=( ⚠h2→SectionHeader, ?error is not null: Notice, FormField{ RecipientInput=( [ (n) ?!Disabled: ⚠button→Button ⚠input→FormField ], ?isOpen && suggestions.Count > 0: (n) ⚠button→Button ) }, [ (2) FormField{ RecipientInput=( [ (n) ?!Disabled: ⚠button→Button ⚠input→FormField ], ?isOpen && suggestions.Count > 0: (n) ⚠button→Button ) } ], FormField, FormField{ RichTextEditor }, AttachmentPicker=( ?Attachments.Count > 0: (n) ⚠button→Button, [ InputFile (3) ⚠button→Button ?OriginalAttachments is { Count:…: ⚠button→Button ], ?openPanel != Panel.None: ( ?openPanel is Panel.Drawings or …: FormField, ( ?openPanel == Panel.Drawings: ( ⚠input→FormField, (n) ?folderGroup.Folder is not null …: ⚠button→Button, ?SelectedDrawingCount > 0 && !at…: ⚠button→Button ) | ?openPanel == Panel.Photos: (n) (n) ⚠button→Button | ?openPanel == Panel.Records: ( ⚠input→FormField, (n) ⚠button→Button ) ) ) ), ?fileToRecord: [ (3) ⚠select→FormField ] ), OutboxPane=( ⚠h2→SectionHeader, ?ComposeAnchor is { } anchor: MailReplyComposer=( ⚠button→Button, ?error is not null: Notice, FormField{ RecipientInput=( [ (n) ?!Disabled: ⚠button→Button ⚠input→FormField ], ?isOpen && suggestions.Count > 0: (n) ⚠button→Button ) }, ?!showBcc: ⚠button→Button, RecipientInput=( [ (n) ?!Disabled: ⚠button→Button ⚠input→FormField ], ?isOpen && suggestions.Count > 0: (n) ⚠button→Button ), ?showBcc: FormField{ RecipientInput=( [ (n) ?!Disabled: ⚠button→Button ⚠input→FormField ], ?isOpen && suggestions.Count > 0: (n) ⚠button→Button ) }, FormField, RichTextEditor, AttachmentPicker=( ?Attachments.Count > 0: (n) ⚠button→Button, [ InputFile (3) ⚠button→Button ?OriginalAttachments is { Count:…: ⚠button→Button ], ?openPanel != Panel.None: ( ?openPanel is Panel.Drawings or …: FormField, ( ?openPanel == Panel.Drawings: ( ⚠input→FormField, (n) ?folderGroup.Folder is not null …: ⚠button→Button, ?SelectedDrawingCount > 0 && !at…: ⚠button→Button ) | ?openPanel == Panel.Photos: (n) (n) ⚠button→Button | ?openPanel == Panel.Records: ( ⚠input→FormField, (n) ⚠button→Button ) ) ) ) ), ?CurrentReplyPending || QueuedRe…: ( ?CurrentReplyPending: [ (2) ⚠button→Button ], (n) ( [ ?!isEditing: ⚠button→Button ⚠button→Button ], ?WorkflowTags(entry) is { Count:…: (n) Pill, ?isEditing: MailReplyComposer=( ⚠button→Button, ?error is not null: Notice, FormField{ RecipientInput=( [ (n) ?!Disabled: ⚠button→Button ⚠input→FormField ], ?isOpen && suggestions.Count > 0: (n) ⚠button→Button ) }, ?!showBcc: ⚠button→Button, RecipientInput=( [ (n) ?!Disabled: ⚠button→Button ⚠input→FormField ], ?isOpen && suggestions.Count > 0: (n) ⚠button→Button ), ?showBcc: FormField{ RecipientInput=( [ (n) ?!Disabled: ⚠button→Button ⚠input→FormField ], ?isOpen && suggestions.Count > 0: (n) ⚠button→Button ) }, FormField, RichTextEditor, AttachmentPicker=( ?Attachments.Count > 0: (n) ⚠button→Button, [ InputFile (3) ⚠button→Button ?OriginalAttachments is { Count:…: ⚠button→Button ], ?openPanel != Panel.None: ( ?openPanel is Panel.Drawings or …: FormField, ( ?openPanel == Panel.Drawings: ( ⚠input→FormField, (n) ?folderGroup.Folder is not null …: ⚠button→Button, ?SelectedDrawingCount > 0 && !at…: ⚠button→Button ) | ?openPanel == Panel.Photos: (n) (n) ⚠button→Button | ?openPanel == Panel.Records: ( ⚠input→FormField, (n) ⚠button→Button ) ) ) ) ) ) ) ) } }, ?RecentTriage.Count > 0: RecentTriageFold=( ⚠h2→SectionHeader ) ) }
```

### `/cost-codes`
`jpms/Pages/CostCodes.razor`

```
Page{ WorkspaceSectionNav, PageHeader{ Toolbar{ ToolbarButton } }, [ (3) ⚠button→Button ], ?loadError is not null: Notice, ( ?activeTab == CostCodesTab.Ours: LoadGate{ ( ?costCodes.Count == 0: EmptyState | ( ExportToExcelButton, RecordsTable{ (n) ( ( ?costCode.IsActive: Pill | Pill ), (2) ⚠button→Button ) } ) ) } | LoadGate{ RecordsTable{ (n) ( ?option.IsArchived: Pill | Pill ) } } ), Modal{ ?formError is not null: Notice, (3) FormField }, Modal{ ( ?gaps.Error is not null: Notice | ( ⚠textarea→FormField, ⚠table→RecordsTable ) ) }, Modal{ ?formError is not null: Notice, (3) FormField } }
```

### `/dashboard`
`jpms/Pages/Dashboard.razor`

```
Page{ ( ?Session.ActiveRole == Role.Admin: AdminHome=( PageHeader{ DropdownMenu }, AdminStatsRow=( LoadGate{ [ (3) StatTile ] } ), MyTodosPanel=( [ ⚠h2→SectionHeader ?!loading: [ (2) ⚠button→Button ] ], ?error is not null: Notice, ?!loading && items.Count > 0 && …: ⚠select→FormField, LoadGate{ ( ?boardView && items.Count > 0: TodoBoard=( (n) ( ⚠h3→SectionHeader, (n) ⚠button→Button ) ) | (n) ⚠button→Button ) } ), AdminKpiPanel=( ⚠h2→SectionHeader, LoadGate{ EmptyState } ), OpenRequestsPanel=( ⚠h2→SectionHeader, LoadGate ), NextValuationsPanel=( ⚠h2→SectionHeader, ( ?Projects.Current is null: LoadGate | ⚠table→RecordsTable ) ), PendingRequestsPanel=( ⚠h2→SectionHeader, LoadGate{ (n) PendingRequestRow=( ?isApproving: RoleAssignmentForm ) } ) ) | ?Session.ActiveRole is not null: RoleHome=( PageHeader, ?Tiles.Count > 0: (n) MetricStat, ?ShowMyDay: MyDayWorkspace=( ?Labour.MyDay() is { WorkerId: n…: ( SectionHeader{ ToolbarButton }, ?actionError is not null: Notice, (n) ( ⚠h3→SectionHeader, ?project.HasSignedOutToday: ?todays.Count > 0: (n) StatusPill=( Pill ) ), ?history.Count > 0: ( ⚠h3→SectionHeader, (n) StatusPill=( Pill ) ), ?day.Rejected.Count > 0: ( ⚠h3→SectionHeader, (n) ⚠input→FormField ) ) ), ?ShowMyTodos: MyTodosPanel=( [ ⚠h2→SectionHeader ?!loading: [ (2) ⚠button→Button ] ], ?error is not null: Notice, ?!loading && items.Count > 0 && …: ⚠select→FormField, LoadGate{ ( ?boardView && items.Count > 0: TodoBoard=( (n) ( ⚠h3→SectionHeader, (n) ⚠button→Button ) ) | (n) ⚠button→Button ) } ), ?ShowRequests: OpenRequestsPanel=( ⚠h2→SectionHeader, LoadGate ), ?ShowValuations: NextValuationsPanel=( ⚠h2→SectionHeader, ( ?Projects.Current is null: LoadGate | ⚠table→RecordsTable ) ), ?ShowExpiringDocuments: ExpiringDocumentsPanel=( ⚠h2→SectionHeader, ( ?LoadState.UntilAllPresent(Compl…: LoadGate | ⚠table→RecordsTable ) ), ?ShowStaleRates: ⚠h2→SectionHeader, ⚠h2→SectionHeader, (n) NavIcon ) ) }
```

### `/directory`
`jpms/Pages/Subcontractors.razor`

```
Page{ ( ?!CanAccess: PageHeader | ( PageHeader{ ?group == DirectoryGroup.Subcont…: ExportToExcelButton }, (n) ⚠button→Button, ( ?group == DirectoryGroup.Clients: ClientsDirectoryTable=( ( ?Clients.Count == 0: LoadGate{ EmptyState } | RecordsTable ) ) | ?group == DirectoryGroup.Archite…: ArchitectsDirectoryTable=( ( ?Architects.Count == 0: LoadGate{ EmptyState } | RecordsTable ) ) | ?group == DirectoryGroup.Staff: StaffDirectoryTable=( ( ?Staff.Count == 0: LoadGate{ EmptyState } | RecordsTable{ (n) (n) RoleBadge } ) ) | ( TabRow, [ (2) FormField (2) FilterChips ], CompaniesDirectoryTable=( ( ?Companies.Count == 0: EmptyState | RecordsTable{ (n) CompanyDirectoryRow=( Pill, (n) Pill, ?Compliance.Current is not null: ComplianceStatusPill=( Pill ) ) } ) ) ) ), Modal{ DirectoryContactForm=( ?error is not null: Notice, FormField, [ FormField ( ?tradeIds.Count > 0: (n) Pill{ ⚠button→Button }, ⚠select→FormField ) ], ⚠input→FormField, (2) [ (2) FormField ], FormField, (2) [ (2) FormField ] ) }, XeroImportModal=( Modal{ ?error is not null: Notice, FormField, ( ?snapshot is null: ?error is null: LoadGate | ?snapshot.Error is not null: Notice | ⚠table→RecordsTable ) } ), ConsolidateRecordsModal=( Modal{ ?error is not null: Notice } ) ) ) }
```

### `/directory/compliance`
`jpms/Pages/ComplianceRegister.razor`

```
Page{ ( ?!CanAccess: PageHeader | ( PageHeader{ Toolbar{ ExportToExcelButton } }, TabRow, [ SearchInput FilterChips ], ( ?dataFailed: Notice | RecordsTable{ (n) ComplianceStatusPill=( Pill ) } ) ) ) }
```

### `/directory/{SubcontractorId}`
`jpms/Pages/SubcontractorDetail.razor`

```
Page{ ( ?!CanAccess: PageHeader | ?!SubcontractorStore.IsLoaded: LoadGate | ?subcontractor is null: PageHeader | ( PageHeader{ ?CanManageXeroLink: (n) InlineConfirm }, XeroLinkModal=( Modal{ Checkbox, ?error is not null: Notice, FormField, ( ?snapshot is null: ?error is null: LoadGate | ?snapshot.Error is not null: Notice | ⚠table→RecordsTable ) } ), XeroContactPushModal=( Modal{ ?error is not null: Notice, LoadGate{ ?preview is not null: ( (n) Notice, [ (2) XeroContactPeopleList=( ⚠h3→SectionHeader, ⚠dl→KeyValueList ) ] ) } } ), ?xeroError is not null: Notice, ?xeroNote is not null: Notice, ⚠h2→SectionHeader, ⚠dl→KeyValueList, ⚠h2→SectionHeader, ?tradesError is not null: Notice, (n) Pill{ ⚠button→Button }, [ ⚠select→FormField ⚠input→FormField ], ⚠h2→SectionHeader, ?contactsError is not null: Notice, ( ?!SubcontractorStore.ContactsLoa…: LoadGate | ⚠table→RecordsTable ), [ (3) FormField ], ⚠h2→SectionHeader, ?inviteError is not null: Notice, ?inviteResult is not null: Notice, ⚠h2→SectionHeader, CisVerificationPanel=( Panel{ ( ?!Subcontractor.HasCisVerificati…: EmptyState | ⚠dl→KeyValueList ) }, Modal{ ?recordError is not null: Notice, FormField, [ (2) FormField ] } ), SubcontractorComplianceList=( ⚠h2→SectionHeader, (n) ComplianceStatusPill=( Pill ), Modal{ ?addError is not null: Notice, [ (2) FormField ], FormField, FormField{ InputFile } }, Modal{ ?editError is not null: Notice, [ (2) FormField ] } ), SubcontractorStatementModal=( Modal{ ( ?loadError is not null: Notice | ( RecordsTable, ( ?drafted: Notice | ( RecordEmailPreviewPanel=( ?Error is not null: Notice, ⚠dl→KeyValueList ), FormField, FormField{ RichTextEditor } ) ), ?error is not null: Notice ) ) } ), Modal{ ?editError is not null: Notice, FormField, (2) [ (2) FormField ], FormField, [ (3) FormField ] } ) ) }
```

### `/document-control`
`jpms/Pages/DocumentControl.razor`

```
Page{ PageHeader{ SearchSelect }, [ (3) ⚠button→Button ], ?loadError is not null: Notice, LoadGate{ (n) DocumentListItem=( ⚠button→Button ) }, LoadGate{ ⚠h2→SectionHeader, ?actionError is not null: Notice, DocumentOutcomeCard, DocumentPreview=( ( ?IsPdf(Item): PdfViewer=( Toolbar{ ?pageCount > 1: ( (2) ToolbarButton, ToolbarDivider ), (4) ToolbarButton, ToolbarDivider, (2) ToolbarButton, ?DownloadUrl is not null: ( ToolbarDivider, ToolbarButton ) }, LoadGate ) | ?IsImage(Item): ImageViewer=( Toolbar{ (5) ToolbarButton, ToolbarDivider, (2) ToolbarButton, ?DownloadUrl is not null: ( ToolbarDivider, ToolbarButton ) }, LoadGate ) ) ), SourceEmailCard=( ⚠button→Button ), ?item.Status == DocumentControlS…: ( [ (3) ⚠button→Button ], ( ?destination == FileDestination.…: ( [ FormField ?drawingIsRevision: FormField FormField ], [ ?!drawingIsRevision: FormField FormField ?!drawingIsRevision: DrawingFolderPicker=( FormField{ DrawingFolderOptions }, ?IsCreatingFolder: [ ⚠input→FormField ⚠select→FormField ] ) ] ) | ?destination == FileDestination.…: ( [ (2) FormField ], [ (3) FormField ] ) | [ FormField{ SearchSelect } (3) FormField ] ) ) } }
```

### `/document-triage`
`jpms/Pages/DocumentControl.razor`

```
Page{ PageHeader{ SearchSelect }, [ (3) ⚠button→Button ], ?loadError is not null: Notice, LoadGate{ (n) DocumentListItem=( ⚠button→Button ) }, LoadGate{ ⚠h2→SectionHeader, ?actionError is not null: Notice, DocumentOutcomeCard, DocumentPreview=( ( ?IsPdf(Item): PdfViewer=( Toolbar{ ?pageCount > 1: ( (2) ToolbarButton, ToolbarDivider ), (4) ToolbarButton, ToolbarDivider, (2) ToolbarButton, ?DownloadUrl is not null: ( ToolbarDivider, ToolbarButton ) }, LoadGate ) | ?IsImage(Item): ImageViewer=( Toolbar{ (5) ToolbarButton, ToolbarDivider, (2) ToolbarButton, ?DownloadUrl is not null: ( ToolbarDivider, ToolbarButton ) }, LoadGate ) ) ), SourceEmailCard=( ⚠button→Button ), ?item.Status == DocumentControlS…: ( [ (3) ⚠button→Button ], ( ?destination == FileDestination.…: ( [ FormField ?drawingIsRevision: FormField FormField ], [ ?!drawingIsRevision: FormField FormField ?!drawingIsRevision: DrawingFolderPicker=( FormField{ DrawingFolderOptions }, ?IsCreatingFolder: [ ⚠input→FormField ⚠select→FormField ] ) ] ) | ?destination == FileDestination.…: ( [ (2) FormField ], [ (3) FormField ] ) | [ FormField{ SearchSelect } (3) FormField ] ) ) } }
```

### `/finance`
`jpms/Pages/CashForecast.razor`

```
Page{ PageHeader, Notice, ( [ ?Projects.Current is null: ⚠select→FormField ProjectMultiSelect=( DropdownMenu{ [ (3) ⚠button→Button ] } ) ExportToExcelButton ], LoadGate{ ?IsDirector: ForecastKpiStrip=( [ (4) StatTile ] ), ( ?FailedSelectedProjects.Count > 0: Notice{ ⚠button→Button }, CashForecastTable=( RecordsTable{ (2) (n) ForecastCategoryRow=( ⚠button→Button, ?Expanded: (n) ForecastProjectLine=( ?Category == ForecastCategory.Fu…: (2) ⚠input→FormField ) ), OverheadsRow=( ⚠input→FormField, (n) ⚠input→FormField ), ?IsDirector && Forecast.Closing.…: ClosingBalanceRow } ), ?IsDirector && forecast.Closing.…: ForecastBalanceChart, Notice, CombinedStatementCard, ProjectCashTable=( RecordsTable{ (n) ProjectCashRow, ?Rows.Count > 1: ProjectCashTotalsRow } ) ) } ) }
```

### `/finance/aged-payables`
`jpms/Pages/AgedPayables.razor`

```
Page{ PageHeader{ Toolbar{ ExportToExcelButton, ToolbarDivider, ToolbarButton } }, LoadGate{ ( [ ⚠h2→SectionHeader [ (2) ⚠button→Button ] ], RecordsTable ) } }
```

### `/finance/aged-receivables`
`jpms/Pages/AgedReceivables.razor`

```
Page{ PageHeader{ Toolbar{ ExportToExcelButton, ToolbarDivider, ToolbarButton } }, LoadGate{ ( [ ⚠h2→SectionHeader [ (2) ⚠button→Button ] ], RecordsTable ) } }
```

### `/finance/allocation`
`jpms/Pages/XeroAllocation.razor`

```
Page{ WorkspaceSectionNav, AllocationPageHeader=( [ ⚠h1→PageHeader [ (2) ⚠button→Button ] ] ), ( ?!tabRestored || Ledger.Counts()…: LoadGate | ( ?activeTab == XeroAllocationStat…: MatchedLinesBanner=( [ ?!armed: ⚠button→Button (2) ⚠button→Button ] ), LoadGate{ [ AllocationTabBar=( [ ⚠button→Button (n) ⚠button→Button ?ShowLabour: ⚠button→Button ?ShowWorkOrderBills: ⚠button→Button (4) ⚠button→Button ] ) [ ExportToExcelButton ?activeTab == XeroAllocationStat…: SearchSelect ⚠input→FormField ] ], ?AllocatedXeroChips is { } xeroC…: FilterChips, ?activeTab == XeroAllocationStat…: LabourSectionStrip=( ?CoveredCount > 0: ⚠button→Button ), ?activeTab == XeroAllocationStat…: WorkOrderBillsStrip, ?activeTab == XeroAllocationStat…: LabourBulkActions=( BulkSelectionBar=( ⚠button→Button ){ ⚠button→Button } ), ?activeTab == XeroAllocationStat…: QueueBulkActions=( BulkSelectionBar=( ⚠button→Button ){ (2) SearchSelect, ⚠button→Button, ⚠select→FormField } ), ?activeTab == XeroAllocationStat…: AllocatedBulkActions=( BulkSelectionBar=( ⚠button→Button ){ (2) ⚠button→Button } ), LoadGate{ ?activeTab == XeroAllocationStat…: BucketChipStrip=( (n) ⚠button→Button ), ?activeTab == XeroAllocationStat…: WorkOrderBillCard=( ( Pill, ?Match.AmountNote is not null: Pill ), WorkOrderBillOrderSlices=( ⚠table→RecordsTable ), WorkOrderBillLinesTable=( ⚠table→RecordsTable ), ( ?Error is not null: Notice | ?!WorkOrderBillTracking.CanBeWri…: Notice ) ) } } ) | ⚠table→RecordsTable ), ?PageCount > 1: [ (2) ⚠button→Button ], Modal{ ?splitLine is not null: ( LedgerLineSummary, SplitEditorForm=( ?Error is not null: Notice, (n) [ (2) SearchSelect ⚠input→FormField ⚠button→Button ], ⚠button→Button ) ) }, (2) SendLinesModal=( Modal{ ?IsOpen: ( ?Error is not null: Notice, SearchSelect ) } ), DisputeLineModal=( Modal{ ?Line is not null: ( ?Error is not null: Notice, LedgerLineSummary, ⚠textarea→FormField ) } ), ConfirmDialog, DisputeDiscussionModal=( Modal{ ?Line is { } discussed: ( LedgerLineSummary, EmptyState, ?Error is not null: Notice, [ ⚠textarea→FormField ⚠button→Button ] ) } ), Modal{ ?viewLine is not null: ( LedgerLineSummary, InvoiceBillLines=( ?Reference is not null || ShowsL…: ?ShowsLines: ⚠table→RecordsTable, ?fetchFailed: Notice ), ?viewLine.AllocationStatus == Xe…: InvoiceViewerActions=( ( ?SplitOpen: ( ⚠button→Button, SplitEditorForm=( ?Error is not null: Notice, (n) [ (2) SearchSelect ⚠input→FormField ⚠button→Button ], ⚠button→Button ) ) | ?DisputeOpen: ( ⚠button→Button, ?DisputeError is not null: Notice, ⚠textarea→FormField ) | ( [ (2) SearchSelect ?!string.IsNullOrEmpty(ArmedBuck…: ⚠button→Button ⚠button→Button ⚠button→Button ], [ ⚠select→FormField (2) ⚠button→Button ] ) ) ), InvoiceDocumentPreview=( ( ?(Error ?? fetchError) is { } sh…: Notice | ( ?attachments.Count > 1: (n) ⚠button→Button, ?selected is not null: ?IsImage(selected): ImageViewer=( Toolbar{ (5) ToolbarButton, ToolbarDivider, (2) ToolbarButton, ?DownloadUrl is not null: ( ToolbarDivider, ToolbarButton ) }, LoadGate ) ) ) ) ) } }
```

### `/finance/cash-forecast`
`jpms/Pages/CashForecast.razor`

```
Page{ PageHeader, Notice, ( [ ?Projects.Current is null: ⚠select→FormField ProjectMultiSelect=( DropdownMenu{ [ (3) ⚠button→Button ] } ) ExportToExcelButton ], LoadGate{ ?IsDirector: ForecastKpiStrip=( [ (4) StatTile ] ), ( ?FailedSelectedProjects.Count > 0: Notice{ ⚠button→Button }, CashForecastTable=( RecordsTable{ (2) (n) ForecastCategoryRow=( ⚠button→Button, ?Expanded: (n) ForecastProjectLine=( ?Category == ForecastCategory.Fu…: (2) ⚠input→FormField ) ), OverheadsRow=( ⚠input→FormField, (n) ⚠input→FormField ), ?IsDirector && Forecast.Closing.…: ClosingBalanceRow } ), ?IsDirector && forecast.Closing.…: ForecastBalanceChart, Notice, CombinedStatementCard, ProjectCashTable=( RecordsTable{ (n) ProjectCashRow, ?Rows.Count > 1: ProjectCashTotalsRow } ) ) } ) }
```

### `/finance/cash-summary`
`jpms/Pages/CashForecast.razor`

```
Page{ PageHeader, Notice, ( [ ?Projects.Current is null: ⚠select→FormField ProjectMultiSelect=( DropdownMenu{ [ (3) ⚠button→Button ] } ) ExportToExcelButton ], LoadGate{ ?IsDirector: ForecastKpiStrip=( [ (4) StatTile ] ), ( ?FailedSelectedProjects.Count > 0: Notice{ ⚠button→Button }, CashForecastTable=( RecordsTable{ (2) (n) ForecastCategoryRow=( ⚠button→Button, ?Expanded: (n) ForecastProjectLine=( ?Category == ForecastCategory.Fu…: (2) ⚠input→FormField ) ), OverheadsRow=( ⚠input→FormField, (n) ⚠input→FormField ), ?IsDirector && Forecast.Closing.…: ClosingBalanceRow } ), ?IsDirector && forecast.Closing.…: ForecastBalanceChart, Notice, CombinedStatementCard, ProjectCashTable=( RecordsTable{ (n) ProjectCashRow, ?Rows.Count > 1: ProjectCashTotalsRow } ) ) } ) }
```

### `/finance/payment-certificates`
`jpms/Pages/PaymentCertificates.razor`

```
Page{ PageHeader, SearchSelect, ?loadError is not null: Notice, LoadGate{ (n) ⚠table→RecordsTable } }
```

### `/finance/profit-summary`
`jpms/Pages/ProfitSummary.razor`

```
Page{ PageHeader, ( [ ?Projects.Current is null: ⚠select→FormField ProjectMultiSelect=( DropdownMenu{ [ (3) ⚠button→Button ] } ) ExportToExcelButton ], ?selectionInitialised && Selecte…: RunningProfitPanel=( [ Toolbar{ (2) ToolbarButton, ToolbarDivider, ToolbarButton } ⚠input→FormField ], RunningProfitTable=( ⚠table→RecordsTable ) ), LoadGate{ ?!selectionInitialised || Select…: ProfitSummaryStrip=( [ (3) StatTile ] ), ( ?FailedSelectedProjects.Count > 0: Notice{ ⚠button→Button }, ?bridge is not null: BudgetForecastBridge, ProfitTable=( RecordsTable{ (11) SortableColumnHeader, (n) ProfitTableRow=( ProjectStageBadge, ?Row.CertifiedToDate == 0m && Ro…: Pill, MarginLine, MemoLine, (2) MarginLine, MemoLine, MarginLine ), ?Rows.Count > 1: ProfitTotalsRow=( MarginLine, MemoLine, (2) MarginLine, (2) MemoLine, MarginLine ) } ), ProfitTableNotes ) }, ?selectionInitialised && Selecte…: TrajectoryPanel, ?selectionInitialised && Selecte…: CumulativePositionPanel=( Toolbar{ ToolbarButton }, ?syncError is not null: Notice, ?reconciliations is { Count: > 0…: ( ?mismatches.Count == 0: Notice | Notice ), LoadGate{ (n) CumulativeChartCard } ) ) }
```

### `/finance/weekly-cashflow`
`jpms/Pages/WeeklyCashflow.razor`

```
Page{ PageHeader{ Toolbar{ ExportToExcelButton, ToolbarDivider, (2) ToolbarButton } }, LoadGate{ WeeklyKpiStrip=( [ ?IsDirector: StatTile StatTile ?IsDirector: (2) StatTile (2) StatTile ] ), ?moveError is not null: Notice{ ⚠button→Button }, WeeklyCashflowGrid=( RecordsTable{ CashflowAddItemRow=( ⚠button→Button ), ?IsDirector && View.Closing is n…: WeeklyClosingBalanceRow } ) }, CashflowItemModal=( Modal{ FormField, [ (2) FormField ], [ FormField ⚠input→FormField ], ?formRecurrence != WeeklyCashflo…: FormField, FormField } ), SupplierGroupsModal=( Modal{ ( ?!editorOpen: EmptyState | (2) FormField ) } ) }
```

### `/finance/xero`
`jpms/Pages/XeroTransactions.razor`

```
Page{ WorkspaceSectionNav, PageHeader{ Toolbar{ ExportToExcelButton, ToolbarDivider, ToolbarButton } }, ( ?Snapshot is null: LoadGate | ( [ (2) ⚠button→Button ], ( ?activeView == View.Transactions: ( [ ⚠input→FormField (n) ⚠button→Button ], RecordsTable ) | RecordsTable ) ) ) }
```

### `/forgot-password`
`jpms/Pages/ForgotPassword.razor`

```
JewelIcon, ( ?sent: ( JewelIcon, ⚠h1→PageHeader ) | ( ⚠h1→PageHeader, ⚠input→FormField ) )
```

### `/imagine/{Token}`
`jpms/Pages/Imagine.razor`

```
JewelIcon, LoadGate{ ( ?view is null: ⚠h1→PageHeader | ( ⚠h1→PageHeader, ?error is not null: Notice, ?v.Proposal is { } proposal: ImagineProposal=( ⚠h2→SectionHeader, ?Proposal.Status == SalesProposa…: Notice, ?!string.IsNullOrWhiteSpace(Prop…: ( ⚠h3→SectionHeader, SimpleMarkdown ), ⚠h3→SectionHeader, ?Proposal.Options.Count > 0: (n) ?o.Recommended && !Accepted: Pill, ?Proposal.Schedule.Count > 0: ⚠h3→SectionHeader, ?!string.IsNullOrWhiteSpace(Prop…: ( ⚠h3→SectionHeader, SimpleMarkdown ), ?!Accepted && Proposal.Status ==…: ( ⚠h3→SectionHeader, [ (2) ⚠input→FormField ], Checkbox, ?declining: ⚠input→FormField ) ), ( ?v.Rounds.Count == 0: ImagineSubmissionForm=( ⚠h2→SectionHeader, InputFile, ?preparing: JewelIcon, ?photos.Count > 0: (n) ⚠button→Button, ⚠textarea→FormField, [ (2) ⚠input→FormField ], Checkbox ) | ( ImagineRoundList=( (n) ( ⚠h2→SectionHeader, ImagineRoundOutcome=( ?Round.Status.IsInProgress(): JewelIcon ){ (n) ( ⚠h3→SectionHeader, ⚠button→Button, ⚠input→FormField, ?revising?.ImageId == c.ImageId: ⚠textarea→FormField ) }, ImagineRoundPhotos ) ), ⚠h2→SectionHeader ) ) ) ) }
```

### `/internal/communications`
`jpms/Pages/SubcontractorCommunications.razor`

```
Page{ PageHeader, [ ⚠button→Button (n) ⚠button→Button ], ?loadError is not null: Notice, LoadGate{ ( ?replySent is not null: ⚠button→Button, ?replyTo is { } replyingTo: MailReplyComposer=( ⚠button→Button, ?error is not null: Notice, FormField{ RecipientInput=( [ (n) ?!Disabled: ⚠button→Button ⚠input→FormField ], ?isOpen && suggestions.Count > 0: (n) ⚠button→Button ) }, ?!showBcc: ⚠button→Button, RecipientInput=( [ (n) ?!Disabled: ⚠button→Button ⚠input→FormField ], ?isOpen && suggestions.Count > 0: (n) ⚠button→Button ), ?showBcc: FormField{ RecipientInput=( [ (n) ?!Disabled: ⚠button→Button ⚠input→FormField ], ?isOpen && suggestions.Count > 0: (n) ⚠button→Button ) }, FormField, RichTextEditor, AttachmentPicker=( ?Attachments.Count > 0: (n) ⚠button→Button, [ InputFile (3) ⚠button→Button ?OriginalAttachments is { Count:…: ⚠button→Button ], ?openPanel != Panel.None: ( ?openPanel is Panel.Drawings or …: FormField, ( ?openPanel == Panel.Drawings: ( ⚠input→FormField, (n) ?folderGroup.Folder is not null …: ⚠button→Button, ?SelectedDrawingCount > 0 && !at…: ⚠button→Button ) | ?openPanel == Panel.Photos: (n) (n) ⚠button→Button | ?openPanel == Panel.Records: ( ⚠input→FormField, (n) ⚠button→Button ) ) ) ) ), CorrespondenceThreadList ) } }
```

### `/internal/communications/{Category?}`
`jpms/Pages/SubcontractorCommunications.razor`

```
Page{ PageHeader, [ ⚠button→Button (n) ⚠button→Button ], ?loadError is not null: Notice, LoadGate{ ( ?replySent is not null: ⚠button→Button, ?replyTo is { } replyingTo: MailReplyComposer=( ⚠button→Button, ?error is not null: Notice, FormField{ RecipientInput=( [ (n) ?!Disabled: ⚠button→Button ⚠input→FormField ], ?isOpen && suggestions.Count > 0: (n) ⚠button→Button ) }, ?!showBcc: ⚠button→Button, RecipientInput=( [ (n) ?!Disabled: ⚠button→Button ⚠input→FormField ], ?isOpen && suggestions.Count > 0: (n) ⚠button→Button ), ?showBcc: FormField{ RecipientInput=( [ (n) ?!Disabled: ⚠button→Button ⚠input→FormField ], ?isOpen && suggestions.Count > 0: (n) ⚠button→Button ) }, FormField, RichTextEditor, AttachmentPicker=( ?Attachments.Count > 0: (n) ⚠button→Button, [ InputFile (3) ⚠button→Button ?OriginalAttachments is { Count:…: ⚠button→Button ], ?openPanel != Panel.None: ( ?openPanel is Panel.Drawings or …: FormField, ( ?openPanel == Panel.Drawings: ( ⚠input→FormField, (n) ?folderGroup.Folder is not null …: ⚠button→Button, ?SelectedDrawingCount > 0 && !at…: ⚠button→Button ) | ?openPanel == Panel.Photos: (n) (n) ⚠button→Button | ?openPanel == Panel.Records: ( ⚠input→FormField, (n) ⚠button→Button ) ) ) ) ), CorrespondenceThreadList ) } }
```

### `/labour/overview`
`jpms/Pages/LabourOverview.razor`

```
Page{ SectionHeader{ Toolbar{ ToolbarButton, ExportToExcelButton } }, ?actionError is not null: Notice, ?weekSummaryLines is not null: Notice, LabourForecastHeader=( Panel{ MetricStat } ), (n) ⚠button→Button, ( ?view == "workers": WorkerPlacementTable=( Panel{ ⚠table→RecordsTable } ) | ?view == "sites": SiteCostTable=( Panel{ ⚠table→RecordsTable } ) | ?view == "costcodes": CostCodeTable=( Panel{ ⚠table→RecordsTable } ) | ?view == "signoff": WeeklySignOffTable=( Panel{ ⚠table→RecordsTable } ) | SettlementSchedulesPanel=( Panel{ ( [ ?Schedules.InvoicesToChase > 0: Pill ?Schedules.WorkersToReconcile > 0: Pill ], ⚠table→RecordsTable ) }, Modal ) ), ?snapshot is not null && snapsho…: ChaseListPanel=( Panel{ ⚠table→RecordsTable, ?Items.Count > 8 && !showAll: ⚠button→Button } ), AbsenceModal=( Modal{ ?error is not null: Notice, FormField, [ (2) FormField ], (2) FormField } ), SettlementLineModal=( Modal{ (5) FormField, ?error is not null: Notice } ), CodingResetModal=( Modal{ FormField, ?error is not null: Notice } ), LabourBillApproveDialog=( ConfirmDialog{ ?error is not null: Notice } ), WeekEntryModal=( Modal{ FormField, ⚠table→RecordsTable, ?error is not null: Notice } ) }
```

### `/labour/workers`
`jpms/Pages/Workers.razor`

```
Page{ ⚠h2→SectionHeader, ?actionError is not null: Notice, LoadGate{ ( ?workers.Count == 0: EmptyState | ( ExportToExcelButton, ⚠table→RecordsTable ) ) }, ?RegistryReady && UnlinkedWorker…: ( ⚠h3→SectionHeader, ?matchError is not null: Notice, ?linkReport is not null: ⚠table→RecordsTable ), Modal{ ?formError is not null: Notice, [ (4) ⚠input→FormField SearchSelect (2) ⚠input→FormField ] } }
```

### `/labour/xero-mapping`
`jpms/Pages/LabourXeroMapping.razor`

```
Page{ SectionHeader{ Toolbar{ ToolbarButton } }, ?actionError is not null: Notice, LoadGate{ Panel{ ( [ (2) FormField ], ( ?openSites.Count == 0: EmptyState | ⚠table→RecordsTable ) ) }, Panel{ ( [ (4) FormField ], ( ?openCodes.Count == 0: EmptyState | ⚠table→RecordsTable ) ) } } }
```

### `/login`
`jpms/Pages/Login.razor`

```
JewelIcon, ⚠h1→PageHeader, (2) ⚠input→FormField
```

### `/logout`
`jpms/Pages/Logout.razor`

```

```

### `/my-day`
`jpms/Pages/MyDay.razor`

```

```

### `/policies`
`jpms/Pages/Policies.razor`

```
Page{ SectionHeader, ?actionError is not null: Notice, Panel{ (n) FormField }, ?IsAdminView && PolicyDocs.Curre…: Panel{ ⚠table→RecordsTable }, Modal{ (3) FormField, ?publishError is not null: Notice } }
```

### `/portal`
`jpms/Pages/PortalHome.razor`

```
Page{ ( ?!CanAccess: PageHeader | ?!HasLinkedRecord: PageHeader | LoadGate{ ( ?myRecord is null: PageHeader | ( PageHeader, ?ExpiringOrExpired.Count > 0: Notice, ⚠h2→SectionHeader, (n) ComplianceStatusPill=( Pill ), ⚠h2→SectionHeader, ?uploadError is not null: Notice, ?uploadNote is not null: Notice, [ (3) FormField ], FormField{ InputFile }, ⚠h2→SectionHeader, (n) Pill, ⚠h2→SectionHeader, (n) ( Pill, ?variationRequest.IsOpen: ⚠button→Button ), ⚠h2→SectionHeader, ?variationError is not null: Notice, ?variationNote is not null: Notice, ( [ (2) FormField ], (2) FormField ) ) ) } ) }
```

### `/portal/work-orders/{WorkOrderId}`
`jpms/Pages/PortalWorkOrderView.razor`

```
Page{ ( ?!CanAccess || !HasLinkedRecord: PageHeader | LoadGate{ ( ?Order is null: PageHeader | ( ?Order.Order.IsAccepted: Pill, ?acceptError is not null: Notice, ?acceptNote is not null: Notice, PurchaseOrderSheet=( ⚠h1→PageHeader, ⚠dl→KeyValueList, ⚠table→RecordsTable, ?Lines.Count > 0: ⚠table→RecordsTable ) ) ) } ) }
```

### `/projects`
`jpms/Pages/Projects.razor`

```
Page{ PageHeader, ExportToExcelButton, ProjectsTable=( ⚠table→RecordsTable ), Modal{ NewProjectForm=( (3) FormField, [ (2) FormField ], FormField ) } }
```

### `/projects/{ProjectId}`
`jpms/Pages/ProjectDetail.razor`

```

```

### `/projects/{ProjectId}/architect-instructions`
`jpms/Pages/ProjectArchitectInstructions.razor`

```
Page{ ProjectPageShell=( ( ?Projects.Current is null: LoadGate | ?Project is null: PageHeader | PageHeader{ ?Neighbour(-1) is { } previous: ⚠button→Button, ?Neighbour(1) is { } next: ⚠button→Button, ProjectStageBadge } ) ){ ⚠h2→SectionHeader, ?error is not null: Notice, Panel{ ⚠table→RecordsTable } }, Modal{ ?dialogError is not null: Notice, [ (2) FormField ], (3) FormField, FormField{ InputFile } }, Modal{ ?dialogError is not null: Notice, (n) ?alreadyLinked: ⚠button→Button }, Modal{ ?dialogError is not null: Notice } }
```

### `/projects/{ProjectId}/bid-package-invites`
`jpms/Pages/ProjectBidPackageInvites.razor`

```
Page{ ProjectPageShell=( ( ?Projects.Current is null: LoadGate | ?Project is null: PageHeader | PageHeader{ ?Neighbour(-1) is { } previous: ⚠button→Button, ?Neighbour(1) is { } next: ⚠button→Button, ProjectStageBadge } ) ){ [ ⚠h2→SectionHeader ExportToExcelButton ], ?error is not null: Notice, RecordsTable{ (n) Pill } }, Modal{ (2) FormField }, Modal{ ?suggestError is not null: Notice, LoadGate{ ( ?suggestions is null: FormField | (n) Pill ) } } }
```

### `/projects/{ProjectId}/bid-package-invites/{BidPackageId}`
`jpms/Pages/ProjectBidPackageInviteDetail.razor`

```
Page{ ProjectPageShell=( ( ?Projects.Current is null: LoadGate | ?Project is null: PageHeader | PageHeader{ ?Neighbour(-1) is { } previous: ⚠button→Button, ?Neighbour(1) is { } next: ⚠button→Button, ProjectStageBadge } ) ){ ?error is not null: Notice, LoadGate{ ( [ [ ⚠h2→SectionHeader Pill ] ?CanManage && package.Status != …: DropdownMenu ], (n) ⚠button→Button, ?activeTab == "tender-list": InvitedSubcontractorsSection=( [ ⚠h3→SectionHeader ?CanEdit: [ ?Recipients.Count > 0: ⚠button→Button ?ReadyForInvites: (2) ⚠button→Button ] ], ?SendNote is not null: Notice, LoadGate{ ( ?Recipients.Count == 0: ?Fetched: EmptyState | RecordsTable{ (n) ( ?sub is { IsProspect: true }: Pill, ?sub is not null && string.IsNul…: Pill ) } ) } ), ?activeTab == "details": PackageDetailsSections=( (2) ⚠h3→SectionHeader, LoadGate{ ( ?LineItems.Count == 0: ?Fetched: EmptyState | (n) RecordsTable{ (n) ( ?string.IsNullOrWhiteSpace(item.…: Pill, ?CanEdit: ⚠button→Button ) } ) } ), ?activeTab == "submissions": TenderSubmissionsSection=( [ ⚠h3→SectionHeader ?CanEdit && HasRecipients: ⚠button→Button ], RecordCorrespondencePanel=( [ ⚠h3→SectionHeader [ ?Loaded && Emails.Count > 0: Pill EmailFinder=( ⚠button→Button, Modal{ ⚠input→FormField, ?error is not null: Notice, ?results is not null: (n) ( ?email.HasAttachments: Pill, [ ?alreadyTagged: Pill (n) Pill ] ) } ) ] ], ?Error is not null: Notice, LoadGate{ ?sentNote is not null: ⚠button→Button, ?replyingTo is { } anchor: MailReplyComposer=( ⚠button→Button, ?error is not null: Notice, FormField{ RecipientInput }, ?!showBcc: ⚠button→Button, RecipientInput, ?showBcc: FormField{ RecipientInput }, FormField, RichTextEditor, AttachmentPicker ), CorrespondenceThreadList } ){ ?DispositionOf(email) is { Outco…: Pill, ?CanEdit: ?!extracted: ⚠button→Button }, ?DiscardedEmails.Count > 0: CorrespondenceThreadList{ ?DispositionOf(email) is { } ver…: Pill, ?CanEdit: ⚠button→Button }, ?AwardNote is not null: Notice, ?Awarded is { } awarded: ( Notice{ ?CanEdit: ⚠button→Button }, ?WorkOrderEmailNote is not null: Notice ), LoadGate{ TenderQuoteComparisonTable=( RecordsTable{ (n) ?won: Pill, ?AwardEnabled: (n) ⚠button→Button } ) } ), ?activeTab == "documents": PackageDocumentsSection=( [ ⚠h3→SectionHeader ?CanEdit: [ InputFile ⚠button→Button ] ], LoadGate{ ?Attachments.Count > 0: (n) ?CanEdit: ⚠button→Button, ?Drawings.Count > 0: (n) Pill } ), ?activeTab == "emails": RecordCorrespondencePanel=( [ ⚠h3→SectionHeader [ ?Loaded && Emails.Count > 0: Pill EmailFinder=( ⚠button→Button, Modal{ ⚠input→FormField, ?error is not null: Notice, ?results is not null: (n) ( ?email.HasAttachments: Pill, [ ?alreadyTagged: Pill (n) Pill ] ) } ) ] ], ?Error is not null: Notice, LoadGate{ ?sentNote is not null: ⚠button→Button, ?replyingTo is { } anchor: MailReplyComposer=( ⚠button→Button, ?error is not null: Notice, FormField{ RecipientInput=( [ (n) ?!Disabled: ⚠button→Button ⚠input→FormField ], ?isOpen && suggestions.Count > 0: (n) ⚠button→Button ) }, ?!showBcc: ⚠button→Button, RecipientInput=( [ (n) ?!Disabled: ⚠button→Button ⚠input→FormField ], ?isOpen && suggestions.Count > 0: (n) ⚠button→Button ), ?showBcc: FormField{ RecipientInput=( [ (n) ?!Disabled: ⚠button→Button ⚠input→FormField ], ?isOpen && suggestions.Count > 0: (n) ⚠button→Button ) }, FormField, RichTextEditor, AttachmentPicker=( ?Attachments.Count > 0: (n) ⚠button→Button, [ InputFile (3) ⚠button→Button ?OriginalAttachments is { Count:…: ⚠button→Button ], ?openPanel != Panel.None: ( ?openPanel is Panel.Drawings or …: FormField, ( ?openPanel == Panel.Drawings: ( ⚠input→FormField, (n) ?folderGroup.Folder is not null …: ⚠button→Button, ?SelectedDrawingCount > 0 && !at…: ⚠button→Button ) | ?openPanel == Panel.Photos: (n) (n) ⚠button→Button | ?openPanel == Panel.Records: ( ⚠input→FormField, (n) ⚠button→Button ) ) ) ) ), CorrespondenceThreadList } ) ) } }, SubcontractorInvitePickerModal=( Modal{ [ ⚠input→FormField ?InvitableTrades.Count > 1: ⚠select→FormField ], ( ?Invitable.Count == 0: EmptyState | (n) ?string.IsNullOrWhiteSpace(sub.C…: Pill ), [ (2) ⚠input→FormField ⚠select→FormField ] } ), LocalSubcontractorFinderModal=( Modal{ ( ?notReadyReason is not null: Notice | ⚠input→FormField ), ?searchError is not null: Notice, ( ?results.Count > 0: (n) ?place.ExistingSubcontractorId i…: Pill | ?hasSearched && !searchBusy && s…: EmptyState ) } ), LineCoverageModal=( Modal{ ?Line is not null: ( FormField, ( ?coverage == BidPackageLineCover…: ⚠select→FormField | ?coverage == BidPackageLineCover…: ( ?Variations.Count == 0: EmptyState | ⚠select→FormField ) ) ) } ), LinkDrawingsModal=( Modal ), TenderInviteComposerModal=( Modal{ ?sendError is not null: Notice, ?draftSavedAt is { } savedAt: Notice, ?MissingEmail.Count > 0: Notice, ?LineItems.Count == 0: Notice, (3) FormField{ RecipientInput=( [ (n) ?!Disabled: ⚠button→Button ⚠input→FormField ], ?isOpen && suggestions.Count > 0: (n) ⚠button→Button ) }, FormField, ⚠button→Button, ?editingBody: RichTextEditor } ), TenderSubmissionModal=( Modal{ ?saveError is not null: Notice, LoadGate{ ( ?issues.Count > 0: Notice | ?sourceEmail is not null && comp…: Notice ), FormField, ⚠table→RecordsTable, FormField } } ), WorkOrderEmailModal=( Modal{ FormField, FormField{ RichTextEditor } } ), DeletePackageModal=( Modal ), PackageDetailsEditorModal=( Modal{ ?(validationError ?? Error) is {…: Notice, ⚠textarea→FormField, RecordsTable{ (n) ( (4) ⚠input→FormField, ⚠select→FormField ) } } ), ValuationLinePickerModal=( Modal{ ( ?lines is null && !loadFailed: LoadGate | ?SelectableLines.Count == 0: EmptyState | ( ⚠input→FormField, RecordsTable{ (n) (n) ( Pill, ?LineTypeBadge(line) is { } badge: Pill ) } ) ) } ) }
```

### `/projects/{ProjectId}/building-control`
`jpms/Pages/ProjectBuildingControl.razor`

```
Page{ ProjectPageShell=( ( ?Projects.Current is null: LoadGate | ?Project is null: PageHeader | PageHeader{ ?Neighbour(-1) is { } previous: ⚠button→Button, ?Neighbour(1) is { } next: ⚠button→Button, ProjectStageBadge } ) ){ ⚠h2→SectionHeader, ?actionError is { } error: Notice, LoadGate{ ( ( ?ActiveCase is null: ⚠h3→SectionHeader | ( [ [ ⚠h3→SectionHeader Pill ] [ ⚠select→FormField ⚠button→Button ] ], ⚠dl→KeyValueList, InputFile, ⚠select→FormField, ( ?CaseFiles(ActiveCase).Count == 0: EmptyState | (n) ⚠button→Button ) ) ), ?caseFormOpen: ( ⚠h3→SectionHeader, [ (2) FormField ], (2) [ (3) FormField ], FormField, ⚠button→Button ), ?ActiveCase is not null: ( ⚠h3→SectionHeader, ?addStageOpen: [ (2) FormField ], ( ?Inspections.Count == 0: EmptyState | RecordsTable{ (n) ( Pill, ?inspection.Status == BuildingCo…: ⚠button→Button ) } ) ) ) } } }
```

### `/projects/{ProjectId}/building-control/inspections/{InspectionId}`
`jpms/Pages/ProjectBuildingControlInspection.razor`

```
Page{ ProjectPageShell=( ( ?Projects.Current is null: LoadGate | ?Project is null: PageHeader | PageHeader{ ?Neighbour(-1) is { } previous: ⚠button→Button, ?Neighbour(1) is { } next: ⚠button→Button, ProjectStageBadge } ) ){ LoadGate{ ( [ ⚠h2→SectionHeader ⚠select→FormField ], ?actionError is { } error: Notice, [ (3) FormField ], (2) FormField, [ ⚠h3→SectionHeader InputFile ], ( ?Photos.Count == 0: EmptyState | (n) ⚠button→Button ), [ ⚠h3→SectionHeader InputFile ], (n) ⚠button→Button, [ ⚠h3→SectionHeader [ ?emails is not null && Emails.Co…: Pill EmailFinder=( ⚠button→Button, Modal{ ⚠input→FormField, ?error is not null: Notice, ?results is not null: (n) ( ?email.HasAttachments: Pill, [ ?alreadyTagged: Pill (n) Pill ] ) } ) ] ], LoadGate{ ?sentNote is not null: ⚠button→Button, ?replyingTo is { } anchor: MailReplyComposer=( ⚠button→Button, ?error is not null: Notice, FormField{ RecipientInput=( [ (n) ?!Disabled: ⚠button→Button ⚠input→FormField ], ?isOpen && suggestions.Count > 0: (n) ⚠button→Button ) }, ?!showBcc: ⚠button→Button, RecipientInput=( [ (n) ?!Disabled: ⚠button→Button ⚠input→FormField ], ?isOpen && suggestions.Count > 0: (n) ⚠button→Button ), ?showBcc: FormField{ RecipientInput=( [ (n) ?!Disabled: ⚠button→Button ⚠input→FormField ], ?isOpen && suggestions.Count > 0: (n) ⚠button→Button ) }, FormField, RichTextEditor, AttachmentPicker=( ?Attachments.Count > 0: (n) ⚠button→Button, [ InputFile (3) ⚠button→Button ?OriginalAttachments is { Count:…: ⚠button→Button ], ?openPanel != Panel.None: ( ?openPanel is Panel.Drawings or …: FormField, ( ?openPanel == Panel.Drawings: ( ⚠input→FormField, (n) ?folderGroup.Folder is not null …: ⚠button→Button, ?SelectedDrawingCount > 0 && !at…: ⚠button→Button ) | ?openPanel == Panel.Photos: (n) (n) ⚠button→Button | ?openPanel == Panel.Records: ( ⚠input→FormField, (n) ⚠button→Button ) ) ) ) ), ( (n) ?email.HasAttachments: ⚠button→Button, CorrespondenceThreadList ) } ) } } }
```

### `/projects/{ProjectId}/calendar`
`jpms/Pages/ProjectCalendar.razor`

```
Page{ ProjectPageShell=( ( ?Projects.Current is null: LoadGate | ?Project is null: PageHeader | PageHeader{ ?Neighbour(-1) is { } previous: ⚠button→Button, ?Neighbour(1) is { } next: ⚠button→Button, ProjectStageBadge } ) ){ SectionHeader, ?actionError is { } error: Notice, LoadGate{ ( (n) (n) ( (n) ⚠button→Button, ( ?!showAll && dayEvents.Count > 3: ⚠button→Button | ?showAll && dayEvents.Count > 3: ⚠button→Button ) ), ⚠h3→SectionHeader, (n) (n) ⚠button→Button ) } } }, Modal{ ?modalError is { } problem: Notice, FormField, (2) [ (2) FormField ], FormField, ?editingId is not null: ⚠button→Button }
```

### `/projects/{ProjectId}/cashflow`
`jpms/Pages/ProjectCashflow.razor`

```
Page{ ProjectPageShell=( ( ?Projects.Current is null: LoadGate | ?Project is null: PageHeader | PageHeader{ ?Neighbour(-1) is { } previous: ⚠button→Button, ?Neighbour(1) is { } next: ⚠button→Button, ProjectStageBadge } ) ){ ⚠h2→SectionHeader, LoadGate{ ⚠button→Button }, ⚠h3→SectionHeader, UnpaidXeroInvoicesModal=( Modal{ ( ExportToExcelButton, ?Lines.Count > 0: ⚠table→RecordsTable, ?UnallocatedBills.Count > 0: ⚠table→RecordsTable ) } ) } }
```

### `/projects/{ProjectId}/communications`
`jpms/Pages/ProjectCommunications.razor`

```
Page{ ProjectPageShell=( ( ?Projects.Current is null: LoadGate | ?Project is null: PageHeader | PageHeader{ ?Neighbour(-1) is { } previous: ⚠button→Button, ?Neighbour(1) is { } next: ⚠button→Button, ProjectStageBadge } ) ){ [ ⚠button→Button (n) ⚠button→Button ], [ ( SearchInput, ?!string.IsNullOrEmpty(searchTex…: ⚠button→Button ) ⚠select→FormField ], ?loadError is not null: Notice, LoadGate{ ( ?replySent is not null: ⚠button→Button, ?replyTo is { } replyingTo: MailReplyComposer=( ⚠button→Button, ?error is not null: Notice, FormField{ RecipientInput=( [ (n) ?!Disabled: ⚠button→Button ⚠input→FormField ], ?isOpen && suggestions.Count > 0: (n) ⚠button→Button ) }, ?!showBcc: ⚠button→Button, RecipientInput=( [ (n) ?!Disabled: ⚠button→Button ⚠input→FormField ], ?isOpen && suggestions.Count > 0: (n) ⚠button→Button ), ?showBcc: FormField{ RecipientInput=( [ (n) ?!Disabled: ⚠button→Button ⚠input→FormField ], ?isOpen && suggestions.Count > 0: (n) ⚠button→Button ) }, FormField, RichTextEditor, AttachmentPicker=( ?Attachments.Count > 0: (n) ⚠button→Button, [ InputFile (3) ⚠button→Button ?OriginalAttachments is { Count:…: ⚠button→Button ], ?openPanel != Panel.None: ( ?openPanel is Panel.Drawings or …: FormField, ( ?openPanel == Panel.Drawings: ( ⚠input→FormField, (n) ?folderGroup.Folder is not null …: ⚠button→Button, ?SelectedDrawingCount > 0 && !at…: ⚠button→Button ) | ?openPanel == Panel.Photos: (n) (n) ⚠button→Button | ?openPanel == Panel.Records: ( ⚠input→FormField, (n) ⚠button→Button ) ) ) ) ), (n) ( ?BucketLabel(item.Message.Bucket…: Pill, ?item.Message.HasAttachments: Pill, (n) Pill, [ (2) ⚠button→Button ?CanTag: ⚠button→Button ], ?CanTag && IsTagPickerOpen(item.…: ( ?tagError is not null: Notice, [ ⚠select→FormField ⚠select→FormField ] ) ) ) } } }
```

### `/projects/{ProjectId}/defects`
`jpms/Pages/ProjectDefects.razor`

```
Page{ ProjectPageShell=( ( ?Projects.Current is null: LoadGate | ?Project is null: PageHeader | PageHeader{ ?Neighbour(-1) is { } previous: ⚠button→Button, ?Neighbour(1) is { } next: ⚠button→Button, ProjectStageBadge } ) ){ SectionHeader, ?actionError is { } error: Notice, ?raiseOpen: ( ⚠h3→SectionHeader, [ FormField FormField{ SearchSelect } ], ?string.IsNullOrWhiteSpace(newSu…: FormField, FormField ), ( ?!Defects.LoadedFor(ProjectId) &…: LoadGate | ?Rows.Count == 0: EmptyState | RecordsTable ), ⚠select→FormField } }
```

### `/projects/{ProjectId}/defects/{DefectId}`
`jpms/Pages/ProjectDefectDetail.razor`

```
Page{ ProjectPageShell=( ( ?Projects.Current is null: LoadGate | ?Project is null: PageHeader | PageHeader{ ?Neighbour(-1) is { } previous: ⚠button→Button, ?Neighbour(1) is { } next: ⚠button→Button, ProjectStageBadge } ) ){ LoadGate{ ( ?defect is null: PageHeader | ( PageHeader{ Pill, ?CanEdit: ⚠select→FormField }, ?error is not null: Notice, [ DefectCommunicationsPanel=( Panel{ EmailFinder=( ⚠button→Button, Modal{ ⚠input→FormField, ?error is not null: Notice, ?results is not null: (n) ( ?email.HasAttachments: Pill, [ ?alreadyTagged: Pill (n) Pill ] ) } ), Toolbar{ ?CanSend: ToolbarButton, ToolbarButton }, ?failed is not null: Notice, ?sentNote is not null: Notice, ?composingNew: DefectEmailComposer=( MailReplyComposer=( ⚠button→Button, ?error is not null: Notice, FormField{ RecipientInput }, ?!showBcc: ⚠button→Button, RecipientInput, ?showBcc: FormField{ RecipientInput }, FormField, RichTextEditor, AttachmentPicker ) ), ?replyingTo is { } replyAnchor: DefectEmailComposer=( MailReplyComposer=( ⚠button→Button, ?error is not null: Notice, FormField{ RecipientInput }, ?!showBcc: ⚠button→Button, RecipientInput, ?showBcc: FormField{ RecipientInput }, FormField, RichTextEditor, AttachmentPicker ) ), ?emails.Count > 0: CorrespondenceThreadList } ) ( Panel{ ⚠dl→KeyValueList }, DefectTodosPanel=( Panel{ ( ?failed is not null: Notice | (n) Pill ) }, Modal{ ?addError is not null: Notice, FormField, [ FormField{ TodoAssigneeSelect=( SearchSelect, ?ShowPeople: SearchSelect ) } FormField ], FormField } ) ) ], Modal{ ?editError is not null: Notice, FormField, FormField{ SearchSelect }, ?string.IsNullOrWhiteSpace(editS…: FormField, FormField } ) ) } } }
```

### `/projects/{ProjectId}/documents`
`jpms/Pages/ProjectDrawings.razor`

```
Page{ ProjectPageShell=( ( ?Projects.Current is null: LoadGate | ?Project is null: PageHeader | PageHeader{ ?Neighbour(-1) is { } previous: ⚠button→Button, ?Neighbour(1) is { } next: ⚠button→Button, ProjectStageBadge } ) ){ [ ( ⚠h2→SectionHeader, ?Ambiguous.Count > 0: Pill ) [ ExportToExcelButton ⚠button→Button ] ], ?isUploading: DrawingUploadForm=( ⚠h3→SectionHeader, DrawingFolderPicker=( FormField{ DrawingFolderOptions }, ?IsCreatingFolder: [ ⚠input→FormField ⚠select→FormField ] ), ?isRevision: FormField, (n) InputFile, ( ?selectedFiles.Count == 1: ⚠button→Button | ?IsBulk: ( ⚠button→Button, (n) ⚠button→Button ) ), ?!isRevision && !IsBulk: (2) FormField, ?!IsBulk: (2) FormField, ?error is not null: Notice, ?failures.Count > 0: Notice ), DrawingsTable=( RecordsTable{ (n) [ ActionIcon ?folderSection.Folder is not null: ActionIcon ?CanManage && folderSection.Fold…: [ (4) ⚠button→Button ] ] } ), Modal{ FormField, ?folderError is not null: Notice }, Modal{ ?folderError is not null: Notice }, Modal{ ?extractAllError is not null: Notice } } }
```

### `/projects/{ProjectId}/documents/ambiguous`
`jpms/Pages/ProjectDrawingsAmbiguous.razor`

```
Page{ ProjectPageShell=( ( ?Projects.Current is null: LoadGate | ?Project is null: PageHeader | PageHeader{ ?Neighbour(-1) is { } previous: ⚠button→Button, ?Neighbour(1) is { } next: ⚠button→Button, ProjectStageBadge } ) ){ ⚠h2→SectionHeader, DrawingRevisionList=( ⚠h2→SectionHeader, (n) [ ( DrawingRevisionLabelEditor=( ( ?!isEditing: ?CanEdit: ⚠button→Button | [ ⚠input→FormField ⚠button→Button ] ) ), ?revision.ApprovalStatus: (3) Pill, ?revision.IsAmbiguous: Pill, [ ?revision.MetadataExtractedAt is…: Pill Pill ?revision.AnalysedAt is { } anal…: Pill Pill ] ) ?CanManage: ⚠button→Button ], Modal ) } }
```

### `/projects/{ProjectId}/documents/{DrawingId}`
`jpms/Pages/ProjectDrawingDetail.razor`

```
Page{ ProjectPageShell=( ( ?Projects.Current is null: LoadGate | ?Project is null: PageHeader | PageHeader{ ?Neighbour(-1) is { } previous: ⚠button→Button, ?Neighbour(1) is { } next: ⚠button→Button, ProjectStageBadge } ) ){ LoadGate{ ( ?drawing is null: ?DrawingStore.DrawingsFailedFor(…: Notice | ( ( DrawingDetailsEditor=( ( ?!isEditing: [ ⚠h2→SectionHeader ?CanEdit: ⚠button→Button ] | ( (2) FormField, ?error is not null: Notice ) ) ), [ ActionIcon ?CanManage: ( ⚠select→FormField, ?moveConfirmation is not null: Pill ) ], ?moveError is not null: Notice ), ?deleteError is not null: Notice, ?extractError is not null: Notice, Modal, ?isUploading: DrawingRevisionUploadForm=( FormField{ InputFile }, (2) FormField, ?error is not null: Notice ), LoadGate{ ( ?!RevisionsReady && RevisionsFai…: Notice | ( [ ( ?IsPreviewingOlderRevision: Notice, ( ?PreviewRevision is null: EmptyState | ?IsPdf(PreviewRevision): PdfViewer=( Toolbar{ ?pageCount > 1: ( (2) ToolbarButton, ToolbarDivider ), (4) ToolbarButton, ToolbarDivider, (2) ToolbarButton, ?DownloadUrl is not null: ( ToolbarDivider, ToolbarButton ) }, LoadGate ) | ?IsImage(PreviewRevision): ImageViewer=( Toolbar{ (5) ToolbarButton, ToolbarDivider, (2) ToolbarButton, ?DownloadUrl is not null: ( ToolbarDivider, ToolbarButton ) }, LoadGate ) ) ) DrawingRevisionList=( ⚠h2→SectionHeader, (n) [ ( DrawingRevisionLabelEditor=( ( ?!isEditing: ?CanEdit: ⚠button→Button | [ ⚠input→FormField ⚠button→Button ] ) ), ?revision.ApprovalStatus: (3) Pill, ?revision.IsAmbiguous: Pill, [ ?revision.MetadataExtractedAt is…: Pill Pill ?revision.AnalysedAt is { } anal…: Pill Pill ] ) ?CanManage: ⚠button→Button ], Modal ) ], DrawingExtractionPanel=( ?RevisionId is not null: Panel{ ( ?loadFailed: Notice | ?view is null: EmptyState | ( ?extraction.Status == DrawingExt…: Notice | ( ( ?structure is not null: ( (n) Notice, [ (5) StatTile ], ⚠dl→KeyValueList, ?structure.Revisions.Count > 0: ( SectionHeader, ⚠table→RecordsTable ), ?dimensionsOpen: ⚠table→RecordsTable, ?calloutsOpen: ?structure.Callouts.Count == 0: EmptyState, ?shapesOpen: ⚠table→RecordsTable ) | Notice ), ?markupsOpen: ⚠table→RecordsTable ) ) ) } ) ) ) } ) ) } } }
```

### `/projects/{ProjectId}/drawings`
`jpms/Pages/LegacyDrawingsRedirect.razor`

```
LoadGate
```

### `/projects/{ProjectId}/drawings/ambiguous`
`jpms/Pages/LegacyDrawingsRedirect.razor`

```
LoadGate
```

### `/projects/{ProjectId}/drawings/{DrawingId}`
`jpms/Pages/LegacyDrawingsRedirect.razor`

```
LoadGate
```

### `/projects/{ProjectId}/financials`
`jpms/Pages/ProjectFinancials.razor`

```
Page{ ProjectPageShell=( ( ?Projects.Current is null: LoadGate | ?Project is null: PageHeader | PageHeader{ ?Neighbour(-1) is { } previous: ⚠button→Button, ?Neighbour(1) is { } next: ⚠button→Button, ProjectStageBadge } ) ){ ⚠h2→SectionHeader, ( ?CostCenters.Current.Count == 0: EmptyState | ( ?Summary.LastRefreshFailed(Proje…: Notice{ ⚠button→Button }, ?actionError is not null: Notice, ?PendingLabourTotal > 0m: Notice, FinancialsTable=( [ ⚠input→FormField ExportToExcelButton ], ⚠table→RecordsTable ), PackageReconciliationSection=( SectionHeader{ ExportToExcelButton }, ?error is not null: Notice, ( ?rows.Count == 0: EmptyState | RecordsTable{ (n) ( ?row.IsLocked: Pill, ?CanManage: ( ?!row.IsLocked: ( (2) ⚠button→Button, ( ?pendingDeleteId == row.Reconcil…: (2) ⚠button→Button | ⚠button→Button ) ) | ⚠button→Button ) ) } ), ⚠button→Button, ManualWorkOrderModal=( Modal{ WorkOrderForm=( ?error is not null: Notice, [ (2) FormField ], FormField, [ (2) FormField ], FormField, (n) ( [ SearchSelect (5) ⚠input→FormField ⚠button→Button ], ⚠textarea→FormField ), ⚠button→Button, ?depositRequired: ⚠input→FormField, ?!IsEditing: ?!saveAsDraft: ( ?SelectedSupplier is { } supplie…: Notice | Notice ) ), InputFile, ?existingAttachments.Count > 0: (n) ⚠button→Button, ?stagedAttachmentFiles.Count > 0: (n) ⚠button→Button, ?!IsEditing && !FormSaveAsDraft: ?createPackage: ( FormField, SearchInput, (n) ?picked: ⚠input→FormField ) }, WorkOrderSaleWarningDialog=( Modal ) ), ReconciliationPackageBuilderModal=( Modal{ FormField, [ SearchInput ( SearchInput, (n) ?picked: ⚠input→FormField ) ], SearchInput, ( ?FilteredCostLines.Count == 0: EmptyState | (n) ?picked: ⚠input→FormField ) } ) ), CostCentreSalesLinesModal=( Modal{ ( ?CountingLines.Count == 0 && Non…: EmptyState | ( ExportToExcelButton, ⚠table→RecordsTable, ?NonCountingLines.Count > 0: ⚠table→RecordsTable ) ) } ), CostCentreWorkOrdersModal=( Modal{ ( ?Entries.Count == 0: EmptyState | ⚠table→RecordsTable ) } ), CostCentreReconciliationModal=( Modal{ LoadGate{ ( ⚠h3→SectionHeader, RecordsTable, ⚠h3→SectionHeader, RecordsTable{ (n) Pill }, ⚠h3→SectionHeader, RecordsTable, ⚠table→RecordsTable ) } } ), CostCentreCostOfSalesModal=( Modal{ ( ?entries.Count == 0: EmptyState | ( ⚠table→RecordsTable, ?AttributionEntries.Count > 0: ⚠table→RecordsTable ) ) }, WorkOrderLinkSplitModal=( Modal{ (n) [ ⚠select→FormField ⚠input→FormField ⚠button→Button ], ⚠button→Button } ) ), Modal{ ⚠input→FormField } ) ) } }
```

### `/projects/{ProjectId}/financials-setup`
`jpms/Pages/ProjectFinancialsSetup.razor`

```

```

### `/projects/{ProjectId}/hs`
`jpms/Pages/ProjectHs.razor`

```
Page{ ProjectPageShell=( ( ?Projects.Current is null: LoadGate | ?Project is null: PageHeader | PageHeader{ ?Neighbour(-1) is { } previous: ⚠button→Button, ?Neighbour(1) is { } next: ⚠button→Button, ProjectStageBadge } ) ){ SectionHeader, TabRow, ?actionError is { } error: Notice, ( ?Pane == AuditsPane: RecordsTable{ (n) ( ?audit.Rating is { } rating: Pill, Pill ) } | RecordsTable{ (n) ( Pill, ⚠select→FormField ) } ), Modal{ [ (4) FormField ] }, Modal{ [ (4) FormField ], FormField } } }
```

### `/projects/{ProjectId}/hs/audits/{HsAuditId}`
`jpms/Pages/ProjectHsAudit.razor`

```
Page{ ProjectPageShell=( ( ?Projects.Current is null: LoadGate | ?Project is null: PageHeader | PageHeader{ ?Neighbour(-1) is { } previous: ⚠button→Button, ?Neighbour(1) is { } next: ⚠button→Button, ProjectStageBadge } ) ){ LoadGate{ PageHeader{ Pill, ( ?LiveScore is { } score: Pill | Pill ), Toolbar{ ToolbarButton } }, ?actionError is { } error: Notice, ?HasUnsavedChanges: Notice, Panel{ [ (8) FormField ], [ (2) FormField ] }, Notice, (n) HsAuditSectionPanel=( Panel{ ⚠table→RecordsTable } ), ConfirmDialog, Modal{ FormField } } } }
```

### `/projects/{ProjectId}/inventory`
`jpms/Pages/ProjectInventory.razor`

```
Page{ ProjectPageShell=( ( ?Projects.Current is null: LoadGate | ?Project is null: PageHeader | PageHeader{ ?Neighbour(-1) is { } previous: ⚠button→Button, ?Neighbour(1) is { } next: ⚠button→Button, ProjectStageBadge } ) ){ SectionHeader, ?actionError is { } error: Notice, ?formOpen: ( ⚠h3→SectionHeader, [ (2) FormField ], (2) FormField ), ( ?!Inventory.LoadedFor(ProjectId)…: LoadGate | ?Rows.Count == 0: EmptyState | RecordsTable{ (n) ( (2) ⚠button→Button, ?isExpanded: RecordCorrespondenceSection=( RecordCorrespondencePanel=( [ ⚠h3→SectionHeader [ ?Loaded && Emails.Count > 0: Pill EmailFinder=( ⚠button→Button, Modal{ ⚠input→FormField, ?error is not null: Notice, ?results is not null: (n) ( ?email.HasAttachments: Pill, [ ?alreadyTagged: Pill (n) Pill ] ) } ) ] ], ?Error is not null: Notice, LoadGate{ ?sentNote is not null: ⚠button→Button, ?replyingTo is { } anchor: MailReplyComposer=( ⚠button→Button, ?error is not null: Notice, FormField{ RecipientInput }, ?!showBcc: ⚠button→Button, RecipientInput, ?showBcc: FormField{ RecipientInput }, FormField, RichTextEditor, AttachmentPicker ), CorrespondenceThreadList } ) ) ) } ) } }
```

### `/projects/{ProjectId}/labour`
`jpms/Pages/ProjectLabour.razor`

```
Page{ ProjectPageShell=( ( ?Projects.Current is null: LoadGate | ?Project is null: PageHeader | PageHeader{ ?Neighbour(-1) is { } previous: ⚠button→Button, ?Neighbour(1) is { } next: ⚠button→Button, ProjectStageBadge } ) ){ ?actionError is not null: Notice, ?approvalFailures.Count > 0: ApprovalFailuresBanner=( Notice ), SectionHeader{ Toolbar{ ExportToExcelButton } }, LoadGate{ Panel{ ( FormField, ( ⚠table→RecordsTable, TimesheetApprovalFooter=( SearchSelect ) ) ) }, [ Panel{ ⚠select→FormField } SiteRegisterPanel=( Panel{ ⚠table→RecordsTable } ) ], Panel{ SettlementSummaryTable=( ⚠table→RecordsTable ), ( ?ledgerLines is null: LoadGate | ?ProjectLedgerLines().Count == 0: EmptyState | CoverInvoiceLinesTable=( ⚠table→RecordsTable ) ) } } }, Modal{ (2) FormField, FormField{ SearchSelect }, FormField, ?manualError is not null: Notice }, Modal{ ⚠input→FormField, ?rejectError is not null: Notice }, Modal{ ⚠input→FormField, ?correctionError is not null: Notice }, Modal{ FormField{ SearchSelect }, ⚠input→FormField, ?moveBudgetBlock is not null: Notice{ Checkbox }, ?correctionError is not null: Notice }, Modal{ Notice, ⚠input→FormField, ?overBudgetError is not null: Notice } }
```

### `/projects/{ProjectId}/operations-setup`
`jpms/Pages/ProjectOperationsSetup.razor`

```

```

### `/projects/{ProjectId}/programme`
`jpms/Pages/ProjectProgramme.razor`

```
Page{ ProjectPageShell=( ( ?Projects.Current is null: LoadGate | ?Project is null: PageHeader | PageHeader{ ?Neighbour(-1) is { } previous: ⚠button→Button, ?Neighbour(1) is { } next: ⚠button→Button, ProjectStageBadge } ) ){ ⚠h2→SectionHeader, [ (4) ⚠button→Button ], ( ?view == SubView.Programme: ProgrammeWorkbench=( ?error is not null: Notice, LoadGate{ ?error is null: ( ( ?programme?.Baseline is { } base…: ( ?movement.CompletionSlipDays > 0: Notice{ (n) ⚠button→Button } | Notice ) | ?Tasks.Count > 0: Notice ), ?draft is { } openDraft: Notice, ( ?openForm == Form.AddTask: [ (3) FormField ] | ?openForm == Form.AddLink: [ (3) FormField ] | ?openForm == Form.Baseline: ( FormField, ?Baselines.Count > 0: (n) [ ?baselineEntry.ProgrammeBaseline…: Pill ?confirmingRemoveBaselineId == b…: (2) ⚠button→Button ⚠button→Button ] ) | ?openForm == Form.DraftFromValua…: FormField ) ), ProgrammeDraftReview=( ?error is not null: Notice, SectionHeader{ ( ?CanEdit: ( Toolbar{ ToolbarButton }, InlineConfirm ) | Pill ) }, ?!string.IsNullOrWhiteSpace(Deta…: Notice, LoadGate{ RecordsTable{ (n) ( Checkbox, [ (n) Pill ?CanEdit: ⚠button→Button ], ⚠input→FormField, Pill, ?mappingEditorLineId == row.Prog…: (n) Checkbox ) } }, ConfirmDialog ), ProgrammeGanttChart=( (n) ( [ ⚠button→Button ?slip > 0: Pill (n) Pill ], ?PredecessorsOf(task.ProgrammeTa…: (n) Pill{ ⚠button→Button }, (n) ProgrammePushBar, ?editingTaskId == task.Programme…: ProgrammeTaskEditor=( [ (4) FormField ] ) ), ProgrammeVariationRows=( ?Rows.Count == 0: EmptyState, (n) ( Pill, ?CanEdit: ⚠button→Button, (n) ProgrammePushBar, ?editingVariationId == row.Varia…: ProgrammeVariationEffectEditor ) ), ProgrammeExtensionRows=( ?Rows.Count == 0: EmptyState, (n) ( Pill, ?CanEdit: ⚠button→Button, ?editingRequestId == row.Eot.Req…: ProgrammeExtensionDaysEditor ) ) ) } ) | ?view == SubView.Claims: ProgrammeClaimsWorkbench=( ?(Error ?? error) is { } shown: Notice, ( ?openForm == Form.Nod: (2) FormField | ?openForm == Form.Eot: ( (2) FormField, [ (2) FormField ], FormField ) | ?openForm == Form.Lad: ( (2) FormField, [ (4) FormField ], FormField ) ), LoadGate{ ⚠h3→SectionHeader, (n) Pill, ⚠h3→SectionHeader, (n) [ ?RelatedNodReference(eot) is { }…: Pill Pill ], ⚠h3→SectionHeader, (n) Pill } ) | ?view == SubView.CriticalRfis: CriticalRfiList=( LoadGate{ (n) [ (2) Pill ] } ) | RelevantEventsList=( ?Error is not null: Notice, LoadGate{ (n) ( ?email.HasAttachments: Pill, [ ⚠button→Button ?CanDraftReply: ⚠button→Button ], ?replyForId == email.Id: ( ?replyError is not null: Notice, ⚠textarea→FormField ), ?replyDraftForId == email.Id && …: Notice ) } ) ) } }
```

### `/projects/{ProjectId}/progress`
`jpms/Pages/ProjectProgress.razor`

```
Page{ ProjectPageShell=( ( ?Projects.Current is null: LoadGate | ?Project is null: PageHeader | PageHeader{ ?Neighbour(-1) is { } previous: ⚠button→Button, ?Neighbour(1) is { } next: ⚠button→Button, ProjectStageBadge } ) ){ LoadGate{ ⚠h2→SectionHeader, ?isBuildingReport: ProgressReportForm=( FormField, [ (2) FormField ], (3) FormField, ( ?AvailableUpdates.Count == 0: EmptyState | (n) ⚠button→Button ), ?error is not null: Notice ), (n) ?CanContribute: ⚠button→Button, ⚠h2→SectionHeader, ?isRecording: ProgressUpdateForm=( FormField{ InputFile }, (4) FormField, [ (6) FormField ], ?error is not null: Notice ), ( ?Updates.Count == 0: EmptyState | (n) ( ?CanContribute: ⚠button→Button, [ (n) ?CanContribute: ⚠button→Button ?CanContribute: InputFile ] ) ) }, ?actionError is not null: Notice } }
```

### `/projects/{ProjectId}/progress/contractors-reports`
`jpms/Pages/ProjectProgressContractorsReports.razor`

```
Page{ ProjectPageShell=( ( ?Projects.Current is null: LoadGate | ?Project is null: PageHeader | PageHeader{ ?Neighbour(-1) is { } previous: ⚠button→Button, ?Neighbour(1) is { } next: ⚠button→Button, ProjectStageBadge } ) ){ SectionHeader, ?error is { } message: Notice, ?isOpening: ContractorsReportOpenForm=( [ (2) FormField ] ), RecordsTable } }
```

### `/projects/{ProjectId}/progress/contractors-reports/{ReportId}`
`jpms/Pages/ProjectProgressContractorsReport.razor`

```
Page{ ProjectPageShell=( ( ?Projects.Current is null: LoadGate | ?Project is null: PageHeader | PageHeader{ ?Neighbour(-1) is { } previous: ⚠button→Button, ?Neighbour(1) is { } next: ⚠button→Button, ProjectStageBadge } ) ){ SectionHeader, ?error is { } message: Notice, ?saved: Notice, LoadGate{ ?view is { } report && draft is …: ( ContractorsReportFindings=( ?Findings.Count > 0: Notice ), LoadGate{ [ ( Panel{ ContractorsReportHeaderFields=( [ (6) FormField ] ) }, Panel{ ContractorsReportUpdatePicker=( ( ?Updates.Count == 0: EmptyState | (n) Checkbox ) ) }, Panel{ ContractorsReportLookAheadEditor=( (n) [ Checkbox ⚠input→FormField ], ⚠input→FormField ) }, Panel{ ContractorsReportNarrativeFields=( (3) FormField ) }, Panel{ ContractorsReportAttendanceTable=( RecordsTable{ (n) ( ⚠input→FormField, Checkbox ) } ) } ) Panel{ ContractorsReportPreview=( (9) ⚠h3→SectionHeader ) } ] } ) }, ConfirmDialog } }
```

### `/projects/{ProjectId}/progress/whatsapp-week`
`jpms/Pages/ProjectProgressWhatsAppWeek.razor`

```
Page{ ProjectPageShell=( ( ?Projects.Current is null: LoadGate | ?Project is null: PageHeader | PageHeader{ ?Neighbour(-1) is { } previous: ⚠button→Button, ?Neighbour(1) is { } next: ⚠button→Button, ProjectStageBadge } ) ){ SectionHeader, ?error is { } message: Notice, ( ?applied is { } written: Notice | LoadGate{ WhatsAppWeekExportForm=( [ FormField FormField{ InputFile } ], FormField ), ?preview is { } week: WhatsAppWeekReview=( (n) Notice, SectionHeader, RecordsTable, WhatsAppUnattributedMessages=( ?Messages.Count > 0: ( Notice, RecordsTable ) ), ?Preview.RepeatedPhotos.Count > 0: Notice, ?Preview.MissingMediaFileNames.C…: Notice ) } ), ConfirmDialog } }
```

### `/projects/{ProjectId}/reconciliation-audit`
`jpms/Pages/ProjectReconciliationAudit.razor`

```
Page{ ( ?!CanAccess: PageHeader | ProjectPageShell=( ( ?Projects.Current is null: LoadGate | ?Project is null: PageHeader | PageHeader{ ?Neighbour(-1) is { } previous: ⚠button→Button, ?Neighbour(1) is { } next: ⚠button→Button, ProjectStageBadge } ) ){ ⚠h2→SectionHeader, ?loadError is not null: Notice, LoadGate{ RecordsTable } } ) }
```

### `/projects/{ProjectId}/requests`
`jpms/Pages/ProjectRequests.razor`

```
Page{ ProjectPageShell=( ( ?Projects.Current is null: LoadGate | ?Project is null: PageHeader | PageHeader{ ?Neighbour(-1) is { } previous: ⚠button→Button, ?Neighbour(1) is { } next: ⚠button→Button, ProjectStageBadge } ) ){ [ ⚠h2→SectionHeader ExportToExcelButton ], Notice, [ (n) ⚠button→Button ( SearchInput, ?Searching: ⚠button→Button ) ], ?Searching: ?HiddenByStatusCount > 0: ⚠button→Button, ?mergeError is not null: Notice, ?draftBatchError is not null: Notice, ( ?statusError is not null: Notice, RequestTable=( RecordsTable{ (n) ( ?record.CriticalPath: Pill, ?record.MergedIntoRequestId is n…: Pill, ActivityBadge=( ?Activity is { } summary && summ…: Pill ), ( ?CanChangeStatus: DropdownMenu | Pill ) ) } ) ) }, RaiseRequestDialog=( Modal{ RequestForm=( ?error is not null: Notice, [ (2) ⚠button→Button ], ?kind == RequestType.Rfi: FormField, (2) FormField, [ (2) FormField ], FormField, ?ShowAttachments: ( InputFile, ?selectedRevisions.Count > 0 || …: (2) (n) ⚠button→Button ), ?backfill: ( [ (2) FormField ], (2) FormField ), Modal ), ?attachmentWarning is not null: Notice } ) }
```

### `/projects/{ProjectId}/requests/variations`
`jpms/Pages/ProjectVariations.razor`

```
Page{ ProjectPageShell=( ( ?Projects.Current is null: LoadGate | ?Project is null: PageHeader | PageHeader{ ?Neighbour(-1) is { } previous: ⚠button→Button, ?Neighbour(1) is { } next: ⚠button→Button, ProjectStageBadge } ) ){ [ ⚠h2→SectionHeader ExportToExcelButton ], Notice, [ FilterChips ( SearchInput, ?Searching: ⚠button→Button ) ], ?Searching: ?HiddenByStatusCount > 0: ⚠button→Button, ?variationsError is not null: Notice, ?OpenRequests.Count > 0 || Revie…: ( ⚠h3→SectionHeader, ?requestError is not null: Notice, (n) ?rejectingRequestId == variation…: [ ⚠input→FormField ⚠button→Button ] ), ?requestError is not null && Ope…: Notice, ( ?FilteredRows.Count == 0: ?FilteringByStatus: ⚠button→Button | ( ?variationStatusError is not null: Notice, RecordsTable{ (n) ( ActivityBadge=( ?Activity is { } summary && summ…: Pill ), ( ?statusChoices.Count > 0: DropdownMenu | Pill ), ?CanIssueWorkOrder(order): ⚠button→Button ) } ) ) }, AddManualVariationDialog=( Modal{ ManualVariationForm=( ?error is not null: Notice, (3) FormField, ⚠h3→SectionHeader, VariationApprovePanel=( ?ShowHeading: ⚠h3→SectionHeader, ?error is not null || SubmitErro…: Notice, (n) ( SearchSelect, (3) ⚠input→FormField, ⚠button→Button ), ⚠button→Button ), (3) FormField ) } ), ?decliningVariation is { } decli…: Modal{ ?variationStatusError is not null: Notice } }
```

### `/projects/{ProjectId}/requests/view/{RequestId}`
`jpms/Pages/ProjectRequestDetail.razor`

```
Page{ ( ?!dataLoaded: ProjectPageShell=( ( ?Projects.Current is null: LoadGate | ?Project is null: PageHeader | PageHeader{ ?Neighbour(-1) is { } previous: ⚠button→Button, ?Neighbour(1) is { } next: ⚠button→Button, ProjectStageBadge } ) ){ LoadGate } | ?record is null: ProjectPageShell=( ( ?Projects.Current is null: LoadGate | ?Project is null: PageHeader | PageHeader{ ?Neighbour(-1) is { } previous: ⚠button→Button, ?Neighbour(1) is { } next: ⚠button→Button, ProjectStageBadge } ) ) | ( ProjectPageShell=( ( ?Projects.Current is null: LoadGate | ?Project is null: PageHeader | PageHeader{ ?Neighbour(-1) is { } previous: ⚠button→Button, ?Neighbour(1) is { } next: ⚠button→Button, ProjectStageBadge } ) ){ RequestHeaderBar=( [ ( [ ?CanEdit: DropdownMenu Pill ?Record.ImpliesVariation: Pill ?Record.CriticalPath: Pill ], ⚠h2→SectionHeader ) DropdownMenu ] ), RecordTabBar, RequestFactsStrip=( [ (3) MetaCell ?Record.Status is RequestStatus.…: MetaCell (2) MetaCell ?Record.Kind is not RequestType.…: MetaCell MetaCell ] ), ?actionError is not null: Notice, ?ShowCriticalPathNudge: CriticalPathNudge=( Notice ), [ ( ?ShowContainerPane: Panel, ?ShowOfficialContent: ( RequestOfficialFormPanel=( [ ⚠h3→SectionHeader ?Record.Kind.IsEmailable(): Toolbar{ ToolbarButton, ?CanDraftEmail: ToolbarButton } ], ?error is not null: Notice, ( ?!Editing: ( ?Record.ItemList.Count == 0 && s…: EmptyState | ?Record.ItemList.Count > 0: ⚠table→RecordsTable ) | ( (n) ( ⚠button→Button, [ (2) FormField ], (2) FormField ), (3) FormField ) ) ), ?!string.IsNullOrWhiteSpace(reco…: RequestResponsePanel=( ⚠h3→SectionHeader ) ), ?ShowContainerPane: ( RequestAttachmentsPanel=( Panel{ ?CanEdit: InputFile, ?error is not null: Notice, (n) ?CanEdit: ⚠button→Button }, Modal{ ?pickerError is not null: Notice } ), RequestConversation=( Panel{ ?!string.IsNullOrWhiteSpace(Requ…: Jewel.JPMS.Features.Triage.Panels.EmailFinder, LoadGate{ (n) ( [ ?!string.IsNullOrEmpty(message.M…: Pill [ ?isInternal: Pill Pill ] ], ?string.IsNullOrEmpty(message.Ma…: ⚠button→Button, ?replies.Count > 0: (n) ConversationMessageCard=( ?ShowVisibility && IsInternal: Pill, ?CanReply: ⚠button→Button, ?Entry.Replies.Count > 0: (n) ConversationMessageCard ), ?!string.IsNullOrEmpty(message.M…: ( [ ⚠button→Button ?CanDraftReply: (2) ⚠button→Button ], ?replyForMessageId == message.Me…: Notice, ?replyForMessageId == message.Me…: Notice ) ), ?error is not null: Notice, ?replyToId is not null: ⚠button→Button, ⚠textarea→FormField } } ), ?!HasOfficialTab: RecordAuditHistory=( Panel ) ) ) ( ?ShowContainerPane && HasOfficia…: RecordAuditHistory=( Panel ), ?ShowOfficialContent: ( ?!record.Kind.IsEmailable() || C…: RequestPartyPanel=( ⚠h3→SectionHeader, ?CanEdit: ( ?Record.HasRfq: Pill, ?Error is not null: Notice, FormField, ?Record.PartyKind == PartyKind.A…: FormField ) ), ?record.Kind is RequestType.Rfi …: RequestVariationCard=( ⚠h3→SectionHeader, ?Error is not null: Notice ) ) ) ] }, (2) Modal, Modal{ FormField, ?closeError is not null: Notice }, EmailDraftStagingModal=( Modal{ ( ?DraftResult is null: ( RecordEmailPreviewPanel=( ?Error is not null: Notice, ⚠dl→KeyValueList ), LoadGate, ?Error is not null: Notice ) | Notice ) } ), Modal{ ⚠textarea→FormField, ?actionError is not null: Notice }, RequestHeaderEditModal=( Modal{ ?error is not null: Notice, [ (2) FormField ], FormField, ?status == RequestStatus.Closed: FormField } ), RequestFactsEditModal=( Modal{ ?error is not null: Notice, [ (2) FormField ?IsClosed: FormField ], (2) FormField, ?Record.Kind is not RequestType.…: FormField } ), RequestDetailEditModal=( Modal{ ?error is not null: Notice, FormField } ), VariationDraftModal=( Modal{ ?Error is not null: Notice, (3) FormField, ?Lines.Count > 0: (n) ?row.Accepted: ⚠select→FormField } ) ) ) }
```

### `/projects/{ProjectId}/requests/{Kind}`
`jpms/Pages/ProjectRequests.razor`

```
Page{ ProjectPageShell=( ( ?Projects.Current is null: LoadGate | ?Project is null: PageHeader | PageHeader{ ?Neighbour(-1) is { } previous: ⚠button→Button, ?Neighbour(1) is { } next: ⚠button→Button, ProjectStageBadge } ) ){ [ ⚠h2→SectionHeader ExportToExcelButton ], Notice, [ (n) ⚠button→Button ( SearchInput, ?Searching: ⚠button→Button ) ], ?Searching: ?HiddenByStatusCount > 0: ⚠button→Button, ?mergeError is not null: Notice, ?draftBatchError is not null: Notice, ( ?statusError is not null: Notice, RequestTable=( RecordsTable{ (n) ( ?record.CriticalPath: Pill, ?record.MergedIntoRequestId is n…: Pill, ActivityBadge=( ?Activity is { } summary && summ…: Pill ), ( ?CanChangeStatus: DropdownMenu | Pill ) ) } ) ) }, RaiseRequestDialog=( Modal{ RequestForm=( ?error is not null: Notice, [ (2) ⚠button→Button ], ?kind == RequestType.Rfi: FormField, (2) FormField, [ (2) FormField ], FormField, ?ShowAttachments: ( InputFile, ?selectedRevisions.Count > 0 || …: (2) (n) ⚠button→Button ), ?backfill: ( [ (2) FormField ], (2) FormField ), Modal ), ?attachmentWarning is not null: Notice } ) }
```

### `/projects/{ProjectId}/settings`
`jpms/Pages/ProjectSettings.razor`

```
Page{ ProjectPageShell=( ( ?Projects.Current is null: LoadGate | ?Project is null: PageHeader | PageHeader{ ?Neighbour(-1) is { } previous: ⚠button→Button, ?Neighbour(1) is { } next: ⚠button→Button, ProjectStageBadge } ) ){ ?Project is not null: ( (n) ⚠button→Button, ?activePane: ( ProjectDetailsEditor=( ?CanEdit: Modal{ ?error is not null: Notice, (4) FormField, ?partySelection.StartsWith(Archi…: FormField, [ (2) FormField ], (2) FormField, [ (2) FormField ], (3) FormField, ?CanDelete: ( FormField, ⚠button→Button ) } ), [ (8) StatTile ], ProjectContractPanel=( Panel{ ( ?dataFailed: Notice | ( ?CanManage: ( InputFile, ?uploadError is not null: Notice ), (n) ?CanManage: (2) ⚠button→Button, ?CanManage: ( [ (2) FormField ], FormField{ InputFile }, ?amendmentError is not null: Notice ), ( ?terms.IsAmended: Notice, ⚠dl→KeyValueList ) ) ) }, ?termsOpen: ProjectContractTermsDialog=( Modal{ [ (2) FormField ], FormField, [ (6) FormField ], [ (2) FormField ], (2) [ (3) FormField ], [ (4) FormField ], [ (6) FormField ] } ), ?editingAmendment is not null: ProjectContractAmendmentDialog=( Modal{ (3) FormField } ), Modal{ ?removeError is not null: Notice } ), NextValuationDateEditor=( Modal{ ?error is not null: Notice, FormField, ?calculate: ( [ (2) FormField ], ?snapToWeekday: [ (2) FormField ] ), ?CurrentDate is not null: ⚠button→Button } ), ProjectRetentionPanel=( ⚠h3→SectionHeader, Modal{ ?error is not null: Notice, (3) [ (2) FormField ] }, Modal{ ?error is not null: Notice, FormField } ), ProjectCorrespondencePanel=( [ ⚠h3→SectionHeader ?loading: JewelIcon ], ?error is not null: Notice, ( ?partyContacts.Count > 0: ⚠table→RecordsTable, ⚠table→RecordsTable, ?CanManage: ( [ (3) FormField ], [ (2) FormField ?editingContactId is not null: ⚠button→Button ] ) ) ) ) ) } }
```

### `/projects/{ProjectId}/setup`
`jpms/Pages/ProjectSetup.razor`

```

```

### `/projects/{ProjectId}/site-instructions`
`jpms/Pages/ProjectSiteInstructions.razor`

```
Page{ ProjectPageShell=( ( ?Projects.Current is null: LoadGate | ?Project is null: PageHeader | PageHeader{ ?Neighbour(-1) is { } previous: ⚠button→Button, ?Neighbour(1) is { } next: ⚠button→Button, ProjectStageBadge } ) ){ SectionHeader, ?actionError is { } error: Notice, ?formOpen: ( ⚠h3→SectionHeader, [ (2) FormField ], FormField ), ( ?!SiteInstructions.LoadedFor(Pro…: LoadGate | ?Rows.Count == 0: EmptyState | RecordsTable{ (n) ( (2) ⚠button→Button, ?isExpanded: RecordCorrespondenceSection=( RecordCorrespondencePanel=( [ ⚠h3→SectionHeader [ ?Loaded && Emails.Count > 0: Pill EmailFinder=( ⚠button→Button, Modal{ ⚠input→FormField, ?error is not null: Notice, ?results is not null: (n) ( ?email.HasAttachments: Pill, [ ?alreadyTagged: Pill (n) Pill ] ) } ) ] ], ?Error is not null: Notice, LoadGate{ ?sentNote is not null: ⚠button→Button, ?replyingTo is { } anchor: MailReplyComposer=( ⚠button→Button, ?error is not null: Notice, FormField{ RecipientInput }, ?!showBcc: ⚠button→Button, RecipientInput, ?showBcc: FormField{ RecipientInput }, FormField, RichTextEditor, AttachmentPicker ), CorrespondenceThreadList } ) ) ) } ) } }
```

### `/projects/{ProjectId}/todos`
`jpms/Pages/ProjectTodos.razor`

```
Page{ ProjectPageShell=( ( ?Projects.Current is null: LoadGate | ?Project is null: PageHeader | PageHeader{ ?Neighbour(-1) is { } previous: ⚠button→Button, ?Neighbour(1) is { } next: ⚠button→Button, ProjectStageBadge } ) ){ ProjectTodoList=( [ ⚠h2→SectionHeader ?!loading: [ (2) ⚠button→Button ] ], ?error is not null: Notice, LoadGate{ [ ( SearchInput, ?HasQuery: ⚠button→Button ) ⚠select→FormField ], ( ( ?boardView: TodoBoard=( (n) ( ⚠h3→SectionHeader, (n) ⚠button→Button ) ) | (n) ⚠button→Button ), ?!boardView && done.Count > 0: ( ⚠h3→SectionHeader, (n) ⚠button→Button ) ), ?HasQuery: ( ⚠h3→SectionHeader, ( ?matchedRequests.Count > 0: (n) ⚠button→Button, ?matchedVariations.Count > 0: (n) ⚠button→Button, ?matchedDrawings.Count > 0: (n) ⚠button→Button ), TaggedEmailSearch ) }, Modal{ ?addError is not null: Notice, FormField, [ FormField{ TodoAssigneeSelect=( SearchSelect, ?ShowPeople: SearchSelect ) } FormField ], FormField } ) } }
```

### `/projects/{ProjectId}/useful-information`
`jpms/Pages/ProjectUsefulInformation.razor`

```
Page{ ProjectPageShell=( ( ?Projects.Current is null: LoadGate | ?Project is null: PageHeader | PageHeader{ ?Neighbour(-1) is { } previous: ⚠button→Button, ?Neighbour(1) is { } next: ⚠button→Button, ProjectStageBadge } ) ){ UsefulInformationPanel=( ⚠h2→SectionHeader, ?error is not null: Notice, LoadGate{ ( ?notes is { Count: 0 }: EmptyState | ?notes is not null: ( ?notes.Count > FilterThreshold: ( SearchInput, ?HasFilter: ⚠button→Button ), (n) ⚠button→Button ) ) }, Modal{ ?dialogError is not null: Notice, (2) FormField } ) } }
```

### `/projects/{ProjectId}/valuation`
`jpms/Pages/ProjectValuation.razor`

```
Page{ ProjectPageShell=( ( ?Projects.Current is null: LoadGate | ?Project is null: PageHeader | PageHeader{ ?Neighbour(-1) is { } previous: ⚠button→Button, ?Neighbour(1) is { } next: ⚠button→Button, ProjectStageBadge } ) ){ SectionHeader{ Toolbar{ ToolbarButton, ExportToExcelButton, ?CanMapClientReferences: ( ToolbarDivider, ToolbarButton ) }, ( ?!ClaimReady: ⚠select→FormField | ?Claims.Count > 0: ⚠select→FormField ) }, ?actionError is not null: Notice{ ⚠button→Button }, ?xeroRaiseNote is not null: Notice, ValuationInvoiceXeroRaiseModal=( Modal{ ?error is not null: Notice, [ (2) FormField ], LoadGate{ ?preview is not null: ( LoadGate{ (n) Notice, ValuationInvoiceXeroRaiseSummary=( ⚠dl→KeyValueList ) }, FormField ) } } ), ?programmeDraftOpened is { } pro…: Notice, LoadGate{ ClaimProgressDialog=( Modal{ ?!ClaimIsDraft: Notice, ?error is not null: Notice, (n) ( ⚠input→FormField, ⚠button→Button ), SearchSelect } ), ?Selected is { } s: [ Pill [ ?s.IsLocked: Toolbar{ (2) ToolbarButton, ?CanEmailStatement: ToolbarButton } ?CanManageClaims: DropdownMenu ] ], ValuationReportTable=( ( ?CanEditEntries: BulkPercentToolbar, (n) ( ValuationSectionHeader=( ⚠button→Button ), ?isOpen: ⚠table→RecordsTable ) ), ?lines.Count > 0: ValuationSummaryPanel=( ⚠h3→SectionHeader, ⚠dl→KeyValueList ) ){ ValuationInvoicesSection=( [ ⚠button→Button Toolbar{ ?CanManage: ( ToolbarButton, ToolbarDivider ), ExportToExcelButton } ], ?isOpen: ( ?xeroRaiseNote is not null: Notice, ⚠table→RecordsTable, ?CanManage: [ (2) FormField ?newIsHistoric: (2) FormField ] ), ValuationInvoiceXeroRaiseModal=( Modal{ ?error is not null: Notice, [ (2) FormField ], LoadGate{ ?preview is not null: ( LoadGate{ (n) Notice, ValuationInvoiceXeroRaiseSummary=( ⚠dl→KeyValueList ) }, FormField ) } } ), ValuationInvoicePaymentSyncModal=( Modal{ ?error is not null: Notice, LoadGate{ ?preview is not null: ( (n) Notice, ?preview.Blockers.Count == 0: LoadGate{ RecordsTable{ (n) Pill } } ) } } ), (3) Modal{ FormField }, Modal{ ?editInvoice is not null: ( (2) FormField, ?editInvoice.IsManual: FormField, FormField ) } ), ValuationClaimCorrespondenceSection=( ⚠button→Button, ( ?isOpen: ( ?Claim is null: EmptyState | LoadGate{ ?loadError is not null: Notice } ) | ?emails is not null: CorrespondenceThreadList ) ) } }, Modal{ ValuationLineForm=( [ (2) FormField ], [ ?elementType == ValuationElement…: (2) FormField (2) FormField ], [ FormField{ SearchSelect } (2) FormField ], [ (3) FormField ], FormField ) }, ?showClientReferences: ClientCostReferencesModal=( Modal{ LoadGate{ EmptyState, ⚠table→RecordsTable } } ), Modal{ ?viewingStatementClaimId is not …: ValuationStatementViewer=( ( ( ?detail.IsDraft: Notice | Notice ), Toolbar{ ToolbarButton, ExportToExcelButton, ?OnEmailRequested.HasDelegate &&…: ( ToolbarDivider, ToolbarButton ) }, (n) ?valuationSection.Lines.Count > 0: ⚠table→RecordsTable ), ⚠h3→SectionHeader, ⚠dl→KeyValueList, ⚠h3→SectionHeader, LoadGate{ ?emailsError is not null: Notice, CorrespondenceThreadList } ) }, ValuationStatementEmailModal=( Modal{ ?Claim is { } snapshot: ( RecordsTable, ( ?drafted: Notice | ( RecordEmailPreviewPanel=( ?Error is not null: Notice, ⚠dl→KeyValueList ), FormField, FormField{ RichTextEditor } ) ), ?error is not null: Notice ) } ), Modal{ (2) FormField }, Modal{ FormField }, (2) Modal } }
```

### `/projects/{ProjectId}/valuation-snapshots`
`jpms/Pages/LegacyValuationSnapshotsRedirect.razor`

```
LoadGate
```

### `/projects/{ProjectId}/variations`
`jpms/Pages/ProjectVariations.razor`

```
Page{ ProjectPageShell=( ( ?Projects.Current is null: LoadGate | ?Project is null: PageHeader | PageHeader{ ?Neighbour(-1) is { } previous: ⚠button→Button, ?Neighbour(1) is { } next: ⚠button→Button, ProjectStageBadge } ) ){ [ ⚠h2→SectionHeader ExportToExcelButton ], Notice, [ FilterChips ( SearchInput, ?Searching: ⚠button→Button ) ], ?Searching: ?HiddenByStatusCount > 0: ⚠button→Button, ?variationsError is not null: Notice, ?OpenRequests.Count > 0 || Revie…: ( ⚠h3→SectionHeader, ?requestError is not null: Notice, (n) ?rejectingRequestId == variation…: [ ⚠input→FormField ⚠button→Button ] ), ?requestError is not null && Ope…: Notice, ( ?FilteredRows.Count == 0: ?FilteringByStatus: ⚠button→Button | ( ?variationStatusError is not null: Notice, RecordsTable{ (n) ( ActivityBadge=( ?Activity is { } summary && summ…: Pill ), ( ?statusChoices.Count > 0: DropdownMenu | Pill ), ?CanIssueWorkOrder(order): ⚠button→Button ) } ) ) }, AddManualVariationDialog=( Modal{ ManualVariationForm=( ?error is not null: Notice, (3) FormField, ⚠h3→SectionHeader, VariationApprovePanel=( ?ShowHeading: ⚠h3→SectionHeader, ?error is not null || SubmitErro…: Notice, (n) ( SearchSelect, (3) ⚠input→FormField, ⚠button→Button ), ⚠button→Button ), (3) FormField ) } ), ?decliningVariation is { } decli…: Modal{ ?variationStatusError is not null: Notice } }
```

### `/projects/{ProjectId}/variations/{VariationOrderId}`
`jpms/Pages/ProjectVariationDetail.razor`

```
Page{ ( ?!orderLoaded: ProjectPageShell=( ( ?Projects.Current is null: LoadGate | ?Project is null: PageHeader | PageHeader{ ?Neighbour(-1) is { } previous: ⚠button→Button, ?Neighbour(1) is { } next: ⚠button→Button, ProjectStageBadge } ) ){ LoadGate } | ?order is null: ProjectPageShell=( ( ?Projects.Current is null: LoadGate | ?Project is null: PageHeader | PageHeader{ ?Neighbour(-1) is { } previous: ⚠button→Button, ?Neighbour(1) is { } next: ⚠button→Button, ProjectStageBadge } ) ) | ProjectPageShell=( ( ?Projects.Current is null: LoadGate | ?Project is null: PageHeader | PageHeader{ ?Neighbour(-1) is { } previous: ⚠button→Button, ?Neighbour(1) is { } next: ⚠button→Button, ProjectStageBadge } ) ){ VariationHeaderBar=( [ ( [ ?StatusMenuItems.Count > 0: DropdownMenu Pill ], ⚠h2→SectionHeader ) DropdownMenu ] ), RecordTabBar, ?error is not null: Notice, ?string.IsNullOrWhiteSpace(order…: Notice, [ ( VariationDocumentPanel=( ⚠h3→SectionHeader, ?error is not null: Notice, (3) FormField ), ?ApprovedOrder is not null && Va…: VariationLinesTable=( ⚠h3→SectionHeader, ⚠table→RecordsTable ), VariationConversation=( Panel{ LoadGate{ (n) ConversationMessageCard=( ?ShowVisibility && IsInternal: Pill, ?CanReply: ⚠button→Button, ?Entry.Replies.Count > 0: (n) ConversationMessageCard ), ConversationComposer=( ?error is not null: Notice, ?ReplyingToAuthor is not null: ⚠button→Button, ⚠textarea→FormField ) } } ), RecordCorrespondencePanel=( [ ⚠h3→SectionHeader [ ?Loaded && Emails.Count > 0: Pill EmailFinder=( ⚠button→Button, Modal{ ⚠input→FormField, ?error is not null: Notice, ?results is not null: (n) ( ?email.HasAttachments: Pill, [ ?alreadyTagged: Pill (n) Pill ] ) } ) ] ], ?Error is not null: Notice, LoadGate{ ?sentNote is not null: ⚠button→Button, ?replyingTo is { } anchor: MailReplyComposer=( ⚠button→Button, ?error is not null: Notice, FormField{ RecipientInput=( [ (n) ?!Disabled: ⚠button→Button ⚠input→FormField ], ?isOpen && suggestions.Count > 0: (n) ⚠button→Button ) }, ?!showBcc: ⚠button→Button, RecipientInput=( [ (n) ?!Disabled: ⚠button→Button ⚠input→FormField ], ?isOpen && suggestions.Count > 0: (n) ⚠button→Button ), ?showBcc: FormField{ RecipientInput=( [ (n) ?!Disabled: ⚠button→Button ⚠input→FormField ], ?isOpen && suggestions.Count > 0: (n) ⚠button→Button ) }, FormField, RichTextEditor, AttachmentPicker=( ?Attachments.Count > 0: (n) ⚠button→Button, [ InputFile (3) ⚠button→Button ?OriginalAttachments is { Count:…: ⚠button→Button ], ?openPanel != Panel.None: ( ?openPanel is Panel.Drawings or …: FormField, ( ?openPanel == Panel.Drawings: ( ⚠input→FormField, (n) ?folderGroup.Folder is not null …: ⚠button→Button, ?SelectedDrawingCount > 0 && !at…: ⚠button→Button ) | ?openPanel == Panel.Photos: (n) (n) ⚠button→Button | ?openPanel == Panel.Records: ( ⚠input→FormField, (n) ⚠button→Button ) ) ) ) ), CorrespondenceThreadList } ) ) ( VariationDetailsCard=( LoadGate{ ?EditingEstimate && CanEditEstim…: FormField } ), ( ?ApprovedOrder is not null: ( ApprovedFiguresPanel=( ⚠h3→SectionHeader, LoadGate, ?CanManage && Revising && Valuat…: FormField ), Modal{ VariationApprovePanel=( ?ShowHeading: ⚠h3→SectionHeader, ?error is not null || SubmitErro…: Notice, (n) ( SearchSelect, (3) ⚠input→FormField, ⚠button→Button ), ⚠button→Button ) } ) | ?CanManage && order.Status.IsPre…: ( StagedBuildUpPanel=( ⚠h3→SectionHeader, Modal{ VariationApprovePanel=( ?ShowHeading: ⚠h3→SectionHeader, ?error is not null || SubmitErro…: Notice, (n) ( SearchSelect, (3) ⚠input→FormField, ⚠button→Button ), ⚠button→Button ), (3) FormField } ), VariationApproveOffer=( Modal{ VariationApprovePanel=( ?ShowHeading: ⚠h3→SectionHeader, ?error is not null || SubmitErro…: Notice, (n) ( SearchSelect, (3) ⚠input→FormField, ⚠button→Button ), ⚠button→Button ) } ), RecordAgreedTenderPanel=( Modal{ ?error is not null: Notice, (2) FormField } ) ) ) ) ], Modal{ ?renameError is not null: Notice, FormField }, ?CanManage && string.IsNullOrWhi…: OriginatingRequestRepair=( Modal{ ?error is not null: Notice, FormField{ SearchSelect } } ), ?CanManage && ApprovedOrder is n…: DeleteVariationPanel=( ConfirmDialog ), DeclineVariationModal=( Modal{ ?Error is not null: Notice } ), VariationOrderEmailModal=( Modal{ ?Order is { } order: ( ?outcome is null: ( FormField, RecordEmailPreviewPanel=( ?Error is not null: Notice, ⚠dl→KeyValueList ) ), ?error is { } message: Notice, ?outcome is { } result: Notice ) } ) } ) }
```

### `/projects/{ProjectId}/voq/{VariationOrderId}`
`jpms/Pages/ProjectVariationDetail.razor`

```
Page{ ( ?!orderLoaded: ProjectPageShell=( ( ?Projects.Current is null: LoadGate | ?Project is null: PageHeader | PageHeader{ ?Neighbour(-1) is { } previous: ⚠button→Button, ?Neighbour(1) is { } next: ⚠button→Button, ProjectStageBadge } ) ){ LoadGate } | ?order is null: ProjectPageShell=( ( ?Projects.Current is null: LoadGate | ?Project is null: PageHeader | PageHeader{ ?Neighbour(-1) is { } previous: ⚠button→Button, ?Neighbour(1) is { } next: ⚠button→Button, ProjectStageBadge } ) ) | ProjectPageShell=( ( ?Projects.Current is null: LoadGate | ?Project is null: PageHeader | PageHeader{ ?Neighbour(-1) is { } previous: ⚠button→Button, ?Neighbour(1) is { } next: ⚠button→Button, ProjectStageBadge } ) ){ VariationHeaderBar=( [ ( [ ?StatusMenuItems.Count > 0: DropdownMenu Pill ], ⚠h2→SectionHeader ) DropdownMenu ] ), RecordTabBar, ?error is not null: Notice, ?string.IsNullOrWhiteSpace(order…: Notice, [ ( VariationDocumentPanel=( ⚠h3→SectionHeader, ?error is not null: Notice, (3) FormField ), ?ApprovedOrder is not null && Va…: VariationLinesTable=( ⚠h3→SectionHeader, ⚠table→RecordsTable ), VariationConversation=( Panel{ LoadGate{ (n) ConversationMessageCard=( ?ShowVisibility && IsInternal: Pill, ?CanReply: ⚠button→Button, ?Entry.Replies.Count > 0: (n) ConversationMessageCard ), ConversationComposer=( ?error is not null: Notice, ?ReplyingToAuthor is not null: ⚠button→Button, ⚠textarea→FormField ) } } ), RecordCorrespondencePanel=( [ ⚠h3→SectionHeader [ ?Loaded && Emails.Count > 0: Pill EmailFinder=( ⚠button→Button, Modal{ ⚠input→FormField, ?error is not null: Notice, ?results is not null: (n) ( ?email.HasAttachments: Pill, [ ?alreadyTagged: Pill (n) Pill ] ) } ) ] ], ?Error is not null: Notice, LoadGate{ ?sentNote is not null: ⚠button→Button, ?replyingTo is { } anchor: MailReplyComposer=( ⚠button→Button, ?error is not null: Notice, FormField{ RecipientInput=( [ (n) ?!Disabled: ⚠button→Button ⚠input→FormField ], ?isOpen && suggestions.Count > 0: (n) ⚠button→Button ) }, ?!showBcc: ⚠button→Button, RecipientInput=( [ (n) ?!Disabled: ⚠button→Button ⚠input→FormField ], ?isOpen && suggestions.Count > 0: (n) ⚠button→Button ), ?showBcc: FormField{ RecipientInput=( [ (n) ?!Disabled: ⚠button→Button ⚠input→FormField ], ?isOpen && suggestions.Count > 0: (n) ⚠button→Button ) }, FormField, RichTextEditor, AttachmentPicker=( ?Attachments.Count > 0: (n) ⚠button→Button, [ InputFile (3) ⚠button→Button ?OriginalAttachments is { Count:…: ⚠button→Button ], ?openPanel != Panel.None: ( ?openPanel is Panel.Drawings or …: FormField, ( ?openPanel == Panel.Drawings: ( ⚠input→FormField, (n) ?folderGroup.Folder is not null …: ⚠button→Button, ?SelectedDrawingCount > 0 && !at…: ⚠button→Button ) | ?openPanel == Panel.Photos: (n) (n) ⚠button→Button | ?openPanel == Panel.Records: ( ⚠input→FormField, (n) ⚠button→Button ) ) ) ) ), CorrespondenceThreadList } ) ) ( VariationDetailsCard=( LoadGate{ ?EditingEstimate && CanEditEstim…: FormField } ), ( ?ApprovedOrder is not null: ( ApprovedFiguresPanel=( ⚠h3→SectionHeader, LoadGate, ?CanManage && Revising && Valuat…: FormField ), Modal{ VariationApprovePanel=( ?ShowHeading: ⚠h3→SectionHeader, ?error is not null || SubmitErro…: Notice, (n) ( SearchSelect, (3) ⚠input→FormField, ⚠button→Button ), ⚠button→Button ) } ) | ?CanManage && order.Status.IsPre…: ( StagedBuildUpPanel=( ⚠h3→SectionHeader, Modal{ VariationApprovePanel=( ?ShowHeading: ⚠h3→SectionHeader, ?error is not null || SubmitErro…: Notice, (n) ( SearchSelect, (3) ⚠input→FormField, ⚠button→Button ), ⚠button→Button ), (3) FormField } ), VariationApproveOffer=( Modal{ VariationApprovePanel=( ?ShowHeading: ⚠h3→SectionHeader, ?error is not null || SubmitErro…: Notice, (n) ( SearchSelect, (3) ⚠input→FormField, ⚠button→Button ), ⚠button→Button ) } ), RecordAgreedTenderPanel=( Modal{ ?error is not null: Notice, (2) FormField } ) ) ) ) ], Modal{ ?renameError is not null: Notice, FormField }, ?CanManage && string.IsNullOrWhi…: OriginatingRequestRepair=( Modal{ ?error is not null: Notice, FormField{ SearchSelect } } ), ?CanManage && ApprovedOrder is n…: DeleteVariationPanel=( ConfirmDialog ), DeclineVariationModal=( Modal{ ?Error is not null: Notice } ), VariationOrderEmailModal=( Modal{ ?Order is { } order: ( ?outcome is null: ( FormField, RecordEmailPreviewPanel=( ?Error is not null: Notice, ⚠dl→KeyValueList ) ), ?error is { } message: Notice, ?outcome is { } result: Notice ) } ) } ) }
```

### `/projects/{ProjectId}/work-order-allocation`
`jpms/Pages/ProjectWorkOrderAllocation.razor`

```
Page{ ProjectPageShell=( ( ?Projects.Current is null: LoadGate | ?Project is null: PageHeader | PageHeader{ ?Neighbour(-1) is { } previous: ⚠button→Button, ?Neighbour(1) is { } next: ⚠button→Button, ProjectStageBadge } ) ){ ⚠h2→SectionHeader, ExportToExcelButton, [ ⚠h3→SectionHeader SearchInput ], RecordsTable{ (n) ?expandedOrderIds.Contains(summa…: (n) ⚠button→Button }, [ ⚠h3→SectionHeader (n) ⚠button→Button ], RecordsTable{ (n) ( ?IsAmountSplit(line): ⚠button→Button | [ ⚠select→FormField ⚠button→Button ] ) }, WorkOrderLinkSplitModal=( Modal{ (n) [ ⚠select→FormField ⚠input→FormField ⚠button→Button ], ⚠button→Button } ) } }
```

### `/projects/{ProjectId}/work-orders`
`jpms/Pages/ProjectWorkOrders.razor`

```
Page{ ProjectPageShell=( ( ?Projects.Current is null: LoadGate | ?Project is null: PageHeader | PageHeader{ ?Neighbour(-1) is { } previous: ⚠button→Button, ?Neighbour(1) is { } next: ⚠button→Button, ProjectStageBadge } ) ){ SectionHeader{ SearchInput, ExportToExcelButton }, ?poEmailNote is not null: Notice{ ⚠button→Button }, LoadGate{ ( ?Orders.Count == 0: EmptyState | ( ?DraftOrders.Count > 0: DraftWorkOrdersPanel=( ⚠h3→SectionHeader, (n) [ Pill ?PendingFor(detail) is { } pendi…: (2) ⚠button→Button DropdownMenu ] ), ?RejectedOrders.Count > 0: RejectedWorkOrdersList=( (n) [ Pill DropdownMenu ] ), ?CancelledOrders.Count > 0: CancelledWorkOrdersList=( (n) Pill ), WorkOrdersTable=( ⚠table→RecordsTable ), ?OrdersWithoutLines.Count > 0: UnpricedWorkOrdersList=( ⚠h3→SectionHeader, (n) [ ?CancelPendingFor(detail): (2) ⚠button→Button DropdownMenu ] ), WorkOrderLineRecodeModal=( Modal{ (n) [ SearchSelect ⚠input→FormField ⚠button→Button ], ⚠button→Button } ) ) ) }, ManualWorkOrderModal=( Modal{ WorkOrderForm=( ?error is not null: Notice, [ (2) FormField ], FormField, [ (2) FormField ], FormField, (n) ( [ SearchSelect (5) ⚠input→FormField ⚠button→Button ], ⚠textarea→FormField ), ⚠button→Button, ?depositRequired: ⚠input→FormField, ?!IsEditing: ?!saveAsDraft: ( ?SelectedSupplier is { } supplie…: Notice | Notice ) ), InputFile, ?existingAttachments.Count > 0: (n) ⚠button→Button, ?stagedAttachmentFiles.Count > 0: (n) ⚠button→Button, ?!IsEditing && !FormSaveAsDraft: ?createPackage: ( FormField, SearchInput, (n) ?picked: ⚠input→FormField ) }, WorkOrderSaleWarningDialog=( Modal ) ), DeleteWorkOrderModal=( Modal{ ?deleting is { } orderToDelete: ⚠button→Button } ), SupplierAccountModal=( Modal{ ( ?loadError is not null: Notice | LoadGate{ ?account is not null: ( SupplierAccountSummary=( [ (6) StatTile ], Notice ), SectionHeader, SupplierAccountOrdersTable=( RecordsTable{ (n) Pill } ), SectionHeader, SupplierAccountInvoicesTable=( RecordsTable{ (n) ( ?invoice.IsCreditNote: Pill, Pill, ?invoice.Placement != ProjectSup…: ?invoice.Placement != ProjectSup…: Pill ) } ) ) } ) } ) } }
```

### `/projects/{ProjectId}/work-orders/{WorkOrderId}/po`
`jpms/Pages/WorkOrderPo.razor`

```
Page{ ( ?Detail is null: PageHeader | ( [ ?Detail.Order.IsCancelled: Pill ?Detail.Order.IsAccepted: Pill ?Detail.Order.Status == WorkOrde…: Pill ?Detail.Order.IsDraft: Pill ?Detail.Order.IsRejected: Pill ], ?emailNote is not null: Notice, ?emailError is not null: Notice, PurchaseOrderSheet=( ⚠h1→PageHeader, ⚠dl→KeyValueList, ⚠table→RecordsTable, ?Lines.Count > 0: ⚠table→RecordsTable ), WorkOrderAttachmentsPanel=( Panel{ ?CanEdit: InputFile, ?error is not null: Notice, (n) ?CanEdit: ⚠button→Button } ), Panel, RecordAuditHistory=( Panel ), Modal{ Notice, FormField, FormField{ RichTextEditor } } ) ) }
```

### `/rate-library`
`jpms/Pages/RateLibrary.razor`

```
Page{ WorkspaceSectionNav, PageHeader{ ExportToExcelButton }, RateTable=( RecordsTable ) }
```

### `/rate-library/stale`
`jpms/Pages/StaleRates.razor`

```
Page{ PageHeader, ExportToExcelButton, ( ?Stale.Count == 0: EmptyState | RateTable=( RecordsTable ) ) }
```

### `/registers`
`jpms/Pages/Registers.razor`

```
Page{ SectionHeader{ Toolbar{ ToolbarButton, ExportToExcelButton } }, ?actionError is not null: Notice, (n) ⚠button→Button, Panel{ ⚠table→RecordsTable }, Modal{ [ (3) ⚠input→FormField (2) FormField (2) ⚠input→FormField FormField ⚠input→FormField ?editError is not null: Notice ] } }
```

### `/requests/triage`
`jpms/Pages/TriageQueue.razor`

```
Page{ ( LoadGate{ ( ?view == QueueView.Active && sel…: TriageBar=( [ ProjectSelect=( DropdownMenu{ ?AllowNone: ⚠button→Button } ) ( (2) TriageDecisionRow=( [ (2) ⚠button→Button ] ), ?ThreadTags is { Count: > 0 } in…: TriageDecisionRow=( [ (2) ⚠button→Button ] ) ) ], ?ActionError is not null: Notice, ?DiscardArmed: Notice, ⚠button→Button ) | ?view == QueueView.Active && que…: OutboxOnlyBar=( ?ActionError is not null: Notice ) ), PanelWorkspace=( (2) PanelRail ){ EmailMirrorPane=( TriageMessageDetail=( ⚠h2→SectionHeader, ?Detail is not null && Detail.At…: ( ?ShowDocumentTriageTick && Ticka…: ⚠button→Button, (n) ?!string.IsNullOrEmpty(attachmen…: ?TriageEmailDisplay.IsPreviewabl…: ⚠button→Button ), ?SentReplies.Count > 0: (n) ⚠button→Button, ⚠button→Button, (n) ⚠button→Button ) ), RecordExplorerPane=( ( ?openRecord is { } record: RecordDocumentView=( ⚠button→Button, ⚠h2→SectionHeader, ?StatusLabel is { Length: > 0 } …: Pill, LoadGate, ?Record.Type == RecordType.Reque…: RecordAttachmentList=( ?items is { Count: > 0 } attachm…: (n) ?PreviewFor(attachment) is { } p…: ⚠button→Button ), RecordEmailList=( ?emails.Count > 0: CorrespondenceThreadList ) ) | ( [ (2) ⚠select→FormField ], SearchInput, LoadGate{ (n) ⚠button→Button } ) ) ), PreviewPane=( ( ⚠button→Button, ( ?Request.IsPdf: PdfViewer=( Toolbar{ ?pageCount > 1: ( (2) ToolbarButton, ToolbarDivider ), (4) ToolbarButton, ToolbarDivider, (2) ToolbarButton, ?DownloadUrl is not null: ( ToolbarDivider, ToolbarButton ) }, LoadGate ) | ImageViewer=( Toolbar{ (5) ToolbarButton, ToolbarDivider, (2) ToolbarButton, ?DownloadUrl is not null: ( ToolbarDivider, ToolbarButton ) }, LoadGate ) ) ) ), XeroExplorerPane=( ?openTransaction is { } transact…: XeroTransactionView=( ⚠button→Button, ⚠h2→SectionHeader, ?Transaction.HasAttachments: (n) ?IsPreviewable(attachment): ⚠button→Button ), ( ?loadFailed: Notice | LoadGate ), ?snapshot.Error is { } error: Notice, [ SearchInput ⚠select→FormField ], ( ?FilteredTransactions.Count == 0: EmptyState | (n) ⚠button→Button ) ), NewEmailComposerPane=( ⚠h2→SectionHeader, ?error is not null: Notice, FormField{ RecipientInput=( [ (n) ?!Disabled: ⚠button→Button ⚠input→FormField ], ?isOpen && suggestions.Count > 0: (n) ⚠button→Button ) }, [ (2) FormField{ RecipientInput=( [ (n) ?!Disabled: ⚠button→Button ⚠input→FormField ], ?isOpen && suggestions.Count > 0: (n) ⚠button→Button ) } ], FormField, FormField{ RichTextEditor }, AttachmentPicker=( ?Attachments.Count > 0: (n) ⚠button→Button, [ InputFile (3) ⚠button→Button ?OriginalAttachments is { Count:…: ⚠button→Button ], ?openPanel != Panel.None: ( ?openPanel is Panel.Drawings or …: FormField, ( ?openPanel == Panel.Drawings: ( ⚠input→FormField, (n) ?folderGroup.Folder is not null …: ⚠button→Button, ?SelectedDrawingCount > 0 && !at…: ⚠button→Button ) | ?openPanel == Panel.Photos: (n) (n) ⚠button→Button | ?openPanel == Panel.Records: ( ⚠input→FormField, (n) ⚠button→Button ) ) ) ), ?fileToRecord: [ (3) ⚠select→FormField ] ), OutboxPane=( ⚠h2→SectionHeader, ?ComposeAnchor is { } anchor: MailReplyComposer=( ⚠button→Button, ?error is not null: Notice, FormField{ RecipientInput=( [ (n) ?!Disabled: ⚠button→Button ⚠input→FormField ], ?isOpen && suggestions.Count > 0: (n) ⚠button→Button ) }, ?!showBcc: ⚠button→Button, RecipientInput=( [ (n) ?!Disabled: ⚠button→Button ⚠input→FormField ], ?isOpen && suggestions.Count > 0: (n) ⚠button→Button ), ?showBcc: FormField{ RecipientInput=( [ (n) ?!Disabled: ⚠button→Button ⚠input→FormField ], ?isOpen && suggestions.Count > 0: (n) ⚠button→Button ) }, FormField, RichTextEditor, AttachmentPicker=( ?Attachments.Count > 0: (n) ⚠button→Button, [ InputFile (3) ⚠button→Button ?OriginalAttachments is { Count:…: ⚠button→Button ], ?openPanel != Panel.None: ( ?openPanel is Panel.Drawings or …: FormField, ( ?openPanel == Panel.Drawings: ( ⚠input→FormField, (n) ?folderGroup.Folder is not null …: ⚠button→Button, ?SelectedDrawingCount > 0 && !at…: ⚠button→Button ) | ?openPanel == Panel.Photos: (n) (n) ⚠button→Button | ?openPanel == Panel.Records: ( ⚠input→FormField, (n) ⚠button→Button ) ) ) ) ), ?CurrentReplyPending || QueuedRe…: ( ?CurrentReplyPending: [ (2) ⚠button→Button ], (n) ( [ ?!isEditing: ⚠button→Button ⚠button→Button ], ?WorkflowTags(entry) is { Count:…: (n) Pill, ?isEditing: MailReplyComposer=( ⚠button→Button, ?error is not null: Notice, FormField{ RecipientInput=( [ (n) ?!Disabled: ⚠button→Button ⚠input→FormField ], ?isOpen && suggestions.Count > 0: (n) ⚠button→Button ) }, ?!showBcc: ⚠button→Button, RecipientInput=( [ (n) ?!Disabled: ⚠button→Button ⚠input→FormField ], ?isOpen && suggestions.Count > 0: (n) ⚠button→Button ), ?showBcc: FormField{ RecipientInput=( [ (n) ?!Disabled: ⚠button→Button ⚠input→FormField ], ?isOpen && suggestions.Count > 0: (n) ⚠button→Button ) }, FormField, RichTextEditor, AttachmentPicker=( ?Attachments.Count > 0: (n) ⚠button→Button, [ InputFile (3) ⚠button→Button ?OriginalAttachments is { Count:…: ⚠button→Button ], ?openPanel != Panel.None: ( ?openPanel is Panel.Drawings or …: FormField, ( ?openPanel == Panel.Drawings: ( ⚠input→FormField, (n) ?folderGroup.Folder is not null …: ⚠button→Button, ?SelectedDrawingCount > 0 && !at…: ⚠button→Button ) | ?openPanel == Panel.Photos: (n) (n) ⚠button→Button | ?openPanel == Panel.Records: ( ⚠input→FormField, (n) ⚠button→Button ) ) ) ) ) ) ) ) } }, ?RecentTriage.Count > 0: RecentTriageFold=( ⚠h2→SectionHeader ) ) }
```

### `/rfis`
`jpms/Pages/RfiDashboard.razor`

```
Page{ PageHeader, [ (n) ⚠button→Button ExportToExcelButton ], ?loadError is not null: Notice, ( ?FilteredRecords.Count == 0: EmptyState | RecordsTable{ (n) ( ?record.ImpliesVariation: Pill, Pill ) } ) }
```

### `/sales/inbox`
`jpms/Pages/SalesInbox.razor`

```
Page{ WorkspaceSectionNav, PageHeader{ SearchInput }, ( ?Inbox.Current is null: LoadGate | ( ( ?!inbox.Configured: Notice | ?inbox.Notice is not null: Notice ), ?actionNote is not null: Notice, [ SalesInboxMessageList=( (n) ⚠button→Button ) SalesInboxThread=( ⚠h2→SectionHeader, ?actionError is not null: Notice, ?replyOpen && CanWork: ⚠textarea→FormField, ?thread is null: LoadGate, (n) SalesInboxThreadMessage=( ⚠button→Button, ?IsExpanded: ?detail is null: LoadGate ), SalesInboxLeadPickerDialog=( Modal{ FormField, ?leads is not null: (n) ⚠button→Button } ) ) ], LeadFormModal=( Modal{ ?error is not null: Notice, [ (8) FormField FormField{ SearchSelect } (2) FormField ( ?FixedStrategyId is not null: ⚠input→FormField | SearchSelect ) ?Existing is null: FormField FormField ] } ) ) ) }
```

### `/sales/leads`
`jpms/Pages/SalesLeads.razor`

```
Page{ WorkspaceSectionNav, PageHeader, LoadGate{ [ (4) StatTile ], Panel{ SearchInput, SearchSelect, [ ⚠button→Button (n) ⚠button→Button ], ⚠table→RecordsTable } }, LeadFormModal=( Modal{ ?error is not null: Notice, [ (8) FormField FormField{ SearchSelect } (2) FormField ( ?FixedStrategyId is not null: ⚠input→FormField | SearchSelect ) ?Existing is null: FormField FormField ] } ) }
```

### `/sales/leads/{LeadId}`
`jpms/Pages/SalesLeadDetail.razor`

```
Page{ ( ?!Detail.LoadedFor(LeadId) && !d…: LoadGate | ?Lead is null: WorkspaceSectionNav | ( WorkspaceSectionNav, LeadPageHeader=( PageHeader{ LeadStagePill=( Pill ), ?ActionMenuItems.Count > 0: DropdownMenu } ), ?actionNote is not null: Notice, [ ( LeadDetailsPanel=( Panel{ ⚠dl→KeyValueList } ), ?!string.IsNullOrWhiteSpace(lead…: Panel, LeadEstimatesPanel=( Panel{ RecordsTable{ (n) ( ?CanWork && estimate.Status.IsOp…: EstimateStatusPill=( Pill ) | EstimateStatusPill=( Pill ) ) } }, EstimateFormModal=( Modal{ ?error is not null: Notice, [ (6) FormField ] } ), EstimateStatusMoveDialog=( Modal{ ?error is not null: Notice, FormField, ?target == EstimateStatus.Submit…: Notice, FormField }, ConfirmDialog ) ), LeadHouseModelSection=( ?ModelledEstimate is { } modelled: LeadHouseModelPanel=( Panel{ FilterChips, Toolbar{ ToolbarButton }, ( LoadGate, HouseModelPlaybackStrip ) } ), ?AsksForAModel: Panel{ EmptyState } ), Panel{ RecordCorrespondenceSection=( RecordCorrespondencePanel=( [ ⚠h3→SectionHeader [ ?Loaded && Emails.Count > 0: Pill EmailFinder=( ⚠button→Button, Modal{ ⚠input→FormField, ?error is not null: Notice, ?results is not null: (n) ( ?email.HasAttachments: Pill, [ ?alreadyTagged: Pill (n) Pill ] ) } ) ] ], ?Error is not null: Notice, LoadGate{ ?sentNote is not null: ⚠button→Button, ?replyingTo is { } anchor: MailReplyComposer=( ⚠button→Button, ?error is not null: Notice, FormField{ RecipientInput }, ?!showBcc: ⚠button→Button, RecipientInput, ?showBcc: FormField{ RecipientInput }, FormField, RichTextEditor, AttachmentPicker ), CorrespondenceThreadList } ) ) }, LeadImaginePanel=( Panel{ ?CanWork: InlineConfirm, ?error is not null: Notice, ?Rounds.Count > 0: (n) Pill } ), LeadProposalsPanel=( Panel{ ?error is not null: Notice, ?note is not null: Notice, (n) ( Pill, ?CanWork: ?p.Status == SalesProposalStatus…: InlineConfirm ) }, ProposalFormModal=( Modal{ ?error is not null: Notice, [ (3) FormField (n) ( (3) ⚠input→FormField, Checkbox ) (n) (3) ⚠input→FormField FormField ?Concepts.Count > 0: [ ⚠button→Button (n) ⚠button→Button ] ] } ), Modal{ ?error is not null: Notice, FormField } ), LeadTimelinePanel=( Panel ) ) LeadStageLadder=( Panel{ (n) ⚠button→Button } ) ], LeadFormModal=( Modal{ ?error is not null: Notice, [ (8) FormField FormField{ SearchSelect } (2) FormField ( ?FixedStrategyId is not null: ⚠input→FormField | SearchSelect ) ?Existing is null: FormField FormField ] } ), LeadStageMoveDialog=( Modal{ ?error is not null: Notice, ?Target == LeadStage.Lost: FormField, FormField } ), LeadWinDialog=( Modal{ ?error is not null: Notice, [ (4) FormField ] } ), LeadDeleteDialog=( Modal{ ?error is not null: Notice } ), LeadActivityLogDialog=( Modal{ ?error is not null: Notice, [ (3) FormField ] } ) ) ) }
```

### `/sales/leads/{LeadId}/estimates/{EstimateId}`
`jpms/Pages/SalesEstimateDetail.razor`

```
Page{ ( ?!loadDone: LoadGate | ?Lead is null || Estimate is null: WorkspaceSectionNav | ( WorkspaceSectionNav, PageHeader{ EstimateStatusPill=( Pill ), DropdownMenu }, ?ShowingStale: Notice, ?actionNote is not null: Notice, ?actionError is not null: Notice, [ EstimateBreakdownEditor=( Panel{ (n) EstimateSectionEditor=( ?Editable: ⚠input→FormField, ⚠table→RecordsTable ) } ) ( EstimateDetailsPanel=( Panel{ ⚠dl→KeyValueList } ), EstimateNarrativePanel=( Panel{ ?!seeded: Notice, (3) FormField } ) ) ], EstimateFormModal=( Modal{ ?error is not null: Notice, [ (6) FormField ] } ), EstimateStatusMoveDialog=( Modal{ ?error is not null: Notice, FormField, ?target == EstimateStatus.Submit…: Notice, FormField }, ConfirmDialog ) ) ) }
```

### `/sales/strategies`
`jpms/Pages/SalesStrategies.razor`

```
Page{ WorkspaceSectionNav, PageHeader, ( ?Strategies.Current is null: LoadGate | (n) ( [ ⚠h2→SectionHeader Pill ], ?strategy.ResearchStatus.IsInPro…: JewelIcon ) ), StrategyFormModal=( Modal{ ?error is not null: Notice, [ (4) FormField ?Existing is null: Checkbox ?detailsOpen: (4) FormField FormField{ SearchSelect } ] } ) }
```

### `/sales/strategies/{StrategyId}`
`jpms/Pages/SalesStrategyDetail.razor`

```
Page{ ( ?!Detail.LoadedFor(StrategyId) &…: LoadGate | ?Strategy is null: WorkspaceSectionNav | ( WorkspaceSectionNav, StrategyPageHeader=( PageHeader{ Pill, ?CanDecide: ⚠select→FormField } ), ?actionError is not null: Notice, StrategyResearchStatusNotice=( ( ?Strategy.ResearchStatus.IsInPro…: JewelIcon | ?Strategy.ResearchStatus == Stra…: Notice ) ), StrategyFunnelTiles=( [ (6) StatTile ] ), [ ( ?!string.IsNullOrWhiteSpace(stra…: Panel{ SimpleMarkdown }, StrategyApproachPlanPanel=( Panel{ LoadGate{ SimpleMarkdown } } ), StrategyLeadsPanel=( Panel{ ⚠table→RecordsTable } ) ) ( StrategyArgumentPanel=( Panel{ ⚠dl→KeyValueList } ), StrategyFunnelPanel=( Panel ) ) ], StrategyFormModal=( Modal{ ?error is not null: Notice, [ (4) FormField ?Existing is null: Checkbox ?detailsOpen: (4) FormField FormField{ SearchSelect } ] } ), LeadFormModal=( Modal{ ?error is not null: Notice, [ (8) FormField FormField{ SearchSelect } (2) FormField ( ?FixedStrategyId is not null: ⚠input→FormField | SearchSelect ) ?Existing is null: FormField FormField ] } ), StrategyPlanEditDialog=( Modal{ ⚠textarea→FormField } ), StrategyPlanGenerateDialog=( Modal{ FormField } ) ) ) }
```

### `/set-password`
`jpms/Pages/SetPassword.razor`

```
JewelIcon, ( ?state == PageState.Checking: LoadGate | ?state == PageState.Invalid: ⚠h1→PageHeader | ( ⚠h1→PageHeader, (2) ⚠input→FormField ) )
```

### `/settings/ai-connections`
`jpms/Pages/AiConnections.razor`

```
Page{ PageHeader, ?error is not null: Notice, LoadGate{ RecordsTable } }
```

### `/site-photos`
`jpms/Pages/SitePhotos.razor`

```
Page{ PageHeader{ FilterChips }, ?CanContribute: SitePhotoDropZone=( InputFile ), ?error is { } message: Notice, ?lastUpload is { } upload: Notice, LoadGate{ ( ?dataFailed: Notice | ?Filtered.Count == 0: EmptyState | (n) SitePhotoCard=( ⚠button→Button, ( ?Photo.IsFiled: Pill | Pill ), ?CanDelete: InlineConfirm ) ) }, Modal{ ?viewing is { } shown: ImageViewer=( Toolbar{ (5) ToolbarButton, ToolbarDivider, (2) ToolbarButton, ?DownloadUrl is not null: ( ToolbarDivider, ToolbarButton ) }, LoadGate ) } }
```

### `/subcontractors/communications`
`jpms/Pages/SubcontractorCommunications.razor`

```
Page{ PageHeader, [ ⚠button→Button (n) ⚠button→Button ], ?loadError is not null: Notice, LoadGate{ ( ?replySent is not null: ⚠button→Button, ?replyTo is { } replyingTo: MailReplyComposer=( ⚠button→Button, ?error is not null: Notice, FormField{ RecipientInput=( [ (n) ?!Disabled: ⚠button→Button ⚠input→FormField ], ?isOpen && suggestions.Count > 0: (n) ⚠button→Button ) }, ?!showBcc: ⚠button→Button, RecipientInput=( [ (n) ?!Disabled: ⚠button→Button ⚠input→FormField ], ?isOpen && suggestions.Count > 0: (n) ⚠button→Button ), ?showBcc: FormField{ RecipientInput=( [ (n) ?!Disabled: ⚠button→Button ⚠input→FormField ], ?isOpen && suggestions.Count > 0: (n) ⚠button→Button ) }, FormField, RichTextEditor, AttachmentPicker=( ?Attachments.Count > 0: (n) ⚠button→Button, [ InputFile (3) ⚠button→Button ?OriginalAttachments is { Count:…: ⚠button→Button ], ?openPanel != Panel.None: ( ?openPanel is Panel.Drawings or …: FormField, ( ?openPanel == Panel.Drawings: ( ⚠input→FormField, (n) ?folderGroup.Folder is not null …: ⚠button→Button, ?SelectedDrawingCount > 0 && !at…: ⚠button→Button ) | ?openPanel == Panel.Photos: (n) (n) ⚠button→Button | ?openPanel == Panel.Records: ( ⚠input→FormField, (n) ⚠button→Button ) ) ) ) ), CorrespondenceThreadList ) } }
```

### `/subcontractors/communications/{Category?}`
`jpms/Pages/SubcontractorCommunications.razor`

```
Page{ PageHeader, [ ⚠button→Button (n) ⚠button→Button ], ?loadError is not null: Notice, LoadGate{ ( ?replySent is not null: ⚠button→Button, ?replyTo is { } replyingTo: MailReplyComposer=( ⚠button→Button, ?error is not null: Notice, FormField{ RecipientInput=( [ (n) ?!Disabled: ⚠button→Button ⚠input→FormField ], ?isOpen && suggestions.Count > 0: (n) ⚠button→Button ) }, ?!showBcc: ⚠button→Button, RecipientInput=( [ (n) ?!Disabled: ⚠button→Button ⚠input→FormField ], ?isOpen && suggestions.Count > 0: (n) ⚠button→Button ), ?showBcc: FormField{ RecipientInput=( [ (n) ?!Disabled: ⚠button→Button ⚠input→FormField ], ?isOpen && suggestions.Count > 0: (n) ⚠button→Button ) }, FormField, RichTextEditor, AttachmentPicker=( ?Attachments.Count > 0: (n) ⚠button→Button, [ InputFile (3) ⚠button→Button ?OriginalAttachments is { Count:…: ⚠button→Button ], ?openPanel != Panel.None: ( ?openPanel is Panel.Drawings or …: FormField, ( ?openPanel == Panel.Drawings: ( ⚠input→FormField, (n) ?folderGroup.Folder is not null …: ⚠button→Button, ?SelectedDrawingCount > 0 && !at…: ⚠button→Button ) | ?openPanel == Panel.Photos: (n) (n) ⚠button→Button | ?openPanel == Panel.Records: ( ⚠input→FormField, (n) ⚠button→Button ) ) ) ) ), CorrespondenceThreadList ) } }
```

### `/suppliers/communications`
`jpms/Pages/SubcontractorCommunications.razor`

```
Page{ PageHeader, [ ⚠button→Button (n) ⚠button→Button ], ?loadError is not null: Notice, LoadGate{ ( ?replySent is not null: ⚠button→Button, ?replyTo is { } replyingTo: MailReplyComposer=( ⚠button→Button, ?error is not null: Notice, FormField{ RecipientInput=( [ (n) ?!Disabled: ⚠button→Button ⚠input→FormField ], ?isOpen && suggestions.Count > 0: (n) ⚠button→Button ) }, ?!showBcc: ⚠button→Button, RecipientInput=( [ (n) ?!Disabled: ⚠button→Button ⚠input→FormField ], ?isOpen && suggestions.Count > 0: (n) ⚠button→Button ), ?showBcc: FormField{ RecipientInput=( [ (n) ?!Disabled: ⚠button→Button ⚠input→FormField ], ?isOpen && suggestions.Count > 0: (n) ⚠button→Button ) }, FormField, RichTextEditor, AttachmentPicker=( ?Attachments.Count > 0: (n) ⚠button→Button, [ InputFile (3) ⚠button→Button ?OriginalAttachments is { Count:…: ⚠button→Button ], ?openPanel != Panel.None: ( ?openPanel is Panel.Drawings or …: FormField, ( ?openPanel == Panel.Drawings: ( ⚠input→FormField, (n) ?folderGroup.Folder is not null …: ⚠button→Button, ?SelectedDrawingCount > 0 && !at…: ⚠button→Button ) | ?openPanel == Panel.Photos: (n) (n) ⚠button→Button | ?openPanel == Panel.Records: ( ⚠input→FormField, (n) ⚠button→Button ) ) ) ) ), CorrespondenceThreadList ) } }
```

### `/suppliers/communications/{Category?}`
`jpms/Pages/SubcontractorCommunications.razor`

```
Page{ PageHeader, [ ⚠button→Button (n) ⚠button→Button ], ?loadError is not null: Notice, LoadGate{ ( ?replySent is not null: ⚠button→Button, ?replyTo is { } replyingTo: MailReplyComposer=( ⚠button→Button, ?error is not null: Notice, FormField{ RecipientInput=( [ (n) ?!Disabled: ⚠button→Button ⚠input→FormField ], ?isOpen && suggestions.Count > 0: (n) ⚠button→Button ) }, ?!showBcc: ⚠button→Button, RecipientInput=( [ (n) ?!Disabled: ⚠button→Button ⚠input→FormField ], ?isOpen && suggestions.Count > 0: (n) ⚠button→Button ), ?showBcc: FormField{ RecipientInput=( [ (n) ?!Disabled: ⚠button→Button ⚠input→FormField ], ?isOpen && suggestions.Count > 0: (n) ⚠button→Button ) }, FormField, RichTextEditor, AttachmentPicker=( ?Attachments.Count > 0: (n) ⚠button→Button, [ InputFile (3) ⚠button→Button ?OriginalAttachments is { Count:…: ⚠button→Button ], ?openPanel != Panel.None: ( ?openPanel is Panel.Drawings or …: FormField, ( ?openPanel == Panel.Drawings: ( ⚠input→FormField, (n) ?folderGroup.Folder is not null …: ⚠button→Button, ?SelectedDrawingCount > 0 && !at…: ⚠button→Button ) | ?openPanel == Panel.Photos: (n) (n) ⚠button→Button | ?openPanel == Panel.Records: ( ⚠input→FormField, (n) ⚠button→Button ) ) ) ) ), CorrespondenceThreadList ) } }
```

### `/todos`
`jpms/Pages/Todos.razor`

```
Page{ ( ?!HasInternalRole: PageHeader | ( PageHeader, ?error is not null: Notice, [ ( SearchInput, ?HasQuery: ⚠button→Button ) [ (2) ⚠button→Button ] ?!boardView: (3) ⚠button→Button (2) ⚠select→FormField ?CanSeeAll: ⚠select→FormField ], LoadGate{ ( ⚠h2→SectionHeader, ( ?boardView: TodoBoard=( (n) ( ⚠h3→SectionHeader, (n) ⚠button→Button ) ) | (n) ⚠button→Button ), ?HasQuery: TaggedEmailSearch ) } ) ) }, Modal{ ?addError is not null: Notice, FormField{ SearchSelect }, FormField, [ FormField{ TodoAssigneeSelect=( SearchSelect, ?ShowPeople: SearchSelect ) } FormField ], FormField }
```

### `/todos/{TodoItemId}`
`jpms/Pages/TodoDetail.razor`

```
Page{ ( ?!HasInternalRole: PageHeader | LoadGate{ ( ?item is null: PageHeader | ( PageHeader{ Pill, ?CanManage: InlineConfirm }, ?error is not null: Notice, [ TodoCommunicationsPanel=( Panel{ EmailFinder=( ⚠button→Button, Modal{ ⚠input→FormField, ?error is not null: Notice, ?results is not null: (n) ( ?email.HasAttachments: Pill, [ ?alreadyTagged: Pill (n) Pill ] ) } ), Toolbar{ ?CanSend: ToolbarButton, ToolbarButton }, ?failed is not null: Notice, ?sentNote is not null: Notice, ?composingNew: TodoEmailComposer=( MailReplyComposer=( ⚠button→Button, ?error is not null: Notice, FormField{ RecipientInput }, ?!showBcc: ⚠button→Button, RecipientInput, ?showBcc: FormField{ RecipientInput }, FormField, RichTextEditor, AttachmentPicker ) ), ?replyingTo is { } replyAnchor: TodoEmailComposer=( MailReplyComposer=( ⚠button→Button, ?error is not null: Notice, FormField{ RecipientInput }, ?!showBcc: ⚠button→Button, RecipientInput, ?showBcc: FormField{ RecipientInput }, FormField, RichTextEditor, AttachmentPicker ) ), UnfiledRepliesNotice, ?emails.Count > 0: CorrespondenceThreadList } ) ( TodoFactsPanel=( Panel{ ⚠dl→KeyValueList } ), TodoActivityPanel=( Panel{ ?formOpen: TodoProgressForm=( ⚠textarea→FormField ) } ), LinkedTodosPanel=( Panel{ ?!loading && failed is null && l…: Pill, ( ?failed is not null: Notice | (n) Pill ) } ) ) ] ) ) } ) }
```

## Components of the site

| Component | Used by | Definition |
| --- | --- | --- |
| `ProjectPageShell` | 35 | `( ?Projects.Current is null: LoadGate \| ?Project is null: PageHeader \| PageHeader{ ?Neighbour(-1) is { } previous: ⚠button→Button, ?Neighbour(1) is { } next: ⚠button→Button, ProjectStageBadge } )` |
| `JewelIcon` | 16 | — |
| `WorkspaceSectionNav` | 16 | — |
| `CorrespondenceThreadList` | 10 | — |
| `MailReplyComposer` | 7 | `⚠button→Button, ?error is not null: Notice, FormField{ RecipientInput }, ?!showBcc: ⚠button→Button, RecipientInput, ?showBcc: FormField{ RecipientInput }, FormField, RichTextEditor, AttachmentPicker` |
| `ActionIcon` | 5 | — |
| `ComplianceStatusPill` | 5 | `Pill` |
| `ImageViewer` | 5 | `Toolbar{ (5) ToolbarButton, ToolbarDivider, (2) ToolbarButton, ?DownloadUrl is not null: ( ToolbarDivider, ToolbarButton ) }, LoadGate` |
| `TodoAssigneeSelect` | 5 | `SearchSelect, ?ShowPeople: SearchSelect` |
| `VariationApprovePanel` | 5 | `?ShowHeading: ⚠h3→SectionHeader, ?error is not null \|\| SubmitErro…: Notice, (n) ( SearchSelect, (3) ⚠input→FormField, ⚠button→Button ), ⚠button→Button` |
| `ConversationMessageCard` | 4 | `?ShowVisibility && IsInternal: Pill, ?CanReply: ⚠button→Button, ?Entry.Replies.Count > 0: (n) ConversationMessageCard` |
| `EmailFinder` | 4 | `⚠button→Button, Modal{ ⚠input→FormField, ?error is not null: Notice, ?results is not null: (n) ( ?email.HasAttachments: Pill, [ ?alreadyTagged: Pill (n) Pill ] ) }` |
| `LeadFormModal` | 4 | `Modal{ ?error is not null: Notice, [ (8) FormField FormField{ SearchSelect } (2) FormField ( ?FixedStrategyId is not null: ⚠input→FormField \| SearchSelect ) ?Existing is null: FormField FormField ] }` |
| `LedgerLineIdentityCell` | 4 | `?Line.HasAttachments: ⚠button→Button` |
| `RecipientInput` | 4 | `[ (n) ?!Disabled: ⚠button→Button ⚠input→FormField ], ?isOpen && suggestions.Count > 0: (n) ⚠button→Button` |
| `RecordCorrespondencePanel` | 4 | `[ ⚠h3→SectionHeader [ ?Loaded && Emails.Count > 0: Pill EmailFinder ] ], ?Error is not null: Notice, LoadGate{ ?sentNote is not null: ⚠button→Button, ?replyingTo is { } anchor: MailReplyComposer, CorrespondenceThreadList }` |
| `RecordEmailPreviewPanel` | 4 | `?Error is not null: Notice, ⚠dl→KeyValueList` |
| `TriageMessageDetail` | 4 | `⚠h2→SectionHeader, ?Detail is not null && Detail.At…: ( ?ShowDocumentTriageTick && Ticka…: ⚠button→Button, (n) ?!string.IsNullOrEmpty(attachmen…: ?TriageEmailDisplay.IsPreviewabl…: ⚠button→Button ), ?SentReplies.Count > 0: (n) ⚠button→Button, ⚠button→Button, (n) ⚠button→Button` |
| `ActivityBadge` | 3 | `?Activity is { } summary && summ…: Pill` |
| `AttachmentPicker` | 3 | `?Attachments.Count > 0: (n) ⚠button→Button, [ InputFile (3) ⚠button→Button ?OriginalAttachments is { Count:…: ⚠button→Button ], ?openPanel != Panel.None: ( ?openPanel is Panel.Drawings or …: FormField, ( ?openPanel == Panel.Drawings: ( ⚠input→FormField, (n) ?folderGroup.Folder is not null …: ⚠button→Button, ?SelectedDrawingCount > 0 && !at…: ⚠button→Button ) \| ?openPanel == Panel.Photos: (n) (n) ⚠button→Button \| ?openPanel == Panel.Records: ( ⚠input→FormField, (n) ⚠button→Button ) ) )` |
| `BulkSelectionBar` | 3 | `⚠button→Button` |
| `EmailListPager` | 3 | — |
| `LedgerLineSummary` | 3 | — |
| `PdfViewer` | 3 | `Toolbar{ ?pageCount > 1: ( (2) ToolbarButton, ToolbarDivider ), (4) ToolbarButton, ToolbarDivider, (2) ToolbarButton, ?DownloadUrl is not null: ( ToolbarDivider, ToolbarButton ) }, LoadGate` |
| `ProjectStageBadge` | 3 | — |
| `RecordCorrespondenceSection` | 3 | `RecordCorrespondencePanel` |
| `SimpleMarkdown` | 3 | — |
| `TodoBoard` | 3 | `(n) ( ⚠h3→SectionHeader, (n) ⚠button→Button )` |
| `TriageEmailRow` | 3 | `⚠button→Button` |
| `ClientConversationPanel` | 2 | `Panel{ LoadGate{ (n) ConversationMessageCard, ConversationComposer } }` |
| `ConversationComposer` | 2 | `?error is not null: Notice, ?ReplyingToAuthor is not null: ⚠button→Button, ⚠textarea→FormField` |
| `DirectoryContactForm` | 2 | `?error is not null: Notice, FormField, [ FormField ( ?tradeIds.Count > 0: (n) Pill{ ⚠button→Button }, ⚠select→FormField ) ], ⚠input→FormField, (2) [ (2) FormField ], FormField, (2) [ (2) FormField ]` |
| `DrawingFolderPicker` | 2 | `FormField{ DrawingFolderOptions }, ?IsCreatingFolder: [ ⚠input→FormField ⚠select→FormField ]` |
| `DrawingRevisionList` | 2 | `⚠h2→SectionHeader, (n) [ ( DrawingRevisionLabelEditor, ?revision.ApprovalStatus: (3) Pill, ?revision.IsAmbiguous: Pill, [ ?revision.MetadataExtractedAt is…: Pill Pill ?revision.AnalysedAt is { } anal…: Pill Pill ] ) ?CanManage: ⚠button→Button ], Modal` |
| `EstimateFormModal` | 2 | `Modal{ ?error is not null: Notice, [ (6) FormField ] }` |
| `EstimateStatusMoveDialog` | 2 | `Modal{ ?error is not null: Notice, FormField, ?target == EstimateStatus.Submit…: Notice, FormField }, ConfirmDialog` |
| `EstimateStatusPill` | 2 | `Pill` |
| `KpiPersonPicker` | 2 | `( ?!Ready: ⚠select→FormField \| SearchSelect ), ?key == NewKey: ⚠input→FormField` |
| `ManualVariationForm` | 2 | `?error is not null: Notice, (3) FormField, ⚠h3→SectionHeader, VariationApprovePanel, (3) FormField` |
| `ManualWorkOrderModal` | 2 | `Modal{ WorkOrderForm, InputFile, ?existingAttachments.Count > 0: (n) ⚠button→Button, ?stagedAttachmentFiles.Count > 0: (n) ⚠button→Button, ?!IsEditing && !FormSaveAsDraft: ?createPackage: ( FormField, SearchInput, (n) ?picked: ⚠input→FormField ) }, WorkOrderSaleWarningDialog` |
| `MarginLine` | 2 | — |
| `MemoLine` | 2 | — |
| `MyTodosPanel` | 2 | `[ ⚠h2→SectionHeader ?!loading: [ (2) ⚠button→Button ] ], ?error is not null: Notice, ?!loading && items.Count > 0 && …: ⚠select→FormField, LoadGate{ ( ?boardView && items.Count > 0: TodoBoard \| (n) ⚠button→Button ) }` |
| `NavIcon` | 2 | — |
| `NextValuationsPanel` | 2 | `⚠h2→SectionHeader, ( ?Projects.Current is null: LoadGate \| ⚠table→RecordsTable )` |
| `OpenRequestsPanel` | 2 | `⚠h2→SectionHeader, LoadGate` |
| `PartyContactsEditor` | 2 | `Modal{ ?error is not null: Notice, ( ?contacts.Count == 0: EmptyState \| ⚠table→RecordsTable ), [ (4) FormField ] }` |
| `ProgrammePushBar` | 2 | — |
| `ProjectMultiSelect` | 2 | `DropdownMenu{ [ (3) ⚠button→Button ] }` |
| `PurchaseOrderSheet` | 2 | `⚠h1→PageHeader, ⚠dl→KeyValueList, ⚠table→RecordsTable, ?Lines.Count > 0: ⚠table→RecordsTable` |
| `RateTable` | 2 | `RecordsTable` |
| `RecordAuditHistory` | 2 | `Panel` |
| `RoleBadge` | 2 | — |
| `SplitEditorForm` | 2 | `?Error is not null: Notice, (n) [ (2) SearchSelect ⚠input→FormField ⚠button→Button ], ⚠button→Button` |
| `StatusPill` | 2 | `Pill` |
| `StrategyFormModal` | 2 | `Modal{ ?error is not null: Notice, [ (4) FormField ?Existing is null: Checkbox ?detailsOpen: (4) FormField FormField{ SearchSelect } ] }` |
| `TaggedEmailSearch` | 2 | — |
| `TodoFactEditor` | 2 | `( ?Assignee: TodoAssigneeSelect \| SearchSelect )` |
| `UpdateToast` | 2 | — |
| `ValuationInvoiceXeroRaiseModal` | 2 | `Modal{ ?error is not null: Notice, [ (2) FormField ], LoadGate{ ?preview is not null: ( LoadGate{ (n) Notice, ValuationInvoiceXeroRaiseSummary }, FormField ) } }` |
| `WorkOrderLinkSplitModal` | 2 | `Modal{ (n) [ ⚠select→FormField ⚠input→FormField ⚠button→Button ], ⚠button→Button }` |
| `AbsenceModal` | 1 | `Modal{ ?error is not null: Notice, FormField, [ (2) FormField ], (2) FormField }` |
| `AddManualVariationDialog` | 1 | `Modal{ ManualVariationForm }` |
| `AdminHome` | 1 | `PageHeader{ DropdownMenu }, AdminStatsRow, MyTodosPanel, AdminKpiPanel, OpenRequestsPanel, NextValuationsPanel, PendingRequestsPanel` |
| `AdminKpiPanel` | 1 | `⚠h2→SectionHeader, LoadGate{ EmptyState }` |
| `AdminStatsRow` | 1 | `LoadGate{ [ (3) StatTile ] }` |
| `AllocatedBulkActions` | 1 | `BulkSelectionBar{ (2) ⚠button→Button }` |
| `AllocationPageHeader` | 1 | `[ ⚠h1→PageHeader [ (2) ⚠button→Button ] ]` |
| `AllocationTabBar` | 1 | `[ ⚠button→Button (n) ⚠button→Button ?ShowLabour: ⚠button→Button ?ShowWorkOrderBills: ⚠button→Button (4) ⚠button→Button ]` |
| `ApprovalFailuresBanner` | 1 | `Notice` |
| `ApprovedFiguresPanel` | 1 | `⚠h3→SectionHeader, LoadGate, ?CanManage && Revising && Valuat…: FormField` |
| `ApprovedSessionGate` | 1 | `( ?!sessionReady: LoadGate \| ?!Session.IsApproved: RequestAccessView )` |
| `ApprovedUserRow` | 1 | `(2) ⚠button→Button, [ (n) ?CanEdit: ⚠button→Button ?CanEdit && AddableRoles.Count >…: ⚠select→FormField ?busy: JewelIcon ], ?resetLink is not null: ⚠input→FormField` |
| `ApprovedUsersPanel` | 1 | `[ ⚠h2→SectionHeader ⚠button→Button ], ?showForm: InviteUserForm, LoadGate{ (n) ApprovedUserRow }` |
| `ArchitectsDirectoryTable` | 1 | `( ?Architects.Count == 0: LoadGate{ EmptyState } \| RecordsTable )` |
| `BucketChipStrip` | 1 | `(n) ⚠button→Button` |
| `BudgetForecastBridge` | 1 | — |
| `BulkPercentToolbar` | 1 | — |
| `CancelledWorkOrdersList` | 1 | `(n) Pill` |
| `CashForecastTable` | 1 | `RecordsTable{ (2) (n) ForecastCategoryRow, OverheadsRow, ?IsDirector && Forecast.Closing.…: ClosingBalanceRow }` |
| `CashflowAddItemRow` | 1 | `⚠button→Button` |
| `CashflowEntryRow` | 1 | `( ?Entry.ItemId is { } itemId: ⚠button→Button \| ⚠button→Button ), (n) ?Entry.WeekIndex == cellIndex: [ ?Entry.Moved: ⚠button→Button (2) ⚠button→Button ]` |
| `CashflowGroupRow` | 1 | `⚠button→Button, (n) ?members.Count > 0: [ ?anyMoved: ⚠button→Button (2) ⚠button→Button ]` |
| `CashflowItemModal` | 1 | `Modal{ FormField, [ (2) FormField ], [ FormField ⚠input→FormField ], ?formRecurrence != WeeklyCashflo…: FormField, FormField }` |
| `CategoryRegisterSection` | 1 | `SearchInput, ?ProjectNameMatch.IsUsable(Proje…: [ (2) ⚠button→Button ], ?loadError is not null: Notice, LoadGate{ CorrespondenceThreadList }` |
| `ChaseListPanel` | 1 | `Panel{ ⚠table→RecordsTable, ?Items.Count > 8 && !showAll: ⚠button→Button }` |
| `CisVerificationPanel` | 1 | `Panel{ ( ?!Subcontractor.HasCisVerificati…: EmptyState \| ⚠dl→KeyValueList ) }, Modal{ ?recordError is not null: Notice, FormField, [ (2) FormField ] }` |
| `ClaimProgressDialog` | 1 | `Modal{ ?!ClaimIsDraft: Notice, ?error is not null: Notice, (n) ( ⚠input→FormField, ⚠button→Button ), SearchSelect }` |
| `ClientCostReferencesModal` | 1 | `Modal{ LoadGate{ EmptyState, ⚠table→RecordsTable } }` |
| `ClientRequestList` | 1 | `( ?Requests.Count == 0: EmptyState \| RecordsTable )` |
| `ClientVariationList` | 1 | `( ?Orders.Count == 0: EmptyState \| RecordsTable )` |
| `ClientsDirectoryTable` | 1 | `( ?Clients.Count == 0: LoadGate{ EmptyState } \| RecordsTable )` |
| `ClosingBalanceRow` | 1 | — |
| `CodingResetModal` | 1 | `Modal{ FormField, ?error is not null: Notice }` |
| `CombinedStatementCard` | 1 | — |
| `CompaniesDirectoryTable` | 1 | `( ?Companies.Count == 0: EmptyState \| RecordsTable{ (n) CompanyDirectoryRow } )` |
| `CompanyDirectoryRow` | 1 | `Pill, (n) Pill, ?Compliance.Current is not null: ComplianceStatusPill` |
| `ConsolidateRecordsModal` | 1 | `Modal{ ?error is not null: Notice }` |
| `ContractorsReportAttendanceTable` | 1 | `RecordsTable{ (n) ( ⚠input→FormField, Checkbox ) }` |
| `ContractorsReportFindings` | 1 | `?Findings.Count > 0: Notice` |
| `ContractorsReportHeaderFields` | 1 | `[ (6) FormField ]` |
| `ContractorsReportLookAheadEditor` | 1 | `(n) [ Checkbox ⚠input→FormField ], ⚠input→FormField` |
| `ContractorsReportNarrativeFields` | 1 | `(3) FormField` |
| `ContractorsReportOpenForm` | 1 | `[ (2) FormField ]` |
| `ContractorsReportPreview` | 1 | `(9) ⚠h3→SectionHeader` |
| `ContractorsReportUpdatePicker` | 1 | `( ?Updates.Count == 0: EmptyState \| (n) Checkbox )` |
| `CostCentreCostOfSalesModal` | 1 | `Modal{ ( ?entries.Count == 0: EmptyState \| ( ⚠table→RecordsTable, ?AttributionEntries.Count > 0: ⚠table→RecordsTable ) ) }, WorkOrderLinkSplitModal` |
| `CostCentreReconciliationModal` | 1 | `Modal{ LoadGate{ ( ⚠h3→SectionHeader, RecordsTable, ⚠h3→SectionHeader, RecordsTable{ (n) Pill }, ⚠h3→SectionHeader, RecordsTable, ⚠table→RecordsTable ) } }` |
| `CostCentreSalesLinesModal` | 1 | `Modal{ ( ?CountingLines.Count == 0 && Non…: EmptyState \| ( ExportToExcelButton, ⚠table→RecordsTable, ?NonCountingLines.Count > 0: ⚠table→RecordsTable ) ) }` |
| `CostCentreWorkOrdersModal` | 1 | `Modal{ ( ?Entries.Count == 0: EmptyState \| ⚠table→RecordsTable ) }` |
| `CostCodeTable` | 1 | `Panel{ ⚠table→RecordsTable }` |
| `CoverInvoiceLinesTable` | 1 | `⚠table→RecordsTable` |
| `CriticalPathNudge` | 1 | `Notice` |
| `CriticalRfiList` | 1 | `LoadGate{ (n) [ (2) Pill ] }` |
| `CumulativeChartCard` | 1 | — |
| `CumulativePositionPanel` | 1 | `Toolbar{ ToolbarButton }, ?syncError is not null: Notice, ?reconciliations is { Count: > 0…: ( ?mismatches.Count == 0: Notice \| Notice ), LoadGate{ (n) CumulativeChartCard }` |
| `DeclineVariationModal` | 1 | `Modal{ ?Error is not null: Notice }` |
| `DefectCommunicationsPanel` | 1 | `Panel{ EmailFinder, Toolbar{ ?CanSend: ToolbarButton, ToolbarButton }, ?failed is not null: Notice, ?sentNote is not null: Notice, ?composingNew: DefectEmailComposer, ?replyingTo is { } replyAnchor: DefectEmailComposer, ?emails.Count > 0: CorrespondenceThreadList }` |
| `DefectEmailComposer` | 1 | `MailReplyComposer` |
| `DefectTodosPanel` | 1 | `Panel{ ( ?failed is not null: Notice \| (n) Pill ) }, Modal{ ?addError is not null: Notice, FormField, [ FormField{ TodoAssigneeSelect } FormField ], FormField }` |
| `DeletePackageModal` | 1 | `Modal` |
| `DeleteVariationPanel` | 1 | `ConfirmDialog` |
| `DeleteWorkOrderModal` | 1 | `Modal{ ?deleting is { } orderToDelete: ⚠button→Button }` |
| `DisputeDiscussionModal` | 1 | `Modal{ ?Line is { } discussed: ( LedgerLineSummary, EmptyState, ?Error is not null: Notice, [ ⚠textarea→FormField ⚠button→Button ] ) }` |
| `DisputeLineModal` | 1 | `Modal{ ?Line is not null: ( ?Error is not null: Notice, LedgerLineSummary, ⚠textarea→FormField ) }` |
| `DocumentListItem` | 1 | `⚠button→Button` |
| `DocumentOutcomeCard` | 1 | — |
| `DocumentPreview` | 1 | `( ?IsPdf(Item): PdfViewer \| ?IsImage(Item): ImageViewer )` |
| `DraftWorkOrdersPanel` | 1 | `⚠h3→SectionHeader, (n) [ Pill ?PendingFor(detail) is { } pendi…: (2) ⚠button→Button DropdownMenu ]` |
| `DrawingDetailsEditor` | 1 | `( ?!isEditing: [ ⚠h2→SectionHeader ?CanEdit: ⚠button→Button ] \| ( (2) FormField, ?error is not null: Notice ) )` |
| `DrawingExtractionPanel` | 1 | `?RevisionId is not null: Panel{ ( ?loadFailed: Notice \| ?view is null: EmptyState \| ( ?extraction.Status == DrawingExt…: Notice \| ( ( ?structure is not null: ( (n) Notice, [ (5) StatTile ], ⚠dl→KeyValueList, ?structure.Revisions.Count > 0: ( SectionHeader, ⚠table→RecordsTable ), ?dimensionsOpen: ⚠table→RecordsTable, ?calloutsOpen: ?structure.Callouts.Count == 0: EmptyState, ?shapesOpen: ⚠table→RecordsTable ) \| Notice ), ?markupsOpen: ⚠table→RecordsTable ) ) ) }` |
| `DrawingFolderOptions` | 1 | — |
| `DrawingRevisionLabelEditor` | 1 | `( ?!isEditing: ?CanEdit: ⚠button→Button \| [ ⚠input→FormField ⚠button→Button ] )` |
| `DrawingRevisionUploadForm` | 1 | `FormField{ InputFile }, (2) FormField, ?error is not null: Notice` |
| `DrawingUploadForm` | 1 | `⚠h3→SectionHeader, DrawingFolderPicker, ?isRevision: FormField, (n) InputFile, ( ?selectedFiles.Count == 1: ⚠button→Button \| ?IsBulk: ( ⚠button→Button, (n) ⚠button→Button ) ), ?!isRevision && !IsBulk: (2) FormField, ?!IsBulk: (2) FormField, ?error is not null: Notice, ?failures.Count > 0: Notice` |
| `DrawingsTable` | 1 | `RecordsTable{ (n) [ ActionIcon ?folderSection.Folder is not null: ActionIcon ?CanManage && folderSection.Fold…: [ (4) ⚠button→Button ] ] }` |
| `EmailDraftStagingModal` | 1 | `Modal{ ( ?DraftResult is null: ( RecordEmailPreviewPanel, LoadGate, ?Error is not null: Notice ) \| Notice ) }` |
| `EmailMirrorPane` | 1 | `TriageMessageDetail` |
| `EstimateBreakdownEditor` | 1 | `Panel{ (n) EstimateSectionEditor }` |
| `EstimateDetailsPanel` | 1 | `Panel{ ⚠dl→KeyValueList }` |
| `EstimateNarrativePanel` | 1 | `Panel{ ?!seeded: Notice, (3) FormField }` |
| `EstimateSectionEditor` | 1 | `?Editable: ⚠input→FormField, ⚠table→RecordsTable` |
| `ExpiringDocumentsPanel` | 1 | `⚠h2→SectionHeader, ( ?LoadState.UntilAllPresent(Compl…: LoadGate \| ⚠table→RecordsTable )` |
| `FinancialsTable` | 1 | `[ ⚠input→FormField ExportToExcelButton ], ⚠table→RecordsTable` |
| `ForecastBalanceChart` | 1 | — |
| `ForecastCategoryRow` | 1 | `⚠button→Button, ?Expanded: (n) ForecastProjectLine` |
| `ForecastKpiStrip` | 1 | `[ (4) StatTile ]` |
| `ForecastProjectLine` | 1 | `?Category == ForecastCategory.Fu…: (2) ⚠input→FormField` |
| `HouseModelPlaybackStrip` | 1 | — |
| `HsAuditSectionPanel` | 1 | `Panel{ ⚠table→RecordsTable }` |
| `ImagineProposal` | 1 | `⚠h2→SectionHeader, ?Proposal.Status == SalesProposa…: Notice, ?!string.IsNullOrWhiteSpace(Prop…: ( ⚠h3→SectionHeader, SimpleMarkdown ), ⚠h3→SectionHeader, ?Proposal.Options.Count > 0: (n) ?o.Recommended && !Accepted: Pill, ?Proposal.Schedule.Count > 0: ⚠h3→SectionHeader, ?!string.IsNullOrWhiteSpace(Prop…: ( ⚠h3→SectionHeader, SimpleMarkdown ), ?!Accepted && Proposal.Status ==…: ( ⚠h3→SectionHeader, [ (2) ⚠input→FormField ], Checkbox, ?declining: ⚠input→FormField )` |
| `ImagineRoundList` | 1 | `(n) ( ⚠h2→SectionHeader, ImagineRoundOutcome{ (n) ( ⚠h3→SectionHeader, ⚠button→Button, ⚠input→FormField, ?revising?.ImageId == c.ImageId: ⚠textarea→FormField ) }, ImagineRoundPhotos )` |
| `ImagineRoundOutcome` | 1 | `?Round.Status.IsInProgress(): JewelIcon` |
| `ImagineRoundPhotos` | 1 | — |
| `ImagineSubmissionForm` | 1 | `⚠h2→SectionHeader, InputFile, ?preparing: JewelIcon, ?photos.Count > 0: (n) ⚠button→Button, ⚠textarea→FormField, [ (2) ⚠input→FormField ], Checkbox` |
| `InviteUserForm` | 1 | `( ?outcome?.Success == true && out…: ( ⚠input→FormField, [ (2) ⚠button→Button ] ) \| (2) FormField )` |
| `InvitedSubcontractorsSection` | 1 | `[ ⚠h3→SectionHeader ?CanEdit: [ ?Recipients.Count > 0: ⚠button→Button ?ReadyForInvites: (2) ⚠button→Button ] ], ?SendNote is not null: Notice, LoadGate{ ( ?Recipients.Count == 0: ?Fetched: EmptyState \| RecordsTable{ (n) ( ?sub is { IsProspect: true }: Pill, ?sub is not null && string.IsNul…: Pill ) } ) }` |
| `InvoiceBillLines` | 1 | `?Reference is not null \|\| ShowsL…: ?ShowsLines: ⚠table→RecordsTable, ?fetchFailed: Notice` |
| `InvoiceDocumentPreview` | 1 | `( ?(Error ?? fetchError) is { } sh…: Notice \| ( ?attachments.Count > 1: (n) ⚠button→Button, ?selected is not null: ?IsImage(selected): ImageViewer ) )` |
| `InvoiceViewerActions` | 1 | `( ?SplitOpen: ( ⚠button→Button, SplitEditorForm ) \| ?DisputeOpen: ( ⚠button→Button, ?DisputeError is not null: Notice, ⚠textarea→FormField ) \| ( [ (2) SearchSelect ?!string.IsNullOrEmpty(ArmedBuck…: ⚠button→Button ⚠button→Button ⚠button→Button ], [ ⚠select→FormField (2) ⚠button→Button ] ) )` |
| `JewelSpinner` | 1 | `( ?Overlay: JewelIcon \| JewelIcon )` |
| `KpiTagSection` | 1 | `⚠button→Button, ?open: (n) [ ⚠button→Button ?staged is not null: ⚠button→Button ⚠button→Button ]` |
| `LabourBillApproveDialog` | 1 | `ConfirmDialog{ ?error is not null: Notice }` |
| `LabourBulkActions` | 1 | `BulkSelectionBar{ ⚠button→Button }` |
| `LabourForecastHeader` | 1 | `Panel{ MetricStat }` |
| `LabourSectionStrip` | 1 | `?CoveredCount > 0: ⚠button→Button` |
| `LeadActivityLogDialog` | 1 | `Modal{ ?error is not null: Notice, [ (3) FormField ] }` |
| `LeadDeleteDialog` | 1 | `Modal{ ?error is not null: Notice }` |
| `LeadDetailsPanel` | 1 | `Panel{ ⚠dl→KeyValueList }` |
| `LeadEstimatesPanel` | 1 | `Panel{ RecordsTable{ (n) ( ?CanWork && estimate.Status.IsOp…: EstimateStatusPill \| EstimateStatusPill ) } }, EstimateFormModal, EstimateStatusMoveDialog` |
| `LeadHouseModelPanel` | 1 | `Panel{ FilterChips, Toolbar{ ToolbarButton }, ( LoadGate, HouseModelPlaybackStrip ) }` |
| `LeadHouseModelSection` | 1 | `?ModelledEstimate is { } modelled: LeadHouseModelPanel, ?AsksForAModel: Panel{ EmptyState }` |
| `LeadImaginePanel` | 1 | `Panel{ ?CanWork: InlineConfirm, ?error is not null: Notice, ?Rounds.Count > 0: (n) Pill }` |
| `LeadPageHeader` | 1 | `PageHeader{ LeadStagePill, ?ActionMenuItems.Count > 0: DropdownMenu }` |
| `LeadProposalsPanel` | 1 | `Panel{ ?error is not null: Notice, ?note is not null: Notice, (n) ( Pill, ?CanWork: ?p.Status == SalesProposalStatus…: InlineConfirm ) }, ProposalFormModal, Modal{ ?error is not null: Notice, FormField }` |
| `LeadStageLadder` | 1 | `Panel{ (n) ⚠button→Button }` |
| `LeadStageMoveDialog` | 1 | `Modal{ ?error is not null: Notice, ?Target == LeadStage.Lost: FormField, FormField }` |
| `LeadStagePill` | 1 | `Pill` |
| `LeadTimelinePanel` | 1 | `Panel` |
| `LeadWinDialog` | 1 | `Modal{ ?error is not null: Notice, [ (4) FormField ] }` |
| `LineCoverageModal` | 1 | `Modal{ ?Line is not null: ( FormField, ( ?coverage == BidPackageLineCover…: ⚠select→FormField \| ?coverage == BidPackageLineCover…: ( ?Variations.Count == 0: EmptyState \| ⚠select→FormField ) ) ) }` |
| `LinkDrawingsModal` | 1 | `Modal` |
| `LinkedTodosPanel` | 1 | `Panel{ ?!loading && failed is null && l…: Pill, ( ?failed is not null: Notice \| (n) Pill ) }` |
| `LocalSubcontractorFinderModal` | 1 | `Modal{ ( ?notReadyReason is not null: Notice \| ⚠input→FormField ), ?searchError is not null: Notice, ( ?results.Count > 0: (n) ?place.ExistingSubcontractorId i…: Pill \| ?hasSearched && !searchBusy && s…: EmptyState ) }` |
| `MatchedLinesBanner` | 1 | `[ ?!armed: ⚠button→Button (2) ⚠button→Button ]` |
| `MetaCell` | 1 | — |
| `MyDayWorkspace` | 1 | `?Labour.MyDay() is { WorkerId: n…: ( SectionHeader{ ToolbarButton }, ?actionError is not null: Notice, (n) ( ⚠h3→SectionHeader, ?project.HasSignedOutToday: ?todays.Count > 0: (n) StatusPill ), ?history.Count > 0: ( ⚠h3→SectionHeader, (n) StatusPill ), ?day.Rejected.Count > 0: ( ⚠h3→SectionHeader, (n) ⚠input→FormField ) )` |
| `NewEmailComposerPane` | 1 | `⚠h2→SectionHeader, ?error is not null: Notice, FormField{ RecipientInput }, [ (2) FormField{ RecipientInput } ], FormField, FormField{ RichTextEditor }, AttachmentPicker, ?fileToRecord: [ (3) ⚠select→FormField ]` |
| `NewProjectForm` | 1 | `(3) FormField, [ (2) FormField ], FormField` |
| `NextValuationDateEditor` | 1 | `Modal{ ?error is not null: Notice, FormField, ?calculate: ( [ (2) FormField ], ?snapToWeekday: [ (2) FormField ] ), ?CurrentDate is not null: ⚠button→Button }` |
| `OriginatingRequestRepair` | 1 | `Modal{ ?error is not null: Notice, FormField{ SearchSelect } }` |
| `OutboxOnlyBar` | 1 | `?ActionError is not null: Notice` |
| `OutboxPane` | 1 | `⚠h2→SectionHeader, ?ComposeAnchor is { } anchor: MailReplyComposer, ?CurrentReplyPending \|\| QueuedRe…: ( ?CurrentReplyPending: [ (2) ⚠button→Button ], (n) ( [ ?!isEditing: ⚠button→Button ⚠button→Button ], ?WorkflowTags(entry) is { Count:…: (n) Pill, ?isEditing: MailReplyComposer ) )` |
| `OverheadsRow` | 1 | `⚠input→FormField, (n) ⚠input→FormField` |
| `PackageDetailsEditorModal` | 1 | `Modal{ ?(validationError ?? Error) is {…: Notice, ⚠textarea→FormField, RecordsTable{ (n) ( (4) ⚠input→FormField, ⚠select→FormField ) } }` |
| `PackageDetailsSections` | 1 | `(2) ⚠h3→SectionHeader, LoadGate{ ( ?LineItems.Count == 0: ?Fetched: EmptyState \| (n) RecordsTable{ (n) ( ?string.IsNullOrWhiteSpace(item.…: Pill, ?CanEdit: ⚠button→Button ) } ) }` |
| `PackageDocumentsSection` | 1 | `[ ⚠h3→SectionHeader ?CanEdit: [ InputFile ⚠button→Button ] ], LoadGate{ ?Attachments.Count > 0: (n) ?CanEdit: ⚠button→Button, ?Drawings.Count > 0: (n) Pill }` |
| `PackageReconciliationSection` | 1 | `SectionHeader{ ExportToExcelButton }, ?error is not null: Notice, ( ?rows.Count == 0: EmptyState \| RecordsTable{ (n) ( ?row.IsLocked: Pill, ?CanManage: ( ?!row.IsLocked: ( (2) ⚠button→Button, ( ?pendingDeleteId == row.Reconcil…: (2) ⚠button→Button \| ⚠button→Button ) ) \| ⚠button→Button ) ) } ), ⚠button→Button, ManualWorkOrderModal, ReconciliationPackageBuilderModal` |
| `PanelRail` | 1 | — |
| `PanelWorkspace` | 1 | `(2) PanelRail` |
| `PathwayActionsSection` | 1 | `?StagedActions.Count > 0 \|\| Stag…: ( (n) ⚠button→Button, ?StagedCreate is { } stagedRecord: ⚠button→Button, (n) ⚠button→Button ), ⚠select→FormField, ?NeedsProject(kind) && ProjectId…: Notice, ?kind: ( StagedRecordActionEditor, StageRequestTransitionAction, StageVariationAction, StageVariationDecisionAction, (2) StagedRecordActionEditor, StageTenderResponseAction, (4) StagedRecordActionEditor, StageCalendarEventAction, StageBuildingControlInspectionAction, StageTodosAction, StageTodoCompleteAction, StageDirectoryContactAction, StageKpiAction ), ?IsRecordCreate(kind): StagedCreateFooter` |
| `PendingRequestRow` | 1 | `?isApproving: RoleAssignmentForm` |
| `PendingRequestsPanel` | 1 | `⚠h2→SectionHeader, LoadGate{ (n) PendingRequestRow }` |
| `PreviewPane` | 1 | `( ⚠button→Button, ( ?Request.IsPdf: PdfViewer \| ImageViewer ) )` |
| `ProfitSummaryStrip` | 1 | `[ (3) StatTile ]` |
| `ProfitTable` | 1 | `RecordsTable{ (11) SortableColumnHeader, (n) ProfitTableRow, ?Rows.Count > 1: ProfitTotalsRow }` |
| `ProfitTableNotes` | 1 | — |
| `ProfitTableRow` | 1 | `ProjectStageBadge, ?Row.CertifiedToDate == 0m && Ro…: Pill, MarginLine, MemoLine, (2) MarginLine, MemoLine, MarginLine` |
| `ProfitTotalsRow` | 1 | `MarginLine, MemoLine, (2) MarginLine, (2) MemoLine, MarginLine` |
| `ProgrammeClaimsWorkbench` | 1 | `?(Error ?? error) is { } shown: Notice, ( ?openForm == Form.Nod: (2) FormField \| ?openForm == Form.Eot: ( (2) FormField, [ (2) FormField ], FormField ) \| ?openForm == Form.Lad: ( (2) FormField, [ (4) FormField ], FormField ) ), LoadGate{ ⚠h3→SectionHeader, (n) Pill, ⚠h3→SectionHeader, (n) [ ?RelatedNodReference(eot) is { }…: Pill Pill ], ⚠h3→SectionHeader, (n) Pill }` |
| `ProgrammeDraftReview` | 1 | `?error is not null: Notice, SectionHeader{ ( ?CanEdit: ( Toolbar{ ToolbarButton }, InlineConfirm ) \| Pill ) }, ?!string.IsNullOrWhiteSpace(Deta…: Notice, LoadGate{ RecordsTable{ (n) ( Checkbox, [ (n) Pill ?CanEdit: ⚠button→Button ], ⚠input→FormField, Pill, ?mappingEditorLineId == row.Prog…: (n) Checkbox ) } }, ConfirmDialog` |
| `ProgrammeExtensionDaysEditor` | 1 | `[ (2) FormField ]` |
| `ProgrammeExtensionRows` | 1 | `?Rows.Count == 0: EmptyState, (n) ( Pill, ?CanEdit: ⚠button→Button, ?editingRequestId == row.Eot.Req…: ProgrammeExtensionDaysEditor )` |
| `ProgrammeGanttChart` | 1 | `(n) ( [ ⚠button→Button ?slip > 0: Pill (n) Pill ], ?PredecessorsOf(task.ProgrammeTa…: (n) Pill{ ⚠button→Button }, (n) ProgrammePushBar, ?editingTaskId == task.Programme…: ProgrammeTaskEditor ), ProgrammeVariationRows, ProgrammeExtensionRows` |
| `ProgrammeTaskEditor` | 1 | `[ (4) FormField ]` |
| `ProgrammeVariationEffectEditor` | 1 | `[ (3) FormField ], ?Row.Pushes.Count > 0: (n) ⚠button→Button` |
| `ProgrammeVariationRows` | 1 | `?Rows.Count == 0: EmptyState, (n) ( Pill, ?CanEdit: ⚠button→Button, (n) ProgrammePushBar, ?editingVariationId == row.Varia…: ProgrammeVariationEffectEditor )` |
| `ProgrammeWorkbench` | 1 | `?error is not null: Notice, LoadGate{ ?error is null: ( ( ?programme?.Baseline is { } base…: ( ?movement.CompletionSlipDays > 0: Notice{ (n) ⚠button→Button } \| Notice ) \| ?Tasks.Count > 0: Notice ), ?draft is { } openDraft: Notice, ( ?openForm == Form.AddTask: [ (3) FormField ] \| ?openForm == Form.AddLink: [ (3) FormField ] \| ?openForm == Form.Baseline: ( FormField, ?Baselines.Count > 0: (n) [ ?baselineEntry.ProgrammeBaseline…: Pill ?confirmingRemoveBaselineId == b…: (2) ⚠button→Button ⚠button→Button ] ) \| ?openForm == Form.DraftFromValua…: FormField ) ), ProgrammeDraftReview, ProgrammeGanttChart }` |
| `ProgressReportForm` | 1 | `FormField, [ (2) FormField ], (3) FormField, ( ?AvailableUpdates.Count == 0: EmptyState \| (n) ⚠button→Button ), ?error is not null: Notice` |
| `ProgressUpdateForm` | 1 | `FormField{ InputFile }, (4) FormField, [ (6) FormField ], ?error is not null: Notice` |
| `ProjectCashRow` | 1 | — |
| `ProjectCashTable` | 1 | `RecordsTable{ (n) ProjectCashRow, ?Rows.Count > 1: ProjectCashTotalsRow }` |
| `ProjectCashTotalsRow` | 1 | — |
| `ProjectContractAmendmentDialog` | 1 | `Modal{ (3) FormField }` |
| `ProjectContractPanel` | 1 | `Panel{ ( ?dataFailed: Notice \| ( ?CanManage: ( InputFile, ?uploadError is not null: Notice ), (n) ?CanManage: (2) ⚠button→Button, ?CanManage: ( [ (2) FormField ], FormField{ InputFile }, ?amendmentError is not null: Notice ), ( ?terms.IsAmended: Notice, ⚠dl→KeyValueList ) ) ) }, ?termsOpen: ProjectContractTermsDialog, ?editingAmendment is not null: ProjectContractAmendmentDialog, Modal{ ?removeError is not null: Notice }` |
| `ProjectContractTermsDialog` | 1 | `Modal{ [ (2) FormField ], FormField, [ (6) FormField ], [ (2) FormField ], (2) [ (3) FormField ], [ (4) FormField ], [ (6) FormField ] }` |
| `ProjectCorrespondencePanel` | 1 | `[ ⚠h3→SectionHeader ?loading: JewelIcon ], ?error is not null: Notice, ( ?partyContacts.Count > 0: ⚠table→RecordsTable, ⚠table→RecordsTable, ?CanManage: ( [ (3) FormField ], [ (2) FormField ?editingContactId is not null: ⚠button→Button ] ) )` |
| `ProjectDetailsEditor` | 1 | `?CanEdit: Modal{ ?error is not null: Notice, (4) FormField, ?partySelection.StartsWith(Archi…: FormField, [ (2) FormField ], (2) FormField, [ (2) FormField ], (3) FormField, ?CanDelete: ( FormField, ⚠button→Button ) }` |
| `ProjectRetentionPanel` | 1 | `⚠h3→SectionHeader, Modal{ ?error is not null: Notice, (3) [ (2) FormField ] }, Modal{ ?error is not null: Notice, FormField }` |
| `ProjectSelect` | 1 | `DropdownMenu{ ?AllowNone: ⚠button→Button }` |
| `ProjectTodoList` | 1 | `[ ⚠h2→SectionHeader ?!loading: [ (2) ⚠button→Button ] ], ?error is not null: Notice, LoadGate{ [ ( SearchInput, ?HasQuery: ⚠button→Button ) ⚠select→FormField ], ( ( ?boardView: TodoBoard \| (n) ⚠button→Button ), ?!boardView && done.Count > 0: ( ⚠h3→SectionHeader, (n) ⚠button→Button ) ), ?HasQuery: ( ⚠h3→SectionHeader, ( ?matchedRequests.Count > 0: (n) ⚠button→Button, ?matchedVariations.Count > 0: (n) ⚠button→Button, ?matchedDrawings.Count > 0: (n) ⚠button→Button ), TaggedEmailSearch ) }, Modal{ ?addError is not null: Notice, FormField, [ FormField{ TodoAssigneeSelect } FormField ], FormField }` |
| `ProjectsTable` | 1 | `⚠table→RecordsTable` |
| `ProposalFormModal` | 1 | `Modal{ ?error is not null: Notice, [ (3) FormField (n) ( (3) ⚠input→FormField, Checkbox ) (n) (3) ⚠input→FormField FormField ?Concepts.Count > 0: [ ⚠button→Button (n) ⚠button→Button ] ] }` |
| `QueueBulkActions` | 1 | `BulkSelectionBar{ (2) SearchSelect, ⚠button→Button, ⚠select→FormField }` |
| `RaiseRequestDialog` | 1 | `Modal{ RequestForm, ?attachmentWarning is not null: Notice }` |
| `RecentTriageFold` | 1 | `⚠h2→SectionHeader` |
| `ReconciliationPackageBuilderModal` | 1 | `Modal{ FormField, [ SearchInput ( SearchInput, (n) ?picked: ⚠input→FormField ) ], SearchInput, ( ?FilteredCostLines.Count == 0: EmptyState \| (n) ?picked: ⚠input→FormField ) }` |
| `RecordAgreedTenderPanel` | 1 | `Modal{ ?error is not null: Notice, (2) FormField }` |
| `RecordAttachmentList` | 1 | `?items is { Count: > 0 } attachm…: (n) ?PreviewFor(attachment) is { } p…: ⚠button→Button` |
| `RecordDocumentView` | 1 | `⚠button→Button, ⚠h2→SectionHeader, ?StatusLabel is { Length: > 0 } …: Pill, LoadGate, ?Record.Type == RecordType.Reque…: RecordAttachmentList, RecordEmailList` |
| `RecordEmailList` | 1 | `?emails.Count > 0: CorrespondenceThreadList` |
| `RecordExplorerPane` | 1 | `( ?openRecord is { } record: RecordDocumentView \| ( [ (2) ⚠select→FormField ], SearchInput, LoadGate{ (n) ⚠button→Button } ) )` |
| `RecordLinkSection` | 1 | `⚠input→FormField` |
| `RejectedWorkOrdersList` | 1 | `(n) [ Pill DropdownMenu ]` |
| `RelevantEventsList` | 1 | `?Error is not null: Notice, LoadGate{ (n) ( ?email.HasAttachments: Pill, [ ⚠button→Button ?CanDraftReply: ⚠button→Button ], ?replyForId == email.Id: ( ?replyError is not null: Notice, ⚠textarea→FormField ), ?replyDraftForId == email.Id && …: Notice ) }` |
| `RequestAccessView` | 1 | `⚠h1→PageHeader, ⚠dl→KeyValueList` |
| `RequestAttachmentsPanel` | 1 | `Panel{ ?CanEdit: InputFile, ?error is not null: Notice, (n) ?CanEdit: ⚠button→Button }, Modal{ ?pickerError is not null: Notice }` |
| `RequestConversation` | 1 | `Panel{ ?!string.IsNullOrWhiteSpace(Requ…: Jewel.JPMS.Features.Triage.Panels.EmailFinder, LoadGate{ (n) ( [ ?!string.IsNullOrEmpty(message.M…: Pill [ ?isInternal: Pill Pill ] ], ?string.IsNullOrEmpty(message.Ma…: ⚠button→Button, ?replies.Count > 0: (n) ConversationMessageCard, ?!string.IsNullOrEmpty(message.M…: ( [ ⚠button→Button ?CanDraftReply: (2) ⚠button→Button ], ?replyForMessageId == message.Me…: Notice, ?replyForMessageId == message.Me…: Notice ) ), ?error is not null: Notice, ?replyToId is not null: ⚠button→Button, ⚠textarea→FormField } }` |
| `RequestDetailEditModal` | 1 | `Modal{ ?error is not null: Notice, FormField }` |
| `RequestFactsEditModal` | 1 | `Modal{ ?error is not null: Notice, [ (2) FormField ?IsClosed: FormField ], (2) FormField, ?Record.Kind is not RequestType.…: FormField }` |
| `RequestFactsStrip` | 1 | `[ (3) MetaCell ?Record.Status is RequestStatus.…: MetaCell (2) MetaCell ?Record.Kind is not RequestType.…: MetaCell MetaCell ]` |
| `RequestForm` | 1 | `?error is not null: Notice, [ (2) ⚠button→Button ], ?kind == RequestType.Rfi: FormField, (2) FormField, [ (2) FormField ], FormField, ?ShowAttachments: ( InputFile, ?selectedRevisions.Count > 0 \|\| …: (2) (n) ⚠button→Button ), ?backfill: ( [ (2) FormField ], (2) FormField ), Modal` |
| `RequestHeaderBar` | 1 | `[ ( [ ?CanEdit: DropdownMenu Pill ?Record.ImpliesVariation: Pill ?Record.CriticalPath: Pill ], ⚠h2→SectionHeader ) DropdownMenu ]` |
| `RequestHeaderEditModal` | 1 | `Modal{ ?error is not null: Notice, [ (2) FormField ], FormField, ?status == RequestStatus.Closed: FormField }` |
| `RequestOfficialFormPanel` | 1 | `[ ⚠h3→SectionHeader ?Record.Kind.IsEmailable(): Toolbar{ ToolbarButton, ?CanDraftEmail: ToolbarButton } ], ?error is not null: Notice, ( ?!Editing: ( ?Record.ItemList.Count == 0 && s…: EmptyState \| ?Record.ItemList.Count > 0: ⚠table→RecordsTable ) \| ( (n) ( ⚠button→Button, [ (2) FormField ], (2) FormField ), (3) FormField ) )` |
| `RequestPartyPanel` | 1 | `⚠h3→SectionHeader, ?CanEdit: ( ?Record.HasRfq: Pill, ?Error is not null: Notice, FormField, ?Record.PartyKind == PartyKind.A…: FormField )` |
| `RequestResponsePanel` | 1 | `⚠h3→SectionHeader` |
| `RequestTable` | 1 | `RecordsTable{ (n) ( ?record.CriticalPath: Pill, ?record.MergedIntoRequestId is n…: Pill, ActivityBadge, ( ?CanChangeStatus: DropdownMenu \| Pill ) ) }` |
| `RequestVariationCard` | 1 | `⚠h3→SectionHeader, ?Error is not null: Notice` |
| `RevokedUserRow` | 1 | `[ ?busy: JewelIcon (2) ⚠button→Button ], Modal` |
| `RevokedUsersPanel` | 1 | `⚠h2→SectionHeader, LoadGate{ (n) RevokedUserRow }` |
| `RoleAssignmentForm` | 1 | `⚠select→FormField` |
| `RoleHome` | 1 | `PageHeader, ?Tiles.Count > 0: (n) MetricStat, ?ShowMyDay: MyDayWorkspace, ?ShowMyTodos: MyTodosPanel, ?ShowRequests: OpenRequestsPanel, ?ShowValuations: NextValuationsPanel, ?ShowExpiringDocuments: ExpiringDocumentsPanel, ?ShowStaleRates: ⚠h2→SectionHeader, ⚠h2→SectionHeader, (n) NavIcon` |
| `RoleOverridePrompt` | 1 | — |
| `RoleSwitcher` | 1 | `?Session.HasMultipleRoles && Ses…: DropdownMenu{ RoleBadge, (n) ⚠button→Button }` |
| `RunningProfitPanel` | 1 | `[ Toolbar{ (2) ToolbarButton, ToolbarDivider, ToolbarButton } ⚠input→FormField ], RunningProfitTable` |
| `RunningProfitTable` | 1 | `⚠table→RecordsTable` |
| `SalesInboxLeadPickerDialog` | 1 | `Modal{ FormField, ?leads is not null: (n) ⚠button→Button }` |
| `SalesInboxMessageList` | 1 | `(n) ⚠button→Button` |
| `SalesInboxThread` | 1 | `⚠h2→SectionHeader, ?actionError is not null: Notice, ?replyOpen && CanWork: ⚠textarea→FormField, ?thread is null: LoadGate, (n) SalesInboxThreadMessage, SalesInboxLeadPickerDialog` |
| `SalesInboxThreadMessage` | 1 | `⚠button→Button, ?IsExpanded: ?detail is null: LoadGate` |
| `SendLinesModal` | 1 | `Modal{ ?IsOpen: ( ?Error is not null: Notice, SearchSelect ) }` |
| `SettlementLineModal` | 1 | `Modal{ (5) FormField, ?error is not null: Notice }` |
| `SettlementSchedulesPanel` | 1 | `Panel{ ( [ ?Schedules.InvoicesToChase > 0: Pill ?Schedules.WorkersToReconcile > 0: Pill ], ⚠table→RecordsTable ) }, Modal` |
| `SettlementSummaryTable` | 1 | `⚠table→RecordsTable` |
| `SideNav` | 1 | `JewelIcon, ⚠button→Button, NavIcon, ?ShowsProjectPicker && IsExpanded: DropdownMenu, ( ?RendersFlat: (n) ( ?IsUnresolvable(item): NavIcon \| NavIcon ) \| (n) ( ?IsExpanded: ( NavIcon, ⚠button→Button ) \| NavIcon ) ), ?StandaloneItems.Count > 0: (n) NavIcon` |
| `SiteCostTable` | 1 | `Panel{ ⚠table→RecordsTable }` |
| `SitePhotoCard` | 1 | `⚠button→Button, ( ?Photo.IsFiled: Pill \| Pill ), ?CanDelete: InlineConfirm` |
| `SitePhotoDropZone` | 1 | `InputFile` |
| `SiteRegisterPanel` | 1 | `Panel{ ⚠table→RecordsTable }` |
| `SourceEmailCard` | 1 | `⚠button→Button` |
| `StaffDirectoryTable` | 1 | `( ?Staff.Count == 0: LoadGate{ EmptyState } \| RecordsTable{ (n) (n) RoleBadge } )` |
| `StageBuildingControlInspectionAction` | 1 | `FormField, [ (2) FormField ], FormField` |
| `StageCalendarEventAction` | 1 | `FormField, (2) [ (2) FormField ], FormField` |
| `StageDirectoryContactAction` | 1 | `DirectoryContactForm` |
| `StageKpiAction` | 1 | `FormField{ KpiPersonPicker }, ⚠textarea→FormField, ⚠button→Button` |
| `StageRequestTransitionAction` | 1 | `?error is not null: Notice, SearchSelect, ?Kind == SystemActionKind.CloseR…: ⚠input→FormField` |
| `StageTenderResponseAction` | 1 | `?error is not null: Notice, SearchSelect` |
| `StageTodoCompleteAction` | 1 | `?error is not null: Notice, ⚠select→FormField` |
| `StageTodosAction` | 1 | `( ?todoEditIndex is { } activeInde…: ( ⚠button→Button, FormField, [ ( ?row.Assignees.Count > 0: (n) ⚠button→Button, TodoAssigneeSelect ) FormField ], FormField, ⚠button→Button ) \| ( ?TitledTodoRows.Count == 0: EmptyState \| ⚠button→Button ) )` |
| `StageVariationAction` | 1 | `ManualVariationForm` |
| `StageVariationDecisionAction` | 1 | `?error is not null: Notice, ( ?ProjectId == "": ⚠select→FormField \| ⚠select→FormField ), ?Kind == SystemActionKind.Approv…: VariationApprovePanel` |
| `StagedBuildUpPanel` | 1 | `⚠h3→SectionHeader, Modal{ VariationApprovePanel, (3) FormField }` |
| `StagedCreateFooter` | 1 | `Notice` |
| `StagedLeadFields` | 1 | `[ (9) FormField ], FormField` |
| `StagedRecordActionEditor` | 1 | `( ?Kind == StagedRecordKind.Request: ( (2) FormField, [ (2) FormField ] ) \| ?Kind == StagedRecordKind.BidPac…: (2) FormField \| ?Kind == StagedRecordKind.WorkOr…: ( [ (2) FormField ], FormField, [ (2) FormField ], FormField, (n) ( [ SearchSelect (2) ⚠input→FormField ⚠button→Button ], ⚠textarea→FormField ), ⚠button→Button, ?Create.DepositRequired: ⚠input→FormField, InputFile, ?Create.UploadFiles.Count > 0: (n) ⚠button→Button, ?!Create.SaveAsDraft: ( ?WorkOrderSupplier is { } woSupp…: Notice \| Notice ) ) \| ?Kind == StagedRecordKind.SiteIn…: (3) FormField \| ?Kind == StagedRecordKind.Invent…: (4) FormField \| ?Kind == StagedRecordKind.Lead: StagedLeadFields \| (3) FormField )` |
| `StrategyApproachPlanPanel` | 1 | `Panel{ LoadGate{ SimpleMarkdown } }` |
| `StrategyArgumentPanel` | 1 | `Panel{ ⚠dl→KeyValueList }` |
| `StrategyFunnelPanel` | 1 | `Panel` |
| `StrategyFunnelTiles` | 1 | `[ (6) StatTile ]` |
| `StrategyLeadsPanel` | 1 | `Panel{ ⚠table→RecordsTable }` |
| `StrategyPageHeader` | 1 | `PageHeader{ Pill, ?CanDecide: ⚠select→FormField }` |
| `StrategyPlanEditDialog` | 1 | `Modal{ ⚠textarea→FormField }` |
| `StrategyPlanGenerateDialog` | 1 | `Modal{ FormField }` |
| `StrategyResearchStatusNotice` | 1 | `( ?Strategy.ResearchStatus.IsInPro…: JewelIcon \| ?Strategy.ResearchStatus == Stra…: Notice )` |
| `SubcontractorComplianceList` | 1 | `⚠h2→SectionHeader, (n) ComplianceStatusPill, Modal{ ?addError is not null: Notice, [ (2) FormField ], FormField, FormField{ InputFile } }, Modal{ ?editError is not null: Notice, [ (2) FormField ] }` |
| `SubcontractorInvitePickerModal` | 1 | `Modal{ [ ⚠input→FormField ?InvitableTrades.Count > 1: ⚠select→FormField ], ( ?Invitable.Count == 0: EmptyState \| (n) ?string.IsNullOrWhiteSpace(sub.C…: Pill ), [ (2) ⚠input→FormField ⚠select→FormField ] }` |
| `SubcontractorStatementModal` | 1 | `Modal{ ( ?loadError is not null: Notice \| ( RecordsTable, ( ?drafted: Notice \| ( RecordEmailPreviewPanel, FormField, FormField{ RichTextEditor } ) ), ?error is not null: Notice ) ) }` |
| `SupplierAccountInvoicesTable` | 1 | `RecordsTable{ (n) ( ?invoice.IsCreditNote: Pill, Pill, ?invoice.Placement != ProjectSup…: ?invoice.Placement != ProjectSup…: Pill ) }` |
| `SupplierAccountModal` | 1 | `Modal{ ( ?loadError is not null: Notice \| LoadGate{ ?account is not null: ( SupplierAccountSummary, SectionHeader, SupplierAccountOrdersTable, SectionHeader, SupplierAccountInvoicesTable ) } ) }` |
| `SupplierAccountOrdersTable` | 1 | `RecordsTable{ (n) Pill }` |
| `SupplierAccountSummary` | 1 | `[ (6) StatTile ], Notice` |
| `SupplierGroupsModal` | 1 | `Modal{ ( ?!editorOpen: EmptyState \| (2) FormField ) }` |
| `TenderInviteComposerModal` | 1 | `Modal{ ?sendError is not null: Notice, ?draftSavedAt is { } savedAt: Notice, ?MissingEmail.Count > 0: Notice, ?LineItems.Count == 0: Notice, (3) FormField{ RecipientInput }, FormField, ⚠button→Button, ?editingBody: RichTextEditor }` |
| `TenderQuoteComparisonTable` | 1 | `RecordsTable{ (n) ?won: Pill, ?AwardEnabled: (n) ⚠button→Button }` |
| `TenderSubmissionModal` | 1 | `Modal{ ?saveError is not null: Notice, LoadGate{ ( ?issues.Count > 0: Notice \| ?sourceEmail is not null && comp…: Notice ), FormField, ⚠table→RecordsTable, FormField } }` |
| `TenderSubmissionsSection` | 1 | `[ ⚠h3→SectionHeader ?CanEdit && HasRecipients: ⚠button→Button ], RecordCorrespondencePanel{ ?DispositionOf(email) is { Outco…: Pill, ?CanEdit: ?!extracted: ⚠button→Button }, ?DiscardedEmails.Count > 0: CorrespondenceThreadList{ ?DispositionOf(email) is { } ver…: Pill, ?CanEdit: ⚠button→Button }, ?AwardNote is not null: Notice, ?Awarded is { } awarded: ( Notice{ ?CanEdit: ⚠button→Button }, ?WorkOrderEmailNote is not null: Notice ), LoadGate{ TenderQuoteComparisonTable }` |
| `TimesheetApprovalFooter` | 1 | `SearchSelect` |
| `TodoActivityPanel` | 1 | `Panel{ ?formOpen: TodoProgressForm }` |
| `TodoCommunicationsPanel` | 1 | `Panel{ EmailFinder, Toolbar{ ?CanSend: ToolbarButton, ToolbarButton }, ?failed is not null: Notice, ?sentNote is not null: Notice, ?composingNew: TodoEmailComposer, ?replyingTo is { } replyAnchor: TodoEmailComposer, UnfiledRepliesNotice, ?emails.Count > 0: CorrespondenceThreadList }` |
| `TodoEmailComposer` | 1 | `MailReplyComposer` |
| `TodoFactsPanel` | 1 | `Panel{ ⚠dl→KeyValueList }` |
| `TodoProgressForm` | 1 | `⚠textarea→FormField` |
| `TrajectoryPanel` | 1 | — |
| `TriageBar` | 1 | `[ ProjectSelect ( (2) TriageDecisionRow, ?ThreadTags is { Count: > 0 } in…: TriageDecisionRow ) ], ?ActionError is not null: Notice, ?DiscardArmed: Notice, ⚠button→Button` |
| `TriageDecisionRow` | 1 | `[ (2) ⚠button→Button ]` |
| `UnassignedRequestsPanel` | 1 | `[ ⚠h2→SectionHeader Pill ], ?Error is not null: Notice` |
| `UnfiledRepliesNotice` | 1 | — |
| `UnpaidXeroInvoicesModal` | 1 | `Modal{ ( ExportToExcelButton, ?Lines.Count > 0: ⚠table→RecordsTable, ?UnallocatedBills.Count > 0: ⚠table→RecordsTable ) }` |
| `UnpricedWorkOrdersList` | 1 | `⚠h3→SectionHeader, (n) [ ?CancelPendingFor(detail): (2) ⚠button→Button DropdownMenu ]` |
| `UsefulInformationPanel` | 1 | `⚠h2→SectionHeader, ?error is not null: Notice, LoadGate{ ( ?notes is { Count: 0 }: EmptyState \| ?notes is not null: ( ?notes.Count > FilterThreshold: ( SearchInput, ?HasFilter: ⚠button→Button ), (n) ⚠button→Button ) ) }, Modal{ ?dialogError is not null: Notice, (2) FormField }` |
| `ValuationClaimCorrespondenceSection` | 1 | `⚠button→Button, ( ?isOpen: ( ?Claim is null: EmptyState \| LoadGate{ ?loadError is not null: Notice } ) \| ?emails is not null: CorrespondenceThreadList )` |
| `ValuationInvoicePaymentSyncModal` | 1 | `Modal{ ?error is not null: Notice, LoadGate{ ?preview is not null: ( (n) Notice, ?preview.Blockers.Count == 0: LoadGate{ RecordsTable{ (n) Pill } } ) } }` |
| `ValuationInvoiceXeroRaiseSummary` | 1 | `⚠dl→KeyValueList` |
| `ValuationInvoicesSection` | 1 | `[ ⚠button→Button Toolbar{ ?CanManage: ( ToolbarButton, ToolbarDivider ), ExportToExcelButton } ], ?isOpen: ( ?xeroRaiseNote is not null: Notice, ⚠table→RecordsTable, ?CanManage: [ (2) FormField ?newIsHistoric: (2) FormField ] ), ValuationInvoiceXeroRaiseModal, ValuationInvoicePaymentSyncModal, (3) Modal{ FormField }, Modal{ ?editInvoice is not null: ( (2) FormField, ?editInvoice.IsManual: FormField, FormField ) }` |
| `ValuationLineForm` | 1 | `[ (2) FormField ], [ ?elementType == ValuationElement…: (2) FormField (2) FormField ], [ FormField{ SearchSelect } (2) FormField ], [ (3) FormField ], FormField` |
| `ValuationLinePickerModal` | 1 | `Modal{ ( ?lines is null && !loadFailed: LoadGate \| ?SelectableLines.Count == 0: EmptyState \| ( ⚠input→FormField, RecordsTable{ (n) (n) ( Pill, ?LineTypeBadge(line) is { } badge: Pill ) } ) ) }` |
| `ValuationReportTable` | 1 | `( ?CanEditEntries: BulkPercentToolbar, (n) ( ValuationSectionHeader, ?isOpen: ⚠table→RecordsTable ) ), ?lines.Count > 0: ValuationSummaryPanel` |
| `ValuationSectionHeader` | 1 | `⚠button→Button` |
| `ValuationStatementEmailModal` | 1 | `Modal{ ?Claim is { } snapshot: ( RecordsTable, ( ?drafted: Notice \| ( RecordEmailPreviewPanel, FormField, FormField{ RichTextEditor } ) ), ?error is not null: Notice ) }` |
| `ValuationStatementViewer` | 1 | `( ( ?detail.IsDraft: Notice \| Notice ), Toolbar{ ToolbarButton, ExportToExcelButton, ?OnEmailRequested.HasDelegate &&…: ( ToolbarDivider, ToolbarButton ) }, (n) ?valuationSection.Lines.Count > 0: ⚠table→RecordsTable ), ⚠h3→SectionHeader, ⚠dl→KeyValueList, ⚠h3→SectionHeader, LoadGate{ ?emailsError is not null: Notice, CorrespondenceThreadList }` |
| `ValuationSummaryPanel` | 1 | `⚠h3→SectionHeader, ⚠dl→KeyValueList` |
| `VariationApproveOffer` | 1 | `Modal{ VariationApprovePanel }` |
| `VariationConversation` | 1 | `Panel{ LoadGate{ (n) ConversationMessageCard, ConversationComposer } }` |
| `VariationDetailsCard` | 1 | `LoadGate{ ?EditingEstimate && CanEditEstim…: FormField }` |
| `VariationDocumentPanel` | 1 | `⚠h3→SectionHeader, ?error is not null: Notice, (3) FormField` |
| `VariationDraftModal` | 1 | `Modal{ ?Error is not null: Notice, (3) FormField, ?Lines.Count > 0: (n) ?row.Accepted: ⚠select→FormField }` |
| `VariationHeaderBar` | 1 | `[ ( [ ?StatusMenuItems.Count > 0: DropdownMenu Pill ], ⚠h2→SectionHeader ) DropdownMenu ]` |
| `VariationLinesTable` | 1 | `⚠h3→SectionHeader, ⚠table→RecordsTable` |
| `VariationOrderEmailModal` | 1 | `Modal{ ?Order is { } order: ( ?outcome is null: ( FormField, RecordEmailPreviewPanel ), ?error is { } message: Notice, ?outcome is { } result: Notice ) }` |
| `WeekEntryModal` | 1 | `Modal{ FormField, ⚠table→RecordsTable, ?error is not null: Notice }` |
| `WeeklyCashflowGrid` | 1 | `RecordsTable{ CashflowAddItemRow, ?IsDirector && View.Closing is n…: WeeklyClosingBalanceRow }` |
| `WeeklyClosingBalanceRow` | 1 | — |
| `WeeklyKpiStrip` | 1 | `[ ?IsDirector: StatTile StatTile ?IsDirector: (2) StatTile (2) StatTile ]` |
| `WeeklySignOffTable` | 1 | `Panel{ ⚠table→RecordsTable }` |
| `WhatsAppUnattributedMessages` | 1 | `?Messages.Count > 0: ( Notice, RecordsTable )` |
| `WhatsAppWeekExportForm` | 1 | `[ FormField FormField{ InputFile } ], FormField` |
| `WhatsAppWeekReview` | 1 | `(n) Notice, SectionHeader, RecordsTable, WhatsAppUnattributedMessages, ?Preview.RepeatedPhotos.Count > 0: Notice, ?Preview.MissingMediaFileNames.C…: Notice` |
| `WorkOrderAttachmentsPanel` | 1 | `Panel{ ?CanEdit: InputFile, ?error is not null: Notice, (n) ?CanEdit: ⚠button→Button }` |
| `WorkOrderBillCard` | 1 | `( Pill, ?Match.AmountNote is not null: Pill ), WorkOrderBillOrderSlices, WorkOrderBillLinesTable, ( ?Error is not null: Notice \| ?!WorkOrderBillTracking.CanBeWri…: Notice )` |
| `WorkOrderBillLinesTable` | 1 | `⚠table→RecordsTable` |
| `WorkOrderBillOrderSlices` | 1 | `⚠table→RecordsTable` |
| `WorkOrderBillsStrip` | 1 | — |
| `WorkOrderEmailModal` | 1 | `Modal{ FormField, FormField{ RichTextEditor } }` |
| `WorkOrderForm` | 1 | `?error is not null: Notice, [ (2) FormField ], FormField, [ (2) FormField ], FormField, (n) ( [ SearchSelect (5) ⚠input→FormField ⚠button→Button ], ⚠textarea→FormField ), ⚠button→Button, ?depositRequired: ⚠input→FormField, ?!IsEditing: ?!saveAsDraft: ( ?SelectedSupplier is { } supplie…: Notice \| Notice )` |
| `WorkOrderLineRecodeModal` | 1 | `Modal{ (n) [ SearchSelect ⚠input→FormField ⚠button→Button ], ⚠button→Button }` |
| `WorkOrderSaleWarningDialog` | 1 | `Modal` |
| `WorkOrdersTable` | 1 | `⚠table→RecordsTable` |
| `WorkerPlacementTable` | 1 | `Panel{ ⚠table→RecordsTable }` |
| `XeroContactPeopleList` | 1 | `⚠h3→SectionHeader, ⚠dl→KeyValueList` |
| `XeroContactPushModal` | 1 | `Modal{ ?error is not null: Notice, LoadGate{ ?preview is not null: ( (n) Notice, [ (2) XeroContactPeopleList ] ) } }` |
| `XeroExplorerPane` | 1 | `?openTransaction is { } transact…: XeroTransactionView, ( ?loadFailed: Notice \| LoadGate ), ?snapshot.Error is { } error: Notice, [ SearchInput ⚠select→FormField ], ( ?FilteredTransactions.Count == 0: EmptyState \| (n) ⚠button→Button )` |
| `XeroImportModal` | 1 | `Modal{ ?error is not null: Notice, FormField, ( ?snapshot is null: ?error is null: LoadGate \| ?snapshot.Error is not null: Notice \| ⚠table→RecordsTable ) }` |
| `XeroLinkModal` | 1 | `Modal{ Checkbox, ?error is not null: Notice, FormField, ( ?snapshot is null: ?error is null: LoadGate \| ?snapshot.Error is not null: Notice \| ⚠table→RecordsTable ) }` |
| `XeroTransactionView` | 1 | `⚠button→Button, ⚠h2→SectionHeader, ?Transaction.HasAttachments: (n) ?IsPreviewable(attachment): ⚠button→Button` |
| `AdminIntegrations` | 0 | `Page{ ( WorkspaceSectionNav, PageHeader, Panel ), ( ?status.LastRefreshError is { Le…: Notice, ?!confirmingDisconnect: ⚠button→Button ), ?actionError is not null: Notice }` |
| `AdminKpis` | 0 | `Page{ ( WorkspaceSectionNav, PageHeader, Panel{ SearchSelect, ?actionError is not null: Notice, ?actionNote is not null: Notice, ( ?personFilter == "": (n) ⚠button→Button, RecordsTable ) }, Modal{ ?editing is not null: ( ?formError is not null: Notice, FormField{ KpiPersonPicker }, ⚠textarea→FormField ) }, Modal{ ?formError is not null: Notice, FormField } ) }` |
| `AdminRevokedUsers` | 0 | `Page{ ( WorkspaceSectionNav, PageHeader, RevokedUsersPanel ) }` |
| `AdminSystem` | 0 | `Page{ ( WorkspaceSectionNav, PageHeader, LoadGate{ (2) Panel }, ?terms.Configured: InputFile ) }` |
| `AdminTrades` | 0 | `Page{ ( WorkspaceSectionNav, PageHeader, Panel{ ⚠input→FormField, ?actionError is not null: Notice, ?actionNote is not null: Notice, ( ?TradesModel.Current is { Count:…: EmptyState \| RecordsTable{ (n) ?renamingId == trade.TradeId: ⚠input→FormField } ) } ) }` |
| `AdminUsers` | 0 | `Page{ ( WorkspaceSectionNav, PageHeader, ApprovedUsersPanel ) }` |
| `AgedPayables` | 0 | `Page{ PageHeader{ Toolbar{ ExportToExcelButton, ToolbarDivider, ToolbarButton } }, LoadGate{ ( [ ⚠h2→SectionHeader [ (2) ⚠button→Button ] ], RecordsTable ) } }` |
| `AgedReceivables` | 0 | `Page{ PageHeader{ Toolbar{ ExportToExcelButton, ToolbarDivider, ToolbarButton } }, LoadGate{ ( [ ⚠h2→SectionHeader [ (2) ⚠button→Button ] ], RecordsTable ) } }` |
| `AgentActivityLog` | 0 | `Page{ PageHeader, [ (2) ⚠button→Button ], LoadGate{ ⚠table→RecordsTable } }` |
| `AiActionsAdmin` | 0 | `Page{ PageHeader, LoadGate{ ( ⚠input→FormField, ?OrphanedAttachments.Count > 0: (n) ⚠button→Button, (n) ( ⚠button→Button, ?IsExpanded(area): (n) ⚠button→Button ) ) } }` |
| `AiConnections` | 0 | `Page{ PageHeader, ?error is not null: Notice, LoadGate{ RecordsTable } }` |
| `AiSkillsAdmin` | 0 | `Page{ PageHeader, ( ?editing is null: LoadGate{ ( ?skills!.Count == 0: EmptyState \| ⚠table→RecordsTable ) } \| ( [ ⚠input→FormField ⚠select→FormField ], ⚠input→FormField, (2) ⚠textarea→FormField, ?!isNew: ( ⚠h2→SectionHeader, ?references.Count > 0: (n) ⚠button→Button, ( [ (2) ⚠input→FormField ], ⚠input→FormField, ⚠textarea→FormField ) ) ) ) }` |
| `AllocatedSummaryRow` | 0 | `LedgerLineIdentityCell, ?Mode == XeroAllocationStatus.Al…: ⚠button→Button, ⚠button→Button, ?Mode == XeroAllocationStatus.Al…: ⚠button→Button` |
| `AllocationTableHeader` | 0 | — |
| `App` | 0 | `KeyedPageRouteView, FocusOnNavigate, ⚠h1→PageHeader` |
| `Architects` | 0 | `Page{ PageHeader{ ExportToExcelButton }, ?loadError is not null: Notice, LoadGate{ ( ?architects.Count == 0: EmptyState \| RecordsTable{ (n) (2) ⚠button→Button } ) }, PartyContactsEditor, (2) Modal{ ?formError is not null: Notice, FormField, [ (2) FormField ] } }` |
| `AuditTrail` | 0 | `Page{ ( ?!CanAccess: PageHeader \| ( PageHeader, [ ⚠button→Button (n) ⚠button→Button (2) ⚠select→FormField ], ?loadError is not null: Notice, LoadGate{ RecordsTable{ (n) ?!string.IsNullOrEmpty(item.Path…: Pill } } ) ) }` |
| `CashForecast` | 0 | `Page{ PageHeader, Notice, ( [ ?Projects.Current is null: ⚠select→FormField ProjectMultiSelect ExportToExcelButton ], LoadGate{ ?IsDirector: ForecastKpiStrip, ( ?FailedSelectedProjects.Count > 0: Notice{ ⚠button→Button }, CashForecastTable, ?IsDirector && forecast.Closing.…: ForecastBalanceChart, Notice, CombinedStatementCard, ProjectCashTable ) } ) }` |
| `CashSummary` | 0 | — |
| `CashflowBandSection` | 0 | `⚠button→Button, ?Expanded: ( (n) CashflowGroupRow{ CashflowEntryRow }, (n) CashflowEntryRow, (n) ⚠button→Button )` |
| `ChatIcon` | 0 | — |
| `ClientDefectForm` | 0 | `⚠h2→SectionHeader, (2) FormField` |
| `ClientPortalHome` | 0 | `Page{ ( ?!CanAccess: PageHeader \| ?!HasLinkedRecord: PageHeader \| ( PageHeader, ?loadError is not null: Notice, LoadGate{ ⚠h2→SectionHeader, ClientRequestList, ⚠h2→SectionHeader, ClientVariationList } ) ) }` |
| `ClientRequestView` | 0 | `Page{ LoadGate{ ( ?record is null: PageHeader \| ( PageHeader, ?!string.IsNullOrWhiteSpace(reco…: Panel, ?!string.IsNullOrWhiteSpace(reco…: Panel, ClientConversationPanel ) ) } }` |
| `ClientVariationView` | 0 | `Page{ LoadGate{ ( ?record is null: PageHeader \| ( PageHeader, ?!string.IsNullOrWhiteSpace(reco…: Panel, ClientConversationPanel ) ) } }` |
| `Clients` | 0 | `Page{ PageHeader{ ExportToExcelButton }, ?loadError is not null: Notice, LoadGate{ ( ?clients.Count == 0: EmptyState \| RecordsTable{ (n) (3) ⚠button→Button } ) }, PartyContactsEditor, Modal{ ?formError is not null: Notice, FormField, [ (2) FormField ] }, Modal{ ?inviteError is not null: Notice, ?inviteResult is not null: Notice }, Modal{ ?formError is not null: Notice, FormField, [ (2) FormField ] } }` |
| `ComplianceRegister` | 0 | `Page{ ( ?!CanAccess: PageHeader \| ( PageHeader{ Toolbar{ ExportToExcelButton } }, TabRow, [ SearchInput FilterChips ], ( ?dataFailed: Notice \| RecordsTable{ (n) ComplianceStatusPill } ) ) ) }` |
| `ConnectAuthorize` | 0 | `( JewelIcon, LoadGate{ ( ⚠h1→PageHeader, ?approveError is not null: Notice ) } )` |
| `CostCodes` | 0 | `Page{ WorkspaceSectionNav, PageHeader{ Toolbar{ ToolbarButton } }, [ (3) ⚠button→Button ], ?loadError is not null: Notice, ( ?activeTab == CostCodesTab.Ours: LoadGate{ ( ?costCodes.Count == 0: EmptyState \| ( ExportToExcelButton, RecordsTable{ (n) ( ( ?costCode.IsActive: Pill \| Pill ), (2) ⚠button→Button ) } ) ) } \| LoadGate{ RecordsTable{ (n) ( ?option.IsArchived: Pill \| Pill ) } } ), Modal{ ?formError is not null: Notice, (3) FormField }, Modal{ ( ?gaps.Error is not null: Notice \| ( ⚠textarea→FormField, ⚠table→RecordsTable ) ) }, Modal{ ?formError is not null: Notice, (3) FormField } }` |
| `Dashboard` | 0 | `Page{ ( ?Session.ActiveRole == Role.Admin: AdminHome \| ?Session.ActiveRole is not null: RoleHome ) }` |
| `DiscardedEmailPanel` | 0 | `?ActionError is not null: Notice, TriageMessageDetail` |
| `DiscardedInboxList` | 0 | `LoadGate{ LoadGate{ (n) TriageEmailRow, EmailListPager } }` |
| `DisputedLineRow` | 0 | `LedgerLineIdentityCell, (2) SearchSelect, ⚠button→Button` |
| `DocumentControl` | 0 | `Page{ PageHeader{ SearchSelect }, [ (3) ⚠button→Button ], ?loadError is not null: Notice, LoadGate{ (n) DocumentListItem }, LoadGate{ ⚠h2→SectionHeader, ?actionError is not null: Notice, DocumentOutcomeCard, DocumentPreview, SourceEmailCard, ?item.Status == DocumentControlS…: ( [ (3) ⚠button→Button ], ( ?destination == FileDestination.…: ( [ FormField ?drawingIsRevision: FormField FormField ], [ ?!drawingIsRevision: FormField FormField ?!drawingIsRevision: DrawingFolderPicker ] ) \| ?destination == FileDestination.…: ( [ (2) FormField ], [ (3) FormField ] ) \| [ FormField{ SearchSelect } (3) FormField ] ) ) } }` |
| `EnvelopeLine` | 0 | — |
| `EstimateLineRow` | 0 | `?Editable: ( ⚠select→FormField, (4) ⚠input→FormField )` |
| `FinanceOverview` | 0 | — |
| `ForgotPassword` | 0 | `JewelIcon, ( ?sent: ( JewelIcon, ⚠h1→PageHeader ) \| ( ⚠h1→PageHeader, ⚠input→FormField ) )` |
| `Imagine` | 0 | `JewelIcon, LoadGate{ ( ?view is null: ⚠h1→PageHeader \| ( ⚠h1→PageHeader, ?error is not null: Notice, ?v.Proposal is { } proposal: ImagineProposal, ( ?v.Rounds.Count == 0: ImagineSubmissionForm \| ( ImagineRoundList, ⚠h2→SectionHeader ) ) ) ) }` |
| `InboxPaneHeader` | 0 | `[ (2) ⚠button→Button ], ?LoadError is not null: Notice` |
| `InlineValueEditor` | 0 | `[ ⚠input→FormField (2) ⚠button→Button ]` |
| `LabourLineRow` | 0 | `LedgerLineIdentityCell, ?Line.MatchedWorkerName is not n…: ?Line.MatchedSubcontractorId is …: SearchSelect, ( ?Line.CoveredByTimesheets: Pill \| ⚠button→Button ), DropdownMenu` |
| `LabourOverview` | 0 | `Page{ SectionHeader{ Toolbar{ ToolbarButton, ExportToExcelButton } }, ?actionError is not null: Notice, ?weekSummaryLines is not null: Notice, LabourForecastHeader, (n) ⚠button→Button, ( ?view == "workers": WorkerPlacementTable \| ?view == "sites": SiteCostTable \| ?view == "costcodes": CostCodeTable \| ?view == "signoff": WeeklySignOffTable \| SettlementSchedulesPanel ), ?snapshot is not null && snapsho…: ChaseListPanel, AbsenceModal, SettlementLineModal, CodingResetModal, LabourBillApproveDialog, WeekEntryModal }` |
| `LabourXeroMapping` | 0 | `Page{ SectionHeader{ Toolbar{ ToolbarButton } }, ?actionError is not null: Notice, LoadGate{ Panel{ ( [ (2) FormField ], ( ?openSites.Count == 0: EmptyState \| ⚠table→RecordsTable ) ) }, Panel{ ( [ (4) FormField ], ( ?openCodes.Count == 0: EmptyState \| ⚠table→RecordsTable ) ) } } }` |
| `LandingLayout` | 0 | `ErrorToast, UpdateToast` |
| `LegacyDrawingsRedirect` | 0 | `LoadGate` |
| `LegacyValuationSnapshotsRedirect` | 0 | `LoadGate` |
| `Login` | 0 | `JewelIcon, ⚠h1→PageHeader, (2) ⚠input→FormField` |
| `Logout` | 0 | — |
| `MainLayout` | 0 | `ErrorToast, UpdateToast, RoleOverridePrompt, ?CanSeeNavigation: SideNav, [ ?CanSeeNavigation: PageHeading ?Auth.IsSignedIn: RoleSwitcher ]` |
| `MetaRow` | 0 | — |
| `MyDay` | 0 | — |
| `Pagination` | 0 | — |
| `PathwayPane` | 0 | `⚠h2→SectionHeader, ?HasOpenEmail && Picked.Count > 0: (n) ⚠button→Button, ?HasActionsTab: [ (2) ⚠button→Button ], ?activeTab == PaneTab.Tagging \|\|…: ( ?HasOpenEmail && NeedsProject &&…: Notice, ⚠button→Button, ?IsOpenSection(typeKey): RecordLinkSection ), ?HasOpenEmail && Config.Pathway …: KpiTagSection, ?Config.Family is { } family: ( [ ⚠button→Button ?HasOpenEmail: ( ?IsPicked(record): ⚠button→Button \| ⚠button→Button ) ], ?IsOpenSection(sectionKey): CategoryRegisterSection ), ?HasActionsTab && activeTab == P…: PathwayActionsSection` |
| `PaymentCertificates` | 0 | `Page{ PageHeader, SearchSelect, ?loadError is not null: Notice, LoadGate{ (n) ⚠table→RecordsTable } }` |
| `Policies` | 0 | `Page{ SectionHeader, ?actionError is not null: Notice, Panel{ (n) FormField }, ?IsAdminView && PolicyDocs.Curre…: Panel{ ⚠table→RecordsTable }, Modal{ (3) FormField, ?publishError is not null: Notice } }` |
| `PortalHome` | 0 | `Page{ ( ?!CanAccess: PageHeader \| ?!HasLinkedRecord: PageHeader \| LoadGate{ ( ?myRecord is null: PageHeader \| ( PageHeader, ?ExpiringOrExpired.Count > 0: Notice, ⚠h2→SectionHeader, (n) ComplianceStatusPill, ⚠h2→SectionHeader, ?uploadError is not null: Notice, ?uploadNote is not null: Notice, [ (3) FormField ], FormField{ InputFile }, ⚠h2→SectionHeader, (n) Pill, ⚠h2→SectionHeader, (n) ( Pill, ?variationRequest.IsOpen: ⚠button→Button ), ⚠h2→SectionHeader, ?variationError is not null: Notice, ?variationNote is not null: Notice, ( [ (2) FormField ], (2) FormField ) ) ) } ) }` |
| `PortalWorkOrderView` | 0 | `Page{ ( ?!CanAccess \|\| !HasLinkedRecord: PageHeader \| LoadGate{ ( ?Order is null: PageHeader \| ( ?Order.Order.IsAccepted: Pill, ?acceptError is not null: Notice, ?acceptNote is not null: Notice, PurchaseOrderSheet ) ) } ) }` |
| `ProfitSummary` | 0 | `Page{ PageHeader, ( [ ?Projects.Current is null: ⚠select→FormField ProjectMultiSelect ExportToExcelButton ], ?selectionInitialised && Selecte…: RunningProfitPanel, LoadGate{ ?!selectionInitialised \|\| Select…: ProfitSummaryStrip, ( ?FailedSelectedProjects.Count > 0: Notice{ ⚠button→Button }, ?bridge is not null: BudgetForecastBridge, ProfitTable, ProfitTableNotes ) }, ?selectionInitialised && Selecte…: TrajectoryPanel, ?selectionInitialised && Selecte…: CumulativePositionPanel ) }` |
| `ProjectArchitectInstructions` | 0 | `Page{ ProjectPageShell{ ⚠h2→SectionHeader, ?error is not null: Notice, Panel{ ⚠table→RecordsTable } }, Modal{ ?dialogError is not null: Notice, [ (2) FormField ], (3) FormField, FormField{ InputFile } }, Modal{ ?dialogError is not null: Notice, (n) ?alreadyLinked: ⚠button→Button }, Modal{ ?dialogError is not null: Notice } }` |
| `ProjectBidPackageInviteDetail` | 0 | `Page{ ProjectPageShell{ ?error is not null: Notice, LoadGate{ ( [ [ ⚠h2→SectionHeader Pill ] ?CanManage && package.Status != …: DropdownMenu ], (n) ⚠button→Button, ?activeTab == "tender-list": InvitedSubcontractorsSection, ?activeTab == "details": PackageDetailsSections, ?activeTab == "submissions": TenderSubmissionsSection, ?activeTab == "documents": PackageDocumentsSection, ?activeTab == "emails": RecordCorrespondencePanel ) } }, SubcontractorInvitePickerModal, LocalSubcontractorFinderModal, LineCoverageModal, LinkDrawingsModal, TenderInviteComposerModal, TenderSubmissionModal, WorkOrderEmailModal, DeletePackageModal, PackageDetailsEditorModal, ValuationLinePickerModal }` |
| `ProjectBidPackageInvites` | 0 | `Page{ ProjectPageShell{ [ ⚠h2→SectionHeader ExportToExcelButton ], ?error is not null: Notice, RecordsTable{ (n) Pill } }, Modal{ (2) FormField }, Modal{ ?suggestError is not null: Notice, LoadGate{ ( ?suggestions is null: FormField \| (n) Pill ) } } }` |
| `ProjectBuildingControl` | 0 | `Page{ ProjectPageShell{ ⚠h2→SectionHeader, ?actionError is { } error: Notice, LoadGate{ ( ( ?ActiveCase is null: ⚠h3→SectionHeader \| ( [ [ ⚠h3→SectionHeader Pill ] [ ⚠select→FormField ⚠button→Button ] ], ⚠dl→KeyValueList, InputFile, ⚠select→FormField, ( ?CaseFiles(ActiveCase).Count == 0: EmptyState \| (n) ⚠button→Button ) ) ), ?caseFormOpen: ( ⚠h3→SectionHeader, [ (2) FormField ], (2) [ (3) FormField ], FormField, ⚠button→Button ), ?ActiveCase is not null: ( ⚠h3→SectionHeader, ?addStageOpen: [ (2) FormField ], ( ?Inspections.Count == 0: EmptyState \| RecordsTable{ (n) ( Pill, ?inspection.Status == BuildingCo…: ⚠button→Button ) } ) ) ) } } }` |
| `ProjectBuildingControlInspection` | 0 | `Page{ ProjectPageShell{ LoadGate{ ( [ ⚠h2→SectionHeader ⚠select→FormField ], ?actionError is { } error: Notice, [ (3) FormField ], (2) FormField, [ ⚠h3→SectionHeader InputFile ], ( ?Photos.Count == 0: EmptyState \| (n) ⚠button→Button ), [ ⚠h3→SectionHeader InputFile ], (n) ⚠button→Button, [ ⚠h3→SectionHeader [ ?emails is not null && Emails.Co…: Pill EmailFinder ] ], LoadGate{ ?sentNote is not null: ⚠button→Button, ?replyingTo is { } anchor: MailReplyComposer, ( (n) ?email.HasAttachments: ⚠button→Button, CorrespondenceThreadList ) } ) } } }` |
| `ProjectCalendar` | 0 | `Page{ ProjectPageShell{ SectionHeader, ?actionError is { } error: Notice, LoadGate{ ( (n) (n) ( (n) ⚠button→Button, ( ?!showAll && dayEvents.Count > 3: ⚠button→Button \| ?showAll && dayEvents.Count > 3: ⚠button→Button ) ), ⚠h3→SectionHeader, (n) (n) ⚠button→Button ) } } }, Modal{ ?modalError is { } problem: Notice, FormField, (2) [ (2) FormField ], FormField, ?editingId is not null: ⚠button→Button }` |
| `ProjectCashflow` | 0 | `Page{ ProjectPageShell{ ⚠h2→SectionHeader, LoadGate{ ⚠button→Button }, ⚠h3→SectionHeader, UnpaidXeroInvoicesModal } }` |
| `ProjectCommunications` | 0 | `Page{ ProjectPageShell{ [ ⚠button→Button (n) ⚠button→Button ], [ ( SearchInput, ?!string.IsNullOrEmpty(searchTex…: ⚠button→Button ) ⚠select→FormField ], ?loadError is not null: Notice, LoadGate{ ( ?replySent is not null: ⚠button→Button, ?replyTo is { } replyingTo: MailReplyComposer, (n) ( ?BucketLabel(item.Message.Bucket…: Pill, ?item.Message.HasAttachments: Pill, (n) Pill, [ (2) ⚠button→Button ?CanTag: ⚠button→Button ], ?CanTag && IsTagPickerOpen(item.…: ( ?tagError is not null: Notice, [ ⚠select→FormField ⚠select→FormField ] ) ) ) } } }` |
| `ProjectDefectDetail` | 0 | `Page{ ProjectPageShell{ LoadGate{ ( ?defect is null: PageHeader \| ( PageHeader{ Pill, ?CanEdit: ⚠select→FormField }, ?error is not null: Notice, [ DefectCommunicationsPanel ( Panel{ ⚠dl→KeyValueList }, DefectTodosPanel ) ], Modal{ ?editError is not null: Notice, FormField, FormField{ SearchSelect }, ?string.IsNullOrWhiteSpace(editS…: FormField, FormField } ) ) } } }` |
| `ProjectDefects` | 0 | `Page{ ProjectPageShell{ SectionHeader, ?actionError is { } error: Notice, ?raiseOpen: ( ⚠h3→SectionHeader, [ FormField FormField{ SearchSelect } ], ?string.IsNullOrWhiteSpace(newSu…: FormField, FormField ), ( ?!Defects.LoadedFor(ProjectId) &…: LoadGate \| ?Rows.Count == 0: EmptyState \| RecordsTable ), ⚠select→FormField } }` |
| `ProjectDetail` | 0 | — |
| `ProjectDetailView` | 0 | `[ ⚠h1→PageHeader ProjectStageBadge ], [ (3) StatTile ], ⚠h2→SectionHeader` |
| `ProjectDrawingDetail` | 0 | `Page{ ProjectPageShell{ LoadGate{ ( ?drawing is null: ?DrawingStore.DrawingsFailedFor(…: Notice \| ( ( DrawingDetailsEditor, [ ActionIcon ?CanManage: ( ⚠select→FormField, ?moveConfirmation is not null: Pill ) ], ?moveError is not null: Notice ), ?deleteError is not null: Notice, ?extractError is not null: Notice, Modal, ?isUploading: DrawingRevisionUploadForm, LoadGate{ ( ?!RevisionsReady && RevisionsFai…: Notice \| ( [ ( ?IsPreviewingOlderRevision: Notice, ( ?PreviewRevision is null: EmptyState \| ?IsPdf(PreviewRevision): PdfViewer \| ?IsImage(PreviewRevision): ImageViewer ) ) DrawingRevisionList ], DrawingExtractionPanel ) ) } ) ) } } }` |
| `ProjectDrawings` | 0 | `Page{ ProjectPageShell{ [ ( ⚠h2→SectionHeader, ?Ambiguous.Count > 0: Pill ) [ ExportToExcelButton ⚠button→Button ] ], ?isUploading: DrawingUploadForm, DrawingsTable, Modal{ FormField, ?folderError is not null: Notice }, Modal{ ?folderError is not null: Notice }, Modal{ ?extractAllError is not null: Notice } } }` |
| `ProjectDrawingsAmbiguous` | 0 | `Page{ ProjectPageShell{ ⚠h2→SectionHeader, DrawingRevisionList } }` |
| `ProjectFinancials` | 0 | `Page{ ProjectPageShell{ ⚠h2→SectionHeader, ( ?CostCenters.Current.Count == 0: EmptyState \| ( ?Summary.LastRefreshFailed(Proje…: Notice{ ⚠button→Button }, ?actionError is not null: Notice, ?PendingLabourTotal > 0m: Notice, FinancialsTable, PackageReconciliationSection, CostCentreSalesLinesModal, CostCentreWorkOrdersModal, CostCentreReconciliationModal, CostCentreCostOfSalesModal, Modal{ ⚠input→FormField } ) ) } }` |
| `ProjectFinancialsSetup` | 0 | — |
| `ProjectHs` | 0 | `Page{ ProjectPageShell{ SectionHeader, TabRow, ?actionError is { } error: Notice, ( ?Pane == AuditsPane: RecordsTable{ (n) ( ?audit.Rating is { } rating: Pill, Pill ) } \| RecordsTable{ (n) ( Pill, ⚠select→FormField ) } ), Modal{ [ (4) FormField ] }, Modal{ [ (4) FormField ], FormField } } }` |
| `ProjectHsAudit` | 0 | `Page{ ProjectPageShell{ LoadGate{ PageHeader{ Pill, ( ?LiveScore is { } score: Pill \| Pill ), Toolbar{ ToolbarButton } }, ?actionError is { } error: Notice, ?HasUnsavedChanges: Notice, Panel{ [ (8) FormField ], [ (2) FormField ] }, Notice, (n) HsAuditSectionPanel, ConfirmDialog, Modal{ FormField } } } }` |
| `ProjectInventory` | 0 | `Page{ ProjectPageShell{ SectionHeader, ?actionError is { } error: Notice, ?formOpen: ( ⚠h3→SectionHeader, [ (2) FormField ], (2) FormField ), ( ?!Inventory.LoadedFor(ProjectId)…: LoadGate \| ?Rows.Count == 0: EmptyState \| RecordsTable{ (n) ( (2) ⚠button→Button, ?isExpanded: RecordCorrespondenceSection ) } ) } }` |
| `ProjectLabour` | 0 | `Page{ ProjectPageShell{ ?actionError is not null: Notice, ?approvalFailures.Count > 0: ApprovalFailuresBanner, SectionHeader{ Toolbar{ ExportToExcelButton } }, LoadGate{ Panel{ ( FormField, ( ⚠table→RecordsTable, TimesheetApprovalFooter ) ) }, [ Panel{ ⚠select→FormField } SiteRegisterPanel ], Panel{ SettlementSummaryTable, ( ?ledgerLines is null: LoadGate \| ?ProjectLedgerLines().Count == 0: EmptyState \| CoverInvoiceLinesTable ) } } }, Modal{ (2) FormField, FormField{ SearchSelect }, FormField, ?manualError is not null: Notice }, Modal{ ⚠input→FormField, ?rejectError is not null: Notice }, Modal{ ⚠input→FormField, ?correctionError is not null: Notice }, Modal{ FormField{ SearchSelect }, ⚠input→FormField, ?moveBudgetBlock is not null: Notice{ Checkbox }, ?correctionError is not null: Notice }, Modal{ Notice, ⚠input→FormField, ?overBudgetError is not null: Notice } }` |
| `ProjectOperationsSetup` | 0 | — |
| `ProjectProgramme` | 0 | `Page{ ProjectPageShell{ ⚠h2→SectionHeader, [ (4) ⚠button→Button ], ( ?view == SubView.Programme: ProgrammeWorkbench \| ?view == SubView.Claims: ProgrammeClaimsWorkbench \| ?view == SubView.CriticalRfis: CriticalRfiList \| RelevantEventsList ) } }` |
| `ProjectProgress` | 0 | `Page{ ProjectPageShell{ LoadGate{ ⚠h2→SectionHeader, ?isBuildingReport: ProgressReportForm, (n) ?CanContribute: ⚠button→Button, ⚠h2→SectionHeader, ?isRecording: ProgressUpdateForm, ( ?Updates.Count == 0: EmptyState \| (n) ( ?CanContribute: ⚠button→Button, [ (n) ?CanContribute: ⚠button→Button ?CanContribute: InputFile ] ) ) }, ?actionError is not null: Notice } }` |
| `ProjectProgressContractorsReport` | 0 | `Page{ ProjectPageShell{ SectionHeader, ?error is { } message: Notice, ?saved: Notice, LoadGate{ ?view is { } report && draft is …: ( ContractorsReportFindings, LoadGate{ [ ( Panel{ ContractorsReportHeaderFields }, Panel{ ContractorsReportUpdatePicker }, Panel{ ContractorsReportLookAheadEditor }, Panel{ ContractorsReportNarrativeFields }, Panel{ ContractorsReportAttendanceTable } ) Panel{ ContractorsReportPreview } ] } ) }, ConfirmDialog } }` |
| `ProjectProgressContractorsReports` | 0 | `Page{ ProjectPageShell{ SectionHeader, ?error is { } message: Notice, ?isOpening: ContractorsReportOpenForm, RecordsTable } }` |
| `ProjectProgressWhatsAppWeek` | 0 | `Page{ ProjectPageShell{ SectionHeader, ?error is { } message: Notice, ( ?applied is { } written: Notice \| LoadGate{ WhatsAppWeekExportForm, ?preview is { } week: WhatsAppWeekReview } ), ConfirmDialog } }` |
| `ProjectReconciliationAudit` | 0 | `Page{ ( ?!CanAccess: PageHeader \| ProjectPageShell{ ⚠h2→SectionHeader, ?loadError is not null: Notice, LoadGate{ RecordsTable } } ) }` |
| `ProjectRequestDetail` | 0 | `Page{ ( ?!dataLoaded: ProjectPageShell{ LoadGate } \| ?record is null: ProjectPageShell \| ( ProjectPageShell{ RequestHeaderBar, RecordTabBar, RequestFactsStrip, ?actionError is not null: Notice, ?ShowCriticalPathNudge: CriticalPathNudge, [ ( ?ShowContainerPane: Panel, ?ShowOfficialContent: ( RequestOfficialFormPanel, ?!string.IsNullOrWhiteSpace(reco…: RequestResponsePanel ), ?ShowContainerPane: ( RequestAttachmentsPanel, RequestConversation, ?!HasOfficialTab: RecordAuditHistory ) ) ( ?ShowContainerPane && HasOfficia…: RecordAuditHistory, ?ShowOfficialContent: ( ?!record.Kind.IsEmailable() \|\| C…: RequestPartyPanel, ?record.Kind is RequestType.Rfi …: RequestVariationCard ) ) ] }, (2) Modal, Modal{ FormField, ?closeError is not null: Notice }, EmailDraftStagingModal, Modal{ ⚠textarea→FormField, ?actionError is not null: Notice }, RequestHeaderEditModal, RequestFactsEditModal, RequestDetailEditModal, VariationDraftModal ) ) }` |
| `ProjectRequests` | 0 | `Page{ ProjectPageShell{ [ ⚠h2→SectionHeader ExportToExcelButton ], Notice, [ (n) ⚠button→Button ( SearchInput, ?Searching: ⚠button→Button ) ], ?Searching: ?HiddenByStatusCount > 0: ⚠button→Button, ?mergeError is not null: Notice, ?draftBatchError is not null: Notice, ( ?statusError is not null: Notice, RequestTable ) }, RaiseRequestDialog }` |
| `ProjectSettings` | 0 | `Page{ ProjectPageShell{ ?Project is not null: ( (n) ⚠button→Button, ?activePane: ( ProjectDetailsEditor, [ (8) StatTile ], ProjectContractPanel, NextValuationDateEditor, ProjectRetentionPanel, ProjectCorrespondencePanel ) ) } }` |
| `ProjectSetup` | 0 | — |
| `ProjectSiteInstructions` | 0 | `Page{ ProjectPageShell{ SectionHeader, ?actionError is { } error: Notice, ?formOpen: ( ⚠h3→SectionHeader, [ (2) FormField ], FormField ), ( ?!SiteInstructions.LoadedFor(Pro…: LoadGate \| ?Rows.Count == 0: EmptyState \| RecordsTable{ (n) ( (2) ⚠button→Button, ?isExpanded: RecordCorrespondenceSection ) } ) } }` |
| `ProjectTodos` | 0 | `Page{ ProjectPageShell{ ProjectTodoList } }` |
| `ProjectUsefulInformation` | 0 | `Page{ ProjectPageShell{ UsefulInformationPanel } }` |
| `ProjectValuation` | 0 | `Page{ ProjectPageShell{ SectionHeader{ Toolbar{ ToolbarButton, ExportToExcelButton, ?CanMapClientReferences: ( ToolbarDivider, ToolbarButton ) }, ( ?!ClaimReady: ⚠select→FormField \| ?Claims.Count > 0: ⚠select→FormField ) }, ?actionError is not null: Notice{ ⚠button→Button }, ?xeroRaiseNote is not null: Notice, ValuationInvoiceXeroRaiseModal, ?programmeDraftOpened is { } pro…: Notice, LoadGate{ ClaimProgressDialog, ?Selected is { } s: [ Pill [ ?s.IsLocked: Toolbar{ (2) ToolbarButton, ?CanEmailStatement: ToolbarButton } ?CanManageClaims: DropdownMenu ] ], ValuationReportTable{ ValuationInvoicesSection, ValuationClaimCorrespondenceSection } }, Modal{ ValuationLineForm }, ?showClientReferences: ClientCostReferencesModal, Modal{ ?viewingStatementClaimId is not …: ValuationStatementViewer }, ValuationStatementEmailModal, Modal{ (2) FormField }, Modal{ FormField }, (2) Modal } }` |
| `ProjectVariationDetail` | 0 | `Page{ ( ?!orderLoaded: ProjectPageShell{ LoadGate } \| ?order is null: ProjectPageShell \| ProjectPageShell{ VariationHeaderBar, RecordTabBar, ?error is not null: Notice, ?string.IsNullOrWhiteSpace(order…: Notice, [ ( VariationDocumentPanel, ?ApprovedOrder is not null && Va…: VariationLinesTable, VariationConversation, RecordCorrespondencePanel ) ( VariationDetailsCard, ( ?ApprovedOrder is not null: ( ApprovedFiguresPanel, Modal{ VariationApprovePanel } ) \| ?CanManage && order.Status.IsPre…: ( StagedBuildUpPanel, VariationApproveOffer, RecordAgreedTenderPanel ) ) ) ], Modal{ ?renameError is not null: Notice, FormField }, ?CanManage && string.IsNullOrWhi…: OriginatingRequestRepair, ?CanManage && ApprovedOrder is n…: DeleteVariationPanel, DeclineVariationModal, VariationOrderEmailModal } ) }` |
| `ProjectVariations` | 0 | `Page{ ProjectPageShell{ [ ⚠h2→SectionHeader ExportToExcelButton ], Notice, [ FilterChips ( SearchInput, ?Searching: ⚠button→Button ) ], ?Searching: ?HiddenByStatusCount > 0: ⚠button→Button, ?variationsError is not null: Notice, ?OpenRequests.Count > 0 \|\| Revie…: ( ⚠h3→SectionHeader, ?requestError is not null: Notice, (n) ?rejectingRequestId == variation…: [ ⚠input→FormField ⚠button→Button ] ), ?requestError is not null && Ope…: Notice, ( ?FilteredRows.Count == 0: ?FilteringByStatus: ⚠button→Button \| ( ?variationStatusError is not null: Notice, RecordsTable{ (n) ( ActivityBadge, ( ?statusChoices.Count > 0: DropdownMenu \| Pill ), ?CanIssueWorkOrder(order): ⚠button→Button ) } ) ) }, AddManualVariationDialog, ?decliningVariation is { } decli…: Modal{ ?variationStatusError is not null: Notice } }` |
| `ProjectWorkOrderAllocation` | 0 | `Page{ ProjectPageShell{ ⚠h2→SectionHeader, ExportToExcelButton, [ ⚠h3→SectionHeader SearchInput ], RecordsTable{ (n) ?expandedOrderIds.Contains(summa…: (n) ⚠button→Button }, [ ⚠h3→SectionHeader (n) ⚠button→Button ], RecordsTable{ (n) ( ?IsAmountSplit(line): ⚠button→Button \| [ ⚠select→FormField ⚠button→Button ] ) }, WorkOrderLinkSplitModal } }` |
| `ProjectWorkOrders` | 0 | `Page{ ProjectPageShell{ SectionHeader{ SearchInput, ExportToExcelButton }, ?poEmailNote is not null: Notice{ ⚠button→Button }, LoadGate{ ( ?Orders.Count == 0: EmptyState \| ( ?DraftOrders.Count > 0: DraftWorkOrdersPanel, ?RejectedOrders.Count > 0: RejectedWorkOrdersList, ?CancelledOrders.Count > 0: CancelledWorkOrdersList, WorkOrdersTable, ?OrdersWithoutLines.Count > 0: UnpricedWorkOrdersList, WorkOrderLineRecodeModal ) ) }, ManualWorkOrderModal, DeleteWorkOrderModal, SupplierAccountModal } }` |
| `Projects` | 0 | `Page{ PageHeader, ExportToExcelButton, ProjectsTable, Modal{ NewProjectForm } }` |
| `QueueEmailReadingPane` | 0 | `TriageMessageDetail{ ?Selected.Bucket is not null \|\| …: [ ?TriagePathways.FromBucket(Selec…: Pill (n) Pill ] }` |
| `QueueInboxList` | 0 | `?UnassignedArrived && Unassigned…: UnassignedRequestsPanel, LoadGate{ LoadGate{ [ (2) ⚠button→Button ], (n) TriageEmailRow, EmailListPager } }` |
| `QueueLineRow` | 0 | `LedgerLineIdentityCell, (2) SearchSelect, ( ?!string.IsNullOrEmpty(ArmedBuck…: ⚠button→Button \| ⚠button→Button ), DropdownMenu` |
| `RateLibrary` | 0 | `Page{ WorkspaceSectionNav, PageHeader{ ExportToExcelButton }, RateTable }` |
| `Registers` | 0 | `Page{ SectionHeader{ Toolbar{ ToolbarButton, ExportToExcelButton } }, ?actionError is not null: Notice, (n) ⚠button→Button, Panel{ ⚠table→RecordsTable }, Modal{ [ (3) ⚠input→FormField (2) FormField (2) ⚠input→FormField FormField ⚠input→FormField ?editError is not null: Notice ] } }` |
| `ReplyComposerForm` | 0 | `⚠button→Button, FormField{ RecipientInput }, ?!ShowBcc: ⚠button→Button, RecipientInput, ?ShowBcc: FormField{ RecipientInput }, FormField, FormField{ RichTextEditor }, AttachmentPicker` |
| `RfiDashboard` | 0 | `Page{ PageHeader, [ (n) ⚠button→Button ExportToExcelButton ], ?loadError is not null: Notice, ( ?FilteredRecords.Count == 0: EmptyState \| RecordsTable{ (n) ( ?record.ImpliesVariation: Pill, Pill ) } ) }` |
| `SalesEstimateDetail` | 0 | `Page{ ( ?!loadDone: LoadGate \| ?Lead is null \|\| Estimate is null: WorkspaceSectionNav \| ( WorkspaceSectionNav, PageHeader{ EstimateStatusPill, DropdownMenu }, ?ShowingStale: Notice, ?actionNote is not null: Notice, ?actionError is not null: Notice, [ EstimateBreakdownEditor ( EstimateDetailsPanel, EstimateNarrativePanel ) ], EstimateFormModal, EstimateStatusMoveDialog ) ) }` |
| `SalesInbox` | 0 | `Page{ WorkspaceSectionNav, PageHeader{ SearchInput }, ( ?Inbox.Current is null: LoadGate \| ( ( ?!inbox.Configured: Notice \| ?inbox.Notice is not null: Notice ), ?actionNote is not null: Notice, [ SalesInboxMessageList SalesInboxThread ], LeadFormModal ) ) }` |
| `SalesLeadDetail` | 0 | `Page{ ( ?!Detail.LoadedFor(LeadId) && !d…: LoadGate \| ?Lead is null: WorkspaceSectionNav \| ( WorkspaceSectionNav, LeadPageHeader, ?actionNote is not null: Notice, [ ( LeadDetailsPanel, ?!string.IsNullOrWhiteSpace(lead…: Panel, LeadEstimatesPanel, LeadHouseModelSection, Panel{ RecordCorrespondenceSection }, LeadImaginePanel, LeadProposalsPanel, LeadTimelinePanel ) LeadStageLadder ], LeadFormModal, LeadStageMoveDialog, LeadWinDialog, LeadDeleteDialog, LeadActivityLogDialog ) ) }` |
| `SalesLeads` | 0 | `Page{ WorkspaceSectionNav, PageHeader, LoadGate{ [ (4) StatTile ], Panel{ SearchInput, SearchSelect, [ ⚠button→Button (n) ⚠button→Button ], ⚠table→RecordsTable } }, LeadFormModal }` |
| `SalesStrategies` | 0 | `Page{ WorkspaceSectionNav, PageHeader, ( ?Strategies.Current is null: LoadGate \| (n) ( [ ⚠h2→SectionHeader Pill ], ?strategy.ResearchStatus.IsInPro…: JewelIcon ) ), StrategyFormModal }` |
| `SalesStrategyDetail` | 0 | `Page{ ( ?!Detail.LoadedFor(StrategyId) &…: LoadGate \| ?Strategy is null: WorkspaceSectionNav \| ( WorkspaceSectionNav, StrategyPageHeader, ?actionError is not null: Notice, StrategyResearchStatusNotice, StrategyFunnelTiles, [ ( ?!string.IsNullOrWhiteSpace(stra…: Panel{ SimpleMarkdown }, StrategyApproachPlanPanel, StrategyLeadsPanel ) ( StrategyArgumentPanel, StrategyFunnelPanel ) ], StrategyFormModal, LeadFormModal, StrategyPlanEditDialog, StrategyPlanGenerateDialog ) ) }` |
| `SetPassword` | 0 | `JewelIcon, ( ?state == PageState.Checking: LoadGate \| ?state == PageState.Invalid: ⚠h1→PageHeader \| ( ⚠h1→PageHeader, (2) ⚠input→FormField ) )` |
| `SettlementScheduleDetail` | 0 | `⚠table→RecordsTable` |
| `SettlementVerdictPill` | 0 | `?Schedule.Verdict: (3) Pill` |
| `SitePhotos` | 0 | `Page{ PageHeader{ FilterChips }, ?CanContribute: SitePhotoDropZone, ?error is { } message: Notice, ?lastUpload is { } upload: Notice, LoadGate{ ( ?dataFailed: Notice \| ?Filtered.Count == 0: EmptyState \| (n) SitePhotoCard ) }, Modal{ ?viewing is { } shown: ImageViewer } }` |
| `SiteVisitsDetail` | 0 | `( ?Visits.Count == 0: EmptyState \| ⚠table→RecordsTable )` |
| `StageForwardToQsAction` | 0 | `?QsRecipients.Count == 0: Notice` |
| `StaleRates` | 0 | `Page{ PageHeader, ExportToExcelButton, ( ?Stale.Count == 0: EmptyState \| RateTable ) }` |
| `SubcontractorCommunications` | 0 | `Page{ PageHeader, [ ⚠button→Button (n) ⚠button→Button ], ?loadError is not null: Notice, LoadGate{ ( ?replySent is not null: ⚠button→Button, ?replyTo is { } replyingTo: MailReplyComposer, CorrespondenceThreadList ) } }` |
| `SubcontractorDetail` | 0 | `Page{ ( ?!CanAccess: PageHeader \| ?!SubcontractorStore.IsLoaded: LoadGate \| ?subcontractor is null: PageHeader \| ( PageHeader{ ?CanManageXeroLink: (n) InlineConfirm }, XeroLinkModal, XeroContactPushModal, ?xeroError is not null: Notice, ?xeroNote is not null: Notice, ⚠h2→SectionHeader, ⚠dl→KeyValueList, ⚠h2→SectionHeader, ?tradesError is not null: Notice, (n) Pill{ ⚠button→Button }, [ ⚠select→FormField ⚠input→FormField ], ⚠h2→SectionHeader, ?contactsError is not null: Notice, ( ?!SubcontractorStore.ContactsLoa…: LoadGate \| ⚠table→RecordsTable ), [ (3) FormField ], ⚠h2→SectionHeader, ?inviteError is not null: Notice, ?inviteResult is not null: Notice, ⚠h2→SectionHeader, CisVerificationPanel, SubcontractorComplianceList, SubcontractorStatementModal, Modal{ ?editError is not null: Notice, FormField, (2) [ (2) FormField ], FormField, [ (3) FormField ] } ) ) }` |
| `SubcontractorTable` | 0 | `RecordsTable{ (n) ( (n) Pill, ComplianceStatusPill ) }` |
| `Subcontractors` | 0 | `Page{ ( ?!CanAccess: PageHeader \| ( PageHeader{ ?group == DirectoryGroup.Subcont…: ExportToExcelButton }, (n) ⚠button→Button, ( ?group == DirectoryGroup.Clients: ClientsDirectoryTable \| ?group == DirectoryGroup.Archite…: ArchitectsDirectoryTable \| ?group == DirectoryGroup.Staff: StaffDirectoryTable \| ( TabRow, [ (2) FormField (2) FilterChips ], CompaniesDirectoryTable ) ), Modal{ DirectoryContactForm }, XeroImportModal, ConsolidateRecordsModal ) ) }` |
| `SummaryRow` | 0 | — |
| `TaggedEmailManagePanel` | 0 | `?ActionError is not null: Notice, TriageMessageDetail, ?Pathway is { } taggedPathway: Pill, ( ?Selected.Categories.Count == 0: EmptyState \| (n) Pill{ ⚠button→Button } ), ⚠select→FormField, ?!IsCompanyWide(LinkRecordType): ⚠select→FormField, ?!string.IsNullOrWhiteSpace(Proj…: ⚠select→FormField` |
| `TaggedInboxBrowser` | 0 | `SearchInput, ?SearchText.Trim().Length > 0: ⚠button→Button, [ (6) ⚠button→Button ], ?Arrived: [ DropdownMenu{ ⚠button→Button } (n) Pill{ ⚠button→Button } ], LoadGate{ LoadGate{ (n) TriageEmailRow, ?SearchResults is null: EmailListPager } }` |
| `TimesheetRow` | 0 | `( ?IsEditing: SearchSelect \| ?Timesheet.CostCode == "": Pill ), ?IsEditing: ⚠input→FormField, StatusPill` |
| `TodoAssigneeBadge` | 0 | `?ShowUnassigned: Pill` |
| `TodoAssigneeFact` | 0 | `?editing: TodoFactEditor` |
| `TodoDetail` | 0 | `Page{ ( ?!HasInternalRole: PageHeader \| LoadGate{ ( ?item is null: PageHeader \| ( PageHeader{ Pill, ?CanManage: InlineConfirm }, ?error is not null: Notice, [ TodoCommunicationsPanel ( TodoFactsPanel, TodoActivityPanel, LinkedTodosPanel ) ] ) ) } ) }` |
| `TodoEmailCard` | 0 | `?Email.HasAttachments: Pill, ?OtherTags.Count > 0: (n) Pill, [ ⚠button→Button ?CanSend: (2) ⚠button→Button ]` |
| `TodoScopeFact` | 0 | `?editing: TodoFactEditor` |
| `Todos` | 0 | `Page{ ( ?!HasInternalRole: PageHeader \| ( PageHeader, ?error is not null: Notice, [ ( SearchInput, ?HasQuery: ⚠button→Button ) [ (2) ⚠button→Button ] ?!boardView: (3) ⚠button→Button (2) ⚠select→FormField ?CanSeeAll: ⚠select→FormField ], LoadGate{ ( ⚠h2→SectionHeader, ( ?boardView: TodoBoard \| (n) ⚠button→Button ), ?HasQuery: TaggedEmailSearch ) } ) ) }, Modal{ ?addError is not null: Notice, FormField{ SearchSelect }, FormField, [ FormField{ TodoAssigneeSelect } FormField ], FormField }` |
| `TodosModal` | 0 | `Modal{ ( ?editIndex is { } activeIndex &&…: ( ⚠button→Button, FormField, [ ( ?row.Assignees.Count > 0: (n) ⚠button→Button, SearchSelect ) FormField ], FormField, ⚠button→Button ) \| ( ?TitledRows.Count == 0: EmptyState \| ⚠button→Button ) ) }` |
| `TriageNoticesStack` | 0 | `?ComposeOutcome is { } outcome: Notice, ?LinkNote is not null: Notice, ?PoEmailNote is not null: Notice, ?OutboxNote is not null: Notice` |
| `TriageQueue` | 0 | `Page{ ( LoadGate{ ( ?view == QueueView.Active && sel…: TriageBar \| ?view == QueueView.Active && que…: OutboxOnlyBar ), PanelWorkspace{ EmailMirrorPane, RecordExplorerPane, PreviewPane, XeroExplorerPane, NewEmailComposerPane, OutboxPane } }, ?RecentTriage.Count > 0: RecentTriageFold ) }` |
| `ValuationDueBadge` | 0 | `?Project.NextExpectedValuationDa…: ( ?status == ValuationDue.Status.O…: Pill \| ?status == ValuationDue.Status.D…: Pill )` |
| `ValuationSummary` | 0 | — |
| `WeeklyCashflow` | 0 | `Page{ PageHeader{ Toolbar{ ExportToExcelButton, ToolbarDivider, (2) ToolbarButton } }, LoadGate{ WeeklyKpiStrip, ?moveError is not null: Notice{ ⚠button→Button }, WeeklyCashflowGrid }, CashflowItemModal, SupplierGroupsModal }` |
| `WorkOrderGroupRow` | 0 | `?GroupBySupplier: ?OnOpenSupplierAccount.HasDelega…: ⚠button→Button, ?Expanded: ⚠table→RecordsTable` |
| `WorkOrderLineRow` | 0 | `Pill, ?Summary is { } summary: (2) Pill, ( ?CancelPending: (2) ⚠button→Button \| DropdownMenu )` |
| `WorkOrderPo` | 0 | `Page{ ( ?Detail is null: PageHeader \| ( [ ?Detail.Order.IsCancelled: Pill ?Detail.Order.IsAccepted: Pill ?Detail.Order.Status == WorkOrde…: Pill ?Detail.Order.IsDraft: Pill ?Detail.Order.IsRejected: Pill ], ?emailNote is not null: Notice, ?emailError is not null: Notice, PurchaseOrderSheet, WorkOrderAttachmentsPanel, Panel, RecordAuditHistory, Modal{ Notice, FormField, FormField{ RichTextEditor } } ) ) }` |
| `WorkerDetailPanel` | 0 | `[ ⚠table→RecordsTable (2) FormField ]` |
| `WorkerPlacementStrip` | 0 | — |
| `Workers` | 0 | `Page{ ⚠h2→SectionHeader, ?actionError is not null: Notice, LoadGate{ ( ?workers.Count == 0: EmptyState \| ( ExportToExcelButton, ⚠table→RecordsTable ) ) }, ?RegistryReady && UnlinkedWorker…: ( ⚠h3→SectionHeader, ?matchError is not null: Notice, ?linkReport is not null: ⚠table→RecordsTable ), Modal{ ?formError is not null: Notice, [ (4) ⚠input→FormField SearchSelect (2) ⚠input→FormField ] } }` |
| `WorkspaceIcon` | 0 | — |
| `XeroAllocation` | 0 | `Page{ WorkspaceSectionNav, AllocationPageHeader, ( ?!tabRestored \|\| Ledger.Counts()…: LoadGate \| ( ?activeTab == XeroAllocationStat…: MatchedLinesBanner, LoadGate{ [ AllocationTabBar [ ExportToExcelButton ?activeTab == XeroAllocationStat…: SearchSelect ⚠input→FormField ] ], ?AllocatedXeroChips is { } xeroC…: FilterChips, ?activeTab == XeroAllocationStat…: LabourSectionStrip, ?activeTab == XeroAllocationStat…: WorkOrderBillsStrip, ?activeTab == XeroAllocationStat…: LabourBulkActions, ?activeTab == XeroAllocationStat…: QueueBulkActions, ?activeTab == XeroAllocationStat…: AllocatedBulkActions, LoadGate{ ?activeTab == XeroAllocationStat…: BucketChipStrip, ?activeTab == XeroAllocationStat…: WorkOrderBillCard } } ) \| ⚠table→RecordsTable ), ?PageCount > 1: [ (2) ⚠button→Button ], Modal{ ?splitLine is not null: ( LedgerLineSummary, SplitEditorForm ) }, (2) SendLinesModal, DisputeLineModal, ConfirmDialog, DisputeDiscussionModal, Modal{ ?viewLine is not null: ( LedgerLineSummary, InvoiceBillLines, ?viewLine.AllocationStatus == Xe…: InvoiceViewerActions, InvoiceDocumentPreview ) } }` |
| `XeroTransactions` | 0 | `Page{ WorkspaceSectionNav, PageHeader{ Toolbar{ ExportToExcelButton, ToolbarDivider, ToolbarButton } }, ( ?Snapshot is null: LoadGate \| ( [ (2) ⚠button→Button ], ( ?activeView == View.Transactions: ( [ ⚠input→FormField (n) ⚠button→Button ], RecordsTable ) \| RecordsTable ) ) ) }` |
| `_Imports` | 0 | — |
