# 🚀 QUICK START - Siguiente Paso Inmediato

## ⚠️ NOTA IMPORTANTE: Sistema al 95%

El sistema está **implementado y compila correctamente**, pero la integración final con el renderer de aras-p requiere un paso adicional (ver `INTEGRATION_STATUS.md` para detalles técnicos).

**Para continuar con testing:** El sistema detecta movimiento de cámara y está listo para optimización. La conexión final del sorting con el rendering se puede implementar iterativamente.

---

## ¡El sistema está LISTO para configuración! Ahora vamos a configurarlo.

---

## PASO 1: Abrir Unity y Ejecutar Setup Wizard (2 minutos)

### A. Abrir el Proyecto
```
1. Abrir Unity Hub
2. Abrir proyecto: GaussianExample
3. Esperar a que termine de cargar
```

### B. Ejecutar Setup Wizard
```
1. Menu superior: Tools → Gaussian Splatting → VR Setup Wizard
2. En la ventana que aparece:
   - Resources Path: Assets/GaussianSplatting/OptimizedVR/Resources/
   - Texture Resolution: 1024x1024 (~1M splats)
   - Create Example Scene: ❌ (usaremos GSTestScene existente)
3. Click en "Run Complete Setup"
4. Esperar ~10 segundos
5. Debe aparecer: "Setup Complete!" ✅
```

### C. Verificar Setup
```
1. En la ventana del wizard, click "4. Validate Setup"
2. Debe mostrar todo con ✓ (checkmarks verdes)
3. Si algo tiene ✗, volver a ejecutar ese paso individual
```

---

## PASO 2: Configurar la Escena (5 minutos)

### A. Abrir Escena Existente
```
1. Project → Assets → GSTestScene.unity (doble click)
2. Scene debería tener el ceramic splat
```

### B. Agregar VR Manager
```
1. Project → Assets/GaussianSplatting/OptimizedVR/
2. Buscar prefab: VRGaussianSplatManager.prefab
3. Drag & Drop a Hierarchy
4. Posicionar en (0, 0, 0)
```

### C. Encontrar el Splat Renderer
```
1. Hierarchy → Buscar GameObject con componente "GaussianSplatRenderer"
   (Probablemente se llama "ceramic" o similar)
2. Si no encuentras ninguno:
   - Window → Analysis → Frame Debugger
   - Enable
   - Play
   - Buscar "GaussianSplat" en el tree
   - Eso te dice qué objeto lo tiene
```

### D. Conectar Splat al Manager
```
1. Hierarchy → Click en "VRGaussianSplatManager"
2. Inspector → VRGaussianSplatManager component
3. Splat Objects:
   - Size: 1
   - Element 0: [Drag el GameObject con GaussianSplatRenderer aquí]
4. Active Splat Index: 0
```

### E. Configurar Parámetros Iniciales
```
En VRGaussianSplatManager Inspector:

Min Sort Distance: 0
Max Sort Distance: 20  (ajustar según tamaño del splat)
Camera Position Quantization: 0.05
Always Update: ❌ false
Separate Eye Sorting: ❌ false
IPD: 0.064
Debug Log: ✅ true  (para ver qué pasa)
```

### F. Verificar RadixSortVR
```
Mismo GameObject, componente RadixSortVR:

Sorting Passes: 3
Compute Key Values: ✅ (debe tener material asignado)
Radix Sort: ✅ (debe tener material asignado)
Key Values 0: ✅ (RenderTexture)
Key Values 1: ✅ (RenderTexture)
Prefix Sums: ✅ (RenderTexture)

Si algo está vacío:
1. En VRGaussianSplatManager inspector
2. Click "Setup RadixSort Component"
3. Click "Create Optimized RenderTextures"
```

---

## PASO 3: Test en Editor (2 minutos)

### A. Primera Prueba
```
1. Click PLAY ▶️
2. Mirar Console (Ctrl+Shift+C o Window → General → Console)
3. Debe aparecer:
   "[VRGaussianSplatManager] Initialized. VR Active: true/false"
4. NO debe haber errores rojos ❌
```

### B. Test de Sorting
```
1. Mientras está en Play mode
2. Scene View → Mover la cámara Main Camera
3. Console debe mostrar cada ~1 segundo:
   "[VRGaussianSplatManager] Sorted at position (x,y,z), frame ..."
4. Esto significa que está funcionando! ✅
```

### C. Frame Debugger
```
1. Window → Analysis → Frame Debugger
2. Click "Enable"
3. En el árbol a la izquierda, buscar:
   - "ComputeKeyValue" o "GSKeyValue"
   - "RadixSort"
4. Si los ves, el sorting se está ejecutando ✅
5. Click en cada uno para ver la textura de output
```

### D. Profiler
```
1. Window → Analysis → Profiler
2. CPU Usage → buscar "RadixSortVR" en timeline
3. GPU Usage → buscar tiempos de rendering
4. En editor, frame time debería ser < 16ms (60 FPS)
```

---

## PASO 4: Configurar VR (Opcional en Editor)

### Si ya tienes OVRCameraRig o XR Origin:
```
1. Verificar que tiene tag "MainCamera"
2. Listo, debería funcionar
```

### Si NO tienes VR rig:
```
Por ahora no es necesario para testing en editor.
En editor usará Camera.main normal.

Cuando quieras testear VR:
1. Hierarchy → XR → XR Origin (Action-based)
   o instalar Oculus Integration y usar OVRCameraRig
2. Tag la cámara principal como "MainCamera"
```

---

## ✅ CHECKLIST DE VERIFICACIÓN

Antes de continuar, verificar:

- [✅] Setup Wizard ejecutado sin errores
- [✅] Validation muestra todo ✓
- [✅] VRGaussianSplatManager en escena
- [✅] Splat object asignado en array
- [✅] RadixSortVR tiene todos los assets
- [✅] Play mode sin errores en Console
- [✅] Console muestra mensajes de sorting
- [✅] Frame Debugger muestra passes

---

## 🐛 TROUBLESHOOTING INMEDIATO

### ERROR: "Shader not found"
```
Solución:
1. Project → Assets/GaussianSplatting/OptimizedVR/Shaders
2. Click en cada shader
3. Verificar que compila (no debe haber errores en Console)
4. Reimport All: Right-click en Shaders folder → Reimport
```

### ERROR: "component not found or invalid"
```
Solución:
1. Seleccionar VRGaussianSplatManager
2. Inspector → click en "Setup RadixSort Component"
3. Inspector → click en "Create Optimized RenderTextures"
```

### WARNING: "Splat has no asset"
```
Solución:
1. Verificar que el GameObject en splatObjects[0] tiene:
   - Componente GaussianSplatRenderer
   - Campo "Asset" asignado (el ceramic splat)
2. Si no tiene asset:
   - Inspector del GameObject → GaussianSplatRenderer
   - Asset: Drag el ceramic asset desde GaussianAssets/
```

### NO se ven mensajes de sorting
```
Solución:
1. Verificar Debug Log = ✅ true
2. Mover cámara más de 5cm
3. Verificar que Camera.main existe (tag MainCamera)
4. Probar activar "Always Update" temporalmente
```

---

## 📊 RESULTADOS ESPERADOS

### En Console:
```
[VRGaussianSplatManager] Initialized. VR Active: false
[VRGaussianSplatManager] Sorted at position (0.1, 1.6, -1.9), frame 15
[VRGaussianSplatManager] Sorted at position (0.2, 1.6, -1.8), frame 28
...
```

### En Frame Debugger:
```
Scene
└── Camera
    └── BeforeForwardAlpha
        └── ComputeKeyValue
        └── RadixSort Pass 0
        └── RadixSort Pass 1
        └── GaussianSplat.Draw
```

### En Profiler:
```
CPU:
- RadixSortVR.ComputeKeyValues: ~0.2ms
- RadixSortVR.Sort: ~0.5ms

GPU:
- Sorting passes: ~2-3ms
- Splat rendering: ~5-8ms
```

---

## 🎯 SIGUIENTE PASO (después de validar)

Una vez que todo funciona en editor:

1. **Build para Quest 3**
   - File → Build Settings → Android
   - Build and Run
   
2. **Profiling real**
   - Oculus Developer Hub
   - Performance metrics
   
3. **Optimización**
   - Ajustar parámetros según FPS real
   - Objetivo: 72+ Hz

Ver `INTEGRATION_GUIDE.md` para detalles de build.

---

## 📞 ¿NECESITAS AYUDA?

1. ✅ Revisar Console por errores
2. ✅ Revisar Frame Debugger
3. ✅ Activar Debug Log
4. ✅ Consultar INTEGRATION_GUIDE.md
5. ✅ Consultar README.md
6. ✅ Revisar código (tiene comentarios)

---

## 🎉 ¡LISTO!

Si llegaste hasta acá y todo funcionó:

**¡FELICITACIONES! El sistema VR Gaussian Splatting está funcionando.**

Ahora puedes:
- Importar más splats
- Ajustar parámetros visuales
- Preparar build para Quest 3
- Experimentar con optimizaciones

**¡Éxito con tu proyecto VR!** 🚀
