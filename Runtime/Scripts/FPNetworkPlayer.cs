
namespace FuzzPhyte.Network
{
    using UnityEngine;
    using Unity.Netcode;
    using TMPro;
    public class FPNetworkPlayer : NetworkBehaviour
    {
        public DevicePlayerType ThePlayerType;
        public TextMeshProUGUI DebugText;
        public Canvas TheUIClientCanvas;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if(IsServer)
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
        public void UISendMessageServer(string details)
        {
            FPNetworkDataStruct data = new FPNetworkDataStruct();
            data.TheDevicePlayerType = ThePlayerType;
            data.TheNetworkPlayerType = NetworkPlayerType.Client;
            data.TheNetworkMessageType = NetworkMessageType.ClientAction;
            data.TheNetworkMessage = details;
            // Send to Server
            ClientInteractionEventRpc(1, data);
            //return data;
        }

        #region RPC Methods
        #region Rpcs Client
        [Rpc(SendTo.Server)]
        public void ClientInteractionEventRpc(int pingCount,FPNetworkDataStruct msgData,RpcParams rpcParams=default)
        {
            // Debug Client
            //Debug.Log($"Interaction Event: {pingCount}");
            // Send to Server
            // That sender ID can be passed in to the PongRpc to send this back to that client and ONLY that client
            DebugText.text = $"Interaction Event: {pingCount}\nMessage: '{msgData.TheNetworkMessage}'\nDevice Type {msgData.TheDevicePlayerType}";
            //RpcTarget.Single(rpcParams.Receive.SenderClientId, RpcTargetUse.Temp));
            //ServerInteractionReceiveRpc(pingCount, msgData, RpcTarget.Single(rpcParams.Receive.SenderClientId, RpcTargetUse.Temp));
            ServerInteractionReceiveRpc(pingCount, msgData,RpcTarget.Single(rpcParams.Receive.SenderClientId, RpcTargetUse.Temp) );
        }
        #endregion
        #region Rpcs Server
        [Rpc(SendTo.SpecifiedInParams)]
        void ServerInteractionReceiveRpc(int pingCount, FPNetworkDataStruct dataReceived, RpcParams rpcParams)
        {
            // Debug Server
            Debug.Log($"Server Interaction Event: {pingCount}");
            Debug.Log($"Server Interaction Event: {dataReceived.TheNetworkMessage}");
            DebugText.text = $"Server Interaction Event: {pingCount}\nMessage: '{dataReceived.TheNetworkMessage}'\nDevice Type {dataReceived.TheDevicePlayerType}";
        }
        #endregion
        #endregion
    }
}
