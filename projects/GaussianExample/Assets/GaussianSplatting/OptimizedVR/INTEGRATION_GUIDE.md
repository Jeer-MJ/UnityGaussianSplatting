# Guía de Integración - VR Gaussian Splatting para Proyecto Actual

## 📋 Estado Actual del Proyecto

Ya tienes:
- ✅ Unity con Meta XR SDK configurado
- ✅ Paquete `org.nesnausk.gaussian-splatting` (aras-p) instalado
- ✅ Splat assets importados (ceramic splat en GaussianAssets/)
- ✅ Sistema VR optimizado creado en `Assets/GaussianSplatting/OptimizedVR/`

---

## 🚀 Pasos de Integración

### PASO 1: Verificar Shaders
```
1. En Unity, ir a Project → Assets → GaussianSplatting → OptimizedVR → Shaders
2. Verificar que existen:
   - GSKeyValue.shader
   - RadixSort.shader
3. Hacer clic en cada uno y verificar que compilan sin errores
4. Si hay errores, revisar Console y corregir
```

### PASO 2: Crear RenderTextures y Materiales

#### Opción A: Usando el Editor Custom (Recomendado)
```
1. Hierarchy → Create Empty → "VRGaussianSplatManager"
2. Add Component → VRGaussianSplatManager
3. En Inspector:
   - Click "Setup RadixSort Component"
   - Click "Create Optimized RenderTextures"
4. Verificar que se crearon en:
   - Assets/GaussianSplatting/OptimizedVR/Resources/
```

#### Opción B: Manual
```
1. Crear Materials:
   a. Project → Create → Material → "ComputeKeyValues"
      - Shader: GaussianSplatting/VR/ComputeKeyValue
      - Guardar en OptimizedVR/Resources/

   b. Project → Create → Material → "RadixSort"
      - Shader: GaussianSplatting/VR/RadixSort
      - Guardar en OptimizedVR/Resources/

2. Crear RenderTextures:
   a. Project → Create → Render Texture → "KeyValues0"
      - Size: 1024 x 1024
      - Color Format: RG Float (32-bit per channel)
      - Depth Buffer: None
      - Filter Mode: Point
      - Wrap Mode: Clamp
      - Mipmaps: No
      
   b. Crear "KeyValues1" (misma configuración que KeyValues0)
   
   c. Project → Create → Render Texture → "PrefixSums"
      - Size: 1024 x 1024
      - Color Format: R Float (16-bit)
      - Depth Buffer: None
      - Filter Mode: Point
      - Wrap Mode: Clamp
      - **Mipmaps: YES** ← IMPORTANTE
      - Auto Generate Mips: No (lo hacemos manualmente)
```

### PASO 3: Configurar Escena de Prueba

#### A. Abrir GSTestScene.unity
```
1. Project → Assets → GSTestScene.unity (doble click)
2. Scene debería tener el ceramic splat ya importado
```

#### B. Encontrar GaussianSplatRenderer Existente
```
1. Hierarchy → buscar GameObject con componente GaussianSplatRenderer
2. Si no existe:
   - Hierarchy → 3D Object → Create Empty → "CeramicSplat"
   - Add Component → GaussianSplatRenderer
   - Asignar el asset ceramic en el campo "Asset"
```

#### C. Agregar VR Manager
```
1. Hierarchy → Create Empty → "VRGaussianSplatManager"
2. Add Component → VRGaussianSplatManager
3. En Inspector:
   - Splat Objects: Drag el GameObject con GaussianSplatRenderer
   - Active Splat Index: 0
   - Min Sort Distance: 0
   - Max Sort Distance: 20 (ajustar según tamaño del splat)
   - Camera Position Quantization: 0.05
   - Always Update: ❌ false
   - Separate Eye Sorting: ❌ false
   - IPD: 0.064
   - Debug Log: ✅ true (para testing)
```

#### D. Configurar RadixSortVR
```
1. En VRGaussianSplatManager GameObject, buscar componente RadixSortVR
2. Asignar:
   - Sorting Passes: 3
   - Compute Key Values: Material "ComputeKeyValues"
   - Radix Sort: Material "RadixSort"
   - Key Values 0: RenderTexture "KeyValues0"
   - Key Values 1: RenderTexture "KeyValues1"
   - Prefix Sums: RenderTexture "PrefixSums"
```

#### E. (Opcional) Agregar Adapter
```
1. Seleccionar GameObject con GaussianSplatRenderer
2. Add Component → GaussianSplatRendererVRAdapter
3. Asignar:
   - VR Manager: Drag "VRGaussianSplatManager"
   - Use VR Sorting: ✅ true
```

### PASO 4: Configurar Cámara VR

#### Si ya tienes OVRCameraRig:
```
1. Verificar que tiene tag "MainCamera"
2. Position cerca del splat (ej: (0, 1.6, -2))
```

#### Si NO tienes XR rig:
```
1. Hierarchy → XR → XR Origin (Action-based) o crear OVRCameraRig
2. Tag la cámara como "MainCamera"
3. Position: (0, 1.6, -2) o según necesidad
```

### PASO 5: Test en Editor

```
1. Click Play
2. Revisar Console:
   - Debe decir: "[VRGaussianSplatManager] Initialized. VR Active: true/false"
   - NO debe haber errores rojos
3. En Scene View:
   - Debe verse el splat renderizado
   - Mover cámara Main debe triggear re-sorts
4. En Game View:
   - Splat debe verse correctamente
5. Frame Debugger (Window → Analysis → Frame Debugger):
   - Click "Enable"
   - Buscar "ComputeKeyValue" y "RadixSort" passes
   - Verificar que se ejecutan
```

### PASO 6: Verificar Performance en Editor

```
1. Profiler (Window → Analysis → Profiler)
2. Click Record
3. Mover cámara
4. Revisar:
   - CPU time: Buscar "RadixSortVR" en timeline
   - GPU time: Revisar rendering
   - Frame time debería ser < 16ms en editor (60 Hz)
```

---

## 🔍 Troubleshooting Integración

### ERROR: "RadixSortVR component not found or invalid"
**Solución:**
```
1. Verificar que VRGaussianSplatManager tiene componente RadixSortVR
2. Verificar que todos los campos de RadixSortVR están asignados
3. Verificar que los shaders compilaron correctamente
```

### ERROR: "Shader not found: GaussianSplatting/VR/..."
**Solución:**
```
1. Reimportar shaders: Right-click shader → Reimport
2. Verificar que están en Shaders/ folder
3. Verificar que no hay errores de compilación en Console
```

### WARNING: "VRGaussianSplatManager not found in scene"
**Solución:**
```
1. Crear GameObject "VRGaussianSplatManager"
2. Add Component → VRGaussianSplatManager
3. Configurar según PASO 3
```

### No se ve sorting funcionando
**Solución:**
```
1. Activar "Debug Log" en VRGaussianSplatManager
2. Mover cámara
3. Console debe mostrar: "Sorted at position ..."
4. Si no:
   - Verificar Camera.main existe
   - Verificar que Splat Object está en array
   - Verificar que asset está asignado
```

### Splat se ve pero no cambia al mover cámara
**Solución:**
```
1. Verificar cameraPositionQuantization (probar 0.01 muy sensible)
2. Activar "Always Update" (solo test)
3. Revisar que RenderTextures no son null
4. Frame Debugger para ver si shaders se ejecutan
```

---

## 🎯 Testing Checklist

Antes de hacer build para Quest 3:

- [ ] Shaders compilan sin errores
- [ ] RenderTextures creadas con settings correctos
- [ ] Materials asignados correctamente
- [ ] VRGaussianSplatManager configurado
- [ ] RadixSortVR tiene todos los assets
- [ ] Splat object asignado en array
- [ ] Cámara VR configurada y taggeada
- [ ] Play mode funciona sin errores
- [ ] Sorting se ejecuta al mover cámara (ver Console con Debug Log)
- [ ] Frame Debugger muestra passes de sorting
- [ ] Profiler muestra tiempos razonables (<5ms sorting)

---

## 📱 Build para Quest 3

### Configuración de Build
```
1. File → Build Settings
2. Platform: Android
3. Texture Compression: ASTC
4. Run Device: Quest 3 (via USB o Wireless)
5. Switch Platform (si no está ya)
```

### Player Settings
```
1. Edit → Project Settings → Player → Android tab
2. Other Settings:
   - Color Space: Linear ← IMPORTANTE
   - Auto Graphics API: ❌ Disable
   - Graphics APIs: Vulkan (first), OpenGLES3 (fallback)
   - Minimum API Level: Android 10.0 (API 29)
   - Target API Level: Android 13.0+ (API 33+)
   - Scripting Backend: IL2CPP
   - ARM64: ✅ Enable
   
3. Quality Settings:
   - Level: Medium o Custom
   - VSync: Don't Sync (XR maneja esto)
   - Anti Aliasing: Disabled
   
4. XR Settings:
   - Stereo Rendering Mode: Single Pass Instanced (si funciona) o Multiview
```

### Primera Build
```
1. Build and Run (puede tardar 10-20 min primera vez)
2. Quest 3 debe estar conectado via USB o mismo WiFi
3. Enable Developer Mode en Quest
4. Aceptar USB debugging en Quest
```

### Testing en Quest
```
1. Ponerse el headset
2. App debería arrancar automáticamente
3. Verificar:
   - Splat se ve correctamente en ambos ojos
   - Performance fluida (no stuttering)
   - Sorting funciona al mover cabeza
4. Usar Oculus Developer Hub:
   - Ver logcat en tiempo real
   - Performance metrics (CPU/GPU/FPS)
```

---

## 📊 Optimización Post-Build

Si performance no es buena:

### 1. Reducir Sorting Passes
```csharp
// En RadixSortVR
sortingPasses = 2; // 8-bit, menos precisión pero más rápido
```

### 2. Incrementar Quantization
```csharp
// En VRGaussianSplatManager
cameraPositionQuantization = 0.1f; // 10cm, menos updates
```

### 3. Reducir Resolución de Sorting
```csharp
// Recrear RenderTextures con 512x512
// Soporta hasta ~256K splats
```

### 4. Reducir Cantidad de Splats
```
- Reimportar splat con menor densidad
- O usar LOD system (futuro)
```

---

## 🎓 Próximos Pasos

Una vez funcionando básicamente:

1. **Ajustar parámetros visuales**
   - maxSortDistance según tamaño de escena
   - Exposure, gamma en GaussianSplatRenderer

2. **Optimizar performance**
   - Profile en Quest real
   - Ajustar sorting passes vs calidad

3. **Agregar más splats**
   - Importar múltiples escenas
   - Usar splatObjects array para cambiar entre ellos

4. **Interacción**
   - Agregar XR Interaction Toolkit
   - Permitir teletransporte, grab, etc.

5. **Optimizaciones avanzadas**
   - LOD system
   - Frustum culling
   - Occlusion culling

---

## ✅ Validación Final

Sistema está correctamente integrado si:

✅ Play mode sin errores  
✅ Sorting se ejecuta (Console log)  
✅ Frame Debugger muestra passes  
✅ Performance <16ms en editor  
✅ Build en Quest 3 exitoso  
✅ 72+ Hz en Quest 3  
✅ Sorting funciona en VR  
✅ Ambos ojos ven correctamente  

---

**¡Éxito! Sistema VR optimizado funcionando.**
