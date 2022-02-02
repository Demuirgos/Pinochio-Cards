using System.Runtime.CompilerServices;
using Room = System.Collections.Generic.List<Game.Models.Player>;

namespace Game.Models;
public enum PreState { Started, Halted, Pending }
public record Player(string Id, string Name);
public record Metadata(GameSession Session, GameState? State);
public record RoomsData(string Id, string OwnerId, string Name, string[] Members, int Size, PreState State);
public partial class GameState {
    public int? ClaimedCard { get; set; } = null;
    public List<int> Deck {get; set;} = new();
    public Stack<int> Board {get; set;} = new();
    public Dictionary<string ,List<int>> PlayerDecks {get; set;} = new();
    public int Turn {get; set;}
    public Room Players {get; set;} = new();
}

public partial class GameSession {
    public string RoomName {get; set;}
    public string RoomId {get; set;}
    public PreState State {get; set;}
    public Player? Dealer {get; set;}
    public Room? Waiting {get; set;}
}