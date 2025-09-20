using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Optica.Infrastructure.Persistence;

namespace Optica.Api.Controllers;

public sealed record BranchDto(Guid Id, string Nombre);

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BranchesController : ControllerBase
{
    private readonly AppDbContext _db;
    public BranchesController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IEnumerable<BranchDto>> List() =>
        await _db.Sucursales.OrderBy(x => x.Nombre)
            .Select(x => new BranchDto(x.Id, x.Nombre))
            .ToListAsync();
}
