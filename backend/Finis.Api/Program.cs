using System.Text;
using Finis.Api.Data;
using Finis.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add Controllers
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

// CORS configuration for Angular frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp", policy =>
    {
        policy.WithOrigins("http://localhost:4200", "http://127.0.0.1:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Database configuration with MySQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=localhost;Port=3306;Database=finis_db;User=root;Password=;CharSet=utf8mb4;";

builder.Services.AddDbContext<FinisDbContext>(options =>
{
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 30)), mySqlOptions =>
    {
        mySqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorNumbersToAdd: null);
    });
});

// Dependency Injection
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<ITermometroEngineService, TermometroEngineService>();
builder.Services.AddScoped<ICreditCardService, CreditCardService>();
builder.Services.AddScoped<IFinisAdvisorService, FinisAdvisorService>();

// JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? "FinisSecureSuperSecretKey2026!FinisFinancialCashflowTermometroLongSecretKey";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "FinisApi";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "FinisApp";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

// Auto-create database schema and seed default data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var dbContext = services.GetRequiredService<FinisDbContext>();
        logger.LogInformation("Verificando/Criando banco de dados MySQL 'finis_db'...");
        await dbContext.Database.EnsureCreatedAsync();
        await DbInitializer.SeedAsync(dbContext);
        logger.LogInformation("Banco de dados pronto e categorias padrão inicializadas!");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Aviso: Não foi possível conectar ao MySQL automaticamente na inicialização. Verifique se o serviço MySQL está em execução na porta 3306 com as credenciais de appsettings.json.");
    }
}

app.UseCors("AllowAngularApp");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
