using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using DiGiorgioLabs.DroneDefenseVR.Core;
using DiGiorgioLabs.DroneDefenseVR.Threat;

namespace DiGiorgioLabs.DroneDefenseVR.Drones
{
    /// <summary>
    /// Commands the 3 tactical drones, spawns hostile threats, and handles point-to-command raycasts.
    /// </summary>
    public class DroneSwarmCommander : MonoBehaviour
    {
        public Transform PlayerTransform;
        public Camera TargetCamera;

        public float PlayerIntegrity { get; private set; } = 100f;
        public int Wave { get; private set; } = 1;
        public int ThreatsNeutralized { get; private set; }
        public int SelectedDroneIndex { get; set; } = -1; // -1 = All 3 Drones
        public float EmpBurstCooldown { get; private set; }

        public readonly List<DroneUnit> Drones = new List<DroneUnit>();
        public ThreatAgent ActiveThreat { get; private set; }

        private Material _sharedMat;
        private float _respawnTimer;

        public void Initialize(Transform player, Camera cam)
        {
            PlayerTransform = player;
            TargetCamera = cam;
            _sharedMat = TacticalMaterials.CreateLitMaterial("DroneDefenseMat");

            SpawnDrones();
            SpawnThreat();
        }

        private void SpawnDrones()
        {
            DroneRole[] roles = { DroneRole.ReconTracker, DroneRole.AegisShield, DroneRole.PulseInterceptor };
            for (int i = 0; i < 3; i++)
            {
                var go = new GameObject($"Drone_{i + 1}_{roles[i]}");
                go.transform.position = PlayerTransform.position + new Vector3((i - 1) * 1.5f, 1.8f, 2.0f);
                var drone = go.AddComponent<DroneUnit>();
                drone.Initialize(roles[i], i, PlayerTransform, _sharedMat);
                Drones.Add(drone);
            }
        }

        private void SpawnThreat()
        {
            if (ActiveThreat != null) Destroy(ActiveThreat.gameObject);

            float angle = Random.Range(-55f, 55f) * Mathf.Deg2Rad;
            Vector3 spawnPos = PlayerTransform.position + new Vector3(Mathf.Sin(angle) * 11f, 0f, Mathf.Cos(angle) * 11f);

            var go = new GameObject($"Threat_Wave_{Wave}");
            go.transform.position = spawnPos;
            ActiveThreat = go.AddComponent<ThreatAgent>();
            ActiveThreat.Initialize(PlayerTransform, _sharedMat, Wave);
            ActiveThreat.OnPlayerAttacked += HandlePlayerAttacked;
            ActiveThreat.OnNeutralized += HandleThreatNeutralized;

            foreach (var d in Drones) d.CurrentThreat = ActiveThreat;
        }

        private void HandlePlayerAttacked(float rawDamage)
        {
            bool aegisBlocking = Drones.Count > 1 && Drones[1].IsShieldActive;
            float dmg = aegisBlocking ? rawDamage * 0.2f : rawDamage;
            PlayerIntegrity = Mathf.Max(0f, PlayerIntegrity - dmg);
        }

        private void HandleThreatNeutralized(ThreatAgent threat)
        {
            ThreatsNeutralized++;
            Wave++;
            _respawnTimer = 2.5f;
        }

        private void Update()
        {
            if (EmpBurstCooldown > 0f) EmpBurstCooldown = Mathf.Max(0f, EmpBurstCooldown - Time.deltaTime);

            if (_respawnTimer > 0f)
            {
                _respawnTimer -= Time.deltaTime;
                if (_respawnTimer <= 0f) SpawnThreat();
            }

            // Point-to-Command on Arena Floor (left click / XR trigger)
            if (Input.GetMouseButtonDown(0) && TargetCamera != null)
            {
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                    return;

                Ray ray = TargetCamera.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit, 40f))
                {
                    if (hit.collider.GetComponentInParent<ThreatAgent>() != null)
                    {
                        IssueOrder(DroneOrder.OrbitAndEngage);
                    }
                    else
                    {
                        SendToPoint(hit.point);
                    }
                }
            }

            // Desktop keyboard shortcuts
            if (Input.GetKeyDown(KeyCode.Alpha1)) SelectedDroneIndex = 0;
            if (Input.GetKeyDown(KeyCode.Alpha2)) SelectedDroneIndex = 1;
            if (Input.GetKeyDown(KeyCode.Alpha3)) SelectedDroneIndex = 2;
            if (Input.GetKeyDown(KeyCode.Alpha0)) SelectedDroneIndex = -1;
            if (Input.GetKeyDown(KeyCode.O)) IssueOrder(DroneOrder.OrbitAndEngage);
            if (Input.GetKeyDown(KeyCode.G)) IssueOrder(DroneOrder.GuardPerimeter);
            if (Input.GetKeyDown(KeyCode.F)) IssueOrder(DroneOrder.FollowPlayer);
            if (Input.GetKeyDown(KeyCode.Space)) TriggerEmpBurst();
        }

        public void IssueOrder(DroneOrder order)
        {
            for (int i = 0; i < Drones.Count; i++)
            {
                if (SelectedDroneIndex == -1 || SelectedDroneIndex == i)
                {
                    Drones[i].CurrentOrder = order;
                }
            }
        }

        public void SendToPoint(Vector3 worldPoint)
        {
            for (int i = 0; i < Drones.Count; i++)
            {
                if (SelectedDroneIndex == -1 || SelectedDroneIndex == i)
                {
                    Drones[i].CommandPoint = worldPoint + new Vector3((i - 1) * 1.1f, 0f, 0f);
                    Drones[i].CurrentOrder = DroneOrder.GoToPoint;
                }
            }
        }

        public void TriggerEmpBurst()
        {
            if (EmpBurstCooldown > 0f || ActiveThreat == null || ActiveThreat.IsNeutralized) return;
            EmpBurstCooldown = 6.0f;
            ActiveThreat.ApplyStasisHit(42f, 2.2f);
        }

        public void ResetSimulation()
        {
            PlayerIntegrity = 100f;
            Wave = 1;
            ThreatsNeutralized = 0;
            EmpBurstCooldown = 0f;
            IssueOrder(DroneOrder.OrbitAndEngage);
            SpawnThreat();
        }
    }
}
