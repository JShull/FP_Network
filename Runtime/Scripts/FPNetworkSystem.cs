namespace FuzzPhyte.Network
{
    /// References 
    /// <summary>
    /// https://docs-multiplayer.unity3d.com/netcode/current/components/networkmanager/
    /// https://docs-multiplayer.unity3d.com/netcode/current/basics/playerobjects/
    /// https://docs-multiplayer.unity3d.com/netcode/current/basics/connection-approval/
    /// https://docs-multiplayer.unity3d.com/netcode/current/basics/object-spawning/
    /// </summary>
    using FuzzPhyte.Utility.FPSystem;
    using UnityEngine;
    using System;
    using System.Net.NetworkInformation;
    using System.Net.Sockets;
    using System.Net;
    using Unity.Netcode;
    using Unity.Netcode.Transports.UTP;
    using UnityEngine.SceneManagement;
    using System.Collections.Generic;
    using System.Collections;
    using UnityEngine.Events;
#if UNITY_IOS && !UNITY_EDITOR
    using System.Runtime.InteropServices;
#endif
    public class FPNetworkSystem : FPSystemBase<FPNetworkData>
    {
        public bool SkipLocalIPAddress;
        public IPAddress CurrentIP;
        public ushort PortAddress = 7777;
        public UnityTransport UnityTransportManager;
        private NetworkManager networkManager;
        [Space]
        [Tooltip("We won't use the network load sync system")]
        public bool UseLocalSceneLoading = false;
        [Tooltip("No more additive scene loading")]
        public bool UseSingleSceneLoad = false;
        [SerializeField] private FPNetworkRpc serverRpcSystem;
        public FPNetworkRpc GetFPNetworkRpc { get => serverRpcSystem; }
        public NetworkSequenceStatus InternalNetworkStatus;
        public Camera SetupCam;
        [Tooltip("Fast work around to configure/turn off scene camera")]
        public bool AssumeVROnStart;
        public NetworkManager NetworkManager { get => networkManager; }
        public NetworkSceneManager NetworkSceneManager { get => networkManager.SceneManager; }
        // we always need a gameobject reference for our localized player prefabs
        [Space]
        [Header("Network Prefab References")]
        public GameObject VRPlayerPrefabRef;
        public GameObject LeftHandPrefabRef;
        public GameObject RightHandPrefabRef;
        public GameObject iPadPlayerPrefabRef;

        public FPNetworkData TheSystemData { get => systemData; }
        [Tooltip("This is the scene that is currently loaded via the network system")]
        [SerializeField] protected Scene activeSceneLoaded;
        #region Initial Connection Data Setup
        [Tooltip("Clients that have confirmed 'ready'")]
        [SerializeField] protected Dictionary<ulong,FPNetworkPlayer> readyClients = new Dictionary<ulong,FPNetworkPlayer>();
        [SerializeField] protected Dictionary<ulong,FPInitialConnectionData> initialClientData = new Dictionary<ulong,FPInitialConnectionData>();
        #endregion
        #region Actions/Events
        public string LastAddedScene;
        //John this should be a session variable that automatically syncs using NetCode via Unity
        public string FirstSceneToLoad = "FrenchModuleOne";
        public event Action<ulong, ConnectionStatus> OnClientConnectionNotification;
        //Event for passing the network cache data to the 'server' player off of the spawned networked prefab object
        public event Action<FPNetworkCache> OnClientDisconnectPassNetworkCache;
        public event Action OnServerDisconnectTriggered;
        public event Action<ulong, int> OnServerConfirmationReady;
        public event Action<ulong> OnClientConfirmedReturn;
        public event Action<FPServerData> OnServerEventTriggered;
        public event Action<FPClientData> OnClientEventTriggered;
        public event Action<IPAddress> OnLocalIPAddressTriggered;
        [Space]
        [Tooltip("Fade in VR sphere? Called right before we load a scene via ClientRPC")]
        public UnityEvent OnNetworkAboutToLoadScene;
        /// <summary>
        /// When we load a scene via our NetworkManager --> this is only a local action not a networked one and a way to send information over to our listeners like TellVRServerIPName
        /// </summary>
        public event Action<string, SceneEventProgressStatus, bool> OnSceneLoadedCallBack;
        public event Action<string, bool> OnSceneUnloadedCallBack;
        #endregion
        #region Testing / Player Color
        public Color ServerColor;
        public List<Color> VariousPlayerColors = new List<Color>();
        private int colorIndex = 0;
        #endregion
        #region Event Data Types
        [Space]
        [Header("Generic Events")]
        public FPNetworkServerEvent GenericServerEvent;
        public FPNetworkClientEvent GenericClientEvent;
        #endregion        
        public override void Initialize(bool runAfterLateUpdateLoop, FPNetworkData data = null)
        {
            if (SkipLocalIPAddress)
            {
                return;
            }
            Debug.LogWarning($"Local Ip Address: {GetLocalIPAddress()}");
        }
        public override void Start()
        {
            networkManager = NetworkManager.Singleton;
            networkManager.OnClientConnectedCallback += OnClientConnectedCallback;
            networkManager.OnClientDisconnectCallback += OnClientDisconnectCallback;

            Application.runInBackground = true;
            InternalNetworkStatus = NetworkSequenceStatus.Startup;
            StartCoroutine(DelayStart());
            // if I am using VR my default camera needs to be off
            if (AssumeVROnStart)
            {
                if (SetupCam != null)
                {
                    SetupCam.gameObject.SetActive(false);
                }
            }
        }
        IEnumerator DelayStart()
        {
            yield return new WaitForSecondsRealtime(1f);
            if (!SkipLocalIPAddress)
            {
                var curIP = GetLocalIPAddress();
                Debug.Log($"Current IP: {curIP}");
                OnLocalIPAddressTriggered?.Invoke(CurrentIP);
            }
            else
            {
                var ipAddy = GetOtherLocalIPAddress();
                if(ipAddy == null)
                {
                    CurrentIP = IPAddress.Parse("10.0.0.2");
                }
                else
                {
                    CurrentIP = IPAddress.Parse(ipAddy.ToString());
                }
                //CurrentIP = new IPAddress()
                //OnLocalIPAddressTriggered?.Invoke(CurrentIP);
            }
            
           
            
        }
        #region Generic Starting Server/Client Scene
        /// <summary>
        /// Function called to start the server side of our network system
        /// </summary>
        public void StartServer()
        {
            if (systemData.TheNetworkPlayerType == NetworkPlayerType.Server && networkManager != null)
            {
                Debug.Log("Starting Server");
                //add my RPC component now
                if (serverRpcSystem == null)
                {
                    serverRpcSystem = networkManager.gameObject.AddComponent<FPNetworkRpc>();
                    serverRpcSystem.FPNetworkSystem = this;
                }
                else
                {
                    serverRpcSystem.FPNetworkSystem = this;
                }

                UnityTransportManager.SetConnectionData(CurrentIP.ToString(), PortAddress);
                networkManager.ConnectionApprovalCallback = ApprovalCheck;
                var serverStart = networkManager.StartServer();
                if (serverStart)
                {
                    // Trigger custom FPEvent
                    var newServerData = new FPServerData(GenericServerEvent, "Server Started");
                    TriggerFPServerEvent(newServerData);
                    InternalNetworkStatus = NetworkSequenceStatus.WaitingForClients;
                }
                else
                {
                    Debug.LogError("Failed to start server");
                }
            }
        }
        /// <summary>
        /// This is called via maybe UI/button and/or other server side event to stop the server
        /// This function then manages other events on behalf of invoking shutdown such as
        /// OnServerDisconnectTriggered and NetworkManager.OnServerStopped callback from TellVRServerIPName
        /// </summary>
        public void StopServer()
        {
            if (systemData.TheNetworkPlayerType == NetworkPlayerType.Server && networkManager != null)
            {
                Debug.Log("Stopping Server");
                networkManager.Shutdown();
                //callback occurs from the Shutdown method
                OnServerDisconnectTriggered?.Invoke();
                InternalNetworkStatus = NetworkSequenceStatus.Finishing;
            }
        }
        public void StartClientPlayer(string ipAddressToConnectTo, int portAddress)
        {
            //if we have a valid IP to connect to
            if (systemData.TheNetworkPlayerType == NetworkPlayerType.Client && networkManager != null)
            {
                networkManager.GetComponent<UnityTransport>().SetConnectionData
                (
                    ipAddressToConnectTo,
                    (ushort)portAddress
                );
                // this sets the device type by the data so when we connect to the server it gets this payload
                networkManager.NetworkConfig.ConnectionData = System.Text.Encoding.ASCII.GetBytes(systemData.TheDevicePlayerType.ToString());
                networkManager.StartClient();
                //need to get the client id for myself
                var clientId = networkManager.LocalClientId;
                var newClientData = new FPClientData(clientId, ConnectionStatus.Connecting, GenericClientEvent, "Client Connection Request");
                TriggerFPClientEvent(newClientData);
            }
        }
        public void DisconnectClientPlayer(NetworkObject player)
        {
            // Note: If a client invokes this method, it will throw an exception.
            if (networkManager != null && systemData.TheNetworkPlayerType == NetworkPlayerType.Server)
            {
                networkManager.DisconnectClient(player.OwnerClientId);
            }
        }
        public void DisconnectClientPlayer(ulong player)
        {
            // Note: If a client invokes this method, it will throw an exception.
            if (networkManager != null && systemData.TheNetworkPlayerType == NetworkPlayerType.Server)
            {
                networkManager.DisconnectClient(player);
            }
        }
        public void ConfigureSetupCam(bool activateCam)
        {
            if (SetupCam != null)
            {
                SetupCam.gameObject.SetActive(activateCam);
            }
        }
        /// <summary>
        /// Wrapper function to use the NetworkSceneManager to load the scene we pass it
        /// generally always called via the server side
        /// Coming in From TellVRServerIPName line 842 in the Ineumerator
        /// </summary>
        /// <param name="sceneData"></param>
        public void LoadNetworkScene(string sceneData)
        {
            if (UseLocalSceneLoading)
            {
                if (networkManager.IsServer && InternalNetworkStatus != NetworkSequenceStatus.Active)
                {
                    if (UseSingleSceneLoad)
                    {

                        //need to rpc the client to load
                        GetFPNetworkRpc.SendLoadCommandToClientServerRpc(sceneData);
                        //this loads the server single
                        SceneManager.LoadSceneAsync(sceneData, LoadSceneMode.Single).completed += (op) =>
                        {
                            //lastAddedScene = sceneData;
                            activeSceneLoaded = SceneManager.GetSceneByName(sceneData);
                            InternalNetworkStatus = NetworkSequenceStatus.Active;
                            OnSceneLoadedCallBack?.Invoke(sceneData, SceneEventProgressStatus.Started, true);
                        };
                       
                    }
                    else
                    {
                        //this loads the serer additive
                        SceneManager.LoadSceneAsync(sceneData, LoadSceneMode.Additive).completed += (op) =>
                        {
                            //lastAddedScene = sceneData;
                            activeSceneLoaded = SceneManager.GetSceneByName(sceneData);
                            InternalNetworkStatus = NetworkSequenceStatus.Active;
                            OnSceneLoadedCallBack?.Invoke(sceneData, SceneEventProgressStatus.Started, true);
                        };
                    }
                }
                return;
            }
            //Network load scene
            if (networkManager.IsServer && InternalNetworkStatus != NetworkSequenceStatus.Active)
            {
                SceneEventProgressStatus sceneStatus;
                bool sceneLoaded = true;

                if (UseSingleSceneLoad)
                {
                    sceneStatus = networkManager.SceneManager.LoadScene(sceneData, LoadSceneMode.Single);
                }
                else
                {
                    sceneStatus = networkManager.SceneManager.LoadScene(sceneData, LoadSceneMode.Additive);
                    //unload last other scene
                    //would we unload a previous scene here?
                    /*
                    var lastSceneLoaded = SceneManager.GetSceneByName(LastAddedScene);
                    Debug.Log($"Do we need to unload a previous scene?");
                    if (lastSceneLoaded != null)
                    {
                        //unload this one?
                        Debug.Log($"Unloading previous scene: {LastAddedScene}");
                        var unloadStatus = networkManager.SceneManager.UnloadScene(lastSceneLoaded);
                        if (unloadStatus != SceneEventProgressStatus.Started)
                        {
                            Debug.LogWarning(
                                $"Failed to unload {LastAddedScene} " +
                                $"with a {nameof(SceneEventProgressStatus)}: {unloadStatus}");
                            sceneLoaded = false;
                        }
                        else
                        {
                            //we were able to unload it
                            Debug.Log($"Unloaded previous Scene successfully");
                        }
                    }
                    //update last scene based on new scene information
                    */
                }


                if (sceneStatus != SceneEventProgressStatus.Started)
                {
                    Debug.LogWarning(
                        $"Failed to load {sceneData} " +
                        $"with a {nameof(SceneEventProgressStatus)}: {sceneStatus}");
                    sceneLoaded = false;
                }
                
                LastAddedScene = sceneData;
                activeSceneLoaded = SceneManager.GetSceneByName(sceneData);
                InternalNetworkStatus = NetworkSequenceStatus.Active;
                OnSceneLoadedCallBack?.Invoke(sceneData, sceneStatus, sceneLoaded);
            }
        }
        /// <summary>
        /// Server side only called via a chain reaction across *.ShutDown() via the NetworkManager
        /// </summary>
        /// <param name="sceneData"></param>
        public async void UnloadnetworkScene()
        {
            if (networkManager.IsServer && InternalNetworkStatus == NetworkSequenceStatus.Finishing)
            {
                if (activeSceneLoaded.IsValid())
                {
                    await SceneManager.UnloadSceneAsync(activeSceneLoaded);
                    Debug.LogWarning($"Unloading Async Scene via Server!");
                    InternalNetworkStatus = NetworkSequenceStatus.Startup;
                    activeSceneLoaded = default;
                }
                else
                {
                    activeSceneLoaded = default;
                    //try via the string name
                    var possibleLastScene = SceneManager.GetSceneByName(LastAddedScene);
                    if (possibleLastScene != null)
                    {
                        //unload this one?
                        Debug.Log($"Unloading previous scene: {LastAddedScene}");
                        if (UseLocalSceneLoading)
                        {
                            //unload locally
                            var unloadStatus = SceneManager.UnloadSceneAsync(possibleLastScene);

                        }
                        else
                        {
                            var unloadStatus = networkManager.SceneManager.UnloadScene(possibleLastScene);
                            if (unloadStatus != SceneEventProgressStatus.Started)
                            {
                                Debug.LogWarning(
                                    $"Failed to unload {LastAddedScene} " +
                                    $"with a {nameof(SceneEventProgressStatus)}: {unloadStatus}");
                            }
                            else
                            {
                                //we were able to unload it
                                Debug.Log($"Unloaded previous Scene successfully");

                            }
                        }

                    }
                }
            }
            LastAddedScene = string.Empty;
            Debug.LogWarning($"We can do any other cleanup here on the server side device");
        }
        public async void UnloadNetworkSceneDisconnectedClient()
        {
            //my client has already disconnected and I need to unload my last scene
            if (networkManager.IsClient)
            {
                var scene = SceneManager.GetSceneByName(LastAddedScene);

                if (scene != null)
                {
                    Debug.Log($"Attempting to unload scene: {scene.name}");
                    if (scene.name.Length > 2)
                    {
                        //SceneManager.GetSceneByName(clientCallbackSceneData)
                        //we are basically disconnected and removing the scene via the SceneManager method
                        await SceneManager.UnloadSceneAsync(scene);
                        OnSceneUnloadedCallBack?.Invoke(LastAddedScene, true);
                    }
                    
                    //                    var sceneStatus = networkManager.SceneManager.UnloadScene(scene);
                    /*
                    bool sceneLoaded=true;
                    if (sceneStatus != SceneEventProgressStatus.Started)
                    {
                        Debug.LogWarning($"Failed to unload {lastAddedScene} " +
                                $"with a {nameof(SceneEventProgressStatus)}: {sceneStatus}");
                                sceneLoaded = false;
                    }
                    */
                    //string sceneName = lastAddedScene;

                }
            }
        }
        #endregion
        #region Callbacks
        public override void OnDestroy()
        {
            // Since the NetworkManager can potentially be destroyed before this component, only
            // remove the subscriptions if that singleton still exists.
            if (networkManager != null)
            {
                networkManager.OnClientConnectedCallback -= OnClientConnectedCallback;
                networkManager.OnClientDisconnectCallback -= OnClientDisconnectCallback;
            }
        }
        private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            // this code is running on the server
            // The client identifier to be authenticated
            var clientId = request.ClientNetworkId;

            // Additional connection data defined by user code
            var connectionData = request.Payload;

            //convert connectionData to a string
            var deviceType = System.Text.Encoding.ASCII.GetString(connectionData);
            // DevicePlayerType devicePlayerType;
            // convert the string to the enum
            // use the payload data to determine what type of client this is, VR or iPad
            // use that information to then retrieve my matching prefab that also needs to be in the network prefab list
            // set the playerPrefabHash to that prefab
            Debug.LogWarning($"{Time.time}:Approval Check...{deviceType}");

            if (Enum.TryParse(deviceType, out DevicePlayerType devicePlayerType))
            {
                switch (devicePlayerType)
                {
                    case DevicePlayerType.iPad:
                        //use network prefab list to get the prefab hash
                        //get the prefab list
                        var returnedItem = networkManager.PrefabHandler.GetNetworkPrefabOverride(iPadPlayerPrefabRef);
                        response.PlayerPrefabHash = returnedItem.GetComponent<NetworkObject>().PrefabIdHash;
                        //response.PlayerPrefabHash = iPadPlayerPrefab.GetComponent<NetworkObject>().PrefabIdHash;
                        response.Approved = true;
                        response.CreatePlayerObject = true;
                        Debug.LogWarning($"iPad Player Approved");
                        break;
                    case DevicePlayerType.MetaQuest:
                        var returnedItemVR = networkManager.PrefabHandler.GetNetworkPrefabOverride(VRPlayerPrefabRef);
                        response.PlayerPrefabHash = returnedItemVR.GetComponent<NetworkObject>().PrefabIdHash;
                        response.Approved = true;
                        response.CreatePlayerObject = true;
                        //JOHN need to generate and spawn the controllers here
                        Debug.LogWarning($"MetaQuest Player Approved");
                        // setup data here?
                        break;
                    default:
                        response.PlayerPrefabHash = null;
                        response.Approved = false;
                        response.CreatePlayerObject = false;
                        Debug.LogError($"Failed to register the correct device type, {deviceType}");
                        response.Reason = $"Failed to register the correct device type, {deviceType}";
                        break;
                }
            }

            // The Prefab hash value of the NetworkPrefab, if null the default NetworkManager player Prefab is used
            // alter position and rotation later based on connection data and/or sequence of connection
            response.Position = Vector3.zero;
            response.Rotation = Quaternion.identity;

            // If additional approval steps are needed, set this to true until the additional steps are complete
            // once it transitions from true to false the connection approval response will be processed.
            response.Pending = false;
        }
        /// <summary>
        /// Object Spawning via NetworkManager 
        /// https://docs-multiplayer.unity3d.com/netcode/current/basics/object-spawning/
        /// </summary>
        /// <param name="clientId"></param>
        private void OnClientConnectedCallback(ulong clientId)
        {
            #region Server Side Only
            if (networkManager.IsServer)
            {
                Debug.LogWarning($"[Server]: OnClientConnectedCallBack");
                InternalNetworkStatus = NetworkSequenceStatus.ConfirmScene;

                // Send a color string to the newly connected client
                if (colorIndex > VariousPlayerColors.Count)
                {
                    colorIndex = 0;
                }
                var color = VariousPlayerColors[colorIndex];
                var colorString = ColorUtility.ToHtmlStringRGB(color);
                Debug.Log($"[Server]: Color pulled: {colorString}, {VariousPlayerColors[colorIndex]}");

                var player = networkManager.ConnectedClients[clientId].PlayerObject.GetComponent<FPNetworkPlayer>();
                if (player != null)
                {
                    Debug.LogWarning($"[Server]: Found Player by clientId {clientId}");
                    // setup our server data for each client so when it's time we can pull this information for the client
                    FPInitialConnectionData clientData = new FPInitialConnectionData()
                    {
                        PlayerColor = colorString,
                        PlayerType = player.ThePlayerType,
                        SceneToLoad = FirstSceneToLoad
                    };


                    ClientRpcParams clientRpcParams = new ClientRpcParams
                    {
                        Send = new ClientRpcSendParams
                        {
                            TargetClientIds = new ulong[] { clientId }
                        }
                    };
                    // server side color updates
                    if (serverRpcSystem != null)
                    {
                        serverRpcSystem.SendColorToClientServerRpc(colorString, clientId);
                        //serverRpcSystem.BroadcastVisualUpdateClientRpc(colorString, clientId);
                        colorIndex++;
                    }
                    //if we are doing a local scene load
                    //server side scene information
                    if (UseLocalSceneLoading)
                    {
                        Debug.LogWarning($"[Server]: Scene that we need to send to our client = {FirstSceneToLoad}");
                        serverRpcSystem.ApplySceneDataToClientRpc(FirstSceneToLoad, clientRpcParams);
                    }
                    //hands if you are VR type
                    if (player.ThePlayerType == DevicePlayerType.MetaQuest)
                    {
                        //spawn the hands
                        Debug.LogError($"[Server]:VR Setup, spawning hands for client: {clientId}");
                        var leftHandPrefab = Instantiate(networkManager.PrefabHandler.GetNetworkPrefabOverride(LeftHandPrefabRef));
                        var leftNetObj = leftHandPrefab.GetComponent<NetworkObject>();
                        leftNetObj.Spawn();

                        var rightHandPrefab = Instantiate(networkManager.PrefabHandler.GetNetworkPrefabOverride(RightHandPrefabRef));
                        var rightNetObj = rightHandPrefab.GetComponent<NetworkObject>();
                        rightNetObj.Spawn();
                        //add in payload information
                        clientData.NetworkIDPayloadA = leftNetObj.NetworkObjectId;
                        clientData.NetworkIDPayloadB = rightNetObj.NetworkObjectId;
                    }
                    //add our client data to the dictionary so it can be pulled out once we hear back from the client
                    if (initialClientData.ContainsKey(clientId))
                    {
                        initialClientData.Remove(clientId);
                    }
                    Debug.LogWarning($"[Server]: Adding data to my client dictionary, {clientId} with a type of {clientData.PlayerType}");
                    initialClientData.Add(clientId, clientData);
                }
                // check our connected client counts after doing everything else
                OnServerConfirmationReady?.Invoke(clientId, networkManager.ConnectedClients.Count);
            }
            else
            {
                Debug.LogWarning($"[Client]: OnClientConnectedCallBack");
                //notify server that I'm "Ready" for information
                var player = networkManager.ConnectedClients[clientId].PlayerObject.GetComponent<FPNetworkPlayer>();
                if (player != null)
                {
                    Debug.LogWarning($"[Client]: Calling Coroutine Connection Sequence Finished");
                    StartCoroutine(player.OnClientFinishedConnectionSequence());
                }
            }
            #endregion
            OnClientConnectionNotification?.Invoke(clientId, ConnectionStatus.Connected);
            var connectionEvent = new FPClientData(clientId, ConnectionStatus.Connected, GenericClientEvent, "Client Connection Callback");
            TriggerFPClientEvent(connectionEvent);
        }
        
        /// <summary>
        /// Called via FPNetworkPlayer under the 'server' player type
        /// </summary>
        /// <param name="clientId"></param>
        public void OnClientConfirmed(ulong clientId)
        {
            Debug.Log($"Client Confirmed: {clientId}");
            OnClientConfirmedReturn?.Invoke(clientId);
        }
        /// <summary>
        /// Called on both server/client when a disconnection occurs
        /// </summary>
        /// <param name="clientId"></param>
        private void OnClientDisconnectCallback(ulong clientId)
        {

            if (networkManager.IsServer)
            {
                Debug.Log($"Server: Client Disconnected: {clientId}");
                //cache data from client
                var networkClientObj = ReturnLocalClientObject(clientId);

                if (networkClientObj != null)
                {

                    // Get the player object associated with this client
                    var playerNetworkObject = networkClientObj.PlayerObject;
                    if (playerNetworkObject != null)
                    {
                        Debug.Log($"Server: Found Local Client Object: {networkClientObj.PlayerObject.name}");
                        if (playerNetworkObject.GetComponent<FPNetworkCache>())
                        {
                            OnClientDisconnectPassNetworkCache?.Invoke(playerNetworkObject.GetComponent<FPNetworkCache>());
                        }
                    }
                }
                //player object

            }
            if (networkManager.IsClient)
            {
                //turn on my camera again
                ConfigureSetupCam(true);
            }
            OnClientConnectionNotification?.Invoke(clientId, ConnectionStatus.Disconnected);
            var connectionEvent = new FPClientData(clientId, ConnectionStatus.Disconnected, GenericClientEvent, "Client Disconnection Callback");
            TriggerFPClientEvent(connectionEvent);
            if (!networkManager.IsServer && networkManager.DisconnectReason != string.Empty)
            {
                Debug.Log($"Approval Declined Reason: {networkManager.DisconnectReason}");
            }
        }
        /// <summary>
        /// Called from the client to the server, rpc after getting connected to let server know we are ready for info
        /// </summary>
        /// <param name="clientId"></param>
        public void ServerOnClientReady(ulong clientId)
        {
            //server is processing this
            var player = ReturnLocalClientObject(clientId);
            if (player != null)
            {
                var fpNetworkP = player.PlayerObject.GetComponent<FPNetworkPlayer>();
                if (fpNetworkP != null)
                {
                    if (!readyClients.ContainsKey(clientId))
                    {
                        readyClients.Add(clientId, fpNetworkP);
                    }
                    // now pull the data from the server and send it to the client
                    Debug.Log($"Sending initial data to client {clientId}");
                    ClientRpcParams clientRpcParams = new ClientRpcParams
                    {
                        Send = new ClientRpcSendParams
                        {
                            TargetClientIds = new ulong[] { clientId }
                        }
                    };
                    // pull server data
                    var someData = initialClientData[clientId];
                    fpNetworkP.SendInitialSetupClientRpc(someData, clientRpcParams);
                }
            }
        }
        #endregion
        #region Standard FP Network Actions-Events
        private void TriggerFPServerEvent(FPServerData serverData)
        {
            OnServerEventTriggered?.Invoke(serverData);
        }
        private void TriggerFPClientEvent(FPClientData clientData)
        {
            OnClientEventTriggered?.Invoke(clientData);
        }
        #endregion
        #region Public Access Methods
        public void UpdateNetworkData(FPNetworkData data)
        {
            //only be called if we are in the setup process and not actually running anything and/or we aren't connected to anything
            if (networkManager.NetworkConfig.ConnectionData.Length == 0)
            {
                Debug.Log($"Updated Network Data Configuration passed {data.TheDevicePlayerType} and {data.TheNetworkPlayerType}");
                systemData = data;
            }
        }
        public void UpdateLastSceneFromClient(string scene)
        {
            LastAddedScene = scene;
            Debug.Log($"Client last scene updated to: {LastAddedScene}");
        }
        public ulong GetLocalClientID()
        {
            return networkManager.LocalClientId;
        }
        public NetworkClient ReturnLocalClientObject(ulong localClientId)
        {
            if (networkManager.ConnectedClients.TryGetValue(localClientId, out var client))
            {
                return client;
            }
            return null;
        }
        #endregion
        #region Local IP Address by Platform
        public string GetOtherLocalIPAddress()
        {
            string localIP = null;

            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    // Skip IPv6 and loopback addresses
                    if (ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip))
                    {
                        localIP = ip.ToString();
                        break;
                    }
                }

                if (string.IsNullOrEmpty(localIP))
                {
                    localIP = "IP Not Found";
                }
            }
            catch (System.Exception e)
            {
                localIP = $"Error: {e.Message}";
                return null;
            }

            return localIP;
        }

        public string GetLocalIPAddress()
        {
            string localIP = string.Empty;

#if UNITY_IOS && !UNITY_EDITOR
            localIP = GetLocalIPAddressiOS();
#elif UNITY_ANDROID && !UNITY_EDITOR
            localIP = GetLocalIPAddressAndroid();
#else
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus == OperationalStatus.Up &&
                    ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                {
                    foreach (var ip in ni.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            localIP = ip.Address.ToString();
                            CurrentIP = ip.Address;
                            return localIP;
                        }
                    }
                }
            }
#endif

            if (string.IsNullOrEmpty(localIP))
                Debug.LogError("Local IP address not found.");
            else
                CurrentIP = IPAddress.Parse(localIP);

            return localIP;
        }

        // iOS and Android specific implementations
#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern IntPtr _GetWiFiIPAddress();

        private string GetLocalIPAddressiOS()
        {
            IntPtr ipPtr = _GetWiFiIPAddress();
            string ip = Marshal.PtrToStringAnsi(ipPtr);
            Debug.Log($"[iOS] IP: {ip}");
            return ip;
        }
#else
        private string GetLocalIPAddressiOS() => "127.0.0.1";
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
        private string GetLocalIPAddressAndroid()
        {
            string ipAddress = string.Empty;
            try
            {
                using (AndroidJavaClass wifiManagerClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                {
                    AndroidJavaObject activity = wifiManagerClass.GetStatic<AndroidJavaObject>("currentActivity");
                    AndroidJavaObject wifiManager = activity.Call<AndroidJavaObject>("getSystemService", "wifi");
                    AndroidJavaObject wifiInfo = wifiManager.Call<AndroidJavaObject>("getConnectionInfo");
                    int ip = wifiInfo.Call<int>("getIpAddress");

                    ipAddress = string.Format("{0}.{1}.{2}.{3}",
                        (ip & 0xff),
                        (ip >> 8 & 0xff),
                        (ip >> 16 & 0xff),
                        (ip >> 24 & 0xff));
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error getting Android local IP: {e}");
            }

            return ipAddress;
        }
#endif
        #endregion



    }
}
