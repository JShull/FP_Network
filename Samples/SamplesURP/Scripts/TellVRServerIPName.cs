namespace  FuzzPhyte.Network.Samples{

    using UnityEngine;
    using FuzzPhyte.Network;
    using System.Collections.Generic;
    using FuzzPhyte.Utility;

    public class TellVRServerIPName : MonoBehaviour
    {
        public FPNetworkSystem NetworkSystem;
        public string WordCheck = "azul";

        public string jsonFileNameNoExtension = "IPWordMappings";
        #region Related to Tell VR Module Setup
        public List<TellVRModule> AllModules = new List<TellVRModule>();
        public FP_Language SelectedLanguage;
        public int SelectedLanguageLevel;
        [SerializeField]
        private TellVRModule moduleData;
        private bool languageSelected;
        private bool languageLevelSelected;
        public UnityEngine.UI.Dropdown LanguageDropdown;
        public UnityEngine.UI.Dropdown LanguageLevelDropdown;
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
        //GUI to test the server name
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 300));
            if (GUILayout.Button("Display Server Name"))
            {
                DisplayServerName();
            }
            GUILayout.EndArea();
        }
        public void UILanguageListDropDownChange(int index)
        {
            var selectedItem = LanguageDropdown.options[index];
            //convert the string to an enum
            if(System.Enum.TryParse(selectedItem.text, out FP_Language selectedLanguage))
            {
                SelectedLanguage = selectedLanguage;
                languageSelected = true;
            }
        }
        public void UILanguageLevelDropDownChange(int index)
        {
            SelectedLanguageLevel = index + 1;
            languageLevelSelected = true;
        }
        public void MatchModuleDataToUserInput()
        {
            if (languageSelected && languageLevelSelected)
            {
                moduleData = AllModules.Find(x => x.ModuleLanguage == SelectedLanguage && x.LanguageLevel == SelectedLanguageLevel);
                if(moduleData!=null)
                {
                    Debug.Log($"Module Found: {moduleData.ModuleLabel}");
                }
            }
        }
        public void DisplayServerName()
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
                if(serverIPLastThree!=null){
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
                string lastThreeDigits = NetworkSystem.CurrentIP.ToString().Split('.')[3];
                string[] words = wordMapping.GetWordsByIP(lastThreeDigits);
                if (words != null)
                {
                    string serverName = string.Join(" ", words);
                    Debug.Log($"Server Name: {serverName}");
                }
            }   
        }

    }
}
