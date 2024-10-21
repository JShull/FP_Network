
namespace FuzzPhyte.Network
{
    using UnityEngine;
    using System.Collections.Generic;
    using System.Collections;
    using System;
    public class FPNetworkCache:MonoBehaviour
    {
        public FPNetworkSystem networkSystem;
        public static FPNetworkCache Instance { get; private set; }
        private Dictionary<ulong,List<FPNetworkDataStruct>> cachedData = new Dictionary<ulong,List<FPNetworkDataStruct>>();
        public void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }
        /// <summary>
        /// Cache our data via the Network Event structure
        /// </summary>
        /// <param name="clientID"></param>
        /// <param name="networkData"></param>
        public void AddData(ulong clientID,FPNetworkDataStruct networkData) 
        {
            if(networkSystem.NetworkManager.IsServer)
            {
                if (cachedData.ContainsKey(clientID))
                {
                    cachedData[clientID].Add(networkData);
                }
                else
                {
                    cachedData.Add(clientID, new List<FPNetworkDataStruct> { networkData });
                }
                Debug.LogWarning($"Added to Dictionary, ID Count: {cachedData.Count} with {cachedData[clientID].Count} List Items");
            }
        }
    }
}
