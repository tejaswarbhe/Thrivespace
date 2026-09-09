using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace StartupIMS.Infrastructure.Persistence.Scaffolded.Core;

[Table("progressreports")]
[Index("CreatedByMentorId", Name = "FK_ProgressReports_Mentors")]
[Index("StartupId", Name = "IX_ProgressReports_StartupId")]
public partial class Progressreport
{
    [Key]
    public int Id { get; set; }

    public int StartupId { get; set; }

    public int? CreatedByMentorId { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime SubmissionDate { get; set; }

    [Column(TypeName = "text")]
    public string Milestones { get; set; } = null!;

    public bool IsCompleted { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? CompletedDate { get; set; }

    [Column(TypeName = "text")]
    public string? Remarks { get; set; }

    [ForeignKey("CreatedByMentorId")]
    [InverseProperty("Progressreports")]
    public virtual Mentor? CreatedByMentor { get; set; }

    [ForeignKey("StartupId")]
    [InverseProperty("Progressreports")]
    public virtual Startup Startup { get; set; } = null!;
}
