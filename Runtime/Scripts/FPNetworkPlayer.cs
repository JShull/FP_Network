namespace FuzzPhyte.Network
{
    using UnityEngine;
    using Unity.Netcode;
    using TMPro;
    using FuzzPhyte.Utility.FPSystem;
    using UnityEngine.SceneManagement;
    using System.Collections.Generic;
    using Unity.Netcode.Components;

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
            //find the hands
            if (ThePlayerType == DevicePlayerType.MetaQuest)
            {
                RequestHandRegistrationServerRpc();
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
        public virtual void OnServerSpawned()
        {
            if(TheUIClientCanvas!=null)
            {
                TheUIClientCanvas.enabled = false;
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
                Transform lOneTransform = proxyClient.transform.Find("LeftController");
                Transform rTwoTransform = proxyClient.transform.Find("RightController");

                if (lOneTransform != null && rTwoTransform != null)
                {
                    Debug.LogError($"Found my LeftController and RightController!");
                    if(lOneTransform.gameObject.GetComponent<IFPNetworkPlayerSetup>()!=null)
                    {
                        lOneTransform.gameObject.GetComponent<IFPNetworkPlayerSetup>().RegisterOtherObjects(lOne,this);
                    }
                    if(rTwoTransform.gameObject.GetComponent<IFPNetworkPlayerSetup>()!=null)
                    {
                        rTwoTransform.gameObject.GetComponent<IFPNetworkPlayerSetup>().RegisterOtherObjects(rTwo,this);
                    }
                }
            }
            //parent?
            /*
            IFPNetworkPlayerSetup playerInterface = proxyClient.GetComponent<IFPNetworkPlayerSetup>();
            if (IsOwner)
            {
                lOne.transform.SetParent(this.transform);
                rTwo.transform.SetParent(this.transform);
            }
            */
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
            networkSystem.UpdateLastSceneFromClient(scene.name);

            //do work based on type of scene load mode
            if(mode == LoadSceneMode.Single)
            {
                //we need to respawn and reset our local proxy all over again
                ClientProxySpawnSetup();
                return;
            }
            if(mode == LoadSceneMode.Additive)
            {
                //turn off confirm panel
                if (TheClientConfirmUIPanel != null)
                {
                    TheClientConfirmUIPanel.SetActive(false);
                }
                return;
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
        [ServerRpc(RequireOwnership =false)]
        protected virtual void ClientReadyServerRpc(FPNetworkDataStruct msgData)
        {
            Debug.Log($"Server Rpc, the client at {msgData.ClientIPAddress} {msgData.TheNetworkMessageType}| Details: {msgData.TheNetworkMessage}");
            if(msgData.TheNetworkMessageType == NetworkMessageType.ClientConfirmed)
            {
                networkSystem.OnClientConfirmed(msgData.TheClientID);
            }
        }
        [ServerRpc]
        public void RequestHandRegistrationServerRpc()
        {
            // On the server, find the hand objects by OwnerClientId:
            var clientId = OwnerClientId;

            var leftHand = networkSystem.FindMyHandObject(clientId, isLeft: true);
            var rightHand = networkSystem.FindMyHandObject(clientId, isLeft: false);

            var clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { clientId }
                }
            };

            serverRpcSystem.RegisterObjectsOnClientRpc(leftHand.NetworkObjectId, rightHand.NetworkObjectId, clientRpcParams);
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
        #endregion
        #endregion
    }
}
