using AppBackend.ApiCore.Extensions;
using AppBackend.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Configs
builder.Services.AddCloudinaryConfig(builder.Configuration);
builder.Services.AddPayOSConfig(builder.Configuration);
builder.Services.AddAutoMapperConfig();
builder.Services.AddDbConfig(builder.Configuration);
builder.Services.AddCorsConfig();
builder.Services.AddSwaggerConfig();
builder.Services.AddDefaultAuth(builder.Configuration);
//Optional login with google
// builder.Services.AddGoogleAuth(builder.Configuration);builder.Services.AddServicesConfig();
builder.Services.AddSessionConfig();
builder.Services.AddHttpContextAccessor();
builder.Services.AddServicesConfig();
builder.Services.AddAutoMapperConfig();
builder.Services.AddRateLimitConfig();   

builder.Services.AddMemoryCache();
builder.Services.AddScoped<AppBackend.Services.Helpers.CacheHelper>();

builder.Services.AddControllers()   
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

builder.Services.Configure<AppBackend.BusinessObjects.AppSettings.GoogleAuthSettings>(builder.Configuration.GetSection("GoogleAuth"));

var app = builder.Build();

// Seed initial data (only runs if tables are empty)
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppBackend.BusinessObjects.Data.HotelManagementContext>();
    await AppBackend.ApiCore.Extension.SeedingData.SeedAsync(context);
}

// Middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRateLimiter();   
// app.UseHttpsRedirection(); // Disabled for local development to avoid HTTPS port issues
app.UseCors("AllowAllOrigins");
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();