using System.Text.Json;
using AutoMapper;
using CommandService.Dtos;
using CommandsService.Data;
using CommandsService.Dtos;
using CommandsService.Models;

namespace CommandsService.EventProcessing;

public class EventProcessor : IEventProcessor
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IMapper _mapper;

    public EventProcessor(IServiceScopeFactory scopeFactory, IMapper mapper)
    {
        _scopeFactory = scopeFactory;
        _mapper = mapper;

    }

    private EventType DetermineEvent(string notificationMessage)
    {
        Console.WriteLine("--> Determining event ");

        var eventType = JsonSerializer.Deserialize<GenericEventDto>(notificationMessage);

        switch (eventType.Event)
        {
            case "Platform_Published":
                Console.WriteLine("--> Platform_Published event detected");
                return EventType.PlatformPublished;
            default:
                Console.WriteLine("--> Could not determine event type");
                return EventType.Undetermined;
        }
    }

    public void ProcessEvent(string message)
    {
        var eventType = DetermineEvent(message);

        switch (eventType)
        {
            case EventType.PlatformPublished:
                addPlatform(message);
                break;
            default:
                break;
        }
    }

    private void addPlatform(string platformPublishgedMessage)
    {
        using (var scope = _scopeFactory.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<ICommandRepo>();

            var platformPublishedDto = JsonSerializer.Deserialize<PlatformPublishedDto>(platformPublishgedMessage);
            try
            {
                var plat = _mapper.Map<Platform>(platformPublishedDto);
                if (!repo.ExternalPlatformExist(plat.ExternalId))
                {
                    repo.CreatePlatform(plat);
                    repo.SaveChanges();
                    Console.WriteLine("--> Platform added");
                }
                else
                {
                    Console.WriteLine("--> Platform already exists ..");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"--> event could not add Platform to db {ex.Message}");
            }
        }
    }
}

enum EventType
{
    PlatformPublished,
    Undetermined
}
