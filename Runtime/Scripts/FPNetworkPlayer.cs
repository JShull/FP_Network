
namespace FuzzPhyte.Network
{
    using UnityEngine;
    using Unity.Netcode;
    using TMPro;
    using FuzzPhyte.Utility.FPSystem;

    public class FPNetworkPlayer : NetworkBehaviour
    {
        public DevicePlayerType ThePlayerType;
        public TextMeshProUGUI DebugText;
        public Canvas TheUIClientCanvas;
        [Tooltip("Panel for UI Confirmation after connection")]
        public GameObject TheClientConfirmUIPanel;
        private ulong myClientID;
        private FPNetworkSystem networkSystem;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            networkSystem = FPSystemBase<FPNetworkData>.Instance as FPNetworkSystem;
            if (networkSystem == null)
            {
                Debug.LogError("$not finding the FPNetworkSystem in the scene.");
            }
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
                    break;
                case DevicePlayerType.MetaQuest:
                    Debug.Log("MetaQuest Player Spawned");
                    break;
                default:
                    Debug.Log("Player Spawned");
                    break;
            }
            myClientID = NetworkManager.Singleton.LocalClientId;

        }
        public void OnServerSpawned()
        {
            TheUIClientCanvas.enabled = false;
        }
        /// <summary>
        /// Local call from some UI element for Testing
        /// </summary>
        /// <param name="details"></param>
        /// <returns>data struct we invoked on the server</returns>
        public void UISendServerEventDetails(string details,NetworkMessageType msgType)
        {
            FPNetworkDataStruct data = new FPNetworkDataStruct();
            data.TheDevicePlayerType = ThePlayerType;
            data.TheNetworkPlayerType = NetworkPlayerType.Client;
            data.TheNetworkMessageType = msgType;
            data.TheNetworkMessage = details;
            data.TheClientID = myClientID;
            data.ClientIPAddress = networkSystem.CurrentIP.ToString();
            // Send to Server Rpc
            SendServerInteractionEventRpc(1, data);
        }
        /// <summary>
        /// Called via Confirm UI Button
        /// </summary>
        /// <param name="details"></param>
        public void UISendServerConfirmationDetails(string details) 
        {
            FPNetworkDataStruct data = new FPNetworkDataStruct();
            data.TheDevicePlayerType = ThePlayerType;
            data.TheNetworkPlayerType = NetworkPlayerType.Client;
            data.TheNetworkMessageType = NetworkMessageType.ClientConfirmed;
            data.TheNetworkMessage = details;
            data.TheClientID = myClientID;
            data.ClientIPAddress = networkSystem.CurrentIP.ToString();
            // Send to Server Rpc
            ClientReadyServerRpc(data);
        }
        #region RPC Methods

        #region Rpcs Server, runs on server then sends to client
        [Rpc(SendTo.Server)]
        public void SendServerInteractionEventRpc(int pingCount,FPNetworkDataStruct msgData,RpcParams rpcParams=default)
        {
            DebugText.text = $"Interaction Event: {pingCount}\nMessage: '{msgData.TheNetworkMessage}'\nDevice Type {msgData.TheDevicePlayerType}";
            //add data to cache
            if (FPNetworkCache.Instance != null)
            {
                //using both data processes for the moment
                FPNetworkCache.Instance.AddData(rpcParams.Receive.SenderClientId, msgData);
                FPNetworkCache.Instance.AddData(networkSystem.CurrentIP.ToString(),rpcParams.Receive.SenderClientId, msgData);
            }
            //RpcTarget.Single(rpcParams.Receive.SenderClientId, RpcTargetUse.Temp));
            //ServerInteractionReceiveRpc(pingCount, msgData, RpcTarget.Single(rpcParams.Receive.SenderClientId, RpcTargetUse.Temp));
            ReceiveInteractionEventRpc(pingCount, msgData,RpcTarget.Single(rpcParams.Receive.SenderClientId, RpcTargetUse.Temp));
        }
        [Rpc(SendTo.Server)]
        public void SendServerConfirmationRpc(FPNetworkDataStruct msgData, RpcParams rpcParams = default)
        {
            Debug.Log($"Server Confirmation Rpc: {msgData.TheNetworkMessage}");
            //ServerMessageConfirmReadyState(msgData);
        }
        #endregion
        [ServerRpc]
        public void ClientReadyServerRpc(FPNetworkDataStruct msgData)
        {
            Debug.Log($"Server, the client at {msgData.ClientIPAddress} {msgData.TheNetworkMessageType}| Details: {msgData.TheNetworkMessage}");
            if(msgData.TheNetworkMessageType == NetworkMessageType.ClientConfirmed)
            {
                networkSystem.OnClientConfirmed(msgData.TheClientID);
            }
        }
        #region Rpcs Running on Client at Server Request
        [Rpc(SendTo.SpecifiedInParams)]
        void ReceiveInteractionEventRpc(int pingCount, FPNetworkDataStruct dataReceived, RpcParams rpcParams)
        {
            // Debug Client at Server Request only on the individual client because of the rpcParams.Receive.SenderClientId
            Debug.Log($"Client Operation Run via Server Request: {pingCount}");
            Debug.Log($"Client Interaction Event: {dataReceived.TheNetworkMessage}");
            DebugText.text = $"Client Interaction Event: {pingCount}\nMessage: '{dataReceived.TheNetworkMessage}'\nDevice Type {dataReceived.TheDevicePlayerType}";
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
