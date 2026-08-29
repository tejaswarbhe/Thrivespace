using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace StartupIMS.Infrastructure.Persistence.Scaffolded.Core;

[Table("fundingrequests")]
[Index("StartupId", Name = "IX_FundingRequests_StartupId")]
public partial class Fundingrequest
{
    [Key]
    public int Id { get; set; }

    public int StartupId { get; set; }

    [Precision(18, 2)]
    public decimal Amount { get; set; }

    [StringLength(50)]
    public string FundingType { get; set; } = null!;

    [StringLength(20)]
    public string ApprovalStatus { get; set; } = null!;

    [StringLength(20)]
    public string? PaymentStatus { get; set; }

    public int? ApprovedByUserId { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? ApprovedDate { get; set; }

    [StringLength(200)]
    public string? TransactionReference { get; set; }

    [StringLength(500)]
    public string? PaymentUrl { get; set; }

    [ForeignKey("StartupId")]
    [InverseProperty("Fundingrequests")]
    public virtual Startup Startup { get; set; } = null!;
}
