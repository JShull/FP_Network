namespace FuzzPhyte.Network
{
    /// References 
    /// <summary>
    /// https://docs-multiplayer.unity3d.com/netcode/current/components/networkmanager/
    /// https://docs-multiplayer.unity3d.com/netcode/current/basics/playerobjects/
    /// https://docs-multiplayer.unity3d.com/netcode/current/basics/connection-approval/
    /// </summary>
    using FuzzPhyte.Utility.FPSystem;
    using UnityEngine;
    using System;
    using System.Net.NetworkInformation;
    using System.Net.Sockets;
    using System.Net;
    using Unity.Netcode;
    using Unity.Netcode.Transports.UTP;
    using FuzzPhyte.SystemEvent;
    public enum ConnectionStatus
    {
        Connected,
        Disconnected,
        Connecting,
        Disconnecting,
    }
    /// <summary>
    /// Used to help manage a similar structure between my derived FPEvent classes associated with the network system
    /// </summary>
    public interface IFPNetworkEvent{
        void SetupEvent();
        void DebugEvent();
    }

    public class FPNetworkSystem : FPSystemBase<FPNetworkData>
    {
        //public FPNetworkData TestData;
        public IPAddress CurrentIP;
        public ushort PortAddress = 7777;
        public UnityTransport UnityTransporManager;
        private NetworkManager networkManager;
        public GameObject VRPlayerPrefab;
        public GameObject iPadPlayerPrefab;
        public FPNetworkData TheSystemData { get => systemData;}
        #region Actions/Events
        public Transform EventClientManager;
        public Transform EventServerManager;
        //protected FP_EventManager<FPNetworkClientEvent> clientEventManager;
        //protected FP_EventManager<FPNetworkServerEvent> serverEventManager;
        public event Action<ulong, ConnectionStatus> OnClientConnectionNotification;
        public event Action<FPServerData> OnServerEventTriggered;
        public event Action<FPClientData> OnClientEventTriggered;
        #endregion
        #region Event Data Types
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
            //setup FP_EventManager(s)
            //EventServerManager.gameObject.AddComponent<FP_EventManager<FPNetworkServerEvent>>();
            //EventClientManager.gameObject.AddComponent<FP_EventManager<FPNetworkClientEvent>>();
            //clientEventManager=EventClientManager.GetComponent<FP_EventManager<FPNetworkClientEvent>>();
            //serverEventManager=EventServerManager.GetComponent<FP_EventManager<FPNetworkServerEvent>>();
            //
            networkManager = NetworkManager.Singleton;
            networkManager.OnClientConnectedCallback += OnClientConnectedCallback;
            networkManager.OnClientDisconnectCallback += OnClientDisconnectCallback;
            //networkManager.PrefabHandler.
            var curIP = GetLocalIPAddress();
            Debug.Log($"Current IP: {curIP}");
        }
        public void StartServer()
        {
            if(systemData.TheNetworkPlayerType == NetworkPlayerType.Server && networkManager!=null)
            {
                Debug.Log("Starting Server");
                UnityTransporManager.SetConnectionData(CurrentIP.ToString(), PortAddress);
                networkManager.ConnectionApprovalCallback = ApprovalCheck;
                networkManager.StartServer();
                // Trigger custom FPEvent
                var newServerData = new FPServerData(GenericServerEvent, "Server Started");
                TriggerFPServerEvent(newServerData);
            }
        }
        public void StopServer()
        {
            if(systemData.TheNetworkPlayerType == NetworkPlayerType.Server && networkManager!=null)
            {
                Debug.Log("Stopping Server");
                networkManager.Shutdown();
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
            //DevicePlayerType devicePlayerType;
            //convert the string to the enum
            // use the payload data to determine what type of client this is, VR or iPad
            // use that information to then retrieve my matching prefab that also needs to be in the network prefab list
            // set the playerPrefabHash to that prefab

            if(Enum.TryParse(deviceType, out DevicePlayerType devicePlayerType)){
                switch(devicePlayerType)
                {
                    case DevicePlayerType.iPad:
                        response.PlayerPrefabHash = iPadPlayerPrefab.GetComponent<NetworkObject>().PrefabIdHash;
                        response.Approved = true;
                        response.CreatePlayerObject = true;
                        break;
                    case DevicePlayerType.MetaQuest:
                        response.PlayerPrefabHash = VRPlayerPrefab.GetComponent<NetworkObject>().PrefabIdHash;
                        response.Approved = true;
                        response.CreatePlayerObject = true;
                        break;
                    default:
                        response.PlayerPrefabHash = null;
                        response.Approved = false;
                        response.CreatePlayerObject = false;
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

        private void OnClientConnectedCallback(ulong clientId)
        {
            OnClientConnectionNotification?.Invoke(clientId, ConnectionStatus.Connected);
            
            var connectionEvent = new FPClientData(clientId, ConnectionStatus.Connected,GenericClientEvent,"Client Connection Callback");
            TriggerFPClientEvent(connectionEvent);
        }

        private void OnClientDisconnectCallback(ulong clientId)
        {
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
        public string GetLocalIPAddress()
        {
            string localIP = string.Empty;
            
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

            if (string.IsNullOrEmpty(localIP))
            {
                Debug.LogError("Local IP address not found.");
            }

            return localIP;
        }
        #endregion
    }
}
