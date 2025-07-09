namespace Eliminator;
public enum GameState
{
    Initialisation, // State occupied in between connecting with the server and receiving the game start info
    Waiting, // State occupied when it is not the player's turn
    QuickPlace, // Once a card is selected it may be placed upon the top of the discard pile (like in snap)
    TurnStart, // State occupied when the player's turn starts
    DeckDraw, // Once a card is drawn from the deck, it may be swapped with one card in a hand or discarded
    DiscardSwap, // Swap a card in hand with the top of the discard pile
    PeekSelf, // Look at only cards in player's own hand
    PeekOther, // Look at any card in any other player's hand
    SwapCardInHands, // Swap a card in two player hands
    Scramble, // Scramble the value of cards
    TurnEnd // State occupied after drawing a card for turn and no card action is active
}
