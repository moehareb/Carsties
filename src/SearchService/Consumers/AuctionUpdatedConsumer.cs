﻿using AutoMapper;
using Contracts;
using MassTransit;
using MongoDB.Entities;
using SearchService.Models;

namespace SearchService.Consumers;
public class AuctionUpdatedConsumer : IConsumer<AuctionUpdated>
{
    private readonly IMapper _mapper;

    public AuctionUpdatedConsumer(IMapper mapper)
    {
        _mapper = mapper;
    }
    public async Task Consume(ConsumeContext<AuctionUpdated> context)
    {
        Console.WriteLine("---> Consuming Auction Updated: " + context.Message.Id);

        var item = _mapper.Map<Item>(context.Message);

        Console.WriteLine(item.Color + "\n" + item.Make + "\n" + item.Model + "\n" + item.Year + "\n");

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
}