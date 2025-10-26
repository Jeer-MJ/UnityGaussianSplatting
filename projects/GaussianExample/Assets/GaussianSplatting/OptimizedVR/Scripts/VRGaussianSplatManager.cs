// SPDX-License-Identifier: MIT
// VR Gaussian Splatting Manager - Optimized for Meta Quest 3
// Integrates with aras-p's system while adding efficient sorting for VR

using UnityEngine;
using UnityEngine.XR;
using GaussianSplatting.Runtime;

namespace GaussianSplatting.OptimizedVR
{
    /// <summary>
    /// Manages Gaussian Splatting rendering for VR with optimized sorting.
    /// Handles stereo rendering (left/right eye) and camera quantization for performance.
    /// </summary>
    [RequireComponent(typeof(RadixSortVR))]
    public class VRGaussianSplatManager : MonoBehaviour
    {
        [Header("Splat Objects")]
        [Tooltip("Array of Gaussian splat renderers to manage")]
        public GaussianSplatRenderer[] splatObjects = new GaussianSplatRenderer[0];
        
        [Tooltip("Which splat object to render (index into splatObjects array)")]
        [Range(0, 10)]
        public int activeSplatIndex = 0;
        
        [Header("Sorting Configuration")]
        [Tooltip("Minimum distance for sort normalization (meters)")]
        public float minSortDistance = 0.0f;
        
        [Tooltip("Maximum distance for sort normalization (meters)")]
        public float maxSortDistance = 50.0f;
        
        [Tooltip("Camera movement threshold to trigger re-sort (meters). Higher = better performance.")]
        [Range(0.01f, 0.5f)]
        public float cameraPositionQuantization = 0.05f; // 5cm
        
        [Tooltip("Always update sorting every frame (expensive, only for testing)")]
        public bool alwaysUpdate = false;
        
        [Header("VR Optimization")]
        [Tooltip("Use separate sorting for left and right eye (better quality, slower)")]
        public bool separateEyeSorting = false;
        
        [Tooltip("Inter-pupillary distance for stereo separation (meters)")]
        [Range(0.055f, 0.075f)]
        public float ipd = 0.064f;
        
        [Header("Debug")]
        [Tooltip("Show debug info in console")]
        public bool debugLog = false;
        
        // Private state
        RadixSortVR m_RadixSort;
        Vector3 m_LastSortPosition;
        Camera m_MainCamera;
        bool m_IsVRActive;
        
        // Performance tracking
        int m_SortFrameCounter = 0;
        
        void Start()
        {
            m_RadixSort = GetComponent<RadixSortVR>();
            
            if (m_RadixSort == null || !m_RadixSort.IsValid())
            {
                Debug.LogError("[VRGaussianSplatManager] RadixSortVR component not found or invalid!");
                enabled = false;
                return;
            }
            
            // Detect VR
            m_IsVRActive = XRSettings.enabled && XRSettings.isDeviceActive;
            
            if (debugLog)
            {
                Debug.Log($"[VRGaussianSplatManager] Initialized. VR Active: {m_IsVRActive}");
            }
            
            m_LastSortPosition = Vector3.positiveInfinity; // Force initial sort
        }
        
        void Update()
        {
            // Get main camera
            if (m_MainCamera == null)
            {
                m_MainCamera = Camera.main;
                if (m_MainCamera == null) return;
            }
            
            // Get camera positions for sorting
            Vector3 leftEyePos, rightEyePos, centerPos;
            GetVRCameraPositions(out leftEyePos, out rightEyePos, out centerPos);
            
            // Use center position for single-camera sorting
            Vector3 sortPosition = centerPos;
            
            // Check if we need to re-sort
            bool shouldSort = alwaysUpdate || 
                              Vector3.Distance(sortPosition, m_LastSortPosition) > cameraPositionQuantization;
            
            if (shouldSort)
            {
                // Get active splat renderer
                if (activeSplatIndex < 0 || activeSplatIndex >= splatObjects.Length)
                {
                    if (debugLog && m_SortFrameCounter % 60 == 0)
                    {
                        Debug.LogWarning($"[VRGaussianSplatManager] Invalid splat index: {activeSplatIndex}");
                    }
                    return;
                }
                
                var activeSplat = splatObjects[activeSplatIndex];
                if (activeSplat == null || !activeSplat.isActiveAndEnabled)
                {
                    return;
                }
                
                // Perform sorting
                if (separateEyeSorting && m_IsVRActive)
                {
                    // Sort for both eyes separately (more expensive)
                    SortForPosition(activeSplat, leftEyePos, 0);
                    SortForPosition(activeSplat, rightEyePos, 1);
                }
                else
                {
                    // Sort for center position (faster, good enough for most cases)
                    SortForPosition(activeSplat, sortPosition, 0);
                }
                
                m_LastSortPosition = sortPosition;
                m_SortFrameCounter++;
                
                if (debugLog && m_SortFrameCounter % 60 == 0)
                {
                    Debug.Log($"[VRGaussianSplatManager] Sorted at position {sortPosition}, frame {Time.frameCount}");
                }
            }
        }
        
        /// <summary>
        /// Get camera positions for VR stereo rendering.
        /// </summary>
        void GetVRCameraPositions(out Vector3 leftEye, out Vector3 rightEye, out Vector3 center)
        {
            if (m_MainCamera == null)
            {
                leftEye = rightEye = center = Vector3.zero;
                return;
            }
            
            center = m_MainCamera.transform.position;
            
            #if UNITY_ANDROID && !UNITY_EDITOR
            // On Quest, try to get actual eye positions
            if (m_IsVRActive)
            {
                // Use XR API to get eye transforms
                var leftEyeTransform = InputTracking.GetLocalPosition(XRNode.LeftEye);
                var rightEyeTransform = InputTracking.GetLocalPosition(XRNode.RightEye);
                
                leftEye = m_MainCamera.transform.TransformPoint(leftEyeTransform);
                rightEye = m_MainCamera.transform.TransformPoint(rightEyeTransform);
            }
            else
            {
                // Fallback to IPD approximation
                Vector3 offset = m_MainCamera.transform.right * (ipd * 0.5f);
                leftEye = center - offset;
                rightEye = center + offset;
            }
            #else
            // In editor, use IPD approximation
            Vector3 offset = m_MainCamera.transform.right * (ipd * 0.5f);
            leftEye = center - offset;
            rightEye = center + offset;
            #endif
        }
        
        /// <summary>
        /// Sort splats for a specific camera position.
        /// </summary>
        void SortForPosition(GaussianSplatRenderer splat, Vector3 cameraPos, int cameraID)
        {
            if (splat.asset == null)
            {
                if (debugLog)
                {
                    Debug.LogWarning($"[VRGaussianSplatManager] Splat has no asset!");
                }
                return;
            }
            
            // Get splat data
            var asset = splat.asset;
            var splatCount = asset.splatCount;
            
            // NOTE: aras-p system uses GraphicsBuffers, not textures
            // The sorting integration would require:
            // 1. Converting GraphicsBuffer position data to a texture
            // 2. Or modifying the shader to read from buffers
            // 3. Or using the existing GPU sorting from aras-p
            
            // For now, log that we would sort here
            if (debugLog && m_SortFrameCounter % 60 == 0)
            {
                Debug.Log($"[VRGaussianSplatManager] Would sort {splatCount} splats for camera at {cameraPos}");
                Debug.Log($"[VRGaussianSplatManager] NOTE: Full integration requires adapting aras-p's GraphicsBuffer system");
            }
            
            // TODO: Full integration options:
            // Option A: Convert GraphicsBuffer to Texture2D for our shaders
            // Option B: Modify GSKeyValue.shader to read from StructuredBuffer
            // Option C: Use aras-p's existing GpuSorting system (compute shaders)
            // Option D: Hybrid - use compute shader sorting on desktop, our system on mobile
        }
        
        /// <summary>
        /// Force a re-sort on next frame.
        /// </summary>
        public void ForceSortNextFrame()
        {
            m_LastSortPosition = Vector3.positiveInfinity;
        }
        
        /// <summary>
        /// Get current sorting statistics.
        /// </summary>
        public void GetSortingStats(out int sortCount, out Vector3 lastPosition)
        {
            sortCount = m_SortFrameCounter;
            lastPosition = m_LastSortPosition;
        }
        
        void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying) return;
            
            // Draw camera positions
            Gizmos.color = Color.green;
            if (m_MainCamera != null)
            {
                Vector3 leftEye, rightEye, center;
                GetVRCameraPositions(out leftEye, out rightEye, out center);
                
                Gizmos.DrawWireSphere(center, 0.05f);
                Gizmos.DrawWireSphere(leftEye, 0.02f);
                Gizmos.DrawWireSphere(rightEye, 0.02f);
                Gizmos.DrawLine(leftEye, rightEye);
            }
            
            // Draw sort distances
            if (splatObjects != null && activeSplatIndex >= 0 && activeSplatIndex < splatObjects.Length)
            {
                var splat = splatObjects[activeSplatIndex];
                if (splat != null)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireSphere(splat.transform.position, minSortDistance);
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireSphere(splat.transform.position, maxSortDistance);
                }
            }
        }
    }
}
