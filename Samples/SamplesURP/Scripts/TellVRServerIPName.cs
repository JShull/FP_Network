namespace  FuzzPhyte.Network.Samples{
    using UnityEngine;
    using FuzzPhyte.Network;
    using System.Collections.Generic;
    using FuzzPhyte.Utility;
    using System;
    using UnityEngine.UI;

    public class TellVRServerIPName : MonoBehaviour
    {
        public FPNetworkSystem NetworkSystem;
        public string WordCheck = "azul";
        public string jsonFileNameNoExtension = "IPWordMappings";
        #region Related to Tell VR Module Setup
        public List<TellVRModule> AllModules = new List<TellVRModule>();
        public FP_Language SelectedLanguage;
        public FP_LanguageLevel SelectedLanguageLevel;
        [SerializeField]
        private TellVRModule moduleData;
        [SerializeField]
        private string serverName;
        private bool languageSelected;
        private bool languageLevelSelected;
        #region UI Input Components
        public TMPro.TMP_Dropdown LanguageDropdown;
        public TMPro.TMP_Dropdown LanguageLevelDropdown;
        public Button ConfirmLanguageButton;
        public Button StartServerButton;
        public TMPro.TextMeshProUGUI ServerNameDisplay;
        public TMPro.TextMeshProUGUI DebugText;
        #endregion
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
                    ServerNameDisplay.enabled = true;
                    DisplayServerName();
                }
            }
        }
        public void StartServerUIAction()
        {
            if(NetworkSystem!=null)
            {
                NetworkSystem.StartServer();
            }
        }
        #endregion
        private void DisplayServerName()
        {
            FPIPWord wordMapping = LoadIPWordMappings();
            if (wordMapping != null)
            {
                Debug.Log("Loaded IP Word Mappings successfully.");
            }else{
                Debug.LogError($"Could not load IP Word Mappings.");
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
                    ipParts[3] = serverIPLastThree;
                    string newIP = string.Join(".", ipParts);
                    Debug.Log($"Server IP To Attempt Connection too: {newIP}");
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
                        serverName = string.Join(" ", words);
                    }
                    Debug.Log($"Activate Server Start Button");
                    StartServerButton.interactable = true;
                    ServerNameDisplay.text = $"{moduleData.ModuleLabel}\n{serverName}";
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
        
    }
}
