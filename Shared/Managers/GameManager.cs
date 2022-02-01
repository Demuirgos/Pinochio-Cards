namespace Game.Models;
public partial class GameState 
{
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
        List<int> BurnedCards = new();
        foreach (var (id, deck) in PlayerDecks)
        {
            var ToBeBurned = deck.GroupBy(i => i).Where(g => g.Count() == this.Players.Count).Select(g => g.Key).ToList();
            PlayerDecks[id].RemoveAll(i => ToBeBurned.Contains(i));
            BurnedCards.AddRange(ToBeBurned);
        }
        this.Deck.AddRange(BurnedCards);
        return this;
    }
    public GameState Update (Message message)
    {
        if(message.PlayerId != this.CurrentPlayer)
            throw new Exception("It's not your turn");
        Func<GameState> handler = message.Action switch {
            ActionType.PlaceCard => () => {
                System.Console.WriteLine(PlayerDecks.Count);
                this.PlayerDecks[this.CurrentPlayer].Remove(message.Card);
                this.ClaimedCard = this.Board.Count > 0 ? this.ClaimedCard : message.Claim;
                this.Board.Push(message.Card);
                this.Turn = this.NextPlayer;
                return HandleBurns();
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
                return HandleBurns();
            },
            ActionType.ForfeitTurn => throw new NotImplementedException(),
            _ => throw new NotImplementedException()
        };
        return handler();
    }    
}

// we have the stack in the board
// we have each players deque
// we have the full deque
// we have the current player