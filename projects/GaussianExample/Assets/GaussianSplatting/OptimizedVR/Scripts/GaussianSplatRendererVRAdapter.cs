// SPDX-License-Identifier: MIT
// Adapter to integrate RadixSort VR with aras-p GaussianSplatRenderer
// Extends the existing renderer without modifying core package

using UnityEngine;
using GaussianSplatting.Runtime;

namespace GaussianSplatting.OptimizedVR
{
    /// <summary>
    /// Extends GaussianSplatRenderer to use optimized VR sorting.
    /// Add this component alongside GaussianSplatRenderer.
    /// </summary>
    [RequireComponent(typeof(GaussianSplatRenderer))]
    public class GaussianSplatRendererVRAdapter : MonoBehaviour
    {
        [Header("VR Sorting")]
        [Tooltip("Reference to the VR sorting manager")]
        public VRGaussianSplatManager vrManager;
        
        [Tooltip("Use VR optimized sorting instead of default compute shader sorting")]
        public bool useVRSorting = true;
        
        [Header("Render Order Integration")]
        [Tooltip("Material property name for sorted indices texture")]
        public string sortedIndicesPropertyName = "_SortedIndices";
        
        // Cache
        GaussianSplatRenderer m_Renderer;
        MaterialPropertyBlock m_PropertyBlock;
        
        void Start()
        {
            m_Renderer = GetComponent<GaussianSplatRenderer>();
            m_PropertyBlock = new MaterialPropertyBlock();
            
            if (m_Renderer == null)
            {
                Debug.LogError("[GaussianSplatRendererVRAdapter] GaussianSplatRenderer not found!");
                enabled = false;
                return;
            }
            
            if (vrManager == null)
            {
                vrManager = FindFirstObjectByType<VRGaussianSplatManager>();
                if (vrManager == null)
                {
                    Debug.LogWarning("[GaussianSplatRendererVRAdapter] VRGaussianSplatManager not found in scene!");
                }
            }
        }
        
        void LateUpdate()
        {
            if (!useVRSorting || vrManager == null) return;
            
            // Get sorted output from VR manager
            var radixSort = vrManager.GetComponent<RadixSortVR>();
            if (radixSort == null) return;
            
            var sortedTexture = radixSort.GetSortedOutput();
            if (sortedTexture == null) return;
            
            // Apply to renderer material
            ApplySortedTexture(sortedTexture);
        }
        
        void ApplySortedTexture(RenderTexture sortedTexture)
        {
            // Method 1: Via MaterialPropertyBlock (doesn't affect shared material)
            var renderer = m_Renderer.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.GetPropertyBlock(m_PropertyBlock);
                m_PropertyBlock.SetTexture(sortedIndicesPropertyName, sortedTexture);
                renderer.SetPropertyBlock(m_PropertyBlock);
            }
            
            // Method 2: Via material directly (affects all instances)
            // Uncomment if you want global effect
            /*
            var material = m_Renderer.GetComponent<Renderer>()?.sharedMaterial;
            if (material != null)
            {
                material.SetTexture(sortedIndicesPropertyName, sortedTexture);
            }
            */
        }
        
        /// <summary>
        /// Get the current sorting texture being used.
        /// </summary>
        public RenderTexture GetCurrentSortedTexture()
        {
            if (vrManager == null) return null;
            
            var radixSort = vrManager.GetComponent<RadixSortVR>();
            return radixSort?.GetSortedOutput();
        }
        
        /// <summary>
        /// Force a re-sort on the next frame.
        /// </summary>
        public void ForceSortNextFrame()
        {
            vrManager?.ForceSortNextFrame();
        }
        
        void OnDrawGizmosSelected()
        {
            // Visualize sorting state
            if (!Application.isPlaying || vrManager == null) return;
            
            Gizmos.color = useVRSorting ? Color.green : Color.gray;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);
        }
    }
}
