namespace FuzzPhyte.Network
{
    using UnityEngine;
    using System.Collections.Generic;

    [System.Serializable]
    public class FPIPWord
    {
        public List<FPIPWordEntry> ipToWordsList;
        public Dictionary<string, string[]> ipToWords;

        public void InitializeDictionary()
        {
            ipToWords = new Dictionary<string, string[]>();
            foreach (var entry in ipToWordsList)
            {
                ipToWords[entry.key] = entry.values;
            }
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
        public string GetIPByWord(string spanishOrFrench)
        {
            foreach (var entry in ipToWords)
            {
                foreach (var word in entry.Value)
                {
                    if (word == spanishOrFrench)
                    {
                        return entry.Key;
                    }
                }
            }
            Debug.LogWarning($"No IP found for word {spanishOrFrench}");
            return null;
        }
    }
}
