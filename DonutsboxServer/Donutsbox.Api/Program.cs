using Donutsbox.Api.Dto;
using Donutsbox.Api.Hubs;
using Donutsbox.Api.Mapper;
using Donutsbox.Api.Services;
using Donutsbox.Api.Services.AuthorService;
using Donutsbox.Api.Services.CreatorPostService;
using Donutsbox.Api.Services.FilesService;
using Donutsbox.Api.Services.Kafka;
using Donutsbox.Api.Services.MinioService;
using Donutsbox.Api.Services.Payments;
using Donutsbox.Api.Services.PostCommentService;
using Donutsbox.Api.Services.UserInteractionService;
using Donutsbox.Api.Services.UserSubscriptionsService;
using Donutsbox.Domain.Constants;
using Donutsbox.Domain.Context;
using Donutsbox.Domain.Entities;
using Donutsbox.Domain.Repositories.AuthorRepository;
using Donutsbox.Domain.Repositories.EntityRepository;
using Donutsbox.Domain.Repositories.ProfileRepository;
using Donutsbox.Domain.Repositories.UserSubscriptionsRepository;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: 'Bearer {token}'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            []
        }
    });
});

builder.Configuration
       .SetBasePath(Directory.GetCurrentDirectory())
       .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
       .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
       .AddEnvironmentVariables();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var key = Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!);
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,

        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key),

        RoleClaimType = ClaimTypes.Role
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var path = context.HttpContext.Request.Path;

            var accessToken = context.Request.Query["access_token"];
            if (!string.IsNullOrEmpty(accessToken) &&
                path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }

            var mediaToken = context.Request.Query["token"];
            if (!string.IsNullOrEmpty(mediaToken) &&
                path.StartsWithSegments("/api/files"))
            {
                context.Token = mediaToken;
            }

            if (string.IsNullOrEmpty(context.Token) &&
                context.Request.Cookies.TryGetValue(AuthConstants.JwtCookieName, out var cookieToken) &&
                !string.IsNullOrEmpty(cookieToken))
            {
                context.Token = cookieToken;
            }

            return Task.CompletedTask;
        }
    };
});

builder.Services.Configure<HostOptions>(o =>
{
    o.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
});

builder.Services.AddAuthorization();

builder.Services.AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>());
builder.Services.AddDbContext<DonutsboxDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<IEntityRepository<User, Guid>>(sp => sp.GetRequiredService<UserRepository>());
builder.Services.AddScoped<IUserSubscriptionsRepository>(sp => sp.GetRequiredService<UserRepository>());
builder.Services.AddScoped<IAuthorRepository, AuthorRepository>();

builder.Services.AddScoped<IEntityRepository<UserAuth, Guid>, UserAuthRepository>();
builder.Services.AddScoped<IEntityRepository<UserData, Guid>, UserDataRepository>();
builder.Services.AddScoped<IEntityRepository<UserSubscription, Guid>, UserSubscriptionRepository>();
builder.Services.AddScoped<IEntityRepository<UserType, int>, UserTypeRepository>();
builder.Services.AddScoped<IEntityRepository<Subscription, Guid>, SubscriptionRepository>();
builder.Services.AddScoped<IEntityRepository<SubscriptionPayment, Guid>, SubscriptionPaymentRepository>();
builder.Services.AddScoped<IEntityRepository<CreatorPageData, Guid>, CreatorPageDataRepository>();
builder.Services.AddScoped<IEntityRepository<ContentPost, Guid>, ContentPostRepository>();
builder.Services.AddScoped<IEntityRepository<SubscriptionPeriod, int>, SubscriptionPeriodRepository>();
builder.Services.AddScoped<IEntityRepository<PostComment, Guid>, PostCommentRepository>();
builder.Services.AddScoped<IEntityRepository<PostReaction, Guid>, PostReactionRepository>();
builder.Services.AddScoped<IEntityRepository<ReactionType, int>, ReactionTypeRepository>();


builder.Services.AddScoped<IEntityService<UserDto, Guid>, UserService>();
builder.Services.AddScoped<IEntityService<UserAuthDto, Guid>, UserAuthService>();
builder.Services.AddScoped<IEntityService<UserDataDto, Guid>, UserDataService>();
builder.Services.AddScoped<IEntityService<UserSubscriptionDto, Guid>, UserSubscriptionService>();
builder.Services.AddScoped<IEntityService<UserTypeDto, int>, UserTypeService>();
builder.Services.AddScoped<IEntityService<SubscriptionDto, Guid>, SubscriptionService>();
builder.Services.AddScoped<IEntityService<CreatorPageDataDto, Guid>, CreatorPageDataService>();
builder.Services.AddScoped<IEntityService<ContentPostDto, Guid>, ContentPostService>();


builder.Services.AddScoped<IUserSubscriptionsService, UserSubscriptionsService>();
builder.Services.AddScoped<IUserInteractionService, UserInteractionService>();
builder.Services.AddScoped<ICreatorPostService, CreatorPostService>();
builder.Services.AddScoped<IAuthorService, AuthorService>();
builder.Services.AddScoped<IPostCommentService, PostCommentService>();
builder.Services.AddScoped<IFilesService, FilesService>();
builder.Services.AddScoped<ISubscriptionPaymentService, SubscriptionPaymentService>();

builder.Services.AddSingleton<IMinioService, MinioService>();

builder.Services.AddScoped<IMessageProducer, KafkaMessageProducer>();

builder.Services.AddSignalR();

var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (corsOrigins.Length == 0)
        {
            policy.AllowAnyOrigin()
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
        else
        {
            policy.WithOrigins(corsOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
    });
});

builder.Services.Configure<YooKassaOptions>(builder.Configuration.GetSection("YooKassa"));
builder.Services.AddHttpClient<IYooKassaClient, YooKassaClient>();

builder.Services.AddControllers();

builder.Services.AddHostedService<VideoProcessedConsumer>();


var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<DonutsboxDbContext>();
        context.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Произошла ошибка при применении миграций базы данных");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "V1");
        c.RoutePrefix = string.Empty;
        c.ConfigObject.AdditionalItems["withCredentials"] = true;
    });
}
app.UseForwardedHeaders();
app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.UseHttpsRedirection();
app.MapControllers();

app.MapHub<CommentsHub>("/hubs/comments");

app.Run();