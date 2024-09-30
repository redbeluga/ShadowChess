using System;
using System.Collections;
using System.Collections.Generic;
using FishNet;
using FishNet.Managing;
using UnityEngine;

public class Bootstrap : MonoBehaviour
{
    [SerializeField] private NetworkManager _networkManager;
    [SerializeField] private LobbyManager _lobbyManager;
    [SerializeField] private SceneManager _sceneManager;
    [SerializeField] private Camera _mainCamera;

    private void Awake()
    {
        // DontDestroyOnLoad(_networkManager.gameObject); // done in networkmanager inspector
        DontDestroyOnLoad(_lobbyManager.gameObject);
        DontDestroyOnLoad(_sceneManager.gameObject);
        DontDestroyOnLoad(_mainCamera.gameObject);
        // _lobbyManager.gameObject.SetActive(true);
        // Debug.Log("ENABLE MANAGER");
        
        // GameObject lobbyManagerNetworker = Instantiate(_lobbyManagerNetworkerPrefab);
        // DontDestroyOnLoad(lobbyManagerNetworker);
        
        LoadLobby();
    }

    void LoadLobby()
    {
        // Debug.Log("Loading MainMenu");
        UnityEngine.SceneManagement.SceneManager.LoadScene("Lobby");
    }
}
