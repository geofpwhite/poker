using System.Text.Json;
using System.Text.Json.Serialization;

namespace Poker.Models
{
    public enum Card
    {
        TwoH, TwoD, TwoC, TwoS,
        ThreeH, ThreeD, ThreeC, ThreeS,
        FourH, FourD, FourC, FourS,
        FiveH, FiveD, FiveC, FiveS,
        SixH, SixD, SixC, SixS,
        SevenH, SevenD, SevenC, SevenS,
        EightH, EightD, EightC, EightS,
        NineH, NineD, NineC, NineS,
        TenH, TenD, TenC, TenS,
        JackH, JackD, JackC, JackS,
        QueenH, QueenD, QueenC, QueenS,
        KingH, KingD, KingC, KingS,
        AceH, AceD, AceC, AceS
    }

    public enum CardValue
    {
        Two, Three, Four, Five, Six, Seven, Eight, Nine, Ten, Jack, Queen, King, Ace
    }

    public enum CardSuit
    {
        Hearts, Diamonds, Clubs, Spades
    }

    public class CardJsonConverter : JsonConverter<Card[]>
    {

        public override bool CanConvert(Type typeToConvert)
        {
            return true;
        }
        public override Card[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException("Deserialization is not supported.");
        }

        public override void Write(Utf8JsonWriter writer, Card[] value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            foreach (var card in value)
            {
                writer.WriteStringValue(card.ToString());
            }
            writer.WriteEndArray();
        }
    }
}


