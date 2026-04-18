using MakesCentsToMe.Api.Features.Accounts;
using MakesCentsToMe.Api.Features.Categories;
using MakesCentsToMe.Api.Features.Import;
using MakesCentsToMe.Api.Features.Institutions;
using MakesCentsToMe.Api.Features.Review;
using MakesCentsToMe.Api.Infrastructure.Claude;
using MakesCentsToMe.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// EF Core with PostgreSQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var dataSourceBuilder = new Npgsql.NpgsqlDataSourceBuilder(connectionString);
dataSourceBuilder.EnableDynamicJson();
var dataSource = dataSourceBuilder.Build();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(dataSource));

// Feature services
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IClaudeAnalysisService, ClaudeAnalysisService>();
builder.Services.AddScoped<IImportService, ImportService>();
builder.Services.AddScoped<IInstitutionService, InstitutionService>();
builder.Services.AddScoped<IReviewService, ReviewService>();

// Claude API HTTP client
builder.Services.AddHttpClient<ClaudeAnalysisService>(client =>
{
    client.BaseAddress = new Uri("https://api.anthropic.com/");
    client.DefaultRequestHeaders.Add("x-api-key", builder.Configuration["Claude:ApiKey"]);
    client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
});

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
    {
        Title = "Makes Cents To Me API",
        Version = "v1",
    });
});

// CORS for Angular dev server
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("https://localhost:4210")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// JSON options
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var app = builder.Build();

// Auto-migrate on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Makes Cents To Me API v1");
    });
}

app.UseCors();
app.UseHttpsRedirection();

// Map feature endpoints
app.MapAccountEndpoints();
app.MapCategoryEndpoints();
app.MapImportEndpoints();
app.MapInstitutionEndpoints();
app.MapReviewEndpoints();

app.Run();
