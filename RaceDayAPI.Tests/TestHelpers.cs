using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using RaceDayAPI.Data;

namespace RaceDayAPI.Tests;

public static class TestHelpers
{
    // Must match "OrganiserInviteCode" in appsettings.json
    public const string OrganiserCode = "RACEDAY-ORG-2026";
    public const string Password = "Password123";

    // ---------- Controller (unit) test helpers ----------

    public static RaceDayContext NewDb() =>
    new(new DbContextOptionsBuilder<RaceDayContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options);

    // Pretends a user is logged in, the same way the login cookie would
    public static void SetUser(ControllerBase controller, int userId, string role, string name = "Test User")
    {
        var identity = new ClaimsIdentity(new[]
        {
      new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
      new Claim(ClaimTypes.Name, name),
      new Claim(ClaimTypes.Role, role)
    }, "test");

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
    }

    // ---------- API (integration) test helpers ----------

    // Each client has its own cookie container, so it acts as one separate browser
    public static HttpClient NewClient(this CustomWebApplicationFactory factory) =>
    factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    public static string UniqueEmail() => $"{Guid.NewGuid():N}@test.com";

    public static Task<HttpResponseMessage> RegisterAsync(
      HttpClient client, string email, bool organiser, string? code = null)
    {
        return client.PostAsJsonAsync("/api/Auth/register", new
        {
            name = organiser ? "Test Organiser" : "Test Participant",
            email,
            password = Password,
            age = 30,
            gender = "Male",
            contactInfo = "0821234567",
            role = organiser ? 0 : 1,
            organiserCode = code
        });
    }

    public static Task<HttpResponseMessage> LoginAsync(HttpClient client, string email, string password = Password)
    {
        return client.PostAsJsonAsync("/api/Auth/login", new { email, password });
    }

    // Registers a new user and logs the client in as them
    public static async Task LoginAsNewUserAsync(HttpClient client, bool organiser)
    {
        var email = UniqueEmail();
        var reg = await RegisterAsync(client, email, organiser, organiser ? OrganiserCode : null);
        reg.EnsureSuccessStatusCode();
        var login = await LoginAsync(client, email);
        login.EnsureSuccessStatusCode();
    }
}