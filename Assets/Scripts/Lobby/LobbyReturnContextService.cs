using IGameFlowInterface;
using UnityEngine;

public class LobbyReturnContextService : GlobalServiceBase, ILobbyReturnContextService
{
    public LobbyOpenRequest CurrentRequest { get; private set; } = LobbyOpenRequest.None;

    public void Request(LobbyOpenRequest request)
    {
        if (CurrentRequest == request) return;

        CurrentRequest = request;
    }
    public LobbyOpenRequest Consume()
    {
        LobbyOpenRequest request = CurrentRequest;
        CurrentRequest = LobbyOpenRequest.None;
        return request;
    }
    public void Clear()
    {
        CurrentRequest = LobbyOpenRequest.None;
    }

}
