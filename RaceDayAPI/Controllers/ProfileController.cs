using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayAPI.Data;
using RaceDayAPI.DTOs;

namespace RaceDayAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]   // both roles, but must be logged in
public class ProfileController : ControllerBase
{
    private readonly RaceDayContext _db;

    public ProfileController(RaceDayContext db)
    {
        _db = db;
    }

    /// <summary>Gets the logged-in user's own profile. Available to Organisers and Participants.</summary>
    /// <returns>The user's profile (never includes the password).</returns>
    /// <response code="200">Profile returned.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="404">User no longer exists.</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProfileDto>> Get()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId);
        if (user == null) return NotFound();

        return new ProfileDto
        {
            UserId = user.UserId,
            Name = user.Name,
            Email = user.Email,
            Age = user.Age,
            Gender = user.Gender,
            ContactInfo = user.ContactInfo,
            Role = user.Role.ToString()
        };
    }

    /// <summary>Updates the logged-in user's own profile. Email and role cannot be changed.</summary>
    /// <param name="dto">Name, age, gender and contact information.</param>
    /// <response code="204">Profile updated.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="404">User no longer exists.</response>
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(UpdateProfileDto dto)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId);
        if (user == null) return NotFound();

        user.Name = dto.Name;
        user.Age = dto.Age;
        user.Gender = dto.Gender;
        user.ContactInfo = dto.ContactInfo;

        await _db.SaveChangesAsync();
        return NoContent();
    }
}