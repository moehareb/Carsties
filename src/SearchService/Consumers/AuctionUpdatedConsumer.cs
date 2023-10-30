using AutoMapper;
using Contracts;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using MongoDB.Entities;
using SearchService.Models;

namespace SearchService.Consumers;
public class AuctionUpdatedConsumer : IConsumer<AuctionUpdated>
{
    private readonly IMapper _mapper;
    private readonly IConfiguration _config;
    private readonly SearchDbContext _context;

    public AuctionUpdatedConsumer(IMapper mapper, IConfiguration config, SearchDbContext context)
    {
        _mapper = mapper;
        _config = config;
        _context = context;
    }
    public async Task Consume(ConsumeContext<AuctionUpdated> context)
    {
        Console.WriteLine("---> Consuming Auction Updated: " + context.Message.Id);

        var item = _mapper.Map<Item>(context.Message);

        Console.WriteLine(item.Color + "\n" + item.Make + "\n" + item.Model + "\n" + item.Year + "\n");

        if (bool.Parse(_config["UseMongoDb"]))
        {
            var result = await DB.Update<Item>()
                                    .Match(a => a.ID == context.Message.Id)
                                    .ModifyOnly(x => new
                                    {
                                        x.Color,
                                        x.Make,
                                        x.Model,
                                        x.Year
                                    }, item).ExecuteAsync();

            if (!result.IsAcknowledged)
                throw new MessageException(typeof(AuctionUpdated), "Problem updating mongodb");
        }
        else
        {
            var auction = await _context.Items.FirstOrDefaultAsync(x => x.ID == context.Message.Id);

            if (auction == null) throw new MessageException(typeof(AuctionUpdated), $"Item with Id: {context.Message.Id} not found");

            auction.Color = context.Message.Color;
            auction.Make = context.Message.Make;
            auction.Model = context.Message.Model;
            auction.Year = context.Message.Year;

            var result = await _context.SaveChangesAsync() > 0;

            if (!result) throw new MessageException(typeof(AuctionUpdated), "Problem updating postgresdb");
        }

    }
}
