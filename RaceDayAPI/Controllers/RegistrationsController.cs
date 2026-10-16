using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayAPI.Data;
using RaceDayAPI.DTOs;
using RaceDayAPI.Models;

namespace RaceDayAPI.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class RegistrationsController : ControllerBase
{
    private readonly RaceDayContext _db;

    public RegistrationsController(RaceDayContext db)
    {
        _db = db;
    }

    /// <summary>Enrols the logged-in Participant in an event under a chosen category.</summary>
    /// <param name="dto">The event ID and the category ID the participant wants to enter.</param>
    /// <returns>The recorded enrolment.</returns>
    /// <response code="201">Enrolment recorded.</response>
    /// <response code="400">Category does not belong to the event.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Caller is not a Participant.</response>
    /// <response code="404">Event not found.</response>
    /// <response code="409">Already enrolled in this event.</response>
    [HttpPost("registrations")]
    [Authorize(Roles = "Participant")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Enrol(EnrolDto dto)
    {
        // The participant ID comes from the login cookie, never from the request body
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var ev = await _db.Events.AsNoTracking()
          .FirstOrDefaultAsync(e => e.EventId == dto.EventId);
        if (ev == null) return NotFound("Event not found.");

        var category = await _db.Categories.AsNoTracking()
          .FirstOrDefaultAsync(c => c.CategoryId == dto.CategoryId && c.EventId == dto.EventId);
        if (category == null) return BadRequest("Category does not belong to this event.");

        if (await _db.Registrations.AnyAsync(r =>
            r.ParticipantId == userId && r.EventId == dto.EventId))
            return Conflict("You are already enrolled in this event.");

        var registration = new Registration
        {
            EventId = dto.EventId,
            CategoryId = dto.CategoryId,
            ParticipantId = userId,
            PaymentStatus = "Pending"
        };

        _db.Registrations.Add(registration);
        await _db.SaveChangesAsync();

        var response = new RegistrationResponseDto
        {
            RegistrationId = registration.RegistrationId,
            EventId = ev.EventId,
            EventName = ev.Name,
            CategoryId = category.CategoryId,
            CategoryName = category.Name,
            ParticipantId = userId,
            ParticipantName = User.FindFirstValue(ClaimTypes.Name) ?? "",
            PaymentStatus = registration.PaymentStatus,
            BibNumber = registration.BibNumber
        };

        return StatusCode(StatusCodes.Status201Created, response);
    }

    /// <summary>Gets the logged-in Participant's own enrolments.</summary>
    /// <returns>A list of the participant's enrolments.</returns>
    /// <response code="200">Enrolments returned.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Caller is not a Participant.</response>
    [HttpGet("registrations/mine")]
    [Authorize(Roles = "Participant")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<RegistrationResponseDto>>> GetMine()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        return await BuildQuery()
          .Where(x => x.ParticipantId == userId)
          .ToListAsync();
    }

    /// <summary>Gets all enrolments for an event (Organiser only).</summary>
    /// <param name="eventId">The event ID.</param>
    /// <returns>A list of enrolments for the event.</returns>
    /// <response code="200">Enrolments returned.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Caller is not an Organiser.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet("events/{eventId}/registrations")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<RegistrationResponseDto>>> GetForEvent(int eventId)
    {
        if (!await _db.Events.AnyAsync(e => e.EventId == eventId))
            return NotFound("Event not found.");

        return await BuildQuery()
          .Where(x => x.EventId == eventId)
          .ToListAsync();
    }

    // Joins registrations with events, categories and users to produce readable results
    private IQueryable<RegistrationResponseDto> BuildQuery()
    {
        return from r in _db.Registrations.AsNoTracking()
               join e in _db.Events on r.EventId equals e.EventId
               join c in _db.Categories on r.CategoryId equals c.CategoryId
               join u in _db.Users on r.ParticipantId equals u.UserId
               select new RegistrationResponseDto
               {
                   RegistrationId = r.RegistrationId,
                   EventId = e.EventId,
                   EventName = e.Name,
                   CategoryId = c.CategoryId,
                   CategoryName = c.Name,
                   ParticipantId = u.UserId,
                   ParticipantName = u.Name,
                   PaymentStatus = r.PaymentStatus,
                   BibNumber = r.BibNumber
               };
    }
}