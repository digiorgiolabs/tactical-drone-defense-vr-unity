using UnityEngine;
using UnityEngine.UI;
using DiGiorgioLabs.DroneDefenseVR.Drones;

namespace DiGiorgioLabs.DroneDefenseVR.UI
{
    /// <summary>
    /// World-Space VR Tactical Command Console for directing the 3 Drones.
    /// </summary>
    public class TacticalDroneHUD : MonoBehaviour
    {
        public DroneSwarmCommander Commander;

        private Text _statusText;
        private Text _selectionText;
        private Text _empText;

        private void Start()
        {
            BuildHUD();
        }

        private void Update()
        {
            if (Commander == null || _statusText == null) return;

            float threatPct = Commander.ActiveThreat != null
                ? (Commander.ActiveThreat.CurrentResolve / Commander.ActiveThreat.MaxResolve) * 100f
                : 0f;

            string shieldState = (Commander.Drones.Count > 1 && Commander.Drones[1].IsShieldActive) ? "ACTIVE (80% Block)" : "STANDBY";
            _statusText.text =
                $"<b>WAVE {Commander.Wave}</b>  |  Neutralized: <b>{Commander.ThreatsNeutralized}</b>\n" +
                $"Safe-Zone Integrity: <color=#38BDF8>{Commander.PlayerIntegrity:F0}%</color>  |  Aegis Shield: <color=#34D399>{shieldState}</color>\n" +
                $"Threat Resolve: <color=#F87171>{threatPct:F0}%</color>";

            string sel = Commander.SelectedDroneIndex switch
            {
                0 => "ALPHA-RECON (Laser Mark)",
                1 => "BETA-AEGIS (Shield Wall)",
                2 => "GAMMA-PULSE (Stasis Fire)",
                _ => "ALL 3 DRONES (Swarm)"
            };
            _selectionText.text = $"Active Selection: <color=#FBBF24>{sel}</color>\nClick Arena Floor to Send Drones | Click Threat to Engage";

            _empText.text = Commander.EmpBurstCooldown > 0f
                ? $"EMP Burst ({Commander.EmpBurstCooldown:F1}s)"
                : "EMP Burst READY";
        }

        private void BuildHUD()
        {
            var canvasGo = new GameObject("TacticalHUD_Canvas");
            canvasGo.transform.SetParent(transform, false);
            canvasGo.transform.localPosition = new Vector3(-1.35f, 1.30f, 1.65f);
            canvasGo.transform.localRotation = Quaternion.Euler(0f, 30f, 0f);
            canvasGo.transform.localScale = Vector3.one * 0.0014f;

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasGo.GetComponent<RectTransform>().sizeDelta = new Vector2(780f, 580f);
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();

            var bg = CreatePanel(canvasGo.transform, "BG", Vector2.zero, new Vector2(780f, 580f), new Color(0.04f, 0.07f, 0.12f, 0.92f));

            var title = CreateText(bg.transform, "Title", new Vector2(0, 245), new Vector2(730, 42), 26, FontStyle.Bold, new Color(0.22f, 0.85f, 1f));
            title.text = "TACTICAL DRONE COMMAND (3-UNIT SWARM)";

            _statusText = CreateText(bg.transform, "Status", new Vector2(0, 170), new Vector2(730, 95), 20, FontStyle.Normal, Color.white);
            _selectionText = CreateText(bg.transform, "Selection", new Vector2(0, 88), new Vector2(730, 56), 17, FontStyle.Normal, new Color(0.82f, 0.90f, 1f));

            // Drone Selection Row
            CreateButton(bg.transform, "SelAll", "ALL 3\nDRONES", new Vector2(-270, 5), new Vector2(165, 64), () => Commander.SelectedDroneIndex = -1);
            CreateButton(bg.transform, "Sel1", "1: ALPHA\nRECON", new Vector2(-90, 5), new Vector2(165, 64), () => Commander.SelectedDroneIndex = 0);
            CreateButton(bg.transform, "Sel2", "2: BETA\nAEGIS", new Vector2(90, 5), new Vector2(165, 64), () => Commander.SelectedDroneIndex = 1);
            CreateButton(bg.transform, "Sel3", "3: GAMMA\nPULSE", new Vector2(270, 5), new Vector2(165, 64), () => Commander.SelectedDroneIndex = 2);

            // Command Orders Row
            CreateButton(bg.transform, "OrdEngage", "ORBIT &\nENGAGE", new Vector2(-240, -85), new Vector2(220, 64), () => Commander.IssueOrder(DroneOrder.OrbitAndEngage));
            CreateButton(bg.transform, "OrdGuard", "GUARD\nPERIMETER", new Vector2(0, -85), new Vector2(220, 64), () => Commander.IssueOrder(DroneOrder.GuardPerimeter));
            CreateButton(bg.transform, "OrdFollow", "FOLLOW /\nRECALL", new Vector2(240, -85), new Vector2(220, 64), () => Commander.IssueOrder(DroneOrder.FollowPlayer));

            // Special Ability & Reset
            var empBtn = CreateButton(bg.transform, "BtnEMP", "EMP Burst READY", new Vector2(-130, -185), new Vector2(300, 64), () => Commander.TriggerEmpBurst());
            _empText = empBtn.GetComponentInChildren<Text>();

            CreateButton(bg.transform, "BtnReset", "RESET ARENA", new Vector2(180, -185), new Vector2(220, 64), () => Commander.ResetSimulation());
        }

        private static GameObject CreatePanel(Transform parent, string name, Vector2 pos, Vector2 size, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<Image>().color = color;
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return go;
        }

        private static Text CreateText(Transform parent, string name, Vector2 pos, Vector2 size, int fontSize, FontStyle style, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            t.fontSize = fontSize;
            t.fontStyle = style;
            t.color = color;
            t.alignment = TextAnchor.MiddleCenter;
            t.supportRichText = true;
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return t;
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 pos, Vector2 size, UnityEngine.Events.UnityAction onClick)
        {
            var go = CreatePanel(parent, name, pos, size, new Color(0.14f, 0.25f, 0.45f, 1f));
            var btn = go.AddComponent<Button>();
            btn.onClick.AddListener(onClick);
            CreateText(go.transform, "Label", Vector2.zero, size - new Vector2(10, 8), 18, FontStyle.Bold, Color.white).text = label;
            return btn;
        }
    }
}
