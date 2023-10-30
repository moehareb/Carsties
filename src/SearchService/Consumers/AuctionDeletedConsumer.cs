using Contracts;
using MassTransit;
using MongoDB.Entities;
using SearchService.Models;

namespace SearchService.Consumers;
public class AuctionDeletedConsumer : IConsumer<AuctionDeleted>
{
    private readonly SearchDbContext _context;
    private readonly IConfiguration _config;

    public AuctionDeletedConsumer(SearchDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }
    public async Task Consume(ConsumeContext<AuctionDeleted> context)
    {
        Console.WriteLine("---> Consuming Auction Deleted: " + context.Message.Id);

        if (bool.Parse(_config["UseMongoDb"]))
        {
            var result = await DB.DeleteAsync<Item>(context.Message.Id);

            if (!result.IsAcknowledged)
                throw new MessageException(typeof(AuctionDeleted), "Problem deleting auction");
        }
        else
        {
            var item = await _context.Items.FindAsync(context.Message.Id);

            if (item == null) throw new MessageException(typeof(AuctionDeleted), "Problem deleting auction");

            _context.Items.Remove(item);

            var result = await _context.SaveChangesAsync() > 0;

            if (!result) throw new MessageException(typeof(AuctionDeleted), "Problem deleting auction");
        }

    }

}
