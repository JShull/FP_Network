namespace FuzzPhyte.Network
{
    using UnityEngine;
    //using FuzzPhyte.Utility.FPSystem;
    using System.Collections;

    public class FPNetworkInteraction : MonoBehaviour
    {
        public NetworkMessageType NetworkMSGType;
        protected string messageIDS;//use some sort of CSV
        public bool SetupOnStart = true;
        public bool DelayOnStart = true;
        public bool Spawner;
        [Header("Interactions by The Items themselves")]
        [Space]
        public FPNetworkPlayer TheNetworkPlayer;
        public FPNetworkSystem FPNetworkSystem;
        public bool NetworkInteractionSetup;
        protected WaitForEndOfFrame waitForEndOfFrame = new WaitForEndOfFrame();
        public void Start()
        {
            if (SetupOnStart)
            {
                if (DelayOnStart)
                {
                    StartCoroutine(DelayOnStartSetup());
                }
                else
                {
                    SetupNetworkReferences();
                }
            }
        }
        IEnumerator DelayOnStartSetup()
        {
            yield return waitForEndOfFrame;
            SetupNetworkReferences();
        }
        
        public void UpdateMessageIDS(string csvValues)
        {
            messageIDS = csvValues;
        }
        #region Items are doing the work vs us listening to them
        /// <summary>
        /// Called from a Unity event or something else that updates the messageIDS
        /// </summary>
        /// <param name="values"></param>
        public void UnityInteractionEventUpdatedDetails(string values)
        {
            messageIDS = values;
            UnityInteractionEvent();
        }/// <summary>
         /// Probably called from an editor UnityEvent event
         /// </summary>
        public void UnityInteractionEvent()
        {
            //we need to find our network system
            if (!NetworkInteractionSetup)
            {
                return;
            }

            TheNetworkPlayer.InteractionTestEventInvoked(NetworkMSGType, messageIDS);
        }
       
        public void SetupNetworkReferences()
        {
            if (FPNetworkSystem == null)
            {
                if (FPNetworkSystem.Instance != null)
                {
                    FPNetworkSystem = (FPNetworkSystem)FPNetworkSystem.Instance;
                }
            }
            if (TheNetworkPlayer == null)
            {
                TheNetworkPlayer = FPNetworkSystem.NetworkManager.SpawnManager.GetLocalPlayerObject().GetComponent<FPNetworkPlayer>();
            }
            NetworkInteractionSetup = true;
        }
        #endregion
    }
}
