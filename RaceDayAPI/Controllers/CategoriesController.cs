using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayAPI.Data;
using RaceDayAPI.DTOs;
using RaceDayAPI.Models;

namespace RaceDayAPI.Controllers;

[ApiController]
[Route("api/events/{eventId}/categories")]
[Authorize]
public class CategoriesController : ControllerBase
{
    private readonly RaceDayContext _db;

    public CategoriesController(RaceDayContext db)
    {
        _db = db;
    }

    /// <summary>Gets all categories for an event. Available to Organisers and Participants.</summary>
    /// <param name="eventId">The event ID.</param>
    /// <response code="200">Categories returned.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<Category>>> GetAll(int eventId)
    {
        if (!await _db.Events.AnyAsync(e => e.EventId == eventId))
            return NotFound("Event not found.");

        return await _db.Categories
          .AsNoTracking()
          .Where(c => c.EventId == eventId)
          .OrderBy(c => c.Distance)
          .ToListAsync();
    }

    /// <summary>Gets one category by its ID. Available to Organisers and Participants.</summary>
    /// <param name="eventId">The event ID.</param>
    /// <param name="id">The category ID.</param>
    /// <response code="200">Category returned.</response>
    /// <response code="404">Category not found for this event.</response>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Category>> GetById(int eventId, int id)
    {
        var category = await _db.Categories.AsNoTracking()
          .FirstOrDefaultAsync(c => c.CategoryId == id && c.EventId == eventId);

        if (category == null) return NotFound();
        return category;
    }

    /// <summary>Creates a category for an event (Organiser only).</summary>
    /// <param name="eventId">The event ID.</param>
    /// <param name="dto">Category name (e.g. "Under 20", "10km") and distance in km.</param>
    /// <returns>The created category.</returns>
    /// <response code="201">Category created.</response>
    /// <response code="403">Caller is not an Organiser.</response>
    /// <response code="404">Event not found.</response>
    [HttpPost]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Category>> Create(int eventId, CategoryDto dto)
    {
        if (!await _db.Events.AnyAsync(e => e.EventId == eventId))
            return NotFound("Event not found.");

        var category = new Category
        {
            EventId = eventId,
            Name = dto.Name,
            Distance = dto.Distance
        };

        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById),
          new { eventId, id = category.CategoryId }, category);
    }

    /// <summary>Updates a category (Organiser only).</summary>
    /// <param name="eventId">The event ID.</param>
    /// <param name="id">The category ID.</param>
    /// <param name="dto">The new category details.</param>
    /// <response code="204">Category updated.</response>
    /// <response code="403">Caller is not an Organiser.</response>
    /// <response code="404">Category not found for this event.</response>
    [HttpPut("{id}")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int eventId, int id, CategoryDto dto)
    {
        var category = await _db.Categories
          .FirstOrDefaultAsync(c => c.CategoryId == id && c.EventId == eventId);
        if (category == null) return NotFound();

        category.Name = dto.Name;
        category.Distance = dto.Distance;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Deletes a category (Organiser only). Fails if participants are enrolled in it.</summary>
    /// <param name="eventId">The event ID.</param>
    /// <param name="id">The category ID.</param>
    /// <response code="204">Category deleted.</response>
    /// <response code="403">Caller is not an Organiser.</response>
    /// <response code="404">Category not found for this event.</response>
    /// <response code="409">Category has enrolments and cannot be deleted.</response>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int eventId, int id)
    {
        var category = await _db.Categories
          .FirstOrDefaultAsync(c => c.CategoryId == id && c.EventId == eventId);
        if (category == null) return NotFound();

        if (await _db.Registrations.AnyAsync(r => r.CategoryId == id))
            return Conflict("Cannot delete a category that already has enrolments.");

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}