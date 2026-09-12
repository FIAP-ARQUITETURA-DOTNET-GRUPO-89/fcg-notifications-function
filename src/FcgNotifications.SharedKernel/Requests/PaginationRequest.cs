namespace FcgNotifications.SharedKernel.Requests;

public sealed record PaginationRequest(
    int Page = 1,
    int PageSize = 10
);
