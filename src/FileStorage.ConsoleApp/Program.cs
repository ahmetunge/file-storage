using FileStorage.ConsoleApp;
using FileStorage.ConsoleApp.Data;
using FileStorage.ConsoleApp.Providers;
using FileStorage.ConsoleApp.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

try
{
    var host = CreateHostBuilder(args).Build();
 
    Log.Information("App starting...");
    
    using var scope = host.Services.CreateScope();
    Log.Information("Migration applying...");
    var dbContext = scope.ServiceProvider.GetRequiredService<FileStorageDbContext>();
    await dbContext.Database.MigrateAsync();
    Log.Information("Migration completed!");

    var app = scope.ServiceProvider.GetRequiredService<ConsoleRunner>();
    await app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
static IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
        .UseSerilog()
        .ConfigureServices((context, services) =>
        {
            services.AddDbContext<FileStorageDbContext>(options =>
                options.UseSqlite("Data Source=../../../FileStorageDb.db"));

            services.AddTransient<IChunkingService, ChunkingService>();
            services.AddTransient<IFileProcessor, FileProcessor>();
            services.AddTransient<IStorageProvider, FileSystemStorageProvider>();
   
            services.AddTransient<ConsoleRunner>();
        });
        