using Game.Models;

namespace Game.Hubs;
public interface IServerHandler
{
    Task GetNotification<T>(T message);
    Task SetupRoom(string userId, string roomId, string roomName);
    Task SetupAccount(string userId, Player players);
    Task ReceiveMessage(string roomId, Message action);
    Task PropagateState(string roomId, GameState state);
    Task BroadcastSession(string roomId, GameSession session);
}