using System;
using Room = System.Collections.Generic.List<Game.Models.Player>;

namespace Game.Models;
public partial class GameSession
{
    public GameSession Create(Player dealer){
        return new GameSession {
            RoomId = Guid.NewGuid(),
            State = PreState.Pending,
            Dealer = dealer,
            Waiting = new()
        };
    }
    public void Add(Player player) => this.Waiting?.Add(player);
    public void Remove(Player player) => this.Waiting?.Remove(player);
    public void Start() => this.State = PreState.Started;
    public void Pause() => this.State = PreState.Halted;
    public void Stop()  => this.State = PreState.Pending;
}

// we have the stack in the board
// we have each players deque
// we have the full deque
// we have the current player