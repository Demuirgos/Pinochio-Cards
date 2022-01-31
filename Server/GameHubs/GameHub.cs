using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Game.Architecture;
using Game.Models;
using System.Configuration;

namespace Game.Hubs;
public class GameHub : Hub
{
    private static Manager Engine = new Manager();
    private static Dictionary<string, string> Users = new();
    private static List<RoomsData>? OpenRooms 
        =>  Engine?.Rooms?.Select(
                room => new RoomsData(
                        Id : room.Key,
                        OwnerId : room.Value.Session.Dealer.Id,
                        Name : room.Value.Session.RoomName,
                        Members : room.Value.Session.Waiting.Select(p => p.Id).ToArray(), 
                        Size : room.Value.Session.Waiting.Count, 
                        State : room.Value.Session.State
                )).ToList();
    public async Task SetupRoom(string userId, string roomId, string roomName){
        Engine.CreateRoom(
            dealer : new Player(userId, Users[userId]),
            name : roomName,
            connectionId : roomId
        );
        await Groups.AddToGroupAsync(userId, roomId);
        await Clients.Client(userId).SendAsync("GetNotification", $"Room Created room Id : {roomId}");
        System.Console.WriteLine("SetupRoom : launching GetUpdate");
        await Clients.All.SendAsync("GetUpdate", OpenRooms);
    }
    public async Task SetupAccount(string userId, string name){
        Users.Add(userId, name);
        await Clients.Client(Context.ConnectionId).SendAsync("GetNotification", $"Account Created");
    }
    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
    }
    public async Task<string> GetId() => Context.ConnectionId; 
    public async Task<List<RoomsData>> GetRooms() => OpenRooms ?? new(); 
    public async Task AddToGroup(string roomId, string userId)
    {
        Engine.JoinRoom(roomId, new Player(userId, Users[userId]));
        await Groups.AddToGroupAsync(userId, roomId);
        await Clients.Client(userId).SendAsync("GetNotification", $"You Joined Room {OpenRooms?.Find(room => room.Id == roomId)?.Name}");
        await Clients.Group(roomId).SendAsync("GetNotification", $"Player {Users[userId]} Joined!!");
        System.Console.WriteLine("AddToGrp : launching GetUpdate");
        await Clients.All.SendAsync("GetUpdate", OpenRooms);
    }

    public async Task RemoveFromGroup(string roomId, string userId)
    {
        Engine.QuitRoom(roomId, new Player(userId, Users[userId]));
        await Groups.RemoveFromGroupAsync(userId, roomId);
        await Clients.Client(userId).SendAsync("GetNotification", $"You Left Room {OpenRooms?.Find(room => room.Id == roomId)?.Name}");
        await Clients.Group(roomId).SendAsync("GetNotification", $"Player {Users[userId]} Left!!");
        System.Console.WriteLine("RemoveFromToGrp : launching GetUpdate");
        await Clients.All.SendAsync("GetUpdate", OpenRooms);
    }

    public async Task RemoveGroup(string roomId, string userId)
    {
        Engine.CloseRoom(roomId, userId);
        await Clients.All.SendAsync("GetUpdate", OpenRooms);
        await Clients.Client(userId).SendAsync("GetNotification", $"Room Closed!!");
    }
}