using AutoMapper;
using Contracts;
using MassTransit;
using MongoDB.Entities;
using SearchService.Models;

namespace SearchService.Consumers;
public class AuctionCreatedConsumer : IConsumer<AuctionCreated>
{
    private readonly IMapper _mapper;
    private readonly SearchDbContext _context;
    private readonly IConfiguration _config;

    public AuctionCreatedConsumer(IMapper mapper, SearchDbContext context, IConfiguration config)
    {
        _mapper = mapper;
        _context = context;
        _config = config;
    }
    public async Task Consume(ConsumeContext<AuctionCreated> context)
    {
        Console.WriteLine("---> Consuming Auction Created: " + context.Message.Id);

        var item = _mapper.Map<Item>(context.Message);

        if (item.Model == "Foo") throw new ArgumentException("Cannot Sell Foo Cars");
        if (bool.Parse(_config["UseMongoDb"]))
        {
            await item.SaveAsync();
        }
        else
        {
            Console.WriteLine("--->\n\n\n\n\n\n Adding Auction Created:\n\n\n\n\n " + context.Message.Id);

            _context.Items.Add(item);
            await _context.SaveChangesAsync();
        }
    }
}


