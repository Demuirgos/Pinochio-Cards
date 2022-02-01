namespace Game.Models;

public enum ActionType {
    PlaceCard,
    QuestionCredibility,
    ForfeitTurn
}
public record Message{
    public ActionType Action {get; init;}
    public string PlayerId { get; init; }
    public int Card { get; init; }
    public int Claim {get; set; }
}
public record ChatMessage(string sender, string message);