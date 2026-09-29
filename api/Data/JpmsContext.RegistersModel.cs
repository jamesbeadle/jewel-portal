using Jewel.JPMS.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Data;

public sealed partial class JpmsContext
{
    /// <summary>The company registers, the policies and the site manual: one code per module, one row per approved version, one acknowledgement per person per version.</summary>
    private static void ConfigureRegistersAndManual(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CompanyRegisterItemEntity>()
            .HasIndex(row => row.Kind)
            .HasDatabaseName("IX_CompanyRegisterItems_Kind");
        modelBuilder.Entity<PolicySignOffEntity>()
            .HasIndex(row => new { row.PolicyDocumentId, row.RecipientEmail })
            .IsUnique()
            .HasDatabaseName("IX_PolicySignOffs_PolicyDocumentId_RecipientEmail");
        modelBuilder.Entity<ManualModuleEntity>()
            .HasIndex(row => row.Code)
            .IsUnique()
            .HasDatabaseName("IX_ManualModules_Code");
        modelBuilder.Entity<ManualModuleVersionEntity>()
            .HasIndex(row => new { row.ManualModuleId, row.Version })
            .IsUnique()
            .HasDatabaseName("IX_ManualModuleVersions_ManualModuleId_Version");
        modelBuilder.Entity<ManualAcknowledgementEntity>()
            .HasIndex(row => new { row.ManualModuleId, row.Version, row.Email })
            .IsUnique()
            .HasDatabaseName("IX_ManualAcknowledgements_ManualModuleId_Version_Email");
    }
}
