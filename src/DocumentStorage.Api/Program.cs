using DocumentStorage.Api.Middleware;
using DocumentStorage.Application.Commands;
using DocumentStorage.Application.DTOs;
using DocumentStorage.Application.Interfaces;
using DocumentStorage.Application.ProjectCommands;
using DocumentStorage.Application.ProjectQueries;
using DocumentStorage.Application.Queries;
using DocumentStorage.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// ── Framework services ──
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// ── Infrastructure (DbContext, repositories, storage providers) ──
builder.Services.AddInfrastructure(builder.Configuration);

// ── CQRS handlers ──
builder.Services.AddScoped<ICommandHandler<InitUploadCommand, InitUploadResponse>, InitUploadCommandHandler>();
builder.Services.AddScoped<ICommandHandler<CompleteUploadCommand, FileDto>, CompleteUploadCommandHandler>();
builder.Services.AddScoped<ICommandHandler<DeleteFileCommand>, DeleteFileCommandHandler>();
builder.Services.AddScoped<ICommandHandler<UpdateDescriptionCommand, FileDto>, UpdateDescriptionCommandHandler>();
builder.Services.AddScoped<IQueryHandler<GetFileByIdQuery, FileDto>, GetFileByIdQueryHandler>();
builder.Services.AddScoped<IQueryHandler<SearchFilesQuery, PagedResult<FileDto>>, SearchFilesQueryHandler>();
builder.Services.AddScoped<IQueryHandler<GetFilesByUserQuery, PagedResult<FileDto>>, GetFilesByUserQueryHandler>();

// ── Project CQRS handlers ──
builder.Services.AddScoped<ICommandHandler<CreateProjectCommand, ProjectDto>, CreateProjectCommandHandler>();
builder.Services.AddScoped<ICommandHandler<UpdateProjectCommand, ProjectDto>, UpdateProjectCommandHandler>();
builder.Services.AddScoped<ICommandHandler<SetProjectActiveCommand>, SetProjectActiveCommandHandler>();
builder.Services.AddScoped<ICommandHandler<RegenerateApiKeyCommand, ProjectDto>, RegenerateApiKeyCommandHandler>();
builder.Services.AddScoped<IQueryHandler<GetProjectByIdQuery, ProjectDto>, GetProjectByIdQueryHandler>();
builder.Services.AddScoped<IQueryHandler<GetAllProjectsQuery, PagedResult<ProjectDto>>, GetAllProjectsQueryHandler>();

var app = builder.Build();

// ── Pipeline ──
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<ProjectResolutionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
