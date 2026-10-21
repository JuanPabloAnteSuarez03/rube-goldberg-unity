# Máquina de Rube Goldberg · Taller Unity

**Repositorio:** <https://github.com/JuanPabloAnteSuarez03/rube-goldberg-unity>

![Unity](https://img.shields.io/badge/Unity-6-000000?logo=unity&logoColor=white)
![Física](https://img.shields.io/badge/F%C3%ADsica-Rigidbody%20%C2%B7%20Colisiones-2f6fdb)

Proyecto de Unity con una **máquina de Rube Goldberg** hecha con primitivas y el motor de físicas
(Rigidbody, colisiones, gravedad y fricción). Toda la máquina se genera por código, así que no hay
que configurar prefabs ni arrastrar objetos a mano.

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

La primera bola se suelta sola después de 1,6 segundos.

| Tecla | Acción |
|-------|--------|
| **R** | Reiniciar la máquina |
| **F** | Cámara lenta (0,25×) |

---

## La cadena de reacciones

```
 [1] Bola 1 baja por la rampa naranja
      ↓
 [2] Golpea la primera ficha y caen las 6 fichas de dominó
      ↓
 [3] La última ficha empuja la Bola 2, que estaba en la repisa
      ↓
 [4] La Bola 2 cae en el plato de META
      ↓
 [5] Se enciende la lámpara verde, sube la bandera y hay confeti
```

| Etapa | Objeto | Física que interviene |
|-------|--------|-----------------------|
| 1 | Rampa + Bola 1 | Gravedad, plano inclinado, fricción y rodadura |
| 2 | Dominó (6 fichas) | Transferencia de impulso y torque |
| 3 | Repisa + Bola 2 | Choque, caída libre |
| 4 | Plato de meta | Trigger (zona de detección) |

El HUD muestra la etapa actual y el tiempo, y al final aparece **¡MÁQUINA COMPLETADA!**.

---

## Estructura del proyecto

```
Assets/
├── Scenes/
│   └── RubeGoldberg.unity      Escena con cámara y luz
├── Scripts/
│   ├── RubeGoldbergBuilder.cs  Construye TODA la máquina por código
│   ├── MachineBootstrap.cs     La genera sola al pulsar Play
│   ├── MachineController.cs    Tiempo, HUD, reinicio y meta
│   └── GoalSensor.cs           Detecta la bola dentro de la meta
└── Editor/
    └── RubeGoldbergMenu.cs     Menú "Rube Goldberg" del editor
```

Materiales: se crean solos en `Assets/Materials` la primera vez que usas el menú del editor.

---

## Ver la máquina en el editor (sin Play)

Menú **Rube Goldberg → Construir máquina en la escena**. Después puedes moverte con el botón
central del ratón por la vista *Scene*. Con **Play** la máquina funciona de verdad.

---

## Para la foto del taller

1. Pulsa **Play** y espera a que se complete la cadena (unos 5 s).
2. Maximiza la vista **Game** (`Ctrl/Cmd + Shift + F` o el botón *Maximize On Play*).
3. Captura la pantalla:
   - Windows: `Win + Shift + S`
   - macOS: `Cmd + Shift + 4`
   - Linux: `PrtSc` o la herramienta de capturas.
4. Opcional: también queda bien una foto de la vista **Scene** con la máquina construida y la
   jerarquía (`Maquina Rube Goldberg`) visible.

---

## Ajustes rápidos

En `RubeGoldbergBuilder.cs` están las medidas al principio del archivo:

- `retardoInicio` (en `MachineController`) → segundos antes de soltar la bola.
- `FichaPaso` → separación del dominó (más juntas = caen más rápido).
- `BolaR`, `BolaBR` → tamaño de las bolas.
- Posición y ángulo de la cámara en `CrearEntorno()`.

---

## Subir a GitHub

El proyecto ya está listo para subirse tal cual (el `.gitignore` excluye `Library/`, `Temp/`, etc.).

```bash
git init
git add .
git commit -m "Máquina de Rube Goldberg en Unity"
git branch -M main
git remote add origin https://github.com/TU-USUARIO/rube-goldberg-unity.git
git push -u origin main
```

Crea el repositorio vacío antes en <https://github.com/new> (sin README, sin .gitignore).
