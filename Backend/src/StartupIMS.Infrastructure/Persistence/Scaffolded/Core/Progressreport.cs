using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace StartupIMS.Infrastructure.Persistence.Scaffolded.Core;

[Table("progressreports")]
[Index("StartupId", Name = "IX_ProgressReports_StartupId")]
public partial class Progressreport
{
    [Key]
    public int Id { get; set; }

    public int StartupId { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime SubmissionDate { get; set; }

    [Column(TypeName = "text")]
    public string Milestones { get; set; } = null!;

    [Column(TypeName = "text")]
    public string? Remarks { get; set; }

    [ForeignKey("StartupId")]
    [InverseProperty("Progressreports")]
    public virtual Startup Startup { get; set; } = null!;
}
