using System;
using System.Collections;
using System.Collections.Generic;
using FishNet.Broadcast;
using FishNet.Object;
using FishNet.Transporting;
using UnityEngine;

public class LobbyManagerNetworker : NetworkBehaviour
{
    public static LobbyManagerNetworker Instance { get; private set; }
    
    public struct  StartGameEvent : IBroadcast
    {
        public string Message;
    }
    
    public struct  KickPlayerEvent : IBroadcast
    {
        public string PlayerId;
    }

    private void Awake()
    {
        Instance = this;
    }

    public void InvokeKickPlayer(string playerId)
    {
        ServerManager.Broadcast(new KickPlayerEvent()
        {
            PlayerId = playerId
        });
    }

    public void InvokeStartGame()
    {
        ServerManager.Broadcast(new StartGameEvent()
        {
            Message = "StartGame"
        });
    }

    
    
    [ContextMenu("Check in")]
    public void IsObjectSpawned()
    {
        Debug.Log(IsOffline);
    }
}
