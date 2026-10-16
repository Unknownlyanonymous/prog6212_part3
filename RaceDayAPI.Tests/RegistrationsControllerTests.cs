using Microsoft.AspNetCore.Mvc;
using RaceDayAPI.Controllers;
using RaceDayAPI.Data;
using RaceDayAPI.DTOs;
using RaceDayAPI.Models;

namespace RaceDayAPI.Tests;

public class RegistrationsControllerTests
{
    // Creates a database with event 1 and category 1 (belonging to event 1) and event 2
    private static async Task<RaceDayContext> SeedDbAsync()
    {
        var db = TestHelpers.NewDb();
        db.Events.Add(new Event { EventId = 1, Name = "10K Run" });
        db.Events.Add(new Event { EventId = 2, Name = "21K Run" });
        db.Categories.Add(new Category { CategoryId = 1, EventId = 1, Name = "10km", Distance = 10 });
        db.Categories.Add(new Category { CategoryId = 2, EventId = 2, Name = "21km", Distance = 21 });
        await db.SaveChangesAsync();
        return db;
    }

    private static RegistrationsController NewController(RaceDayContext db, int userId = 10)
    {
        var controller = new RegistrationsController(db);
        TestHelpers.SetUser(controller, userId, "Participant", "Test Participant");
        return controller;
    }

    [Fact]
    public async Task Enrol_ValidRequest_RecordsEnrolmentCorrectly()
    {
        var db = await SeedDbAsync();
        var controller = NewController(db, userId: 10);

        var result = await controller.Enrol(new EnrolDto { EventId = 1, CategoryId = 1 });

        var created = Assert.IsType<ObjectResult>(result);
        Assert.Equal(201, created.StatusCode);

        var saved = Assert.Single(db.Registrations);
        Assert.Equal(1, saved.EventId);
        Assert.Equal(1, saved.CategoryId);
        Assert.Equal(10, saved.ParticipantId);       // taken from the logged-in user
        Assert.Equal("Pending", saved.PaymentStatus);
    }

    [Fact]
    public async Task Enrol_AlreadyEnrolled_ReturnsConflict()
    {
        var db = await SeedDbAsync();
        var controller = NewController(db, userId: 10);

        await controller.Enrol(new EnrolDto { EventId = 1, CategoryId = 1 });
        var second = await controller.Enrol(new EnrolDto { EventId = 1, CategoryId = 1 });

        Assert.IsType<ConflictObjectResult>(second);
        Assert.Single(db.Registrations);
    }

    [Fact]
    public async Task Enrol_CategoryFromAnotherEvent_ReturnsBadRequest()
    {
        var db = await SeedDbAsync();
        var controller = NewController(db);

        // Category 2 belongs to event 2, not event 1
        var result = await controller.Enrol(new EnrolDto { EventId = 1, CategoryId = 2 });

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(db.Registrations);
    }

    [Fact]
    public async Task Enrol_EventDoesNotExist_ReturnsNotFound()
    {
        var db = await SeedDbAsync();
        var controller = NewController(db);

        var result = await controller.Enrol(new EnrolDto { EventId = 999, CategoryId = 1 });

        Assert.IsType<NotFoundObjectResult>(result);
        Assert.Empty(db.Registrations);
    }

    [Fact]
    public async Task GetMine_ReturnsOnlyTheLoggedInParticipantsEnrolments()
    {
        var db = await SeedDbAsync();
        db.Users.Add(new UserProfile { UserId = 10, Name = "Me", Email = "me@test.com", PasswordHash = "x" });
        db.Users.Add(new UserProfile { UserId = 20, Name = "Other", Email = "other@test.com", PasswordHash = "x" });
        db.Registrations.Add(new Registration { EventId = 1, CategoryId = 1, ParticipantId = 10 });
        db.Registrations.Add(new Registration { EventId = 2, CategoryId = 2, ParticipantId = 20 });
        await db.SaveChangesAsync();
        var controller = NewController(db, userId: 10);

        var result = await controller.GetMine();

        var list = result.Value!.ToList();
        Assert.Single(list);
        Assert.Equal(10, list[0].ParticipantId);
    }
}