using System;
using Room = System.Collections.Generic.List<Game.Models.Player>;

namespace Game.Models;
public partial class GameState 
{
    public void Initiate(GameSession session) {
        this.ClaimedCard = null;
        this.Deck = CardsActions.Fill(session.Waiting.Count);
        this.Players = session.Waiting;  
    } 
    public void Start() => (this.Deck, this.PlayerDecks) = this.Deck.Distribute(this.Players.Count);
    public int NextPlayer     => CurrentPlayer + 1 % Players.Count;
    public int PreviousPlayer => (CurrentPlayer - 1 + Players.Count) % Players.Count;
    public GameState Update(Message action)
    {
        Func<GameState> handler = action switch {
            PlaceCard message           => () => {
                this.PlayerDecks[this.CurrentPlayer].Remove(message.Card);
                this.ClaimedCard = this.Board.Count > 0 ? this.ClaimedCard : message.Claim;
                this.Board.Push(message.Card);
                this.CurrentPlayer = this.NextPlayer;
                return this;
            },
            QuestionCredibility message => () => {
                var (currClaim, currCard) = (this.ClaimedCard, this.Board.Peek());
                if (currClaim == currCard) {
                    while(Board.Count > 0) {
                        this.PlayerDecks[this.CurrentPlayer].Add(this.Board.Pop());
                    }
                } else {
                    while(Board.Count > 0) {
                        this.PlayerDecks[this.PreviousPlayer].Add(this.Board.Pop());
                    }
                    this.CurrentPlayer = this.NextPlayer;
                }
                return this;
            },
            ForfeitTurn message         => throw new NotImplementedException(),
            _ => throw new NotImplementedException()
        };
        return handler();
    }    
}

// we have the stack in the board
// we have each players deque
// we have the full deque
// we have the current player