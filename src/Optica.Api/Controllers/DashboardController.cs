// Controllers/DashboardController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Optica.Domain.Entities;
using Optica.Infrastructure.Persistence;

using System.ComponentModel;
using System.Globalization;
using Optica.Domain.Enums;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _context;

    public DashboardController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("kpis")]
    public async Task<ActionResult<DashboardKpisResponse>> GetKpis(
        [FromQuery] string period = "week",
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string branchId = "all")
    {
        var (currentStart, currentEnd, previousStart, previousEnd) = GetDateRange(period, startDate, endDate);

        var kpis = new DashboardKpisResponse();

        // Pacientes atendidos (visitas únicas)
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

        // Órdenes cobradas (visitas con pagos)
        var currentCobradas = await GetVisitasWithPagosQuery(branchId, currentStart, currentEnd).CountAsync();
        var previousCobradas = await GetVisitasWithPagosQuery(branchId, previousStart, previousEnd).CountAsync();
        kpis.OrdersPaid = new KpiData
        {
            Value = currentCobradas,
            Change = CalculatePercentageChange(currentCobradas, previousCobradas)
        };

        // Ingresos totales
        var currentIngresos = await GetPagosQuery(branchId, currentStart, currentEnd).SumAsync(p => p.Monto);
        var previousIngresos = await GetPagosQuery(branchId, previousStart, previousEnd).SumAsync(p => p.Monto);
        kpis.TotalIncome = new KpiData
        {
            Value = currentIngresos,
            Change = CalculatePercentageChange(currentIngresos, previousIngresos)
        };

        // Enviadas a laboratorio
        var currentLab = await GetVisitasByStatusQuery(branchId, 5, currentStart, currentEnd).CountAsync(); // Estado 5 = Enviada a laboratorio
        var previousLab = await GetVisitasByStatusQuery(branchId, 5, previousStart, previousEnd).CountAsync();
        kpis.SentToLab = new KpiData
        {
            Value = currentLab,
            Change = CalculatePercentageChange(currentLab, previousLab)
        };

        // Entregadas a clientes
        var currentEntregadas = await GetVisitasByStatusQuery(branchId, 8, currentStart, currentEnd).CountAsync(); // Estado 8 = Entregada
        var previousEntregadas = await GetVisitasByStatusQuery(branchId, 8, previousStart, previousEnd).CountAsync();
        kpis.DeliveredToCustomers = new KpiData
        {
            Value = currentEntregadas,
            Change = CalculatePercentageChange(currentEntregadas, previousEntregadas)
        };

        return kpis;
    }

    [HttpGet("patient-attendance")]
    public async Task<ActionResult<PatientAttendanceResponse>> GetPatientAttendance(
        [FromQuery] string period = "week",
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string branchId = "all")
    {
        var (currentStart, currentEnd, _, _) = GetDateRange(period, startDate, endDate);

        var visitas = await GetVisitasQuery(branchId, currentStart, currentEnd)
            .GroupBy(v => v.Fecha.Date)
            .Select(g => new
            {
                Date = g.Key,
                Total = g.Count(),
                NewPatients = g.Select(v => v.PacienteId).Distinct().Count(p =>
                    !_context.Visitas.Any(v2 => v2.PacienteId == p && v2.Fecha < g.Key))
            })
            .OrderBy(x => x.Date)
            .ToListAsync();

        var labels = GenerateDateLabels(currentStart, currentEnd, period);
        var totalData = new List<int>();
        var newPatientsData = new List<int>();

        foreach (var label in labels)
        {
            var visita = visitas.FirstOrDefault(v => v.Date == label);
            totalData.Add(visita?.Total ?? 0);
            newPatientsData.Add(visita?.NewPatients ?? 0);
        }

        return new PatientAttendanceResponse
        {
            Labels = labels.Select(d => d.ToString("MMM dd")).ToArray(),
            TotalPatients = totalData.ToArray(),
            NewPatients = newPatientsData.ToArray()
        };
    }

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
            .Select(g => new
            {
                Method = g.Key,
                Total = g.Sum(p => p.Monto),
                Count = g.Count()
            })
            .ToListAsync();

        var totalAmount = pagos.Sum(p => p.Total);
        var methods = new Dictionary<int, string>
        {
            { 0, "Efectivo" },
            { 1, "Tarjeta" },
            { 2, "Transferencia" }
        };

        return new PaymentMethodsResponse
        {
            Labels = methods.Values.ToArray(),
            Data = methods.Keys.Select(method =>
                (int)((pagos.FirstOrDefault(p => p.Method == (MetodoPago)method)?.Total ?? 0) / totalAmount * 100)
            ).ToArray(),
            Amounts = methods.Keys.Select(method =>
                pagos.FirstOrDefault(p => p.Method == (MetodoPago)method)?.Total ?? 0
            ).ToArray()
        };
    }

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
            .Select(g => new { Estado = g.Key, Count = g.Count() })
            .ToListAsync();

        return new OrderStatusResponse
        {
            Labels = estados.Values.ToArray(),
            Data = estados.Keys.Select(estado =>
                counts.FirstOrDefault(c => (int)c.Estado == estado)?.Count ?? 0
            ).ToArray()
        };
    }

    [HttpGet("sales-by-category")]
    public async Task<ActionResult<SalesByCategoryResponse>> GetSalesByCategory(
        [FromQuery] string period = "month",
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string branchId = "all")
    {
        var (currentStart, currentEnd, _, _) = GetDateRange(period, startDate, endDate);

        var ventas = await _context.Visitas
            .Where(v => v.Fecha >= currentStart && v.Fecha <= currentEnd)
            .Where(v => branchId == "all" || v.SucursalId.ToString() == branchId)
            .SelectMany(v => v.Conceptos)
            .GroupBy(c => c.Concepto)
            .Select(g => new
            {
                Category = g.Key,
                Total = g.Sum(c => c.Monto)
            })
            .ToListAsync();

        var total = ventas.Sum(v => v.Total);

        return new SalesByCategoryResponse
        {
            Labels = ventas.Select(v => v.Category).ToArray(),
            Data = ventas.Select(v => (int)(v.Total / total * 100)).ToArray(),
            Amounts = ventas.Select(v => v.Total).ToArray()
        };
    }

    [HttpGet("monthly-revenue")]
    public async Task<ActionResult<MonthlyRevenueResponse>> GetMonthlyRevenue(
        [FromQuery] string branchId = "all")
    {
        var currentYear = DateTime.Today.Year;
        var previousYear = currentYear - 1;

        var revenueCurrent = await GetPagosQuery(branchId, new DateTime(currentYear, 1, 1), new DateTime(currentYear, 12, 31))
            .GroupBy(p => p.Fecha.Month)
            .Select(g => new { Month = g.Key, Total = g.Sum(p => p.Monto) })
            .ToListAsync();

        var revenuePrevious = await GetPagosQuery(branchId, new DateTime(previousYear, 1, 1), new DateTime(previousYear, 12, 31))
            .GroupBy(p => p.Fecha.Month)
            .Select(g => new { Month = g.Key, Total = g.Sum(p => p.Monto) })
            .ToListAsync();

        var months = new[] { "Ene", "Feb", "Mar", "Abr", "May", "Jun", "Jul", "Ago", "Sep", "Oct", "Nov", "Dic" };
        var currentYearData = new decimal[12];
        var previousYearData = new decimal[12];

        for (int i = 0; i < 12; i++)
        {
            currentYearData[i] = revenueCurrent.FirstOrDefault(r => r.Month == i + 1)?.Total ?? 0;
            previousYearData[i] = revenuePrevious.FirstOrDefault(r => r.Month == i + 1)?.Total ?? 0;
        }

        return new MonthlyRevenueResponse
        {
            Labels = months,
            CurrentYear = currentYearData,
            PreviousYear = previousYearData
        };
    }

    // Métodos auxiliares
    private IQueryable<HistoriaClinicaVisita> GetVisitasQuery(string branchId, DateTime? startDate, DateTime? endDate)
    {
        var query = _context.Visitas.AsQueryable();

        if (branchId != "all")
            query = query.Where(v => v.SucursalId.ToString() == branchId);

        if (startDate.HasValue)
            query = query.Where(v => v.Fecha >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(v => v.Fecha <= endDate.Value);

        return query;
    }

    private IQueryable<Paciente> GetPacientesQuery(string branchId, DateTime? startDate, DateTime? endDate)
    {
        var query = _context.Pacientes.AsQueryable();

        if (branchId != "all")
            query = query.Where(p => p.SucursalIdAlta.ToString() == branchId);

        if (startDate.HasValue)
            query = query.Where(p => p.FechaRegistro >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(p => p.FechaRegistro <= endDate.Value);

        return query;
    }

    private IQueryable<HistoriaPago> GetPagosQuery(string branchId, DateTime? startDate, DateTime? endDate)
    {
        var query = _context.HistoriaPagos
            .Include(hp => hp.Visita)
            .AsQueryable();

        if (branchId != "all")
            query = query.Where(hp => hp.Visita.SucursalId.ToString() == branchId);

        if (startDate.HasValue)
            query = query.Where(hp => hp.Fecha >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(hp => hp.Fecha <= endDate.Value);

        return query;
    }

    private IQueryable<HistoriaClinicaVisita> GetVisitasWithPagosQuery(string branchId, DateTime? startDate, DateTime? endDate)
    {
        return GetVisitasQuery(branchId, startDate, endDate)
            .Where(v => v.Pagos.Any());
    }

    private IQueryable<HistoriaClinicaVisita> GetVisitasByStatusQuery(string branchId, int status, DateTime? startDate, DateTime? endDate)
    {

        return GetVisitasQuery(branchId, startDate, endDate)
            .Where(v => (int)v.Estado == status);
    }

    private (DateTime currentStart, DateTime currentEnd, DateTime previousStart, DateTime previousEnd)
        GetDateRange(string period, DateTime? customStart, DateTime? customEnd)
    {
        DateTime currentStart, currentEnd, previousStart, previousEnd;

        var today = DateTime.Today;

        switch (period)
        {
            case "day":
                currentStart = today;
                currentEnd = today.AddDays(1).AddSeconds(-1);
                previousStart = today.AddDays(-1);
                previousEnd = today.AddSeconds(-1);
                break;

            case "week":
                var startOfWeek = today.AddDays(-(int)today.DayOfWeek + (int)DayOfWeek.Monday);
                currentStart = startOfWeek;
                currentEnd = startOfWeek.AddDays(7).AddSeconds(-1);
                previousStart = startOfWeek.AddDays(-7);
                previousEnd = startOfWeek.AddSeconds(-1);
                break;

            case "month":
                currentStart = new DateTime(today.Year, today.Month, 1);
                currentEnd = currentStart.AddMonths(1).AddSeconds(-1);
                previousStart = currentStart.AddMonths(-1);
                previousEnd = currentStart.AddSeconds(-1);
                break;

            case "year":
                currentStart = new DateTime(today.Year, 1, 1);
                currentEnd = currentStart.AddYears(1).AddSeconds(-1);
                previousStart = currentStart.AddYears(-1);
                previousEnd = currentStart.AddSeconds(-1);
                break;

            case "custom":
                currentStart = customStart ?? today;
                currentEnd = customEnd ?? today.AddDays(1).AddSeconds(-1);
                var daysDiff = (currentEnd - currentStart).Days;
                previousStart = currentStart.AddDays(-daysDiff - 1);
                previousEnd = currentStart.AddSeconds(-1);
                break;

            default:
                throw new ArgumentException("Período no válido");
        }

        return (currentStart, currentEnd, previousStart, previousEnd);
    }

    private decimal CalculatePercentageChange(decimal current, decimal previous)
    {
        if (previous == 0) return current > 0 ? 100 : 0;
        return ((current - previous) / previous) * 100;
    }

    private List<DateTime> GenerateDateLabels(DateTime start, DateTime end, string period)
    {
        var labels = new List<DateTime>();
        var current = start;

        while (current <= end)
        {
            labels.Add(current);
            current = period switch
            {
                "day" => current.AddHours(6),
                "week" => current.AddDays(1),
                "month" => current.AddDays(1),
                "year" => current.AddMonths(1),
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