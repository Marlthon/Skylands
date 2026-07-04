using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Skylands
{
    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    public class BiomeControl : MonoBehaviour, Hoverable, Interactable
    {
        [Header("Island Control")]
        public GameObject ilhaPai; // ainda arrastado no Inspector
        public List<Material> materiaisDoBioma;

        [Header("Trees Groups (0 = On, 1 = Off)")]
        public List<GameObject> ArvoresObjects;

        [Header("WaterWell Groups (0 = On, 1 = Off)")]
        public List<GameObject> WaterWellObjects;

        [Header("LampPosts Groups (0 = On, 1 = Off)")]
        public List<GameObject> LampPostsObjects;

        [HideInInspector] public string TreeToggleKey = "H";
        [HideInInspector] public string WaterWellToggleKey = "J";
        [HideInInspector] public string LampPostsToggleKey = "L";
        [HideInInspector] public string TextureToggleKey = "K";

        public ZoneShaderManager zoneManager; // ligação com ZoneShaderManager

        private ZNetView m_nview;
        private bool m_initialized = false;
        private bool m_inCustomization = false;
        public bool IsInCustomization => m_inCustomization;

        private const string ZDO_MATERIAL_INDEX = "biome_material_index";
        private const string ZDO_TREES_INDEX = "biome_trees_index";
        private const string ZDO_WATERWELL_INDEX = "biome_waterwell_index";
        private const string ZDO_LAMPPOSTS_INDEX = "biome_lampposts_index";

        private void Awake()
        {
            // Agora ele pega automaticamente o ZNetView do pai
            m_nview = GetComponentInParent<ZNetView>();

            if (m_nview != null)
            {
                m_nview.Register("RPC_OnBiomeChanged", (long sender) => UpdateVisuals());
            }
        }

        private void Start()
        {
            StartCoroutine(DelayedSetup());
        }

        private ZoneShaderManager FindZoneManager()
        {
            if (zoneManager != null) return zoneManager;

            zoneManager = GetComponentInChildren<ZoneShaderManager>(true);
            if (zoneManager != null) return zoneManager;

            zoneManager = GetComponentInParent<ZoneShaderManager>();
            return zoneManager;
        }

        private IEnumerator DelayedSetup()
        {
            while (m_nview == null || !m_nview.IsValid() || m_nview.GetZDO() == null)
            {
                yield return new WaitForSeconds(0.1f);
            }
            yield return new WaitForSeconds(0.2f);

            FindZoneManager();
            m_initialized = true;
            UpdateVisuals();
        }

        public void UpdateVisuals()
        {
            if (!m_initialized || m_nview == null || !m_nview.IsValid() || m_nview.GetZDO() == null)
                return;

            var zdo = m_nview.GetZDO();

            ApplyGroup(ArvoresObjects, zdo.GetInt(ZDO_TREES_INDEX, 0));
            ApplyGroup(WaterWellObjects, zdo.GetInt(ZDO_WATERWELL_INDEX, 0));
            ApplyGroup(LampPostsObjects, zdo.GetInt(ZDO_LAMPPOSTS_INDEX, 0));

            if (ilhaPai != null && materiaisDoBioma != null && materiaisDoBioma.Count > 0)
            {
                int biomeIndex = zdo.GetInt(ZDO_MATERIAL_INDEX, 0);
                if (biomeIndex < materiaisDoBioma.Count)
                {
                    Material material = materiaisDoBioma[biomeIndex];

                    foreach (var renderer in ilhaPai.GetComponentsInChildren<MeshRenderer>(true))
                    {
                        renderer.material = material;
                    }

                    var zsm = FindZoneManager();
                    if (zsm != null)
                    {
                        bool isSnow = material != null &&
                                      material.name.StartsWith("Snow", StringComparison.OrdinalIgnoreCase);

                        zsm.allowSnow = isSnow;
                        zsm.ApplySnowState();
                    }
                }
            }
        }

        private void ApplyGroup(List<GameObject> group, int index)
        {
            if (group == null || group.Count == 0) return;
            for (int i = 0; i < group.Count; i++)
            {
                if (group[i] != null)
                    group[i].SetActive(i == index);
            }
        }

        public void ForceUpdate()
        {
            if (m_initialized)
            {
                UpdateVisuals();
            }
        }

        #region Hover/Interact
        public string GetHoverName() => "Island Control";

        public string GetHoverText()
        {
            if (!m_initialized || m_nview == null || !m_nview.IsValid())
                return "Loading...";

            if (!m_inCustomization)
            {
                return Localization.instance.Localize("[<color=yellow><b>E</b></color>] Customize Island");
            }

            string texto = "<color=yellow>- Island Customization -</color>";

            var zdo = m_nview.GetZDO();

            if (ArvoresObjects != null && ArvoresObjects.Count > 1)
            {
                int treesIndex = zdo.GetInt(ZDO_TREES_INDEX, 0);
                texto += $"\n[{TreeToggleKey}] Trees ({(treesIndex == 0 ? "On" : "Off")})";
            }

            if (WaterWellObjects != null && WaterWellObjects.Count > 1)
            {
                int waterWellIndex = zdo.GetInt(ZDO_WATERWELL_INDEX, 0);
                texto += $"\n[{WaterWellToggleKey}] WaterWell ({(waterWellIndex == 0 ? "On" : "Off")})";
            }

            if (LampPostsObjects != null && LampPostsObjects.Count > 1)
            {
                int lampPostsIndex = zdo.GetInt(ZDO_LAMPPOSTS_INDEX, 0);
                texto += $"\n[{LampPostsToggleKey}] LampPosts ({(lampPostsIndex == 0 ? "On" : "Off")})";
            }

            if (materiaisDoBioma != null && materiaisDoBioma.Count > 0)
            {
                int currentIndex = zdo.GetInt(ZDO_MATERIAL_INDEX, 0);
                string currentName = materiaisDoBioma[currentIndex].name;
                texto += $"\n[{TextureToggleKey}] Texture ({currentName})";
            }

            return Localization.instance.Localize(texto);
        }

        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            if (!hold)
            {
                m_inCustomization = !m_inCustomization;
                user.Message(MessageHud.MessageType.Center,
                    m_inCustomization ? "Customization Enabled" : "Customization Disabled");
                return true;
            }
            return false;
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;
        #endregion
    }
}
