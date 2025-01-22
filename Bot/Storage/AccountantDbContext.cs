using Bot.Models;
using Microsoft.EntityFrameworkCore;

namespace Bot.Storage;

public class AccountantDbContext : DbContext
{
    public DbSet<Category> Categories { get; set; }
    public DbSet<Purchase> Purchases { get; set; }
}