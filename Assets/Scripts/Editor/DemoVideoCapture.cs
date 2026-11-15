#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using DiGiorgioLabs.DroneDefenseVR.Core;
using DiGiorgioLabs.DroneDefenseVR.Drones;
using DiGiorgioLabs.DroneDefenseVR.Threat;

namespace DiGiorgioLabs.DroneDefenseVR.Editor
{
    public static class DemoVideoCapture
    {
        public static void CaptureDemo()
        {
            string outDir = "/tmp/frames_triwing";
            if (Directory.Exists(outDir)) Directory.Delete(outDir, true);
            Directory.CreateDirectory(outDir);

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var lGo = new GameObject("ArenaLight");
            var l = lGo.AddComponent<Light>();
            l.type = LightType.Directional;
            l.intensity = 1.25f;
            lGo.transform.rotation = Quaternion.Euler(50f, -35f, 0f);

            var mat = TacticalMaterials.CreateLitMaterial("ArenaMat");
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.transform.localScale = new Vector3(3f, 1f, 3f);
            floor.GetComponent<Renderer>().sharedMaterial = mat;
            TacticalMaterials.SetColor(floor.GetComponent<Renderer>(), new Color(0.06f, 0.09f, 0.14f), 0.04f);

            var player = new GameObject("PlayerSafeZone").transform;
            var threatGo = new GameObject("Threat");
            threatGo.transform.position = new Vector3(0.5f, 0f, 7.5f);
            var threat = threatGo.AddComponent<ThreatAgent>();
            threat.Initialize(player, mat, 1);

            var drones = new DroneUnit[3];
            DroneRole[] roles = { DroneRole.ReconTracker, DroneRole.AegisShield, DroneRole.PulseInterceptor };
            for (int i = 0; i < 3; i++)
            {
                var dGo = new GameObject($"Drone_{i}");
                dGo.transform.position = new Vector3((i - 1) * 1.5f, 1.8f, 3.0f);
                drones[i] = dGo.AddComponent<DroneUnit>();
                drones[i].Initialize(roles[i], i, player, mat);
                drones[i].CurrentThreat = threat;
                drones[i].CurrentOrder = DroneOrder.OrbitAndEngage;
            }

            var camGo = new GameObject("CaptureCam");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.04f, 0.08f);

            int width = 1280, height = 720;
            var rt = new RenderTexture(width, height, 24);
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            cam.targetTexture = rt;

            int totalFrames = 144; // 6s @ 24fps
            for (int f = 0; f < totalFrames; f++)
            {
                float u = (float)f / totalFrames;
                float t = f / 24f;

                // Animate threat advancing & drones orbiting/shielding
                threat.transform.position = new Vector3(Mathf.Sin(t * 1.1f) * 1.8f, 0f, Mathf.Lerp(7.8f, 4.2f, u));
                Vector3 tp = threat.transform.position;

                drones[0].transform.position = tp + new Vector3(Mathf.Cos(t * 1.8f) * 2.5f, 2.5f, Mathf.Sin(t * 1.8f) * 2.5f);
                drones[1].transform.position = Vector3.Lerp(player.position, tp, 0.45f) + new Vector3(0, 1.45f, 0);
                drones[2].transform.position = tp + new Vector3(Mathf.Cos(t * 2.4f + 2f) * 2.1f, 1.9f, Mathf.Sin(t * 2.4f + 2f) * 2.1f);
                foreach (var d in drones) d.transform.LookAt(tp + Vector3.up * 1.2f);

                float camAngle = Mathf.Lerp(-0.35f, 0.45f, u);
                cam.transform.position = tp + new Vector3(Mathf.Sin(camAngle) * 5.2f, 2.3f, -Mathf.Cos(camAngle) * 5.2f);
                cam.transform.LookAt(tp + Vector3.up * 1.15f);

                RenderTexture.active = rt;
                cam.Render();
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply();
                File.WriteAllBytes(Path.Combine(outDir, $"frame_{f:D4}.png"), tex.EncodeToPNG());
                RenderTexture.active = null;
            }

            cam.targetTexture = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);
            Debug.Log($"[DemoVideoCapture] Captured {totalFrames} frames to {outDir}");
        }
    }
}
#endif
