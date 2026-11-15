using UnityEngine;
using DiGiorgioLabs.DroneDefenseVR.Core;

namespace DiGiorgioLabs.DroneDefenseVR.Threat
{
    /// <summary>
    /// Hostile intruder advancing toward the player's safe zone, flanking and telegraphing strikes.
    /// </summary>
    public class ThreatAgent : MonoBehaviour
    {
        public float MaxResolve = 100f;
        public float CurrentResolve { get; private set; }
        public float MoveSpeed = 1.85f;
        public float AttackRange = 6.5f;
        public float AttackInterval = 2.8f;
        public bool IsNeutralized { get; private set; }
        public bool IsMarkedByRecon { get; set; }

        public Transform PlayerTarget;
        public System.Action<float> OnPlayerAttacked;
        public System.Action<ThreatAgent> OnNeutralized;

        private float _attackTimer;
        private float _staggerTimer;
        private float _flankSign = 1f;
        private float _flankSwitchTimer;
        private Renderer _bodyRend;
        private Renderer _visorRend;
        private LineRenderer _telegraphLine;
        private Material _sharedMat;

        public void Initialize(Transform playerTarget, Material sharedMat, int wave)
        {
            PlayerTarget = playerTarget;
            _sharedMat = sharedMat;
            MaxResolve = 100f + (wave - 1) * 30f;
            CurrentResolve = MaxResolve;
            MoveSpeed = 1.7f + (wave - 1) * 0.15f;
            IsNeutralized = false;
            _attackTimer = 0f;
            _flankSign = Random.value > 0.5f ? 1f : -1f;

            BuildVisuals();
        }

        private void BuildVisuals()
        {
            // Stylized tactical humanoid figure
            var torso = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            torso.name = "Threat_Torso";
            torso.transform.SetParent(transform, false);
            torso.transform.localPosition = new Vector3(0, 1.0f, 0);
            torso.transform.localScale = new Vector3(0.55f, 0.7f, 0.45f);
            _bodyRend = torso.GetComponent<Renderer>();
            _bodyRend.sharedMaterial = _sharedMat;
            TacticalMaterials.SetColor(_bodyRend, new Color(0.28f, 0.12f, 0.16f), 0.4f);

            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Threat_Head";
            head.transform.SetParent(transform, false);
            head.transform.localPosition = new Vector3(0, 1.82f, 0);
            head.transform.localScale = Vector3.one * 0.34f;
            var hRend = head.GetComponent<Renderer>();
            hRend.sharedMaterial = _sharedMat;
            TacticalMaterials.SetColor(hRend, new Color(0.22f, 0.12f, 0.16f), 0.3f);

            var visor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visor.name = "Threat_Visor";
            visor.transform.SetParent(head.transform, false);
            visor.transform.localPosition = new Vector3(0, 0.05f, 0.42f);
            visor.transform.localScale = new Vector3(0.68f, 0.25f, 0.35f);
            Object.Destroy(visor.GetComponent<Collider>());
            _visorRend = visor.GetComponent<Renderer>();
            _visorRend.sharedMaterial = _sharedMat;
            TacticalMaterials.SetColor(_visorRend, new Color(1.0f, 0.18f, 0.18f), 2.2f);

            _telegraphLine = gameObject.AddComponent<LineRenderer>();
            _telegraphLine.positionCount = 2;
            _telegraphLine.startWidth = 0.025f;
            _telegraphLine.endWidth = 0.015f;
            _telegraphLine.material = _sharedMat;
            _telegraphLine.enabled = false;
        }

        private void Update()
        {
            if (IsNeutralized || PlayerTarget == null) return;

            if (_staggerTimer > 0f)
            {
                _staggerTimer -= Time.deltaTime;
                _telegraphLine.enabled = false;
                TacticalMaterials.SetColor(_bodyRend, new Color(0.4f, 0.7f, 1.0f), 1.4f);
                return;
            }

            TacticalMaterials.SetColor(_bodyRend, IsMarkedByRecon ? new Color(0.75f, 0.20f, 0.22f) : new Color(0.28f, 0.12f, 0.16f), IsMarkedByRecon ? 1.1f : 0.4f);

            Vector3 toPlayer = PlayerTarget.position - transform.position;
            toPlayer.y = 0f;
            float dist = toPlayer.magnitude;
            Vector3 dir = dist > 0.01f ? toPlayer / dist : Vector3.forward;

            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 6f);

            _flankSwitchTimer -= Time.deltaTime;
            if (_flankSwitchTimer <= 0f)
            {
                _flankSign = -_flankSign;
                _flankSwitchTimer = Random.Range(2.0f, 4.0f);
            }

            // Advance toward safe-zone perimeter while strafing
            if (dist > 3.2f)
            {
                Vector3 strafe = Vector3.Cross(Vector3.up, dir) * _flankSign * 0.55f;
                Vector3 step = (dir + strafe).normalized * MoveSpeed * Time.deltaTime;
                transform.position += step;
            }

            // Telegraph and fire strike when in range
            if (dist <= AttackRange)
            {
                _attackTimer += Time.deltaTime;
                if (_attackTimer >= AttackInterval - 0.9f)
                {
                    _telegraphLine.enabled = true;
                    _telegraphLine.SetPosition(0, transform.position + new Vector3(0, 1.55f, 0));
                    _telegraphLine.SetPosition(1, PlayerTarget.position + new Vector3(0, 1.25f, 0));
                    TacticalMaterials.SetColor(_visorRend, new Color(1.0f, 0.1f, 0.1f), 3.5f);
                }
                if (_attackTimer >= AttackInterval)
                {
                    _attackTimer = 0f;
                    _telegraphLine.enabled = false;
                    OnPlayerAttacked?.Invoke(14f);
                }
            }
            else
            {
                _telegraphLine.enabled = false;
            }
        }

        public void ApplyStasisHit(float amount, float staggerSeconds)
        {
            if (IsNeutralized) return;

            float mult = IsMarkedByRecon ? 1.4f : 1.0f;
            CurrentResolve = Mathf.Max(0f, CurrentResolve - amount * mult);
            _staggerTimer = Mathf.Max(_staggerTimer, staggerSeconds);

            if (CurrentResolve <= 0f)
            {
                IsNeutralized = true;
                _telegraphLine.enabled = false;
                TacticalMaterials.SetColor(_bodyRend, new Color(0.15f, 0.25f, 0.35f), 0.2f);
                TacticalMaterials.SetColor(_visorRend, Color.black, 0f);
                transform.rotation = Quaternion.Euler(85f, transform.eulerAngles.y, 0f);
                OnNeutralized?.Invoke(this);
            }
        }
    }
}
