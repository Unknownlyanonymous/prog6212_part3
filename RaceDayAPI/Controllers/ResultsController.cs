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
public class ResultsController : ControllerBase
{
    private readonly RaceDayContext _db;

    public ResultsController(RaceDayContext db)
    {
        _db = db;
    }

    /// <summary>Captures a finish time and position for an enrolment (Organiser only).</summary>
    /// <param name="dto">Registration ID, finish time (hh:mm:ss) and finishing position.</param>
    /// <returns>The recorded result, including the calculated pace.</returns>
    /// <response code="201">Result recorded.</response>
    /// <response code="400">Invalid finish time format.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Caller is not an Organiser.</response>
    /// <response code="404">Registration not found.</response>
    /// <response code="409">Result already exists, or the position is taken in this category.</response>
    [HttpPost("results")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(ResultDto dto)
    {
        if (!TryParseTime(dto.FinishTime, out var finishTime))
            return BadRequest("Finish time must be in the format hh:mm:ss, for example 00:52:30.");

        var registration = await _db.Registrations.AsNoTracking()
          .FirstOrDefaultAsync(r => r.RegistrationId == dto.RegistrationId);
        if (registration == null) return NotFound("Registration not found.");

        if (await _db.Results.AnyAsync(r => r.RegistrationId == dto.RegistrationId))
            return Conflict("A result has already been captured for this registration.");

        if (await PositionTaken(registration.CategoryId, dto.Position, null))
            return Conflict("That position is already taken in this category.");

        var category = await _db.Categories.AsNoTracking()
          .FirstAsync(c => c.CategoryId == registration.CategoryId);

        var result = new Result
        {
            RegistrationId = dto.RegistrationId,
            FinishTime = finishTime,
            Position = dto.Position,
            Pace = CalculatePace(finishTime, category.Distance)
        };

        _db.Results.Add(result);
        await _db.SaveChangesAsync();

        var response = await BuildQuery().FirstAsync(x => x.ResultId == result.ResultId);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    /// <summary>Corrects an existing result (Organiser only).</summary>
    /// <param name="id">The result ID.</param>
    /// <param name="dto">The corrected finish time and position. The registration cannot be changed.</param>
    /// <response code="204">Result updated.</response>
    /// <response code="400">Invalid finish time format.</response>
    /// <response code="403">Caller is not an Organiser.</response>
    /// <response code="404">Result not found.</response>
    /// <response code="409">Position is taken in this category.</response>
    [HttpPut("results/{id}")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(int id, ResultDto dto)
    {
        if (!TryParseTime(dto.FinishTime, out var finishTime))
            return BadRequest("Finish time must be in the format hh:mm:ss, for example 00:52:30.");

        var result = await _db.Results.FindAsync(id);
        if (result == null) return NotFound();

        var registration = await _db.Registrations.AsNoTracking()
          .FirstAsync(r => r.RegistrationId == result.RegistrationId);

        if (await PositionTaken(registration.CategoryId, dto.Position, id))
            return Conflict("That position is already taken in this category.");

        var category = await _db.Categories.AsNoTracking()
          .FirstAsync(c => c.CategoryId == registration.CategoryId);

        result.FinishTime = finishTime;
        result.Position = dto.Position;
        result.Pace = CalculatePace(finishTime, category.Distance);

        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Gets all results for an event (Organiser only).</summary>
    /// <param name="eventId">The event ID.</param>
    /// <returns>Results for the event, ordered by category and position.</returns>
    /// <response code="200">Results returned.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Caller is not an Organiser.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet("events/{eventId}/results")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<ResultResponseDto>>> GetForEvent(int eventId)
    {
        var ev = await _db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.EventId == eventId);
        if (ev == null) return NotFound("Event not found.");

        return await BuildQuery()
          .Where(x => x.EventName == ev.Name)
          .OrderBy(x => x.CategoryName).ThenBy(x => x.Position)
          .ToListAsync();
    }

    /// <summary>Gets the logged-in Participant's own results.</summary>
    /// <returns>The participant's results only.</returns>
    /// <response code="200">Results returned.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Caller is not a Participant.</response>
    [HttpGet("results/mine")]
    [Authorize(Roles = "Participant")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<ResultResponseDto>>> GetMine()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        return await BuildQuery()
          .Where(x => x.ParticipantId == userId)
          .ToListAsync();
    }

    // Accepts hh:mm:ss and rejects zero or negative times
    private static bool TryParseTime(string text, out TimeSpan time)
    {
        return TimeSpan.TryParse(text, out time) && time > TimeSpan.Zero;
    }

    // Pace in minutes per kilometre, e.g. "5:15 min/km"
    private static string CalculatePace(TimeSpan finishTime, double distanceKm)
    {
        var secondsPerKm = finishTime.TotalSeconds / distanceKm;
        var minutes = (int)(secondsPerKm / 60);
        var seconds = (int)(secondsPerKm % 60);
        return $"{minutes}:{seconds:D2} min/km";
    }

    // True if another result in the same category already has this position
    private async Task<bool> PositionTaken(int categoryId, int position, int? ignoreResultId)
    {
        return await (from res in _db.Results
                      join r in _db.Registrations on res.RegistrationId equals r.RegistrationId
                      where r.CategoryId == categoryId
                         && res.Position == position
                         && res.ResultId != ignoreResultId
                      select res).AnyAsync();
    }

    private IQueryable<ResultResponseDto> BuildQuery()
    {
        return from res in _db.Results.AsNoTracking()
               join r in _db.Registrations on res.RegistrationId equals r.RegistrationId
               join e in _db.Events on r.EventId equals e.EventId
               join c in _db.Categories on r.CategoryId equals c.CategoryId
               join u in _db.Users on r.ParticipantId equals u.UserId
               select new ResultResponseDto
               {
                   ResultId = res.ResultId,
                   RegistrationId = res.RegistrationId,
                   EventName = e.Name,
                   CategoryName = c.Name,
                   ParticipantId = u.UserId,
                   ParticipantName = u.Name,
                   FinishTime = res.FinishTime,
                   Position = res.Position,
                   Pace = res.Pace
               };
    }
}