namespace FuzzPhyte.Network.Samples
{
    using System.Collections.Generic;
    using Unity.Netcode;
    using UnityEngine;

    /// <summary>
    /// This is on the local spawned Proxy Right/Left Hand
    /// We end up spawning the network objects for our hands as network prefabs
    /// once they are spawned we pass them back and connect them via the proxy hands
    /// </summary>
    public class TellVRSyncControllerVR : MonoBehaviour, IFPNetworkPlayerSetup
    {
        FPNetworkPlayer myNetworkPlayer;
        public string ControllerName;
        public Transform LocalHandVisual;
        [Tooltip("Real Hand Controller")]
        public Transform VRHandProxy;
        public FPNetworkObject ControllerNetworkObject;
        protected bool _running = false;
        public List<GameObject> OtherIFPNetworkItems = new List<GameObject>();
        public List<IFPNetworkPlayerSetup> IFPNetworkItems = new List<IFPNetworkPlayerSetup>();

        public void Awake()
        {
            for(int i = 0; i < OtherIFPNetworkItems.Count; i++)
            {
                var anItem = OtherIFPNetworkItems[i].GetComponent<IFPNetworkPlayerSetup>();
                if (anItem != null)
                {
                    IFPNetworkItems.Add(anItem);
                }
            }
        }
        public void RegisterOtherObjects(NetworkObject networkObject, FPNetworkPlayer player)
        {
            //throw new System.NotImplementedException();
            if(myNetworkPlayer == null)
            {
                myNetworkPlayer = player;
            }
            if (ControllerNetworkObject == null)
            {
                if (networkObject.GetComponent<FPNetworkObject>() != null)
                {
                    ControllerNetworkObject = networkObject.GetComponent<FPNetworkObject>();
                    Debug.LogError($"Registered Controller Network Object: {ControllerNetworkObject.name}");
                    Debug.LogError($"[Client]: Requesting ownership of this controller!");
                    myNetworkPlayer.RequestOwnershipServerRpc(networkObject.NetworkObjectId);
                    Debug.LogError($"Set Running to true you mfers!");
                    _running = true;
                }
            }
        }
        public List<IFPNetworkPlayerSetup> ReturnOtherIFPNetworkObjects()
        {
            return IFPNetworkItems;
        }

        public void SetupSystem(FPNetworkPlayer player)
        {
            myNetworkPlayer = player;
            //FPNetworkPlayer = player;
            //find our existing VR player head
            VRHandProxy = GameObject.Find(ControllerName).transform;

            //turn off any renderers? camera cull?
            if (VRHandProxy != null)
            {
                Debug.LogWarning($"Found a VR Controller, {VRHandProxy.name}");
                this.LocalHandVisual.SetParent(VRHandProxy);
                this.LocalHandVisual.localPosition = Vector3.zero;
                this.LocalHandVisual.localRotation = Quaternion.identity;
            }
            else
            {
                Debug.LogError($"VR Controller not found");
            }
        }
        public void LateUpdate()
        {
            if (!_running)
            {
                return;
            }
            if (VRHandProxy!=null && ControllerNetworkObject!=null)
            {
                ControllerNetworkObject.UpdatePositionAndRotation(LocalHandVisual.position, LocalHandVisual.rotation);
            }

        }
    }
}
