using System.Threading;
using System.Threading.Tasks;
using Conduit.Infrastructure;
using Conduit.Infrastructure.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Conduit.Features.Users;

[Route("user")]
[Authorize(AuthenticationSchemes = JwtIssuerOptions.Schemes)]
public class UserController(IMediator mediator, ICurrentUserAccessor currentUserAccessor)
{
    [HttpGet]
    public Task<UserEnvelope> GetCurrent(CancellationToken cancellationToken) =>
        mediator.Send(
            new Details.Query(currentUserAccessor.GetCurrentUsername() ?? "<unknown>"),
            cancellationToken
        );

    [HttpPut]
    public Task<UserEnvelope> UpdateUser(
        [FromBody] Edit.Command command,
        CancellationToken cancellationToken
    ) => mediator.Send(command, cancellationToken);
}
