using LibraAnalyse.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add services to the container.
builder.Services.AddRazorPages();

// Attempt to read the ClickHouse connection string
string clickHouseConnectionString;
try
{
    clickHouseConnectionString = File.ReadAllText("C:\\credentials.txt");
    if (string.IsNullOrWhiteSpace(clickHouseConnectionString))
    {
        throw new Exception("Connection string is empty.");
    }
}
catch (Exception ex)
{
    var loggerFactory = LoggerFactory.Create(loggingBuilder =>
    {
        loggingBuilder.AddConsole();
    });
    var logger = loggerFactory.CreateLogger("Startup");
    logger.LogCritical(ex, "Failed to read ClickHouse connection string from file.");
    throw; // Terminate the application if the connection string can't be read
}

// Register ClickHouseService with dependency injection
builder.Services.AddSingleton<ClickHouseService>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<ClickHouseService>>();
    return new ClickHouseService(clickHouseConnectionString, logger);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

// Map API controllers if you have any
app.MapControllers();

app.Run();
