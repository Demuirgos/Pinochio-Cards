using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Game.Architecture;
using Game.Models;

namespace Game.Hubs;
public class GameHub : Hub
{
    public enum MessageType {
        GetNotification,  GetUpdate,  GetMessage,
        GetGameStarted, GetGameEnded, AlterState, GetState
    }
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
    public override async Task OnConnectedAsync() 
        => await base.OnConnectedAsync();
    public async Task<string> GetId() 
        =>  Context.ConnectionId; 
    public async Task<List<RoomsData>> GetRooms() 
        =>  OpenRooms ?? new(); 
    public async Task<Metadata> GetRoom(string roomId) 
        =>  Engine.Rooms[roomId];
    public async Task SendMessage(string userId, string roomId, string message) 
        =>  await Clients.Group(roomId).SendAsync(
                MessageType.GetMessage.ToString(), 
                new ChatMessage(Users[userId], message));
    public async Task SetupRoom(string userId, string roomId, string roomName){
        try
        {
            Engine.CreateRoom(
                dealer : new Player(userId, Users[userId]),
                name : roomName,
                connectionId : roomId
            );
            await Groups.AddToGroupAsync(userId, roomId);

            await Clients.Client(userId).SendAsync(
                MessageType.GetNotification.ToString(), 
                $"Room Created room Id : {roomId}");
            await Clients.All.SendAsync(
                MessageType.GetUpdate.ToString(), 
                OpenRooms);
        }
        catch (System.Exception e)
        {
            await Clients.Clients(userId).SendAsync(
                MessageType.GetNotification.ToString(), e.Message);
        }
    }

    public async Task StartRoom(string userId, string roomId){
        try{
            Engine.InitiateRoom(roomId)
                .StartGame(roomId);
            await Clients.Group(roomId).SendAsync(
                MessageType.GetNotification.ToString(),  
                $"Room Started room Id : {roomId}");
            await Clients.Group(roomId).SendAsync(
                MessageType.GetGameStarted.ToString(), roomId);
            await Clients.All.SendAsync(
                MessageType.GetUpdate.ToString(), OpenRooms);
        } catch(Exception e){
            await Clients.Group(roomId).SendAsync(
                MessageType.GetNotification.ToString(), e.Message);
        }
    }
    public async Task StopRoom(string userId, string roomId){
        Engine.ResetRoom(roomId);
        await Clients.Group(roomId).SendAsync(
            MessageType.GetNotification.ToString(),  
            $"Room Ended room Id : {roomId}");
        await Clients.Group(roomId).SendAsync(
            MessageType.GetGameEnded.ToString(), roomId);
        await Clients.All.SendAsync(
            MessageType.GetUpdate.ToString(), OpenRooms);
    }

    public async Task AlterState(string roomId, Message action){
        try{
            Engine.UpdateRoom(roomId, action);
            await Clients.Group(roomId).SendAsync(
                MessageType.GetState.ToString(), Engine.Rooms[roomId]);
        } catch(Exception e) {
            await Clients.Clients(action.PlayerId).SendAsync(
                MessageType.GetNotification.ToString(), e.Message);
        }
    }
    public async Task SetupAccount(string userId, string name){
        try{
            Users.Add(userId, name);
            await Clients.Client(Context.ConnectionId).SendAsync(
                MessageType.GetNotification.ToString(), $"Account Created");
        } catch(Exception e) {
            await Clients.Clients(userId).SendAsync(
                MessageType.GetNotification.ToString(), e.Message);
        }
    }
    public async Task AddToGroup(string roomId, string userId)
    {
        OpenRooms
            .Where(r => r.Id != roomId || r.OwnerId != userId)
            .ToList().ForEach(async room => {
                await RemoveFromGroup(room.Id, userId);
                Engine.QuitRoom(room.Id, userId );
            });
        
        Engine.JoinRoom(roomId, new Player(userId, Users[userId]));
        await Groups.AddToGroupAsync(userId, roomId);
        await Clients.Client(userId).SendAsync(
            MessageType.GetNotification.ToString(), 
            $"You Joined Room {OpenRooms?.Find(room => room.Id == roomId)?.Name}");
        await Clients.Group(roomId).SendAsync(
            MessageType.GetNotification.ToString(), 
            $"Player {Users[userId]} Joined!!");
        await Clients.All.SendAsync(
            MessageType.GetUpdate.ToString(), OpenRooms);
    }

    public async Task RemoveFromGroup(string roomId, string userId)
    {
        Engine.QuitRoom(roomId, userId);
        await Groups.RemoveFromGroupAsync(userId, roomId);
        await Clients.Client(userId).SendAsync(
            MessageType.GetNotification.ToString(), 
            $"You Left Room {OpenRooms?.Find(room => room.Id == roomId)?.Name}");
        await Clients.Group(roomId).SendAsync(
            MessageType.GetNotification.ToString(), 
            $"Player {Users[userId]} Left!!");
        await Clients.All.SendAsync(
            MessageType.GetUpdate.ToString(), OpenRooms);
    }

    public async Task RemoveGroup(string roomId, string userId)
    {
        Engine.CloseRoom(roomId, userId);
        await Clients.All.SendAsync(
            MessageType.GetUpdate.ToString(), OpenRooms);
        await Clients.Client(userId).SendAsync(
            MessageType.GetNotification.ToString(), $"Room Closed!!");
    }
}