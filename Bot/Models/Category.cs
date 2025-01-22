using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bot.Models;

[Table("Categories")]
public class Category
{
    [Key]
    [Column("Id")]
    public int Id { get; set; }
    [Column("Name")]
    public string Name { get; set; } = null!;

    public virtual ICollection<Purchase> Purchases { get; } = new List<Purchase>();
}