using Microsoft.AspNetCore.Http.HttpResults;
using Nuts.Application.Common;
using Nuts.Application.Media;
using Nuts.Domain.Entities;

namespace Nuts.Api.Endpoints;

public static class MediaEndpoints
{
    public static void MapMediaEndpoints(this IEndpointRouteBuilder app)
    {
        // Public
        app.MapGet("/api/media", async (IMediaMentionRepository repo, CancellationToken ct) =>
        {
            var mentions = await repo.GetVisibleAsync(ct);
            return TypedResults.Ok(mentions.Select(m => new MediaDto(m.Id, m.Source, m.Quote, m.Url, m.LogoPath)));
        }).WithTags("Media");

        // Admin
        var adminGroup = app.MapGroup("/api/admin/media")
            .WithTags("Admin — Media")
            .RequireAuthorization("Admin");

        adminGroup.MapGet("/", async (IMediaMentionRepository repo, CancellationToken ct) =>
        {
            var mentions = await repo.GetAllAsync(ct);
            return TypedResults.Ok(mentions.Select(m => new AdminMediaDto(
                m.Id, m.Source, m.Quote, m.Url, m.LogoPath, m.IsVisible, m.SortOrder, m.CreatedAt)));
        });

        adminGroup.MapPost("/", async Task<Results<Created<AdminMediaDto>, BadRequest<ProblemHttpResult>>> (
            CreateMediaRequest req, IMediaMentionRepository repo, IUnitOfWork uow, CancellationToken ct) =>
        {
            var result = MediaMention.Create(req.Source, req.Quote, req.Url);
            return await result.Match<Task<Results<Created<AdminMediaDto>, BadRequest<ProblemHttpResult>>>>(
                async mention =>
                {
                    repo.Add(mention);
                    await uow.SaveChangesAsync(ct);
                    var dto = new AdminMediaDto(mention.Id, mention.Source, mention.Quote,
                        mention.Url, mention.LogoPath, mention.IsVisible, mention.SortOrder, mention.CreatedAt);
                    return TypedResults.Created($"/api/admin/media/{mention.Id}", dto);
                },
                error => Task.FromResult<Results<Created<AdminMediaDto>, BadRequest<ProblemHttpResult>>>(
                    TypedResults.BadRequest(TypedResults.Problem(error.Message, statusCode: 400))));
        });

        adminGroup.MapPut("/{id:guid}", async Task<Results<Ok, NotFound, BadRequest<ProblemHttpResult>>> (
            Guid id, UpdateMediaRequest req, IMediaMentionRepository repo, IUnitOfWork uow, CancellationToken ct) =>
        {
            var mention = await repo.GetByIdAsync(id, ct);
            if (mention is null) return TypedResults.NotFound();

            var updateResult = mention.Update(req.Source, req.Quote, req.Url, req.IsVisible, req.SortOrder);
            if (updateResult.IsFailure)
                return TypedResults.BadRequest(TypedResults.Problem(updateResult.Error.Message, statusCode: 400));

            await uow.SaveChangesAsync(ct);
            return TypedResults.Ok();
        });

        adminGroup.MapDelete("/{id:guid}", async Task<Results<NoContent, NotFound>> (
            Guid id, IMediaMentionRepository repo, IUnitOfWork uow, CancellationToken ct) =>
        {
            var mention = await repo.GetByIdAsync(id, ct);
            if (mention is null) return TypedResults.NotFound();
            repo.Remove(mention);
            await uow.SaveChangesAsync(ct);
            return TypedResults.NoContent();
        });
    }
}

public sealed record MediaDto(Guid Id, string Source, string Quote, string? Url, string? LogoPath);

public sealed record AdminMediaDto(
    Guid Id, string Source, string Quote, string? Url, string? LogoPath,
    bool IsVisible, int SortOrder, DateTime CreatedAt);

public sealed record CreateMediaRequest
{
    public required string Source { get; init; }
    public required string Quote { get; init; }
    public string? Url { get; init; }
}

public sealed record UpdateMediaRequest
{
    public required string Source { get; init; }
    public required string Quote { get; init; }
    public string? Url { get; init; }
    public required bool IsVisible { get; init; }
    public required int SortOrder { get; init; }
}
