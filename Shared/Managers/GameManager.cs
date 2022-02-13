namespace Game.Models;
[System.Serializable]
public class GameEndedException : System.Exception{
    public String WinnerID { get; set; }
    public GameEndedException(String winnerID) {
        WinnerID = winnerID;
    }
    public string Message(string name) => $"The winner is {name}";
}
public partial class GameState 
{
    public void Initiate(GameSession session) {
        this.ClaimedCard = null;
        this.DupCount = session.PlayerCount;
        this.Deck = CardsActions.Fill(session.Waiting.Count);
        this.Players = session.Waiting;  
    } 
    public void Start() => (this.Deck, this.PlayerDecks) = this.Deck.Distribute(this.Players);
    public string CurrentPlayer  => this.Players[this.Turn].Id;
    public int NextPlayer     => (this.Turn + 1) % Players.Count;
    public int PreviousPlayer => (this.Turn  - 1 + Players.Count) % Players.Count;
    public GameState HandleBurns() {
        List<int> BurnedCards = new();
        foreach (var (id, deck) in PlayerDecks)
        {
            var ToBeBurned = deck.GroupBy(i => i).Where(g => g.Count() % this.DupCount == 0).Select(g => g.Key).ToList();
            PlayerDecks[id].RemoveAll(i => ToBeBurned.Contains(i));
            BurnedCards.AddRange(ToBeBurned);
        }
        this.Deck.AddRange(BurnedCards);
        return this;
    }

    public GameState CheckEnd() {
        foreach (var player in PlayerDecks)
            if(player.Value.Count == 0) throw new GameEndedException(player.Key); // workarround 
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
                return HandleBurns().CheckEnd();
            },
            _ => throw new NotImplementedException()
        };
        return handler();
    }    
}

// we have the stack in the board
// we have each players deque
// we have the full deque
// we have the current player