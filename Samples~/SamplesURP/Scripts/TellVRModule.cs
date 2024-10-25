namespace FuzzPhyte.Network.Samples{
    using FuzzPhyte.Utility;
    using UnityEngine;

    [CreateAssetMenu(fileName = "TellVRModule", menuName = "FuzzPhyte/Modules/TellVRModule")]
    public class TellVRModule : FP_Data
    {
        [Tooltip("The language of the module")]
        public FP_Language ModuleLanguage;
        [Tooltip("The level of the Language")]
        public FP_LanguageLevel LanguageLevel;
        [Tooltip("The Scene to Load for the module")]
        public string ModuleSceneName;
        [Tooltip("If we load an asset via a package, this would be the index/address of that asset")]
        public string AssetSceneIndex;
        [Tooltip("The label of the module")]
        public string ModuleLabel;
        [Tooltip("The description of the module")]
        public string ModuleDescription;
        [Tooltip("The version of the module")]
        public int ModuleVersion;   
    }
}
