using Donutsbox.Api.Dto;
using Donutsbox.Api.Mapper;
using Donutsbox.Api.Services;
using Donutsbox.Domain.Context;
using Donutsbox.Domain.Entities;
using Donutsbox.Domain.Repositories.EntityRepository;
using Donutsbox.Domain.Repositories.ProfileRepository;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
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
});

builder.Services.AddAuthorization();

builder.Services.AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>());
builder.Services.AddDbContext<DonutsboxDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));
builder.Services.AddScoped<IEntityRepository<User, Guid>, UserRepository>();
builder.Services.AddScoped<IEntityRepository<UserAuth, Guid>, UserAuthRepository>();
builder.Services.AddScoped<IEntityRepository<UserData, Guid>, UserDataRepository>();
builder.Services.AddScoped<IEntityRepository<UserSubscription, Guid>, UserSubscriptionRepository>();
builder.Services.AddScoped<IEntityRepository<UserType, int>, UserTypeRepository>();
builder.Services.AddScoped<IEntityRepository<Subscription, Guid>, SubscriptionRepository>();
builder.Services.AddScoped<IEntityRepository<CreatorPageData, Guid>, CreatorPageDataRepository>();
builder.Services.AddScoped<IEntityRepository<ContentPost, Guid>, ContentPostRepository>();

builder.Services.AddScoped<IEntityService<UserDto, Guid>, UserService>();
builder.Services.AddScoped<IEntityService<UserAuthDto, Guid>, UserAuthService>();
builder.Services.AddScoped<IEntityService<UserDataDto, Guid>, UserDataService>();
builder.Services.AddScoped<IEntityService<UserSubscriptionDto, Guid>, UserSubscriptionService>();
builder.Services.AddScoped<IEntityService<UserTypeDto, int>, UserTypeService>();
builder.Services.AddScoped<IEntityService<SubscriptionDto, Guid>, SubscriptionService>();
builder.Services.AddScoped<IEntityService<CreatorPageDataDto, Guid>, CreatorPageDataService>();
builder.Services.AddScoped<IEntityService<ContentPostDto, Guid>, ContentPostService>();

builder.Services.AddCors(options => options.AddDefaultPolicy(policy => { policy.AllowAnyOrigin(); policy.AllowAnyMethod(); policy.AllowAnyHeader(); }));

builder.Services.AddControllers();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "V1");
        c.RoutePrefix = string.Empty;
    });
}
app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.UseHttpsRedirection();
app.MapControllers();
app.Run();
