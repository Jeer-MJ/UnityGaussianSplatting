// ============================================================================
// VectorDecoder.cs
// ============================================================================
// Decodifica datos de posición comprimidos desde GraphicsBuffer a vectores float3.
//
// PROPÓSITO:
// - aras-p almacena posiciones en formatos comprimidos (Norm11, Norm16, etc)
// - Este decodificador convierte de formato binario a float3 utilizable
//
// FORMATOS SOPORTADOS:
// - Norm11:   11+11+10 bits (32 bits total) - Rango normalizado [-1, 1]
// - Norm16:   16+16+16 bits (48 bits = 6 bytes) - Mayor precisión
// - Float32:  32+32+32 bits (96 bits = 12 bytes) - Precisión completa
// - Norm6:    6+6+6 bits (18 bits en 3 bytes) - Baja precisión
//
// USO:
//   uint packedData = buffer[index];
//   float3 position = VectorDecoder.DecodeNorm11(packedData);
//
// PERFORMANCE:
// - Decodificación pura CPU
// - ~0.3-0.8ms para 1M splats
// - Sin allocations (métodos estáticos)
// ============================================================================

using Unity.Mathematics;
using GaussianSplatting.Runtime;

namespace GaussianSplatting.OptimizedVR
{
    /// <summary>
    /// Decodificador de vectores comprimidos desde formatos binarios de aras-p.
    /// Métodos estáticos sin allocations para máxima performance.
    /// </summary>
    public static class VectorDecoder
    {
        // ====================================================================
        // NORM11: 11+11+10 bits en uint32
        // ====================================================================
        // Formato: [11 bits X][11 bits Y][10 bits Z]
        // Precisión: ~2048 pasos para X/Z, 1024 para Y
        // Rango: [-1, 1] normalizado
        // Uso típico: Posiciones en escena normalizada
        // ====================================================================
        
        /// <summary>
        /// Decodifica formato Norm11 (11+11+10 bits) a float3.
        /// 
        /// Estructura del uint32:
        /// Bits 31-21: X (11 bits) → Rango [0, 2047] → Normalizado [-1, 1]
        /// Bits 20-11: Y (10 bits) → Rango [0, 1023] → Normalizado [-1, 1]
        /// Bits 10-0:  Z (11 bits) → Rango [0, 2047] → Normalizado [-1, 1]
        /// </summary>
        /// <param name="packed">Datos comprimidos en formato uint32</param>
        /// <returns>Vector3 decodificado en rango [-1, 1]</returns>
        public static float3 DecodeNorm11(uint packed)
        {
            // Extraer componentes usando máscaras de bits
            uint x = (packed >> 21) & 0x7FF;  // 0x7FF = 2047 (11 bits)
            uint y = (packed >> 11) & 0x3FF;  // 0x3FF = 1023 (10 bits)
            uint z = packed & 0x7FF;           // 0x7FF = 2047 (11 bits)
            
            // Normalizar a [0, 1] y luego a [-1, 1]
            return new float3(
                (x / 2047.0f) * 2.0f - 1.0f,
                (y / 1023.0f) * 2.0f - 1.0f,
                (z / 2047.0f) * 2.0f - 1.0f
            );
        }

        // ====================================================================
        // NORM16: 16+16+16 bits (6 bytes total)
        // ====================================================================
        // Formato: 3 ushorts consecutivos
        // Precisión: 65536 pasos por componente
        // Rango: [-1, 1] normalizado
        // Uso típico: Alta precisión de posiciones
        // ====================================================================
        
        /// <summary>
        /// Decodifica formato Norm16 (16+16+16 bits) desde 3 ushorts.
        /// 
        /// Estructura:
        /// ushort[0]: X (16 bits) → [0, 65535] → [-1, 1]
        /// ushort[1]: Y (16 bits) → [0, 65535] → [-1, 1]
        /// ushort[2]: Z (16 bits) → [0, 65535] → [-1, 1]
        /// 
        /// Mayor precisión que Norm11 (16 bits vs 10-11 bits).
        /// </summary>
        /// <param name="x">Componente X comprimido</param>
        /// <param name="y">Componente Y comprimido</param>
        /// <param name="z">Componente Z comprimido</param>
        /// <returns>Vector3 decodificado en rango [-1, 1]</returns>
        public static float3 DecodeNorm16(ushort x, ushort y, ushort z)
        {
            // Normalizar de [0, 65535] a [0, 1] y luego a [-1, 1]
            const float scale = 1.0f / 65535.0f;
            return new float3(
                (x * scale) * 2.0f - 1.0f,
                (y * scale) * 2.0f - 1.0f,
                (z * scale) * 2.0f - 1.0f
            );
        }
        
        /// <summary>
        /// Sobrecarga de DecodeNorm16 para trabajar con array de bytes.
        /// Lee 6 bytes consecutivos como 3 ushorts.
        /// </summary>
        /// <param name="bytes">Array de bytes (mínimo 6 bytes)</param>
        /// <param name="offset">Offset inicial en el array</param>
        /// <returns>Vector3 decodificado</returns>
        public static float3 DecodeNorm16FromBytes(byte[] bytes, int offset)
        {
            // Leer 3 ushorts (little-endian)
            ushort x = (ushort)(bytes[offset + 0] | (bytes[offset + 1] << 8));
            ushort y = (ushort)(bytes[offset + 2] | (bytes[offset + 3] << 8));
            ushort z = (ushort)(bytes[offset + 4] | (bytes[offset + 5] << 8));
            
            return DecodeNorm16(x, y, z);
        }

        // ====================================================================
        // FLOAT32: 32+32+32 bits (12 bytes total)
        // ====================================================================
        // Formato: 3 floats IEEE 754
        // Precisión: Completa (32 bits float)
        // Rango: Cualquier valor float válido
        // Uso típico: Máxima precisión, sin compresión
        // ====================================================================
        
        /// <summary>
        /// Decodifica formato Float32 (sin compresión).
        /// 
        /// Simplemente retorna los 3 floats como float3.
        /// No hay conversión necesaria, solo empaquetado.
        /// 
        /// Nota: Este formato NO comprime, usa 12 bytes por posición.
        /// </summary>
        /// <param name="x">Componente X (float)</param>
        /// <param name="y">Componente Y (float)</param>
        /// <param name="z">Componente Z (float)</param>
        /// <returns>Vector3 con valores originales</returns>
        public static float3 DecodeFloat32(float x, float y, float z)
        {
            // Sin conversión necesaria - ya son floats
            return new float3(x, y, z);
        }
        
        /// <summary>
        /// Sobrecarga de DecodeFloat32 para trabajar con array de bytes.
        /// Lee 12 bytes consecutivos como 3 floats (little-endian).
        /// </summary>
        /// <param name="bytes">Array de bytes (mínimo 12 bytes)</param>
        /// <param name="offset">Offset inicial en el array</param>
        /// <returns>Vector3 decodificado</returns>
        public static float3 DecodeFloat32FromBytes(byte[] bytes, int offset)
        {
            // Leer 3 floats usando BitConverter (respeta endianness de sistema)
            float x = System.BitConverter.ToSingle(bytes, offset + 0);
            float y = System.BitConverter.ToSingle(bytes, offset + 4);
            float z = System.BitConverter.ToSingle(bytes, offset + 8);
            
            return new float3(x, y, z);
        }

        // ====================================================================
        // NORM6: 6+6+6 bits (18 bits en 3 bytes)
        // ====================================================================
        // Formato: [6 bits X][6 bits Y][6 bits Z] + padding
        // Precisión: 64 pasos por componente (baja)
        // Rango: [-1, 1] normalizado
        // Uso típico: Máxima compresión, baja precisión aceptable
        // ====================================================================
        
        /// <summary>
        /// Decodifica formato Norm6 (6+6+6 bits) desde 3 bytes.
        /// 
        /// Estructura:
        /// Byte 0 bits 7-2: X (6 bits) → [0, 63] → [-1, 1]
        /// Byte 1 bits 7-2: Y (6 bits) → [0, 63] → [-1, 1]
        /// Byte 2 bits 7-2: Z (6 bits) → [0, 63] → [-1, 1]
        /// 
        /// ADVERTENCIA: Baja precisión (solo 64 pasos).
        /// Puede causar cuantización visible.
        /// </summary>
        /// <param name="xByte">Byte conteniendo X (bits 7-2)</param>
        /// <param name="yByte">Byte conteniendo Y (bits 7-2)</param>
        /// <param name="zByte">Byte conteniendo Z (bits 7-2)</param>
        /// <returns>Vector3 decodificado en rango [-1, 1]</returns>
        public static float3 DecodeNorm6(byte xByte, byte yByte, byte zByte)
        {
            // Extraer 6 bits superiores de cada byte
            uint x = (uint)(xByte >> 2) & 0x3F;  // 0x3F = 63 (6 bits)
            uint y = (uint)(yByte >> 2) & 0x3F;
            uint z = (uint)(zByte >> 2) & 0x3F;
            
            // Normalizar de [0, 63] a [-1, 1]
            const float scale = 1.0f / 63.0f;
            return new float3(
                (x * scale) * 2.0f - 1.0f,
                (y * scale) * 2.0f - 1.0f,
                (z * scale) * 2.0f - 1.0f
            );
        }

        // ====================================================================
        // UTILIDADES: Información de formatos
        // ====================================================================
        
        /// <summary>
        /// Retorna el tamaño en bytes de un formato dado.
        /// Útil para calcular offsets en buffers.
        /// </summary>
        /// <param name="format">Formato de posición de aras-p</param>
        /// <returns>Bytes por vector en ese formato</returns>
        public static int GetVectorSize(GaussianSplatAsset.VectorFormat format)
        {
            switch (format)
            {
                case GaussianSplatAsset.VectorFormat.Norm11:
                    return 4;  // 32 bits = 4 bytes
                
                case GaussianSplatAsset.VectorFormat.Norm16:
                    return 6;  // 16+16+16 bits = 6 bytes
                
                case GaussianSplatAsset.VectorFormat.Float32:
                    return 12; // 32+32+32 bits = 12 bytes
                
                case GaussianSplatAsset.VectorFormat.Norm6:
                    return 3;  // 6+6+6 bits = 3 bytes (redondeado)
                
                default:
                    UnityEngine.Debug.LogError($"[VectorDecoder] Formato desconocido: {format}");
                    return 4;  // Fallback a 4 bytes
            }
        }
        
        /// <summary>
        /// Retorna descripción legible del formato.
        /// Útil para debugging y logs.
        /// </summary>
        public static string GetFormatDescription(GaussianSplatAsset.VectorFormat format)
        {
            switch (format)
            {
                case GaussianSplatAsset.VectorFormat.Norm11:
                    return "Norm11 (11+11+10 bits, 4 bytes, precisión media)";
                
                case GaussianSplatAsset.VectorFormat.Norm16:
                    return "Norm16 (16+16+16 bits, 6 bytes, alta precisión)";
                
                case GaussianSplatAsset.VectorFormat.Float32:
                    return "Float32 (32+32+32 bits, 12 bytes, precisión completa)";
                
                case GaussianSplatAsset.VectorFormat.Norm6:
                    return "Norm6 (6+6+6 bits, 3 bytes, baja precisión)";
                
                default:
                    return $"Desconocido ({format})";
            }
        }
    }
}
