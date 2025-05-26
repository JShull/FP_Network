namespace FuzzPhyte.Network
{
    using UnityEngine;
    using Unity.Netcode;
    using TMPro;
    using FuzzPhyte.Utility.FPSystem;
    using UnityEngine.SceneManagement;
    using System.Collections.Generic;
    using Unity.Netcode.Components;
    using System.Collections;

    /// <summary>
    /// Responsible for managing the player's networked state and interactions
    /// Responsible for dealing with client proxy setup
    /// </summary>
    public class FPNetworkPlayer : NetworkBehaviour,IFPNetworkProxySetup
    {
        public DevicePlayerType ThePlayerType;
        [SerializeField]private MeshRenderer DebugRenderer;
        [SerializeField]private string DebugColor;
        public TextMeshProUGUI DebugText;
        public Canvas TheUIClientCanvas;
        [Tooltip("Panel for UI Confirmation after connection")]
        public GameObject TheClientConfirmUIPanel;
        protected ulong myClientID;
        private FPNetworkSystem networkSystem;
        private FPNetworkRpc serverRpcSystem;
        private NetworkTransform networkTransform;
        [Tooltip("Prefab to spawn for local Proxy")]
        public GameObject LocalPrefabSpawn;
        [Tooltip("If we are using Scene Manager and not Network Scene Manager")]
        public bool ChildProxyClient;
        [SerializeField]protected GameObject proxyClient;
        public NetworkObject LOneOtherObject;
        public NetworkObject RTwoOtherObject;
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
        public virtual void OnServerSpawned()
        {
            if (TheUIClientCanvas != null)
            {
                TheUIClientCanvas.enabled = false;
            }

        }
        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            if (IsServer)
            {    
                if (LOneOtherObject != null && LOneOtherObject.IsSpawned)
                    LOneOtherObject.Despawn();

                if (RTwoOtherObject != null && RTwoOtherObject.IsSpawned)
                    RTwoOtherObject.Despawn();
            }
            else
            {
                OnClientDespawned();
            }
        }
        public virtual void OnClientSpawned()
        {
            ClientProxySpawnSetup();
            myClientID = networkSystem.GetLocalClientID();
            //setup scene loading events based on networkSystem
            if (networkSystem != null)
            {
                if (networkSystem.UseLocalSceneLoading)
                {
                    SceneManager.sceneLoaded += OnLocalSceneLoadedEventCompleted;
                }
                else
                {
                    //network scene loading
                    networkSystem.NetworkSceneManager.OnLoadEventCompleted += OnNetworkSceneLoadedEventCompleted;
                }
            }
            if (TheClientConfirmUIPanel != null)
            {
                TheClientConfirmUIPanel.SetActive(false);
            }
        }
        /// <summary>
        /// Called last after we have gone through spawning and connection callback from my client
        /// Called after we load back into a scene to reassign our networked prefabs based on proxy spawned items
        /// </summary>
        public IEnumerator OnClientFinishedConnectionSequence()
        {
            yield return new WaitForSecondsRealtime(0.5f);            
            if (IsOwner)
            {
                NotifyReadyServerRpc();
            }
        }
        protected virtual void ClientProxySpawnSetup()
        {
            switch (ThePlayerType)
            {
                case DevicePlayerType.None:
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
                        IFPNetworkUISetup playerUIInterface = proxyClient.GetComponent<IFPNetworkUISetup>();
                        if (playerUIInterface != null)
                        {
                            playerUIInterface.OnUISetup(this);
                        }
                    }
                    break;
                case DevicePlayerType.MetaQuest:
                    Debug.Log("MetaQuest Player Spawned");
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
                        IFPNetworkUISetup playerUIInterface = proxyClient.GetComponent<IFPNetworkUISetup>();
                        if (playerUIInterface != null)
                        {
                            playerUIInterface.OnUISetup(this);
                        }
                    }
                    break;
                default:
                    Debug.Log("Player Spawned");
                    break;
            }
        }
        public virtual void OnClientDespawned()
        {
            if (IsOwner)
            {
                if (proxyClient != null)
                {
                    Destroy(proxyClient);
                    proxyClient = null;
                }
            }
        }
        

        public override void OnDestroy()
        {
            if (IsClient)
            {
                if (networkSystem != null)
                {
                    if (networkSystem.UseLocalSceneLoading)
                    {
                        SceneManager.sceneLoaded -= OnLocalSceneLoadedEventCompleted;
                    }
                    else
                    {
                        if (networkSystem.NetworkSceneManager != null)
                        {
                            networkSystem.NetworkSceneManager.OnLoadEventCompleted -= OnNetworkSceneLoadedEventCompleted;
                        }
                    }
                    
                }
                
                if (proxyClient != null)
                {
                    Destroy(proxyClient);
                }
            }
            
            base.OnDestroy();
        }
        // Call this method from the local proxy with updated position and rotation
        public virtual void UpdatePositionAndRotation(Vector3 position, Quaternion rotation)
        {
            // Only run the RPC if this object has ownership (i.e., on the networked client)
            if (IsOwner)
            {
                // Call the server RPC to update position and rotation on the server
                SendPositionAndRotationToServerRpc(position, rotation);
            }
        }
        public virtual void ClientDebugSetup(string debugColor,Color dColor) 
        {
            DebugColor=debugColor;
            if (DebugRenderer != null)
            {
                DebugRenderer.material.color = dColor;
            }
            Debug.Log($"Client: Color applied to client: {debugColor} -> {dColor}");
        }
        /// <summary>
        /// Data passed into my client on the scene we are going to start for our module
        /// </summary>
        /// <param name="nextScene"></param>
        public virtual void ClientServerSceneSetup(string nextScene)
        {
            networkSystem.FirstSceneToLoad = nextScene;
        }
        
        /// <summary>
        /// Local call from some UI element for Testing
        /// </summary>
        /// <param name="details"></param>
        /// <returns>data struct we invoked on the server</returns>
        public virtual void UISendServerEventDetails(string details,NetworkMessageType msgType)
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
        public virtual void UISendServerConfirmationDetails(string details) 
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
        #region VR Controller Setup
       
        public void RegisterOtherObjects(NetworkObject lOne, NetworkObject rTwo)
        {
            LOneOtherObject = lOne;
            RTwoOtherObject = rTwo;
            Debug.LogWarning($"Register Other Objects called...");
            // Optionally set parent for local representation
            // call that interface to set up the local representation of the controllers
            //go find the children transform under our proxyClient 
            if(proxyClient!=null)
            {
                Debug.LogError($"Found a proxy Client!");
                //get all nested potential items under proxy
                var proxyClientNetworkSetupInterface = proxyClient.gameObject.GetComponent<IFPNetworkPlayerSetup>();
                if(proxyClientNetworkSetupInterface != null)
                {
                    Debug.LogError($"Found a proxy Client interface! you MFers!");
                    var listItems = proxyClientNetworkSetupInterface.ReturnOtherIFPNetworkObjects();
                    if (listItems.Count == 2)
                    {
                        //we have two items
                        var anInterfaceLeft = listItems[0];
                        var anInterfaceRight = listItems[1];
                        if (anInterfaceLeft != null)
                        {
                            Debug.LogError($"register left you bitch!");
                            anInterfaceLeft.RegisterOtherObjects(lOne, this);
                        }
                        if(anInterfaceRight != null)
                        {
                            Debug.LogError($"register right you bitch!");
                            anInterfaceRight.RegisterOtherObjects(rTwo, this);
                        }                   
                    }
                }
            }
        }
        #endregion
        
        #region Network Callbacks
        /// <summary>
        /// Network Scene Manager Callback
        /// </summary>
        /// <param name="sceneName"></param>
        /// <param name="loadSceneMode"></param>
        /// <param name="clientsCompleted"></param>
        /// <param name="clientsTimedOut"></param>
        protected virtual void OnNetworkSceneLoadedEventCompleted(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
        {
            if (DebugText != null)
            {
                DebugText.text += $"Scene Loaded: {sceneName} with {clientsCompleted.Count} clients completed and {clientsTimedOut.Count} clients timed out.\n";
            }
            else
            {
               Debug.LogWarning($"Scene Loaded: {sceneName} with {clientsCompleted.Count} clients completed and {clientsTimedOut.Count} clients timed out.\n");
            }

            Debug.Log($"Scene Loaded: {sceneName} with {clientsCompleted.Count} clients completed and {clientsTimedOut.Count} clients timed out.");
            networkSystem.UpdateLastSceneFromClient(sceneName);
            //do work based on scene load type
            if (loadSceneMode == LoadSceneMode.Single)
            {
                //need to reset our local proxy again
                ClientProxySpawnSetup();
                StartCoroutine(OnClientFinishedConnectionSequence());
                //need to pass my reference information for the Networked Controllers
                return;
            }
            if(loadSceneMode == LoadSceneMode.Additive)
            {
                if (TheClientConfirmUIPanel != null)
                {
                    TheClientConfirmUIPanel.SetActive(false);
                }
            }
        }
        /// <summary>
        /// Local Scene Manager Callback
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="mode"></param>
        protected virtual void OnLocalSceneLoadedEventCompleted(Scene scene, LoadSceneMode mode)
        {
            if (!IsServer)
            {
                Debug.LogWarning($"[Client]: Reset my ClientProxySpawn");
                networkSystem.UpdateLastSceneFromClient(scene.name);
                //do work based on type of scene load mode
                if (mode == LoadSceneMode.Single)
                {
                    //we need to respawn and reset our local proxy all over again
                    ClientProxySpawnSetup();
                    StartCoroutine(OnClientFinishedConnectionSequence());
                    // var player = networkManager.ConnectedClients[clientId].PlayerObject.GetComponent<FPNetworkPlayer>();
                    /*
                     * if (player != null)
                {
                    Debug.LogWarning($"[Client]: Calling Coroutine Connection Sequence Finished");
                    StartCoroutine(player.OnClientFinishedConnectionSequence());
                }
                     * */
                    return;
                }
                if (mode == LoadSceneMode.Additive)
                {
                    //turn off confirm panel
                    if (TheClientConfirmUIPanel != null)
                    {
                        TheClientConfirmUIPanel.SetActive(false);
                    }
                    return;
                }
            }
        }
        #endregion
        public virtual FPNetworkDataStruct ReturnClientDataStruct(string details, NetworkMessageType msgType)
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
        public virtual void SendServerInteractionEventRpc(string ipAddy,FPNetworkDataStruct msgData,RpcParams rpcParams=default)
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
        public virtual void DisconnectClientRequestRpc(FPNetworkDataStruct msgData, RpcParams rpcParams = default)
        {
            Debug.Log($"Disconnect Client Request: {msgData.TheNetworkMessage}");
            //ServerMessageConfirmReadyState(msgData);
            networkSystem.DisconnectClientPlayer(msgData.TheClientID);
        }
        [ServerRpc]
        protected virtual void SendPositionAndRotationToServerRpc(Vector3 position, Quaternion rotation)
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
        [ServerRpc]
        public void RequestOwnershipServerRpc(ulong targetObjectNetworkId)
        {
            var targetObject = networkSystem.NetworkManager.SpawnManager.SpawnedObjects[targetObjectNetworkId];
            Debug.LogError($"[Server]: Requesting ownership of object with Network ID: {targetObjectNetworkId} by Client ID: {OwnerClientId}");
            if (targetObject != null)
            {
                targetObject.ChangeOwnership(OwnerClientId);
            }
        }
        [ServerRpc]
        protected void NotifyReadyServerRpc(ServerRpcParams rpcParams = default)
        {
            Debug.LogWarning($"Client {OwnerClientId} is READY!");
            networkSystem.ServerOnClientReady(OwnerClientId);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="msgData"></param>
        [ServerRpc(RequireOwnership =false)]
        protected virtual void ClientReadyServerRpc(FPNetworkDataStruct msgData)
        {
            Debug.Log($"Server Rpc, the client at {msgData.ClientIPAddress} {msgData.TheNetworkMessageType}| Details: {msgData.TheNetworkMessage}");
            if(msgData.TheNetworkMessageType == NetworkMessageType.ClientConfirmed)
            {
                networkSystem.OnClientConfirmed(msgData.TheClientID);
            }
        }
        
        #endregion
        #region Rpcs Running on Client at Server Request
        [Rpc(SendTo.SpecifiedInParams)]
        protected virtual void ReceiveInteractionEventRpc(string ipAddy, FPNetworkDataStruct dataReceived, RpcParams rpcParams)
        {
            // Debug Client at Server Request only on the individual client because of the rpcParams.Receive.SenderClientId
            Debug.Log($"Client Operation Run via Server Request: {ipAddy}");
            Debug.Log($"Client Interaction Event: {dataReceived.TheNetworkMessage}");
            DebugText.text = $"Client Interaction Event: {ipAddy}\nMessage: '{dataReceived.TheNetworkMessage}'\nDevice Type {dataReceived.TheDevicePlayerType}";
        }
        [ClientRpc]
        public virtual void ServerMessageConfirmReadyStateClientRpc(FPNetworkDataStruct msgData)
        {
            Debug.Log($"Message Received to confirm connection request: {msgData.TheNetworkMessageType.ToString()} with some details {msgData.TheNetworkMessage}");
            // open up and invoke UI to confirm connection
            if(TheClientConfirmUIPanel!=null)
            {
                TheClientConfirmUIPanel.SetActive(true);
            }
            
        }
        /// <summary>
        /// Called on all clients right before we call the network scene to load
        /// </summary>
        [ClientRpc]
        public virtual void ServerMessageAboutToLoadSceneClientRpc()
        {
            Debug.LogWarning($"[Client]: Running local Unity Event Just before Scene Launch");
            networkSystem.OnNetworkAboutToLoadScene.Invoke();
        }
        [ClientRpc]
        public void SendInitialSetupClientRpc(FPInitialConnectionData playerData,ClientRpcParams rpcParams = default)
        {
            Debug.Log("[Client]: Received initial setup from server!");
            // this is running on the client
            if (playerData.PlayerType == DevicePlayerType.MetaQuest)
            {
                //register my hands
                
                var leftHand = networkSystem.NetworkManager.SpawnManager.SpawnedObjects[playerData.NetworkIDPayloadA].GetComponent<NetworkObject>();
                var rightHand = networkSystem.NetworkManager.SpawnManager.SpawnedObjects[playerData.NetworkIDPayloadB].GetComponent<NetworkObject>();
                if (leftHand != null && rightHand != null) 
                {
                    RegisterOtherObjects(leftHand, rightHand);
                }
            }
            // update player color
            if (DebugRenderer != null)
            {
                var colorString = playerData.PlayerColor;
                Debug.LogWarning($"[Client]: Apply Color(AC): Color coming in:{colorString}");
                if (!colorString.Contains("#"))
                {
                    colorString = "#" + colorString;
                    Debug.LogWarning($"[Client]: AC:Changing Color to include hash: {colorString}");
                }
                Color color;
                if (ColorUtility.TryParseHtmlString(colorString, out color))
                {
                    // Assuming you have a reference to the material (DebugMat in this case)
                    ClientDebugSetup(colorString, color);
                }
                else
                {
                    Debug.LogError($"[Client]: AC:Invalid color string: {colorString}");
                }
            }
            //update scene to load
            networkSystem.FirstSceneToLoad = playerData.SceneToLoad;
            Debug.LogWarning($"[Client]: Updated scene to load: {playerData.SceneToLoad}");
        }
        #endregion
        #endregion
    }
}
