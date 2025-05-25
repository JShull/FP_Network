namespace FuzzPhyte.Network.Samples
{
    using FuzzPhyte.UI.Camera;
    using Unity.Netcode;
    using UnityEngine;
    using System.Collections.Generic;
    public class TellVRSyncLocalInput : MonoBehaviour, IFPNetworkPlayerSetup
    {
        public FPNetworkPlayer FPNetworkPlayer;
        public FPUI_CameraControl FPUtilCameraControl;
        private bool _running;
        public Camera ClientCam;
        public List<IFPNetworkPlayerSetup> OtherItems = new List<IFPNetworkPlayerSetup>();
        public List<GameObject> OtherIFPNetworkItems = new List<GameObject>();
        public void Awake()
        {
            for (int i = 0; i < OtherItems.Count; i++)
            {
                var item = OtherIFPNetworkItems[i].GetComponent<IFPNetworkPlayerSetup>();
                if (item!=null)
                {
                    OtherItems.Add(item);
                }
            }
        }
        public void SetupSystem(FPNetworkPlayer player)
        {
            FPNetworkPlayer = player;
            if (FPUtilCameraControl != null)
            {
                FPUtilCameraControl.Setup(ClientCam, true);
                _running = true;
            }
        }
        public List<IFPNetworkPlayerSetup> ReturnOtherIFPNetworkObjects()
        {
            return OtherItems;
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

