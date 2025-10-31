using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Optica.Domain.Entities;
using Optica.Domain.Enums;
using Optica.Infrastructure.Persistence;

using System.ComponentModel;
using System.Globalization;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _context;

    public DashboardController(AppDbContext context)
    {
        _context = context;
    }

    // -------------------- 1️⃣ KPI PRINCIPALES --------------------
    [HttpGet("kpis")]
    public async Task<ActionResult<DashboardKpisResponse>> GetKpis(
        [FromQuery] string period = "week",
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string branchId = "all")
    {
        var (currentStart, currentEnd, previousStart, previousEnd) = GetDateRange(period, startDate, endDate);
        var kpis = new DashboardKpisResponse();

        // Pacientes atendidos
        var currentVisitas = await GetVisitasQuery(branchId, currentStart, currentEnd).CountAsync();
        var previousVisitas = await GetVisitasQuery(branchId, previousStart, previousEnd).CountAsync();
        kpis.PatientsAttended = new KpiData
        {
            Value = currentVisitas,
            Change = CalculatePercentageChange(currentVisitas, previousVisitas)
        };

        // Nuevos pacientes
        var currentNuevos = await GetPacientesQuery(branchId, currentStart, currentEnd).CountAsync();
        var previousNuevos = await GetPacientesQuery(branchId, previousStart, previousEnd).CountAsync();
        kpis.NewPatients = new KpiData
        {
            Value = currentNuevos,
            Change = CalculatePercentageChange(currentNuevos, previousNuevos)
        };

        // Órdenes cobradas
        var currentCobradas = await GetVisitasWithPagosQuery(branchId, currentStart, currentEnd).CountAsync();
        var previousCobradas = await GetVisitasWithPagosQuery(branchId, previousStart, previousEnd).CountAsync();
        kpis.OrdersPaid = new KpiData
        {
            Value = currentCobradas,
            Change = CalculatePercentageChange(currentCobradas, previousCobradas)
        };

        // Ingresos totales
        var currentIngresos = await GetPagosQuery(branchId, currentStart, currentEnd).SumAsync(p => (decimal?)p.Monto) ?? 0;
        var previousIngresos = await GetPagosQuery(branchId, previousStart, previousEnd).SumAsync(p => (decimal?)p.Monto) ?? 0;
        kpis.TotalIncome = new KpiData
        {
            Value = currentIngresos,
            Change = CalculatePercentageChange(currentIngresos, previousIngresos)
        };

        // Enviadas a laboratorio
        var currentLab = await GetVisitasByStatusQuery(branchId, 5, currentStart, currentEnd).CountAsync();
        var previousLab = await GetVisitasByStatusQuery(branchId, 5, previousStart, previousEnd).CountAsync();
        kpis.SentToLab = new KpiData
        {
            Value = currentLab,
            Change = CalculatePercentageChange(currentLab, previousLab)
        };

        // Entregadas a clientes
        var currentEntregadas = await GetVisitasByStatusQuery(branchId, 8, currentStart, currentEnd).CountAsync();
        var previousEntregadas = await GetVisitasByStatusQuery(branchId, 8, previousStart, previousEnd).CountAsync();
        kpis.DeliveredToCustomers = new KpiData
        {
            Value = currentEntregadas,
            Change = CalculatePercentageChange(currentEntregadas, previousEntregadas)
        };

        return Ok(kpis);
    }

    // -------------------- 2️⃣ PACIENTES ATENDIDOS --------------------
    [HttpGet("patient-attendance")]
    public async Task<ActionResult<PatientAttendanceResponse>> GetPatientAttendance(
        [FromQuery] string period = "week",
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string branchId = "all")
    {
        var (currentStart, currentEnd, _, _) = GetDateRange(period, startDate, endDate);

        // Obtener visitas dentro del rango
        var query = _context.Visitas
            .AsNoTracking()
            .Where(v => v.Fecha >= currentStart && v.Fecha <= currentEnd);

        if (branchId != "all" && Guid.TryParse(branchId, out var branchGuid))
            query = query.Where(v => v.SucursalId == branchGuid);

        var visitas = await query
            .Select(v => new { v.Fecha, v.PacienteId })
            .ToListAsync();

        // Cálculo de nuevos pacientes con precarga
        var primeros = await _context.Visitas
            .AsNoTracking()
            .GroupBy(v => v.PacienteId)
            .Select(g => new { PacienteId = g.Key, Primera = g.Min(v => v.Fecha) })
            .ToListAsync();

        var agrupado = visitas
            .GroupBy(v => v.Fecha.Date)
            .Select(g => new
            {
                Date = g.Key,
                Total = g.Count(),
                NewPatients = g
                    .Select(v => v.PacienteId)
                    .Distinct()
                    .Count(pid => primeros.Any(p => p.PacienteId == pid && p.Primera.Date == g.Key))
            })
            .OrderBy(x => x.Date)
            .ToList();

        var labels = GenerateDateLabels(currentStart, currentEnd, period);
        var totalData = new List<int>();
        var newPatientsData = new List<int>();

        foreach (var label in labels)
        {
            var item = agrupado.FirstOrDefault(v => v.Date == label);
            totalData.Add(item?.Total ?? 0);
            newPatientsData.Add(item?.NewPatients ?? 0);
        }

        return Ok(new PatientAttendanceResponse
        {
            Labels = labels.Select(d => period switch
            {
                "day" => d.ToString("HH:mm"),  // horas si el periodo es diario
                "week" => d.ToString("dd MMM"), // días del mes si es semanal
                "month" => $"Sem {System.Globalization.CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(d, CalendarWeekRule.FirstDay, DayOfWeek.Monday)}", // semanas
                "year" => d.ToString("MMM"),   // meses si es anual
                _ => d.ToString("MMM dd")
            }).ToArray(),
            TotalPatients = totalData.ToArray(),
            NewPatients = newPatientsData.ToArray()
        });
    }

    // -------------------- 3️⃣ MÉTODOS DE PAGO --------------------
    [HttpGet("payment-methods")]
    public async Task<ActionResult<PaymentMethodsResponse>> GetPaymentMethods(
        [FromQuery] string period = "week",
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string branchId = "all")
    {
        var (currentStart, currentEnd, _, _) = GetDateRange(period, startDate, endDate);

        var pagos = await GetPagosQuery(branchId, currentStart, currentEnd)
            .GroupBy(p => p.Metodo)
            .Select(g => new { Metodo = g.Key, Total = g.Sum(p => p.Monto) })
            .ToListAsync();

        var total = pagos.Sum(p => p.Total);
        var methods = new Dictionary<int, string>
        {
            { 0, "Efectivo" },
            { 1, "Tarjeta" },
            { 2, "Transferencia" }
        };

        // Evitar división por cero
        return Ok(new PaymentMethodsResponse
        {
            Labels = methods.Values.ToArray(),
            Data = methods.Keys.Select(m =>
                total > 0 ? (int)((pagos.FirstOrDefault(p => (int)p.Metodo == m)?.Total ?? 0) / total * 100) : 0
            ).ToArray(),
            Amounts = methods.Keys.Select(m =>
                pagos.FirstOrDefault(p => (int)p.Metodo == m)?.Total ?? 0
            ).ToArray()
        });
    }

    // -------------------- 4️⃣ ESTADOS DE ÓRDENES --------------------
    [HttpGet("order-status")]
    public async Task<ActionResult<OrderStatusResponse>> GetOrderStatus(
        [FromQuery] string branchId = "all")
    {
        var estados = new Dictionary<int, string>
        {
            { 0, "Creada" },
            { 1, "Registrada" },
            { 2, "Enviada a laboratorio" },
            { 3, "Lista en laboratorio" },
            { 4, "Recibida en sucursal" },
            { 5, "Lista para entrega" },
            { 6, "Entregada al cliente" },
            { 7, "Cancelada" }
        };

        var counts = await GetVisitasQuery(branchId, null, null)
            .GroupBy(v => v.Estado)
            .Select(g => new { Estado = (int)g.Key, Count = g.Count() })
            .ToListAsync();

        return Ok(new OrderStatusResponse
        {
            Labels = estados.Values.ToArray(),
            Data = estados.Keys.Select(e => counts.FirstOrDefault(c => c.Estado == e)?.Count ?? 0).ToArray()
        });
    }

    // -------------------- 5️⃣ VENTAS POR CATEGORÍA --------------------
    [HttpGet("sales-by-category")]
    public async Task<ActionResult<SalesByCategoryResponse>> GetSalesByCategory(
        [FromQuery] string period = "month",
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string branchId = "all")
    {
        var (currentStart, currentEnd, _, _) = GetDateRange(period, startDate, endDate);

        var query = _context.Visitas
            .AsNoTracking()
            .Include(v => v.Conceptos)
            .Where(v => v.Fecha >= currentStart && v.Fecha <= currentEnd);

        if (branchId != "all" && Guid.TryParse(branchId, out var branchGuid))
            query = query.Where(v => v.SucursalId == branchGuid);

        var ventas = await query
            .SelectMany(v => v.Conceptos)
            .GroupBy(c => c.Concepto)
            .Select(g => new { Category = g.Key, Total = g.Sum(c => c.Monto) })
            .ToListAsync();

        var total = ventas.Sum(v => v.Total);
        return Ok(new SalesByCategoryResponse
        {
            Labels = ventas.Select(v => v.Category).ToArray(),
            Data = ventas.Select(v => total > 0 ? (int)(v.Total / total * 100) : 0).ToArray(),
            Amounts = ventas.Select(v => v.Total).ToArray()
        });
    }

    // -------------------- 6️⃣ INGRESOS MENSUALES --------------------
    [HttpGet("monthly-revenue")]
    public async Task<ActionResult<MonthlyRevenueResponse>> GetMonthlyRevenue(
        [FromQuery] string branchId = "all")
    {
        var year = DateTime.Today.Year;
        var prevYear = year - 1;

        var current = await GetPagosQuery(branchId, new DateTime(year, 1, 1), new DateTime(year, 12, 31))
            .GroupBy(p => p.Fecha.Month)
            .Select(g => new { g.Key, Total = g.Sum(p => p.Monto) })
            .ToListAsync();

        var previous = await GetPagosQuery(branchId, new DateTime(prevYear, 1, 1), new DateTime(prevYear, 12, 31))
            .GroupBy(p => p.Fecha.Month)
            .Select(g => new { g.Key, Total = g.Sum(p => p.Monto) })
            .ToListAsync();

        var months = new[] { "Ene", "Feb", "Mar", "Abr", "May", "Jun", "Jul", "Ago", "Sep", "Oct", "Nov", "Dic" };
        var currentData = new decimal[12];
        var previousData = new decimal[12];

        for (int i = 0; i < 12; i++)
        {
            currentData[i] = current.FirstOrDefault(r => r.Key == i + 1)?.Total ?? 0;
            previousData[i] = previous.FirstOrDefault(r => r.Key == i + 1)?.Total ?? 0;
        }

        return Ok(new MonthlyRevenueResponse
        {
            Labels = months,
            CurrentYear = currentData,
            PreviousYear = previousData
        });
    }

    // -------------------- AUXILIARES --------------------
    private IQueryable<HistoriaClinicaVisita> GetVisitasQuery(string branchId, DateTime? start, DateTime? end)
    {
        var q = _context.Visitas.AsNoTracking().AsQueryable();
        if (branchId != "all" && Guid.TryParse(branchId, out var id)) q = q.Where(v => v.SucursalId == id);
        if (start.HasValue) q = q.Where(v => v.Fecha >= start.Value);
        if (end.HasValue) q = q.Where(v => v.Fecha <= end.Value);
        return q;
    }

    private IQueryable<Paciente> GetPacientesQuery(string branchId, DateTime? start, DateTime? end)
    {
        var q = _context.Pacientes.AsNoTracking().AsQueryable();
        if (branchId != "all" && Guid.TryParse(branchId, out var id)) q = q.Where(p => p.SucursalIdAlta == id);
        if (start.HasValue) q = q.Where(p => p.FechaRegistro >= start.Value);
        if (end.HasValue) q = q.Where(p => p.FechaRegistro <= end.Value);
        return q;
    }

    private IQueryable<HistoriaPago> GetPagosQuery(string branchId, DateTime? start, DateTime? end)
    {
        IQueryable<HistoriaPago> q = _context.HistoriaPagos.AsNoTracking();

        q = q.Include(hp => hp.Visita);

        if (branchId != "all" && Guid.TryParse(branchId, out var id))
            q = q.Where(hp => hp.Visita.SucursalId == id);

        if (start.HasValue)
            q = q.Where(hp => hp.Fecha >= start.Value);

        if (end.HasValue)
            q = q.Where(hp => hp.Fecha <= end.Value);

        return q;
    }


    private IQueryable<HistoriaClinicaVisita> GetVisitasWithPagosQuery(string branchId, DateTime? start, DateTime? end)
        => GetVisitasQuery(branchId, start, end).Where(v => v.Pagos.Any());

    private IQueryable<HistoriaClinicaVisita> GetVisitasByStatusQuery(string branchId, int status, DateTime? start, DateTime? end)
        => GetVisitasQuery(branchId, start, end).Where(v => (int)v.Estado == status);

    private (DateTime, DateTime, DateTime, DateTime) GetDateRange(string period, DateTime? customStart, DateTime? customEnd)
    {
        DateTime today = DateTime.Today;
        DateTime cs, ce, ps, pe;

        switch (period)
        {
            case "day":
                cs = today; ce = today.AddDays(1).AddSeconds(-1);
                ps = cs.AddDays(-1); pe = cs.AddSeconds(-1); break;
            case "week":
                var sow = today.AddDays(-(int)today.DayOfWeek + (int)DayOfWeek.Monday);
                cs = sow; ce = sow.AddDays(7).AddSeconds(-1);
                ps = sow.AddDays(-7); pe = sow.AddSeconds(-1); break;
            case "month":
                cs = new DateTime(today.Year, today.Month, 1);
                ce = cs.AddMonths(1).AddSeconds(-1);
                ps = cs.AddMonths(-1); pe = cs.AddSeconds(-1); break;
            case "year":
                cs = new DateTime(today.Year, 1, 1);
                ce = cs.AddYears(1).AddSeconds(-1);
                ps = cs.AddYears(-1); pe = cs.AddSeconds(-1); break;
            case "custom":
                cs = customStart ?? today;
                ce = customEnd ?? today.AddDays(1).AddSeconds(-1);
                int diff = (ce - cs).Days;
                ps = cs.AddDays(-diff - 1);
                pe = cs.AddSeconds(-1); break;
            default:
                throw new ArgumentException("Período no válido");
        }
        return (cs, ce, ps, pe);
    }

    private decimal CalculatePercentageChange(decimal current, decimal previous)
        => previous == 0 ? (current > 0 ? 100 : 0) : ((current - previous) / previous) * 100;

    private List<DateTime> GenerateDateLabels(DateTime start, DateTime end, string period)
    {
        var labels = new List<DateTime>();
        var current = start;

        while (current <= end)
        {
            labels.Add(current);
            current = period switch
            {
                "day" => current.AddHours(6),       // Cada 6 horas
                "week" => current.AddDays(1),       // Cada día
                "month" => current.AddDays(7),      // Cada semana
                "year" => current.AddMonths(1),     // Cada mes
                _ => current.AddDays(1)
            };
        }

        return labels;
    }

}

// Modelos de respuesta
public class DashboardKpisResponse
{
    public KpiData PatientsAttended { get; set; } = new();
    public KpiData NewPatients { get; set; } = new();
    public KpiData OrdersPaid { get; set; } = new();
    public KpiData TotalIncome { get; set; } = new();
    public KpiData SentToLab { get; set; } = new();
    public KpiData DeliveredToCustomers { get; set; } = new();
}

public class KpiData
{
    public decimal Value { get; set; }
    public decimal Change { get; set; }
}

public class PatientAttendanceResponse
{
    public string[] Labels { get; set; } = Array.Empty<string>();
    public int[] TotalPatients { get; set; } = Array.Empty<int>();
    public int[] NewPatients { get; set; } = Array.Empty<int>();
}

public class PaymentMethodsResponse
{
    public string[] Labels { get; set; } = Array.Empty<string>();
    public int[] Data { get; set; } = Array.Empty<int>();
    public decimal[] Amounts { get; set; } = Array.Empty<decimal>();
}

public class OrderStatusResponse
{
    public string[] Labels { get; set; } = Array.Empty<string>();
    public int[] Data { get; set; } = Array.Empty<int>();
}

public class SalesByCategoryResponse
{
    public string[] Labels { get; set; } = Array.Empty<string>();
    public int[] Data { get; set; } = Array.Empty<int>();
    public decimal[] Amounts { get; set; } = Array.Empty<decimal>();
}

public class MonthlyRevenueResponse
{
    public string[] Labels { get; set; } = Array.Empty<string>();
    public decimal[] CurrentYear { get; set; } = Array.Empty<decimal>();
    public decimal[] PreviousYear { get; set; } = Array.Empty<decimal>();
}