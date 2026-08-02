using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace StartupIMS.Infrastructure.Persistence.Scaffolded.Core;

[Table("mentorassignments")]
[Index("MentorId", Name = "IX_MentorAssignments_MentorId")]
[Index("StartupId", Name = "IX_MentorAssignments_StartupId")]
public partial class Mentorassignment
{
    [Key]
    public int Id { get; set; }

    public int StartupId { get; set; }

    public int MentorId { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime AssignedDate { get; set; }

    [StringLength(20)]
    public string Status { get; set; } = null!;

    [ForeignKey("MentorId")]
    [InverseProperty("Mentorassignments")]
    public virtual Mentor Mentor { get; set; } = null!;

    [ForeignKey("StartupId")]
    [InverseProperty("Mentorassignments")]
    public virtual Startup Startup { get; set; } = null!;
}
