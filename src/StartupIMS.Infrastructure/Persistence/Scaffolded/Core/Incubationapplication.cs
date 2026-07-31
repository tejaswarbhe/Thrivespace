using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace StartupIMS.Infrastructure.Persistence.Scaffolded.Core;

[Table("incubationapplications")]
[Index("StartupId", Name = "IX_Applications_StartupId")]
public partial class Incubationapplication
{
    [Key]
    public int Id { get; set; }

    public int StartupId { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime SubmissionDate { get; set; }

    [StringLength(20)]
    public string Status { get; set; } = null!;

    [Column(TypeName = "text")]
    public string? Remarks { get; set; }

    [ForeignKey("StartupId")]
    [InverseProperty("Incubationapplications")]
    public virtual Startup Startup { get; set; } = null!;
}
