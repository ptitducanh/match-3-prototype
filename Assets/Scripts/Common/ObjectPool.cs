using System.Collections.Generic;
using UnityEngine;

namespace Scripts.Common
{
    /// <summary>
    /// A simple object pool implementation.
    /// </summary>
    public class ObjectPool : Singleton<ObjectPool>
    {
        [SerializeField] private int          poolSize = 10;
        [SerializeField] private GameObject[] prefabs;

        private Dictionary<string, List<GameObject>> _pool      = new();
        private Dictionary<string, GameObject>       _prefabMap = new();

        private void Start()
        {
            InitializePool();
        }

        /// <summary>
        /// Get a game object from the pool.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public GameObject Get(string name)
        {
            if (!_pool.ContainsKey(name))
            {
                _pool.Add(name, new List<GameObject>());
            }

            if (_pool[name].Count == 0)
            {
                var prefab = _prefabMap[name];
                var go     = Instantiate(prefab);
                go.name = name;
                go.SetActive(true);
                return go;
            }

            var last   = _pool[name].Count - 1;
            var result = _pool[name][last];
            _pool[name].RemoveAt(last);
            result.SetActive(true);
            return result;
        }

        /// <summary>
        /// Return a game object to the pool.
        /// </summary>
        /// <param name="go"></param>
        public void Return(GameObject go)
        {
            if (!_pool.ContainsKey(go.name))
            {
                _pool.Add(go.name, new List<GameObject>());
            }

            _pool[go.name].Add(go);
            go.SetActive(false);
            go.transform.SetParent(null);
        }

        private void InitializePool()
        {
            foreach (var prefab in prefabs)
            {
                _prefabMap.Add(prefab.name, prefab);
                for (int i = 0; i < poolSize; i++)
                {
                    var itemObject = Instantiate(prefab);
                    itemObject.name = prefab.name;
                    Return(itemObject);
                }
            }
        }
    }
}