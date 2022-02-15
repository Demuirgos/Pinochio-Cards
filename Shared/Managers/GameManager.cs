namespace Game.Models;
[System.Serializable]
public class GameEndedException : System.Exception {
    public String WinnerID { get; set; }
    public GameEndedException(String winnerID) {
        WinnerID = winnerID;
    }
    public new string Message(string name) => $"The winner is {name}";
}
public partial class GameState 
{
    public event Action<GameState>? OnStateChanged;
    public void Initiate(GameSession session) {
        this.ClaimedCard = null;
        this.Deck = CardsActions.Fill(session.Waiting.Count);
        this.Players = session.Waiting;  
    } 
    public void Start() => (this.Deck, this.PlayerDecks) = this.Deck.Distribute(this.Players);
    public string CurrentPlayer  => this.Players[this.Turn].Id;
    public int NextPlayer     => (this.Turn + 1) % Players.Count;
    public int PreviousPlayer => (this.Turn  - 1 + Players.Count) % Players.Count;
    public GameState HandleBurns() {
        foreach (var (id, deck) in PlayerDecks)
        {
            deck.GroupBy(i => i)
                .GroupBy(g => g.Count() % this.Players.Count == 0)
                .ToList().ForEach(pool => 
                    {
                        if(pool.Key)
                            this.Deck.AddRange(pool.SelectMany(i => i));
                        else
                            PlayerDecks[id] = pool.SelectMany(g => g.Chunk(this.Players.Count).ToArray()[^1]).ToList();
                    });                    
        }
        return this;
    }

    public GameState CheckEnd() {
        if(Players.Count == 1) throw new GameEndedException(Players[0].Id);
        foreach (var player in PlayerDecks)
            if(player.Value.Count == 0) 
                throw new GameEndedException(player.Key); // workarround 
        return this;
    }

    public GameState Update (Message message)
    {
        if(message.Action != ActionType.ForfeitTurn && message.PlayerId != this.CurrentPlayer)
            throw new Exception("It's not your turn");
        Func<GameState> handler = message.Action switch {
            ActionType.PlaceCard => () => {
                this.PlayerDecks[this.CurrentPlayer].Remove(message.Card);
                this.ClaimedCard = this.Board.Count > 0 ? this.ClaimedCard : message.Claim;
                this.Board.Push(message.Card);
                this.Turn = this.NextPlayer;
                return HandleBurns().CheckEnd();
            },
            ActionType.QuestionCredibility => () => {
                var (currClaim, currCard) = (this.ClaimedCard, this.Board.Peek());
                if (currClaim == currCard) {
                    while(Board.Count > 0) {
                        this.PlayerDecks[this.CurrentPlayer].Add(this.Board.Pop());
                    }
                    this.Turn = this.NextPlayer;
                } else {
                    while(Board.Count > 0) {
                        this.PlayerDecks[this.Players[this.PreviousPlayer].Id].Add(this.Board.Pop());
                    }
                }
                this.ClaimedCard = null;
                return HandleBurns().CheckEnd();
            },
            ActionType.ForfeitTurn => () => {
                var burntDeck = this.PlayerDecks[message.PlayerId];
                //remove player from room
                this.Players.Remove(this.Players.First(p => p.Id == message.PlayerId));
                this.PlayerDecks.Remove(message.PlayerId);
                //distribute his cards to other players
                int idx = 0;
                while(burntDeck.Count > 0){
                    this.PlayerDecks[this.Players[idx].Id].Add(burntDeck.Last());
                    burntDeck.RemoveAt(burntDeck.Count - 1);
                    idx = (idx + 1) % this.Players.Count;
                }
                if(this.CurrentPlayer == message.PlayerId)
                    this.Turn = this.NextPlayer;
                return HandleBurns().CheckEnd();
            },
            _ => throw new NotImplementedException()
        };
        return handler();
    }    
}