using LawConnect.Application.Common.Models;
using MediatR;

namespace LawConnect.Application.Features.Lawyers.Queries.ListSpecializations;

/// <summary>
/// Lists active practice areas for registration and search filters.
/// </summary>
public sealed record ListSpecializationsQuery : IRequest<Result<IReadOnlyList<SpecializationOptionResponse>>>;

/// <summary>
/// Practice area option.
/// </summary>
public sealed record SpecializationOptionResponse(Guid Id, string Name, string Slug);
