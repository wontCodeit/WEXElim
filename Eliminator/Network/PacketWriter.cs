using System.Diagnostics;
using System.Text;

namespace Eliminator;

public static class PacketWriter
{
    private static readonly List<byte> _writtenPacket = [];

    private static byte[] RetrievePacket()
    {
        var packet = _writtenPacket.ToArray();
        _writtenPacket.Clear();
        return packet;
    }

    #region WriteSpecificDataType
    private static void WriteToPacket(bool data) => _writtenPacket.AddRange(BitConverter.GetBytes(data));
    private static void WriteToPacket(byte data) => _writtenPacket.Add(data);
    private static void WriteToPacket(OpCode opCode) => WriteToPacket((byte)opCode);
    private static void WriteToPacket(CardValue cardValue) => WriteToPacket((byte)cardValue);
    private static void WriteToPacket(ushort data) => _writtenPacket.AddRange(BitConverter.GetBytes(data));
    private static void WriteToPacket(int data) => _writtenPacket.AddRange(BitConverter.GetBytes(data));
    private static void WriteToPacket(string data)
    {
        WriteToPacket(data.Length);
        _writtenPacket.AddRange(Encoding.UTF8.GetBytes(data));
    }
    #endregion

    /// <summary>
    /// Make a packet that tells the client the id the server has assigned it
    /// </summary>
    /// <param name="id"> Client's id </param>
    /// <returns> The packet as a <see cref="byte"/> array </returns>
    public static byte[] WriteAssignIdPacket(byte id)
    {
        WriteToPacket(OpCode.AssignId);
        WriteToPacket(id);

        return RetrievePacket();
    }

    /// <summary>
    /// Make a packet that tells the server the client's requested username
    /// </summary>
    /// <param name="username"> Client's requested username </param>
    /// <returns> The packet as a <see cref="byte"/> array </returns>
    public static byte[] WriteConnectionPacket(string username)
    {
        WriteToPacket(OpCode.Connect);
        WriteToPacket(username);

        return RetrievePacket();
    }

    /// <summary>
    /// Make a packet that tells the client whether their username/connection was accepted and why if not
    /// </summary>
    /// <param name="success"> Whether their username/connection was accepted </param>
    /// <param name="errorMessage"> The reason for rejection </param>
    /// <returns> The packet as a <see cref="byte"/> array </returns>
    public static byte[] WriteConnectionResponsePacket(bool success, ErrorMessage? errorMessage)
    {
        Debug.Assert(success || errorMessage is not null, "If and only if the connection is rejected, must return an error message");

        WriteToPacket(OpCode.ConnectionResponse);
        WriteToPacket(success);

        if (!success)
        {
            WriteToPacket((byte)errorMessage!);
        }

        return RetrievePacket();
    }

    /// <summary>
    /// Make a packet that tells the client the number of players and their associated ids and names
    /// </summary>
    /// <param name="startingCards"> The number of cards each player should start with </param>
    /// <param name="deckSize"> The multiple of 54 standard cards that the deck contains i.e. 1 = 54 cards in the deck, 2 = 108 </param>
    /// <param name="turnTimeLimit"> The time limit on a player's turn in seconds </param>
    /// <param name="users"> List of players ids + usernames </param>
    /// <returns> The packet as a <see cref="byte"/> array </returns>
    public static byte[] WriteInitialiseGamePacket(int startingCards, int deckSize, int turnTimeLimit, List<(byte, string)> users)
    {
        WriteToPacket(OpCode.InitialiseGame);
        WriteToPacket(startingCards);
        WriteToPacket(deckSize);
        WriteToPacket(turnTimeLimit);
        WriteToPacket((byte)users.Count);
        users.ForEach(user =>
        {
            WriteToPacket(user.Item1);
            WriteToPacket(user.Item2);
        });

        return RetrievePacket();
    }

    /// <summary>
    /// Make a packet that tells the client the id of the disconnected player
    /// </summary>
    /// <param name="playerId"> id of disconnected player </param>
    /// <returns> The packet as a <see cref="byte"/> array </returns>
    public static byte[] WriteDisconnectionPacket(byte playerId)
    {
        WriteToPacket(OpCode.Disconnection);
        WriteToPacket(playerId);

        return RetrievePacket();
    }

    /// <summary>
    /// Make a packet that tells the server the id of a card to QuickPlace and what the <see cref="CardValue"/>
    /// the client sees at the top of the discard pile
    /// </summary>
    /// <param name="cardId"> id of card to QuickPlace </param>
    /// <param name="clientKnownCard"> the <see cref="CardValue"/> the client sees on the discard pile </param>
    /// <returns> The packet as a <see cref="byte"/> array </returns>
    public static byte[] WriteQuickPlacePacket(ushort cardId, CardValue clientKnownCard)
    {
        WriteToPacket(OpCode.QuickPlace);
        WriteToPacket(cardId);
        WriteToPacket(clientKnownCard);

        return RetrievePacket();
    }

    /// <summary>
    /// Make a packet that tells the client the result of a QuickPlace and who did it
    /// </summary>
    /// <param name="result"> Result of the attempted QuickPlace </param>
    /// <param name="playerId"> Id of who attempted the QuickPlace </param>
    /// <param name="cardValue"> Success or fail, everyone sees what the quick placed card value was </param>
    /// <returns> The packet as a <see cref="byte"/> array </returns>
    public static byte[] WriteQuickPlaceResultPacket(QuickPlaceSuccess result, byte playerId, CardValue cardValue)
    {
        WriteToPacket(OpCode.QuickPlaceResult);
        WriteToPacket((byte)result);
        WriteToPacket(playerId);
        WriteToPacket(cardValue);

        return RetrievePacket();
    }

    /// <summary>
    /// Make a packet that tells the client to start their turn
    /// </summary>
    /// <param name="playerId"> Id of the player who should start their turn </param>
    /// <returns> The packet as a <see cref="byte"/> array </returns>
    public static byte[] WriteStartTurnPacket(byte playerId)
    {
        WriteToPacket(OpCode.StartTurn);
        WriteToPacket(playerId);

        return RetrievePacket();
    }

    /// <summary>
    /// Make a packet that tells the server you request to draw a card
    /// </summary>
    /// <returns> The packet as a <see cref="byte"/> array </returns>
    public static byte[] WriteDrawPacket()
    {
        WriteToPacket(OpCode.Draw);

        return RetrievePacket();
    }

    /// <summary>
    /// Make a packet that tells the client what card they drew
    /// </summary>
    /// <param name="cardValue"> The <see cref="CardValue"/> of the drawn card </param>
    /// <returns> The packet as a <see cref="byte"/> array </returns>
    public static byte[] WriteDrawResultPacket(CardValue cardValue)
    {
        WriteToPacket(OpCode.DrawResult);
        WriteToPacket(cardValue);

        return RetrievePacket();
    }

    /// <summary>
    /// Tells the client they should show the turn player has drawn a card
    /// </summary>
    /// <returns> The packet as a <see cref="byte"/> array </returns>
    public static byte[] WriteDisplayDrawPacket()
    {
        WriteToPacket(OpCode.DisplayDraw);

        return RetrievePacket();
    }

    /// <summary>
    /// Tells the server the client is requesting to discard their currently drawn card
    /// </summary>
    /// <returns> The packet as a <see cref="byte"/> array </returns>
    public static byte[] WriteDiscardPacket()
    {
        WriteToPacket(OpCode.Discard);

        return RetrievePacket();
    }

    /// <summary>
    /// Tells the client the <see cref="CardValue"/> of the discarded card
    /// </summary>
    /// <param name="cardValue"> the <see cref="CardValue"/> of the discarded card </param>
    /// <returns> The packet as a <see cref="byte"/> array </returns>
    public static byte[] WriteDiscardResultPacket(CardValue cardValue)
    {
        WriteToPacket(OpCode.DiscardResult);
        WriteToPacket(cardValue);

        return RetrievePacket();
    }

    /// <summary>
    /// To tell the server which two cards to have swap <see cref="CardValue"/>s
    /// </summary>
    /// <param name="cardId1"> id of one of the cards that will swap <see cref="CardValue"/> </param>
    /// <param name="cardId2"> id of the other card that will swap <see cref="CardValue"/> </param>
    /// <returns> The packet as a <see cref="byte"/> array </returns>
    public static byte[] WriteSwapPacket(ushort cardId1, ushort cardId2)
    {
        WriteToPacket(OpCode.Swap);
        WriteToPacket(cardId1);
        WriteToPacket(cardId2);

        return RetrievePacket();
    }

    /// <summary>
    /// To tell the client which two cards that have swapped <see cref="CardValue"/>s
    /// </summary>
    /// <param name="cardId1"> id of one of the cards that has swapped <see cref="CardValue"/> </param>
    /// <param name="cardId2"> id of the other card that has swapped <see cref="CardValue"/> </param>
    /// <returns> The packet as a <see cref="byte"/> array </returns>
    public static byte[] WriteDisplaySwapPacket(ushort cardId1, ushort cardId2)
    {
        WriteToPacket(OpCode.DisplaySwap);
        WriteToPacket(cardId1);
        WriteToPacket(cardId2);

        return RetrievePacket();
    }

    /// <summary>
    /// Tells the server what card is being peeked
    /// </summary>
    /// <param name="cardId"> The id of the card to be peeked </param>
    /// <returns> The packet as a <see cref="byte"/> array </returns>
    public static byte[] WritePeekPacket(ushort cardId)
    {
        WriteToPacket(OpCode.Peek);
        WriteToPacket(cardId);

        return RetrievePacket();
    }

    /// <summary>
    /// Tells the client the <see cref="CardValue"/> of the card that has been peeked
    /// </summary>
    /// <param name="cardValue"> the <see cref="CardValue"/> of the peeked card </param>
    /// <returns> The packet as a <see cref="byte"/> array </returns>
    public static byte[] WritePeekResultPacket(CardValue cardValue)
    {
        WriteToPacket(OpCode.PeekResult);
        WriteToPacket(cardValue);

        return RetrievePacket();
    }

    /// <summary>
    /// Tells the client what card is being peeked 
    /// </summary>
    /// <param name="cardValue"> The id of the peeked card </param>
    /// <returns> The packet as a <see cref="byte"/> array </returns>
    public static byte[] WriteDisplayPeekPacket(ushort cardId)
    {
        WriteToPacket(OpCode.DisplayPeek);
        WriteToPacket(cardId);

        return RetrievePacket();
    }

    /// <summary>
    /// Tells the server the id of the player who's cards should have their <see cref="CardValue"/>s scrambled
    /// </summary>
    /// <param name="playerId"> Id of the player who will be scrambled </param>
    /// <returns> The packet as a <see cref="byte"/> array </returns>
    public static byte[] WriteScramblePacket(byte playerId)
    {
        WriteToPacket(OpCode.Scramble);
        WriteToPacket(playerId);

        return RetrievePacket();
    }

    /// <summary>
    /// Tells the client the id of the player who's cards have had their <see cref="CardValue"/>s scrambled
    /// </summary>
    /// <param name="playerId"> Id of the player who has been scrambled </param>
    /// <returns> The packet as a <see cref="byte"/> array </returns>
    public static byte[] WriteDisplayScramblePacket(byte playerId)
    {
        WriteToPacket(OpCode.DisplayScramble);
        WriteToPacket(playerId);

        return RetrievePacket();
    }

    /// <summary>
    /// Tells the server you have elected to end your turn early
    /// </summary>
    /// <returns> The packet as a <see cref="byte"/> array </returns>
    public static byte[] WritePassTurnPacket()
    {
        WriteToPacket(OpCode.PassTurn);

        return RetrievePacket();
    }

    /// <summary>
    /// Tells the client that the turn player has forfeited their turn (time constraints)
    /// </summary>
    /// <returns> The packet as a <see cref="byte"/> array </returns>
    public static byte[] WriteForceEndTurnPacket()
    {
        WriteToPacket(OpCode.ForceEndTurn);

        return RetrievePacket();
    }

    /// <summary>
    /// Tells the server that you are calling it
    /// </summary>
    /// <returns> The packet as a <see cref="byte"/> array </returns>
    public static byte[] WriteCallItPacket()
    {
        WriteToPacket(OpCode.CallIt);

        return RetrievePacket();
    }

    /// <summary>
    /// Tells the client that the turn player has called it
    /// </summary>
    /// <returns> The packet as a <see cref="byte"/> array </returns>
    public static byte[] WriteCalledItPacket()
    {
        WriteToPacket(OpCode.CalledIt);

        return RetrievePacket();
    }

    /// <summary>
    /// Tells the client the score of each associated player and signals the end of the game
    /// </summary>
    /// <param name="scores"> List of player id and their score </param>
    /// <returns></returns>
    public static byte[] WriteGameEndPacket(List<(byte, int)> scores)
    {
        WriteToPacket(OpCode.GameEnd);
        WriteToPacket((byte)scores.Count);
        scores.ForEach(score =>
        {
            WriteToPacket(score.Item1);
            WriteToPacket(score.Item2);
        });

        return RetrievePacket();
    }
}