using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Realms // Use o seu namespace
{
    [RequireComponent(typeof(BoxCollider))]
    public class ZoneShaderManager : MonoBehaviour
    {
        // No Unity, arraste o seu ficheiro de shader 'Custom_Piece.shader' para este campo.
        public Shader noSnowShader;

        // Guarda os renderers que já foram modificados.
        private HashSet<Renderer> modifiedRenderers = new HashSet<Renderer>();

        // Nome do shader vanilla que queremos substituir.
        private const string VANILLA_PIECE_SHADER_NAME = "Custom/Piece";

        private BoxCollider zoneCollider;

        private void Awake()
        {
            zoneCollider = GetComponent<BoxCollider>();
            // Garante que o collider seja um trigger para não ter colisões físicas.
            zoneCollider.isTrigger = true;
        }

        private void Start()
        {
            if (noSnowShader == null)
            {
                Debug.LogError("[ZoneShaderManager] O 'No Snow Shader' não foi associado no Inspector!");
                return;
            }
            // Inicia uma rotina que verifica os objetos na área a cada 2 segundos.
            StartCoroutine(SwapShadersInZone());
        }

        private IEnumerator SwapShadersInZone()
        {
            while (true)
            {
                // Calcula os limites da zona, levando em conta a escala do GameObject.
                Vector3 center = transform.position + Vector3.Scale(transform.localScale, zoneCollider.center);
                Vector3 size = Vector3.Scale(transform.localScale, zoneCollider.size);

                // Encontra todos os colliders dentro da zona de proteção.
                Collider[] collidersInZone = Physics.OverlapBox(center, size / 2, transform.rotation);

                foreach (Collider itemCollider in collidersInZone)
                {
                    Renderer itemRenderer = itemCollider.GetComponent<Renderer>();

                    if (itemRenderer != null && !modifiedRenderers.Contains(itemRenderer))
                    {
                        bool needsModification = false;
                        foreach (Material mat in itemRenderer.materials)
                        {
                            // Se algum dos materiais usar o shader vanilla...
                            if (mat.shader.name == VANILLA_PIECE_SHADER_NAME)
                            {
                                needsModification = true;
                                break;
                            }
                        }

                        if (needsModification)
                        {
                            Debug.Log($"[ZoneShaderManager] Encontrado objeto '{itemRenderer.name}' com shader vanilla. A substituí-lo.");
                            // Aplica o nosso shader sem neve a todos os materiais do objeto.
                            foreach (Material mat in itemRenderer.materials)
                            {
                                mat.shader = noSnowShader;
                            }
                            modifiedRenderers.Add(itemRenderer);
                        }
                    }
                }

                yield return new WaitForSeconds(2f);
            }
        }
    }
}