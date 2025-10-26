// SPDX-License-Identifier: MIT
// GPU Radix Sort using rasterization (no compute shaders, mobile VR compatible)
// Adapted from VRChat Gaussian Splatting system by d4rkpl4y3r
// Optimized for Meta Quest 3

using UnityEngine;
using UnityEngine.Rendering;

namespace GaussianSplatting.OptimizedVR
{
    /// <summary>
    /// GPU-based radix sort using only rasterization (no compute shaders).
    /// Compatible with mobile VR platforms like Meta Quest 3.
    /// Uses mipmap-based prefix sum technique for efficient sorting on mobile GPUs.
    /// </summary>
    public class RadixSortVR : MonoBehaviour
    {
        [Header("Sorting Configuration")]
        [Tooltip("Number of sorting passes (3 = 12-bit, 4 = 16-bit). Lower is faster for mobile.")]
        [Range(2, 4)]
        public int sortingPasses = 3; // 12-bit for mobile optimization
        
        [Header("Materials")]
        [Tooltip("Material with GSKeyValue shader for computing sort keys")]
        public Material computeKeyValues;
        
        [Tooltip("Material with RadixSort shader for sorting passes")]
        public Material radixSort;
        
        [Header("RenderTextures")]
        [Tooltip("Ping texture for key-value pairs (RGFloat, 1024x1024)")]
        public RenderTexture keyValues0;
        
        [Tooltip("Pong texture for key-value pairs (RGFloat, 1024x1024)")]
        public RenderTexture keyValues1;
        
        [Tooltip("Prefix sum texture with mipmaps (RFloat, 1024x1024, mipmapped)")]
        public RenderTexture prefixSums;
        
        // Shader property IDs
        static readonly int _SplatCount = Shader.PropertyToID("_SplatCount");
        static readonly int _SplatToWorld = Shader.PropertyToID("_SplatToWorld");
        static readonly int _CameraPosition = Shader.PropertyToID("_CameraPosition");
        static readonly int _MinSortDistance = Shader.PropertyToID("_MinSortDistance");
        static readonly int _MaxSortDistance = Shader.PropertyToID("_MaxSortDistance");
        static readonly int _RadixSortBit = Shader.PropertyToID("_RadixSortBit");
        static readonly int _KeyValues = Shader.PropertyToID("_KeyValues");
        static readonly int _PrefixSums = Shader.PropertyToID("_PrefixSums");
        static readonly int _GS_Positions = Shader.PropertyToID("_GS_Positions");
        static readonly int _SplatPosTexSize = Shader.PropertyToID("_SplatPosTexSize");
        
        const int RADIX_BITS = 4; // 4 bits per pass (16 buckets)
        
        Material m_TempMat;
        
        void OnDestroy()
        {
            if (m_TempMat != null)
            {
                DestroyImmediate(m_TempMat);
                m_TempMat = null;
            }
        }
        
        /// <summary>
        /// Compute sorting keys for all splats based on camera position.
        /// This calculates the distance from each splat to the camera and normalizes it.
        /// </summary>
        /// <param name="splatPositions">Texture containing splat positions (RGB = XYZ)</param>
        /// <param name="splatToWorld">Transform matrix from splat local space to world space</param>
        /// <param name="cameraPosition">World position of the camera</param>
        /// <param name="minDistance">Minimum distance for normalization</param>
        /// <param name="maxDistance">Maximum distance for normalization</param>
        /// <param name="splatCount">Total number of splats</param>
        public void ComputeKeyValues(
            Texture splatPositions,
            Matrix4x4 splatToWorld,
            Vector3 cameraPosition,
            float minDistance,
            float maxDistance,
            int splatCount)
        {
            if (computeKeyValues == null || keyValues0 == null)
            {
                Debug.LogError("[RadixSortVR] Missing computeKeyValues material or keyValues0 texture!");
                return;
            }
            
            // Set shader parameters
            computeKeyValues.SetTexture(_GS_Positions, splatPositions);
            computeKeyValues.SetMatrix(_SplatToWorld, splatToWorld);
            computeKeyValues.SetVector(_CameraPosition, cameraPosition);
            computeKeyValues.SetFloat(_MinSortDistance, minDistance);
            computeKeyValues.SetFloat(_MaxSortDistance, maxDistance);
            computeKeyValues.SetInt(_SplatCount, splatCount);
            
            if (splatPositions != null)
            {
                computeKeyValues.SetVector(_SplatPosTexSize, 
                    new Vector4(splatPositions.width, splatPositions.height, 0, 0));
            }
            
            // Render to keyValues0
            Graphics.Blit(null, keyValues0, computeKeyValues);
        }
        
        /// <summary>
        /// Execute radix sort passes to sort splats front-to-back.
        /// Uses ping-pong buffers and mipmap-based prefix sums.
        /// </summary>
        public void Sort()
        {
            if (radixSort == null || keyValues0 == null || keyValues1 == null || prefixSums == null)
            {
                Debug.LogError("[RadixSortVR] Missing radix sort materials or textures!");
                return;
            }
            
            RenderTexture srcKeyValues = keyValues0;
            RenderTexture dstKeyValues = keyValues1;
            
            // Execute sorting passes (LSB to MSB)
            for (int pass = 0; pass < sortingPasses; pass++)
            {
                int bitShift = pass * RADIX_BITS;
                
                // Pass 0: Count values per bucket and write to prefix sums
                radixSort.SetInt(_RadixSortBit, bitShift);
                radixSort.SetTexture(_KeyValues, srcKeyValues);
                Graphics.Blit(srcKeyValues, prefixSums, radixSort, 0);
                
                // Mipmaps automatically calculate prefix sums via averaging
                prefixSums.GenerateMips();
                
                // Pass 1: Scatter to sorted positions
                radixSort.SetTexture(_PrefixSums, prefixSums);
                Graphics.Blit(srcKeyValues, dstKeyValues, radixSort, 1);
                
                // Swap buffers for next pass
                var temp = srcKeyValues;
                srcKeyValues = dstKeyValues;
                dstKeyValues = temp;
            }
            
            // Ensure final result is in keyValues0
            if (sortingPasses % 2 == 1)
            {
                Graphics.Blit(keyValues1, keyValues0);
            }
        }
        
        /// <summary>
        /// Get the final sorted render order texture.
        /// This should be passed to the gaussian splatting render shader.
        /// </summary>
        public RenderTexture GetSortedOutput()
        {
            return keyValues0;
        }
        
        /// <summary>
        /// Validate that all required resources are assigned.
        /// </summary>
        public bool IsValid()
        {
            return computeKeyValues != null && 
                   radixSort != null && 
                   keyValues0 != null && 
                   keyValues1 != null && 
                   prefixSums != null;
        }
        
        /// <summary>
        /// Create RenderTextures with optimal settings for Quest 3.
        /// Call this from editor or initialization code.
        /// </summary>
        public static void CreateOptimizedRenderTextures(
            out RenderTexture keyValues0,
            out RenderTexture keyValues1,
            out RenderTexture prefixSums,
            int resolution = 1024)
        {
            // Key-value textures: RG channels (Red = sort key, Green = original index)
            keyValues0 = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.RGFloat)
            {
                name = "RadixSort_KeyValues0",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                useMipMap = false,
                autoGenerateMips = false
            };
            keyValues0.Create();
            
            keyValues1 = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.RGFloat)
            {
                name = "RadixSort_KeyValues1",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                useMipMap = false,
                autoGenerateMips = false
            };
            keyValues1.Create();
            
            // Prefix sums: Single channel with mipmaps for reduction
            prefixSums = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.RFloat)
            {
                name = "RadixSort_PrefixSums",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                useMipMap = true,
                autoGenerateMips = false // We manually call GenerateMips
            };
            prefixSums.Create();
        }
    }
}
