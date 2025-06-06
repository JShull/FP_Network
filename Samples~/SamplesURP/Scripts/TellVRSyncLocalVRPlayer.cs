namespace FuzzPhyte.Network.Samples
{
    using Unity.Netcode;
    using UnityEngine;
    using UnityEngine.UI;
    using System.Collections.Generic;
    public class TellVRSyncLocalVRPlayer : MonoBehaviour, IFPNetworkPlayerSetup,IFPNetworkUISetup
    {
        public string PlayerRealName = "CenterEyeAnchor";
        public FPNetworkPlayer FPNetworkPlayer;
        public string MenuLocationRefName = "ConfirmMenuLocation";
        public GameObject LocalVRWorldConfirmCanvas;
        [Space]
        public Button ButtonConfirmReadyNetworkSession;
        [SerializeField] protected bool buttonListenerActivated;
        public string DetailsConfirmReady = "Ready to start, button was pushed!";
        [Space]
        private bool _running;
        [SerializeField] protected Transform VRHead;
        public List<GameObject> VRItems = new List<GameObject>();
        public List<IFPNetworkPlayerSetup> OtherNetworkLocalObjects = new List<IFPNetworkPlayerSetup>();
        public void Awake()
        {
            for (int i = 0; i < VRItems.Count; i++) 
            {
                var item = VRItems[i].GetComponent<IFPNetworkPlayerSetup>();
                if (item != null)
                {
                    OtherNetworkLocalObjects.Add(item);
                }
            }
        }
        //[SerializeField] protected Transform VRRightController;
        //[SerializeField] protected Transform VRLeftController;
        public void RegisterOtherObjects(NetworkObject networkObject, FPNetworkPlayer player)
        {
            //throw new System.NotImplementedException();
            Debug.LogWarning($"Not fully Setup for VR just yet");
        }
        public List<IFPNetworkPlayerSetup> ReturnOtherIFPNetworkObjects()
        {
            return OtherNetworkLocalObjects;
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
            if (VRHead != null)
            {
                Debug.Log($"Found VR Head: {VRHead.name}");
                _running = true;
            }
            else
            {
                Debug.LogError($"VR Head not found");
            }
            if (LocalVRWorldConfirmCanvas != null)
            {
                var confirmMenuLocation = GameObject.Find(MenuLocationRefName);
                if (confirmMenuLocation != null)
                {
                    LocalVRWorldConfirmCanvas.transform.SetParent(confirmMenuLocation.transform);
                    LocalVRWorldConfirmCanvas.transform.localPosition = Vector3.zero;
                    LocalVRWorldConfirmCanvas.transform.localRotation = Quaternion.identity;
                }
                else
                {
                    LocalVRWorldConfirmCanvas.transform.SetParent(null);
                }
            }
            //setup other possible interface items
            Debug.LogWarning($"Count of other interface items: {OtherNetworkLocalObjects.Count}");
            for(int i=0;i< OtherNetworkLocalObjects.Count; i++)
            {
                var anItem = OtherNetworkLocalObjects[i];
                anItem.SetupSystem(player);
            }
            if (ButtonConfirmReadyNetworkSession == null)
            {
                Debug.LogError($"ButtonConfirmReadyNetworkSession not assigned");
                return;
            }
            if (buttonListenerActivated)
            {
                return;
            }
            ButtonConfirmReadyNetworkSession.onClick.AddListener(() =>
            {
                FPNetworkPlayer.UISendServerConfirmationDetails(DetailsConfirmReady);
                ButtonConfirmReadyNetworkSession.interactable = false;
            });
            buttonListenerActivated = true;
        }
        public void OnUISetup(FPNetworkPlayer player)
        {
            if (FPNetworkPlayer == null)
            {
                //assign the player
                FPNetworkPlayer = player;
            }
            
            if (ButtonConfirmReadyNetworkSession == null)
            {
                Debug.LogError($"ButtonConfirmReadyNetworkSession not assigned");
                return;
            }
            if (buttonListenerActivated)
            {
                return;
            }
            ButtonConfirmReadyNetworkSession.onClick.AddListener(() =>
            {
                FPNetworkPlayer.UISendServerConfirmationDetails(DetailsConfirmReady);
                ButtonConfirmReadyNetworkSession.interactable = false;
            });
            buttonListenerActivated = true;
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
