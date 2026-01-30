using LiveChatTask.Data;
using LiveChatTask.Hubs;
using LiveChatTask.Models;
using LiveChatTask.Services;
using LiveChatTask.Swagger;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// 1. Add Services
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 4;
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// AuthService registration
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<IChatSettingsService, ChatSettingsService>();
builder.Services.AddScoped<IPresenceService, PresenceService>();
builder.Services.AddScoped<IFileUploadService, FileUploadService>();
builder.Services.AddHostedService<PresenceMonitor>();
builder.Services.AddHostedService<IdleChatMonitor>();

// Configure authentication cookie paths for login/access denied redirects
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/User/Login";
    options.AccessDeniedPath = "/User/Login";
    options.Cookie.HttpOnly = true;
    options.SlidingExpiration = true;
});
builder.Services.AddControllers().AddJsonOptions(options => {
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;

});
builder.Services.AddRazorPages();
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "LiveChat API", Version = "v1" });
    c.CustomSchemaIds(type => type.FullName?.Replace("+", ".") ?? type.Name);
    //c.SchemaFilter<SwaggerExcludeEntitySchemaFilter>();
});
var app = builder.Build();

// Seed Database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var context = services.GetRequiredService<AppDbContext>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

    await DbInitializer.InitializeAsync(context, userManager, roleManager);
}


// 2. Configure Middleware
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "LiveChat API v1");
        c.RoutePrefix = "swagger"; // localhost/swagger
    });
}
else
{
    app.UseExceptionHandler("/Error");
    
}


app.UseHttpsRedirection();

var staticFileOptions = new StaticFileOptions();
var contentTypeProvider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
contentTypeProvider.Mappings[".webm"] = "audio/webm";
contentTypeProvider.Mappings[".weba"] = "audio/webm";
contentTypeProvider.Mappings[".ogg"] = "audio/ogg";
staticFileOptions.ContentTypeProvider = contentTypeProvider;
app.UseStaticFiles(staticFileOptions);

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapRazorPages();
app.MapHub<ChatHub>("/hubs/chat");

app.Run();
