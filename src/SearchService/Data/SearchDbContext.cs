using Microsoft.EntityFrameworkCore;
using SearchService.Models;

namespace SearchService;
public class SearchDbContext : DbContext
{
    public SearchDbContext(DbContextOptions options) : base(options)
    {

    }

    public DbSet<Item> Items { get; set; }
}
