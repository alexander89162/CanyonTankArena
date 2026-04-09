using System.Collections.Generic;
using UnityEngine;    
    
    public class SpawnPoint : MonoBehaviour
    {
        public int id;
        public LayerMask unitLayerMask;
        public float requiredOpenSpace;

        private Queue<UnitData> queue = new Queue<UnitData>();
        private UnitManager unitManager;

        public void Enqueue(UnitData unit)
        {
            queue.Enqueue(unit);
        }

        public void TryFlush()
        {
            while (queue.Count > 0 && SpotOpen())
            {
                Spawn(queue.Dequeue());
            }
        }

        private void Spawn(UnitData unit)
        {
            unit.gameObject.transform.position = transform.position;
            unit.gameObject.transform.rotation = transform.rotation;

            var health = unit.gameObject.GetComponent<HealthComponent>();
            if (health != null)
            {
                health.Initialize(health.MaxHealth * unit.config.healthMultiplier);
                health.OnDeath.AddListener(() => unitManager.enemiesRemaining--);
            }

            unit.gameObject.SetActive(true);
        }

        private bool SpotOpen()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, requiredOpenSpace, unitLayerMask);
            return hits.Length == 0;
        }

        public bool HasQueuedUnits()
        {
            return queue.Count > 0;
        }

        public void Configure(LayerMask defaultMask, float defaultSpace, UnitManager manager)
        {
            if (unitLayerMask == 0) unitLayerMask = defaultMask;
            if (requiredOpenSpace == 0f) requiredOpenSpace = defaultSpace;
            unitManager = manager;
        }

        #if UNITY_EDITOR
        void OnValidate()
        {
            if (id == 0) 
            {
                var all = FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None);
                int maxId = 0;
                foreach (var sp in all)
                    if (sp != this) maxId = Mathf.Max(maxId, sp.id);
                id = maxId + 1;
            }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Gizmos.DrawSphere(transform.position, requiredOpenSpace);
        }
        #endif
    }