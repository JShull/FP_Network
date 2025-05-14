namespace FuzzPhyte.Network.Samples
{
    using Unity.Netcode;
    using UnityEngine;
    public class TellVRSyncLocalVRPlayer : MonoBehaviour, IFPNetworkPlayerSetup
    {
        public FPNetworkPlayer FPNetworkPlayer;
        private bool _running;
        [SerializeField] protected Transform VRHead;
        public void RegisterOtherObjects(NetworkObject networkObject, FPNetworkPlayer player)
        {
            //throw new System.NotImplementedException();
            Debug.LogWarning($"Not fully Setup for VR just yet");
        }

        public void SetupSystem(FPNetworkPlayer player)
        {
            FPNetworkPlayer = player;
            //find our existing VR player head
            VRHead = GameObject.Find("CenterEyeAnchor").transform;
            if(VRHead !=null)
            {
                Debug.Log($"Found VR Head: {VRHead.name}");
                _running = true;
            }else
            {
                Debug.LogError($"VR Head not found");
            }
        }
        public void LateUpdate()
        {
            if(!_running)
            {
                return;
            }
            if (FPNetworkPlayer != null)
            {
                FPNetworkPlayer.UpdatePositionAndRotation(VRHead.position, VRHead.rotation);
            }
        }
    }
}
