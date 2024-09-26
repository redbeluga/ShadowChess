using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using FishNet.Managing;
using FishNet.Transporting.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using Random = UnityEngine.Random;

[Serializable]
public enum EncryptionType
{
    DTLS, // Datagram Transport Layer Security
    WSS // Web Socket Secure
}

public class LobbyManager : MonoBehaviour
{
    [SerializeField] string lobbyName = "Lobby";
    [SerializeField] int maxPlayers = 4;
    [SerializeField] public NetworkManager _networkManager;
    [SerializeField] EncryptionType encryption = EncryptionType.DTLS;

    public static LobbyManager Instance { get; private set; }

    public string PlayerId { get; private set; }
    public string PlayerName { get; private set; }
    private bool isHost = false;
    public string CurrentHostId { get; private set; }

    Lobby currentLobby;
    private string connectionType => encryption == EncryptionType.DTLS ? k_dtlsEncryption : k_wssEncryption;

    const float k_lobbyHeartbeatInterval = 15f;
    const float k_lobbyPollInterval = 1.1f;
    const string k_keyJoinCode = "RelayJoinCode";
    private const string k_hostID = "HostID";
    const string k_dtlsEncryption = "dtls"; // Datagram Transport Layer Security
    const string k_wssEncryption = "wss"; // Web Socket Secure, use for WebGL builds

    public event EventHandler<LobbyEventArgs> OnLeaveLobby;
    public event EventHandler<LobbyEventArgs> OnJoinLobby;
    
    public class LobbyEventArgs : EventArgs {
        public Lobby lobby;
    }

    private void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(this);
    }

    async void Start()
    {
        await Authenticate();
    }

    private void Update()
    {
        HandleHeartbeatAsync();
        HandlePollForUpdatesAsync();
    }

    async Task Authenticate()
    {
        await Authenticate("Player" + Random.Range(0, 1000));
    }

    async Task Authenticate(string playerName)
    {
        if (UnityServices.State == ServicesInitializationState.Uninitialized)
        {
            InitializationOptions options = new InitializationOptions();
            options.SetProfile(playerName);

            await UnityServices.InitializeAsync(options);
        }

        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log("Signed in as " + AuthenticationService.Instance.PlayerId);
        };

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            PlayerId = AuthenticationService.Instance.PlayerId;
            PlayerName = playerName;
        }
    }

    public async Task CreateLobby()
    {
        if (currentLobby != null) return;
        try
        {
            CurrentHostId = PlayerId;
            
            Allocation allocation = await AllocateRelay();
            string relayJoinCode = await GetRelayJoinCode(allocation);

            CreateLobbyOptions options = new CreateLobbyOptions
            {
                IsPrivate = false
            };

            currentLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);
            Debug.Log("Created lobby: " + currentLobby.Name + " with code " + currentLobby.LobbyCode);

            await LobbyService.Instance.UpdateLobbyAsync(currentLobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    { k_keyJoinCode, new DataObject(DataObject.VisibilityOptions.Member, relayJoinCode) },
                    { k_hostID, new DataObject(DataObject.VisibilityOptions.Member, PlayerId) }
                }
            });

            _networkManager.GetComponent<FishyUnityTransport>()
                .SetRelayServerData(new RelayServerData(allocation, connectionType));
            
            Debug.Log("INVOKING EVENT HANDLER");

            OnJoinLobby?.Invoke(this, new LobbyEventArgs { lobby = currentLobby });

            _networkManager.ServerManager.StartConnection();
            _networkManager.ClientManager.StartConnection();
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError("Failed to create lobby: " + e.Message);
        }
    }


    public async Task JoinLobby(string joinCode)
    {
        if (currentLobby != null) return;
        try
        {
            currentLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(joinCode);

            string relayJoinCode = currentLobby.Data[k_keyJoinCode].Value;
            JoinAllocation joinAllocation = await JoinRelay(relayJoinCode);

            CurrentHostId = currentLobby.Data[k_keyJoinCode].Value;

            _networkManager.GetComponent<FishyUnityTransport>()
                .SetRelayServerData(new RelayServerData(joinAllocation, connectionType));
            
            OnJoinLobby?.Invoke(this, new LobbyEventArgs { lobby = currentLobby });

            _networkManager.ClientManager.StartConnection();
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError("Failed to quick join lobby: " + e.Message);
        }
    }

    public async Task QuickJoinLobby()
    {
        if (currentLobby != null) return;
        try
        {
            currentLobby = await LobbyService.Instance.QuickJoinLobbyAsync();
            CurrentHostId = currentLobby.HostId;

            string relayJoinCode = currentLobby.Data[k_keyJoinCode].Value;
            JoinAllocation joinAllocation = await JoinRelay(relayJoinCode);

            _networkManager.GetComponent<FishyUnityTransport>()
                .SetRelayServerData(new RelayServerData(joinAllocation, connectionType));
            
            OnJoinLobby?.Invoke(this, new LobbyEventArgs { lobby = currentLobby });

            _networkManager.ClientManager.StartConnection();

            Debug.Log(_networkManager.IsServerStarted);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError("Failed to quick join lobby: " + e.Message);
        }
    }

    public async Task LeaveLobby()
    {
        if (currentLobby == null) return;
        try
        {
            await LobbyService.Instance.RemovePlayerAsync(currentLobby.Id, PlayerId);
            Debug.Log("Player left the lobby: " + currentLobby.Name);

            currentLobby = null;
            isHost = false;
            CurrentHostId = null;

            _networkManager.ClientManager.StopConnection();
            
            OnLeaveLobby?.Invoke(this, new LobbyEventArgs { lobby = currentLobby });
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError("Failed to leave lobby: " + e.Message);
        }
    }

    [ContextMenu("Get Lobby Info")]
    public async Task GetLobbyInfo()
    {
        if (currentLobby == null)
        {
            Debug.Log("Haven't joined bobby");
        }
        else
        {
            Debug.Log(currentLobby.Players.Count + " player(s) in lobby: " + currentLobby.Name);
        }
    }

    async Task<Allocation> AllocateRelay()
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers - 1);
            return allocation;
        }
        catch (RelayServiceException e)
        {
            Debug.LogError("Failed to allocate relay: " + e.Message);
            return default;
        }
    }

    async Task<string> GetRelayJoinCode(Allocation allocation)
    {
        try
        {
            string relayJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            return relayJoinCode;
        }
        catch (RelayServiceException e)
        {
            Debug.LogError("Failed to get relay join code: " + e.Message);
            return default;
        }
    }

    async Task<JoinAllocation> JoinRelay(string relayJoinCode)
    {
        try
        {
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(relayJoinCode);
            return joinAllocation;
        }
        catch (RelayServiceException e)
        {
            Debug.LogError("Failed to join relay: " + e.Message);
            return default;
        }
    }

    private float heartBeatTimer, pollUpdateTimer;

    async Task HandleHeartbeatAsync()
    {
        try
        {
            if (currentLobby != null && isHost)
            {
                heartBeatTimer -= Time.deltaTime;
                if (heartBeatTimer < 0)
                {
                    heartBeatTimer = k_lobbyHeartbeatInterval;
                    await LobbyService.Instance.SendHeartbeatPingAsync(currentLobby.Id);
                    Debug.Log("Sent heartbeat");
                }
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError("Failed to heartbeat lobby: " + e.Message);
        }
    }

    async Task HandlePollForUpdatesAsync()
    {
        try
        {
            if (currentLobby != null)
            {
                pollUpdateTimer -= Time.deltaTime;
                if (pollUpdateTimer < 0)
                {
                    pollUpdateTimer = k_lobbyPollInterval;
                    currentLobby = await LobbyService.Instance.GetLobbyAsync(currentLobby.Id);

                    if (CurrentHostId != currentLobby.HostId)
                    {
                        Debug.Log("Host left");
                        Debug.Log(_networkManager.IsServerStarted);
                        LeaveLobby();
                    }
                }
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError("Failed to poll for updates on lobby: " + e.Message);
        }
    }

    public string getLobbyName()
    {
        if (currentLobby == null)
        {
            return "null";
        }

        return currentLobby.Name;
    }
}