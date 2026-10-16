using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayAPI.Data;
using RaceDayAPI.DTOs;
using RaceDayAPI.Models;

namespace RaceDayAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]   // every action requires a logged-in user
public class EventsController : ControllerBase
{
    private readonly RaceDayContext _db;

    public EventsController(RaceDayContext db)
    {
        _db = db;
    }

    /// <summary>Gets all events. Available to Organisers and Participants.</summary>
    /// <returns>A list of events.</returns>
    /// <response code="200">Events returned.</response>
    /// <response code="401">Not logged in.</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<Event>>> GetAll()
    {
        return await _db.Events.AsNoTracking().OrderBy(e => e.Date).ToListAsync();
    }

    /// <summary>Gets one event by its ID. Available to Organisers and Participants.</summary>
    /// <param name="id">The event ID.</param>
    /// <response code="200">Event returned.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Event>> GetById(int id)
    {
        var ev = await _db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.EventId == id);
        if (ev == null) return NotFound();
        return ev;
    }

    /// <summary>Creates a new event (Organiser only).</summary>
    /// <param name="dto">Name, description, date, location and event type (0 = Run, 1 = Walk, 2 = Cycle).</param>
    /// <returns>The created event.</returns>
    /// <response code="201">Event created.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Caller is not an Organiser.</response>
    [HttpPost]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Event>> Create(EventDto dto)
    {
        var ev = new Event
        {
            Name = dto.Name,
            Description = dto.Description,
            Date = dto.Date,
            Location = dto.Location,
            EventType = dto.EventType
        };

        _db.Events.Add(ev);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = ev.EventId }, ev);
    }

    /// <summary>Updates an existing event (Organiser only).</summary>
    /// <param name="id">The event ID.</param>
    /// <param name="dto">The new event details.</param>
    /// <response code="204">Event updated.</response>
    /// <response code="403">Caller is not an Organiser.</response>
    /// <response code="404">Event not found.</response>
    [HttpPut("{id}")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, EventDto dto)
    {
        var ev = await _db.Events.FindAsync(id);
        if (ev == null) return NotFound();

        ev.Name = dto.Name;
        ev.Description = dto.Description;
        ev.Date = dto.Date;
        ev.Location = dto.Location;
        ev.EventType = dto.EventType;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Deletes an event (Organiser only). Fails if participants are already enrolled.</summary>
    /// <param name="id">The event ID.</param>
    /// <response code="204">Event deleted.</response>
    /// <response code="403">Caller is not an Organiser.</response>
    /// <response code="404">Event not found.</response>
    /// <response code="409">Event has enrolments and cannot be deleted.</response>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id)
    {
        var ev = await _db.Events.FindAsync(id);
        if (ev == null) return NotFound();

        if (await _db.Registrations.AnyAsync(r => r.EventId == id))
            return Conflict("Cannot delete an event that already has enrolments.");

        _db.Events.Remove(ev);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}