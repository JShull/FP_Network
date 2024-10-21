namespace  FuzzPhyte.Network.Samples{
    using UnityEngine;
    using FuzzPhyte.Network;
    using System.Collections.Generic;
    using FuzzPhyte.Utility;
    using System;
    using UnityEngine.UI;
    using FuzzPhyte.SystemEvent;
    using TMPro;

    public class TellVRServerIPName : MonoBehaviour
    {
        public FPNetworkSystem NetworkSystem;
        public string WordCheck = "azul";
        public string jsonFileNameNoExtension = "IPWordMappings";
        #region Related to Tell VR Module Setup
        [Tooltip("One of the module data structures to use")]
        public List<TellVRModule> AllModules = new List<TellVRModule>();
        [Tooltip("Possible Data Selectable Objects to Use")]
        public List<FPNetworkData> AllDataConnectionProfiles = new List<FPNetworkData>();
        public FP_Language SelectedLanguage;
        public FP_LanguageLevel SelectedLanguageLevel;
        public DevicePlayerType SelectedDeviceType;
        public NetworkPlayerType SelectedNetworkType;
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
        private bool networkTypeSelected;
        #region UI Input Components
        public TMPro.TMP_Dropdown LanguageDropdown;
        public TMPro.TMP_Dropdown LanguageLevelDropdown;
        public TMPro.TMP_Dropdown DeviceTypeDropdown;
        public TMPro.TMP_Dropdown NetworkTypeDropdown;
        public TMPro.TMP_InputField ServerNameInputField;
        public Button ConfirmLanguageButton;
        public Button StartServerButton;
        public Button DisconnectServerButton;
        public Button ConfirmServerNameButton;
        public Button StartClientButton;
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
        #endregion
        #endregion
        public void Start()
        {
            NetworkSystem.OnServerEventTriggered+=OnServerEventTriggered;
            NetworkSystem.OnClientEventTriggered+=OnClientEventTriggered;
        }
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
        /// <summary>
        /// Dropdown UI Event is calling this from TMP_DropDowns in the scene(s)
        /// </summary>
        /// <param name="index"></param>
        public void UILanguageListDropDownChange(int index)
        {
            var selectedItem = LanguageDropdown.options[index];
            //convert the string to an enum
            if(System.Enum.TryParse(selectedItem.text, out FP_Language selectedLanguage))
            {
                SelectedLanguage = selectedLanguage;
                languageSelected = true;
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
                if(moduleData!=null)
                {
                    Debug.Log($"Module Found: {moduleData.ModuleLabel}");
                    DebugText.text += $"Module Found: {moduleData.ModuleLabel}\n";
                    ServerNameDisplay.enabled = true;
                    DisplayServerName();
                }
            }
        }
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
                }        
            }
        }
        /// <summary>
        /// Called via the Client Confirm Button
        /// </summary>
        public void UIConfirmServerName()
        {
            //lock in server name
            DisplayServerName();
            if(serverIPFound)
            {
                ServerNameInputField.interactable = false;
                StartClientButton.interactable = true;
                ConfirmServerNameButton.interactable = false;
            }else
            {
                Debug.LogError($"Didn't find server IP name: {WordCheck}, please check spelling");
                DebugText.text += $"Didn't find server IP name: {WordCheck}, please check spelling\n";
                WordCheck = "";
                ServerNameInputField.text = "";
                ServerNameInputField.interactable = true;
                ConfirmServerNameButton.interactable = true;
                StartClientButton.interactable=false;
            }
            
            
        }
        public void StartClientConnectionUIAction()
        {
            if(NetworkSystem!=null)
            {
                if(NetworkSystem.TheSystemData.TheNetworkPlayerType == NetworkPlayerType.Client && serverIPFound)
                {
                    
                    var port = NetworkSystem.PortAddress;
                    NetworkSystem.StartClientPlayer(serverIPToConnect,port);
                    //NetworkSystem.
                    //NetworkManager.ConnectionApprovalRequest
                    Debug.Log($"Attempting to connect to server at: {serverIPToConnect}");
                    DebugText.text += $"Attempting to connect to server at: {serverIPToConnect}\n";
                }
            }
        }
        public void UIInputServerNameChange()
        {
            WordCheck = ServerNameInputField.text;
            ConfirmServerNameButton.interactable = true;
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
                    playerScript.UISendMessageServer(ClientMessageInputField.text);
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
        #endregion
        #region Callbacks
        public void OnServerEventTriggered(FPServerData serverData)
        {
            Debug.Log($"Server Event Triggered: {serverData.ServerAction}");
            DebugText.text += $"Server Event Triggered: {serverData.ServerAction}\n";
        }
        public void OnClientEventTriggered(FPClientData clientData)
        {
            Debug.Log($"Client Event Triggered: {clientData.ClientAction}");
            DebugText.text += $"Client Event Triggered: {clientData.ClientAction}\n";
            if(clientData.Status == ConnectionStatus.Disconnected)
            {
                //reset the UI
                UIClientTestPanel.SetActive(false);
            }
        }
        #endregion
        
    }
}
