using Contracts;
using MassTransit;
using MongoDB.Entities;
using SearchService.Models;

namespace SearchService.Consumers;
public class AuctionFinishedConsumer : IConsumer<AuctionFinished>
{
    private readonly SearchDbContext _context;
    private readonly IConfiguration _config;

    public AuctionFinishedConsumer(SearchDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }
    public async Task Consume(ConsumeContext<AuctionFinished> context)
    {
        Console.WriteLine("--> Consuming Auction Finished");

        if (bool.Parse(_config["UseMongoDb"]))
        {
            var auction = await DB.Find<Item>().OneAsync(context.Message.AuctionId);

            if (context.Message.ItemSold)
            {
                auction.Winner = context.Message.Winner;
                auction.SoldAmount = (int)context.Message.Amount;
            }

            auction.Status = "Finished";

            await auction.SaveAsync();
        }
        else
        {
            var auction = await _context.Items.FindAsync(context.Message.AuctionId);

            if (context.Message.ItemSold)
            {
                auction.Winner = context.Message.Winner;
                auction.SoldAmount = (int)context.Message.Amount;
            }

            auction.Status = "Finished";

            await _context.SaveChangesAsync();
        }
    }
}
