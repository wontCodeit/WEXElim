namespace Eliminator;
public enum OpCode: byte
{
    AssignId, // to contain a client's id so packets sent can be identified (note that server is always 255, clients from 0-254)
    Connect, // to contain message length and username
    ConnectionResponse, // to contain success bool, then if not successful error message enum
    InitialiseGame, // to contain game config settings- number of cards, deck size, turn time cut off
                    // and number of players then for each their id and name

    Disconnection, // to contain a player id informing the client that said player has lost connection

    QuickPlace, // to contain what id card is placed and what the user sees on the discard pile
    QuickPlaceResult, // a byte enum for success/fail/too late and id of who did it - if success, client is playing and they did it they should await a discard result

    StartTurn, // to contain user id who should start their turn, sent to all so everyone knows who's turn it is

    Draw, // to contain only OpCode as a request from client to draw a card
    DrawResult, // to contain a card value (id is known as they simply progress in sequence)
    DisplayDraw, // to contain only OpCode to prompt waiting clients to display a card draw

    Discard, // to contain only an OpCode as a request to discard currently drawn card
    DiscardResult, // to contain Card Value of discarded card (necessary to tell all clients the discard value, even if turn player knows from draw)

    Swap, // to contain two card ids that are requested to swap
    DisplaySwap, // to contain two card ids that clients should display swapping (since Card Values are swapped, not ids) 

    Peek, // to contain a card id to peek (client must handle validity)
    PeekResult, // to contain Card Value peeked
    DisplayPeek, // to contain a card id that clients should see is being peeked

    Scramble, // to contain the player id that is requested to scramble
    DisplayScramble, // to display that a scramble has occurred (since Card Values are scrambled, not ids)

    PassTurn, // to contain only an OpCode stating that the client elects to end their turn early
    ForceEndTurn, // to contain only an OpCode stating that the client has forfeited their turn (time constraints)

    CallIt, // to contain only an OpCode stating the client is calling it
    CalledIt, // to contain only an OpCode informing all other clients that the turn player has called it

    GameEnd, // to contain a list of player ids and hand scores, from which the client can determine winner, loser and display end screen
}
