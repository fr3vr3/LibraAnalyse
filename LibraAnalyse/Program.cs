using LibraAnalyse.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// Register ClickHouseService with dependency injection
var clickHouseConnectionString = "Host=db.0l.fyi;Port=443;Username=ol_ro;Password=389c0705a2e2f17efc854cb02da03486;Database=olfyi;Secure=true;";
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
