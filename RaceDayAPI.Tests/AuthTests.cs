using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using RaceDayAPI.Data;

namespace RaceDayAPI.Tests;

public class AuthTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Register_ValidParticipant_Returns201()
    {
        var client = _factory.NewClient();
        var res = await TestHelpers.RegisterAsync(client, TestHelpers.UniqueEmail(), organiser: false);
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
    }

    [Fact]
    public async Task Register_OrganiserWithCorrectCode_Returns201()
    {
        var client = _factory.NewClient();
        var res = await TestHelpers.RegisterAsync(
          client, TestHelpers.UniqueEmail(), organiser: true, code: TestHelpers.OrganiserCode);
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
    }

    [Fact]
    public async Task Register_OrganiserWithWrongCode_Returns403()
    {
        var client = _factory.NewClient();
        var res = await TestHelpers.RegisterAsync(
          client, TestHelpers.UniqueEmail(), organiser: true, code: "WRONG");
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        var client = _factory.NewClient();
        var email = TestHelpers.UniqueEmail();

        await TestHelpers.RegisterAsync(client, email, organiser: false);
        var second = await TestHelpers.RegisterAsync(client, email, organiser: false);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Register_ShortPassword_Returns400()
    {
        var client = _factory.NewClient();
        var res = await client.PostAsJsonAsync("/api/Auth/register", new
        {
            name = "Test",
            email = TestHelpers.UniqueEmail(),
            password = "123",
            age = 25,
            gender = "Male",
            contactInfo = "0821234567",
            role = 1
        });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Register_StoresHashedPassword_NotPlainText()
    {
        var client = _factory.NewClient();
        var email = TestHelpers.UniqueEmail();
        await TestHelpers.RegisterAsync(client, email, organiser: false);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RaceDayContext>();
        var user = db.Users.Single(u => u.Email == email);

        Assert.NotEqual(TestHelpers.Password, user.PasswordHash);
        Assert.False(string.IsNullOrWhiteSpace(user.PasswordHash));
    }

    [Fact]
    public async Task Login_ValidCredentials_Returns200()
    {
        var client = _factory.NewClient();
        var email = TestHelpers.UniqueEmail();
        await TestHelpers.RegisterAsync(client, email, organiser: false);

        var res = await TestHelpers.LoginAsync(client, email);

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var client = _factory.NewClient();
        var email = TestHelpers.UniqueEmail();
        await TestHelpers.RegisterAsync(client, email, organiser: false);

        var res = await TestHelpers.LoginAsync(client, email, "WrongPassword1");

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Login_UnknownEmail_Returns401()
    {
        var client = _factory.NewClient();
        var res = await TestHelpers.LoginAsync(client, "nobody@test.com");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }
}