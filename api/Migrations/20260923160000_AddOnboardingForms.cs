using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <summary>
    /// The forms new starters and sub-contractors fill in, moved from the JPS Dashboard into the
    /// portal for both Jewel companies (2026-09-23): the one-time links and new starter packs
    /// (FormInvites, FormPacks — only a token's hash is stored), the forms sent (FormSubmissions),
    /// their files (FormUploads — the bytes live in the three forms containers, never here), the
    /// person or company everything is filed under (FormFolders), and the three registers the forms
    /// feed: RightToWorkChecks, TrainingRecords and WorkstationActions, with DrivingLicenceChecks for
    /// the vehicle form's outcome. ComplianceDocuments gains the insurance chase's LastChasedAt and
    /// ChaseCount, and FormCompany — the Jewel company whose form a certificate came in on, the only
    /// certificates the chase asks for. Additive only; no FKs, as everywhere else. Scoped script:
    /// api/Migrations/add-onboarding-forms.sql.
    /// </summary>
    [DbContext(typeof(JpmsContext))]
    [Migration("20260923160000_AddOnboardingForms")]
    public partial class AddOnboardingForms : Migration
    {
        private static readonly string[] Tables =
        {
            "FormPacks", "FormInvites", "FormSubmissions", "FormUploads", "FormFolders",
            "RightToWorkChecks", "TrainingRecords", "WorkstationActions", "DrivingLicenceChecks"
        };

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FormPacks",
                columns: table => new
                {
                    FormPackId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Company = table.Column<int>(type: "int", nullable: false),
                    PersonName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    EngagedAs = table.Column<int>(type: "int", nullable: false),
                    HasP45 = table.Column<bool>(type: "bit", nullable: false),
                    IsWorkingAtAScreen = table.Column<bool>(type: "bit", nullable: false),
                    IsGettingAVehicle = table.Column<bool>(type: "bit", nullable: false),
                    MustHoldATicket = table.Column<bool>(type: "bit", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    SentByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SentByName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SentAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    OpenedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastChasedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ChaseCount = table.Column<int>(type: "int", nullable: false),
                    CancelledAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_FormPacks", x => x.FormPackId));

            migrationBuilder.CreateIndex(
                name: "IX_FormPacks_TokenHash",
                table: "FormPacks",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateTable(
                name: "FormInvites",
                columns: table => new
                {
                    FormInviteId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    FormPackId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    FormSlug = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Company = table.Column<int>(type: "int", nullable: false),
                    PersonName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CompanyName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    SentByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SentByName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SentAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    OpenedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UsedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FormSubmissionId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CancelledAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_FormInvites", x => x.FormInviteId));

            migrationBuilder.CreateIndex(
                name: "IX_FormInvites_TokenHash",
                table: "FormInvites",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FormInvites_FormPackId",
                table: "FormInvites",
                column: "FormPackId");

            migrationBuilder.CreateTable(
                name: "FormSubmissions",
                columns: table => new
                {
                    FormSubmissionId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    FormSlug = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Company = table.Column<int>(type: "int", nullable: false),
                    SessionId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    FormInviteId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    FormPackId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    FormFolderId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    SubmitterName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    FilingName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    IsVerifiedLink = table.Column<bool>(type: "bit", nullable: false),
                    SentToEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SentByName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    AnswersJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    HandledByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    HandledAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RedactedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DestroyedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ClientHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_FormSubmissions", x => x.FormSubmissionId));

            migrationBuilder.CreateIndex(
                name: "IX_FormSubmissions_SessionId",
                table: "FormSubmissions",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_FormSubmissions_SubmittedAt",
                table: "FormSubmissions",
                column: "SubmittedAt");

            migrationBuilder.CreateTable(
                name: "FormUploads",
                columns: table => new
                {
                    FormUploadId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SessionId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    FormSlug = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Company = table.Column<int>(type: "int", nullable: false),
                    QuestionKey = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Store = table.Column<int>(type: "int", nullable: false),
                    BlobRef = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Size = table.Column<long>(type: "bigint", nullable: false),
                    UploadedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    FormSubmissionId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ClientHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletionReason = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_FormUploads", x => x.FormUploadId));

            migrationBuilder.CreateIndex(
                name: "IX_FormUploads_SessionId",
                table: "FormUploads",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_FormUploads_FormSubmissionId",
                table: "FormUploads",
                column: "FormSubmissionId");

            migrationBuilder.CreateTable(
                name: "FormFolders",
                columns: table => new
                {
                    FormFolderId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Company = table.Column<int>(type: "int", nullable: false),
                    EngagementEndedOn = table.Column<DateOnly>(type: "date", nullable: true),
                    VehicleReturnedOn = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSubmittedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_FormFolders", x => x.FormFolderId));

            migrationBuilder.CreateIndex(
                name: "IX_FormFolders_NormalizedName",
                table: "FormFolders",
                column: "NormalizedName");

            migrationBuilder.CreateTable(
                name: "RightToWorkChecks",
                columns: table => new
                {
                    RightToWorkCheckId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    FormSubmissionId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    PersonName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Company = table.Column<int>(type: "int", nullable: false),
                    JobRole = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    EngagedAs = table.Column<int>(type: "int", nullable: false),
                    EngagedSince = table.Column<DateOnly>(type: "date", nullable: true),
                    Route = table.Column<int>(type: "int", nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    IdspProvider = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    SeenVia = table.Column<int>(type: "int", nullable: false),
                    DocumentReference = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CheckedByName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CheckedOn = table.Column<DateOnly>(type: "date", nullable: false),
                    IsDocumentGenuine = table.Column<bool>(type: "bit", nullable: false),
                    IsLikenessConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    IsPermittedToDoTheWork = table.Column<bool>(type: "bit", nullable: false),
                    IsEvidenceFiled = table.Column<bool>(type: "bit", nullable: false),
                    IsTimeLimited = table.Column<bool>(type: "bit", nullable: false),
                    PermissionExpiresOn = table.Column<DateOnly>(type: "date", nullable: true),
                    FollowUpOn = table.Column<DateOnly>(type: "date", nullable: true),
                    Outcome = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    EvidenceUploadId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    RecordedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EngagementEndedOn = table.Column<DateOnly>(type: "date", nullable: true),
                    ConfirmedOn = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_RightToWorkChecks", x => x.RightToWorkCheckId));

            migrationBuilder.CreateTable(
                name: "TrainingRecords",
                columns: table => new
                {
                    TrainingRecordId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    FormSubmissionId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Company = table.Column<int>(type: "int", nullable: false),
                    PersonName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Course = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CertificateNumber = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CompletedOn = table.Column<DateOnly>(type: "date", nullable: false),
                    ExpiresOn = table.Column<DateOnly>(type: "date", nullable: true),
                    CertificateUploadId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    AcceptedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    AcceptedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastChasedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ChaseCount = table.Column<int>(type: "int", nullable: false),
                    EndedOn = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_TrainingRecords", x => x.TrainingRecordId));

            migrationBuilder.CreateTable(
                name: "WorkstationActions",
                columns: table => new
                {
                    WorkstationActionId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    FormSubmissionId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PersonName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Workstation = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    QuestionKey = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    State = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ResolvedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RaisedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_WorkstationActions", x => x.WorkstationActionId));

            migrationBuilder.CreateIndex(
                name: "IX_WorkstationActions_FormSubmissionId",
                table: "WorkstationActions",
                column: "FormSubmissionId");

            migrationBuilder.CreateTable(
                name: "DrivingLicenceChecks",
                columns: table => new
                {
                    DrivingLicenceCheckId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    FormSubmissionId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DvlaCheckedOn = table.Column<DateOnly>(type: "date", nullable: false),
                    IsWithinInsuranceCriteria = table.Column<bool>(type: "bit", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CheckedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CheckedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    PhotosDeleted = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_DrivingLicenceChecks", x => x.DrivingLicenceCheckId));

            migrationBuilder.CreateIndex(
                name: "IX_DrivingLicenceChecks_FormSubmissionId",
                table: "DrivingLicenceChecks",
                column: "FormSubmissionId",
                unique: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastChasedAt",
                table: "ComplianceDocuments",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ChaseCount",
                table: "ComplianceDocuments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "FormCompany",
                table: "ComplianceDocuments",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "FormCompany", table: "ComplianceDocuments");
            migrationBuilder.DropColumn(name: "ChaseCount", table: "ComplianceDocuments");
            migrationBuilder.DropColumn(name: "LastChasedAt", table: "ComplianceDocuments");
            foreach (var table in Tables) migrationBuilder.DropTable(name: table);
        }
    }
}
