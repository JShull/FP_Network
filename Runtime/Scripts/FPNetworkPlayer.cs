
namespace FuzzPhyte.Network
{
    using UnityEngine;
    using Unity.Netcode;
    using TMPro;
    using FuzzPhyte.Utility.FPSystem;
    using UnityEngine.SceneManagement;
    using System.Collections.Generic;
    using FuzzPhyte.Utility.TestingDebug;
    using Unity.Netcode.Components;

    public class FPNetworkPlayer : NetworkBehaviour
    {
        public DevicePlayerType ThePlayerType;
        [SerializeField]private MeshRenderer DebugRenderer;
        [SerializeField]private string DebugColor;
        public TextMeshProUGUI DebugText;
        public Canvas TheUIClientCanvas;
        [Tooltip("Panel for UI Confirmation after connection")]
        public GameObject TheClientConfirmUIPanel;
        private ulong myClientID;
        private FPNetworkSystem networkSystem;
        private FPNetworkRpc serverRpcSystem;
        private NetworkTransform networkTransform;
        public GameObject LocalPrefabSpawn;
        private GameObject proxyClient;
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            networkTransform = this.GetComponent<NetworkTransform>();
            networkSystem = FPSystemBase<FPNetworkData>.Instance as FPNetworkSystem;
            if (networkSystem == null)
            {
                Debug.LogError("$not finding the FPNetworkSystem in the scene.");
                return;
            }
            //get serverRpcSystem from my networkSystem
            serverRpcSystem = networkSystem.GetFPNetworkRpc;
            if(serverRpcSystem == null)
            {
                Debug.LogError("No Server Rpc System Found");
                serverRpcSystem = networkSystem.gameObject.AddComponent<FPNetworkRpc>();
            }
            serverRpcSystem.FPNetworkPlayer = this;

            
            if (IsServer)
            {
                OnServerSpawned();
            }
            else
            {
                OnClientSpawned(); 
            }
        }
        public void OnClientSpawned()
        {
            switch(ThePlayerType)
            {
                case DevicePlayerType.iPad:
                    Debug.Log("iPad Player Spawned");
                    networkSystem.ConfigureSetupCam(false);
                    if (IsOwner)
                    {
                        proxyClient = GameObject.Instantiate(LocalPrefabSpawn, this.transform.position, this.transform.rotation);
                        // Try to get the component that implements the interface
                        IFPNetworkPlayerSetup playerInterface = proxyClient.GetComponent<IFPNetworkPlayerSetup>();
                        if (playerInterface != null)
                        {
                            playerInterface.SetupSystem(this);
                        }
                    }
                   
                    break;
                case DevicePlayerType.MetaQuest:
                    Debug.Log("MetaQuest Player Spawned");
                    break;
                default:
                    Debug.Log("Player Spawned");
                    break;
            }
            myClientID = networkSystem.GetLocalClientID();
            networkSystem.NetworkSceneManager.OnLoadEventCompleted += OnLoadedEventCompleted;
            TheClientConfirmUIPanel.SetActive(false);
            
        }
        public void OnServerSpawned()
        {
            TheUIClientCanvas.enabled = false;
        }

        public override void OnDestroy()
        {
            if (IsClient)
            {
                networkSystem.NetworkSceneManager.OnLoadEventCompleted -= OnLoadedEventCompleted;
            }
            if(proxyClient!=null)
            {
                Destroy(proxyClient);
            }
            base.OnDestroy();
        }
        // Call this method from the local proxy with updated position and rotation
        public void UpdatePositionAndRotation(Vector3 position, Quaternion rotation)
        {
            // Only run the RPC if this object has ownership (i.e., on the networked client)
            if (IsOwner)
            {
                // Call the server RPC to update position and rotation on the server
                SendPositionAndRotationToServerRpc(position, rotation);
            }
        }
        public void ClientDebugSetup(string debugColor,Color dColor) 
        {
            DebugColor=debugColor;
            if (DebugRenderer != null)
            {
                DebugRenderer.material.color = dColor;
            }
            Debug.Log($"Client: Color applied to client: {debugColor} -> {dColor}");
        }
        /// <summary>
        /// Local call from some UI element for Testing
        /// </summary>
        /// <param name="details"></param>
        /// <returns>data struct we invoked on the server</returns>
        public void UISendServerEventDetails(string details,NetworkMessageType msgType)
        {
            FPNetworkDataStruct data = new FPNetworkDataStruct()
            {
                TheDevicePlayerType = ThePlayerType,
                TheNetworkPlayerType = NetworkPlayerType.Client,
                TheNetworkMessageType = msgType,
                TheNetworkMessage = details,
                TheClientID = myClientID,
                ClientIPAddress = networkSystem.CurrentIP.ToString(),
                ClientColor = DebugColor,
            };
           
            // Send to Server Rpc
            SendServerInteractionEventRpc(networkSystem.CurrentIP.ToString(), data);
        }
        /// <summary>
        /// Called via Confirm UI Button
        /// </summary>
        /// <param name="details"></param>
        public void UISendServerConfirmationDetails(string details) 
        {
            FPNetworkDataStruct data = new FPNetworkDataStruct()
            {
                TheDevicePlayerType = ThePlayerType,
                TheNetworkPlayerType = NetworkPlayerType.Client,
                TheNetworkMessageType = NetworkMessageType.ClientConfirmed,
                TheNetworkMessage = details,
                TheClientID = myClientID,
                ClientIPAddress = networkSystem.CurrentIP.ToString(),
                ClientColor = DebugColor,
            };
            // Send to Server Rpc
            ClientReadyServerRpc(data);
            
        }
        #region Network Callbacks
        private void OnLoadedEventCompleted(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
        {
            DebugText.text+= $"Scene Loaded: {sceneName} with {clientsCompleted.Count} clients completed and {clientsTimedOut.Count} clients timed out.\n";
            Debug.Log($"Scene Loaded: {sceneName} with {clientsCompleted.Count} clients completed and {clientsTimedOut.Count} clients timed out.");
            networkSystem.UpdateLastSceneFromClient(sceneName);
        }
        #endregion
        public FPNetworkDataStruct ReturnClientDataStruct(string details, NetworkMessageType msgType)
        {
            FPNetworkDataStruct data = new FPNetworkDataStruct()
            {
                TheDevicePlayerType = ThePlayerType,
                TheNetworkPlayerType = NetworkPlayerType.Client,
                TheNetworkMessageType = msgType,
                TheNetworkMessage = details,
                TheClientID = myClientID,
                ClientIPAddress = networkSystem.CurrentIP.ToString(),
                ClientColor = DebugColor,
            };
            return data;
        }
        #region RPC Methods
        #region Rpcs Server, runs on server then sends to client
        
        [Rpc(SendTo.Server)]
        public void SendServerInteractionEventRpc(string ipAddy,FPNetworkDataStruct msgData,RpcParams rpcParams=default)
        {
            DebugText.text = $"Interaction Event: {ipAddy}\nMessage: '{msgData.TheNetworkMessage}'\nDevice Type {msgData.TheDevicePlayerType}";
            //add data to cache
            if (FPNetworkCache.Instance != null)
            {
                
                //FPNetworkCache.Instance.AddData(rpcParams.Receive.SenderClientId, msgData);
                FPNetworkCache.Instance.AddData(ipAddy, rpcParams.Receive.SenderClientId, msgData);
            }
            ReceiveInteractionEventRpc(ipAddy, msgData,RpcTarget.Single(rpcParams.Receive.SenderClientId, RpcTargetUse.Temp));
        }
        [Rpc(SendTo.Server)]
        public void SendServerConfirmationRpc(FPNetworkDataStruct msgData, RpcParams rpcParams = default)
        {
            Debug.Log($"Server Confirmation Rpc: {msgData.TheNetworkMessage}");
            //ServerMessageConfirmReadyState(msgData);
        }
        [Rpc(SendTo.Server)]
        public void DisconnectClientRequestRpc(FPNetworkDataStruct msgData, RpcParams rpcParams = default)
        {
            Debug.Log($"Disconnect Client Request: {msgData.TheNetworkMessage}");
            //ServerMessageConfirmReadyState(msgData);
            networkSystem.DisconnectClientPlayer(msgData.TheClientID);
        }
        #endregion
        [ServerRpc]
        private void SendPositionAndRotationToServerRpc(Vector3 position, Quaternion rotation)
        {
            // Set the new position and rotation on the server's instance
            transform.position = position;
            transform.rotation = rotation;

            // Update the NetworkTransform to sync the new position and rotation across clients
            if (networkTransform != null)
            {
                networkTransform.SetState(position, rotation,networkTransform.transform.localScale, true);
            }
        }
        [ServerRpc(RequireOwnership =false)]
        public void ClientReadyServerRpc(FPNetworkDataStruct msgData)
        {
            Debug.Log($"Server Rpc, the client at {msgData.ClientIPAddress} {msgData.TheNetworkMessageType}| Details: {msgData.TheNetworkMessage}");
            if(msgData.TheNetworkMessageType == NetworkMessageType.ClientConfirmed)
            {
                networkSystem.OnClientConfirmed(msgData.TheClientID);
            }
        }
        #region Rpcs Running on Client at Server Request
        [Rpc(SendTo.SpecifiedInParams)]
        void ReceiveInteractionEventRpc(string ipAddy, FPNetworkDataStruct dataReceived, RpcParams rpcParams)
        {
            // Debug Client at Server Request only on the individual client because of the rpcParams.Receive.SenderClientId
            Debug.Log($"Client Operation Run via Server Request: {ipAddy}");
            Debug.Log($"Client Interaction Event: {dataReceived.TheNetworkMessage}");
            DebugText.text = $"Client Interaction Event: {ipAddy}\nMessage: '{dataReceived.TheNetworkMessage}'\nDevice Type {dataReceived.TheDevicePlayerType}";
        }
        [ClientRpc]
        public void ServerMessageConfirmReadyStateClientRpc(FPNetworkDataStruct msgData)
        {
            Debug.Log($"Message Received to confirm connection request: {msgData.TheNetworkMessageType.ToString()} with some details {msgData.TheNetworkMessage}");
            // open up and invoke UI to confirm connection
            TheClientConfirmUIPanel.SetActive(true);
        }
        #endregion
            #endregion
    }
}
