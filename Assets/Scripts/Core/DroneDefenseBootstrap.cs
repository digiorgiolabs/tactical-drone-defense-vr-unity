using UnityEngine;
using UnityEngine.EventSystems;
using DiGiorgioLabs.DroneDefenseVR.Drones;
using DiGiorgioLabs.DroneDefenseVR.UI;

namespace DiGiorgioLabs.DroneDefenseVR.Core
{
    public class DroneDefenseBootstrap : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInit()
        {
            if (FindObjectOfType<DroneSwarmCommander>() != null) return;
            var root = new GameObject("DroneDefenseVR_App");
            root.AddComponent<DroneDefenseBootstrap>();
        }

        private Camera _cam;
        private Transform _playerRig;
        private float _yaw;
        private float _pitch = 8f;

        private void Awake()
        {
            BuildArena();
            SetupRigAndEventSystem();

            var commander = gameObject.AddComponent<DroneSwarmCommander>();
            commander.Initialize(_playerRig, _cam);

            var hud = gameObject.AddComponent<TacticalDroneHUD>();
            hud.Commander = commander;
        }

        private void BuildArena()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.14f, 0.17f, 0.24f);

            if (FindObjectOfType<Light>() == null)
            {
                var lGo = new GameObject("ArenaLight");
                var l = lGo.AddComponent<Light>();
                l.type = LightType.Directional;
                l.intensity = 1.2f;
                lGo.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
            }

            var mat = TacticalMaterials.CreateLitMaterial("ArenaMat");

            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "ArenaFloor";
            floor.transform.localScale = new Vector3(3f, 1f, 3f);
            floor.GetComponent<Renderer>().sharedMaterial = mat;
            TacticalMaterials.SetColor(floor.GetComponent<Renderer>(), new Color(0.06f, 0.09f, 0.14f), 0.04f);

            // Tactical cover pillars
            Vector3[] pillars =
            {
                new Vector3(-5f, 1.1f, 6f),
                new Vector3(5f, 1.1f, 6f),
                new Vector3(-6f, 1.1f, -3f),
                new Vector3(6f, 1.1f, -3f)
            };
            for (int i = 0; i < pillars.Length; i++)
            {
                var p = GameObject.CreatePrimitive(PrimitiveType.Cube);
                p.name = $"CoverPillar_{i}";
                p.transform.position = pillars[i];
                p.transform.localScale = new Vector3(1.2f, 2.2f, 1.2f);
                p.GetComponent<Renderer>().sharedMaterial = mat;
                TacticalMaterials.SetColor(p.GetComponent<Renderer>(), new Color(0.14f, 0.20f, 0.30f), 0.15f);
            }
        }

        private void SetupRigAndEventSystem()
        {
            _playerRig = new GameObject("PlayerSafeZone").transform;
            _playerRig.position = Vector3.zero;

            _cam = Camera.main;
            if (_cam == null)
            {
                var camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                _cam = camGo.AddComponent<Camera>();
                camGo.AddComponent<AudioListener>();
            }
            _cam.transform.SetParent(_playerRig, false);
            _cam.transform.localPosition = new Vector3(0, 1.65f, 0);
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.backgroundColor = new Color(0.02f, 0.04f, 0.08f);

            if (FindObjectOfType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }
        }

        private void Update()
        {
            if (_cam == null) return;
            if (Input.GetMouseButton(1))
            {
                _yaw += Input.GetAxis("Mouse X") * 2.5f;
                _pitch = Mathf.Clamp(_pitch - Input.GetAxis("Mouse Y") * 2.5f, -60f, 60f);
                _cam.transform.localRotation = Quaternion.Euler(_pitch, _yaw, 0f);
            }

            Vector3 move = Vector3.zero;
            if (Input.GetKey(KeyCode.W)) move += _cam.transform.forward;
            if (Input.GetKey(KeyCode.S)) move -= _cam.transform.forward;
            if (Input.GetKey(KeyCode.D)) move += _cam.transform.right;
            if (Input.GetKey(KeyCode.A)) move -= _cam.transform.right;
            move.y = 0f;
            _playerRig.position += move.normalized * (3.2f * Time.deltaTime);
        }
    }
}
