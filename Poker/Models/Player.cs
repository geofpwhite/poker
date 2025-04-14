namespace Poker.Models;

public class Player
{
    public string ConnectionId { get; set; }

    public Player(string connectionId)
    {
        this.ConnectionId = connectionId;
    }

    public required string Name { get; init; }
    public int Chips { get; set; } = 0;
    public Card[] Cards { get; set; } = [];
    public bool Folded { get; set; } = false;

    // public Player(string name, int chips)
    // {
    //     Name = name;
    //     Chips = chips;
    // }
}
    

