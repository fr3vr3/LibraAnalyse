using LibraAnalyse.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// Register ClickHouseService with dependency injection
var clickHouseConnectionString = Environment.GetEnvironmentVariable("CLICKHOUSE_CONNECTION_STRING");


builder.Services.AddSingleton(new ClickHouseService(clickHouseConnectionString));

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
