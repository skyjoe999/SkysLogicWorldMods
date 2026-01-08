using LogicAPI.Server.Components;

namespace SkysChallengeSystem.Server.Components;

public interface IChallengeRoutingAccess
{
    void ConnectAnswer(string name, IInputPeg peg);
    void ConnectQuestion(string name, IOutputPeg peg);
    void DisconnectAnswer(string name, IInputPeg peg);
    void DisconnectQuestion(string name, IOutputPeg peg);
    void ClearConnections();
}