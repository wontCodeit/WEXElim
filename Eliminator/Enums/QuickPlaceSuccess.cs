namespace Eliminator;
public enum QuickPlaceSuccess: byte
{
    Success = 0, // Player made a successful QuickPlace
    Failure = 1, // Player made a failed QuickPlace
    TooLate = 2, // Client's discard pile was not up to date with server's discard pile
}
