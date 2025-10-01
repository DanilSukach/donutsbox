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
    public required DbSet<SubscriptionPeriod> SubscriptionPeriods { get; set; } 

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<UserType>().HasData(
            new UserType { Id = 1, Name = "User" },
            new UserType { Id = 2, Name = "Creator" },
            new UserType { Id = 3, Name = "Administrator" }
        );
        modelBuilder.Entity<ReactionType>().HasData(
            new ReactionType { Id = 1, Name = "Like" },
            new ReactionType { Id = 2, Name = "Dislike" }
        );
        modelBuilder.Entity<SubscriptionPeriod>().HasData(
            new SubscriptionPeriod { Id = 1, Months = 1 },
            new SubscriptionPeriod { Id = 2, Months = 3 },
            new SubscriptionPeriod { Id = 3, Months = 6 },
            new SubscriptionPeriod { Id = 4, Months = 12 }
        );
    }
}