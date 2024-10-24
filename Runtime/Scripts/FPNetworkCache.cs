
namespace FuzzPhyte.Network
{
    using UnityEngine;
    using System.Collections.Generic;
    using System.Collections;
    using System;
    using FuzzPhyte.Utility;
    public class FPNetworkCache:MonoBehaviour
    {
        public FPNetworkSystem networkSystem;
        public static FPNetworkCache Instance { get; private set; }
        private Dictionary<string, FPSerializedNetworkData<FPNetworkDataStruct>> cachedNetworkData = new Dictionary<string, FPSerializedNetworkData<FPNetworkDataStruct>>();
        //private Dictionary<ulong,List<FPNetworkDataStruct>> cachedData = new Dictionary<ulong,List<FPNetworkDataStruct>>();
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
        /*
        /// <summary>
        /// Cache our data via the Network Event structure
        /// </summary>
        /// <param name="clientID"></param>
        /// <param name="networkData"></param>
        [Obsolete]
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
        */
        public void AddData(string ipAddress, ulong clientID,FPNetworkDataStruct networkData)
        {
            if (networkSystem.NetworkManager.IsServer)
            {
                if(cachedNetworkData.ContainsKey(ipAddress))
                {
                    cachedNetworkData[ipAddress].list.Add(networkData);
                }
                else
                {
                    var dataList = new List<FPNetworkDataStruct>() { networkData };
                    var newClass = new FPSerializedNetworkData<FPNetworkDataStruct>(dataList,clientID, ipAddress);
                    cachedNetworkData.Add(ipAddress, newClass);
                }
            }
        }
        /// <summary>
        /// Debug print out testing for data
        /// </summary>
        public void PrintData()
        {
            Debug.LogWarning($"Printing Data! Cached Data Count: {cachedNetworkData.Count}");
            foreach(var data in cachedNetworkData)
            {
                Debug.Log($"Client ID: {data.Key}");
                foreach(var item in data.Value.list)
                {
                    Debug.Log($"Message Type {item.TheNetworkMessageType.ToString()} | Message: {item.TheNetworkMessage}");
                }
            }
        }
        /// <summary>
        /// Stub out for saving the file locally
        /// </summary>
        private void SaveDataToFile()
        {
            
        }
    }
}
