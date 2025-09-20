using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Optica.Infrastructure.Persistence;

namespace Optica.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MaterialesController : ControllerBase
{
    private readonly AppDbContext _db;
    public record MaterialItemDto(Guid Id, string Descripcion, string? Marca);
    public MaterialesController(AppDbContext db) => _db = db;

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<IEnumerable<MaterialItemDto>>> Get()
    {
        var list = await _db.Materiales
            .AsNoTracking()
            .OrderBy(x => x.Descripcion)
            .Select(x => new MaterialItemDto(x.Id, x.Descripcion, x.Marca))
            .ToListAsync();

        return Ok(list);
    }
}
