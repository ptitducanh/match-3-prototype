using System;
using System.Collections.Generic;
using UnityEngine;

namespace Scripts.Common
{
    public class EventHub : Singleton<EventHub>
    {
        private Dictionary<string, List<Action>> _events = new();
        
        public void Subscribe(string eventName, Action action)
        {
            if (!_events.ContainsKey(eventName))
            {
                _events.Add(eventName, new List<Action>());
            }
            _events[eventName].Add(action);
        }
        
        public void Unsubscribe(string eventName, Action action)
        {
            if (!_events.ContainsKey(eventName))
            {
                return;
            }
            _events[eventName].Remove(action);
        }
        
        public void Publish(string eventName)
        {
            if (!_events.ContainsKey(eventName))
            {
                return;
            }
            foreach (var action in _events[eventName])
            {
                action();
            }
        }
    }
}