using System;
using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
using FishNet.Transporting;
using UnityEngine;

public class LobbyManagerNetworker : NetworkBehaviour
{
    public static LobbyManagerNetworker Instance { get; private set; }
    
    public event EventHandler OnKickedFromLobby;

    private void Awake()
    {
        Instance = this;
    }

    [ServerRpc (RequireOwnership = false)]
    public void ServerInvokeKickedFromLobby(string playerId)
    {
        LocalInvokeKickedFromLobby(playerId);
    }
    
    [ObserversRpc]
    public void LocalInvokeKickedFromLobby(string playerId)
    {
        if (LobbyManager.Instance.PlayerId == playerId)
        {
            OnKickedFromLobby?.Invoke(this, EventArgs.Empty);
        }
    }
    
    [ContextMenu("Check in")]
    public void IsObjectSpawned()
    {
        Debug.Log(IsOffline);
    }
}
