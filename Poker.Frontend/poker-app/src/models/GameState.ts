
export interface GameState {
    CommunityCards: string[];
    Players: Player[];
    Pot: number;
    Turn: number;
    CurrentBet: number;
    Dealer: number;
}
export interface Player {
    Id: string;
    Cards: string[];
    Folded: boolean;
    LastBet: number;
    Chips: number;
    Name: string;
}