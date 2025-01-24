using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bot.Models;

[Table("Purchases")]
public class Purchase
{
    [Key]
    [Column("Id")]
    public int Id { get; set; }
    [Column("CategoryId")]
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    [Column("User")]
    public string User { get; set; } = null!;
    [Column("Spent")]
    public decimal Spent { get; set; }
    [Column("Comment")]
    [MaxLength(128)]
    public string? Comment { get; set; }
    [Column("Date")]
    public DateTime Date { get; set; }
}