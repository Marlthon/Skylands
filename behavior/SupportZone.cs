using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Skylands
{
    [Obfuscation(Exclude = true, ApplyToMembers = true)]
    [RequireComponent(typeof(Collider))]
    public class SupportZone : MonoBehaviour
    {
        public static List<Collider> AllSupportZones = new List<Collider>();
        private Collider m_collider;

        // Guardamos players dentro desta zona
        private static HashSet<long> playersInside = new HashSet<long>();

        private void Awake()
        {
            m_collider = GetComponent<Collider>();
            if (m_collider != null)
                m_collider.isTrigger = true; // garante que é trigger
        }

        private void OnEnable()
        {
            if (m_collider != null && !AllSupportZones.Contains(m_collider))
            {
                AllSupportZones.Add(m_collider);
            }
        }

        private void OnDisable()
        {
            if (m_collider != null)
            {
                AllSupportZones.Remove(m_collider);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            var player = other.GetComponentInParent<Player>();
            if (player != null)
            {
                playersInside.Add(player.GetPlayerID());
                //Debug.Log($"[Skylands] Player {player.GetPlayerID()} entrou no SupportZone de {name}");
            }
        }

        private void OnTriggerExit(Collider other)
        {
            var player = other.GetComponentInParent<Player>();
            if (player != null)
            {
                playersInside.Remove(player.GetPlayerID());
                //Debug.Log($"[Skylands] Player {player.GetPlayerID()} saiu do SupportZone de {name}");
            }
        }

        public static bool IsPlayerInside(Player player)
        {
            return player != null && playersInside.Contains(player.GetPlayerID());
        }
    }
}
