namespace FuzzPhyte.Network
{
    using UnityEngine;
    using Unity.Netcode;
    using FuzzPhyte.Utility.FPSystem;
    using Unity.Netcode.Components;

    public class FPNetworkObject : NetworkBehaviour
    {
        protected FPNetworkRpc serverRpcSystem;
        protected FPNetworkSystem networkSystem;
        protected NetworkTransform networkTransform;
        protected bool _running;
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
            if (serverRpcSystem == null)
            {
                Debug.LogError("No Server Rpc System Found");
                serverRpcSystem = networkSystem.gameObject.AddComponent<FPNetworkRpc>();
            }
            if (IsServer)
            {
                //OnServerSpawned();
            }
            else
            {
                //OnClientSpawned();
            }
            _running = true;
        }
        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
        }
       
        public virtual void UpdatePositionAndRotation(Vector3 position, Quaternion rotation)
        {
            // Only run the RPC if this object has ownership (i.e., on the networked client)
            if (IsOwner)
            {
                // Call the server RPC to update position and rotation on the server
                SendPositionAndRotationToServerRpc(position, rotation);
            }
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
                networkTransform.SetState(position, rotation, networkTransform.transform.localScale, true);
            }
        }
    }
}
