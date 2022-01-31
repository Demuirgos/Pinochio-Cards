namespace Game.Models;
public abstract record Message{}
public record ChatMessage(string sender, string message);
public record PlaceCard : Message {
    public int PlayerId { get; init; }
    public int Card { get; init; }
    public int Claim {get; set; }
}
public record QuestionCredibility : Message {
    public int PlayerId { get; init; }
}

public record ForfeitTurn : Message {
    public int PlayerId { get; init; }
}