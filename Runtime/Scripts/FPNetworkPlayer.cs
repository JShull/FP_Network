
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
        private ulong myClientID;

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
        public void UISendMessageServer(string details)
        {
            FPNetworkDataStruct data = new FPNetworkDataStruct();
            data.TheDevicePlayerType = ThePlayerType;
            data.TheNetworkPlayerType = NetworkPlayerType.Client;
            data.TheNetworkMessageType = NetworkMessageType.ClientAction;
            data.TheNetworkMessage = details;
            data.TheClientID = myClientID;
            // Send to Server
            SendServerInteractionEventRpc(1, data);
            //return data;
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
                FPNetworkCache.Instance.AddData(rpcParams.Receive.SenderClientId, msgData);
            }
            //RpcTarget.Single(rpcParams.Receive.SenderClientId, RpcTargetUse.Temp));
            //ServerInteractionReceiveRpc(pingCount, msgData, RpcTarget.Single(rpcParams.Receive.SenderClientId, RpcTargetUse.Temp));
            ReceiveInteractionEventRpc(pingCount, msgData,RpcTarget.Single(rpcParams.Receive.SenderClientId, RpcTargetUse.Temp));
        }
        #endregion
        #region Rpcs Running on Client at Server Request
        [Rpc(SendTo.SpecifiedInParams)]
        void ReceiveInteractionEventRpc(int pingCount, FPNetworkDataStruct dataReceived, RpcParams rpcParams)
        {
            // Debug Client at Server Request only on the individual client because of the rpcParams.Receive.SenderClientId
            Debug.Log($"Client Operation Run via Server Request: {pingCount}");
            Debug.Log($"Client Interaction Event: {dataReceived.TheNetworkMessage}");
            DebugText.text = $"Client Interaction Event: {pingCount}\nMessage: '{dataReceived.TheNetworkMessage}'\nDevice Type {dataReceived.TheDevicePlayerType}";
        }
        #endregion
        #endregion
    }
}
