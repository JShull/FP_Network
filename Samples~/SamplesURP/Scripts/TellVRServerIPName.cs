namespace  FuzzPhyte.Network.Samples{

    using UnityEngine;
    using FuzzPhyte.Network;
    using System.Collections.Generic;
    using FuzzPhyte.Utility;
    using System;
    using UnityEngine.UI;
    using System.Linq;
    using TMPro;
    using System.Collections;
    using System.Net;
    using Unity.Netcode;

    public class TellVRServerIPName : MonoBehaviour,IFPDontDestroy
    {
        public static TellVRServerIPName Instance { get; protected set; }
        public bool DontDestroy { get => dontDestroy; set => dontDestroy = value; }
        [SerializeField] protected bool dontDestroy = false;
        public FPNetworkSystem NetworkSystem;
        #region Related to Tell VR Module Setup
        public string WordCheck = "azul";
        public string PlayerName = "Player";
        public string jsonFileNameNoExtension = "IPWordMappings";
        [Tooltip("One of the module data structures to use")]
        public List<TellVRModule> AllModules = new List<TellVRModule>();
        [Tooltip("Possible Data Selectable Objects to Use")]
        public List<FPNetworkData> AllDataConnectionProfiles = new List<FPNetworkData>();
        public FP_Language SelectedLanguage;
        public FP_LanguageLevel SelectedLanguageLevel;
        public DevicePlayerType SelectedDeviceType;
        public NetworkPlayerType SelectedNetworkType;
        [Header("Local Parameters for Confirmation")]
        [SerializeField] private int clientConnectionsUntilStart = 2;
        [SerializeField] private int clientConfirmedConnections = 0;
        [SerializeField] private float delayUntilStart = 5f;
        [SerializeField]
        private TellVRModule moduleData;
        [SerializeField]
        private string serverName;
        [SerializeField]
        private string serverIPToConnect;
        [Tooltip("we found an ip address in our lookup table")]
        private bool serverIPFound;
        private bool languageSelected;
        private bool languageLevelSelected;
        private bool deviceSelected;
        public bool DeviceSelected {get=>deviceSelected;set=>deviceSelected=value;}
        private bool networkTypeSelected;
        public bool NetworkTypeSelect { get=>networkTypeSelected; set=>networkTypeSelected = value; }
        #region UI Input Components
        public TMPro.TMP_Dropdown LanguageDropdown;
        public TMPro.TMP_Dropdown LanguageLevelDropdown;
        public TMPro.TMP_Dropdown DeviceTypeDropdown;
        public TMPro.TMP_Dropdown NetworkTypeDropdown;
        public TMPro.TMP_InputField ServerNameInputField;
        public TMPro.TMP_Dropdown NumberStudentsField;
        public Button ConfirmLanguageButton;
        public Button StartServerButton;
        public Button DisconnectServerButton;
        [Space]
        [Header("Client UI Related Items")]
        public TellVRClientUIData IPadClientUIData;
        public TellVRClientUIData VRClientUIData;
        [Obsolete]public Button ConfirmServerNameButton;
        [Obsolete]public Button StartClientButton;
        [Obsolete]public Button StopClientButton;
        public TMPro.TextMeshProUGUI ServerNameDisplay;
        public TMPro.TextMeshProUGUI DebugText;

        [Tooltip("The UI Panel for the Server-based on options selected")]
        public GameObject UIServerPanel;
        [Tooltip("The UI Panel for the Client-based on options selected")]
        public GameObject UIClientiPadPanel;
        [Tooltip("The UI Panel for the Client-based on options selected for VR - different project")]
        public GameObject UIClientVRPanel;
        
        [Space]
        [Header("Test RPC events")]
        public GameObject UIClientTestPanel;
        public TMP_InputField ClientMessageInputField;
        public Button ClientMessageButton;
        [Space]
        [Header("Client Input Fields for language ip and ip override")]
        [Obsolete]public TMP_InputField LanguageIPField;
        [Obsolete]public TMP_InputField ClientOverrideIPField;
        
        
        #endregion
        #endregion
        #region Unity Functions
        public void Awake()
        {
            if(Instance == null)
            {
                Instance = this;
                if (DontDestroy)
                {
                    DontDestroyOnLoad(this.gameObject);
                }
            }
            else
            {
                Destroy(this.gameObject);
                Debug.LogWarning($"Destroying {this.gameObject.name} as there is already an instance of {this.GetType().Name} in the scene.");
            }
            // testing actions/events
            NetworkSystem.OnLocalIPAddressTriggered+=OnLocalIPAddressData;
        }
        public void Start()
        {
            StartCoroutine(DelayActionRegistration());
        }
        IEnumerator DelayActionRegistration(){
            yield return new WaitForEndOfFrame();
            //scene loaded via network call registration for all clients
            //NetworkSystem.NetworkSceneManager.OnLoadEventCompleted += OnLoadedEventCompleted;
            //other events/actions
            NetworkSystem.OnServerEventTriggered+=OnServerEventTriggered;
            NetworkSystem.OnClientEventTriggered+=OnClientEventTriggered;
            NetworkSystem.OnServerConfirmationReady += OnServerConfirmationCheck;
            NetworkSystem.OnClientConfirmedReturn += OnClientReturnConfirmationCheck;
            NetworkSystem.OnSceneLoadedCallBack+= LocalSceneLoadDebug;
            NetworkSystem.OnServerDisconnectTriggered+=ServerDisconnectCallback;
            NetworkSystem.NetworkManager.OnServerStopped+=OnNetworkManagerServerStopped;
            //network OnLocalIPAddressTriggered is called in awake function
        }
        public void OnDisable()
        {
            if(NetworkSystem!=null)
            {
                NetworkSystem.OnServerEventTriggered-=OnServerEventTriggered;
                NetworkSystem.OnClientEventTriggered-=OnClientEventTriggered;
                NetworkSystem.OnServerConfirmationReady -= OnServerConfirmationCheck;
                NetworkSystem.OnClientConfirmedReturn -= OnClientReturnConfirmationCheck;
                NetworkSystem.OnSceneLoadedCallBack-= LocalSceneLoadDebug;
                NetworkSystem.OnServerDisconnectTriggered-=ServerDisconnectCallback;
                NetworkSystem.NetworkManager.OnServerStopped-=OnNetworkManagerServerStopped;
                NetworkSystem.OnLocalIPAddressTriggered-=OnLocalIPAddressData;
            }
        }
        #endregion
        public FPIPWord LoadIPWordMappings()
        {
            string jsonText = string.Empty;

            #if UNITY_EDITOR
            // Load JSON from Resources folder in the Editor
            jsonText = LoadFromResources(jsonFileNameNoExtension);
            #else
            // Load JSON at runtime from Resources folder (for iOS, Android, etc.)
            jsonText = LoadFromResources(jsonFileNameNoExtension);
            #endif

            if (!string.IsNullOrEmpty(jsonText))
            {
                var returnValue = JsonUtility.FromJson<FPIPWord>(jsonText);
                Debug.Log($"Return Value: {jsonText}");
                returnValue.InitializeDictionary();
                Debug.LogWarning($"Return Value length: {returnValue.ipToWords.Count}");
                return returnValue;
            }
            else
            {
                Debug.LogError("Could not find or load the IPWordMappings JSON.");
                DebugText.text += $"Could not find or load the IPWordMappings JSON.\n";
                return null;
            }
        }
        private static string LoadFromResources(string fileNameWithoutExtension)
        {
            TextAsset jsonFile = Resources.Load<TextAsset>(fileNameWithoutExtension);
            if (jsonFile != null)
            {
                return jsonFile.text;
            }
            else
            {
                Debug.LogError($"JSON file {fileNameWithoutExtension} not found in Resources.");
                //DebugText.text += $"JSON file {fileNameWithoutExtension} not found in Resources.\n";
                return string.Empty;
            }
        }

        #region UI Related Functions
        public void UINumberStudentDropDownChange()
        {
            clientConnectionsUntilStart = NumberStudentsField.value;
        }
        /// <summary>
        /// Dropdown UI Event is calling this from TMP_DropDowns in the scene(s)
        /// </summary>
        /// <param name="index"></param>
        public void UILanguageListDropDownChange(int index)
        {
            var selectedItem = LanguageDropdown.options[index];
            //convert the string to an enum
            if (System.Enum.TryParse(selectedItem.text, out FP_Language selectedLanguage))
            {
                SelectedLanguage = selectedLanguage;
                languageSelected = true;
                if (!languageLevelSelected)
                {
                    Array enumValues = Enum.GetValues(typeof(FP_LanguageLevel));
                    SelectedLanguageLevel = (FP_LanguageLevel)enumValues.GetValue(0);
                    languageLevelSelected = true;
                }
                UnlockButtonConfirm();
            }
        }
        /// <summary>
        /// Dropdown UI Event is calling this from TMP_DropDowns in the scene(s)
        /// </summary>
        /// <param name="index"></param>
        public void UILanguageLevelDropDownChange(int index)
        {
            Array enumValues = Enum.GetValues(typeof(FP_LanguageLevel));
            SelectedLanguageLevel = (FP_LanguageLevel)enumValues.GetValue(index);
            //SelectedLanguageLevel = index + 1;
            languageLevelSelected = true;
            UnlockButtonConfirm();
        }
        /// <summary>
        /// Called via Device Drop Down Change from the Drop Down List
        /// </summary>
        /// <param name="index"></param>
        public void UIDeviceTypeDropDownChange(int index)
        {
            Array enumValues = Enum.GetValues(typeof(DevicePlayerType));
            SelectedDeviceType = (DevicePlayerType)enumValues.GetValue(index);
            deviceSelected = true;
            switch(SelectedDeviceType)
            {
                case DevicePlayerType.None:
                case DevicePlayerType.iPad:
                    //Debug.Log($"Device Selected: {SelectedDeviceType}");
                    //DebugText.text += $"Device Selected: {SelectedDeviceType}\n";
                    IPadClientUIData.IsClientType = true;
                    break;
                case DevicePlayerType.MetaQuest:
                    VRClientUIData.IsClientType = true;
                    break;
            }
            ChangeUIElements();
        }
        /// <summary>
        /// Called via Network Drop Down Change from the Drop Down List
        /// </summary>
        /// <param name="index"></param>
        public void UINetworkTypeDropDownChange(int index)
        {
            Array enumValues = Enum.GetValues(typeof(NetworkPlayerType));
            SelectedNetworkType = (NetworkPlayerType)enumValues.GetValue(index);
            networkTypeSelected = true;
            ChangeUIElements();
        }
        
        ///Quick external method to do some things
        public void ExternalChangeUIElements()
        {
            ChangeUIElements();
        }
        private void ChangeUIElements()
        {
            if(deviceSelected && networkTypeSelected)
            {
                //update our data block to one from our list
                if(AllDataConnectionProfiles.Count>0)
                {
                    var someData = AllDataConnectionProfiles.Find(x => x.TheNetworkPlayerType == SelectedNetworkType && x.TheDevicePlayerType == SelectedDeviceType);
                    if(someData!=null)
                    {
                        NetworkSystem.UpdateNetworkData(someData);
                        Debug.Log($"Network Data Found: {NetworkSystem.TheSystemData.TheNetworkPlayerType}");
                        DebugText.text += $"Network Data Found: {NetworkSystem.TheSystemData.TheNetworkPlayerType}\n";
                        if(NetworkSystem.TheSystemData.TheNetworkPlayerType == NetworkPlayerType.Server)
                        {
                            UIServerPanel.SetActive(true);
                            if(UIClientVRPanel!=null)
                            {
                                UIClientVRPanel.SetActive(false);
                            }
                            UIClientiPadPanel.SetActive(false);
                        }else if(NetworkSystem.TheSystemData.TheNetworkPlayerType == NetworkPlayerType.Client)
                        {
                            UIServerPanel.SetActive(false);
                            if(NetworkSystem.TheSystemData.TheDevicePlayerType == DevicePlayerType.MetaQuest)
                            {
                                UIClientVRPanel.SetActive(true);
                                UIClientiPadPanel.SetActive(false);
                            }else
                            {
                                UIClientiPadPanel.SetActive(true);
                                UIClientVRPanel.SetActive(false);
                            }
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Called via Confirm Language Button
        /// </summary>
        public void MatchModuleDataToUserInput()
        {
            if (languageSelected && languageLevelSelected)
            {
                moduleData = AllModules.Find(x => x.ModuleLanguage == SelectedLanguage && x.LanguageLevel == SelectedLanguageLevel);
                if (moduleData != null)
                {
                    Debug.Log($"Module Found: {moduleData.ModuleLabel}");
                    DebugText.text += $"Module Found: {moduleData.ModuleLabel}\n";
                    ServerNameDisplay.enabled = true;
                    DisplayServerName();
                    if (NetworkSystem != null)
                    {
                        NetworkSystem.FirstSceneToLoad = moduleData.ModuleSceneName;
                    }
                }
            }
        }
        /// <summary>
        /// Called from the "Start Server" button in the UI
        /// </summary>
        public void StartServerUIAction()
        {
            if(NetworkSystem!=null)
            {
                if(NetworkSystem.TheSystemData.TheNetworkPlayerType == NetworkPlayerType.Server)
                {
                    NetworkSystem.StartServer();
                    Debug.Log($"Server Started");
                    DebugText.text += $"Server Started...\n";
                    StartServerButton.interactable = false;
                    DisconnectServerButton.interactable = true;
                }        
            }
        }
        public void StopServerUIAction()
        {
            if(NetworkSystem!=null)
            {
                if(NetworkSystem.TheSystemData.TheNetworkPlayerType == NetworkPlayerType.Server)
                {
                    NetworkSystem.StopServer();
                    Debug.Log($"Server Stopped");
                    DebugText.text += $"Server Stopped...\n";
                    StartServerButton.interactable = true;
                    DisconnectServerButton.interactable = false;
                    ResetUIVariablesOnServerReset();
                }        
            }
        }
        /// <summary>
        /// Called via the Client Confirm Button
        /// </summary>
        public void UIConfirmServerName()
        {
            //lock in server name
            //override if we have Ip address typed in
            string textToCheck = string.Empty;
            switch(SelectedDeviceType){
                case DevicePlayerType.iPad:
                case DevicePlayerType.None:
                    textToCheck = IPadClientUIData.InputFieldClientServerIPOverride.text;
                    break;
                case DevicePlayerType.MetaQuest:
                    textToCheck = VRClientUIData.InputFieldClientServerIPOverride.text;
                    break;
            }
            if(!string.IsNullOrEmpty(textToCheck))
            {
                serverIPToConnect = textToCheck;
                serverIPFound = true;
                //ServerNameInputField.interactable = false;
                //StartClientButton.interactable = true;
                switch(SelectedDeviceType){
                    case DevicePlayerType.iPad:
                    case DevicePlayerType.None:
                        IPadClientUIData.ButtonClientConfirmServer.interactable = false;
                        IPadClientUIData.InputFieldClientServerName.interactable = false;
                        if (IPadClientUIData.InputFieldClientPlayerName != null)
                        {
                            IPadClientUIData.InputFieldClientPlayerName.interactable = false;
                        }
                        IPadClientUIData.ButtonClientStart.interactable = true;
                    break;
                    case DevicePlayerType.MetaQuest:
                        VRClientUIData.ButtonClientConfirmServer.interactable = false;
                        VRClientUIData.InputFieldClientServerName.interactable = false;
                        if (VRClientUIData.InputFieldClientPlayerName != null)
                        {
                            VRClientUIData.InputFieldClientPlayerName.interactable = false;
                        }
                        VRClientUIData.ButtonClientStart.interactable = true;
                    break;
                    
                }
                //ConfirmServerNameButton.interactable = false;
                Debug.Log($"Override IP: {serverIPToConnect}");
                DebugText.text += $"Override IP: {serverIPToConnect}\n";
                return;
            }
            DisplayServerName();
            if(serverIPFound)
            {
                //ServerNameInputField.interactable = false;
                //StartClientButton.interactable = true;
                switch(SelectedDeviceType){
                    case DevicePlayerType.iPad:
                    case DevicePlayerType.None:
                        IPadClientUIData.ButtonClientConfirmServer.interactable = false;
                        IPadClientUIData.ButtonClientStart.interactable = true;
                        IPadClientUIData.InputFieldClientServerName.interactable = false;
                    break;
                    case DevicePlayerType.MetaQuest:
                        VRClientUIData.ButtonClientConfirmServer.interactable = false;
                        VRClientUIData.ButtonClientStart.interactable = true;
                        VRClientUIData.InputFieldClientServerName.interactable = false;
                    break;
                    
                }
                //ConfirmServerNameButton.interactable = false;
            }else
            {
                Debug.LogError($"Didn't find server IP name: {WordCheck}, please check spelling");
                DebugText.text += $"Didn't find server IP name: {WordCheck}, please check spelling\n";
                WordCheck = "";
                //ServerNameInputField.text = "";
                //ServerNameInputField.interactable = true;
                switch(SelectedDeviceType){
                    case DevicePlayerType.iPad:
                    case DevicePlayerType.None:
                        IPadClientUIData.ButtonClientStart.interactable = false;
                        IPadClientUIData.ButtonClientConfirmServer.interactable = true;
                        IPadClientUIData.InputFieldClientServerName.interactable = true;
                        if (IPadClientUIData.InputFieldClientPlayerName != null)
                        {
                            IPadClientUIData.InputFieldClientPlayerName.interactable = true;
                        }
                        IPadClientUIData.InputFieldClientServerName.text = "";
                        break;
                        case DevicePlayerType.MetaQuest:
                        VRClientUIData.ButtonClientStart.interactable = false;
                        VRClientUIData.ButtonClientConfirmServer.interactable = true;
                        VRClientUIData.InputFieldClientServerName.interactable = true;
                        if (VRClientUIData.InputFieldClientPlayerName != null)
                        {
                            VRClientUIData.InputFieldClientPlayerName.interactable = true;
                        }
                        VRClientUIData.InputFieldClientServerName.text = "";
                        break;

                }
                //ConfirmServerNameButton.interactable = true;
                //StartClientButton.interactable=false;
            }
        }
        public void StartClientConnectionUIAction()
        {
            if(NetworkSystem!=null)
            {
                if(NetworkSystem.TheSystemData.TheNetworkPlayerType == NetworkPlayerType.Client && serverIPFound)
                {
                    
                    var port = NetworkSystem.PortAddress;
                    NetworkSystem.StartClientPlayer(serverIPToConnect,port,PlayerName);
                    Debug.Log($"Attempting to connect to server at: {serverIPToConnect}");
                    DebugText.text += $"Attempting to connect to server at: {serverIPToConnect}\n";
                }
            }
        }
        /// <summary>
        /// Called from an external source
        /// like if you wanted to have someone in VR grab an item that meant an IP address
        /// we then turn off all the other UI related items
        /// </summary>
        /// <param name="externalIpAddress"></param>
        public void ExternalServerIPInjection(string externalIpAddress)
        {
            serverIPToConnect = externalIpAddress;
            DebugText.text += $"Override IP: {serverIPToConnect}\n";
            Debug.LogWarning($"Manual override of ip address, changed to: {serverIPToConnect}");
            serverIPFound = true;
           
            var fullIP = NetworkSystem.CurrentIP.ToString();
            Debug.Log($"My IP Address: {fullIP}");
            Debug.Log($"Server IP To Attempt Connection too: {serverIPToConnect}");
            DebugText.text += $"Server IP To Attempt Connection too: {serverIPToConnect}\n";
            ServerNameDisplay.text = serverIPToConnect;
            
            
            switch (SelectedDeviceType)
            {
                case DevicePlayerType.MetaQuest:
                    VRClientUIData.ButtonClientConfirmServer.interactable = false;
                    VRClientUIData.ButtonClientStart.interactable = true;
                    VRClientUIData.InputFieldClientServerName.interactable = false;
                    if (VRClientUIData.InputFieldClientPlayerName != null)
                    {
                        VRClientUIData.InputFieldClientPlayerName.interactable = false;
                    }
                    break;

            }
        }
        /// <summary>
        /// Called from UI button to stop the client from the client
        /// this then hits an Rpc that is on the FPNetworkPlayer script
        /// </summary>
        public void StopClientConnectionUIAction()
        {
            if(NetworkSystem!=null)
            {
                if(NetworkSystem.NetworkManager.IsConnectedClient)
                {
                    DebugText.text+=$"Connected client, requesting to disconnect...\n";
                }
                if(NetworkSystem.TheSystemData.TheNetworkPlayerType == NetworkPlayerType.Client)
                {
                    //RPC call to our FPNetworkPlayer
                    
                    var networkClientObj = NetworkSystem.ReturnLocalClientObject(NetworkSystem.GetLocalClientID());
                    if(networkClientObj!=null)
                    {
                        // Get the player object associated with this client
                        var playerNetworkObject = networkClientObj.PlayerObject;
                        if(playerNetworkObject.GetComponent<FPNetworkPlayer>())
                        {
                            var data = playerNetworkObject.GetComponent<FPNetworkPlayer>().ReturnClientDataStruct("Disconnect request",NetworkMessageType.ClientDisconnectRequest);
                            
                            playerNetworkObject.GetComponent<FPNetworkPlayer>().DisconnectClientRequestRpc(data);
                            //NetworkSystem.DisconnectClientPlayer()
                            Debug.Log($"Client Stopped");
                            DebugText.text += $"Client Stopped...\n";
                        }else
                        {
                            Debug.LogError($"FPNetworkPlayer not found on the player prefab.");
                            DebugText.text += $"ERROR: FPNetworkPlayer not found on the player prefab.\n";
                        }
                    }else
                    {
                        Debug.LogError($"Network Client Obj returned null using ID: {NetworkSystem.GetLocalClientID()}");
                        DebugText.text+= $"ERROR: Network Client Obj returned null using ID: {NetworkSystem.GetLocalClientID()}\n";
                    }
                }
            }
        }
        public void UIInputServerNameChange()
        {
            //WordCheck = ServerNameInputField.text;
            switch(SelectedDeviceType){
                    case DevicePlayerType.iPad:
                    case DevicePlayerType.None:
                        WordCheck = IPadClientUIData.InputFieldClientServerName.text;
                        IPadClientUIData.ButtonClientConfirmServer.interactable = true;
                        break;
                        case DevicePlayerType.MetaQuest:
                        WordCheck = VRClientUIData.InputFieldClientServerName.text;
                        VRClientUIData.ButtonClientConfirmServer.interactable = true;
                        break;
                }
            
        }
        /// <summary>
        /// Referenced from the dropdown for Client Player Name
        /// </summary>
        public void UIPlayerNameChanged()
        {
            //WordCheck = ServerNameInputField.text;
            switch (SelectedDeviceType)
            {
                case DevicePlayerType.iPad:
                case DevicePlayerType.None:
                    PlayerName = IPadClientUIData.InputFieldClientPlayerName.text;
                    //IPadClientUIData.ButtonClientConfirmServer.interactable = true;
                    break;
                case DevicePlayerType.MetaQuest:
                    PlayerName = VRClientUIData.InputFieldClientPlayerName.text;
                    //VRClientUIData.ButtonClientConfirmServer.interactable = true;
                    break;
            }
        }
        private void DisplayServerName()
        {
            FPIPWord wordMapping = LoadIPWordMappings();
            if (wordMapping != null)
            {
                Debug.Log("Loaded IP Word Mappings successfully.");
                DebugText.text += "Loaded IP Word Mappings successfully.\n";
            }else{
                Debug.LogError($"Could not load IP Word Mappings.");
                DebugText.text += $"Could not load IP Word Mappings.\n";
                return;
            }
            var fullIP = NetworkSystem.CurrentIP.ToString();
            Debug.Log($"My IP Address: {fullIP}");
            if(NetworkSystem.TheSystemData.TheNetworkPlayerType != NetworkPlayerType.Server)
            {
                Debug.LogWarning("This is not a server, we must be a client, let's display the input panel for the user to put the magic word in");
                var serverIPLastThree = wordMapping.GetIPByWord(WordCheck);
                if(serverIPLastThree!=null)
                {
                    //take my fullIP, remove the last three and add the three I just got
                    string[] ipParts = fullIP.Split('.');
                    
                    //remove leading zeros
                    serverIPLastThree = serverIPLastThree.TrimStart('0');

                    ipParts[3] = serverIPLastThree;
                    //remove leading zeros
                    

                    string newIP = string.Join(".", ipParts);
                    serverIPToConnect=newIP;
                    serverIPFound = true;
                    Debug.Log($"Server IP To Attempt Connection too: {serverIPToConnect}");
                    DebugText.text += $"Server IP To Attempt Connection too: {serverIPToConnect}\n";
                    ServerNameDisplay.text = serverIPToConnect;
                    //Debug.Log($"Server IP: {serverIPLastThree}");
                }
                return;
            }else
            {
                // add leading zeros if the number is less than 100
                
                string lastThreeDigits = NetworkSystem.CurrentIP.ToString().Split('.')[3];
                if (lastThreeDigits.Length < 3)
                {
                    lastThreeDigits = lastThreeDigits.PadLeft(3, '0');
                }
                string[] words = wordMapping.GetWordsByIP(lastThreeDigits);
                if (words != null)
                {
                    if(moduleData!=null)
                    {
                        Debug.Log($"Module Found: {moduleData.ModuleLabel}");
                        DebugText.text += $"Module Found: {moduleData.ModuleLabel}\n";
                        //spanish is 0, french is 1
                        if(words.Length>=2)
                        {
                            if(moduleData.ModuleLanguage == FP_Language.Spanish)
                            {
                                serverName = words[0];
                            }
                        if(moduleData.ModuleLanguage == FP_Language.French)
                            {
                                serverName = words[1];
                            }
                        }
                    }else
                    {
                        Debug.LogWarning("No module data found, using the first word in the list");
                        DebugText.text += "No module data found, using the first word in the list\n";
                        serverName = string.Join(" ", words);
                    }
                    Debug.Log($"Activate Server Start Button");
                    DebugText.text += $"Activate Server Start Button\n";
                    StartServerButton.interactable = true;
                    DisconnectServerButton.interactable = false;
                    ServerNameDisplay.text = $"{moduleData.ModuleLabel}\n{serverName}\n{lastThreeDigits}";
                    Debug.Log($"Server Name: {serverName}");
                }
            }   
        }
        private void UnlockButtonConfirm()
        {
            if(languageLevelSelected && languageSelected)
            {
                ConfirmLanguageButton.interactable = true;
            }
        }
        #endregion
        /// <summary>
        /// Reset local parameters on a server disconnect/reset via server (not client)
        /// </summary>
        private void ResetUIVariablesOnServerReset()
        {
            clientConfirmedConnections = 0;
            switch(SelectedDeviceType){
                case DevicePlayerType.iPad:
                case DevicePlayerType.None:
                    if(IPadClientUIData.IsClientType){
                        IPadClientUIData.InputFieldClientServerName.interactable = true;
                        IPadClientUIData.ButtonClientConfirmServer.interactable = true;
                        IPadClientUIData.ButtonClientStop.interactable = false;
                        IPadClientUIData.InputFieldClientServerName.text = "";
                        if (IPadClientUIData.InputFieldClientPlayerName != null)
                        {
                            IPadClientUIData.InputFieldClientPlayerName.text = "Player";
                        }
                       
                    }
                    break;
                case DevicePlayerType.MetaQuest:
                    if(VRClientUIData.IsClientType)
                    {
                        VRClientUIData.ButtonClientConfirmServer.interactable = true;
                        VRClientUIData.InputFieldClientServerName.interactable = true;
                        VRClientUIData.ButtonClientStop.interactable = false;
                        VRClientUIData.InputFieldClientServerName.text = "";
                        if(VRClientUIData.InputFieldClientPlayerName!= null)
                        {
                            VRClientUIData.InputFieldClientPlayerName.text = "Player";
                        }
                       
                    }
                    
                    break;

            }
            //ServerNameInputField.interactable = true;
            //ConfirmServerNameButton.interactable = true;
            //StopClientButton.interactable = false;
        }
        #region Client Event Functions
        public void UITestEventClientMessage()
        {
            var localClientId = NetworkSystem.GetLocalClientID();
            var localClientObject = NetworkSystem.ReturnLocalClientObject(localClientId);
            if(localClientObject!=null)
            {
                // Get the player object associated with this client
                var playerNetworkObject = localClientObject.PlayerObject;

                // Assuming the player object has a script that handles the custom RPC
                var playerScript = playerNetworkObject.GetComponent<FPNetworkPlayer>();

                if (playerScript != null)
                {
                    // Call the custom RPC on the player's script
                    playerScript.UISendServerEventDetails(ClientMessageInputField.text,NetworkMessageType.ClientMessage);
                    DebugText.text += $"Client Message Sent: {ClientMessageInputField.text}\n";
                }
                else
                {
                    Debug.LogError("UISendMessageServer not found on the player prefab.");
                    DebugText.text += "UISendMessageServer not found on the player prefab.\n";
                }
            }
            else
            {
                Debug.LogError($"Client player object not found for {localClientId}.");
                DebugText.text += $"Client player object not found for {localClientId}.\n";
            }
        }
        /// <summary>
        /// local client Debug
        /// </summary>
        public void LoadCompleteDebug(SceneEvent sceneEvent)
        {
            Debug.Log($"Client and server Finished:: {sceneEvent.SceneName}");
            DebugText.text += $"Client & Server Finished: {sceneEvent.SceneName}\n";
            if(sceneEvent.SceneName == moduleData.ModuleSceneName)
            {
                Debug.Log($"Scene Loaded: {sceneEvent.SceneName}");
                DebugText.text += $"Scene Loaded: {sceneEvent.SceneName}\n";
                //do something
            }
        }
        #endregion
        #region Callbacks
        public void OnNetworkManagerServerStopped(bool serverBooleanStopped)
        {
            Debug.LogWarning($"Callback from NetworkManager.OnServerStopped: {serverBooleanStopped}");
            //unload the network scene if we are the server
            if(NetworkSystem.NetworkManager.IsServer)
            {
                //this actually unloads the additive scene our server probably has loaded
                NetworkSystem.UnloadnetworkScene();
            }
        }
        public void ServerDisconnectCallback()
        {
            //check network cache for any data
            if(this.GetComponent<FPNetworkCache>())
            {
                var cache = this.GetComponent<FPNetworkCache>();
                cache.PrintData();
            }
        }
        public void OnServerEventTriggered(FPServerData serverData)
        {
            Debug.Log($"Server Event Triggered: {serverData.ServerAction}");
            DebugText.text += $"Server Event Triggered: {serverData.ServerAction}\n";
        }
        public void OnClientEventTriggered(FPClientData clientData)
        {
            Debug.Log($"Client Event Triggered: {clientData.ClientAction}");
            DebugText.text += $"Client Event Triggered: {clientData.ClientAction}, with status of {clientData.Status}\n";
            if(clientData.Status == ConnectionStatus.Disconnected)
            {
                //reset the UI
                UIClientTestPanel.SetActive(false);
                if(NetworkSystem!=null)
                {
                    NetworkSystem.UnloadNetworkSceneDisconnectedClient();
                }
                Debug.LogWarning($"Client Disconnected events go after this - like changing UI stuff");
                //update the ui and the state of buttons
                //ConfirmServerNameButton.interactable = true;
                //StartClientButton.interactable = false;
                //StopClientButton.interactable = false;
                //LanguageIPField.interactable = true;
                //ClientOverrideIPField.text = "";
                //ClientOverrideIPField.interactable = true;
                // updated configuration
                switch(SelectedDeviceType){
                    case DevicePlayerType.iPad:
                    case DevicePlayerType.None:
                        if(IPadClientUIData.IsClientType){
                            IPadClientUIData.ButtonClientConfirmServer.interactable = true;
                            IPadClientUIData.ButtonClientStart.interactable = false;
                            IPadClientUIData.ButtonClientStop.interactable = false;
                            IPadClientUIData.InputFieldClientServerName.interactable = true;
                            if (IPadClientUIData.InputFieldClientPlayerName != null)
                            {
                                IPadClientUIData.InputFieldClientPlayerName.interactable = true;
                            }
                            IPadClientUIData.InputFieldClientServerName.text = "";
                            IPadClientUIData.InputFieldClientServerIPOverride.interactable = true;
                            IPadClientUIData.InputFieldClientServerIPOverride.text = "";
  
                        }
                        break;
                    case DevicePlayerType.MetaQuest:
                        if(VRClientUIData.IsClientType)
                        {
                            VRClientUIData.ButtonClientConfirmServer.interactable = true;
                            VRClientUIData.ButtonClientStart.interactable = false;
                            VRClientUIData.ButtonClientStop.interactable = false;
                            VRClientUIData.InputFieldClientServerName.interactable = true;
                            if (VRClientUIData.InputFieldClientPlayerName != null)
                            {
                                VRClientUIData.InputFieldClientPlayerName.interactable = true;
                            }
                            VRClientUIData.InputFieldClientServerName.text = "";
                            VRClientUIData.InputFieldClientServerIPOverride.interactable = true;
                            VRClientUIData.InputFieldClientServerIPOverride.text = "";
                        }
                        
                        break;

                }

                //is there a way to check if our scene has been added via loaded in?
                //if so we want to request the networkManager to unload it

            }
        }
        /// <summary>
        /// This fires off when we have the minimum number of players connected to the server
        /// </summary>
        /// <param name="clientConfirmedID"></param>
        /// <param name="currentNumberConnected"></param>
        public void OnServerConfirmationCheck(ulong clientConfirmedID, int currentNumberConnected)
        {
            if (currentNumberConnected >= clientConnectionsUntilStart)
            {
                Debug.Log("Starting ConfirmScene Handshake");
                DebugText.text += $"Starting ConfirmScene Handshake: {currentNumberConnected}/{clientConnectionsUntilStart}\n";
                // send a message to all clients to start the confirmation process
                var keys = NetworkSystem.NetworkManager.ConnectedClients.Keys.ToList();
                // setup data
                FPNetworkDataStruct ServerData = new FPNetworkDataStruct()
                {
                    TheNetworkMessage = $"Confirm Ready State with {keys.Count} connected clients!",
                    TheNetworkMessageType = NetworkMessageType.ServerConfirmation,
                    TheDevicePlayerType = DevicePlayerType.None,
                    TheNetworkPlayerType = NetworkPlayerType.Server,
                    TheClientID = clientConfirmedID,
                    ClientIPAddress = NetworkSystem.CurrentIP.ToString(),
                    ClientColor = NetworkSystem.ServerColor.ToString()
                };
                for (int i = 0; i < keys.Count; i++)
                {
                    var aKey = keys[i];
                    var aClient = NetworkSystem.NetworkManager.ConnectedClients[aKey];
                    if (aClient.PlayerObject.GetComponent<FPNetworkPlayer>())
                    {
                        aClient.PlayerObject.GetComponent<FPNetworkPlayer>().ServerMessageConfirmReadyStateClientRpc(ServerData);
                    }
                }
            }
            else
            {
                Debug.Log($"Still waiting for all clients to connect: {currentNumberConnected}/{clientConnectionsUntilStart}");
                DebugText.text += $"Still waiting for all clients to connect: {currentNumberConnected}/{clientConnectionsUntilStart}\n";
            }
        }
        /// <summary>
        /// Debug method to display our ip address at the beginning for testing
        /// </summary>
        /// <param name="ip"></param>
        public void OnLocalIPAddressData(IPAddress ip)
        {
            DebugText.text += $"Local IP Address: {ip}\n";
        }
        
        private void LocalSceneLoadDebug(string sceneName,SceneEventProgressStatus sceneStatus,bool loadedCorrectly)
        {
            DebugText.text = $"Scene Loaded: {sceneName} with status: {sceneStatus.ToString()} and it loaded? {loadedCorrectly}\n";
            Debug.Log($"Scene Loaded: {sceneName} with status: {sceneStatus.ToString()} and it loaded? {loadedCorrectly}");
            if(NetworkSystem.NetworkManager.IsClient)
            {
                NetworkSystem.UpdateLastSceneFromClient(sceneName);
            }
        }
        /// <summary>
        /// Coming in from FPNetworkSystem as a callback
        /// this comes in under the 'server' context, don't think clients will call this but will double/check in the logic
        /// </summary>
        /// <param name="clientConfirmedID"></param>
        public void OnClientReturnConfirmationCheck(ulong clientConfirmedID)
        {
            Debug.Log($"Client confirmed, {clientConfirmedID}");
            if (DebugText != null)
            {
                DebugText.text += $"Client confirmed, {clientConfirmedID}\n";
            }
            
            //double checking
            if (NetworkSystem.NetworkManager.IsServer)
            {
                clientConfirmedConnections++;
                if(clientConfirmedConnections>=clientConnectionsUntilStart)
                {
                    Debug.Log("All Clients Confirmed, Starting Game");
                    if (DebugText != null)
                    {
                        DebugText.text += $"All Clients Confirmed, Starting Game\n";
                    }
                   
                    //tell each client we are about to load a network scene
                    var keys = NetworkSystem.NetworkManager.ConnectedClients.Keys.ToList();
                    for (int i = 0; i < keys.Count; i++)
                    {
                        var aKey = keys[i];
                        var aClient = NetworkSystem.NetworkManager.ConnectedClients[aKey];
                        if (aClient.PlayerObject.GetComponent<FPNetworkPlayer>())
                        {
                            aClient.PlayerObject.GetComponent<FPNetworkPlayer>().ServerMessageAboutToLoadSceneClientRpc();
                        }
                    }
                    //start the game by loading the scene
                    StartCoroutine(DelayLoadingNetworkScene());
                }
            }
        }
        IEnumerator DelayLoadingNetworkScene()
        {
            Debug.LogWarning($"Network Scene Delay activated!");
            yield return new WaitForSeconds(delayUntilStart);
            NetworkSystem.LoadNetworkScene(moduleData.ModuleSceneName);
        }
        #endregion  
    }
}
