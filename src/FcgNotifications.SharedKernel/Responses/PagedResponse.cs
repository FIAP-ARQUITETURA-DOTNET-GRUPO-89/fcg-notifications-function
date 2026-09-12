namespace FcgNotifications.SharedKernel.Responses;

/// <summary>
/// Envelope universal para respostas paginadas da API.
/// </summary>
/// <typeparam name="T">O tipo do DTO/Response que está sendo listado</typeparam>
public record PagedResponse<T>
{
    public IEnumerable<T> Items { get; init; }
    public int TotalCount { get; init; }
    public int CurrentPage { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;

    public PagedResponse(IEnumerable<T> items, int totalCount, int currentPage, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        CurrentPage = currentPage;
        PageSize = pageSize;
    }
}
