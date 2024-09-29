using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using FishNet.Managing;
using FishNet.Object;
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
    private string lobbyName;
    [SerializeField] int maxPlayers = 2;
    public NetworkManager _networkManager;
    [SerializeField] EncryptionType encryption = EncryptionType.DTLS;

    public static LobbyManager Instance { get; private set; }

    public string PlayerId { get; private set; }
    public string PlayerName { get; private set; }
    private bool isHost = false;
    private bool playerReady = false;
    public string CurrentHostId { get; private set; }

    private Lobby currentLobby;
    private string connectionType => encryption == EncryptionType.DTLS ? k_dtlsEncryption : k_wssEncryption;

    private const float k_lobbyHeartbeatInterval = 15f;
    private const float k_lobbyPollInterval = 1.1f;
    private const string k_keyJoinCode = "RelayJoinCode";
    private const string k_hostID = "HostID";
    private const string k_dtlsEncryption = "dtls"; // Datagram Transport Layer Security
    private const string k_wssEncryption = "wss"; // Web Socket Secure, use for WebGL builds
    public const string k_playerName = "PlayerName";
    public const string k_playerReady = "PlayerReady";

    public event EventHandler<LobbyEventArgs> OnLeaveLobby;
    public event EventHandler<LobbyEventArgs> OnStartGame;
    public event EventHandler OnCreateLobby;
    public event EventHandler OnQueueJoinLobby;
    public event EventHandler<LobbyEventArgs> OnJoinLobby;
    public event EventHandler<LobbyEventArgs> OnJoinedLobbyUpdate;
    public event EventHandler<OnLobbyListChangedEventArgs> OnLobbyListChanged;

    public class OnLobbyListChangedEventArgs : EventArgs
    {
        public List<Lobby> lobbyList;
    }

    public class KickedFromLobbyEventArgs : EventArgs
    {
        public string playerId;
    }

    public class LobbyEventArgs : EventArgs
    {
        public Lobby lobby;
    }

    [SerializeField] private GameObject lobbyManagerNetworkerPrefab;

    private void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(this);
    }

    async void Start()
    {
        await Authenticate();
    }

    void OnApplicationQuit()
    {
        if (IsLobbyHost())
        {
            LeaveLobby();
        }
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

    public async Task CreateLobby(String lobbyName, bool isPrivate)
    {
        // Debug.Log("HEY:");
        if (currentLobby != null) return;
        try
        {
            OnCreateLobby?.Invoke(this, EventArgs.Empty);
            Unity.Services.Lobbies.Models.Player player = GetPlayer();
            CurrentHostId = PlayerId;
            isHost = true;

            Allocation allocation = await AllocateRelay();
            string relayJoinCode = await GetRelayJoinCode(allocation);

            CreateLobbyOptions options = new CreateLobbyOptions
            {
                Player = player,
                IsPrivate = isPrivate
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

            OnJoinLobby?.Invoke(this, new LobbyEventArgs { lobby = currentLobby });

            _networkManager.ServerManager.StartConnection();
            _networkManager.ClientManager.StartConnection();

            // SpawnManagers();
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError("Failed to create lobby: " + e.Message);
        }
    }


    public async Task JoinLobby(string password, bool isId)
    {
        if (currentLobby != null) return;
        try
        {
            OnQueueJoinLobby?.Invoke(this, EventArgs.Empty);
            pollUpdateTimer = k_lobbyPollInterval;
            Unity.Services.Lobbies.Models.Player player = GetPlayer();

            currentLobby = isId
                ? await LobbyService.Instance.JoinLobbyByIdAsync(password, new JoinLobbyByIdOptions()
                {
                    Player = player
                })
                : await LobbyService.Instance.JoinLobbyByCodeAsync(password, new JoinLobbyByCodeOptions
                {
                    Player = player
                });

            CurrentHostId = currentLobby.Data[k_hostID].Value;
            // Debug.Log("Joined " + currentLobby.Name);

            string relayJoinCode = currentLobby.Data[k_keyJoinCode].Value;
            JoinAllocation joinAllocation = await JoinRelay(relayJoinCode);
            // Debug.Log("Joined relay");

            _networkManager.GetComponent<FishyUnityTransport>()
                .SetRelayServerData(new RelayServerData(joinAllocation, connectionType));

            OnJoinLobby?.Invoke(this, new LobbyEventArgs { lobby = currentLobby });

            _networkManager.ClientManager.StartConnection();

            // Debug.Log("Started fishnet connection");
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
            OnQueueJoinLobby?.Invoke(this, EventArgs.Empty);
            Unity.Services.Lobbies.Models.Player player = GetPlayer();
            currentLobby = await LobbyService.Instance.QuickJoinLobbyAsync(new QuickJoinLobbyOptions()
            {
                Player = player
            });
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
            Debug.Log(PlayerId);
            await LobbyService.Instance.RemovePlayerAsync(currentLobby.Id, PlayerId);
            Debug.Log("Player left the lobby: " + currentLobby.Name);

            currentLobby = null;
            isHost = false;
            CurrentHostId = null;
            playerReady = false;

            _networkManager.ServerManager.StopConnection(false);
            _networkManager.ClientManager.StopConnection();

            OnLeaveLobby?.Invoke(this, new LobbyEventArgs { lobby = currentLobby });
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError("Failed to leave lobby: " + e.Message);
        }
    }

    public async void RefreshLobbyList()
    {
        try
        {
            // Debug.Log("Refresh Query");
            QueryLobbiesOptions options = new QueryLobbiesOptions();
            options.Count = 25;

            // Filter for open lobbies only
            options.Filters = new List<QueryFilter>
            {
                new QueryFilter(
                    field: QueryFilter.FieldOptions.AvailableSlots,
                    op: QueryFilter.OpOptions.GT,
                    value: "0")
            };

            // Order by newest lobbies first
            options.Order = new List<QueryOrder>
            {
                new QueryOrder(
                    asc: false,
                    field: QueryOrder.FieldOptions.Created)
            };

            QueryResponse lobbyListQueryResponse = await Lobbies.Instance.QueryLobbiesAsync();
            // Debug.Log(lobbyListQueryResponse.Results.Count);
            // Debug.Log("Lobby list change event invoked");
            OnLobbyListChanged?.Invoke(this,
                new OnLobbyListChangedEventArgs { lobbyList = lobbyListQueryResponse.Results });
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }
    public async void KickPlayer(string playerId)
    {
        if (currentLobby != null && IsLobbyHost())
        {
            LobbyManagerNetworker.Instance.ServerInvokeKickedFromLobby(playerId);
        }
    }

    // [ContextMenu("Check Client")]
    // public void CheckClient()
    // {
    //     Debug.Log("HELLO");
    //     Debug.Log(LobbyManagerNetworker.Instance.IsObjectSpawned());
    //     Debug.Log(_networkManager.ClientManager.Started);
    // }

    private Unity.Services.Lobbies.Models.Player GetPlayer()
    {
        return new Unity.Services.Lobbies.Models.Player(AuthenticationService.Instance.PlayerId, null,
            new Dictionary<string, PlayerDataObject>
            {
                {
                    k_playerName, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, PlayerName)
                },
                {
                    k_playerReady,
                    new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, playerReady.ToString())
                },
            });
    }

    public async void ReadyUp()
    {
        if (currentLobby != null)
        {
            playerReady = !playerReady;
            try
            {
                UpdatePlayerOptions options = new UpdatePlayerOptions();

                options.Data = new Dictionary<string, PlayerDataObject>()
                {
                    {
                        k_playerReady, new PlayerDataObject(
                            visibility: PlayerDataObject.VisibilityOptions.Public,
                            value: playerReady.ToString())
                    }
                };

                string playerId = PlayerId;

                Lobby lobby = await LobbyService.Instance.UpdatePlayerAsync(currentLobby.Id, playerId, options);
                currentLobby = lobby;

                OnJoinedLobbyUpdate?.Invoke(this, new LobbyEventArgs { lobby = currentLobby });
            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
            }
        }
    }

    // private void SpawnManagers()
    // {
    //     GameObject managerInstance = Instantiate(lobbyManagerNetworkerPrefab);
    //     managerInstance.GetComponent<NetworkObject>().Spawn(managerInstance);
    // }

    public void StartGame()
    {
        Debug.Log("Starting game");
    }

    [ContextMenu("Get Manager Info")]
    public async Task GetManagerInfo()
    {
        Debug.Log(PlayerId);
        if (currentLobby == null)
        {
            Debug.Log("Haven't joined lobby");
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

                    OnJoinedLobbyUpdate?.Invoke(this, new LobbyEventArgs { lobby = currentLobby });

                    if (CurrentHostId != currentLobby.HostId)
                    {
                        Debug.Log("Host left");
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

    // public string getLobbyName()
    // {
    //     if (currentLobby == null)
    //     {
    //         return "null";
    //     }
    //
    //     return currentLobby.Name;
    // }

    public bool IsLobbyHost()
    {
        if (currentLobby == null || !isHost) return false;
        return true;
    }

    public bool IsLobbyHost(string playerId)
    {
        if (currentLobby == null) return false;
        return playerId.Equals(currentLobby.HostId);
    }
}