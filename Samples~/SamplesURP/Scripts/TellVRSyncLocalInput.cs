namespace FuzzPhyte.Network.Samples
{
    using FuzzPhyte.UI.Camera;
    using Unity.Netcode;
    using UnityEngine;
    
    public class TellVRSyncLocalInput : MonoBehaviour, IFPNetworkPlayerSetup
    {
        public FPNetworkPlayer FPNetworkPlayer;
        public FPUI_CameraControl FPUtilCameraControl;
        private bool _running;
        public Camera ClientCam;

        public void SetupSystem(FPNetworkPlayer player)
        {
            FPNetworkPlayer = player;
            if (FPUtilCameraControl != null)
            {
                FPUtilCameraControl.Setup(ClientCam, true);
                _running = true;
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
                FPNetworkPlayer.UpdatePositionAndRotation(FPUtilCameraControl.LocalTransform.position, FPUtilCameraControl.LocalTransform.rotation);
            }
        }

        public virtual void RegisterOtherObjects(NetworkObject netObject,FPNetworkPlayer player)
        {
            Debug.LogError($"Not fully setup!");
        }

        
    }
}

