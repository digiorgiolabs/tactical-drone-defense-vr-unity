using UnityEngine;
using DiGiorgioLabs.DroneDefenseVR.Core;
using DiGiorgioLabs.DroneDefenseVR.Threat;

namespace DiGiorgioLabs.DroneDefenseVR.Drones
{
    public enum DroneRole
    {
        ReconTracker = 0,
        AegisShield = 1,
        PulseInterceptor = 2
    }

    public enum DroneOrder
    {
        FollowPlayer,
        GuardPerimeter,
        OrbitAndEngage,
        GoToPoint
    }

    /// <summary>
    /// Individual controllable tactical drone with procedural 3D quad-rotor geometry and role abilities.
    /// </summary>
    public class DroneUnit : MonoBehaviour
    {
        public DroneRole Role;
        public DroneOrder CurrentOrder = DroneOrder.OrbitAndEngage;
        public Transform PlayerTransform;
        public ThreatAgent CurrentThreat;
        public Vector3 CommandPoint;
        public int FormationIndex;

        public float MoveSpeed = 5.5f;
        public float ActionCooldown = 1.15f;
        public bool IsShieldActive { get; private set; }

        private float _cooldownTimer;
        private Transform[] _rotors = new Transform[4];
        private Renderer _coreRend;
        private GameObject _shieldWall;
        private LineRenderer _actionBeam;
        private float _beamFlashTimer;
        private Material _sharedMat;

        public Color RoleColor => Role switch
        {
            DroneRole.ReconTracker => new Color(0.20f, 0.85f, 1.0f),
            DroneRole.AegisShield => new Color(0.25f, 0.55f, 1.0f),
            _ => new Color(0.22f, 0.95f, 0.62f)
        };

        public void Initialize(DroneRole role, int index, Transform player, Material sharedMat)
        {
            Role = role;
            FormationIndex = index;
            PlayerTransform = player;
            _sharedMat = sharedMat;
            BuildDroneGeometry();
        }

        private void BuildDroneGeometry()
        {
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = $"{Role}_Chassis";
            body.transform.SetParent(transform, false);
            body.transform.localScale = new Vector3(0.32f, 0.10f, 0.32f);
            _coreRend = body.GetComponent<Renderer>();
            _coreRend.sharedMaterial = _sharedMat;
            TacticalMaterials.SetColor(_coreRend, RoleColor, 1.4f);

            Vector3[] armOffsets =
            {
                new Vector3(0.22f, 0.03f, 0.22f),
                new Vector3(-0.22f, 0.03f, 0.22f),
                new Vector3(0.22f, 0.03f, -0.22f),
                new Vector3(-0.22f, 0.03f, -0.22f)
            };

            for (int i = 0; i < 4; i++)
            {
                var rotor = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                rotor.name = $"Rotor_{i}";
                rotor.transform.SetParent(transform, false);
                rotor.transform.localPosition = armOffsets[i];
                rotor.transform.localScale = new Vector3(0.18f, 0.01f, 0.18f);
                Object.Destroy(rotor.GetComponent<Collider>());
                var rr = rotor.GetComponent<Renderer>();
                rr.sharedMaterial = _sharedMat;
                TacticalMaterials.SetColor(rr, new Color(0.8f, 0.88f, 0.96f), 0.4f);
                _rotors[i] = rotor.transform;
            }

            if (Role == DroneRole.AegisShield)
            {
                _shieldWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                _shieldWall.name = "Aegis_Barrier";
                _shieldWall.transform.SetParent(transform, false);
                _shieldWall.transform.localPosition = new Vector3(0, -0.25f, 0.45f);
                _shieldWall.transform.localScale = new Vector3(1.55f, 1.35f, 0.06f);
                Object.Destroy(_shieldWall.GetComponent<Collider>());
                var sr = _shieldWall.GetComponent<Renderer>();
                sr.sharedMaterial = _sharedMat;
                TacticalMaterials.SetColor(sr, new Color(0.22f, 0.65f, 1.0f), 1.8f);
            }

            _actionBeam = gameObject.AddComponent<LineRenderer>();
            _actionBeam.positionCount = 2;
            _actionBeam.startWidth = 0.035f;
            _actionBeam.endWidth = 0.015f;
            _actionBeam.material = _sharedMat;
            _actionBeam.enabled = false;
        }

        private void Update()
        {
            for (int i = 0; i < _rotors.Length; i++)
            {
                if (_rotors[i] != null) _rotors[i].Rotate(Vector3.up, 1400f * Time.deltaTime);
            }

            Vector3 desiredPos = ComputeDesiredPosition();
            desiredPos.y += Mathf.Sin(Time.time * 3f + FormationIndex) * 0.06f;
            transform.position = Vector3.Lerp(transform.position, desiredPos, Time.deltaTime * MoveSpeed);

            if (CurrentThreat != null && !CurrentThreat.IsNeutralized)
            {
                Vector3 lookDir = (CurrentThreat.transform.position + Vector3.up * 1.2f) - transform.position;
                if (lookDir.sqrMagnitude > 0.01f)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 8f);
                }
            }

            ExecuteRoleAbility();
        }

        private Vector3 ComputeDesiredPosition()
        {
            Vector3 playerPos = PlayerTransform != null ? PlayerTransform.position : Vector3.zero;
            bool hasThreat = CurrentThreat != null && !CurrentThreat.IsNeutralized;

            if (CurrentOrder == DroneOrder.GoToPoint)
            {
                return CommandPoint + new Vector3(0, 1.8f, 0);
            }

            if (CurrentOrder == DroneOrder.FollowPlayer || !hasThreat)
            {
                float angle = (FormationIndex - 1) * 55f * Mathf.Deg2Rad;
                return playerPos + new Vector3(Mathf.Sin(angle) * 1.8f, 1.9f, Mathf.Cos(angle) * 1.8f);
            }

            Vector3 threatPos = CurrentThreat.transform.position;
            Vector3 dirToThreat = (threatPos - playerPos).normalized;

            if (CurrentOrder == DroneOrder.GuardPerimeter || Role == DroneRole.AegisShield)
            {
                // Aegis intercepts directly between Player and Threat
                float shieldDist = Role == DroneRole.AegisShield ? 2.4f : 2.8f;
                Vector3 side = Vector3.Cross(Vector3.up, dirToThreat) * ((FormationIndex - 1) * 0.9f);
                return playerPos + dirToThreat * shieldDist + side + new Vector3(0, 1.45f, 0);
            }

            // OrbitAndEngage around Threat
            float orbitAngle = Time.time * 1.15f + FormationIndex * 2.1f;
            float orbitRadius = Role == DroneRole.ReconTracker ? 3.4f : 2.6f;
            float height = Role == DroneRole.ReconTracker ? 2.8f : 2.0f;
            return threatPos + new Vector3(Mathf.Cos(orbitAngle) * orbitRadius, height, Mathf.Sin(orbitAngle) * orbitRadius);
        }

        private void ExecuteRoleAbility()
        {
            bool hasThreat = CurrentThreat != null && !CurrentThreat.IsNeutralized;

            if (Role == DroneRole.AegisShield && _shieldWall != null)
            {
                IsShieldActive = hasThreat && CurrentOrder != DroneOrder.FollowPlayer;
                _shieldWall.SetActive(IsShieldActive);
            }

            if (!hasThreat || CurrentOrder == DroneOrder.FollowPlayer)
            {
                _actionBeam.enabled = false;
                if (Role == DroneRole.ReconTracker && CurrentThreat != null) CurrentThreat.IsMarkedByRecon = false;
                return;
            }

            if (Role == DroneRole.ReconTracker)
            {
                CurrentThreat.IsMarkedByRecon = true;
                _actionBeam.enabled = true;
                _actionBeam.SetPosition(0, transform.position);
                _actionBeam.SetPosition(1, CurrentThreat.transform.position + Vector3.up * 1.4f);
                TacticalMaterials.SetColor(_coreRend, RoleColor, 2.0f);
                return;
            }

            if (Role == DroneRole.PulseInterceptor)
            {
                _cooldownTimer -= Time.deltaTime;
                if (_cooldownTimer <= 0f)
                {
                    _cooldownTimer = ActionCooldown;
                    _beamFlashTimer = 0.18f;
                    CurrentThreat.ApplyStasisHit(18f, 0.35f);
                }

                if (_beamFlashTimer > 0f)
                {
                    _beamFlashTimer -= Time.deltaTime;
                    _actionBeam.enabled = true;
                    _actionBeam.SetPosition(0, transform.position);
                    _actionBeam.SetPosition(1, CurrentThreat.transform.position + Vector3.up * 1.1f);
                }
                else
                {
                    _actionBeam.enabled = false;
                }
            }
        }
    }
}
