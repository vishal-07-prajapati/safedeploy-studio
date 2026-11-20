using SafeDeployStudio.Api.Hubs;
using SafeDeployStudio.Infrastructure;
using SafeDeployStudio.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// Add API controllers & SignalR
builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "SafeDeploy Studio API", Version = "v1" });
});

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Register SafeDeploy Studio Core services (EF DbContext, Providers, Guardrails, AI, Vault, Diff)
builder.Services.AddSafeDeployStudioCore(builder.Configuration);

var app = builder.Build();

// Seed initial database
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SafeDeployDbContext>();
    await DatabaseSeeder.SeedAsync(db);
}

// HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "SafeDeploy Studio API v1"));
}

app.UseCors("AllowAll");
app.UseRouting();
app.MapControllers();
app.MapHub<DeploymentHub>("/hubs/deployment");

app.Run();
