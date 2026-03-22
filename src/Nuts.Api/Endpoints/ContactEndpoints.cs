using Microsoft.AspNetCore.Http.HttpResults;
using Nuts.Application.Common;
using Nuts.Application.Contacts;
using Nuts.Domain.Entities;

namespace Nuts.Api.Endpoints;

public static class ContactEndpoints
{
    public static void MapContactEndpoints(this IEndpointRouteBuilder app)
    {
        // Public — submit contact form
        app.MapPost("/api/contacts", async Task<Results<Created, BadRequest<ProblemHttpResult>>> (
            SubmitContactRequest req, IContactRequestRepository repo, IUnitOfWork uow, CancellationToken ct) =>
        {
            var result = ContactRequest.Create(req.Name, req.Phone, req.Email, req.Message);
            return await result.Match<Task<Results<Created, BadRequest<ProblemHttpResult>>>>(
                async contact =>
                {
                    repo.Add(contact);
                    await uow.SaveChangesAsync(ct);
                    return TypedResults.Created($"/api/admin/contacts/{contact.Id}");
                },
                error => Task.FromResult<Results<Created, BadRequest<ProblemHttpResult>>>(
                    TypedResults.BadRequest(TypedResults.Problem(error.Message, statusCode: 400))));
        }).WithTags("Contacts");

        // Admin
        var adminGroup = app.MapGroup("/api/admin/contacts")
            .WithTags("Admin — Contacts")
            .RequireAuthorization("Admin");

        adminGroup.MapGet("/", async (IContactRequestRepository repo, CancellationToken ct) =>
        {
            var contacts = await repo.GetAllAsync(ct);
            return TypedResults.Ok(contacts.Select(c => new ContactDto(
                c.Id, c.Name, c.Phone, c.Email, c.Message, c.IsProcessed, c.CreatedAt)));
        });

        adminGroup.MapPost("/{id:guid}/process", async Task<Results<Ok, NotFound>> (
            Guid id, IContactRequestRepository repo, IUnitOfWork uow, CancellationToken ct) =>
        {
            var contact = await repo.GetByIdAsync(id, ct);
            if (contact is null) return TypedResults.NotFound();
            contact.MarkProcessed();
            await uow.SaveChangesAsync(ct);
            return TypedResults.Ok();
        });

        adminGroup.MapDelete("/{id:guid}", async Task<Results<NoContent, NotFound>> (
            Guid id, IContactRequestRepository repo, IUnitOfWork uow, CancellationToken ct) =>
        {
            var contact = await repo.GetByIdAsync(id, ct);
            if (contact is null) return TypedResults.NotFound();
            repo.Remove(contact);
            await uow.SaveChangesAsync(ct);
            return TypedResults.NoContent();
        });
    }
}

public sealed record SubmitContactRequest
{
    public required string Name { get; init; }
    public required string Phone { get; init; }
    public string? Email { get; init; }
    public string? Message { get; init; }
}

public sealed record ContactDto(
    Guid Id, string Name, string Phone, string? Email,
    string? Message, bool IsProcessed, DateTime CreatedAt);
