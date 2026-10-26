using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace Finis.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class BaseApiController : ControllerBase
{
    protected int GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (claim == null || !int.TryParse(claim.Value, out int userId))
        {
            throw new UnauthorizedAccessException("Usuário não autenticado ou token inválido.");
        }
        return userId;
    }
}
