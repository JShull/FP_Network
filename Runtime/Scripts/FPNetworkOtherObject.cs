namespace FuzzPhyte.Network
{
    using UnityEngine;
    using Unity.Netcode;
    using TMPro;
    using FuzzPhyte.Utility.FPSystem;
    using UnityEngine.SceneManagement;
    using System.Collections.Generic;
    using Unity.Netcode.Components;
    public class FPNetworkOtherObject : NetworkBehaviour,IFPNetworkProxySetup
    {
        protected FPNetworkSystem networkSystem;
        protected NetworkTransform networkTransform;
        [Tooltip("The prefab to spawn on the client when this object is spawned")]
        public GameObject LocalPrefabSpawn;
        protected GameObject proxyClient;
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

            if (IsServer)
            {
                OnServerSpawned();
            }
            else
            {
                OnClientSpawned(); 
            }
        }
        public virtual void OnClientSpawned()
        {
            // only run the RPC if this object has ownership (i.e., on the networked client)
            if (IsOwner)
            {
                proxyClient = GameObject.Instantiate(LocalPrefabSpawn, this.transform.position, this.transform.rotation);
                // Try to get the component that implements the interface
                IFPNetworkOtherObjectSetup otherInterface = proxyClient.GetComponent<IFPNetworkOtherObjectSetup>();
                if (otherInterface != null)
                {
                    otherInterface.SetupSystem(this);
                }
            }
        }
        public virtual void OnServerSpawned()
        {

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
                networkTransform.SetState(position, rotation,networkTransform.transform.localScale, true);
            }
        }
    }
}
