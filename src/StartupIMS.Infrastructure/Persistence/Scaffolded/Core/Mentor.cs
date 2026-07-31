using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace StartupIMS.Infrastructure.Persistence.Scaffolded.Core;

[Table("mentors")]
[Index("UserId", Name = "UX_Mentors_UserId", IsUnique = true)]
public partial class Mentor
{
    [Key]
    public int Id { get; set; }

    public int UserId { get; set; }

    [StringLength(200)]
    public string Expertise { get; set; } = null!;

    public int ExperienceYears { get; set; }

    [StringLength(200)]
    public string Organization { get; set; } = null!;

    [InverseProperty("Mentor")]
    public virtual ICollection<Mentorassignment> Mentorassignments { get; set; } = new List<Mentorassignment>();
}
