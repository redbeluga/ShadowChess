using System.Collections;
using System.Collections.Generic;
using FishNet;
using FishNet.Managing.Scened;
using UnityEngine;

public class SceneManager : MonoBehaviour
{
    public static SceneManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    public void LoadScene(string sceneName)
    {
        if (!InstanceFinder.IsServerStarted)
        {
            return;
        }

        SceneLoadData sld = new SceneLoadData(sceneName);
        InstanceFinder.SceneManager.LoadGlobalScenes(sld);
    }
    
    public void UnloadScene(string sceneName)
    {
        if (!InstanceFinder.IsServerStarted)
        {
            return;
        }

        SceneUnloadData sld = new SceneUnloadData(sceneName);
        InstanceFinder.SceneManager.UnloadGlobalScenes(sld);
    }
}