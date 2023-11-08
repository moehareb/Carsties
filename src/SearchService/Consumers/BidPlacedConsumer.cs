using Contracts;
using MassTransit;
using MongoDB.Entities;
using SearchService.Models;

namespace SearchService.Consumers;
public class BidPlacedConsumer : IConsumer<BidPlaced>
{
    private readonly SearchDbContext _context;
    private readonly IConfiguration _config;

    public BidPlacedConsumer(SearchDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }
    public async Task Consume(ConsumeContext<BidPlaced> context)
    {
        Console.WriteLine("--> Consuming Bid Placed");

        if (bool.Parse(_config["UseMongoDb"]))
        {
            var auction = await DB.Find<Item>().OneAsync(context.Message.AuctionId);

            if (context.Message.BidStatus.Contains("Accepted") && context.Message.Amount > auction.CurrentHighBid)
            {
                auction.CurrentHighBid = context.Message.Amount;
                await auction.SaveAsync();
            }
        }
        else
        {
            var auction = await _context.Items.FindAsync(context.Message.Id);
            if (context.Message.BidStatus.Contains("Accepted") && context.Message.Amount > auction.CurrentHighBid)
            {
                auction.CurrentHighBid = context.Message.Amount;
                await _context.SaveChangesAsync();
            }
        }
    }
}
