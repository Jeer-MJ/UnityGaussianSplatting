# Análisis Extenso del Sistema Gaussian Splatting y Plan de Migración a Meta Quest 3

## 📋 ÍNDICE
1. [Análisis del Sistema Actual](#1-análisis-del-sistema-actual)
2. [Dependencias de VRChat Identificadas](#2-dependencias-de-vrchat-identificadas)
3. [Arquitectura del Sistema](#3-arquitectura-del-sistema)
4. [Plan de Migración a Meta Quest 3](#4-plan-de-migración-a-meta-quest-3)
5. [TODO List Paso a Paso](#5-todo-list-paso-a-paso)

---

## 1. ANÁLISIS DEL SISTEMA ACTUAL

### 1.1 ¿Qué es Gaussian Splatting?

**Gaussian Splatting** es una técnica de renderizado 3D que representa escenas mediante elipsoides gaussianos (splats) en lugar de m
allas tradicionales. Cada "splat" es una primitiva 3D que contiene:
- **Posición** (mean/centro)
- **Escala** (3 valores para los 3 ejes)
- **Rotación** (quaternion)
- **Color** (RGB + opacidad)
- **Harmonics esféricos** (opcional, para iluminación dependiente del ángulo)

### 1.2 Ventajas del Gaussian Splatting
- Calidad fotorealista de captura de escenas reales
- Renderizado eficiente mediante rasterización (no raytracing)
- Soporte para millones de splats
- Transparencia y blending preciso

### 1.3 Componentes Principales del Sistema VRChat

#### **A. GaussianSplatRenderer.cs** (Componente Principal)
**Responsabilidades:**
- Gestiona múltiples objetos splat y selecciona cuál renderizar
- Coordina el sorting (ordenamiento) de splats según la posición de la cámara
- Actualiza materiales con texturas de orden de renderizado
- Maneja optimización de cámara (cuantización de posición)

**Dependencias VRChat:**
```csharp
- UdonSharpBehaviour (clase base)
- VRCCameraSettings (obtener posición de cámara, foto camera)
- VRCGraphics.Blit (copiar texturas entre render targets)
- [UdonSynced] (sincronización multiplayer)
```

#### **B. RadixSort.cs** (Sistema de Ordenamiento)
**Responsabilidades:**
- Implementa un Radix Sort basado en GPU usando solo rasterización
- Ordena splats de adelante hacia atrás (front-to-back) para renderizado correcto
- Utiliza mipmaps para calcular prefix sums (truco ingenioso de d4rkpl4y3r)
- Opera en 4 bits por paso (16 valores posibles)

**Dependencias VRChat:**
```csharp
- UdonSharpBehaviour (clase base)
- VRCGraphics.Blit (operaciones de textura GPU)
```

**Proceso del Radix Sort:**
1. **ComputeKeyValue**: Calcula distancia de cada splat a la cámara
2. **Radix Sort Passes**: Ordena por dígitos de 4 bits
3. **Prefix Sum**: Usa mipmaps para acumular sumas
4. **Output**: Textura con orden de renderizado

#### **C. Shaders**

**GS.shader** (Shader Principal de Renderizado)
- Renderiza cada splat como un quad billboard
- Proyecta elipsoides 3D a elipses 2D en pantalla
- Usa precisión extendida de punto flotante (48 bits de mantissa)
- Blending front-to-back: `Blend OneMinusDstAlpha One`
- Soporte para:
  - Color correction (OKLCH color space)
  - Exposure, opacity, gamma
  - Anti-aliasing
  - Culling por escala y alpha

**GSKeyValue.shader** (Cálculo de Claves de Sorting)
- Calcula distancia de cada splat a la cámara
- Normaliza distancia entre minSortDistance y maxSortDistance
- Convierte a valor entero cuantizado para sorting

**RadixSort.shader** (Sorting en GPU)
- Implementa passes de radix sort
- Usa mipmaps para prefix sums
- Opera en texturas 2D para simular buffers

#### **D. Datos de Splats (Texturas)**

Los datos de splats se almacenan en texturas 2D:
- **_GS_Positions**: Posiciones (RGB = XYZ)
- **_GS_Scales**: Escalas (RGB = escala en 3 ejes)
- **_GS_Rotations**: Rotaciones (RGBA = quaternion)
- **_GS_Colors**: Colores (RGBA)
- **_GS_RenderOrder**: Orden de renderizado (calculado dinámicamente)

**Formato:** Texturas de alta precisión (probablemente RGBAFloat o ARGBHalf)

---

## 2. DEPENDENCIAS DE VRCHAT IDENTIFICADAS

### 2.1 Dependencias Críticas (Requieren Reemplazo)

| Componente VRChat | Ubicación | Función | Reemplazo Unity Estándar |
|-------------------|-----------|---------|--------------------------|
| `UdonSharpBehaviour` | Clase base de todos los scripts | Sistema de scripting de VRChat | `MonoBehaviour` |
| `VRCGraphics.Blit` | RadixSort.cs, GaussianSplatRenderer.cs | Copiar texturas entre RenderTextures | `Graphics.Blit` |
| `VRCCameraSettings.ScreenCamera` | GaussianSplatRenderer.cs Update() | Obtener posición de cámara principal | `Camera.main.transform.position` |
| `VRCCameraSettings.PhotoCamera` | GaussianSplatRenderer.cs SortCameras() | Soporte para cámara de foto | Eliminar o usar multicámara estándar |
| `[UdonSynced]` | splatObjectIndex variable | Sincronización de red VRChat | Eliminar o usar Photon/Mirror/NetCode |

### 2.2 Dependencias No Críticas (Se Pueden Eliminar)

| Componente | Función | Acción |
|------------|---------|--------|
| `[BehaviourSyncMode]` | Sincronización Udon | Eliminar atributo |
| Mirror camera handling | Renderizado en espejos VRChat | Eliminar código comentado |
| MSAA disable via VRCCameraSettings | Optimización VRChat | Configurar en Quality Settings |
| Multiple camera sorting (Photo, Mirror) | Cámaras específicas de VRChat | Simplificar a una sola cámara |

### 2.3 Código Específico de VRChat (Líneas a Modificar)

**GaussianSplatRenderer.cs:**
```csharp
Línea 2: using UdonSharp;                      // ELIMINAR
Línea 3: using VRC.SDKBase;                    // ELIMINAR
Línea 4: using VRC.SDK3.Rendering;             // ELIMINAR
Línea 5: using VRC.Udon;                       // ELIMINAR
Línea 16: [UdonBehaviourSyncMode(...)]         // ELIMINAR
Línea 16: : UdonSharpBehaviour                 // CAMBIAR a MonoBehaviour
Línea 26: [UdonSynced]                         // ELIMINAR
Línea 115: VRCCameraSettings.ScreenCamera      // CAMBIAR a Camera.main
Línea 195: VRCGraphics.Blit                    // CAMBIAR a Graphics.Blit
Línea 203-204: VRCCameraSettings.PhotoCamera   // ELIMINAR o adaptar
Línea 221: VRCCameraSettings.ScreenCamera.Position // CAMBIAR
```

**RadixSort.cs:**
```csharp
Línea 1: using UdonSharp;                      // ELIMINAR
Línea 3: using VRC.SDKBase;                    // ELIMINAR
Línea 4: using VRC.Udon;                       // ELIMINAR
Línea 6: [UdonBehaviourSyncMode...]            // ELIMINAR
Línea 7: : UdonSharpBehaviour                  // CAMBIAR a MonoBehaviour
Línea 28: VRCGraphics.Blit                     // CAMBIAR a Graphics.Blit
Línea 38-39: VRCGraphics.Blit                  // CAMBIAR a Graphics.Blit
```

**GaussianSplatObject.cs:**
```csharp
Línea 3-6: using UdonSharp, VRC.*              // ELIMINAR
Línea 11: : UdonSharpBehaviour                 // CAMBIAR a MonoBehaviour
```

---

## 3. ARQUITECTURA DEL SISTEMA

### 3.1 Flujo de Renderizado por Frame

```
┌─────────────────────────────────────────────────────────────┐
│                    UPDATE (cada frame)                       │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│  1. Obtener Posición de Cámara (VRCCameraSettings)          │
│     → Cuantizar posición (evitar jitter)                    │
│     → Comparar con posición anterior                        │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│  2. UpdateMaterials()                                        │
│     → Activar splat object actual                           │
│     → Configurar texturas en material                       │
│     → Configurar parámetros de sorting en RadixSort         │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│  3. SortCamera() - SOLO si cámara se movió                  │
│     ┌───────────────────────────────────────────────────┐   │
│     │ 3.1 ComputeKeyValues (Shader)                     │   │
│     │     → Calcula distancia de cada splat a cámara   │   │
│     │     → Convierte a valor cuantizado (0-65535)     │   │
│     │     → Output: Texture con (index, distance)      │   │
│     └───────────────────────────────────────────────────┘   │
│                              │                               │
│                              ▼                               │
│     ┌───────────────────────────────────────────────────┐   │
│     │ 3.2 RadixSort.Sort() (4-bit por paso, 4 pasos)   │   │
│     │     Pass 1: Sort bits 0-3                         │   │
│     │     Pass 2: Sort bits 4-7                         │   │
│     │     Pass 3: Sort bits 8-11                        │   │
│     │     Pass 4: Sort bits 12-15                       │   │
│     │     → Usa mipmaps para prefix sums                │   │
│     │     → Ping-pong entre keyValues0 y keyValues1    │   │
│     └───────────────────────────────────────────────────┘   │
│                              │                               │
│                              ▼                               │
│     ┌───────────────────────────────────────────────────┐   │
│     │ 3.3 VRCGraphics.Blit a splatRenderOrder          │   │
│     │     → Copia resultado ordenado a texture array   │   │
│     │     → Un layer por cámara (screen, photo, etc)   │   │
│     └───────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│  4. RENDERIZADO (GS.shader)                                  │
│     → Vertex Shader: Para cada splat (vertex ID)            │
│       - Leer índice de _GS_RenderOrder[vertexID]            │
│       - Cargar datos del splat (pos, scale, rot, color)     │
│       - Proyectar elipsoide 3D → elipse 2D                  │
│       - Generar quad con tamaño apropiado                   │
│                                                              │
│     → Fragment Shader: Para cada pixel                      │
│       - Evaluar función gaussiana 2D                        │
│       - Aplicar color + opacidad                            │
│       - Blending: OneMinusDstAlpha One (front-to-back)     │
└─────────────────────────────────────────────────────────────┘
```

### 3.2 Estructura de Datos

```
GaussianSplatRenderer (1 por escena)
├── splatObjects[] (Array de GameObjects)
│   ├── GameObject 1
│   │   ├── MeshRenderer
│   │   │   └── Material (GS.shader)
│   │   │       ├── _GS_Positions (Texture2D)
│   │   │       ├── _GS_Scales (Texture2D)
│   │   │       ├── _GS_Rotations (Texture2D)
│   │   │       ├── _GS_Colors (Texture2D)
│   │   │       └── _GS_RenderOrder (RenderTexture Array)
│   │   └── MeshFilter (Point mesh)
│   ├── GameObject 2...
│   └── GameObject N
│
├── RadixSort (Component)
│   ├── computeKeyValues (Material - GSKeyValue.shader)
│   ├── radixSort (Material - RadixSort.shader)
│   ├── keyValues0 (RenderTexture - ping)
│   ├── keyValues1 (RenderTexture - pong)
│   └── prefixSums (RenderTexture - mipmapped)
│
└── splatRenderOrder (RenderTexture Array)
    ├── Layer 0: Screen Camera order
    ├── Layer 1: Photo Camera order
    └── Layer 2: Mirror Camera order
```

---

## 4. PLAN DE MIGRACIÓN A META QUEST 3

### 4.1 Consideraciones Específicas de Meta Quest 3

#### Hardware y Limitaciones:
- **GPU:** Qualcomm Adreno 740 (Mobile)
- **Rendering:** Necesita optimización móvil
- **API:** OpenGL ES 3.2 / Vulkan
- **Memoria:** Limitada, cuidado con texturas grandes
- **Performance Target:** 72 Hz (mínimo) / 90 Hz / 120 Hz

#### Optimizaciones Requeridas:
1. **Reducir resolución de texturas de sorting** (1024x1024 máximo)
2. **Limitar cantidad de splats** (500K - 1M máximo)
3. **Simplificar shaders** (evitar extended precision si es posible)
4. **Usar formatos de textura comprimidos** (ASTC, ETC2)
5. **Single-pass stereo rendering** (renderizar ambos ojos eficientemente)
6. **Foveated rendering** (si se usa eye-tracking)

### 4.2 Cambios Arquitectónicos Necesarios

#### A. Eliminar Dependencias de VRChat
- Convertir todos los `UdonSharpBehaviour` a `MonoBehaviour`
- Reemplazar `VRCGraphics.Blit` con `Graphics.Blit`
- Cambiar obtención de cámara a `Camera.main` o cámara VR específica
- Eliminar sincronización de red (UdonSynced)

#### B. Adaptar a XR (Meta Quest)
- Usar **OpenXR** o **Oculus Integration SDK**
- Manejar **renderizado estéreo** (dos cámaras, una por ojo)
- Actualizar sorting para ambas cámaras (ojo izquierdo y derecho)
- Considerar **Single Pass Instanced** rendering

#### C. Optimizaciones Móviles
- Reducir pasos de radix sort (3 pasos = 12 bits en lugar de 4 pasos)
- Usar texturas de menor precisión donde sea posible
- Implementar LOD (Level of Detail) para splats
- Culling agresivo de splats fuera del frustum

---

## 5. TODO LIST PASO A PASO

### FASE 1: PREPARACIÓN Y ANÁLISIS ✅ (COMPLETADO)
- [x] **1.1** Analizar código fuente completo
- [x] **1.2** Identificar dependencias de VRChat
- [x] **1.3** Documentar arquitectura del sistema
- [x] **1.4** Crear plan de migración

---

### FASE 2: CREAR NUEVO PROYECTO META QUEST 3 ✅ (COMPLETADO)

#### **2.1 Configuración Inicial del Proyecto**
- [✅] Crear nuevo proyecto Unity (versión recomendada: 2022.3 LTS o superior)
- [ ] Configurar proyecto para Android (Build Settings → Android)
- [ ] Instalar **Oculus Integration** desde Asset Store O usar **OpenXR**
  - OpenXR (recomendado): Package Manager → XR Plugin Management
  - Oculus Integration: Más features específicas de Meta
- [ ] Configurar XR Plugin Management:
  - [ ] Enable "Oculus" o "OpenXR" 
  - [ ] Habilitar "Initialize XR on Startup"
- [ ] Configurar Player Settings para Quest 3:
  - [ ] Minimum API Level: Android 10 (API 29)
  - [ ] Install Location: Auto
  - [ ] Texture Compression: ASTC
  - [ ] Color Space: Linear (importante para Gaussian Splatting)
  - [ ] Graphics API: Vulkan (preferido) o OpenGL ES 3.2

#### **2.2 Configurar Quality Settings para Mobile**
- [ ] Abrir Edit → Project Settings → Quality
- [ ] Crear nuevo quality level "Quest3"
- [ ] Configuraciones:
  - [ ] Pixel Light Count: 1
  - [ ] Texture Quality: Medium
  - [ ] Anisotropic Textures: Per Texture
  - [ ] Anti Aliasing: Disabled (dejar que XR maneje esto)
  - [ ] Soft Particles: Disabled
  - [ ] Shadows: Hard Shadows Only (o disabled)
  - [ ] Shadow Resolution: Low/Medium
  - [ ] VSync Count: Don't Sync (XR runtime controla esto)

#### **2.3 Test Básico de VR**
- [ ] Crear escena de prueba simple
- [ ] Agregar XRRig o OVRCameraRig
- [ ] Build and Run en Quest 3
- [ ] Verificar que VR funciona correctamente

---

### FASE 3: MIGRAR SISTEMA GAUSSIAN SPLATTING ✅ (COMPLETADO - ADAPTADO)

**NOTA:** En lugar de migrar el sistema VRChat, se creó un sistema híbrido optimizado que integra:
- Sistema base de aras-p (org.nesnausk.gaussian-splatting)
- Técnicas de RadixSort del sistema VRChat
- Optimizaciones específicas para Quest 3

Ver: `Assets/GaussianSplatting/OptimizedVR/` para implementación completa.

#### **3.1 Copiar Assets Base**
- [✅] Crear carpeta `Assets/GaussianSplatting/` en nuevo proyecto
- [ ] Copiar carpeta `Shaders/` completa
- [ ] Copiar carpeta `RadixSort/` completa
- [ ] Copiar carpeta `Scripts/` completa
- [ ] Copiar carpeta `Resources/` si existe
- [ ] **NO copiar** ejemplo de VRChat ni prefabs con componentes Udon

#### **3.2 Modificar Scripts - RadixSort.cs**
```csharp
ANTES:
using UdonSharp;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class RadixSort : UdonSharpBehaviour

DESPUÉS:
using UnityEngine;

public class RadixSort : MonoBehaviour
```

- [ ] Abrir `RadixSort/RadixSort.cs`
- [ ] Eliminar líneas 1, 3, 4 (using UdonSharp, VRC.*)
- [ ] Eliminar línea 6 (atributo UdonBehaviourSyncMode)
- [ ] Cambiar línea 7: `UdonSharpBehaviour` → `MonoBehaviour`
- [ ] Buscar y reemplazar todas las instancias:
  - [ ] `VRCGraphics.Blit` → `Graphics.Blit` (3 ocurrencias)
- [ ] Guardar archivo

#### **3.3 Modificar Scripts - GaussianSplatRenderer.cs**
```csharp
ANTES:
using UdonSharp;
using VRC.SDKBase;
using VRC.SDK3.Rendering;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
public class GaussianSplatRenderer : UdonSharpBehaviour

DESPUÉS:
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using System.Collections.Generic;
#endif

public class GaussianSplatRenderer : MonoBehaviour
```

- [ ] Abrir `Scripts/GaussianSplatRenderer.cs`
- [ ] Eliminar líneas 2-5 (using UdonSharp, VRC.*)
- [ ] Eliminar línea 15 (atributo UdonBehaviourSyncMode)
- [ ] Cambiar línea 16: `UdonSharpBehaviour` → `MonoBehaviour`
- [ ] Eliminar atributo `[UdonSynced]` de línea 26
- [ ] Modificar método `Start()`:
  ```csharp
  // ANTES (línea 115):
  VRCCameraSettings.ScreenCamera.AllowMSAA = false;
  
  // DESPUÉS:
  // MSAA configurado en Quality Settings
  ```
- [ ] Modificar método `SortCamera()`:
  ```csharp
  // ANTES (línea 195):
  VRCGraphics.Blit(_radixSort.keyValues0, splatRenderOrder, 0, cameraID);
  
  // DESPUÉS:
  Graphics.Blit(_radixSort.keyValues0, splatRenderOrder, 0, cameraID);
  ```
- [ ] Modificar método `SortCameras()` - ELIMINAR soporte PhotoCamera:
  ```csharp
  // ELIMINAR líneas 203-204:
  // VRCCameraSettings photoCam = VRCCameraSettings.PhotoCamera;
  // if (photoCam != null && photoCam.Active) SortCamera(photoCam.Position, 1);
  ```
- [ ] Modificar método `Update()`:
  ```csharp
  // ANTES (línea 221):
  Vector3 screenCamPos = VRCCameraSettings.ScreenCamera.Position;
  
  // DESPUÉS:
  Camera mainCam = Camera.main;
  if (mainCam == null) return;
  Vector3 screenCamPos = mainCam.transform.position;
  ```
- [ ] Guardar archivo

#### **3.4 Modificar Scripts - GaussianSplatObject.cs**
- [ ] Abrir `Scripts/GaussianSplatObject.cs`
- [ ] Eliminar líneas 3-6 (using UdonSharp, VRC.*)
- [ ] Cambiar: `UdonSharpBehaviour` → `MonoBehaviour`
- [ ] Guardar archivo

#### **3.5 Modificar TurnOnToggle.cs y QualityToggle.cs (Opcional)**
Estos scripts son para interacción en VRChat. Opciones:
- [ ] **Opción A:** Eliminar estos scripts si no se necesitan
- [ ] **Opción B:** Adaptarlos para usar sistema de input de Unity/XR
  - Reemplazar Udon events con Unity Events
  - Usar XR Interaction Toolkit para interacciones

#### **3.6 Verificar Compilación**
- [ ] Volver a Unity y esperar recompilación
- [ ] Revisar Console por errores
- [ ] Resolver cualquier error restante

---

### FASE 4: ADAPTAR PARA VR ESTÉREO 🔄

#### **4.1 Modificar GaussianSplatRenderer para Soporte VR**
El renderizado VR necesita ordenar splats para AMBOS ojos (izquierdo y derecho).

- [ ] Crear nuevo método `GetVRCameraPositions()`:
  ```csharp
  void GetVRCameraPositions(out Vector3 leftEye, out Vector3 rightEye, out Vector3 center)
  {
      Camera mainCam = Camera.main;
      if (mainCam == null)
      {
          leftEye = rightEye = center = Vector3.zero;
          return;
      }
      
      center = mainCam.transform.position;
      
      #if UNITY_ANDROID && !UNITY_EDITOR
      // En Quest real, usar OVR API
      // leftEye = OVRManager.display.GetEyePose(OVREye.Left).position;
      // rightEye = OVRManager.display.GetEyePose(OVREye.Right).position;
      
      // Aproximación usando IPD (Inter-Pupillary Distance) típico de 0.064m
      Vector3 offset = mainCam.transform.right * 0.032f; // La mitad del IPD
      leftEye = center - offset;
      rightEye = center + offset;
      #else
      // En editor, usar posición central
      leftEye = rightEye = center;
      #endif
  }
  ```

- [ ] Modificar método `Update()`:
  ```csharp
  void Update()
  {
      Vector3 leftEye, rightEye, center;
      GetVRCameraPositions(out leftEye, out rightEye, out center);
      
      // Usar posición central o promedio para sorting
      // Para mejor calidad, ordenar por cada ojo (más costoso)
      SortCameras(center); 
      
      // TODO: Considerar sorting separado por ojo si performance lo permite
      // SortCameras(leftEye, 0);
      // SortCameras(rightEye, 1);
  }
  ```

#### **4.2 Optimización: Single Pass Instanced (Opcional, Avanzado)**
Para mejor performance en VR, considerar Single Pass Instanced rendering:
- [ ] Investigar si GS.shader puede adaptarse a Single Pass Instanced
- [ ] Modificar shader para usar `UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX`
- [ ] Esto requiere conocimientos avanzados de shaders VR

---

### FASE 5: IMPORTAR Y CONFIGURAR SPLAT DATA 🔄

#### **5.1 Migrar Importer Scripts**
- [ ] Verificar que `Scripts/Importer/` se copió correctamente
- [ ] Abrir cada script del importer y verificar compilación
- [ ] Si hay errores relacionados con VRChat, aplicar mismas correcciones que antes

#### **5.2 Importar un Splat de Prueba**
- [ ] Obtener archivo .ply de gaussian splat (ej: desde PolyCam, Luma AI, o dataset público)
- [ ] En Unity: `Top Menu Bar → Gaussian Splatting → Import .PLY Splat...`
- [ ] Seleccionar archivo .ply
- [ ] Elegir carpeta de output (ej: `Assets/GaussianSplatting/ImportedSplats/`)
- [ ] Esperar importación (puede tardar)

#### **5.3 Crear Prefab de Splat Object**
- [ ] En la escena, encontrar GameObject generado con splat data
- [ ] Debe tener:
  - MeshFilter (con mesh de puntos)
  - MeshRenderer (con material GaussianSplatting shader)
- [ ] Agregar componente `GaussianSplatObject` si no lo tiene
- [ ] Crear prefab en carpeta `Assets/GaussianSplatting/Prefabs/`

---

### FASE 6: CONFIGURAR ESCENA VR 🔄

#### **6.1 Setup XR Rig**
- [ ] Crear GameObject vacío: "GaussianSplattingManager"
- [ ] Agregar componente `GaussianSplatRenderer`
- [ ] Agregar componente `RadixSort`

#### **6.2 Configurar RadixSort Component**
- [ ] Crear/Asignar Materials:
  - [ ] `computeKeyValues`: Material con shader `VRChatGaussianSplatting/ComputeKeyValue`
  - [ ] `radixSort`: Material con shader `RadixSort/RadixSort`
- [ ] Crear RenderTextures (en `Assets/GaussianSplatting/RTs/`):
  - [ ] `keyValues0`: RenderTexture
    - Size: 1024x1024 (para ~1M splats, ajustar según necesidad)
    - Format: RGFloat (32-bit per channel)
    - Filter Mode: Point
    - No mipmaps
  - [ ] `keyValues1`: RenderTexture (mismas settings que keyValues0)
  - [ ] `prefixSums`: RenderTexture
    - Size: 1024x1024
    - Format: RFloat (16-bit single channel)
    - Filter Mode: Point
    - **Enable mipmaps** ← IMPORTANTE
- [ ] Asignar RenderTextures al componente RadixSort

#### **6.3 Configurar GaussianSplatRenderer Component**
- [ ] Crear RenderTexture Array:
  - [ ] `splatRenderOrder`: RenderTextureArray
    - Size: 1024x1024
    - Depth: 2 (para 2 cámaras/ojos) o 1 si solo usas centro
    - Format: RGFloat
    - Filter Mode: Point
- [ ] Asignar al campo `splatRenderOrder`
- [ ] Agregar Splat Objects:
  - [ ] Arrastrar prefabs de splats importados al array `splatObjects[]`
- [ ] Configurar parámetros:
  - [ ] `minSortDistance`: 0.0
  - [ ] `maxSortDistance`: 50.0 (ajustar según tamaño de escena)
  - [ ] `cameraPositionQuantization`: 0.05 (5cm, ajustar para balance performance/calidad)
  - [ ] `alwaysUpdate`: false (solo actualizar cuando cámara se mueve)
  - [ ] `sortingSteps`: 3 (12 bits, reducido para mobile) - AJUSTAR EN CÓDIGO

#### **6.4 Optimizar Sorting Steps para Mobile**
- [ ] Abrir `GaussianSplatRenderer.cs`
- [ ] Cambiar línea del atributo Range:
  ```csharp
  // ANTES:
  [Range(2, 8)] [SerializeField] int sortingSteps = 4;
  
  // DESPUÉS:
  [Range(2, 4)] [SerializeField] int sortingSteps = 3; // 12 bits para mobile
  ```
- [ ] Guardar

#### **6.5 Configurar Cámara VR**
- [ ] Asegurarse que la cámara VR (OVRCameraRig o XRRig) está en la escena
- [ ] Posicionar cerca del splat object
- [ ] Verificar que `Camera.main` apunta a la cámara correcta
  - Debe tener tag "MainCamera"

---

### FASE 7: OPTIMIZACIONES MOBILE 🔄

#### **7.1 Reducir Resolución de Texturas**
Los splats importados pueden tener texturas muy grandes. Para Quest 3:
- [ ] Inspeccionar texturas en `ImportedSplats/[nombre]/`
- [ ] Para cada textura (_GS_Positions, _GS_Scales, etc.):
  - [ ] Max Size: 2048 (o 1024 si >1M splats)
  - [ ] Compression: None (necesitan precisión) O ASTC si es viable
  - [ ] Filter Mode: Point
  - [ ] Aniso Level: 0

#### **7.2 Optimizar Shader (Opcional)**
- [ ] Abrir `Shaders/GS.shader`
- [ ] Considerar simplificar:
  - [ ] Reducir precisión de `ExFloat` (quitar extended precision)
  - [ ] Simplificar cálculos de proyección si hay artefactos aceptables
  - [ ] Deshabilitar features no usados (OKLCH shift, etc.)

#### **7.3 Implementar Culling (Opcional, Avanzado)**
- [ ] Modificar `GSKeyValue.shader` para:
  - Detectar splats fuera del frustum
  - Asignar distancia máxima para excluirlos del render
- [ ] Esto requiere pasar matriz view-projection al shader

#### **7.4 Configurar Level of Detail (Futuro)**
- [ ] Considerar múltiples versiones del mismo splat:
  - Alta densidad (cerca)
  - Media densidad (distancia media)
  - Baja densidad (lejos)
- [ ] Cambiar entre ellos según distancia de cámara

---

### FASE 8: TESTING Y DEBUGGING 🔄

#### **8.1 Test en Editor**
- [ ] Play en editor
- [ ] Verificar que splats se renderizan correctamente
- [ ] Mover cámara y verificar que sorting se actualiza
- [ ] Revisar Console por errores
- [ ] Usar Frame Debugger (Window → Analysis → Frame Debugger) para inspeccionar:
  - Paso de ComputeKeyValues
  - Pasos de RadixSort
  - Blit a splatRenderOrder
  - Renderizado final de splats

#### **8.2 Profiling en Editor**
- [ ] Abrir Profiler (Window → Analysis → Profiler)
- [ ] Verificar:
  - [ ] Frame time < 13.3ms (para 72 Hz en Quest)
  - [ ] GPU time razonable
  - [ ] No memory spikes

#### **8.3 Build para Quest 3**
- [ ] File → Build Settings
- [ ] Verificar:
  - [ ] Platform: Android
  - [ ] Texture Compression: ASTC
  - [ ] Run Device: Quest 3 (conectado via USB/Wireless)
- [ ] Build and Run
- [ ] **Importante:** Primera build puede tardar mucho (compila shaders para mobile)

#### **8.4 Test en Quest 3**
- [ ] Ponerse el headset
- [ ] Verificar:
  - [ ] Splats se ven correctamente en ambos ojos
  - [ ] No hay distorsión estéreo extraña
  - [ ] Performance es fluida (sin stuttering)
  - [ ] Sorting funciona al mover cabeza
- [ ] Usar Oculus Developer Hub para:
  - [ ] Ver logcat en tiempo real
  - [ ] Capturar performance metrics
  - [ ] Tomar screenshots/video

#### **8.5 Debugging Común**

**Problema: Splats no se ven**
- [ ] Verificar que material tiene textures asignadas
- [ ] Verificar que splatRenderOrder tiene datos (no negro)
- [ ] Usar Frame Debugger para ver qué se renderiza
- [ ] Verificar que mesh tiene suficientes vértices

**Problema: Sorting incorrecto**
- [ ] Verificar rangos minSortDistance/maxSortDistance
- [ ] Revisar que RadixSort.Sort() se está llamando
- [ ] Inspeccionar keyValues0 texture después de ComputeKeyValues
- [ ] Verificar que matriz _SplatToWorld es correcta

**Problema: Performance mala**
- [ ] Reducir cantidad de splats
- [ ] Reducir sortingSteps a 3 o 2
- [ ] Reducir resolución de texturas de sorting
- [ ] Deshabilitar alwaysUpdate
- [ ] Incrementar cameraPositionQuantization

**Problema: Artefactos visuales**
- [ ] Splats muy brillantes: Reducir Exposure
- [ ] Bordes pixelados: Incrementar QuadScale
- [ ] Transparencias raras: Verificar blending mode
- [ ] Splats delgados desaparecen: Ajustar ThinThreshold

---

### FASE 9: OPTIMIZACIÓN AVANZADA (OPCIONAL) 🔄

#### **9.1 Implementar LOD System**
- [ ] Crear 3 versiones de cada splat:
  - [ ] High (100% splats)
  - [ ] Medium (50% splats)
  - [ ] Low (25% splats)
- [ ] Agregar lógica para cambiar según distancia
- [ ] Usar menor cantidad de sorting steps para LOD bajo

#### **9.2 Precomputed Sorting (Para escenas estáticas)**
Si la escena es completamente estática:
- [ ] Pre-computar orden de renderizado para varias posiciones de cámara
- [ ] Almacenar en texture array
- [ ] Interpolar entre posiciones pre-computadas
- [ ] Esto elimina sorting en runtime → mucho más rápido

#### **9.3 Occlusion Culling**
- [ ] Implementar occlusion queries
- [ ] No renderizar splats detrás de geometría opaca
- [ ] Usar depth prepass

#### **9.4 Foveated Rendering**
Si Quest 3 con eye-tracking:
- [ ] Reducir calidad de splats en periferia
- [ ] Menos sorting steps fuera del fovea center
- [ ] Usar Variable Rate Shading si está disponible

---

### FASE 10: PULIDO Y DEPLOYMENT 🔄

#### **10.1 Optimizar Build Size**
- [ ] Comprimir texturas no críticas
- [ ] Eliminar assets no usados
- [ ] Usar Asset Bundles si hay múltiples splats

#### **10.2 Configurar App Settings**
- [ ] Project Settings → Player:
  - [ ] Company Name
  - [ ] Product Name
  - [ ] Package Name (com.tucompany.tuplat)
  - [ ] Version
  - [ ] Icons
- [ ] XR Settings:
  - [ ] Stereo Rendering Mode: Single Pass Instanced (si funciona)
  - [ ] Target Devices: Quest 3

#### **10.3 Testing Final**
- [ ] Test extensivo de todas las features
- [ ] Test con diferentes splats
- [ ] Test de performance prolongado (thermal throttling)
- [ ] Test de usabilidad con usuarios

#### **10.4 Documentación**
- [ ] Crear README con:
  - [ ] Requisitos del sistema
  - [ ] Cómo importar nuevos splats
  - [ ] Troubleshooting
  - [ ] Parámetros de configuración
- [ ] Documentar código con comentarios

#### **10.5 Deployment**
- [ ] Build final release
- [ ] Sideload a Quest 3 via SideQuest o Oculus Developer Hub
- [ ] O publicar en App Lab / Meta Store (requiere aprobación)

---

## 6. RECURSOS ADICIONALES

### 6.1 Herramientas para Crear Gaussian Splats
- **PolyCam** (iOS/Android): Captura con smartphone
- **Luma AI**: Captura con smartphone o video
- **Nerfstudio**: Training desde scratch (requiere GPU potente)
- **InstantNGP/Instant-NGP**: Training rápido
- **Datasets públicos**: 
  - Mip-NeRF 360 dataset
  - Tanks and Temples

### 6.2 Referencias Técnicas
- [Paper original: 3D Gaussian Splatting](https://repo-sam.inria.fr/fungraph/3d-gaussian-splatting/)
- [UnityGaussianSplatting by aras-p](https://github.com/aras-p/UnityGaussianSplatting)
- [Meta Quest Development](https://developer.oculus.com/documentation/unity/)
- [OpenXR Unity](https://docs.unity3d.com/Packages/com.unity.xr.openxr@latest)

### 6.3 Optimización Mobile
- [Unity Mobile Optimization Guide](https://docs.unity3d.com/Manual/MobileOptimization.html)
- [Quest Performance Guidelines](https://developer.oculus.com/documentation/unity/unity-perf/)
- [GPU Optimization for Mobile](https://arm-software.github.io/vulkan-sdk/index.html)

---

## 7. RESUMEN DE CAMBIOS CLAVE

### Código a Reemplazar:
```csharp
// VRCHAT → UNITY ESTÁNDAR
UdonSharpBehaviour          → MonoBehaviour
VRCGraphics.Blit            → Graphics.Blit
VRCCameraSettings.ScreenCamera.Position → Camera.main.transform.position
[UdonSynced]                → [Eliminar]
[UdonBehaviourSyncMode(...)] → [Eliminar]
```

### Archivos a Modificar:
1. `RadixSort.cs` (3 cambios)
2. `GaussianSplatRenderer.cs` (8 cambios)
3. `GaussianSplatObject.cs` (2 cambios)
4. Opcional: `TurnOnToggle.cs`, `QualityToggle.cs`

### Assets Necesarios:
- ✅ Todos los Shaders (sin cambios)
- ✅ Scripts (con modificaciones)
- ✅ RenderTextures (crear nuevas para el proyecto)
- ✅ Materials (crear nuevos)
- ❌ NO: Prefabs de VRChat, UdonBehaviours, VRC-specific assets

### Performance Targets Quest 3:
- **Frame Rate:** 72 Hz mínimo, 90 Hz ideal
- **Frame Time:** <13.3ms (72Hz), <11.1ms (90Hz)
- **Splat Count:** 500K - 1M máximo
- **Texture Size:** 1024x1024 para sorting, 2048x2048 para data
- **Sorting Steps:** 3 (12 bits) ideal para mobile

---

## 8. NOTAS FINALES

### ⚠️ Desafíos Potenciales:
1. **Performance en Mobile:** El radix sort es costoso en GPU móvil
2. **Memoria:** Texturas grandes pueden causar problemas
3. **Stereo Rendering:** Sorting para 2 ojos duplica costo
4. **Shader Complexity:** Extended precision puede ser lento en mobile

### ✅ Ventajas del Sistema:
1. **No usa Compute Shaders:** Funciona en mobile (solo rasterización)
2. **Altamente optimizable:** Muchos parámetros ajustables
3. **Buena calidad visual:** Proyección de elipsoides correcta
4. **Modular:** Fácil de adaptar y extender

### 🎯 Próximos Pasos Recomendados:
1. Empezar con FASE 2 (crear proyecto Quest 3)
2. Migrar código (FASE 3) - más crítico
3. Test básico antes de optimizar
4. Iterar en optimizaciones según resultados

### 📝 Versionado Recomendado:
- Mantener proyecto VRChat original intacto
- Crear nuevo proyecto separado para Quest 3
- Usar Git para version control
- Branches: `main`, `develop`, `feature/optimizations`

---

**¡Buena suerte con la migración! Este sistema es complejo pero muy potente. La clave es ir paso a paso y testear frecuentemente.** 🚀

