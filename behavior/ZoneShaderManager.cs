using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Skylands
{
    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    [RequireComponent(typeof(BoxCollider))]
    public class ZoneShaderManager : MonoBehaviour
    {
        [Tooltip("Shader sem neve (Custom_Piece.shader).")]
        public Shader noSnowPieceShader;

        [Header("Extra")]
        [Tooltip("Objeto VFX de neve caindo que será ativado quando a neve estiver habilitada.")]
        public GameObject snowVFX;

        [HideInInspector] public bool allowSnow = false;

        private const string PIECE_SHADER_NAME = "Custom/Piece";
        private const string SNOW_PROPERTY_NAME = "_AddSnow";

        private BoxCollider zoneCollider;

        private readonly HashSet<Renderer> renderersInZone = new HashSet<Renderer>();
        private readonly Dictionary<Material, Shader> originalShaders = new Dictionary<Material, Shader>();

        private static readonly List<ZoneShaderManager> allZones = new List<ZoneShaderManager>();

        private void Awake()
        {
            zoneCollider = GetComponent<BoxCollider>();
            zoneCollider.isTrigger = true;

            allZones.Add(this);
            RegisterRenderersInZone(); // pega objetos já existentes
        }

        private void OnDestroy()
        {
            allZones.Remove(this);
        }

        private void RegisterRenderersInZone()
        {
            Vector3 worldCenter = transform.TransformPoint(zoneCollider.center);
            Vector3 worldSize = Vector3.Scale(zoneCollider.size, transform.lossyScale);
            Collider[] colliders = Physics.OverlapBox(worldCenter, worldSize * 0.5f, transform.rotation);

            foreach (var col in colliders)
            {
                if (col == null) continue;
                Renderer[] rs = col.GetComponentsInChildren<Renderer>(true);
                RegisterRenderers(rs);
            }
        }

        public void RegisterRenderers(Renderer[] rs)
        {
            foreach (var r in rs)
            {
                if (r == null) continue;
                if (renderersInZone.Add(r))
                {
                    foreach (var mat in r.materials)
                    {
                        if (mat != null && !originalShaders.ContainsKey(mat))
                        {
                            originalShaders[mat] = mat.shader;
                        }
                    }
                }
            }
        }

        public void ApplySnowState()
        {
            foreach (var r in renderersInZone)
            {
                if (r == null) continue;
                foreach (var mat in r.materials)
                {
                    if (mat == null) continue;

                    if (originalShaders.TryGetValue(mat, out var originalShader) && originalShader != null)
                    {
                        if (originalShader.name == PIECE_SHADER_NAME)
                        {
                            if (!allowSnow && noSnowPieceShader != null)
                            {
                                mat.shader = noSnowPieceShader;
                            }
                            else
                            {
                                mat.shader = originalShader;
                            }
                        }
                    }

                    if (mat.HasProperty(SNOW_PROPERTY_NAME))
                    {
                        mat.SetFloat(SNOW_PROPERTY_NAME, allowSnow ? 1f : 0f);
                    }
                }
            }

            // >>> Ativar/Desativar VFX de neve
            if (snowVFX != null)
            {
                snowVFX.SetActive(allowSnow);
            }
        }

        public bool IsInsideZone(Vector3 pos)
        {
            Vector3 worldCenter = transform.TransformPoint(zoneCollider.center);
            Vector3 halfSize = Vector3.Scale(zoneCollider.size, transform.lossyScale) * 0.5f;
            Bounds b = new Bounds(worldCenter, halfSize * 2f);
            return b.Contains(pos);
        }

        [Obfuscation(Exclude = true, ApplyToMembers = true)]
        [HarmonyPatch(typeof(Piece), "Awake")]
        private static class Piece_Awake_Patch
        {
            static void Postfix(Piece __instance)
            {
                if (__instance == null) return;
                Renderer[] rs = __instance.GetComponentsInChildren<Renderer>(true);
                Vector3 pos = __instance.transform.position;

                foreach (var zsm in allZones)
                {
                    if (zsm != null && zsm.IsInsideZone(pos))
                    {
                        zsm.RegisterRenderers(rs);
                        zsm.ApplySnowState();
                    }
                }
            }
        }
    }
}
