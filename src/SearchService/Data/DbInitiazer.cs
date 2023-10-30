using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using MongoDB.Entities;
using SearchService.Models;
using SearchService.Services;

namespace SearchService.Data;
public class DbInitiazer
{
    public static async Task InitDbPostgress(WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        await SeedDataAsync(scope.ServiceProvider.GetService<SearchDbContext>(), app);
    }

    private static async Task SeedDataAsync(SearchDbContext context, WebApplication app)
    {
        context.Database.Migrate();

        if (context.Items.Any())
        {
            Console.WriteLine("Already Have Data");
            return;
        }

        using var scope = app.Services.CreateScope();

        var httpClient = scope.ServiceProvider.GetRequiredService<AuctionServiceHttpClient>();

        var items = await httpClient.GetItemsForSearchDbPostgres();

        Console.WriteLine("############## \n" + items.Count);

        context.AddRange(items);

        context.SaveChanges();
    }

    public static async Task InitDb(WebApplication app)
    {
        await DB.InitAsync("SearchDb", MongoClientSettings.FromConnectionString(app.Configuration.GetConnectionString("MongoDbConnection")));

        await DB.Index<Item>()
            .Key(x => x.Make, KeyType.Text)
            .Key(x => x.Model, KeyType.Text)
            .Key(x => x.Color, KeyType.Text)
            .CreateAsync();

        var count = await DB.CountAsync<Item>();

        using var scope = app.Services.CreateScope();

        var httpClient = scope.ServiceProvider.GetRequiredService<AuctionServiceHttpClient>();

        var items = await httpClient.GetItemsForSearchDb();

        Console.WriteLine(items.Count + " return from Auction Service");

        if (items.Count > 0) await DB.SaveAsync(items);
    }
}
