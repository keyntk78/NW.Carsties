using Microsoft.AspNetCore.Mvc;
using MongoDB.Entities;
using SearchService.Models;
using SearchService.RequestHelpers;

namespace SearchService.Controllers;

[ApiController]
[Route("api/search")]
public class SearchController : ControllerBase
{
    public async Task<ActionResult<List<Item>>> SearchItems([FromQuery] SearchParam searchParam)
    {
        var query = DB.PagedSearch<Item>(); // Không có tham số kiểu

        if (!string.IsNullOrEmpty(searchParam.SearchTerm))
        {
            query.Match(Search.Full, searchParam.SearchTerm).SortByTextScore();
        }

        switch (searchParam.OrderBy)
        {
            case "make":
                query.Sort(x => x.Ascending(a => a.Make));
                break;
            case "new":
                query.Sort(x => x.Descending(a => a.CreatedAt));
                break;
            default:
                query.Sort(x => x.Ascending(a => a.Make));
                break;
        }

        switch (searchParam.FilterBy)
        {
            case "finished":
                query.Match(x => x.AuctionEnd < DateTime.UtcNow);
                break;
            case "endingSoon":
                query.Match(x => x.AuctionEnd < DateTime.UtcNow.AddHours(6) && x.AuctionEnd > DateTime.UtcNow);
                break;
            default:
                query.Match(x => x.AuctionEnd > DateTime.UtcNow);
                break;
        }

        if (!string.IsNullOrEmpty(searchParam.Seller))
        {
            query.Match(x => x.Seller == searchParam.Seller);
        }

        if (!string.IsNullOrEmpty(searchParam.Winner))
        {
            query.Match(x => x.Winner == searchParam.Winner);
        }

        query.PageNumber(searchParam.PageNumber);
        query.PageSize(searchParam.PageSize);

        var result = await query.ExecuteAsync();

        return Ok(new
        {
            results = result.Results,
            pageCount = result.PageCount,
            totalCount = result.TotalCount
        });
    }

}
