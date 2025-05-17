namespace FuzzPhyte.Network.Samples
{
    using Unity.Netcode;
    using UnityEngine;
    using UnityEngine.UI;

    public class TellVRSyncLocalVRPlayer : MonoBehaviour, IFPNetworkPlayerSetup,IFPNetworkUISetup
    {
        public string PlayerRealName = "CenterEyeAnchor";
        public FPNetworkPlayer FPNetworkPlayer;
        public GameObject LocalVRWorldCanvasPrefab;
        [Space]
        public Button ButtonConfirmReadyNetworkSession;
        public string DetailsConfirmReady = "Ready to start, button was pushed!";
        [Space]
        private bool _running;
        [SerializeField] protected Transform VRHead;
        //[SerializeField] protected Transform VRRightController;
        //[SerializeField] protected Transform VRLeftController;
        public void RegisterOtherObjects(NetworkObject networkObject, FPNetworkPlayer player)
        {
            //throw new System.NotImplementedException();
            Debug.LogWarning($"Not fully Setup for VR just yet");
        }

        public void SetupSystem(FPNetworkPlayer player)
        {
            if (FPNetworkPlayer == null)
            {
                //assign the player
                FPNetworkPlayer = player;
            }
            //find our existing VR player head
            VRHead = GameObject.Find(PlayerRealName).transform;
            if(VRHead !=null)
            {
                Debug.Log($"Found VR Head: {VRHead.name}");
                _running = true;
            }else
            {
                Debug.LogError($"VR Head not found");
            }
        }
        public void OnUISetup(FPNetworkPlayer player)
        {
            if (FPNetworkPlayer == null)
            {
                //assign the player
                FPNetworkPlayer = player;
            }
            if(ButtonConfirmReadyNetworkSession == null)
            {
                Debug.LogError($"ButtonConfirmReadyNetworkSession not assigned");
                return;
            }
            ButtonConfirmReadyNetworkSession.onClick.AddListener(() =>
            {
                FPNetworkPlayer.UISendServerConfirmationDetails(DetailsConfirmReady);
                ButtonConfirmReadyNetworkSession.interactable = false;
            });
        }
        public void LateUpdate()
        {
            if (!_running)
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
