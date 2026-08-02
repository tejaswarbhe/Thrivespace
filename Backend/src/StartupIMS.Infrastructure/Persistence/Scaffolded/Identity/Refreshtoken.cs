using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace StartupIMS.Infrastructure.Persistence.Scaffolded.Identity;

[Table("refreshtokens")]
[Index("TokenHash", Name = "IX_RefreshTokens_TokenHash")]
[Index("UserId", Name = "IX_RefreshTokens_UserId")]
public partial class Refreshtoken
{
    [Key]
    public int Id { get; set; }

    public int UserId { get; set; }

    [StringLength(200)]
    public string TokenHash { get; set; } = null!;

    [Column(TypeName = "datetime")]
    public DateTime ExpiresAt { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime CreatedAt { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? RevokedAt { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("Refreshtokens")]
    public virtual User User { get; set; } = null!;
}
