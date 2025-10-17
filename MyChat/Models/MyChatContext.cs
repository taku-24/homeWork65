using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MyChat.Models;

public class MyChatContext : IdentityDbContext<User, IdentityRole<int>, int>
{
    public DbSet<Message> Messages { get; set; }

    public MyChatContext(DbContextOptions<MyChatContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<IdentityRole<int>>().HasData(
            new IdentityRole<int> { Id = 1, Name = "admin", NormalizedName = "ADMIN" },
            new IdentityRole<int> { Id = 2, Name = "user",  NormalizedName = "USER"  }
        );
    }
}