using FishNet;
using FishNet.Connection;
using FishNet.Transporting;
using UnityEngine;

public class LocalGameManager : MonoBehaviour
{
    public static LocalGameManager Instance {get; private set;}

    private void Awake()
    {
        Instance = this;

        if (InstanceFinder.IsServerStarted)
        {
            InstanceFinder.ServerManager.OnRemoteConnectionState += OnPlayerLeave;
        }
        else
        {
            InstanceFinder.ClientManager.OnClientConnectionState += OnPlayerLeave;
        }
    }

    public void GameOver(bool winner, string reason)
    {
        Board.Instance.GameOver = true;
        Debug.Log("game over: " + reason);
        GameOverUI.Instance.Show(winner, reason);
    }

    private void OnPlayerLeave(ClientConnectionStateArgs args)
    {
        Debug.Log(args.ConnectionState);
        if (args.ConnectionState == LocalConnectionState.Stopped && LobbyManager.Instance.InGame)
        {
            Debug.Log("Disconnection caught");
            GameOver(true, "By disconnection");
        }
    }
    
    private void OnPlayerLeave(NetworkConnection connection, RemoteConnectionStateArgs args)
    {
        if (args.ConnectionState == RemoteConnectionState.Stopped && InstanceFinder.IsHostStarted && LobbyManager.Instance.InGame)
        {
            Debug.Log("Disconnection caught");
            GameOver(true, "By disconnection");
        }
    }
}
