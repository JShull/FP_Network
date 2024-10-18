using UnityEngine;

namespace FuzzPhyte.Network
{
    using FuzzPhyte.SystemEvent;
    public class FPNetworkServerEventComponent:FPEventComponent<FPNetworkServerEvent>
    {
        protected override object GetEventData()
        {
            return GameEvent; // Return the FPEvent
        }

        protected override void ExecuteEvent()
        {
            Debug.Log($"Executing server event: {this.gameObject.name}: {GameEvent}");
            GameEvent.Execute();
        }

        public override void ManagerEvent()
        {
            // Custom logic for manager events
            Debug.Log($"Server, Manager Event? {this.gameObject.name}");
        }
    }
}
