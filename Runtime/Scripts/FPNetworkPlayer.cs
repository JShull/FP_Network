
namespace FuzzPhyte.Network
{
    using UnityEngine;
    using FuzzPhyte.Utility;
    public class FPNetworkPlayer : MonoBehaviour
    {
        public DevicePlayerType ThePlayerType;

        public void OnClientSpawned()
        {
            switch(ThePlayerType)
            {
                case DevicePlayerType.iPad:
                    Debug.Log("iPad Player Spawned");
                    break;
                case DevicePlayerType.MetaQuest:
                    Debug.Log("MetaQuest Player Spawned");
                    break;
                default:
                    Debug.Log("Player Spawned");
                    break;
            }
        }
    }
}
