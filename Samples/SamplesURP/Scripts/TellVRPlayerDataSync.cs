namespace FuzzPhyte.Network.Samples
{
    using UnityEngine;
    using FuzzPhyte.Network;
    using System;

    [Obsolete]
    public class TellVRPlayerDataSync : MonoBehaviour
    {
        public FPNetworkSystem networkSystem;
        public FPNetworkCache networkCache;
        public void Awake()
        {
            if(networkSystem!=null)
            {
                networkSystem.OnClientDisconnectPassNetworkCache += OnClientDisconnectPassNetworkCache;
            }
        }
        public void OnDisable()
        {
            if(networkSystem!=null)
            {
                networkSystem.OnClientDisconnectPassNetworkCache -= OnClientDisconnectPassNetworkCache;
            }
        }
        public void OnClientDisconnectPassNetworkCache(FPNetworkCache networkData)
        {
            
        }
    }
}