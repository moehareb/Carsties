using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MongoDB.Entities;
using SearchService.Models;
using SearchService.RequestHelpers;

namespace SearchService.Controllers;

[ApiController]
[Route("api/search")]
public class SearchController : ControllerBase
{
    private readonly SearchDbContext _dbContext;
    private readonly IConfiguration _config;

    public SearchController(SearchDbContext dbContext, IConfiguration config)
    {
        _dbContext = dbContext;
        _config = config;
    }
    [HttpGet]
    public async Task<ActionResult<List<Item>>> SearchItems([FromQuery] SearchParams searchParams)
    {
        if (bool.Parse(_config["UseMongoDb"]))
        {
            Console.WriteLine("###################################################\n\n\nConsuming Mongo Db\n\n\n###################################################");
            var query = DB.PagedSearch<Item, Item>();

            if (!string.IsNullOrEmpty(searchParams.SearchTerm))
            {
                query.Match(Search.Full, searchParams.SearchTerm).SortByTextScore();
            }

            query = searchParams.OrderBy switch
            {
                "make" => query.Sort(x => x.Ascending(a => a.Make)),
                "new" => query.Sort(x => x.Descending(a => a.CreatedAt)),
                _ => query.Sort(x => x.Ascending(a => a.AuctionEnd))
            };

            query = searchParams.FilterBy switch
            {
                "finished" => query.Match(x => x.AuctionEnd < DateTime.UtcNow),
                "endingsoon" => query.Match(x => x.AuctionEnd < DateTime.UtcNow.AddHours(6) && x.AuctionEnd > DateTime.UtcNow),

                _ => query.Match(x => x.AuctionEnd > DateTime.UtcNow)
            };

            if (!string.IsNullOrEmpty(searchParams.Seller))
            {
                query.Match(x => x.Seller == searchParams.Seller);
            }

            if (!string.IsNullOrEmpty(searchParams.Seller))
            {
                query.Match(x => x.Winner == searchParams.Winner);
            }

            query.PageNumber(searchParams.PageNumber);
            query.PageSize(searchParams.PageSize);

            var result = await query.ExecuteAsync();

            return Ok(new
            {
                results = result.Results,
                pageCount = result.PageCount,
                totalCount = result.TotalCount
            });
        }
        else
        {
            Console.WriteLine("###################################################\n\n\nConsuming Postgres Db\n\n\n###################################################");

            var query = _dbContext.Items.AsQueryable();

            if (!string.IsNullOrEmpty(searchParams.SearchTerm))
            {
                // query = query.Where(x => EF.Functions.ILike(x.Make, $"%{searchParams.SearchTerm}%"));
                query = query.Where(x => x.Make.Equals(searchParams.SearchTerm));
            }

            query = searchParams.OrderBy switch
            {
                "make" => query.OrderBy(x => x.Make),
                "new" => query.OrderByDescending(x => x.CreatedAt),
                _ => query.OrderBy(x => x.AuctionEnd)
            };

            query = searchParams.FilterBy switch
            {
                "finished" => query.Where(x => x.AuctionEnd < DateTime.UtcNow),
                "endingsoon" => query.Where(x => x.AuctionEnd < DateTime.UtcNow.AddHours(6) && x.AuctionEnd > DateTime.UtcNow),
                _ => query.Where(x => x.AuctionEnd > DateTime.UtcNow)
            };

            if (!string.IsNullOrEmpty(searchParams.Seller))
            {
                query = query.Where(x => x.Seller == searchParams.Seller);
            }

            if (!string.IsNullOrEmpty(searchParams.Winner))
            {
                query = query.Where(x => x.Winner == searchParams.Winner);
            }

            var totalCount = await query.CountAsync();
            var results = await query.Skip((searchParams.PageNumber - 1) * searchParams.PageSize)
                                   .Take(searchParams.PageSize)
                                   .ToListAsync();

            return Ok(new
            {
                results,
                pageCount = (int)Math.Ceiling((double)totalCount / searchParams.PageSize),
                totalCount
            });
        }
    }
}
