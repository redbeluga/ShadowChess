using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using FishNet;
using FishNet.Broadcast;
using FishNet.Managing;
using FishNet.Transporting.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.VisualScripting;
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
    [SerializeField] private GameObject _lobbyManagerNetworkerPrefab;
    private LobbyManagerNetworker lobbyManagerNetworker;
    public NetworkManager _networkManager;
    [SerializeField] EncryptionType encryption = EncryptionType.DTLS;

    public static LobbyManager Instance { get; private set; }

    public string PlayerId { get; private set; }
    public string PlayerName { get; private set; }
    private bool isHost = false;
    private bool playerReady = false;
    public bool PlayerJoined { get; private set; }
    public string CurrentHostId { get; private set; }

    public Lobby CurrentLobby { get; private set; }
    public bool InGame;
    private string connectionType => encryption == EncryptionType.DTLS ? k_dtlsEncryption : k_wssEncryption;

    private const float k_lobbyHeartbeatInterval = 15f;
    private const float k_lobbyPollInterval = 1.1f;
    private const string k_keyJoinCode = "RelayJoinCode";
    private const string k_hostID = "HostID";
    private const string k_dtlsEncryption = "dtls"; // Datagram Transport Layer Security
    private const string k_wssEncryption = "wss"; // Web Socket Secure, use for WebGL builds
    public const string k_playerName = "PlayerName";
    public const string k_playerReady = "PlayerReady";
    public const string k_playerJoined = "PlayerJoined";

    public event EventHandler<LobbyEventArgs> OnLeaveLobby;
    public event EventHandler OnCreateLobby;
    public event EventHandler OnQueueJoinLobby;
    public event EventHandler<LobbyEventArgs> OnJoinLobby;
    public event EventHandler<LobbyEventArgs> OnJoinedLobbyUpdate;
    public event EventHandler<OnLobbyListChangedEventArgs> OnLobbyListChanged;
    public event EventHandler ErrorWhileLoading;

    public class OnLobbyListChangedEventArgs : EventArgs
    {
        public List<Lobby> lobbyList;
    }

    public class LobbyEventArgs : EventArgs
    {
        public Lobby lobby;
    }

    [SerializeField] private GameObject lobbyManagerNetworkerPrefab;

    [SerializeField] private string lobbySceneName;
    [SerializeField] private string gameSceneName;

    private void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(this);

        PlayerJoined = false;

        Application.quitting += PlayerQuitApplication;
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

    public async Task CreateLobby(String lobbyName, bool isPrivate)
    {
        // Debug.Log("HEY:");
        if (CurrentLobby != null) return;
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

            CurrentLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);
            Debug.Log("Created lobby: " + CurrentLobby.Name + " with code " + CurrentLobby.LobbyCode);

            await LobbyService.Instance.UpdateLobbyAsync(CurrentLobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    { k_keyJoinCode, new DataObject(DataObject.VisibilityOptions.Member, relayJoinCode) },
                    { k_hostID, new DataObject(DataObject.VisibilityOptions.Member, PlayerId) }
                }
            });

            _networkManager.GetComponent<FishyUnityTransport>()
                .SetRelayServerData(new RelayServerData(allocation, connectionType));

            _networkManager.ServerManager.StartConnection();
            _networkManager.ClientManager.StartConnection();

            SpawnManagers();

            OnJoinLobby?.Invoke(this, new LobbyEventArgs { lobby = CurrentLobby });
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError("Failed to create lobby: " + e.Message);
            ErrorWhileLoading?.Invoke(this, EventArgs.Empty);
        }
    }

    private void SpawnManagers()
    {
        lobbyManagerNetworker = Instantiate(_lobbyManagerNetworkerPrefab).GetComponent<LobbyManagerNetworker>();
        DontDestroyOnLoad(lobbyManagerNetworker.gameObject);
        InstanceFinder.ServerManager.Spawn(lobbyManagerNetworker.gameObject, null);
    }

    public async Task JoinLobby(string password, bool isId)
    {
        if (CurrentLobby != null) return;
        try
        {
            pollUpdateTimer = k_lobbyPollInterval;
            Unity.Services.Lobbies.Models.Player player = GetPlayer();

            CurrentLobby = isId
                ? await LobbyService.Instance.JoinLobbyByIdAsync(password, new JoinLobbyByIdOptions()
                {
                    Player = player
                })
                : await LobbyService.Instance.JoinLobbyByCodeAsync(password, new JoinLobbyByCodeOptions
                {
                    Player = player
                });
            OnQueueJoinLobby?.Invoke(this, EventArgs.Empty);

            CurrentHostId = CurrentLobby.Data[k_hostID].Value;
            // Debug.Log("Joined " + CurrentLobby.Name);

            string relayJoinCode = CurrentLobby.Data[k_keyJoinCode].Value;
            JoinAllocation joinAllocation = await JoinRelay(relayJoinCode);
            // Debug.Log("Joined relay");

            _networkManager.GetComponent<FishyUnityTransport>()
                .SetRelayServerData(new RelayServerData(joinAllocation, connectionType));

            _networkManager.ClientManager.StartConnection();

            OnJoinLobby?.Invoke(this, new LobbyEventArgs { lobby = CurrentLobby });

            // Debug.Log("Started fishnet connection");
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError("Failed to quick join lobby: " + e.Message);
            ErrorWhileLoading?.Invoke(this, EventArgs.Empty);
        }
    }

    public async Task QuickJoinLobby()
    {
        if (CurrentLobby != null) return;
        try
        {
            OnQueueJoinLobby?.Invoke(this, EventArgs.Empty);
            Unity.Services.Lobbies.Models.Player player = GetPlayer();
            CurrentLobby = await LobbyService.Instance.QuickJoinLobbyAsync(new QuickJoinLobbyOptions()
            {
                Player = player
            });
            CurrentHostId = CurrentLobby.HostId;

            string relayJoinCode = CurrentLobby.Data[k_keyJoinCode].Value;
            JoinAllocation joinAllocation = await JoinRelay(relayJoinCode);

            _networkManager.GetComponent<FishyUnityTransport>()
                .SetRelayServerData(new RelayServerData(joinAllocation, connectionType));

            OnJoinLobby?.Invoke(this, new LobbyEventArgs { lobby = CurrentLobby });

            _networkManager.ClientManager.StartConnection();

            Debug.Log(_networkManager.IsServerStarted);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError("Failed to quick join lobby: " + e.Message);
        }
    }

    public async Task LeaveLobby(bool inGame)
    {
        if (CurrentLobby == null) return;
        try
        {
            await LobbyService.Instance.RemovePlayerAsync(CurrentLobby.Id, PlayerId);
            Debug.Log("Player left the lobby: " + CurrentLobby.Name);

            CurrentLobby = null;
            isHost = false;
            CurrentHostId = null;
            playerReady = false;
            PlayerJoined = false;
            InGame = false;
            Debug.Log(CurrentLobby == null);

            _networkManager.ServerManager.StopConnection(false);
            _networkManager.ClientManager.StopConnection();
            
            if (inGame)
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(lobbySceneName);
            }

            OnLeaveLobby?.Invoke(this, new LobbyEventArgs { lobby = CurrentLobby });
        }
        catch (LobbyServiceException e)
        {
            CurrentLobby = null;
            isHost = false;
            CurrentHostId = null;
            playerReady = false;
            PlayerJoined = false;
            InGame = false;
            Debug.LogError("Failed to leave lobby: " + e.Message);
        }
    }

    private async void PlayerQuitApplication()
    {
        _networkManager.ServerManager.StopConnection(false);
        _networkManager.ClientManager.StopConnection();

        await LobbyService.Instance.RemovePlayerAsync(CurrentLobby.Id, PlayerId);
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
            OnLobbyListChanged?.Invoke(this,
                new OnLobbyListChangedEventArgs { lobbyList = lobbyListQueryResponse.Results });
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    public void KickPlayer(string playerId)
    {
        if (CurrentLobby != null && IsLobbyHost())
        {
            LobbyManagerNetworker.Instance.InvokeKickPlayer(playerId);
        }
    }

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
                {
                    k_playerJoined,
                    new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, PlayerJoined.ToString())
                }
            });
    }

    public async void SetPlayerJoined()
    {
        if (CurrentLobby != null)
        {
            PlayerJoined = true;
            try
            {
                UpdatePlayerOptions options = new UpdatePlayerOptions();

                options.Data = new Dictionary<string, PlayerDataObject>()
                {
                    {
                        k_playerJoined, new PlayerDataObject(
                            visibility: PlayerDataObject.VisibilityOptions.Public,
                            value: PlayerJoined.ToString())
                    }
                };

                string playerId = PlayerId;

                Lobby lobby = await LobbyService.Instance.UpdatePlayerAsync(CurrentLobby.Id, playerId, options);
                CurrentLobby = lobby;

                OnJoinedLobbyUpdate?.Invoke(this, new LobbyEventArgs { lobby = CurrentLobby });
            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
            }
        }
    }

    public async void ReadyUp()
    {
        if (CurrentLobby != null)
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

                Lobby lobby = await LobbyService.Instance.UpdatePlayerAsync(CurrentLobby.Id, playerId, options);
                CurrentLobby = lobby;

                OnJoinedLobbyUpdate?.Invoke(this, new LobbyEventArgs { lobby = CurrentLobby });
            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
            }
        }
    }

    public void StartGame()
    {
        SceneManager.Instance.LoadScene(gameSceneName);
        SceneManager.Instance.UnloadScene(lobbySceneName);
        LobbyManagerNetworker.Instance.InvokeStartGame();
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
            ErrorWhileLoading?.Invoke(this, EventArgs.Empty);
            return default;
        }
    }

    private float heartBeatTimer, pollUpdateTimer;

    async Task HandleHeartbeatAsync()
    {
        try
        {
            if (CurrentLobby != null && isHost)
            {
                heartBeatTimer -= Time.deltaTime;
                if (heartBeatTimer < 0)
                {
                    heartBeatTimer = k_lobbyHeartbeatInterval;
                    await LobbyService.Instance.SendHeartbeatPingAsync(CurrentLobby.Id);
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
        if (CurrentLobby == null) return;

        try
        {
            pollUpdateTimer -= Time.deltaTime;
            if (pollUpdateTimer < 0)
            {
                pollUpdateTimer = k_lobbyPollInterval;
                CurrentLobby = await LobbyService.Instance.GetLobbyAsync(CurrentLobby.Id);

                OnJoinedLobbyUpdate?.Invoke(this, new LobbyEventArgs { lobby = CurrentLobby });

                if (CurrentHostId != CurrentLobby.HostId && !InGame)
                {
                    Debug.Log("Host left");
                    LeaveLobby(false);
                }
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError("Failed to poll for updates on lobby: " + e.Message);
        }
    }

    [ContextMenu("Get Info")]
    public void GetInfo()
    {
        Debug.Log(CurrentLobby.Players.Count);
        Debug.Log(InGame);
        Debug.Log(CurrentLobby.HostId);
        Debug.Log(CurrentLobby.LastUpdated.ToLocalTime().ToString("MM/dd/yyyy hh:mm:ss tt"));
    }

    public bool IsLobbyHost()
    {
        if (CurrentLobby == null || !isHost) return false;
        return true;
    }

    public bool IsLobbyHost(string playerId)
    {
        if (CurrentLobby == null) return false;
        return playerId.Equals(CurrentLobby.HostId);
    }
}