// SPDX-License-Identifier: MIT
// VR Gaussian Splatting Manager - Optimized for Meta Quest 3
// Integrates with aras-p's system while adding efficient sorting for VR

using UnityEngine;
using UnityEngine.XR;
using UnityEngine.Rendering;
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
        /// Converts GraphicsBuffer to Texture2D and executes radix sort.
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
            
            // PASO 1: Obtener GraphicsBuffer de posiciones desde renderer
            GraphicsBuffer posBuffer = splat.GetPositionBuffer();
            
            if (posBuffer == null || posBuffer.count == 0)
            {
                if (debugLog)
                {
                    Debug.LogWarning($"[VRGaussianSplatManager] No se pudo obtener buffer de posiciones!");
                }
                return;
            }
            
            // PASO 2: Convertir GraphicsBuffer a Texture2D
            Texture2D posTexture = ConvertPositionBufferToTexture(posBuffer, splatCount, asset.posFormat);
            
            if (posTexture == null)
            {
                Debug.LogError($"[VRGaussianSplatManager] Conversión de buffer a texture falló!");
                return;
            }
            
            // PASO 3: Ejecutar radix sort
            try
            {
                // Obtener matriz de transform del splat
                Matrix4x4 splatToWorld = splat.transform.localToWorldMatrix;
                
                m_RadixSort.ComputeKeyValues(
                    posTexture,
                    splatToWorld,
                    cameraPos,
                    minSortDistance,
                    maxSortDistance,
                    splatCount
                );
                
                m_RadixSort.Sort();
                
                if (debugLog && m_SortFrameCounter % 60 == 0)
                {
                    Debug.Log($"[VRGaussianSplatManager] ✅ Sorted {splatCount} splats para cámara en {cameraPos}");
                }
            }
            finally
            {
                // PASO 4: Cleanup - destruir texture temporal
                if (posTexture != null)
                {
                    DestroyImmediate(posTexture);
                }
            }
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
        
        // ====================================================================
        // BUFFER → TEXTURE CONVERSION
        // ====================================================================
        
        /// <summary>
        /// Convierte GraphicsBuffer de posiciones a Texture2D para shaders.
        /// 
        /// PROPÓSITO:
        /// - aras-p almacena posiciones en GraphicsBuffer (GPU)
        /// - Nuestros shaders esperan Texture2D (sampler2D)
        /// - Esta conversión hace el bridge entre ambos sistemas
        /// 
        /// PROCESO:
        /// 1. Lee datos del GraphicsBuffer (GPU → CPU)
        /// 2. Decodifica formato comprimido (Norm11, Norm16, etc.)
        /// 3. Convierte a float3 (posiciones XYZ)
        /// 4. Crea Texture2D y escribe datos
        /// 5. Sube a GPU
        /// 
        /// PERFORMANCE:
        /// - CPU: ~0.2-0.5ms (lectura buffer)
        /// - Conversión: ~0.3-0.8ms (decodificación)
        /// - GPU upload: ~0.1-0.3ms
        /// - Total: ~0.5-1.5ms (1M splats)
        /// 
        /// IMPORTANTE:
        /// - Texture creada es temporal - debe destruirse después de usar
        /// - Solo se llama cuando cámara se mueve (5-10 veces/seg, no 72/seg)
        /// - Overhead es aceptable gracias a quantization
        /// </summary>
        /// <param name="posBuffer">Buffer GPU de posiciones de aras-p</param>
        /// <param name="splatCount">Número de splats en el buffer</param>
        /// <param name="format">Formato de compresión usado (Norm11, Norm16, etc.)</param>
        /// <returns>Texture2D con posiciones decodificadas, o null si falla</returns>
        Texture2D ConvertPositionBufferToTexture(
            GraphicsBuffer posBuffer,
            int splatCount,
            GaussianSplatAsset.VectorFormat format)
        {
            if (posBuffer == null || splatCount <= 0)
            {
                Debug.LogError("[ConvertPositionBufferToTexture] Buffer inválido o splatCount <= 0");
                return null;
            }
            
            // PASO 1: Calcular dimensiones de la texture
            // Usamos texture cuadrada o rectangular para fitting óptimo
            int texWidth = Mathf.CeilToInt(Mathf.Sqrt(splatCount));
            int texHeight = Mathf.CeilToInt((float)splatCount / texWidth);
            
            if (debugLog)
            {
                Debug.Log($"[ConvertPositionBufferToTexture] Creando texture {texWidth}x{texHeight} " +
                          $"para {splatCount} splats, formato {format}");
            }
            
            // PASO 2: Obtener tamaño de cada vector en bytes
            int vectorSize = VectorDecoder.GetVectorSize(format);
            int bufferSize = splatCount * vectorSize;
            
            // PASO 3: Leer datos del GraphicsBuffer a CPU
            byte[] bufferData = new byte[bufferSize];
            
            try
            {
                posBuffer.GetData(bufferData, 0, 0, bufferSize);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ConvertPositionBufferToTexture] Error leyendo buffer: {ex.Message}");
                return null;
            }
            
            // PASO 4: Crear texture para almacenar posiciones
            // Formato RGBAFloat para máxima precisión (xyz en RGB, W sin usar)
            Texture2D posTexture = new Texture2D(
                texWidth,
                texHeight,
                UnityEngine.Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat,
                UnityEngine.Experimental.Rendering.TextureCreationFlags.None
            );
            
            // PASO 5: Decodificar datos según formato
            Color[] pixels = new Color[texWidth * texHeight];
            
            for (int i = 0; i < splatCount; i++)
            {
                int offset = i * vectorSize;
                Unity.Mathematics.float3 position;
                
                // Decodificar según formato
                switch (format)
                {
                    case GaussianSplatAsset.VectorFormat.Norm11:
                        // Leer uint32 (4 bytes)
                        uint packed = System.BitConverter.ToUInt32(bufferData, offset);
                        position = VectorDecoder.DecodeNorm11(packed);
                        break;
                    
                    case GaussianSplatAsset.VectorFormat.Norm16:
                        // Leer 3 ushorts (6 bytes)
                        position = VectorDecoder.DecodeNorm16FromBytes(bufferData, offset);
                        break;
                    
                    case GaussianSplatAsset.VectorFormat.Float32:
                        // Leer 3 floats (12 bytes)
                        position = VectorDecoder.DecodeFloat32FromBytes(bufferData, offset);
                        break;
                    
                    case GaussianSplatAsset.VectorFormat.Norm6:
                        // Leer 3 bytes
                        position = VectorDecoder.DecodeNorm6(
                            bufferData[offset + 0],
                            bufferData[offset + 1],
                            bufferData[offset + 2]
                        );
                        break;
                    
                    default:
                        Debug.LogError($"[ConvertPositionBufferToTexture] Formato no soportado: {format}");
                        DestroyImmediate(posTexture);
                        return null;
                }
                
                // Convertir float3 a Color (xyz → rgb, w=1)
                pixels[i] = new Color(position.x, position.y, position.z, 1.0f);
            }
            
            // Rellenar pixels restantes con transparente (si texHeight > necesario)
            for (int i = splatCount; i < pixels.Length; i++)
            {
                pixels[i] = Color.clear;
            }
            
            // PASO 6: Escribir pixels a texture y subir a GPU
            posTexture.SetPixels(pixels);
            posTexture.Apply(false, false); // No mipmaps, readable desde CPU
            
            if (debugLog)
            {
                Debug.Log($"[ConvertPositionBufferToTexture] ✅ Texture creada: " +
                          $"{texWidth}x{texHeight}, {splatCount} splats decodificados");
            }
            
            return posTexture;
        }
    }
}
