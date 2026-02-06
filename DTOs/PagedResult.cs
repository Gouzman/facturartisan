namespace FacturArtisan.Api.DTOs;

public class PagedResult<T>
{
    public int Page { get; set; }
    public int PageSize { get; set; }

    public int TotalCount { get; set; }

    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);

    public List<T> Items { get; set; } = new();
}
