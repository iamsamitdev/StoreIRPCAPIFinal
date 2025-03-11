using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using StoreIRPCAPI.Data;
using StoreIRPCAPI.Converters;

var builder = WebApplication.CreateBuilder(args);

// ให้ ASP.NET Core โหลดค่าจาก appsettings.json
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(8800); // HTTP
    options.ListenAnyIP(8801, listenOptions => listenOptions.UseHttps()); // HTTPS
});

// For Entity Framework with Npgsql
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});

// Adding Identity
builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Adding Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
// Adding Jwt Bearer
.AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new TokenValidationParameters()
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidAudience = builder.Configuration.GetSection("JWT:ValidAudience").Value!,
        ValidIssuer = builder.Configuration.GetSection("JWT:ValidIssuer").Value!,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration.GetSection("JWT:Secret").Value!))
    };
});

// Allow CORS
builder.Services.AddCors(options => 
{
 options.AddPolicy("MultipleOrigins",
    policy =>
    {
        policy.WithOrigins(
            // "*", // Allow any origin
            "https://itgenius.co.th", // Allow specific origin
            "https://*.itgenius.co.th/", // Allow subdomain
            "https://*.azurewebsites.net/", // Azure Apps
            "https://*.netlify.app/", // Netlify Apps
            "https://*.vercel.app/", // Vercel Apps
            "https://*.herokuapp.com/", // Heroku Apps
            "https://*.firebaseapp.com/", // Firebase Apps
            "https://*.github.io/", // Github Pages
            "https://*.gitlab.io/", // Gitlab Pages
            "https://*.onrender.com/", // Render Apps
            "https://*.surge.sh/", // Surge Apps
            "http://localhost:8080", // Vue , Svelte Apps
            "http://localhost:4200", // Angular Apps
            "http://localhost:3000", // React Apps
            "http://localhost:5173", // Vite Apps
            "http://localhost:5000", // Blazor Apps
            "http://localhost:5001" // Blazor Apps
        )
        .SetIsOriginAllowedToAllowWildcardSubdomains()
        .AllowAnyMethod()
        .AllowAnyHeader();
    });
});

// Cors Allow Specific
// builder.Services.AddCors(options =>
// {
//     options.AddPolicy("AllowSpecificOrigin",
//         builder =>
//         {
//             builder.WithOrigins("http://localhost:3000", "http://localhost:4200");
//             builder.WithHeaders("Authorization", "Content-Type", "Accept", "Origin", "X-Request-With");
//             builder.WithMethods("GET", "POST", "PUT", "DELETE");
//         }
//     );
// });

// กำหนดการตั้งค่า JSON serialization เพื่อกำหนดรูปแบบวันที่ให้ไม่มี timezone
builder.Services.AddControllers().AddJsonOptions(options =>
{
    // กำหนดรูปแบบวันที่ให้ไม่มี timezone
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    // กำหนดให้ DateTime ไม่มี timezone
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    // เพิ่ม DateTimeWithoutTimeZoneConverter
    options.JsonSerializerOptions.Converters.Add(new DateTimeWithoutTimeZoneConverter());
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opt =>
    {
        opt.SwaggerDoc(
            "v1",
            new Microsoft.OpenApi.Models.OpenApiInfo
            {
                Title = "Store IRPC API",
                Description = "API สำหรับระบบร้านค้า IRPC",
            }
        );

        opt.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
        {
            Name = "Authorization",
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "JWT Authorization header using the Bearer scheme. \r\n\r\n Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: \"Bearer 12345abcdef\"",
        });

        opt.AddSecurityRequirement(new OpenApiSecurityRequirement
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
                new string[] {}

            }
        });
    }
);

var app = builder.Build();

// เปิด Swagger สำหรับ Production ด้วย
if (app.Environment.IsProduction() || app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    
    // Redirect ไป Swagger ถ้าเข้าหน้าแรก
    app.Use(async (context, next) =>
    {
        if (context.Request.Path == "/")
        {
            context.Response.Redirect("/swagger/index.html");
            return;
        }
        await next();
    });
}


// app.UseHttpsRedirection();

// Add Authentication
app.UseAuthentication();

app.UseAuthorization();

// กำหนดให้สามารถเข้าถึงไฟล์ใน wwwroot ได้
app.UseStaticFiles();

app.MapControllers();

app.Run();
