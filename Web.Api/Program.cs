using Application.Interfaces;
using Application.Services;
using Application.Validators;
using FluentValidation;
using Infrastructure.Data;
using Infrastructure.Email;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Web.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseMySQL(connectionString, b => b.MigrationsAssembly("Web.Api")));


// Repositories
builder.Services.AddScoped<IVehicleRepository, VehicleRepository>();
builder.Services.AddScoped<IVehicleTypeRepository, VehicleTypeRepository>();

// Application Services
builder.Services.AddScoped<IParkingService, ParkingService>();
builder.Services.AddScoped<IEmailService, EmailService>();

// Email Infrastructure
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection(EmailOptions.SectionName));
builder.Services.AddMemoryCache();
builder.Services.AddTransient<EmailDelegatingHandler>();
builder.Services.AddHttpClient<EmailClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration.GetSection(EmailOptions.SectionName)["BaseUrl"] ?? "https://dev-sites.similtech.co/api-email/");
})
.AddHttpMessageHandler<EmailDelegatingHandler>();

// Validation
builder.Services.AddValidatorsFromAssemblyContaining<EntryRequestValidator>();


builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Swagger / OpenAPI
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Parqueadero API",
        Version = "v1",
        Description = "API de gestión de parqueadero — Prueba Técnica Semi Senior"
    });
});

var app = builder.Build();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Swagger UI
app.UseSwagger(c =>
{
    c.RouteTemplate = "swagger/{documentName}/swagger.json";
});
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Parqueadero API v1");
    c.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
