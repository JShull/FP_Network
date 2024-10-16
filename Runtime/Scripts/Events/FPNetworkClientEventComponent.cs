using UnityEngine;

namespace FuzzPhyte.Network
{
    using FuzzPhyte.SystemEvent;
    public class FPNetworkClientEventComponent:FPEventComponent<FPClientEvent>
    {
        protected override object GetEventData()
        {
            return GameEvent; // Return the FPEvent
        }

        protected override void ExecuteEvent()
        {
            Debug.Log($"Executing Client event: {this.gameObject.name}: {GameEvent}");
            GameEvent.Execute();
        }

        public override void ManagerEvent()
        {
            // Custom logic for manager events
            Debug.Log($"Client, Manager Event? {this.gameObject.name}");
        }
    }
}
