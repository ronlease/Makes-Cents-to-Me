using MakesCentsToMe.Api.Features.Accounts;
using MakesCentsToMe.Api.Features.Import;
using MakesCentsToMe.Api.Features.Institutions;
using MakesCentsToMe.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// EF Core with PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Feature services
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IImportService, ImportService>();
builder.Services.AddScoped<IInstitutionService, InstitutionService>();

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
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// JSON options
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
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
app.MapInstitutionEndpoints();
app.MapAccountEndpoints();
app.MapImportEndpoints();

app.Run();
