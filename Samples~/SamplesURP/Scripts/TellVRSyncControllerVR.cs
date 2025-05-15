namespace FuzzPhyte.Network.Samples
{
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
        public void RegisterOtherObjects(NetworkObject networkObject, FPNetworkPlayer player)
        {
            //throw new System.NotImplementedException();
            if (ControllerNetworkObject == null)
            {
                if (networkObject.GetComponent<FPNetworkObject>() != null)
                {
                    ControllerNetworkObject = networkObject.GetComponent<FPNetworkObject>();
                    _running = true;
                }
            }
        }

        public void SetupSystem(FPNetworkPlayer player)
        {
            myNetworkPlayer = player;
            //FPNetworkPlayer = player;
            //find our existing VR player head
            VRHandProxy = GameObject.Find(ControllerName).transform;
            LocalHandVisual.SetParent(VRHandProxy);
            LocalHandVisual.localPosition = Vector3.zero;
            LocalHandVisual.localRotation = Quaternion.identity;
            //turn off any renderers? camera cull?
            if (VRHandProxy != null )
            {
                Debug.Log($"Found VR Controller");
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
