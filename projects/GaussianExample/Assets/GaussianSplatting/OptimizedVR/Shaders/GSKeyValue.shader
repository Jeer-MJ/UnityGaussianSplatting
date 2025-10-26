// SPDX-License-Identifier: MIT
// Compute key-value pairs for radix sorting Gaussian splats
// Optimized for mobile VR (Quest 3, GLES 3.2/Vulkan)

Shader "GaussianSplatting/VR/ComputeKeyValue"
{
    Properties
    {
        _GS_Positions ("Splat Positions", 2D) = "black" {}
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
        
        Pass
        {
            Name "ComputeKeyValue"
            ZTest Always
            ZWrite Off
            Cull Off
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            
            #include "UnityCG.cginc"
            
            // Input texture with splat positions (RGB = XYZ in local space)
            sampler2D _GS_Positions;
            float4 _SplatPosTexSize; // (width, height, 1/width, 1/height)
            
            // Transform and camera parameters
            float4x4 _SplatToWorld;
            float3 _CameraPosition;
            float _MinSortDistance;
            float _MaxSortDistance;
            int _SplatCount;
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };
            
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            
            // Output: RG format
            // R = normalized distance (sort key, 0-65535 range)
            // G = original index (for tracking splat identity)
            float2 frag(v2f i) : SV_Target
            {
                // Calculate linear index from UV coordinates
                int2 pixelCoord = int2(i.uv * _SplatPosTexSize.xy);
                int linearIndex = pixelCoord.y * (int)_SplatPosTexSize.x + pixelCoord.x;
                
                // Early exit if beyond splat count
                if (linearIndex >= _SplatCount)
                {
                    return float2(1.0, asfloat(linearIndex)); // Max distance, preserve index
                }
                
                // Read splat position from texture (local space)
                float4 localPos = tex2D(_GS_Positions, i.uv);
                
                // Transform to world space
                float3 worldPos = mul(_SplatToWorld, float4(localPos.xyz, 1.0)).xyz;
                
                // Calculate distance to camera
                float distance = length(worldPos - _CameraPosition);
                
                // Normalize distance to [0, 1] range
                float normalizedDist = saturate(
                    (distance - _MinSortDistance) / 
                    (_MaxSortDistance - _MinSortDistance)
                );
                
                // Quantize to 16-bit for sorting (0 to 65535)
                // Note: We'll actually use 12-bit (4096 values) for mobile optimization
                float sortKey = normalizedDist;
                
                // Preserve original index as float
                float indexAsFloat = asfloat(linearIndex);
                
                return float2(sortKey, indexAsFloat);
            }
            ENDCG
        }
    }
    
    Fallback Off
}
