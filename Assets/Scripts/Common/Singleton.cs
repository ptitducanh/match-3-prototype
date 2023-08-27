using System;
using UnityEngine;

namespace Scripts.Common
{
    public class Singleton<T> : MonoBehaviour where T : Singleton<T>
    {
        public static T Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null)
            {
                Instance = FindFirstObjectByType<T>();
            }
            else
            {
                Instance = (T) this;
            }
        }
    }
}