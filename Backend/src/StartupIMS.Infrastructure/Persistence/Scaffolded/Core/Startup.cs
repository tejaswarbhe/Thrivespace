using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace StartupIMS.Infrastructure.Persistence.Scaffolded.Core;

[Table("startups")]
[Index("UserId", Name = "UX_Startups_UserId", IsUnique = true)]
public partial class Startup
{
    [Key]
    public int Id { get; set; }

    public int UserId { get; set; }

    [StringLength(200)]
    public string Name { get; set; } = null!;

    [StringLength(100)]
    public string Domain { get; set; } = null!;

    [Column(TypeName = "text")]
    public string? Description { get; set; }

    public DateOnly FoundingDate { get; set; }

    [StringLength(20)]
    public string Status { get; set; } = null!;

    [InverseProperty("Startup")]
    public virtual ICollection<Fundingrequest> Fundingrequests { get; set; } = new List<Fundingrequest>();

    [InverseProperty("Startup")]
    public virtual ICollection<Incubationapplication> Incubationapplications { get; set; } = new List<Incubationapplication>();

    [InverseProperty("Startup")]
    public virtual ICollection<Mentorassignment> Mentorassignments { get; set; } = new List<Mentorassignment>();

    [InverseProperty("Startup")]
    public virtual ICollection<Progressreport> Progressreports { get; set; } = new List<Progressreport>();
}
