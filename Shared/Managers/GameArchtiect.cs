using System;
using Game.Models;
namespace Game.Architecture;
public class Manager {
    public Dictionary<string, Metadata>? Rooms {get; set;} = new();
    public void CreateRoom(Player dealer, string name, string connectionId){
        if(Rooms?.Values?.Where((m) => m.Session.Dealer == dealer).Any() ?? false)
            throw new Exception("A dealer can only Have one Room");
        Rooms?.Add(connectionId , new Metadata(
            Session : new GameSession {
                RoomName = name,
                RoomId = Guid.NewGuid(),
                State = PreState.Pending,
                Dealer = dealer,
                Waiting = new(){
                    dealer
                }
            }, 
            State : null
        ));
    }
    public void JoinRoom(string roomId, Player player){
        Rooms[roomId].Session.Add(player);
    }
    public void QuitRoom(string roomId, Player player){
        Rooms[roomId].Session.Remove(player);
    }
    public void InitiateRoom(string roomId){
        Rooms[roomId].Session.Pause();
        Rooms[roomId].State.Initiate(Rooms[roomId].Session);
    }
    public void StartGame(string roomId) {
        Rooms[roomId].Session.Start();
        Rooms[roomId].State.Start();
    }

    public void CloseRoom(string roomId, string userId){
        if(Rooms[roomId].Session.Dealer.Id == userId)
            Rooms.Remove(roomId);
    }
}