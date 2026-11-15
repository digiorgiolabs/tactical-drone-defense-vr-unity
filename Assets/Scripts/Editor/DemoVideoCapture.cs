#if UNITY_EDITOR
using System.Collections.Generic;
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
            l.intensity = 1.35f;
            l.color = new Color(0.92f, 0.96f, 1.0f);
            lGo.transform.rotation = Quaternion.Euler(48f, -32f, 0f);

            var fillGo = new GameObject("FillLight");
            var fill = fillGo.AddComponent<Light>();
            fill.type = LightType.Point;
            fill.range = 25f;
            fill.intensity = 1.4f;
            fill.color = new Color(0.25f, 0.65f, 1.0f);
            fillGo.transform.position = new Vector3(0, 5f, 3f);

            var mat = TacticalMaterials.CreateLitMaterial("ArenaMat");

            // Dark tactical arena floor
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.transform.localScale = new Vector3(3.5f, 1f, 3.5f);
            floor.GetComponent<Renderer>().sharedMaterial = mat;
            TacticalMaterials.SetColor(floor.GetComponent<Renderer>(), new Color(0.03f, 0.05f, 0.09f), 0.02f);

            // Safe-zone perimeter ring (24 segments)
            for (int s = 0; s < 24; s++)
            {
                float a0 = (s / 24f) * Mathf.PI * 2f;
                float a1 = ((s + 1) / 24f) * Mathf.PI * 2f;
                Vector3 p0 = new Vector3(Mathf.Cos(a0) * 3.0f, 0.03f, Mathf.Sin(a0) * 3.0f);
                Vector3 p1 = new Vector3(Mathf.Cos(a1) * 3.0f, 0.03f, Mathf.Sin(a1) * 3.0f);
                var seg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                seg.transform.position = (p0 + p1) * 0.5f;
                seg.transform.rotation = Quaternion.FromToRotation(Vector3.up, (p1 - p0).normalized);
                seg.transform.localScale = new Vector3(0.04f, Vector3.Distance(p0, p1) * 0.5f, 0.04f);
                seg.GetComponent<Renderer>().sharedMaterial = mat;
                TacticalMaterials.SetColor(seg.GetComponent<Renderer>(), new Color(0.15f, 0.85f, 1.0f), 1.8f);
            }

            // Cover pillars with glowing trim
            Vector3[] pillars =
            {
                new Vector3(-3.8f, 1.1f, 5.2f),
                new Vector3(3.8f, 1.1f, 5.2f),
                new Vector3(-5.2f, 1.1f, 1.2f),
                new Vector3(5.2f, 1.1f, 1.2f)
            };
            foreach (var pPos in pillars)
            {
                var p = GameObject.CreatePrimitive(PrimitiveType.Cube);
                p.transform.position = pPos;
                p.transform.localScale = new Vector3(1.1f, 2.2f, 1.1f);
                p.GetComponent<Renderer>().sharedMaterial = mat;
                TacticalMaterials.SetColor(p.GetComponent<Renderer>(), new Color(0.10f, 0.15f, 0.24f), 0.15f);

                var trim = GameObject.CreatePrimitive(PrimitiveType.Cube);
                trim.transform.position = pPos + new Vector3(0, 1.05f, 0);
                trim.transform.localScale = new Vector3(1.14f, 0.08f, 1.14f);
                trim.GetComponent<Renderer>().sharedMaterial = mat;
                TacticalMaterials.SetColor(trim.GetComponent<Renderer>(), new Color(0.18f, 0.75f, 1.0f), 1.6f);
            }

            var player = new GameObject("PlayerSafeZone").transform;

            var threatGo = new GameObject("Threat");
            threatGo.transform.position = new Vector3(0.8f, 0f, 8.5f);
            var threat = threatGo.AddComponent<ThreatAgent>();
            threat.Initialize(player, mat, 1);

            var drones = new DroneUnit[3];
            DroneRole[] roles = { DroneRole.ReconTracker, DroneRole.AegisShield, DroneRole.PulseInterceptor };
            for (int i = 0; i < 3; i++)
            {
                var dGo = new GameObject($"Drone_{i}");
                dGo.transform.position = new Vector3((i - 1) * 1.5f, 1.8f, 2.5f);
                drones[i] = dGo.AddComponent<DroneUnit>();
                drones[i].Initialize(roles[i], i, player, mat);
                drones[i].CurrentThreat = threat;
                drones[i].CurrentOrder = DroneOrder.OrbitAndEngage;
            }

            // Visual laser beams + EMP bolt + EMP shockwave ring
            var reconBeam = CreateCylinderBeam(mat, new Color(0.20f, 0.90f, 1.0f), 0.022f, 2.2f);
            var threatBeam = CreateCylinderBeam(mat, new Color(1.0f, 0.20f, 0.20f), 0.025f, 2.5f);
            var empBolt = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            empBolt.transform.localScale = Vector3.one * 0.16f;
            empBolt.GetComponent<Renderer>().sharedMaterial = mat;
            TacticalMaterials.SetColor(empBolt.GetComponent<Renderer>(), new Color(0.22f, 1.0f, 0.65f), 2.8f);

            var shockRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shockRing.GetComponent<Renderer>().sharedMaterial = mat;
            TacticalMaterials.SetColor(shockRing.GetComponent<Renderer>(), new Color(0.25f, 0.95f, 0.72f), 2.4f);

            var camGo = new GameObject("CaptureCam");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.03f, 0.07f);

            int width = 1280, height = 720;
            var rt = new RenderTexture(width, height, 24);
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            cam.targetTexture = rt;

            int totalFrames = 192; // 8s @ 24fps
            for (int f = 0; f < totalFrames; f++)
            {
                float u = (float)f / totalFrames;
                float t = f / 24f;

                // Threat weaves between cover pillars toward safe zone
                Vector3 tp = new Vector3(Mathf.Sin(t * 1.15f) * 2.2f, 0f, Mathf.Lerp(8.8f, 4.4f, u));
                threat.transform.position = tp;
                threat.transform.LookAt(player.position);

                // Drone 0: ALPHA-RECON high orbit + cyan laser lock
                drones[0].transform.position = tp + new Vector3(Mathf.Cos(t * 1.6f) * 2.4f, 2.6f, Mathf.Sin(t * 1.6f) * 2.4f);
                drones[0].transform.LookAt(tp + Vector3.up * 1.3f);
                UpdateBeam(reconBeam.transform, drones[0].transform.position, tp + Vector3.up * 1.55f, 0.018f);

                // Drone 1: BETA-AEGIS intercepts directly between player & threat
                Vector3 aegisPos = Vector3.Lerp(player.position, tp, 0.42f) + new Vector3(0f, 1.45f, 0f);
                drones[1].transform.position = aegisPos;
                drones[1].transform.LookAt(tp + Vector3.up * 1.3f);

                // Threat laser hits Aegis shield during middle frames
                bool threatFiring = (f % 60) > 25 && (f % 60) < 50;
                threatBeam.SetActive(threatFiring);
                if (threatFiring)
                {
                    UpdateBeam(threatBeam.transform, tp + Vector3.up * 1.55f, aegisPos + new Vector3(0, -0.15f, 0), 0.022f);
                }

                // Drone 2: GAMMA-PULSE fires emerald EMP bolts
                drones[2].transform.position = tp + new Vector3(Mathf.Cos(t * 2.2f + 2.1f) * 2.0f, 1.85f, Mathf.Sin(t * 2.2f + 2.1f) * 2.0f);
                drones[2].transform.LookAt(tp + Vector3.up * 1.2f);
                float boltPhase = (t * 1.6f) % 1f;
                empBolt.transform.position = Vector3.Lerp(drones[2].transform.position, tp + Vector3.up * 1.2f, boltPhase);

                // Expanding EMP shockwave at 5.5s
                if (f >= 132 && f < 168)
                {
                    shockRing.SetActive(true);
                    float sp = (f - 132) / 36f;
                    float rad = Mathf.Lerp(0.4f, 4.5f, sp);
                    shockRing.transform.position = tp + Vector3.up * 0.15f;
                    shockRing.transform.localScale = new Vector3(rad, 0.04f, rad);
                }
                else
                {
                    shockRing.SetActive(false);
                }

                // Over-the-shoulder tactical VR perspective looking past safe-zone ring & Aegis shield
                float camAngle = Mathf.Lerp(-0.55f, 0.55f, u);
                cam.transform.position = new Vector3(Mathf.Sin(camAngle) * 3.8f, 2.35f, -1.6f);
                cam.transform.LookAt(Vector3.Lerp(aegisPos, tp + Vector3.up * 1.1f, 0.65f));

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

        private static GameObject CreateCylinderBeam(Material mat, Color color, float radius, float emission)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.GetComponent<Renderer>().sharedMaterial = mat;
            TacticalMaterials.SetColor(go.GetComponent<Renderer>(), color, emission);
            return go;
        }

        private static void UpdateBeam(Transform beam, Vector3 a, Vector3 b, float radius)
        {
            Vector3 d = b - a;
            beam.position = (a + b) * 0.5f;
            beam.rotation = Quaternion.FromToRotation(Vector3.up, d.normalized);
            beam.localScale = new Vector3(radius, d.magnitude * 0.5f, radius);
        }
    }
}
#endif
