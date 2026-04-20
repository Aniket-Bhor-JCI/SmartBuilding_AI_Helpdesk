using System.Security.Claims;
using backend.Data;
using backend.Dtos;
using backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TicketsController(AppDbContext context) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<TicketResponse>> Create(CreateTicketRequest request)
    {
        var userId = GetUserId();
        var ticket = new Ticket
        {
            Issue = request.Issue,
            Category = request.Category,
            Location = request.Location,
            Priority = request.Priority,
            Status = "Open",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId
        };

        context.Tickets.Add(ticket);
        await context.SaveChangesAsync();

        var user = await context.Users.FindAsync(userId);
        return Ok(MapTicket(ticket, user?.Name));
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TicketResponse>>> GetAll()
    {
        var isAdmin = User.IsInRole(UserRole.Admin.ToString());
        var userId = GetUserId();

        var query = context.Tickets
            .Include(ticket => ticket.CreatedByUser)
            .AsQueryable();

        if (!isAdmin)
        {
            query = query.Where(ticket => ticket.CreatedBy == userId);
        }

        var tickets = await query
            .OrderByDescending(ticket => ticket.CreatedAt)
            .Select(ticket => new TicketResponse(
                ticket.Id,
                ticket.Issue,
                ticket.Category,
                ticket.Location,
                ticket.Priority,
                ticket.Status,
                ticket.CreatedAt,
                ticket.CreatedBy,
                ticket.CreatedByUser != null ? ticket.CreatedByUser.Name : null,
                ticket.AssignedTo))
            .ToListAsync();

        return Ok(tickets);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<ActionResult<TicketResponse>> Update(int id, UpdateTicketRequest request)
    {
        var ticket = await context.Tickets.Include(item => item.CreatedByUser).FirstOrDefaultAsync(item => item.Id == id);
        if (ticket is null)
        {
            return NotFound();
        }

        ticket.Status = string.IsNullOrWhiteSpace(request.Status) ? ticket.Status : request.Status;
        ticket.Priority = string.IsNullOrWhiteSpace(request.Priority) ? ticket.Priority : request.Priority;
        ticket.AssignedTo = request.AssignedTo?.Trim();
        await context.SaveChangesAsync();

        return Ok(MapTicket(ticket, ticket.CreatedByUser?.Name));
    }

    private int GetUserId()
    {
        return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? throw new InvalidOperationException("Missing user id claim."));
    }

    private static TicketResponse MapTicket(Ticket ticket, string? createdByName)
    {
        return new TicketResponse(
            ticket.Id,
            ticket.Issue,
            ticket.Category,
            ticket.Location,
            ticket.Priority,
            ticket.Status,
            ticket.CreatedAt,
            ticket.CreatedBy,
            createdByName,
            ticket.AssignedTo);
    }
}
