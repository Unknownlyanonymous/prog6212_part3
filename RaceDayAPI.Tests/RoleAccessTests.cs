using System.Net;
using System.Net.Http.Json;

namespace RaceDayAPI.Tests;

public class RoleAccessTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public RoleAccessTests(CustomWebApplicationFactory factory) => _factory = factory;

    private static object NewEventBody() => new
    {
        name = "Test 10K",
        description = "Test event",
        date = DateTime.Now.AddDays(30),
        location = "Johannesburg",
        eventType = 0
    };

    // ----- Not logged in -> 401 -----

    [Theory]
    [InlineData("/api/Events")]
    [InlineData("/api/Profile")]
    [InlineData("/api/events/1/categories")]
    [InlineData("/api/registrations/mine")]
    [InlineData("/api/results/mine")]
    public async Task ProtectedEndpoints_WhenNotLoggedIn_Return401(string url)
    {
        var client = _factory.NewClient();
        var res = await client.GetAsync(url);
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    // ----- Participant calling organiser functionality -> 403 -----

    [Fact]
    public async Task CreateEvent_AsParticipant_Returns403()
    {
        var client = _factory.NewClient();
        await TestHelpers.LoginAsNewUserAsync(client, organiser: false);

        var res = await client.PostAsJsonAsync("/api/Events", NewEventBody());

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task ViewEnrolmentsForEvent_AsParticipant_Returns403()
    {
        var client = _factory.NewClient();
        await TestHelpers.LoginAsNewUserAsync(client, organiser: false);

        var res = await client.GetAsync("/api/events/1/registrations");

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_AsParticipant_Returns403()
    {
        var client = _factory.NewClient();
        await TestHelpers.LoginAsNewUserAsync(client, organiser: false);

        var res = await client.PostAsJsonAsync("/api/results",
          new { registrationId = 1, finishTime = "00:50:00", position = 1 });

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    // ----- Organiser calling participant functionality -> 403 -----

    [Fact]
    public async Task Enrol_AsOrganiser_Returns403()
    {
        var client = _factory.NewClient();
        await TestHelpers.LoginAsNewUserAsync(client, organiser: true);

        var res = await client.PostAsJsonAsync("/api/registrations",
          new { eventId = 1, categoryId = 1 });

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task ViewMyResults_AsOrganiser_Returns403()
    {
        var client = _factory.NewClient();
        await TestHelpers.LoginAsNewUserAsync(client, organiser: true);

        var res = await client.GetAsync("/api/results/mine");

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    // ----- Correct role succeeds -----

    [Fact]
    public async Task CreateEvent_AsOrganiser_Returns201()
    {
        var client = _factory.NewClient();
        await TestHelpers.LoginAsNewUserAsync(client, organiser: true);

        var res = await client.PostAsJsonAsync("/api/Events", NewEventBody());

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
    }

    [Fact]
    public async Task ViewEvents_AsParticipant_Returns200()
    {
        var client = _factory.NewClient();
        await TestHelpers.LoginAsNewUserAsync(client, organiser: false);

        var res = await client.GetAsync("/api/Events");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task Logout_ThenAccessProtectedEndpoint_Returns401()
    {
        var client = _factory.NewClient();
        await TestHelpers.LoginAsNewUserAsync(client, organiser: false);

        var logout = await client.PostAsync("/api/Auth/logout", null);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        var res = await client.GetAsync("/api/Events");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }
}