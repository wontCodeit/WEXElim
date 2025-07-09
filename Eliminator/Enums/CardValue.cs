using System.Diagnostics;

namespace Eliminator;
public enum CardValue: byte
{
    Back = 0,
    SpadesAce = 1,
    SpadesTwo = 2,
    SpadesThree = 3,
    SpadesFour = 4,
    SpadesFive = 5,
    SpadesSix = 6,
    SpadesSeven = 7,
    SpadesEight = 8,
    SpadesNine = 9,
    SpadesTen = 10,
    SpadesJack = 11,
    SpadesQueen = 12,
    SpadesKing = 13,
    HeartsAce = 14,
    HeartsTwo = 15,
    HeartsThree = 16,
    HeartsFour = 17,
    HeartsFive = 18,
    HeartsSix = 19,
    HeartsSeven = 20,
    HeartsEight = 21,
    HeartsNine = 22,
    HeartsTen = 23,
    HeartsJack = 24,
    HeartsQueen = 25,
    HeartsKing = 26,
    DiamondsAce = 27,
    DiamondsTwo = 28,
    DiamondsThree = 29,
    DiamondsFour = 30,
    DiamondsFive = 31,
    DiamondsSix = 32,
    DiamondsSeven = 33,
    DiamondsEight = 34,
    DiamondsNine = 35,
    DiamondsTen = 36,
    DiamondsJack = 37,
    DiamondsQueen = 38,
    DiamondsKing = 39,
    ClubsAce = 40,
    ClubsTwo = 41,
    ClubsThree = 42,
    ClubsFour = 43,
    ClubsFive = 44,
    ClubsSix = 45,
    ClubsSeven = 46,
    ClubsEight = 47,
    ClubsNine = 48,
    ClubsTen = 49,
    ClubsJack = 50,
    ClubsQueen = 51,
    ClubsKing = 52,
    JokerBlack = 53,
    JokerColour = 54
}

public static class Extensions
{
    public static CardAction GetCardAction(this CardValue card)
    {
        Debug.Assert((byte)card is not 0 or > 54, "Cannot GetCardAction for a card which isn't found in a standard deck.");
        var num = (byte)card % 13;

        // RedKing only
        if (num == 0 && (byte)card != 13 && (byte)card != 52)
        {
            return CardAction.Scramble;
        }

        // J and Q
        if (num > 10)
        {
            return CardAction.Swap;
        }

        // 9 and 10
        if (num > 8)
        {
            return CardAction.PeekOther;
        }

        // 7 and 8
        if (num > 6)
        {
            return CardAction.PeekSelf;
        }

        // All others
        return CardAction.None;
    }
}

