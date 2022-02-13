using System;
using System.ComponentModel;
using System.Data;
using System.Security.Cryptography.X509Certificates;
using Game.Models;
namespace Game.Architecture;
public class Manager {
    public Dictionary<string, Metadata>? Rooms {get; set;} = new();
    public Manager UpdateRoom(string roomId, Message message) {
        Rooms[roomId].State.Update(message);
        return this;
    }
    public Manager CreateRoom(Player dealer, int dups, string name, string connectionId, bool isLocked, string password){
        if(Rooms?.Values?.Where((m) => m.Session.Dealer == dealer).Any() ?? false)
            throw new Exception("A dealer can only Have one Room");
        Rooms?.Add(connectionId , new Metadata(
            Session : new GameSession {
                RoomName = name,
                SessionType = isLocked ? GameSession.Type.Locked : GameSession.Type.Public,
                Password = password,
                PlayerCount = dups,
                RoomId = connectionId,
                State = PreState.Pending,
                Dealer = dealer,
                Waiting = new(){
                    dealer
                }
            }, 
            State : new GameState()
        ));
        return this;
    }
    public Manager JoinRoom(string roomId, Player player, string? password = null){
        switch (Rooms[roomId].Session.SessionType)
        {
            case GameSession.Type.Locked :
                if(Rooms[roomId].Session.Password == password)
                    Rooms[roomId].Session.Add(player);
                else throw new Exception($"Room Password Incorrect");
                return this;
            case GameSession.Type.Public :
                Rooms[roomId].Session.Add(player);
                return this;
            default : return this;
        }
    }
    public Manager QuitRoom(string roomId, string playerId){
        var playerToBeRemoved = Rooms[roomId].Session.Waiting.FirstOrDefault(p => p.Id == playerId);
        Rooms[roomId].Session.Remove(playerToBeRemoved);
        return this;
    }
    public Manager InitiateRoom(string roomId){
        if(Rooms[roomId].Session.Waiting.Count < 4 )
            throw new Exception("Game Cannot start with players < 4") ;
        Rooms[roomId].Session.Pause();
        Rooms[roomId].State.Initiate(Rooms[roomId].Session);
        return this;
    }

    public Manager ResetRoom(string roomId){
        Rooms[roomId].Session.State = PreState.Pending;
        Rooms[roomId] = new Metadata(
            Session : Rooms[roomId].Session,
            State : new GameState()
        );
        return this;
    }
    public Manager StartGame(string roomId) {
        if(Rooms[roomId].Session.Waiting.Count < 4 )
            throw new Exception("Game Cannot start with players < 4") ;
        Rooms[roomId].Session.Start();
        Rooms[roomId].State.Start();
        return this;
    }

    public Manager CloseRoom(string roomId, string userId){
        if(Rooms[roomId].Session.Dealer.Id == userId)
            Rooms.Remove(roomId);
        return this;
    }

    public Manager FreeRooms(){
        Rooms = Rooms.Where(r => r.Value.State.Players.Count !=0).ToDictionary(r => r.Key, r => r.Value);
        return this;
    }
}