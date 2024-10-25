namespace FuzzPhyte.Network
{
    using UnityEngine;
    using System.Collections.Generic;

    public class FPNetworkCache:MonoBehaviour
    {
        public FPNetworkSystem networkSystem;
        public static FPNetworkCache Instance { get; private set; }
        protected Dictionary<string, FPSerializedNetworkData<FPNetworkDataStruct>> cachedNetworkData = new Dictionary<string, FPSerializedNetworkData<FPNetworkDataStruct>>();
        
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
        public virtual void AddData(string ipAddress, ulong clientID,FPNetworkDataStruct networkData)
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
        public virtual void PrintData()
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
        protected virtual void SaveDataToFile()
        {
            
        }
    }
}
