using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayAPI.Data;
using RaceDayAPI.DTOs;
using RaceDayAPI.Models;

namespace RaceDayAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly RaceDayContext _db;
    private readonly IPasswordHasher<UserProfile> _hasher;
    private readonly IConfiguration _config;

    public AuthController(RaceDayContext db, IPasswordHasher<UserProfile> hasher, IConfiguration config)
    {
        _db = db;
        _hasher = hasher;
        _config = config;
    }

    /// <summary>Registers a new user as an Organiser or Participant.</summary>
    /// <param name="dto">Name, email, password (min 8 characters), age, gender, contact info and role (0 = Organiser, 1 = Participant). Organisers must also supply the organiser invite code.</param>
    /// <returns>The new user's ID, name and role. The password is never returned.</returns>
    /// <response code="201">User registered.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="403">Invalid organiser code.</response>
    /// <response code="409">Email already registered.</response>
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        if (await _db.Users.AnyAsync(u => u.Email == dto.Email))
            return Conflict("Email already registered.");

        if (dto.Role == UserRole.Organiser &&
          dto.OrganiserCode != _config["OrganiserInviteCode"])
            return StatusCode(StatusCodes.Status403Forbidden, "Invalid organiser code.");

        var user = new UserProfile
        {
            Name = dto.Name,
            Email = dto.Email,
            Age = dto.Age,
            Gender = dto.Gender,
            ContactInfo = dto.ContactInfo,
            Role = dto.Role
        };
        user.PasswordHash = _hasher.HashPassword(user, dto.Password);

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created,
          new { user.UserId, user.Name, Role = user.Role.ToString() });
    }

    /// <summary>Logs in with email and password and starts a session cookie.</summary>
    /// <param name="dto">Email and password.</param>
    /// <returns>The user's ID, name and role.</returns>
    /// <response code="200">Logged in. A session cookie is set.</response>
    /// <response code="401">Invalid email or password.</response>
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);

        if (user == null ||
          _hasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password)
            == PasswordVerificationResult.Failed)
            return Unauthorized("Invalid email or password.");

        var claims = new List<Claim>
    {
      new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
      new(ClaimTypes.Name, user.Name),
      new(ClaimTypes.Role, user.Role.ToString())
    };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(
          CookieAuthenticationDefaults.AuthenticationScheme,
          new ClaimsPrincipal(identity));

        return Ok(new { user.UserId, user.Name, Role = user.Role.ToString() });
    }

    /// <summary>Logs out and ends the session.</summary>
    /// <response code="204">Logged out.</response>
    /// <response code="401">Not logged in.</response>
    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }
}