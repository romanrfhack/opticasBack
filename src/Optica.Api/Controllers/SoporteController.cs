using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Optica.Domain.Dtos;
using Optica.Domain.Entities;
using Optica.Infrastructure.Persistence;

using System.Security.Claims;

namespace Optica.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SoporteController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly ILogger<SoporteController> _logger;

        public SoporteController(AppDbContext db, ILogger<SoporteController> logger)
        {
            _db = db; _logger = logger;
        }

        // POST /api/soporte
        [HttpPost]
        [Authorize] // si quieres permitir anónimo, usa [AllowAnonymous]
        public async Task<ActionResult<object>> Crear([FromBody] SupportCreateRequest req)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Guid? userId = Guid.TryParse(userIdStr, out var id) ? id : null;

            var email = string.IsNullOrWhiteSpace(req.Email)
                ? (User.FindFirstValue(ClaimTypes.Email) ?? "desconocido@local")
                : req.Email;

            var t = new SupportTicket
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Email = email!,
                Asunto = req.Asunto,
                Mensaje = req.Mensaje,
                CreatedAt = DateTime.UtcNow,
                Estado = "Abierto"
            };

            _db.SupportTickets.Add(t);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Nuevo ticket {Id} de {Email}: {Asunto}", t.Id, t.Email, t.Asunto);

            return Ok(new { folio = t.Id, createdAt = t.CreatedAt });
        }

        // GET /api/soporte (admin)
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<SupportTicket>>> Listar(int take = 100)
        {
            var list = await _db.SupportTickets
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedAt)
                .Take(take)
                .ToListAsync();
            return Ok(list);
        }
    }
}
