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
    using System.Runtime.InteropServices;
    using System.Collections.Generic;
    using System.Collections;

    #region Network Related Enums
    [Serializable]
    public enum NetworkSequenceStatus
    {
        None = 0,
        Startup = 1,
        WaitingForClients = 2,
        ConfirmScene = 3,
        Active = 4,
        Finishing = 5,
        QA = 10,
        Done = 86,
        DEBUG = 99
    }
    [Serializable]
    public enum ConnectionStatus
    {
        Connected,
        Disconnected,
        Connecting,
        Disconnecting,
    }
    [Serializable]
    public enum DevicePlayerType
    {
        None,
        iPad,
        MetaQuest
    }
    [Serializable]
    public enum NetworkPlayerType
    {
        None,
        Server,
        Client,
        Host
    }
    
    [Serializable]
    public enum NetworkMessageType
    {
        None,
        ServerConfirmation,
        ClientConfirmed,
        ClientChoice,
        ClientInteraction,
        ClientLocationUpdate,
        ClientImage,
        ClientMessage,
        ClientDisconnectRequest
    }
    #endregion
    /// <summary>
    /// Used to help manage a similar structure between my derived FPEvent classes associated with the network system
    /// </summary>
    public interface IFPNetworkEvent{
        void SetupEvent();
        void DebugEvent();
    }
    public interface IFPNetworkPlayerSetup
    {
        void SetupSystem(FPNetworkPlayer player);
        void RegisterOtherObjects(NetworkObject networkObject,FPNetworkPlayer player);
    }
    public interface IFPNetworkOtherObjectSetup
    {
        void SetupSystem(FPNetworkOtherObject otherObject);
    }
    public interface IFPNetworkProxySetup
    {
        void OnClientSpawned();
        void OnServerSpawned();
        void OnNetworkSpawn();
        /// <summary>
        /// Should result in calling a ServerRPC
        /// This is a wrapper function to take information in from some other script and by using a 
        /// </summary>
        /// <param name="position"></param>
        /// <param name="rotation"></param>
        void UpdatePositionAndRotation(Vector3 position, Quaternion rotation);
    }
    public class FPNetworkSystem : FPSystemBase<FPNetworkData>
    {
        public IPAddress CurrentIP;
        public ushort PortAddress = 7777;
        public UnityTransport UnityTransportManager;
        private NetworkManager networkManager;
        [SerializeField]private FPNetworkRpc serverRpcSystem;
        public FPNetworkRpc GetFPNetworkRpc { get => serverRpcSystem;}
        public NetworkSequenceStatus InternalNetworkStatus;
        public Camera SetupCam;
        [Tooltip("Fast work around to configure/turn off scene camera")]
        public bool AssumeVROnStart;
        public NetworkManager NetworkManager { get => networkManager;}
        public NetworkSceneManager NetworkSceneManager { get => networkManager.SceneManager;}
        // we always need a gameobject reference for our localized player prefabs
        public GameObject VRPlayerPrefabRef;
        public GameObject LeftHandPrefabRef;
        public GameObject RightHandPrefabRef;
        public GameObject iPadPlayerPrefabRef;
        public FPNetworkData TheSystemData { get => systemData;}
        [Tooltip("This is the scene that is currently loaded via the network system")]
        [SerializeField]protected Scene activeSceneLoaded;
        #region Actions/Events
        public string lastAddedScene;
        public event Action<ulong, ConnectionStatus> OnClientConnectionNotification;
        //Event for passing the network cache data to the 'server' player off of the spawned networked prefab object
        public event Action<FPNetworkCache> OnClientDisconnectPassNetworkCache;
        public event Action OnServerDisconnectTriggered;
        public event Action<ulong,int> OnServerConfirmationReady;
        public event Action<ulong> OnClientConfirmedReturn;
        public event Action<FPServerData> OnServerEventTriggered;
        public event Action<FPClientData> OnClientEventTriggered;
        public event Action<IPAddress> OnLocalIPAddressTriggered;
        /// <summary>
        /// When we load a scene via our NetworkManager --> this is only a local action not a networked one and a way to send information over to our listeners like TellVRServerIPName
        /// </summary>
        public event Action<string,SceneEventProgressStatus,bool> OnSceneLoadedCallBack;
        public event Action<string,bool> OnSceneUnloadedCallBack;
        #endregion
        #region Testing / Player Color
        public Color ServerColor;
        public List<Color>VariousPlayerColors = new List<Color>();
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
            //base.Initialize(RunAfterLateUpdateLoop, data);
            Debug.LogWarning($"Local Ip Address: {GetLocalIPAddress()}");
            //systemData = data;
        }
        public override void Start()
        {
            networkManager = NetworkManager.Singleton;
            networkManager.OnClientConnectedCallback += OnClientConnectedCallback;
            networkManager.OnClientDisconnectCallback += OnClientDisconnectCallback;
            //networkManager.ConnectionApprovalCallback = ApprovalCheck;
            //networkManager.PrefabHandler.
            
            Application.runInBackground = true;
            InternalNetworkStatus = NetworkSequenceStatus.Startup;
            StartCoroutine(DelayStart());
            // if I am using VR my default camera needs to be off
            if (AssumeVROnStart)
            {
                if(SetupCam!=null)
                {
                    SetupCam.gameObject.SetActive(false);
                }
            }
        }
        IEnumerator DelayStart()
        {
            yield return new WaitForSecondsRealtime(1f);
            var curIP = GetLocalIPAddress();
            Debug.Log($"Current IP: {curIP}");
            OnLocalIPAddressTriggered?.Invoke(CurrentIP);
        }

        /// <summary>
        /// Function called to start the server side of our network system
        /// </summary>
        public void StartServer()
        {
            if(systemData.TheNetworkPlayerType == NetworkPlayerType.Server && networkManager!=null)
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
                if(serverStart)
                {
                    // Trigger custom FPEvent
                    var newServerData = new FPServerData(GenericServerEvent, "Server Started");
                    TriggerFPServerEvent(newServerData);
                    InternalNetworkStatus = NetworkSequenceStatus.WaitingForClients;
                }else{
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
            if(systemData.TheNetworkPlayerType == NetworkPlayerType.Server && networkManager!=null)
            {
                Debug.Log("Stopping Server");
                networkManager.Shutdown();
                //callback occurs from the Shutdown method
                OnServerDisconnectTriggered?.Invoke();
                InternalNetworkStatus = NetworkSequenceStatus.Finishing;
            }
        }
        public void StartClientPlayer(string ipAddressToConnectTo,int portAddress)
        {
            //if we have a valid IP to connect to
            if(systemData.TheNetworkPlayerType==NetworkPlayerType.Client && networkManager!=null)
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
                var newClientData = new FPClientData(clientId,ConnectionStatus.Connecting,GenericClientEvent,"Client Connection Request");
                TriggerFPClientEvent(newClientData);
            }   
        }
        public void DisconnectClientPlayer(NetworkObject player)
        {   
            // Note: If a client invokes this method, it will throw an exception.
            if(networkManager!=null && systemData.TheNetworkPlayerType == NetworkPlayerType.Server)
            {
                networkManager.DisconnectClient(player.OwnerClientId);
            }
        }
        public void DisconnectClientPlayer(ulong player)
        {
            // Note: If a client invokes this method, it will throw an exception.
            if(networkManager!=null && systemData.TheNetworkPlayerType == NetworkPlayerType.Server)
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
        /// </summary>
        /// <param name="sceneData"></param>
        public void LoadNetworkScene(string sceneData)
        {
            //Network load scene
            if (networkManager.IsServer && InternalNetworkStatus!=NetworkSequenceStatus.Active)
            {

                var sceneStatus = networkManager.SceneManager.LoadScene(sceneData, LoadSceneMode.Additive);
                bool sceneLoaded=true;
                if (sceneStatus != SceneEventProgressStatus.Started)
                {
                    Debug.LogWarning(
                        $"Failed to load {sceneData} " +
                        $"with a {nameof(SceneEventProgressStatus)}: {sceneStatus}");
                        sceneLoaded = false;
                }else{
                    //would we unload a previous scene here?

                    var lastSceneLoaded = SceneManager.GetSceneByName(lastAddedScene);
                    Debug.Log($"Do we need to unload a previous scene?");
                    if(lastSceneLoaded!=null)
                    {
                        //unload this one?
                        Debug.Log($"Unloading previous scene: {lastAddedScene}");
                        var unloadStatus = networkManager.SceneManager.UnloadScene(lastSceneLoaded);
                        if (unloadStatus != SceneEventProgressStatus.Started)
                        {
                            Debug.LogWarning(
                                $"Failed to unload {lastAddedScene} " +
                                $"with a {nameof(SceneEventProgressStatus)}: {unloadStatus}");
                                sceneLoaded = false;
                        }else{
                            //we were able to unload it
                            Debug.Log($"Unloaded previous Scene successfully");
                        }
                    }
                    //update last scene based on new scene information
                    
                }
                lastAddedScene = sceneData;
                activeSceneLoaded = SceneManager.GetSceneByName(sceneData);
                InternalNetworkStatus = NetworkSequenceStatus.Active;
                OnSceneLoadedCallBack?.Invoke(sceneData,sceneStatus,sceneLoaded);
            }
        }
        /// <summary>
        /// Server side only called via a chain reaction across *.ShutDown() via the NetworkManager
        /// </summary>
        /// <param name="sceneData"></param>
        public async void UnloadnetworkScene()
        {
            if (networkManager.IsServer && InternalNetworkStatus==NetworkSequenceStatus.Finishing)
            {
                if(activeSceneLoaded.IsValid())
                {
                    await SceneManager.UnloadSceneAsync(activeSceneLoaded);
                    Debug.LogWarning($"Unloading Async Scene via Server!");
                    InternalNetworkStatus = NetworkSequenceStatus.Startup;
                    activeSceneLoaded = default;
                }else{
                    activeSceneLoaded = default;
                    //try via the string name
                    var possibleLastScene = SceneManager.GetSceneByName(lastAddedScene);
                    if(possibleLastScene!=null)
                    {
                        //unload this one?
                        Debug.Log($"Unloading previous scene: {lastAddedScene}");
                        var unloadStatus = networkManager.SceneManager.UnloadScene(possibleLastScene);
                        if (unloadStatus != SceneEventProgressStatus.Started)
                        {
                            Debug.LogWarning(
                                $"Failed to unload {lastAddedScene} " +
                                $"with a {nameof(SceneEventProgressStatus)}: {unloadStatus}");
                        }else{
                            //we were able to unload it
                            Debug.Log($"Unloaded previous Scene successfully");
                            
                        }
                    }
                }
            }
            lastAddedScene = string.Empty;
            Debug.LogWarning($"We can do any other cleanup here on the server side device");
        }
        public async void UnloadNetworkSceneDisconnectedClient()
        {
            //my client has already disconnected and I need to unload my last scene
            if(networkManager.IsClient)
            {
                var scene = SceneManager.GetSceneByName(lastAddedScene);
                if(scene!=null)
                {
                    //SceneManager.GetSceneByName(clientCallbackSceneData)
                    //we are basically disconnected and removing the scene via the SceneManager method
                    await SceneManager.UnloadSceneAsync(scene);
                    OnSceneUnloadedCallBack?.Invoke(lastAddedScene,true);
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
            
            if(Enum.TryParse(deviceType, out DevicePlayerType devicePlayerType)){
                switch(devicePlayerType)
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
                // check our connected client counts
                
                Debug.Log($"Server: OnClientConnectedCallBack");
                OnServerConfirmationReady?.Invoke(clientId, networkManager.ConnectedClients.Count);
                InternalNetworkStatus = NetworkSequenceStatus.ConfirmScene;
                
                // Send a color string to the newly connected client
                if(colorIndex>VariousPlayerColors.Count)
                {
                    colorIndex=0;
                }
                var color = VariousPlayerColors[colorIndex];
                var colorString = ColorUtility.ToHtmlStringRGB(color);
                Debug.Log($"Server Color pulled: {colorString}, {VariousPlayerColors[colorIndex]}");
                var player = networkManager.ConnectedClients[clientId].PlayerObject.GetComponent<FPNetworkPlayer>();
                if (player != null)
                {
                    if (serverRpcSystem != null)
                    {
                        serverRpcSystem.SendColorToClientServerRpc(colorString, clientId);
                        serverRpcSystem.BroadcastVisualUpdateClientRpc(colorString, clientId);
                        colorIndex++;
                    }
                    //hands if you are VR type
                    if (player.ThePlayerType  == DevicePlayerType.MetaQuest)
                    {
                        Debug.Log($"[VR Setup] Spawning hands for client: {clientId}");
                        var leftHandPrefab = Instantiate(networkManager.PrefabHandler.GetNetworkPrefabOverride(LeftHandPrefabRef));
                        var leftNetObj = leftHandPrefab.GetComponent<NetworkObject>();
                        leftNetObj.SpawnWithOwnership(clientId);
                        
                        var rightHandPrefab = Instantiate(networkManager.PrefabHandler.GetNetworkPrefabOverride(RightHandPrefabRef));
                        var rightNetObj = rightHandPrefab.GetComponent<NetworkObject>();
                        rightNetObj.SpawnWithOwnership(clientId);

                        player.RegisterOtherObjects(leftNetObj, rightNetObj); // Assumes this method exists
                    }
                }

            }
            #endregion
            OnClientConnectionNotification?.Invoke(clientId, ConnectionStatus.Connected);
            var connectionEvent = new FPClientData(clientId, ConnectionStatus.Connected,GenericClientEvent,"Client Connection Callback");
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
        private void OnClientDisconnectCallback(ulong clientId)
        {
            
            if(networkManager.IsServer)
            {
                Debug.Log($"Server: Client Disconnected: {clientId}");
                //cache data from client
                var networkClientObj = ReturnLocalClientObject(clientId);
                
                if(networkClientObj!=null)
                {
                    
                    // Get the player object associated with this client
                    var playerNetworkObject = networkClientObj.PlayerObject;
                    if(playerNetworkObject!=null)
                    {
                        Debug.Log($"Server: Found Local Client Object: {networkClientObj.PlayerObject.name}");
                        if(playerNetworkObject.GetComponent<FPNetworkCache>())
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
            var connectionEvent = new FPClientData(clientId, ConnectionStatus.Disconnected,GenericClientEvent, "Client Disconnection Callback");
            TriggerFPClientEvent(connectionEvent);
            if (!networkManager.IsServer && networkManager.DisconnectReason != string.Empty)
            {
                Debug.Log($"Approval Declined Reason: {networkManager.DisconnectReason}");
            }
        }
        
        #endregion
        #region Standard FP Network events
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
            if(networkManager.NetworkConfig.ConnectionData.Length == 0)
            {
                Debug.Log($"Updated Network Data Configuration passed { data.TheDevicePlayerType} and {data.TheNetworkPlayerType}");
                systemData = data;
            }
        }
        public void UpdateLastSceneFromClient(string scene)
        {
            lastAddedScene = scene;
            Debug.Log($"Client last scene updated to: {lastAddedScene}");
        }
        public ulong GetLocalClientID()
        {
            return networkManager.LocalClientId;
        }
        public NetworkClient ReturnLocalClientObject(ulong localClientId)
        {
            if(networkManager.ConnectedClients.TryGetValue(localClientId, out var client)){
                return client;
            }
            return null;
        }
#region Local IP Address by Platform
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


/*
        public string GetLocalIPAddress()
        {
            string localIP = string.Empty;

#if UNITY_STANDALONE_WIN
            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                // Check if the adapter is operational and it's a wireless LAN adapter
                if (ni.OperationalStatus == OperationalStatus.Up &&
                    ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
                {
                    foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            localIP = ip.Address.ToString();
                            CurrentIP = ip.Address;
                            break;
                        }
                    }
                }
                if (!string.IsNullOrEmpty(localIP))
                    break;
            }
#else
            // Loop through all network interfaces
            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                // Check if the network interface is up and not a loopback (ignore loopback addresses)
                if (ni.OperationalStatus == OperationalStatus.Up &&
                    ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                {
                    // Get the IP properties of the interface
                    foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
                    {
                        // Check if it's an IPv4 address
                        if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            localIP = ip.Address.ToString();
                            CurrentIP = ip.Address;
                            break;
                        }
                    }
                }
                
                // If we found a valid IP, break the loop
                if (!string.IsNullOrEmpty(localIP))
                    break;
            }
#endif
            if (string.IsNullOrEmpty(localIP))
            {
                Debug.LogError("Local IP address not found.");
            }
            return localIP;
        }
        */
#endregion
    }
}
