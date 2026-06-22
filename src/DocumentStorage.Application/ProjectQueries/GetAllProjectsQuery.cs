namespace DocumentStorage.Application.ProjectQueries;

public record GetAllProjectsQuery(
    int Page = 1,
    int PageSize = 20
);
