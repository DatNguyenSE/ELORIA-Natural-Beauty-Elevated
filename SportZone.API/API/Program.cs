using System.Text;
using SportZone.Application.Interfaces;
using SportZone.Application.Interfaces.IService;
using SportZone.Infrastructure.Repositories;
using SportZone.Infrastructure.Service;
using API.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using AutoMapper;
using SportZone.Application.Services;
using SportZone.Application.Mappings;
using SportZone.Infrastructure.Data;
using SportZone.API.Middlewares;
using Sport.Application.IService;
using Sport.Infrastructure.Service;
using SportZone.Domain.Entities;
using Hangfire;
using Hangfire.PostgreSql;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddControllers();
//AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile).Assembly);
// Swagger config
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(option =>
{
    option.SwaggerDoc("v1", new OpenApiInfo { Title = "SportZone API", Version = "v1" });

    // Cấu hình để Swagger hiển thị nút "Authorize" (ổ khóa)
    option.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
      In = ParameterLocation.Header,
      Description = "Please, enter the token code in the blank",
      Name = "Authorizaion",
      Type = SecuritySchemeType.Http,
      BearerFormat = "JWT",
      Scheme = "Bearer"
    });
    option.AddSecurityRequirement(new OpenApiSecurityRequirement
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
            new string[]{}
        }
    });
});

//Email
builder.Services.Configure<SportZone.Infrastructure.Configuration.EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));

//Cấu hình DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// THÊM CẤU HÌNH HANGFIRE 
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(c => c.UseNpgsqlConnection(builder.Configuration.GetConnectionString("DefaultConnection"))));

// Thêm Hangfire Server để xử lý job ngầm
builder.Services.AddHangfireServer();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddTransient<IEmailService, EmailService>();
builder.Services.AddScoped<IVnPayService,VnPayService>();


builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IProductSizeService, ProductSizeService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IPhotoService,PhotoService>();
builder.Services.AddScoped<IPromotionService, PromotionService>();
builder.Services.AddScoped<IFeatureService, FeatureService>();
builder.Services.AddScoped<IMembersService, MembersService>();

//Identity(user)
builder.Services.AddIdentityCore<AppUser>(opt =>
{
    opt.Password.RequireNonAlphanumeric = false; //no (@, #, !)
    opt.User.RequireUniqueEmail = true; //Unique Email
})
.AddRoles<IdentityRole>() // Activate the Role feature
.AddEntityFrameworkStores<AppDbContext>() // store user to db via AppDbcontext
.AddDefaultTokenProviders();

//JWT config
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var tokenKey = builder.Configuration["TokenKey"]
            ?? throw new Exception("Token key not found - Program.cs");
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true, // Token signature varification 
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenKey)),  //compare signature to secret-key
            ValidateIssuer = false, // skip issuer
            ValidateAudience = false // skip Audience
        };
    });

var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(); 
    app.UseHangfireDashboard(); 
}

// Configure the HTTP request pipeline.
app.UseMiddleware<ExceptionMiddleware>();
app.UseCors(x => x
    .WithOrigins("http://localhost:4200", "https://localhost:4200")
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials() 
);

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();

app.MapFallbackToController("Index", "Fallback");

// Trong file Program.cs
using var scope = app.Services.CreateScope();
var services = scope.ServiceProvider;

try 
{
    var context = services.GetRequiredService<AppDbContext>();
    
    await context.Database.MigrateAsync();

    await Seed.SeedCategories(context); 

    await Seed.SeedProducts(context); 
    
    await Seed.SeedProductSizes(context);

    await Seed.SeedComboItems(context);

}
catch (Exception ex)
{
    var logger = services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "Lỗi trong quá trình Seeding dữ liệu");
}
app.Run();

