using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Service.Operaciones.Application.Common.Behaviors;
using Service.Operaciones.Application.Interfaces;
using Service.Operaciones.Infrastructure.Database;
using Service.Operaciones.Infrastructure.Repositories;
using Service.Operaciones.Infrastructure.Services;
using Service.Operaciones.Infrastructure.Services.Parsers;
using Service.Operaciones.API.Middleware;

// Fix for Npgsql DateTime issues
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();

// CORS para Angular
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp",
        policy => policy.WithOrigins(allowedOrigins)
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials());
});

// Swagger con JWT
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Ingrese el token JWT obtenido de Service.Seguridad"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
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
    });
});

// JWT Bearer
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "http://localhost:5000";
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "Service.Seguridad",
            ValidAudience = "Monocont"
        };
    });

builder.Services.AddAuthorization();

// DbContext
var connectionString = builder.Configuration.GetConnectionString("OperacionesDb");
builder.Services.AddDbContext<OperacionesDbContext>(options =>
    options.UseNpgsql(connectionString));

// Memory Cache
builder.Services.AddMemoryCache();

// Unit of Work y Repositorios
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IArchivoCargaRepository, ArchivoCargaRepository>();
builder.Services.AddScoped<IArchivoCargaErrorRepository, ArchivoCargaErrorRepository>();
builder.Services.AddScoped<ICompraRepository, CompraRepository>();
builder.Services.AddScoped<IVentaRepository, VentaRepository>();
builder.Services.AddScoped<IVentaEmpresaRepository, VentaEmpresaRepository>();
builder.Services.AddScoped<IVentaMatchRepository, VentaMatchRepository>();

// Servicios de Application
builder.Services.AddScoped<IHashService, HashService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUsuarioActualService, UsuarioActualService>();
builder.Services.AddScoped<IAccesoEmpresaValidator, AccesoEmpresaValidator>();
builder.Services.AddScoped<IArchivoSunatParserFactory, ArchivoSunatParserFactory>();
builder.Services.AddScoped<IVentaValidationService, Service.Operaciones.Application.Services.VentaValidationService>();
builder.Services.AddScoped<ICompraValidationService, Service.Operaciones.Application.Services.CompraValidationService>();
builder.Services.AddScoped<IVentaEmpresaParser, VentaEmpresaExcelParser>();
builder.Services.AddScoped<IVentaEmpresaValidationService, Service.Operaciones.Application.Services.VentaEmpresaValidationService>();

// HttpClient para Service.Empresa
var empresaUrl = builder.Configuration.GetSection("ServiceUrls:Empresa").Value ?? "http://localhost:5001";
builder.Services.AddHttpClient<IEmpresaService, EmpresaHttpService>(client =>
{
    client.BaseAddress = new Uri(empresaUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

// MediatR
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Service.Operaciones.Application.Common.Behaviors.ValidationBehavior<,>).Assembly);
});

// FluentValidation
builder.Services.AddValidatorsFromAssembly(typeof(Service.Operaciones.Application.Common.Behaviors.ValidationBehavior<,>).Assembly);
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

var app = builder.Build();

// Middlewares
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseMiddleware<ResponseFormattingMiddleware>();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseCors("AllowAngularApp");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
