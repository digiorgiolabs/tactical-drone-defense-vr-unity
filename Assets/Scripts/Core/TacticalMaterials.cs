using UnityEngine;

namespace DiGiorgioLabs.DroneDefenseVR.Core
{
    public static class TacticalMaterials
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly MaterialPropertyBlock Block = new MaterialPropertyBlock();

        public static Material CreateLitMaterial(string name)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                            ?? Shader.Find("Standard")
                            ?? Shader.Find("Unlit/Color");
            var mat = new Material(shader) { name = name };
            mat.EnableKeyword("_EMISSION");
            mat.SetFloat("_Smoothness", 0.6f);
            return mat;
        }

        public static void SetColor(Renderer renderer, Color color, float emission = 0.5f)
        {
            if (renderer == null) return;
            renderer.GetPropertyBlock(Block);
            Block.SetColor(BaseColorId, color);
            Block.SetColor(ColorId, color);
            Block.SetColor(EmissionColorId, color * emission);
            renderer.SetPropertyBlock(Block);
        }
    }
}
