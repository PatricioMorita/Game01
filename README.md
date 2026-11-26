# Pixel en el Valle — Prototipo de plataformas 2D

**Asignatura:** Desarrollo de Videojuegos — Prueba 02: Arquitectura y Lógica de un Videojuego
**Integrantes:** Patricio Mora · Francisco Pinilla

## Objetivo
Prototipo de un nivel de plataformas 2D. **Relato:** Pixel se alejó de casa y debe cruzar el valle hasta la bandera junto a su casa. El jugador recorre el nivel (Tilemap), junta monedas, esquiva trampas y llega a la meta. Si cae al vacío o toca una trampa, pierde y puede reintentar.

## Plataforma objetivo: PC Windows (x86_64)
- **Tipo de juego:** un plataformas de precisión se controla mejor con teclado que con touch.
- **Público:** estudiantes y jugadores casuales en PC. No hace falta instalación, basta con ejecutar el `.exe`.
- **Despliegue:** el build Windows no requiere firma, SDK ni dispositivo físico (a diferencia de Android), así que es más fácil de probar y evaluar.

## Configuración clave
| Elemento | Decisión |
|---|---|
| Versión de Unity | 6000.2.3f1 |
| Pipeline | **URP 2D** (Renderer2D), liviano y pensado para sprites y luces 2D |
| Quality | Por defecto en Windows: *Ultra* (el juego 2D es liviano), VSync activado, todos los niveles usan el asset URP 2D |
| Input | Input Manager (teclado), *Active Input Handling = Both* |
| Física | Rigidbody2D + gravedad ajustada al caer + Physics Material 2D sin fricción en el jugador |
| Audio | `AudioManager` persistente: BGM en loop + SFX con `PlayOneShot` |

### Organización de carpetas
```
Project/Assets/
├── Audio/       música (BGM) y efectos (SFX) .wav
├── Editor/      ProjectBuilder.cs (genera assets, escenas y build)
├── Physics/     PlayerNoFriction (Physics Material 2D)
├── Prefabs/     Player, Coin, Spikes, Goal, AudioManager
├── Scenes/      MainMenu, Level1, GameOver
├── Scripts/     lógica del juego
├── Sprites/     personajes y objetos
├── Tiles/       tiles Grass y Dirt del Tilemap
└── Settings/    URP / Renderer2D
```

## Arquitectura (game loop)
`MainMenu` → **Jugar** → `Level1` → (meta = victoria · trampa/caída = derrota) → `GameOver` → **Reintentar** / **Menú**

| Script | Responsabilidad |
|---|---|
| `PlayerController` | Movimiento, salto con física, gravedad ajustada, detección de suelo, muerte por caída |
| `GameManager` | Puntaje, victoria/derrota y paso a GameOver |
| `AudioManager` | Música de fondo y efectos, persistente entre escenas |
| `Coin` / `Hazard` / `Goal` | Interacciones del nivel (prefabs reutilizables) |
| `MenuUI` / `GameOverUI` | Botones con sonido de clic y pantalla de resultado |
| `CameraFollow` | Cámara que sigue al jugador |

Todos los parámetros (velocidad, salto, volumen, nombres de escena) se exponen con `[SerializeField]` y se ajustan en el Inspector, sin valores fijos en el código.

## Audio
- **BGM:** música en loop desde MainMenu.
- **SFX:** salto, moneda, muerte, victoria y clic de UI.

## Ejecución
- **Build:** abrir `Build/Game01.exe`.
- **Editor:** abrir `Project/` con Unity 6000.2.3f1, abrir `Assets/Scenes/MainMenu` y presionar Play.

## Controles
| Acción | Tecla |
|---|---|
| Moverse | A / D o ← / → |
| Saltar | Espacio |
| Menús | Mouse |

## Dependencias
Universal RP 17.2, Input System 1.14, uGUI 2.0 (TextMeshPro), 2D Tilemap. Arte (pixel art) y audio (chiptune) originales, generados por código en `Editor/ProjectBuilder.cs`: sin assets de terceros.

## Riesgos y mitigaciones
| Riesgo | Mitigación |
|---|---|
| Conflictos al trabajar los dos en la misma escena | Cada integrante trabaja en escenas o prefabs distintos; se usan prefabs para compartir cambios |
| Repositorio pesado | `.gitignore` de Unity (excluye Library/, Temp/, Logs/) |
| El jugador se pega a las paredes | Physics Material 2D con fricción 0 en el jugador |
| Atravesar el suelo a gran velocidad | Collision Detection *Continuous* en el Rigidbody2D |
| Assets con copyright | Todo el arte y el audio es original |
| Geometría del suelo vacía al generar escenas por script | `CompositeCollider2D.GenerateGeometry()` antes de guardar la escena |

## Roles
- **Patricio Mora:** programación (scripts, física, game loop).
- **Francisco Pinilla:** diseño del nivel (Tilemap), UI, audio y build.
