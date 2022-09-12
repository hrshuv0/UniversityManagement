using Microsoft.EntityFrameworkCore;
using UniversityManagement.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<SchoolContext>(options =>
{
    options.UseSqlServer(connectionString: connectionString);

    });


builder.Services.AddDatabaseDeveloperPageExceptionFilter();




var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();

}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

SeedData();

app.Run();


void SeedData()
{
    using var scope = app.Services.CreateScope();
    var service = scope.ServiceProvider;

    try
    {
        var context = service.GetRequiredService<SchoolContext>();
        DbInitializer.Initialize(context);
    }
    catch (Exception)
    {

    }
}