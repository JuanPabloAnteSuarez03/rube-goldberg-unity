# Máquina de Rube Goldberg · Taller Unity

**Repositorio:** <https://github.com/JuanPabloAnteSuarez03/rube-goldberg-unity>

![Unity](https://img.shields.io/badge/Unity-6-000000?logo=unity&logoColor=white)
![Física](https://img.shields.io/badge/F%C3%ADsica-Rigidbody%20%C2%B7%20Colisiones-2f6fdb)
![Ray%20tracing](https://img.shields.io/badge/Ray%20tracing-GPU%20compute-2fbf71)

Proyecto de Unity con una **máquina de Rube Goldberg de 12 toques** hecha con primitivas y el motor
de físicas (Rigidbody, colisiones, gravedad y fricción). Toda la máquina se genera por código, así
que no hay que configurar prefabs ni arrastrar objetos a mano. Incluye una **vista de ray tracing
por GPU** (sin HDRP ni DXR) que se activa con **T**.

---

## Cómo abrirlo

1. En Unity Hub pulsa **Import projects** y elige la carpeta **padre**
   (`.../Desarrollo de Videojuegos`), que es la que contiene a `Maquina de Rube Goldberg`.
   El Hub nuevo busca proyectos *dentro* de la carpeta que seleccionas.
2. Ábrelo con **Unity 6** (probado con 6000.6.6f1). Si Unity pide *actualizar* el proyecto, acepta.
3. Abre la escena **`Assets/Scenes/RubeGoldberg.unity`**.
4. Pulsa **Play (▶)**.

> Atajo: si el Hub se resiste, abre el proyecto directo con el editor:
>
> ```bash
> ~/Unity/Hub/Editor/6000.6.6f1/Editor/Unity -projectPath "$(pwd)"
> ```

> Si por alguna razón la escena no abre, crea una escena nueva y pulsa Play: la máquina se genera
> automáticamente en cualquier escena (ver `MachineBootstrap.cs`).

La primera bola se suelta sola después de 1,6 segundos y la cadena completa tarda unos **6,5 s**
(≈8 s contando el retardo). El récord se mide desde que se suelta la bola.

| Tecla | Acción |
|-------|--------|
| **R** | Reiniciar la máquina |
| **F** | Cámara lenta (0,25×) |
| **T** | Activar / desactivar el ray tracing |

---

## Los 12 toques

```
 [1]  La bola 1 baja por la rampa naranja y golpea la ficha 1
  ↓
 [2-6] Las 6 fichas de dominó caen en cadena sobre la mesa alta
  ↓
 [7]  La ficha 6 derriba la torre (ficha 7) de la mesa baja
  ↓
 [8-11] La torre y las fichas 8, 9, 10 y 11 siguen la cadena
  ↓
 [12] La ficha 11 empuja la ficha final, que cae en el plato de META
  ↓
 Se enciende la lámpara verde, sube la bandera y hay confeti
```

| Toque | Objeto | Física que interviene |
|-------|--------|-----------------------|
| 1 | Rampa + bola 1 + ficha 1 | Gravedad, plano inclinado, rodadura e impulso |
| 2-6 | Fichas 1 → 6 | Transferencia de momento y torque |
| 7 | Ficha 6 → torre (ficha 7) | Caída desde la mesa alta y palanca |
| 8-11 | Torre y fichas 8 → 11 | Dominó: choque, torque y gravedad |
| 12 | Ficha 11 → ficha final | Choque y caída libre al plato |

La mesa baja está un escalón por debajo de la mesa alta. La primera ficha de la mesa baja es una
**torre de 1,5 m**, para que el último dominó de arriba la alcance al caer. Las fichas de la mesa
baja están más juntas (0,42 m) para que la cadena no pierda energía.

El HUD muestra la etapa actual, el tiempo, una barra con los **12 toques** y el estado del ray
tracing. Sobre la máquina hay un tablero con 12 indicadores que se ponen verdes al completar cada
toque. Al final aparece **¡MÁQUINA COMPLETADA!** y se lanza el confeti.

---

## Ray tracing

La vista de ray tracing está **activada por defecto**; pulsa **T** para comparar con el render normal.

- `Assets/Resources/RayTracing.compute` es un trazador de rayos en GPU. Por cada píxel lanza:
  - un rayo primario contra esferas, cajas y el suelo;
  - dos rayos de sombra (sombra suave);
  - un rayo de reflexión;
  - dos rayos de oclusión ambiental.
- `Assets/Scripts/RayTracingView.cs` recoge las primitivas de la escena (posición, rotación,
  tamaño, color, brillo y emisión) y las pasa al shader cada frame. También lee la cámara y la luz
  direccional, así que la imagen sigue a la máquina en movimiento.
- Funciona en el **render pipeline integrado** con compute shaders: no hace falta HDRP ni una GPU
  RTX. Por eso también corre en Linux y macOS, donde Unity no ofrece DXR.
- Si la GPU no soporta compute shaders, el efecto se desactiva solo y se usa el render normal.
- La resolución interna es el **75 %** de la pantalla (`escalaResolucion` en `RayTracingView`).
  Bájala a 0,5 si quieres más rendimiento; súbela a 1 para máxima calidad.

> ¿Por qué no HDRP + DXR? El ray tracing *hardware* de Unity exige HDRP y DX12 (Windows). Este
> proyecto es de pipeline integrado y multiplataforma, así que el trazador se implementó con
> compute shaders para que funcione en cualquier equipo.

---

## Estructura del proyecto

```
Assets/
├── Scenes/
│   └── RubeGoldberg.unity      Escena con cámara, luces y la máquina montada
├── Scripts/
│   ├── RubeGoldbergBuilder.cs  Construye TODA la máquina, el marcador y la meta
│   ├── MachineBootstrap.cs     La genera sola al pulsar Play
│   ├── MachineController.cs    Tiempo, HUD, 12 toques, reinicio y cámara lenta
│   ├── GoalSensor.cs           Detecta la ficha final dentro de la meta
│   └── RayTracingView.cs       Vista de ray tracing por GPU
├── Resources/
│   └── RayTracing.compute      Trazador de rayos (compute shader)
├── Editor/
│   ├── RubeGoldbergMenu.cs     Menú "Rube Goldberg"
│   ├── RubeGoldbergValidator.cs Simula la cadena y comprueba los 12 toques
│   └── RubeGoldbergRenderTest.cs Prueba de render PNG (batch)
└── Materials/                  Materiales generados automáticamente
```

Materiales: se crean solos en `Assets/Materials` la primera vez que usas el menú del editor. La
escena guardada ya los referencia, así que incluye la carpeta `Assets/Materials` al subir el
proyecto (no está en `.gitignore`).

---

## Ver la máquina en el editor (sin Play)

Menú **Rube Goldberg → Construir máquina en la escena**. Después puedes moverte con el botón
central del ratón por la vista *Scene*. Con **Play** la máquina funciona de verdad.

También puedes reconstruir y guardar la escena completa con
**Rube Goldberg → Reconstruir y guardar escena**.

---

## Comprobar que la cadena funciona

Menú **Rube Goldberg → Validar la cadena (12 toques)**. Abre una escena nueva, construye la máquina,
simula la física en el editor y muestra un informe con los 12 toques y dónde quedó la ficha final.
La validación actual da **12/12** y la ficha final entra en la meta en unos **6,5 s**.

> La validación abre una escena nueva: guarda tu escena antes de usarla.

En batch (sin abrir la interfaz):

```bash
~/Unity/Hub/Editor/6000.6.6f1/Editor/Unity -batchmode -nographics \
  -projectPath "$(pwd)" -executeMethod RubeGoldbergValidator.ValidarBatch
```

---

## Para la foto del taller

1. Pulsa **Play** y espera a que se complete la cadena (unos 8 s).
2. Maximiza la vista **Game** (`Ctrl/Cmd + Shift + F` o el botón *Maximize On Play*).
3. Captura la pantalla:
   - Windows: `Win + Shift + S`
   - macOS: `Cmd + Shift + 4`
   - Linux: `PrntScr` o la herramienta de capturas.
4. Opcional: pulsa **T** para la versión con ray tracing (o déjala activada, viene por defecto) y
   saca una segunda captura. También queda bien una foto de la vista **Scene** con la jerarquía
   (`Maquina Rube Goldberg`) visible.

---

## Ajustes rápidos

En `RubeGoldbergBuilder.cs`, al principio del archivo:

- `retardoInicio` (en `MachineController`) → segundos antes de soltar la bola.
- `FichaPaso` → separación del primer dominó (más juntas = caen más rápido).
- `Ficha2Paso` → separación del segundo dominó (0,42 por defecto).
- `FichaTorreAlto` / `FichaTorreMasa` → tamaño y peso de la torre de la mesa baja.
- `BolaR`, `FichaFinalX` → tamaño de la bola y posición de la ficha final.
- Posición y ángulo de la cámara en `CrearEntorno()`.
- `escalaResolucion` (en `RayTracingView`) → calidad/rendimiento del ray tracing.

---

## Subir a GitHub

El proyecto ya está listo para subirse tal cual (el `.gitignore` excluye `Library/`, `Temp/`, etc.).

```bash
git init
git add .
git commit -m "Máquina de Rube Goldberg: 12 toques + ray tracing"
git branch -M main
git remote add origin https://github.com/TU-USUARIO/rube-goldberg-unity.git
git push -u origin main
```

Crea el repositorio vacío antes en <https://github.com/new> (sin README, sin .gitignore).
