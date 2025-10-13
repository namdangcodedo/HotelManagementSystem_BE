using AppBackend.ApiCore.Extendsions;
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

builder.Services.AddControllers()   
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });


var app = builder.Build();

// Run seeding once
SeedData.Initialize(app);

// Middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRateLimiter();   
app.UseHttpsRedirection();
app.UseCors("AllowAllOrigins");
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();