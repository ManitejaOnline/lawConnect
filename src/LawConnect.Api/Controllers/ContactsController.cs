using LawConnect.Api.Extensions;
using LawConnect.Application.Features.Contacts.Models;
using LawConnect.Application.Features.Contacts.Queries.GetCallContact;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LawConnect.Api.Controllers;

/// <summary>Authorized contact numbers for the native phone dialer.</summary>
[ApiController]
[Authorize]
[Route("api/contacts")]
public sealed class ContactsController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>Initializes a new instance of the <see cref="ContactsController"/> class.</summary>
    public ContactsController(ISender sender) => _sender = sender;

    /// <summary>
    /// Returns a phone number when an authenticated customer calls a lawyer, or when the parties share
    /// an assigned emergency or a non-cancelled appointment. Never used by public lawyer search.
    /// </summary>
    [HttpGet("{userId:guid}")]
    [ProducesResponseType(typeof(CallContactResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCallContact(Guid userId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetCallContactQuery(userId), cancellationToken);
        return result.ToActionResult();
    }
}
