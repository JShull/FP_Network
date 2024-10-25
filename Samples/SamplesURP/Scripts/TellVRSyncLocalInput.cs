namespace FuzzPhyte.Network.Samples
{
    using FuzzPhyte.Utility.TestingDebug;
    using UnityEngine;
    
    public class TellVRSyncLocalInput : MonoBehaviour, IFPNetworkPlayerSetup
    {
        public FPNetworkPlayer FPNetworkPlayer;
        public FPUtilCameraControl FPUtilCameraControl;
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

        
    }
}

