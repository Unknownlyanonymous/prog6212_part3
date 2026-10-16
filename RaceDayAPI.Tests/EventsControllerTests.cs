using Microsoft.AspNetCore.Mvc;
using RaceDayAPI.Controllers;
using RaceDayAPI.DTOs;
using RaceDayAPI.Models;

namespace RaceDayAPI.Tests;

public class EventsControllerTests
{
    private static EventDto NewDto(string name = "Joburg 10K") => new()
    {
        Name = name,
        Description = "Community race",
        Date = DateTime.Now.AddDays(30),
        Location = "Johannesburg",
        EventType = EventType.Run
    };

    [Fact]
    public async Task Create_ValidEvent_SavesAndReturnsCreated()
    {
        var db = TestHelpers.NewDb();
        var controller = new EventsController(db);

        var result = await controller.Create(NewDto());

        Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Single(db.Events);
        Assert.Equal("Joburg 10K", db.Events.Single().Name);
    }

    [Fact]
    public async Task GetById_ExistingEvent_ReturnsEvent()
    {
        var db = TestHelpers.NewDb();
        db.Events.Add(new Event { EventId = 1, Name = "5K" });
        await db.SaveChangesAsync();
        var controller = new EventsController(db);

        var result = await controller.GetById(1);

        Assert.Equal("5K", result.Value!.Name);
    }

    [Fact]
    public async Task GetById_MissingEvent_ReturnsNotFound()
    {
        var controller = new EventsController(TestHelpers.NewDb());

        var result = await controller.GetById(999);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Update_ExistingEvent_ChangesValues()
    {
        var db = TestHelpers.NewDb();
        db.Events.Add(new Event { EventId = 1, Name = "Old Name" });
        await db.SaveChangesAsync();
        var controller = new EventsController(db);

        var result = await controller.Update(1, NewDto("New Name"));

        Assert.IsType<NoContentResult>(result);
        Assert.Equal("New Name", db.Events.Single().Name);
    }

    [Fact]
    public async Task Update_MissingEvent_ReturnsNotFound()
    {
        var controller = new EventsController(TestHelpers.NewDb());

        var result = await controller.Update(999, NewDto());

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Delete_ExistingEvent_RemovesIt()
    {
        var db = TestHelpers.NewDb();
        db.Events.Add(new Event { EventId = 1, Name = "5K" });
        await db.SaveChangesAsync();
        var controller = new EventsController(db);

        var result = await controller.Delete(1);

        Assert.IsType<NoContentResult>(result);
        Assert.Empty(db.Events);
    }

    [Fact]
    public async Task Delete_MissingEvent_ReturnsNotFound()
    {
        var controller = new EventsController(TestHelpers.NewDb());

        var result = await controller.Delete(999);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Delete_EventWithEnrolments_ReturnsConflict()
    {
        var db = TestHelpers.NewDb();
        db.Events.Add(new Event { EventId = 1, Name = "5K" });
        db.Registrations.Add(new Registration { EventId = 1, CategoryId = 1, ParticipantId = 10 });
        await db.SaveChangesAsync();
        var controller = new EventsController(db);

        var result = await controller.Delete(1);

        Assert.IsType<ConflictObjectResult>(result);
        Assert.Single(db.Events);
    }
}