# Guía del facilitador — Taller de videojuegos (60 min)

Proyecto: **MUL-Taller** (Unity 6000.3.23f1, plantilla 2D Platformer).
Público: estudiantes de 9° a 11°. Formato: una pareja por computador.

---

## Antes del taller (una sola vez, tú)

1. Abre el proyecto. **No** debe aparecer la ventana *Tutorials* ni el *Welcome Dialog*.
2. `Window ▸ Layouts ▸ Default` (el layout que dejó la plantilla de tutoriales queda raro).
3. Abre `Assets/_Taller/Scenes/Nivel_Taller.unity` y dale **Play**. Comprueba:
   - HUD arriba: la moneda con `0 / 5` a la izquierda, la cara del personaje con
     `× 3` a la derecha y `0 PTS` debajo.
   - Abajo, en su cartel, el mensaje amarillo `Te faltan 5 monedas`.
   - Al juntar 5 monedas el contador se pone dorado, el mensaje cambia y el arco
     del final ya deja pasar.
   - Al llegar al arco el personaje hace el pulgar arriba **antes** de que salga
     el cartel de GANASTE.
   - Al caer al hueco del piso 3 veces sale **PERDISTE**; con **R** vuelve a empezar.
4. Copia la carpeta del proyecto a cada máquina (o clona el repo).

### Checklist por máquina
- Unity **6000.3.23f1** instalado desde Unity Hub.
- Módulo de Windows Build Support **no** hace falta (no vamos a compilar).
- El proyecto abre en menos de 2 minutos la segunda vez (la primera importa todo).

---

## Agenda

| Min | Bloque | Qué pasa |
|-----|--------|----------|
| 0–5 | **Engancharlos** | Todos abren `Assets/Scenes/SampleScene.unity` y le dan Play. Que jueguen. "Esto es un juego real hecho en Unity. En 55 minutos va a ser *tuyo*." |
| 5–12 | **Tour del editor** | Las 4 ventanas + el botón Play. Primer cambio en vivo. |
| 12–35 | **Construyan su nivel** | En `Nivel_Taller`. Es el bloque más largo a propósito. |
| 35–50 | **Código** | `ReglasDelJuego.cs` y `PoderesDelJugador.cs`. |
| 50–60 | **Muestra** | Se paran y juegan el nivel del compañero de al lado. Cierre. |

---

## Bloque 1 · Tour del editor (5–12 min)

Muestra en el proyector, ellos repiten:

- **Hierarchy** (izquierda) — la lista de todo lo que existe en el nivel.
- **Scene** (centro) — el mundo, para moverse y armar. Rueda del mouse = zoom, botón derecho arrastrando = mirar alrededor, **F** = enfocar lo seleccionado.
- **Inspector** (derecha) — las propiedades del objeto seleccionado. *"Todo lo que ves aquí lo puedes cambiar."*
- **Project** (abajo) — los archivos: sprites, sonidos, prefabs, scripts.
- **▶ Play** — probar. **Ojo: los cambios hechos con Play encendido se pierden al apagarlo.**

### Primer cambio (que lo hagan todos)
1. En Hierarchy selecciona **Player**.
2. En el Inspector busca *Player Controller* → `Max Speed`. Cámbialo de `3` a `12`.
3. Play. Corre como loco.
4. Ahora `Jump Take Off Speed` de `7` a `15`. Play. Salta hasta las nubes.

> Aquí es donde se les prende el bombillo. No sigas hasta que todos lo hayan logrado.

---

## Bloque 2 · Construyan su nivel (12–35 min)

Que abran **`Assets/_Taller/Scenes/Nivel_Taller.unity`** (doble clic desde Project).
Es un nivel casi vacío a propósito: un piso largo con un hueco, dos plataformas,
3 monedas, 1 enemigo y el arco de meta. El cielo está casi limpio (solo nubes y
montañas lejanas) porque **el fondo también se pinta** — es parte del trabajo.

### Pintar plataformas
1. `Window ▸ 2D ▸ Tile Palette`.
2. En el desplegable de arriba elige la paleta del proyecto.
3. En Hierarchy, dentro de **Grid**, selecciona **Level** (es la capa que tiene colisión: si pintas en otra, el jugador la atraviesa).
4. Elige el pincel 🖌, elige un tile y pinta en la ventana Scene.
5. Borrar: mantén **Shift** mientras pintas.

### Poner cosas
Arrastra desde **Project** hacia la ventana **Scene**:

| Qué | Dónde está |
|-----|-----------|
| Moneda | `Assets/_Taller/Prefabs/Moneda.prefab` |
| Enemigo (patrulla solo) | `Assets/_Taller/Prefabs/Enemigo.prefab` |
| Cama elástica | `Assets/Mod Assets/Powerup Prefabs/Bouncepad.prefab` |
| Zona de velocidad | `Assets/Mod Assets/Powerup Prefabs/Speedpad.prefab` |
| Decoración (comida, plantas, espacio…) | `Assets/Mod Assets/2D Props/…` |

- La **meta** es el objeto `Victory` en Hierarchy (el arco de piedra): muévelo al final de tu nivel. Mientras falten monedas su trigger está apagado, así que se puede atravesar sin ganar.
- El **punto de salida** es `SpawnPoint`: ahí aparece el jugador al empezar y al revivir.
- Para que el enemigo camine más o menos lejos, selecciona el objeto `Enemigo` y cambia `Start Position` / `End Position` del componente *Patrol Path*.

### Si el nivel les queda muy largo
La cámara está encerrada en un rectángulo. Selecciona **CinemachineConfiner** en Hierarchy y arrastra los puntos verdes del *Polygon Collider 2D* (botón **Edit Collider**) para agrandar la zona.

### Retos sugeridos (escríbelos en el tablero)
- 🟢 Pon 8 monedas y haz que una sea muy difícil de alcanzar.
- 🟢 Haz un pozo que mate y una cama elástica para cruzarlo.
- 🟡 Haz una torre de plataformas que suba hasta arriba.
- 🟡 Pon 3 enemigos seguidos.
- 🔴 Haz un camino secreto que solo se vea si te atreves a saltar al vacío.

---

## Bloque 3 · Código (35–50 min)

Que abran **`Assets/_Taller/Scripts/ReglasDelJuego.cs`** (doble clic).
Todo lo importante está marcado con `>>> CAMBIA ESTO <<<`.

> No hay que explicarles qué es `[System.NonSerialized]`. Si preguntan:
> "es una nota para Unity que dice: hazle caso al código, no a la escena".

### 3.1 Variables (5 min)
> "Una variable es una cajita con nombre que guarda un valor."

```csharp
[System.NonSerialized] public int monedasParaGanar = 5;   // súbelo a 10
[System.NonSerialized] public int vidas = 3;              // bájalo a 1 -> modo difícil
[System.NonSerialized] public int puntosPorMoneda = 10;   // súbelo a 100
```
Guardan (**Ctrl+S**), vuelven a Unity, esperan 2 segundos a que compile
(rueda girando abajo a la derecha), y Play.

> **Por qué estas variables sí obedecen al código.** Normalmente Unity guarda
> una copia de cada variable pública dentro de la escena, y esa copia le gana
> a lo que diga el archivo — es la trampa clásica que vuelve loco a todo el
> mundo. Por eso las variables del taller llevan `[NonSerialized]`: así el
> código es la única verdad y lo que escriban se ve de inmediato.
>
> La lección de Inspector ya la dieron en el Bloque 1 con el Player.

### 3.2 Condicional (10 min)
> "Un `if` es una pregunta. Si la respuesta es sí, hace lo de adentro."

La regla que abre la meta:
```csharp
if (monedasRecogidas >= monedasParaGanar)
{
    AbrirLaMeta(true);
    MostrarMensaje("¡La meta está abierta! Corre hacia el arco");
}
```
Que cambien el mensaje por uno suyo. Que prueben poner `>` en vez de `>=`
y descubran que ahora hay que juntar una moneda de más.

Y el bono:
```csharp
if (monedasRecogidas > monedasParaGanar)
{
    puntos = puntos + bonoPorMonedaExtra;
}
```

### 3.3 Si van rápido: sacar la variable al Inspector (3 min)

Este es el mejor momento para explicar por qué existe `[System.NonSerialized]`,
porque ahora ya lo vivieron.

> "Unity normalmente **guarda una copia** de cada variable pública dentro de la
> escena, para que la puedas ajustar sin tocar código. El problema es que esa
> copia le gana al archivo: cambias el número en el código y no pasa nada.
> `[System.NonSerialized]` le dice a Unity que no guarde copia."

Que borren `[System.NonSerialized]` de una sola línea, por ejemplo `vidas`:

```csharp
public int vidas = 3;
```

Guardan, vuelven a Unity, seleccionan **GameController** en Hierarchy y ahí
está el campo *Vidas* en el Inspector. Ahora que lo comprueben: cambiar el
número en el código ya **no** hace nada, y el que manda es el del Inspector.

Es la primera vez que ven una decisión de diseño de software: las dos opciones
sirven, cada una para algo distinto.

### 3.4 El superpoder (3 min)
**`Assets/_Taller/Scripts/PoderesDelJugador.cs`**
```csharp
[System.NonSerialized] public int saltosExtra = 1;        // 0 = normal, 1 = doble, 2 = triple
[System.NonSerialized] public float fuerzaSaltoExtra = 7f;
[System.NonSerialized] public float saltoMinimo = 0.7f;   // cuánto salta si solo TOCAS la tecla
```
Que pongan `2` y `12`. Play. Explosión de risas garantizada.

> Los saltos extra salen siempre completos, sin depender de cuánto dejes apretada
> la tecla. El que sí responde a eso es el primer salto: `saltoMinimo` decide
> cuánto te levantas si solo la tocas. Con `1` el salto deja de ser variable.

> **Para quien va muy adelantado.** La guía del estudiante tiene al final del
> bloque 3 una tanda de retos graduados (fácil / medio / difícil): escribir el
> resumen del cartel final, bono progresivo, mensajes distintos según cuántas
> monedas falten, medallas por puntaje. Mandálos ahí en vez de inventarles trabajo.

### 3.5 Si queda tiempo: dale tu estilo al HUD (5 min)

El HUD es un objeto normal de la escena, no algo intocable. En **Hierarchy**,
despliega **HUD Taller**:

```
HUD Taller
  Marcador Monedas   -> Icono + Numero
  Marcador Vidas     -> Icono + Numero
  Marcador Puntos    -> Numero
  Cartel Mensaje     -> Texto
  Panel Victoria     -> Tarjeta (Franja, Titulo, Instruccion)
  Panel Derrota      -> Tarjeta (Franja, Titulo, Instruccion)
```

Cosas de un minuto cada una:

| Qué quieren | Dónde |
|---|---|
| Cambiar el color de una pastilla | Selecciona `Marcador Monedas` → *Image* → **Color** |
| Cambiar el color o el tamaño del número | Selecciona `Numero` → *TextMeshPro* → **Vertex Color** / **Font Size** |
| Cambiar el icono | Selecciona `Icono` → *Image* → **Source Image**: arrastra cualquier sprite, por ejemplo un donut de `Mod Assets` |
| Mover un marcador | Selecciónalo y usa la herramienta **Rect** (tecla `T`) en la vista Game |
| Cambiar el texto de GANASTE | `Panel Victoria → Tarjeta → Titulo` → **Text** |

Y en el código, en `ReglasDelJuego.cs`, el formato del contador:

```csharp
textoMonedas.text = monedasRecogidas + " / " + monedasParaGanar;
```

Que prueben `"Monedas: " + monedasRecogidas` o lo que se les ocurra. También
están ahí los colores `Apagado` y `Dorado`, que son los que usa el contador
antes y después de completar las monedas.

> Ojo: los paneles de victoria y derrota están **desactivados** en la escena.
> Para verlos mientras los editan, actívalos con la casilla de arriba del
> Inspector y **vuelve a desactivarlos** antes de darle Play.

---

## Bloque 4 · Muestra (50–60 min)

Todos se levantan y juegan el nivel del computador de al lado. 2 minutos por puesto.
Cierre corto:

- Lo que hicieron hoy — nivel, reglas, poderes — es exactamente lo que hace un
  *game designer* y un *gameplay programmer*.
- Unity es gratis para uso personal; el proyecto se lo pueden llevar.
- Siguiente paso si les gustó: el tutorial oficial *2D Platformer Microgame* de Unity Learn.

---

## Problemas comunes

| Síntoma | Solución |
|---|---|
| Cambié el script y no pasa nada | Faltó guardar (Ctrl+S) o Unity aún está compilando (rueda abajo a la derecha). |
| Cambié un número del Player y sigue igual | El `Max Speed` del Player sí vive en el Inspector: cámbialo ahí, no en el código. |
| Borré `[System.NonSerialized]` y ahora el código no manda | Correcto, es justo lo que hace. Ahora el valor lo pone el Inspector del **GameController**. Para volver atrás, escribe el atributo otra vez. |
| Apreté Escape y salió un menú raro | Es el menú de ejemplo que trae la plantilla, con textos de relleno. Vuelve a apretar **Escape** para cerrarlo. |
| Pinté tiles y el jugador los atraviesa | Estaban pintando en una capa de fondo. Debe ser **Grid ▸ Level**. |
| El jugador cae para siempre | Hay un hueco en el piso, o el `SpawnPoint` quedó fuera del nivel. |
| La cámara no sigue al jugador | El nivel se salió del rectángulo del **CinemachineConfiner**. |
| Toqué algo y quedó feo | `Ctrl+Z`. Si ya no hay vuelta atrás: menú **Taller ▸ 3 - Generar Nivel_Taller**, que rehace la escena desde cero. |
| Perdí todo | El menú **Taller ▸ Preparar todo** regenera prefabs y nivel. |

---

## Qué se agregó al proyecto original

- `Assets/_Taller/Scripts/ReglasDelJuego.cs` — monedas, vidas, puntos, meta que se abre, pantallas de Victoria/Derrota. Se engancha a los eventos existentes del template (`Simulation.Event<T>.OnExecute`); no modifica ningún script original.
- `Assets/_Taller/Scripts/PoderesDelJugador.cs` — doble salto. Se agregó al prefab `Assets/Prefabs/Player.prefab`, así que funciona en cualquier escena.
- `Assets/_Taller/Editor/TallerSetup.cs` — menú **Taller** para regenerar todo entre sesiones.
- `Assets/_Taller/Prefabs/` — `Moneda` y `Enemigo` listos para arrastrar.
- `Assets/_Taller/Scenes/Nivel_Taller.unity` — el lienzo de los estudiantes.
- Se eliminaron `Assets/Tutorials/` y los paquetes `com.unity.learn.iet-framework`, `com.unity.connect.share` y `com.unity.multiplayer.center` (el primero causaba un `NullReferenceException` cada vez que se daba Play).

`Assets/Scenes/SampleScene.unity` quedó **intacta** como demo.

## Ajustes de sensación de juego

- **El salto ya no suena.** El campo `Jump Audio` del prefab del Player quedó vacío
  a propósito. Siguen sonando pasos, monedas, aterrizaje y daño. Para devolverlo,
  arrastra `Assets/Audio/jump.wav` a ese campo.
- **Movimiento más fluido.** `KinematicObject` resuelve las colisiones asignando
  `body.position`, que en Unity es un teleport y descarta la interpolación del
  Rigidbody2D: el sprite avanzaba a saltos de 50 Hz. Ahora rebobina al inicio del
  paso y entrega el destino con `MovePosition`, y el Fixed Timestep subió a 60 Hz.
  Es el único script original modificado.
- **Colisiones lisas.** El tilemap `Level` tenía un `TilemapCollider2D` sin
  `CompositeCollider2D`, así que cada tile era un collider suelto y el jugador
  enganchaba en las costuras internas al saltar pegado a una pared. Ahora se
  fusionan en un solo contorno (menú **Taller ▸ 4** para aplicarlo a otra escena).
  Además, `KinematicObject` cancelaba **las dos** componentes de la velocidad al
  golpear cualquier cosa en el aire, así que rozar una pared mataba el salto;
  ahora solo cancela el eje que de verdad chocó.
- **El salto se siente firme.** `PlayerController` pone `stopJump` al soltar la
  tecla y `ComputeVelocity` hace `velocity.y *= jumpDeceleration`, que en la escena
  vale **0**: soltar borraba el impulso entero, así que el salto era binario
  (toque = brinquito, mantener = salto completo). Ahora `PoderesDelJugador` sostiene
  un "piso" de velocidad que decae exactamente al ritmo de la gravedad, así que
  nunca regala altura y solo impide el corte de golpe. El salto normal conserva
  como mínimo `saltoMinimo` de su impulso; los saltos extra se protegen enteros.
  Además el `gravityModifier` del Player pasó de `1` a `1.5`: se cae más rápido de
  lo que se sube, que es el truco clásico para que no se sienta flotante.
- **El salto extra tiene animación.** El Animator solo reacciona a `grounded`, que
  ya es falso en el aire, así que el segundo salto no cambiaba nada y el personaje
  parecía flotar. `PoderesDelJugador` ahora vuelve a lanzar el estado `Player-Jump`
  desde el frame 0 en cada salto extra.
- **La celebración de la meta se ve.** `PlayerEnteredVictoryZone` sí dispara el
  trigger `victory`, pero el cartel ponía `Time.timeScale = 0` en el mismo
  instante y eso congela también el Animator. Ahora el cartel espera
  `segundosDeCelebracion` (1.8 s) antes de tapar la pantalla.
- **Reaparición arreglada.** El objeto `SpawnPoint` del template no lleva el
  componente `SpawnPoint`; la referencia que usa `PlayerSpawn` al reaparecer es
  `model.spawnPoint` del GameController. El generador movía otro objeto, así que
  el jugador arrancaba bien pero reaparecía enterrado en la posición del nivel
  original. Ahora mueve la referencia real y le añade el componente.
