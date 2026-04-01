using System;
using System.Collections.Generic;
using UnityEngine;

namespace Spriter2UnityDX
{
    [DisallowMultipleComponent]
    public class EventController : MonoBehaviour
    {
        private readonly Dictionary<string, Action> eventInfo = new Dictionary<string, Action>();

        public void AddHandler(string spriterEventName, Action action)
        {
            if (!eventInfo.ContainsKey(spriterEventName))
            {
                eventInfo[spriterEventName] = null;
            }

            eventInfo[spriterEventName] += action;
        }

        public void RemoveHandler(string spriterEventName, Action action)
        {
            if (eventInfo.TryGetValue(spriterEventName, out var existing))
            {
                existing -= action;

                if (existing == null)
                {
                    eventInfo.Remove(spriterEventName);
                }
                else
                {
                    eventInfo[spriterEventName] = existing;
                }
            }
        }

        // Called by animation events.
        public void EventController_HandleEvent(string spriterEventName)
        {
            // Note: The name of this method is meant to be unique to this component so that it can be easily found
            // during reimports and removed from animations (while leaving user-defined animation events alone.)

            if (eventInfo.TryGetValue(spriterEventName, out var action))
            {
                action?.Invoke();
            }
            else
            {
                Debug.LogWarning($"A Spriter Event Handler for the Spriter event '{spriterEventName}' didn't " +
                    "register with the Event Controller");
            }
        }
    }
}
