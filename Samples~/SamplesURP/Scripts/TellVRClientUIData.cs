namespace  FuzzPhyte.Network.Samples{
    using System;
    using TMPro;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// Quick client UI references for the Editor to be more organized associated with the TellVRServerIPName example
    /// </summary>
    [Serializable]
    public struct TellVRClientUIData
    {
        [TextArea(2,3)]
        public string ClientDetails;
        public bool IsClientType;
        [Tooltip("Where the related client UI will be parented too")]
        public Transform ClientUIParent;
        public TMP_InputField InputFieldClientServerName;
        public TMP_InputField InputFieldClientServerIPOverride;
        public Button ButtonClientConfirmServer;
        public Button ButtonClientStart;
        public Button ButtonClientStop;
    }
}