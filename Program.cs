using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Conduit;
using Conduit.Domain;
using Conduit.Infrastructure;
using Conduit.Infrastructure.Errors;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;

// read database configuration (database provider + database connection) from environment variables
//Environment.GetEnvironmentVariable(DEFAULT_DATABASE_PROVIDER)
//Environment.GetEnvironmentVariable(DEFAULT_DATABASE_CONNECTION_STRING)
var defaultDatabaseConnectionSrting = "Filename=festival.db";
var defaultDatabaseProvider = "sqlite";

var builder = WebApplication.CreateBuilder(args);

// take the connection string from the environment variable or use hard-coded database name
var connectionString = defaultDatabaseConnectionSrting;

// take the database provider from the environment variable or use hard-coded database provider
var databaseProvider = defaultDatabaseProvider;

builder.Services.AddDbContext<ConduitContext>(options =>
{
    if (databaseProvider.ToLowerInvariant().Trim().Equals("sqlite", StringComparison.Ordinal))
    {
        options.UseSqlite(connectionString);
    }
    else if (
        databaseProvider.ToLowerInvariant().Trim().Equals("sqlserver", StringComparison.Ordinal)
    )
    {
        // only works in windows container
        options.UseSqlServer(connectionString);
    }
    else
    {
        throw new InvalidOperationException(
            "Database provider unknown. Please check configuration"
        );
    }
});

builder.Services.AddLocalization(x => x.ResourcesPath = "Resources");

// Inject an implementation of ISwaggerProvider with defaulted settings applied
builder.Services.AddSwaggerGen(x =>
{
    x.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            In = ParameterLocation.Header,
            Description = "Please insert JWT with Bearer into field",
            Name = "Authorization",
            Type = SecuritySchemeType.ApiKey,
            BearerFormat = "JWT"
        }
    );

    x.SupportNonNullableReferenceTypes();

    x.AddSecurityRequirement(
        new OpenApiSecurityRequirement
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
                Array.Empty<string>()
            }
        }
    );
    x.SwaggerDoc("v1", new OpenApiInfo { Title = "RealWorld API", Version = "v1" });
    x.CustomSchemaIds(y => y.FullName);
    x.DocInclusionPredicate((_, _) => true);
    x.TagActionsBy(y => new List<string> { y.GroupName ?? throw new InvalidOperationException() });
    x.CustomSchemaIds(s => s.FullName?.Replace("+", "."));
});

builder.Services.AddCors();
builder
    .Services.AddMvc(opt =>
    {
        opt.Conventions.Add(new GroupByApiRootConvention());
        opt.Filters.Add(typeof(ValidatorActionFilter));
        opt.EnableEndpointRouting = false;
    })
    .AddJsonOptions(opt =>
        opt.JsonSerializerOptions.DefaultIgnoreCondition = System
            .Text
            .Json
            .Serialization
            .JsonIgnoreCondition
            .WhenWritingNull
    );

builder.Services.AddConduit();

builder.Services.AddJwt();

var app = builder.Build();

app.Services.GetRequiredService<ILoggerFactory>().AddSerilogLogging();

app.UseMiddleware<ErrorHandlingMiddleware>();

app.UseCors(x => x.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());

app.UseAuthentication();
app.UseMvc();

// Enable middleware to serve generated Swagger as a JSON endpoint
app.UseSwagger(c => c.RouteTemplate = "swagger/{documentName}/swagger.json");

// Enable middleware to serve swagger-ui assets(HTML, JS, CSS etc.)
app.UseSwaggerUI(x => x.SwaggerEndpoint("/swagger/v1/swagger.json", "RealWorld API V1"));

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ConduitContext>();
    var created = dbContext.Database.EnsureCreated();
    
    // Seed data nếu database mới được tạo
    if (created)
    {
        await SeedDatabase(dbContext);
    }
}

static async Task SeedDatabase(ConduitContext context)
{
    // Kiểm tra xem đã có dữ liệu chưa
    if (await context.Persons.AnyAsync())
    {
        return;
    }

    // Seed Users
    var admin = new Person
    {
        Username = "admin",
        Email = "admin@realworld.com",
        Bio = "Administrator of RealWorld",
        Image = "https://via.placeholder.com/150",
        Hash = System.Text.Encoding.UTF8.GetBytes("AQAAAAEAACcQAAAAEFtpJR7Z3K7HcE7K5o8sNrJGHtUfHKlvQrKRkM7eZj8J6P3n4TFd9ZmGkEr6L1xCqA=="),
        Salt = System.Text.Encoding.UTF8.GetBytes("salt123")
    };

    var testUser = new Person
    {
        Username = "testuser",
        Email = "test@realworld.com",
        Bio = "Test user for RealWorld",
        Image = "https://via.placeholder.com/150",
        Hash = System.Text.Encoding.UTF8.GetBytes("AQAAAAEAACcQAAAAEFtpJR7Z3K7HcE7K5o8sNrJGHtUfHKlvQrKRkM7eZj8J6P3n4TFd9ZmGkEr6L1xCqA=="),
        Salt = System.Text.Encoding.UTF8.GetBytes("salt456")
    };

    context.Persons.AddRange(admin, testUser);
    await context.SaveChangesAsync();

    // Seed Tags
    var tags = new[]
    {
        new Tag { TagId = "technology" },
        new Tag { TagId = "programming" },
        new Tag { TagId = "aspnet" },
        new Tag { TagId = "csharp" },
        new Tag { TagId = "webdev" }
    };
    context.Tags.AddRange(tags);
    await context.SaveChangesAsync();

    // Seed Articles
    var article1 = new Article
    {
        Title = "Welcome to RealWorld",
        Description = "Getting started with RealWorld application",
        Body = "This is the first article in our RealWorld application. Learn how to create amazing content!",
        Slug = "welcome-to-realworld",
        Author = admin,
        CreatedAt = DateTime.UtcNow.AddDays(-7),
        UpdatedAt = DateTime.UtcNow.AddDays(-7)
    };

    var article2 = new Article
    {
        Title = "ASP.NET Core Best Practices",
        Description = "Learn the best practices for ASP.NET Core development",
        Body = "In this article, we'll explore the best practices for developing applications with ASP.NET Core.",
        Slug = "aspnet-core-best-practices",
        Author = admin,
        CreatedAt = DateTime.UtcNow.AddDays(-5),
        UpdatedAt = DateTime.UtcNow.AddDays(-5)
    };

    context.Articles.AddRange(article1, article2);
    await context.SaveChangesAsync();

    // Seed Comments
    var comments = new[]
    {
        new Comment
        {
            Body = "Great article! Very helpful for beginners.",
            Article = article1,
            Author = testUser,
            CreatedAt = DateTime.UtcNow.AddDays(-6),
            UpdatedAt = DateTime.UtcNow.AddDays(-6)
        },
        new Comment
        {
            Body = "Thanks for sharing these best practices!",
            Article = article2,
            Author = testUser,
            CreatedAt = DateTime.UtcNow.AddDays(-4),
            UpdatedAt = DateTime.UtcNow.AddDays(-4)
        }
    };
    context.Comments.AddRange(comments);
    await context.SaveChangesAsync();

    // Seed Article Tags
    var articleTags = new[]
    {
        new ArticleTag { Article = article1, Tag = tags[0] }, // technology
        new ArticleTag { Article = article1, Tag = tags[4] }, // webdev
        new ArticleTag { Article = article2, Tag = tags[1] }, // programming
        new ArticleTag { Article = article2, Tag = tags[2] }, // aspnet
        new ArticleTag { Article = article2, Tag = tags[3] }  // csharp
    };
    context.ArticleTags.AddRange(articleTags);
    await context.SaveChangesAsync();
}
app.Run();
