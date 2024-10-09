namespace FuzzPhyte.Network
{
    using UnityEngine;
    using System.Collections.Generic;

    [System.Serializable]
    public class FPIPWord
    {
        public Dictionary<string, string[]> ipToWords;

        public static FPIPWord LoadFromJson(string jsonPath)
        {
            // Assuming you have a utility to load text from the file.
            string jsonString = System.IO.File.ReadAllText(jsonPath); 
            return JsonUtility.FromJson<FPIPWord>(jsonString);
        }

        public string[] GetWordsByIP(string lastThreeDigits)
        {
            if (ipToWords.ContainsKey(lastThreeDigits))
            {
                return ipToWords[lastThreeDigits];
            }
            else
            {
                Debug.LogWarning($"No words found for IP ending in {lastThreeDigits}");
                return null;
            }
        }
    }
}
