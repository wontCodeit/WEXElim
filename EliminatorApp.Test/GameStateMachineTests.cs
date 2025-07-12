using Eliminator;

namespace EliminatorApp.Test;

/// <summary>
/// Class unfinished as I began using Stateless instead of rolling my own state machine
/// </summary>
public class GameStateMachineTests
{
    /// <summary>
    /// GIVEN:
    /// WHEN: <see cref = "GameStateMachine" /> instantiated
    /// THEN: Enters<see cref = "GameState.Initialisation" />
    /// </ summary >
    [Fact]
    public void BeginsWithInitialisation()
    {
        var stateMachine = new GameStateMachine(new HandManager(4, 4, new BlankDeck(1), new CardCounter()), 0);

        Assert.True(stateMachine.CurrentState == GameState.Initialisation);
    }
}
