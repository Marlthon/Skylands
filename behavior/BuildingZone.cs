using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

[Obfuscation(Exclude = true, ApplyToMembers = true)]
[RequireComponent(typeof(Collider))]
public class BuildingZone : MonoBehaviour
{
    public static List<Collider> AllZones = new List<Collider>();

    private Collider m_collider;

    private void Awake()
    {
        m_collider = GetComponent<Collider>();
        if (m_collider != null)
            m_collider.isTrigger = true;
    }

    private void OnEnable()
    {
        if (m_collider != null && !AllZones.Contains(m_collider))
            AllZones.Add(m_collider);
    }

    private void OnDisable()
    {
        if (m_collider != null)
            AllZones.Remove(m_collider);
    }
}