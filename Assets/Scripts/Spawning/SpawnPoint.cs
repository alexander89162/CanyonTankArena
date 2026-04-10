using System.Collections.Generic;
using UnityEditor;
using UnityEngine;    
    
    public class SpawnPoint : MonoBehaviour
    {
        public int id;
        public LayerMask unitLayerMask;
        public float requiredOpenSpace;
        public Vector3 exitOffset; // movement from spawn point in local space in order to exit spawn area (avoid getting stuck)
        public Vector3 exitPoint {get; private set;}

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
            #if UNITY_EDITOR
            if (unit.unitRoot == null) Debug.Log($"unit.gameObject null in Spawn()");
            #endif

            unit.unitRoot.transform.position = transform.position;
            unit.unitRoot.transform.rotation = transform.rotation;
            unit.spawnPoint = this;

            var holder = unit.unitRoot.AddComponent<UnitDataHolder>();
            holder.data = unit;

            #if UNITY_EDITOR
            if (holder == null) Debug.LogError($"UnitDataHolder component was null in Spawn() for prefab: {unit.config.prefabName}");
            #endif
    
            var health = unit.unitRoot.GetComponent<HealthComponent>();
            if (health != null)
            {
                health.Initialize(health.MaxHealth * unit.config.healthMultiplier);
                health.OnDeath.AddListener(() => unitManager.enemiesRemaining--);
            }
            #if UNITY_EDITOR
            else
                Debug.LogError($"Health component was null in Spawn() for prefab: {unit.config.prefabName}");
            #endif

            unit.unitRoot.SetActive(true);
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
            exitPoint = exitOffset != Vector3.zero 
                ? transform.position + transform.rotation * exitOffset
                : transform.position + transform.forward * 90f;
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
            // spawn point
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Gizmos.DrawSphere(transform.position, requiredOpenSpace);

            // draw for exit point
            Handles.color = Color.darkBlue;
            Handles.DrawAAPolyLine(20f, transform.position, transform.position + transform.rotation * exitOffset);
            Gizmos.color = Color.darkBlue;
            Gizmos.DrawSphere(transform.position + transform.rotation * exitOffset, 8f);
        }
        #endif
    }