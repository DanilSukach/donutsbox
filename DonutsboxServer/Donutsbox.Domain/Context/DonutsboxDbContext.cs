using Donutsbox.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Donutsbox.Domain.Context;

public class DonutsboxDbContext(DbContextOptions<DonutsboxDbContext> options) : DbContext(options)
{
    public required DbSet<User> Users { get; set; }
    public required DbSet<UserType> UserTypes { get; set; }
    public required DbSet<UserAuth> UsersAuths { get; set; }
    public required DbSet<UserData> UsersData { get; set; }
    public required DbSet<UserSubscription> UsersSubscriptions { get; set; }
    public required DbSet<Subscription> Subscriptions { get; set; }
    public required DbSet<CreatorPageData> CreatorsPageData { get; set; }
    public required DbSet<ContentPost> ContentPosts { get; set; }
    public required DbSet<PostComment> PostComments { get; set; }
    public required DbSet<PostReaction> PostReactions { get; set; }
    public required DbSet<ReactionType> ReactionTypes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<UserType>().HasData(
            new UserType { Id = 1, Name = "User" },
            new UserType { Id = 2, Name = "Creator" },
            new UserType { Id = 3, Name = "Administrator" }
        );

        modelBuilder.Entity<UserType>()
            .HasMany<User>()
            .WithOne(u => u.Type)
            .HasForeignKey("type_id")
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserAuth>()
            .HasOne<User>()
            .WithOne()
            .HasForeignKey<User>(u => u.AuthId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<User>()
            .HasOne<UserData>()
            .WithOne()
            .HasForeignKey<UserData>(ud => ud.UserId);

        modelBuilder.Entity<User>()
            .HasMany<UserSubscription>()
            .WithOne()
            .HasForeignKey(us => us.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Subscription>()
            .HasMany<UserSubscription>()
            .WithOne()
            .HasForeignKey(us => us.SubscriptionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CreatorPageData>()
            .HasMany<Subscription>()
            .WithOne()
            .HasForeignKey(s => s.PageId)
            .OnDelete(DeleteBehavior.Cascade);
    
        modelBuilder.Entity<CreatorPageData>()
            .HasMany<ContentPost>()
            .WithOne()
            .HasForeignKey(cp => cp.PageId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<User>()
            .HasOne<CreatorPageData>()
            .WithOne()
            .HasForeignKey<CreatorPageData>(cp => cp.GUID)
            .OnDelete(DeleteBehavior.Cascade);

        //modelBuilder.Entity<User>()
        //    .HasOne(u => u.UserAuth)
        //    .WithOne(ua => ua.User);

        modelBuilder.Entity<PostComment>()
            .HasKey(pc => pc.Id);

        modelBuilder.Entity<PostComment>()
            .HasOne<ContentPost>()
            .WithMany()
            .HasForeignKey(pc => pc.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PostComment>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(pc => pc.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}