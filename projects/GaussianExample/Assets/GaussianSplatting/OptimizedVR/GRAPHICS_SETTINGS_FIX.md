# 🔧 SOLUCIÓN RÁPIDA: Shader Stripping para Android

## El Archivo ProjectSettings/GraphicsSettings.asset

Para arreglar los errores de Wave Operations de aras-p, necesitas modificar la configuración de Graphics.

### Opción A: Usar Unity Editor UI (Fácil)

1. **Edit → Project Settings → Graphics**
2. **Scroll down** hasta "Shader Stripping"
3. **Vulkan section:**
   - **Wave Operations:** Disabled ✓
4. **Save**

### Opción B: Editar Directamente (Rápido)

Si no encuentras la opción en la UI:

1. **Cierra Unity**
2. Abre: `ProjectSettings/GraphicsSettings.asset` con un editor de texto
3. Busca: `m_ShaderKeywords` o `m_SelectedShaderStrippingSettings`
4. Agrega esta sección si no existe:

```yaml
m_ShaderStrippingSettings:
  m_DebugShaderVariantCompilation: 0
  m_DefaultShaderChunkCount: 16
  m_DefaultShaderChunkSize: 16777216
  m_DefaultShaderVariantCompilationCount: 16
  m_DefaultShaderVariantCompilationSize: 16777216
```

### Opción C: Crear Build Profile Específico (Mejor)

Para Android específicamente:

1. **Edit → Project Settings → Graphics**
2. **Platform-specific overrides → Android**
3. En la sección **Shader Stripping:**
   - Desactiva todas las opciones que requieren Wave Operations
4. **Apply**

## 🎯 Lo Que Necesitas Hacer

Si los shaders siguen fallando al compilar para Android:

### Paso 1: Verificar Graphics Settings
```
Edit → Project Settings → Graphics
```

### Paso 2: Vulkan Configuration
Busca esta sección y verifica:
```
Vulkan:
  - Enabled: Yes
  - Wave Operations: DISABLED  ← Importante
  - Bindless: Disabled
```

### Paso 3: Android Specific
```
Platform-specific settings → Android:
  - Graphics API: Vulkan
  - Vulkan Version: 1.0  ← No 1.1 (Wave Ops)
```

### Paso 4: Rebuild
```
File → Build Settings
Platform → Android
Build
```

## 🚨 Si Sigue Fallando

Si después de esto sigue dando el error de Wave Operations:

### Solución Nuclear: Excluir aras-p Compute Shaders

1. **Assets → Find "DeviceRadixSort"**
2. **Right-click → Properties**
3. **Editor: Uncheck "Vulkan"**
4. Repite para todos los compute shaders de aras-p

O mejor aún, en el archivo .meta del shader:

```yaml
userData: 
assetBundleName: 
assetBundleVariant: 
shelterAsset: 0
```

Cambia a:

```yaml
userData: 
assetBundleName: 
assetBundleVariant: 
shelterAsset: 0
# Excluir de Vulkan
excludePlatforms:
- Android
```

## ✅ Verificación

Después de hacer esto, intenta compilar:

```
Build → Build and Run (Android)
```

Si compila sin esos errores de Wave Operations, ¡problema resuelto!

## 📊 Por Qué Funciona Esto

- **Wave Operations** = Warp operations en GPU (Vulkan 1.1+)
- **Quest 3** = Vulkan 1.0
- **Solución** = Deshabilitar Wave Operations para Android
- **Nuestro código** = NO usa Wave Operations, así que no afecta

## 🎯 Lo Importante

Tu `RadixSortVR` **NO usa** ninguna Wave Operation:
- ✅ Solo Graphics.Blit
- ✅ Solo RenderTextures
- ✅ Solo Mipmaps
- ✅ 100% Compatible Quest 3

El error es **solo de aras-p**, no afecta nuestro sorting.

---

**¿Sigue sin funcionar?** Avísame qué errores ves después de esto y podemos hacer un fix más específico.
