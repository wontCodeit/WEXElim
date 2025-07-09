using Eliminator;

namespace EliminatorApp.Test;
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
        var stateMachine = new GameStateMachine(new HandManager(4, 4, new BlankDeck(1)), 0);

        Assert.True(stateMachine.CurrentState == GameState.Initialisation);
    }
}
