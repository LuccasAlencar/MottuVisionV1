using System.Collections.Generic;
using MottuVision.Dtos;

namespace MottuVision.Dtos;
public class PagedResult<T>
{
    public required IEnumerable<T> Items { get; init; }
    public required int Page { get; init; }
    public required int PageSize { get; init; }
    public required long TotalCount { get; init; }
    public IEnumerable<Link> Links { get; init; } = [];
}