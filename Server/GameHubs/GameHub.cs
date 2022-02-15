using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Game.Architecture;
using Game.Models;
using System.Linq;

namespace Game.Hubs;
public class GameHub : Hub
{
    public enum MessageType {
        GetNotification,  GetUpdate,  GetMessage, GetIndicator,
        GetGameStarted, GetGameEnded, AlterState, GetState
    }
    private static Manager Engine = new Manager();
    private static Dictionary<string, string> Users = new();
    private static List<RoomsData>? OpenRooms 
        =>  Engine?.Rooms?.Select(
                room => new RoomsData(
                        Id : room.Key,
                        Password : room.Value.Session.Password,
                        IsPrivate : room.Value.Session.SessionType == GameSession.Type.Locked,
                        OwnerId : room.Value.Session.Dealer.Id,
                        Name : room.Value.Session.RoomName,
                        Members : room.Value.Session.Waiting.Select(p => p.Id).ToArray(), 
                        Size : room.Value.Session.Waiting.Count, 
                        State : room.Value.Session.State
                )).ToList();
    public override async Task OnConnectedAsync() 
        => await base.OnConnectedAsync();
    public override async Task OnDisconnectedAsync(Exception? _) {
        await base.OnDisconnectedAsync(null);
        var room = OpenRooms.Where(
                                r => r.Members.Contains(Context.ConnectionId) && r.State == PreState.Started
                            ).FirstOrDefault();
        if(room is not null)
            Engine.UpdateRoom(room.Id, new Message {
                Action = ActionType.ForfeitTurn,
                PlayerId = Context.ConnectionId
            }).FreeRooms();
    } 
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
    public async Task SetupRoom(string userId, string roomId, string roomName, string password){
        try
        {
            Engine.CreateRoom(
                dealer : new Player(userId, Users[userId]),
                name : roomName,
                connectionId : roomId,
                isLocked : !String.IsNullOrWhiteSpace(password),
                password : password
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
        var SendGameIndicators = async (string playerId, int? card) => {
            await Clients.Clients(playerId).SendAsync(
                MessageType.GetIndicator.ToString(), new Indicator(1, "It your turn Now!"));
            await Clients.Group(roomId).SendAsync(
                MessageType.GetIndicator.ToString(), new Indicator(2, card is not null ? $"claimed card : {card}" : "No card claimed"));
            if(action.Action == ActionType.QuestionCredibility)
            {
                string message1 = "", message2 = "";
                if(action.PlayerId != playerId){
                    (message1, message2) = ("You Lost, He told the truth!!", "He Lost!!");
                } else if (action.PlayerId == playerId) {
                    (message1, message2) = ("You're right, He Lied!!", "He was Right!!");
                }
                await Clients.Clients(playerId).SendAsync(
                    MessageType.GetIndicator.ToString(), new Indicator(3, message1));
                await Clients.GroupExcept(roomId, playerId).SendAsync(
                    MessageType.GetIndicator.ToString(), new Indicator(4, message2));
            }
        };
        try{
            var Room = Engine.UpdateRoom(roomId, action).Rooms[roomId];
            await Clients.Group(roomId).SendAsync(
                MessageType.GetState.ToString(), Room);
            await SendGameIndicators(Room.State.CurrentPlayer, Room.State.ClaimedCard);
        } 
        catch (GameEndedException end)
        {
            await Clients.Group(roomId).SendAsync(
                MessageType.GetNotification.ToString(), end.Message(Users[end.WinnerID]));
            await Task.Delay(10*1000);
            await Clients.Group(roomId).SendAsync(
                MessageType.GetGameEnded.ToString(), roomId);
            
        }
        catch(Exception e) {
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
    public async Task AddToGroup(string roomId, string userId, string password)
    {
        try{

            if(Users.ContainsKey(userId)) {
                OpenRooms
                    .Where(r => r.Id != roomId && r.OwnerId != userId)
                    .ToList().ForEach(async room => {
                        await RemoveFromGroup(room.Id, userId);
                        Engine.QuitRoom(room.Id, userId );
                    });
                
                Engine.JoinRoom(roomId, new Player(userId, Users[userId]), password);
                await Groups.AddToGroupAsync(userId, roomId);
                await Clients.Client(userId).SendAsync(
                    MessageType.GetNotification.ToString(), 
                    $"You Joined Room {OpenRooms?.Find(room => room.Id == roomId)?.Name}");
                await Clients.Group(roomId).SendAsync(
                    MessageType.GetNotification.ToString(), 
                    $"Player {Users[userId]} Joined!!");
                await Clients.All.SendAsync(
                    MessageType.GetUpdate.ToString(), OpenRooms);
            } else {
                await Clients.Clients(userId).SendAsync(
                    MessageType.GetNotification.ToString(), "Create Account First");
            }
        } catch(Exception e) {
            await Clients.Clients(userId).SendAsync(
                MessageType.GetNotification.ToString(), e.Message);
        }
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