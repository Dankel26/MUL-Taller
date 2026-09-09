using System.Collections.Generic;
using System.IO;
using Platformer.Mechanics;
using Taller;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

/// <summary>
/// Herramientas para preparar el proyecto del taller.
/// Menu "Taller" en la barra superior de Unity.
/// Tambien se puede correr sin abrir el editor con:
///   Unity.exe -batchmode -quit -projectPath . -executeMethod TallerSetup.PrepararTodo
/// </summary>
public static class TallerSetup
{
    const string RutaSampleScene = "Assets/Scenes/SampleScene.unity";
    const string RutaNivelTaller = "Assets/_Taller/Scenes/Nivel_Taller.unity";
    const string RutaPrefabMoneda = "Assets/_Taller/Prefabs/Moneda.prefab";
    const string RutaPrefabEnemigo = "Assets/_Taller/Prefabs/Enemigo.prefab";
    const string RutaPrefabEnemyBase = "Assets/Prefabs/Enemy.prefab";
    const string RutaPrefabJugador = "Assets/Prefabs/Player.prefab";
    const string RutaPrefabArco = "Assets/Mod Assets/2D Props/Ancient/Archway.prefab";
    const string RutaTileSuperficie = "Assets/Tiles/TileGroundTop.asset";
    const string RutaTileRelleno = "Assets/Tiles/TileGround.asset";
    const string RutaTileNube = "Assets/Tiles/cloud.asset";
    const string RutaPastilla = "Assets/_Taller/Arte/PastillaHud.png";
    const string RutaTileMontana = "Assets/Tiles/mountains.asset";

    // Medidas del nivel del taller, en unidades del mundo.
    const float PisoY = 0f;          // altura de la superficie donde se camina
    const float NivelDesdeX = -4f;
    const float NivelHastaX = 44f;
    const float SalidaX = 0f;
    const float MetaX = 40f;

    // ------------------------------------------------------------------
    // Menus
    // ------------------------------------------------------------------

    [MenuItem("Taller/1 - Crear HUD en la escena actual", false, 1)]
    public static void CrearHudEnEscenaActual()
    {
        var escena = EditorSceneManager.GetActiveScene();
        ConstruirHudYReglas();
        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena);
        Debug.Log("[Taller] HUD creado en " + escena.name);
    }

    [MenuItem("Taller/2 - Generar prefabs del taller", false, 2)]
    public static void GenerarPrefabsDelTaller()
    {
        AsegurarCarpetas();
        EditorSceneManager.OpenScene(RutaSampleScene, OpenSceneMode.Single);
        CrearPrefabMoneda();
        CrearPrefabEnemigo();
        AgregarPoderesAlJugador();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Taller] Prefabs generados en Assets/_Taller/Prefabs");
    }

    [MenuItem("Taller/3 - Generar Nivel_Taller (borra y rehace la escena)", false, 3)]
    public static void GenerarNivelTaller()
    {
        AsegurarCarpetas();

        if (AssetDatabase.LoadAssetAtPath<Object>(RutaNivelTaller) != null)
            AssetDatabase.DeleteAsset(RutaNivelTaller);
        if (!AssetDatabase.CopyAsset(RutaSampleScene, RutaNivelTaller))
        {
            Debug.LogError("[Taller] No se pudo copiar la escena base.");
            return;
        }
        AssetDatabase.Refresh();

        var escena = EditorSceneManager.OpenScene(RutaNivelTaller, OpenSceneMode.Single);

        VaciarNivel();
        PintarPiso();
        PintarCielo();
        FusionarColisionesDelTilemap();
        ReubicarPuntosClave();
        SembrarContenidoInicial();
        AjustarLimitesDeCamara();
        ConstruirHudYReglas();

        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena);
        RegistrarEnBuildSettings();
        Debug.Log("[Taller] Nivel_Taller generado.");
    }

    [MenuItem("Taller/4 - Fusionar colisiones del tilemap (escena actual)", false, 4)]
    public static void FusionarColisionesDeLaEscenaActual()
    {
        var escena = EditorSceneManager.GetActiveScene();
        FusionarColisionesDelTilemap();
        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena);
        Debug.Log("[Taller] Colisiones fusionadas en " + escena.name);
    }

    [MenuItem("Taller/Preparar todo (2 + 3)", false, 20)]
    public static void PrepararTodo()
    {
        GenerarPrefabsDelTaller();
        ArreglarColisionesDeSampleScene();
        GenerarNivelTaller();
        Debug.Log("[Taller] Preparacion completa.");
    }

    /// <summary>
    /// La escena demo tiene el mismo problema de colisiones por tile que el nivel
    /// del taller, y es la primera que juegan. Solo se le tocan los colliders.
    /// </summary>
    static void ArreglarColisionesDeSampleScene()
    {
        var escena = EditorSceneManager.OpenScene(RutaSampleScene, OpenSceneMode.Single);
        FusionarColisionesDelTilemap();
        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena);
    }

    // ------------------------------------------------------------------
    // Nivel
    // ------------------------------------------------------------------

    static void VaciarNivel()
    {
        // Se borran TODOS los tilemaps. Los de fondo del nivel original estan
        // alineados con un piso que estaba mucho mas abajo, asi que si se dejan
        // quedan edificios flotando por debajo del suelo nuevo.
        foreach (var mapa in Object.FindObjectsByType<Tilemap>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            mapa.ClearAllTiles();

        foreach (var moneda in Object.FindObjectsByType<TokenInstance>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (moneda != null) Object.DestroyImmediate(moneda.gameObject);

        foreach (var enemigo in Object.FindObjectsByType<EnemyController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (enemigo != null) Object.DestroyImmediate(enemigo.gameObject);

        foreach (var ruta in Object.FindObjectsByType<PatrolPath>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (ruta != null) Object.DestroyImmediate(ruta.gameObject);
    }

    static void PintarPiso()
    {
        var mapa = BuscarTilemapSolido();
        if (mapa == null) { Debug.LogError("[Taller] No encontre el tilemap con colision."); return; }

        var superficie = AssetDatabase.LoadAssetAtPath<TileBase>(RutaTileSuperficie);
        var relleno = AssetDatabase.LoadAssetAtPath<TileBase>(RutaTileRelleno);
        if (superficie == null || relleno == null) { Debug.LogError("[Taller] Faltan los tiles del piso."); return; }

        var celdaIzquierda = mapa.WorldToCell(new Vector3(NivelDesdeX, PisoY - 0.01f, 0f));
        var celdaDerecha = mapa.WorldToCell(new Vector3(NivelHastaX, PisoY - 0.01f, 0f));

        // Un hueco en el piso, para que el nivel de arranque ya tenga peligro
        // y se pueda mostrar el contador de vidas.
        var huecoDesde = mapa.WorldToCell(new Vector3(17f, PisoY - 0.01f, 0f)).x;
        var huecoHasta = mapa.WorldToCell(new Vector3(19f, PisoY - 0.01f, 0f)).x;

        for (var x = celdaIzquierda.x; x <= celdaDerecha.x; x++)
        {
            if (x >= huecoDesde && x <= huecoHasta) continue;
            mapa.SetTile(new Vector3Int(x, celdaIzquierda.y, 0), superficie);
            for (var capa = 1; capa <= 8; capa++)
                mapa.SetTile(new Vector3Int(x, celdaIzquierda.y - capa, 0), relleno);
        }

        // Dos plataformas flotantes de ejemplo, para que se vea que se puede.
        PintarPlataforma(mapa, superficie, 12f, 3.2f, 6);
        PintarPlataforma(mapa, superficie, 22f, 5.2f, 6);

        mapa.CompressBounds();
        mapa.RefreshAllTiles();

        PintarDecorado();
    }

    /// <summary>
    /// Un fondo minimo pintado por nosotros: montanas lejanas y nubes.
    /// El resto lo pintan los estudiantes.
    /// </summary>
    static void PintarDecorado()
    {
        var nube = AssetDatabase.LoadAssetAtPath<TileBase>(RutaTileNube);
        var montana = AssetDatabase.LoadAssetAtPath<TileBase>(RutaTileMontana);

        var fondoLejano = BuscarTilemapPorNombre("farbackground");
        if (fondoLejano != null && montana != null)
        {
            var fila = fondoLejano.WorldToCell(new Vector3(0f, PisoY - 1f, 0f)).y;
            for (var x = -30; x <= 130; x += 10)
                fondoLejano.SetTile(new Vector3Int(x, fila, 0), montana);
            fondoLejano.CompressBounds();
        }

        var fondo = BuscarTilemapPorNombre("background");
        if (fondo != null && nube != null)
        {
            var alturas = new[] { 14, 19, 16, 22, 15, 20, 17, 21, 16, 18, 23, 15 };
            for (var i = 0; i < alturas.Length; i++)
                fondo.SetTile(new Vector3Int(-16 + i * 12, alturas[i], 0), nube);
            fondo.CompressBounds();
        }
    }

    static Tilemap BuscarTilemapPorNombre(string nombre)
    {
        foreach (var mapa in Object.FindObjectsByType<Tilemap>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (mapa.gameObject.name.Trim().ToLowerInvariant() == nombre) return mapa;
        return null;
    }

    /// <summary>Con el fondo vacio hace falta un cielo de verdad detras.</summary>
    static void PintarCielo()
    {
        var camara = Camera.main;
        if (camara == null) return;
        camara.clearFlags = CameraClearFlags.SolidColor;
        camara.backgroundColor = new Color(0.42f, 0.72f, 0.96f);
        EditorUtility.SetDirty(camara);
    }

    static void PintarPlataforma(Tilemap mapa, TileBase tile, float x, float y, int anchoEnCeldas)
    {
        var celda = mapa.WorldToCell(new Vector3(x, y, 0f));
        for (var i = 0; i < anchoEnCeldas; i++)
            mapa.SetTile(new Vector3Int(celda.x + i, celda.y, 0), tile);
    }

    static Tilemap BuscarTilemapSolido()
    {
        foreach (var mapa in Object.FindObjectsByType<Tilemap>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (mapa.GetComponent<TilemapCollider2D>() != null) return mapa;
        return null;
    }

    /// <summary>
    /// Sin CompositeCollider2D cada tile es un collider independiente y el jugador
    /// engancha en las costuras internas entre tiles, sobre todo al saltar pegado a
    /// una pared. Fusionarlos deja un solo contorno y las paredes quedan lisas.
    /// </summary>
    static void FusionarColisionesDelTilemap()
    {
        foreach (var mapa in Object.FindObjectsByType<Tilemap>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var colision = mapa.GetComponent<TilemapCollider2D>();
            if (colision == null) continue;

            // CompositeCollider2D exige un Rigidbody2D. Tiene que ser estatico o el
            // suelo se cae al darle Play.
            var cuerpo = mapa.GetComponent<Rigidbody2D>();
            if (cuerpo == null) cuerpo = mapa.gameObject.AddComponent<Rigidbody2D>();
            cuerpo.bodyType = RigidbodyType2D.Static;

            var compuesto = mapa.GetComponent<CompositeCollider2D>();
            if (compuesto == null) compuesto = mapa.gameObject.AddComponent<CompositeCollider2D>();
            compuesto.geometryType = CompositeCollider2D.GeometryType.Polygons;
            compuesto.generationType = CompositeCollider2D.GenerationType.Synchronous;

            colision.compositeOperation = Collider2D.CompositeOperation.Merge;
            compuesto.GenerateGeometry();

            EditorUtility.SetDirty(mapa.gameObject);
            Debug.Log("[Taller] Colisiones fusionadas en el tilemap '" + mapa.gameObject.name + "'.");
        }
    }

    static void ReubicarPuntosClave()
    {
        // Ojo: el objeto "SpawnPoint" del template NO lleva el componente SpawnPoint.
        // La referencia que de verdad usa PlayerSpawn al reaparecer es la del modelo
        // del GameController, asi que hay que mover esa. Si se mueve otra, el jugador
        // arranca bien pero reaparece donde estaba la salida del nivel original.
        var salida = UbicarPuntoDeSalida();
        if (salida == null)
        {
            Debug.LogError("[Taller] No encontre el punto de salida (model.spawnPoint).");
        }
        else
        {
            salida.position = new Vector3(SalidaX, PisoY + 1.5f, 0f);
            if (salida.GetComponent<SpawnPoint>() == null) salida.gameObject.AddComponent<SpawnPoint>();
            EditorUtility.SetDirty(salida.gameObject);
        }

        var jugador = Object.FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);
        if (jugador == null) Debug.LogError("[Taller] No encontre al jugador.");
        else jugador.transform.position = new Vector3(SalidaX, PisoY + 1.5f, 0f);

        var meta = Object.FindFirstObjectByType<VictoryZone>(FindObjectsInactive.Include);
        if (meta == null) Debug.LogError("[Taller] No encontre la meta (VictoryZone).");
        else MontarMeta(meta);

        // Una sola zona de muerte, ancha y debajo de todo el nivel.
        var zonas = Object.FindObjectsByType<DeathZone>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (zonas.Length == 0) Debug.LogError("[Taller] No encontre ninguna DeathZone.");
        for (var i = 1; i < zonas.Length; i++)
            if (zonas[i] != null) Object.DestroyImmediate(zonas[i].gameObject);

        if (zonas.Length > 0 && zonas[0] != null)
        {
            zonas[0].transform.position = new Vector3((NivelDesdeX + NivelHastaX) * 0.5f, PisoY - 12f, 0f);
            zonas[0].transform.localScale = Vector3.one;
            var caja = zonas[0].GetComponent<BoxCollider2D>();
            if (caja != null)
            {
                caja.offset = Vector2.zero;
                caja.size = new Vector2((NivelHastaX - NivelDesdeX) + 60f, 10f);
            }
        }
    }

    static Transform UbicarPuntoDeSalida()
    {
        var control = Object.FindFirstObjectByType<GameController>(FindObjectsInactive.Include);
        if (control != null && control.model != null && control.model.spawnPoint != null)
            return control.model.spawnPoint;

        var marca = Object.FindFirstObjectByType<SpawnPoint>(FindObjectsInactive.Include);
        return marca != null ? marca.transform : null;
    }

    /// <summary>
    /// La meta del template es un trigger invisible: en el nivel original se
    /// entendia por los tiles del fondo, que aqui borramos. Le colgamos un arco
    /// para que se vea a donde hay que llegar, y ajustamos el trigger al arco.
    /// </summary>
    static void MontarMeta(VictoryZone meta)
    {
        meta.transform.localScale = Vector3.one;
        meta.transform.position = new Vector3(MetaX, PisoY, 0f);

        var marca = meta.transform.Find("Marca");
        if (marca == null)
        {
            var arco = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabArco);
            if (arco != null)
            {
                var instancia = (GameObject)PrefabUtility.InstantiatePrefab(arco);
                instancia.name = "Marca";
                instancia.transform.SetParent(meta.transform, false);
                instancia.transform.localPosition = new Vector3(0f, 0f, 0.5f);
                marca = instancia.transform;
            }
        }

        var caja = meta.GetComponent<BoxCollider2D>();
        var dibujo = marca != null ? marca.GetComponentInChildren<SpriteRenderer>() : null;

        if (dibujo != null && dibujo.sprite != null)
        {
            // Subimos la meta para que la base del arco quede apoyada en el piso.
            var limites = dibujo.sprite.bounds;
            meta.transform.position = new Vector3(MetaX, PisoY - limites.min.y, 0f);

            if (caja != null)
            {
                caja.offset = new Vector2(limites.center.x, limites.center.y);
                caja.size = new Vector2(Mathf.Max(1.5f, limites.size.x * 0.5f), limites.size.y);
            }
        }
        else if (caja != null)
        {
            meta.transform.position = new Vector3(MetaX, PisoY + 1.5f, 0f);
            caja.offset = Vector2.zero;
            caja.size = new Vector2(2f, 3f);
        }
    }

    static void SembrarContenidoInicial()
    {
        var padreMonedas = BuscarOCrear("Tokens");
        var monedaPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabMoneda);
        if (monedaPrefab != null)
        {
            ColocarPrefab(monedaPrefab, padreMonedas, new Vector3(5f, PisoY + 1.2f, 0f));
            ColocarPrefab(monedaPrefab, padreMonedas, new Vector3(13f, PisoY + 4.4f, 0f));
            ColocarPrefab(monedaPrefab, padreMonedas, new Vector3(23f, PisoY + 6.4f, 0f));
        }

        var padreEnemigos = BuscarOCrear("Enemies");
        var enemigoPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabEnemigo);
        if (enemigoPrefab != null)
            ColocarPrefab(enemigoPrefab, padreEnemigos, new Vector3(30f, PisoY + 1f, 0f));
    }

    static void ColocarPrefab(GameObject prefab, GameObject padre, Vector3 posicion)
    {
        var instancia = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        if (padre != null) instancia.transform.SetParent(padre.transform, false);
        instancia.transform.position = posicion;
    }

    static GameObject BuscarOCrear(string nombre)
    {
        var encontrado = GameObject.Find(nombre);
        if (encontrado != null) return encontrado;
        return new GameObject(nombre);
    }

    static void AjustarLimitesDeCamara()
    {
        foreach (var poligono in Object.FindObjectsByType<PolygonCollider2D>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!poligono.gameObject.name.ToLowerInvariant().Contains("confiner")) continue;
            poligono.transform.position = Vector3.zero;
            poligono.transform.localScale = Vector3.one;
            poligono.pathCount = 1;
            poligono.SetPath(0, new[]
            {
                new Vector2(NivelDesdeX - 2f, PisoY - 6f),
                new Vector2(NivelHastaX + 2f, PisoY - 6f),
                new Vector2(NivelHastaX + 2f, PisoY + 18f),
                new Vector2(NivelDesdeX - 2f, PisoY + 18f),
            });
        }
    }

    static void RegistrarEnBuildSettings()
    {
        var escenas = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if (!escenas.Exists(e => e.path == RutaNivelTaller))
            escenas.Add(new EditorBuildSettingsScene(RutaNivelTaller, true));
        EditorBuildSettings.scenes = escenas.ToArray();
    }

    // ------------------------------------------------------------------
    // Prefabs
    // ------------------------------------------------------------------

    static void CrearPrefabMoneda()
    {
        var moneda = Object.FindFirstObjectByType<TokenInstance>(FindObjectsInactive.Include);
        if (moneda == null) { Debug.LogError("[Taller] No hay ninguna moneda en la escena base."); return; }

        var copia = Object.Instantiate(moneda.gameObject);
        copia.name = "Moneda";
        copia.transform.position = Vector3.zero;
        PrefabUtility.SaveAsPrefabAsset(copia, RutaPrefabMoneda);
        Object.DestroyImmediate(copia);
    }

    static void CrearPrefabEnemigo()
    {
        var enemyBase = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabEnemyBase);
        if (enemyBase == null) { Debug.LogError("[Taller] Falta Assets/Prefabs/Enemy.prefab"); return; }

        var raiz = new GameObject("Enemigo");
        var ruta = raiz.AddComponent<PatrolPath>();
        ruta.startPosition = new Vector2(-3f, 0f);
        ruta.endPosition = new Vector2(3f, 0f);

        var cuerpo = (GameObject)PrefabUtility.InstantiatePrefab(enemyBase);
        cuerpo.transform.SetParent(raiz.transform, false);
        var control = cuerpo.GetComponent<EnemyController>();
        if (control != null)
        {
            control.path = ruta;
            PrefabUtility.RecordPrefabInstancePropertyModifications(control);
        }

        PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefabEnemigo);
        Object.DestroyImmediate(raiz);
    }

    /// <summary>
    /// Los poderes van en el prefab del jugador, no en la instancia de la escena,
    /// para que funcionen en cualquier escena y en cualquier Player que se arrastre.
    /// </summary>
    static void AgregarPoderesAlJugador()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabJugador);
        if (prefab == null) { Debug.LogError("[Taller] Falta Assets/Prefabs/Player.prefab"); return; }
        if (prefab.GetComponent<PoderesDelJugador>() != null) return;

        var contenido = PrefabUtility.LoadPrefabContents(RutaPrefabJugador);
        contenido.AddComponent<PoderesDelJugador>();
        PrefabUtility.SaveAsPrefabAsset(contenido, RutaPrefabJugador);
        PrefabUtility.UnloadPrefabContents(contenido);
        Debug.Log("[Taller] Poderes agregados a Player.prefab");
    }

    // ------------------------------------------------------------------
    // HUD
    // ------------------------------------------------------------------

    // Paleta del HUD, sacada del propio juego.
    static readonly Color TintaHud = new Color(0.94f, 0.97f, 1f);
    static readonly Color FondoHud = new Color(0.04f, 0.09f, 0.15f, 0.66f);
    static readonly Color FondoHudSuave = new Color(0.04f, 0.09f, 0.15f, 0.48f);
    static readonly Color TintaSuave = new Color(0.70f, 0.81f, 0.91f);
    static readonly Color AmarilloMoneda = new Color(0.98f, 0.80f, 0.24f);
    static readonly Color VerdeVictoria = new Color(0.36f, 0.87f, 0.55f);
    static readonly Color RojoDerrota = new Color(0.93f, 0.40f, 0.35f);

    static void ConstruirHudYReglas()
    {
        var anterior = GameObject.Find("HUD Taller");
        if (anterior != null) Object.DestroyImmediate(anterior);

        var canvasGO = new GameObject("HUD Taller", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var escalador = canvasGO.GetComponent<CanvasScaler>();
        escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escalador.referenceResolution = new Vector2(1920f, 1080f);
        escalador.matchWidthOrHeight = 0.5f;

        var lienzo = canvasGO.transform;

        // --- Monedas: icono de la moneda + contador, arriba a la izquierda ---
        var marcadorMonedas = CrearPastilla(lienzo, "Marcador Monedas",
            new Vector2(0f, 1f), new Vector2(36f, -30f), new Vector2(276f, 96f), FondoHud);
        CrearIcono(marcadorMonedas.transform, "Icono", SpriteDeLaMoneda(), new Vector2(22f, 0f), 58f, TinteDe(RutaPrefabMoneda));
        var textoMonedas = CrearEtiqueta(marcadorMonedas.transform, "Numero", "0 / 5", 52,
            96f, 22f, TextAlignmentOptions.Left, TintaHud);
        AjustarSolo(textoMonedas);

        // --- Vidas: cara del personaje + "x N", arriba a la derecha ---
        var marcadorVidas = CrearPastilla(lienzo, "Marcador Vidas",
            new Vector2(1f, 1f), new Vector2(-36f, -30f), new Vector2(250f, 96f), FondoHud);
        CrearIcono(marcadorVidas.transform, "Icono", SpriteDelJugador(), new Vector2(4f, 15f), 142f, TinteDe(RutaPrefabJugador));
        var textoVidas = CrearEtiqueta(marcadorVidas.transform, "Numero", "\u00d7 3", 52,
            124f, 22f, TextAlignmentOptions.Left, TintaHud);
        AjustarSolo(textoVidas);

        // --- Puntos: debajo de las vidas, mas discreto ---
        var marcadorPuntos = CrearPastilla(lienzo, "Marcador Puntos",
            new Vector2(1f, 1f), new Vector2(-36f, -140f), new Vector2(250f, 62f), FondoHudSuave);
        var textoPuntos = CrearEtiqueta(marcadorPuntos.transform, "Numero", "0 PTS", 34,
            20f, 22f, TextAlignmentOptions.Right, TintaSuave);
        AjustarSolo(textoPuntos);

        // --- Mensaje del objetivo, abajo en el centro ---
        var cartelMensaje = CrearPastilla(lienzo, "Cartel Mensaje",
            new Vector2(0.5f, 0f), new Vector2(0f, 46f), new Vector2(600f, 78f), FondoHud);
        var textoMensaje = CenirAlTexto(cartelMensaje, "Texto", "Te faltan 5 monedas", 42, AmarilloMoneda);

        TextMeshProUGUI resumenVictoria, resumenDerrota;
        var panelVictoria = CrearPanelFinal(lienzo, "Panel Victoria", "GANASTE", VerdeVictoria, out resumenVictoria);
        var panelDerrota = CrearPanelFinal(lienzo, "Panel Derrota", "PERDISTE", RojoDerrota, out resumenDerrota);

        var gameController = Object.FindFirstObjectByType<GameController>(FindObjectsInactive.Include);
        if (gameController == null) { Debug.LogError("[Taller] No hay GameController en la escena."); return; }

        var reglas = gameController.GetComponent<ReglasDelJuego>();
        if (reglas == null) reglas = gameController.gameObject.AddComponent<ReglasDelJuego>();

        reglas.meta = Object.FindFirstObjectByType<VictoryZone>(FindObjectsInactive.Include);
        reglas.textoMonedas = textoMonedas;
        reglas.textoVidas = textoVidas;
        reglas.textoPuntos = textoPuntos;
        reglas.textoMensaje = textoMensaje;
        reglas.cartelMensaje = cartelMensaje;
        reglas.panelVictoria = panelVictoria;
        reglas.panelDerrota = panelDerrota;
        reglas.resumenVictoria = resumenVictoria;
        reglas.resumenDerrota = resumenDerrota;
        EditorUtility.SetDirty(reglas);

        panelVictoria.SetActive(false);
        panelDerrota.SetActive(false);
    }

    /// <summary>
    /// Pastilla redondeada de 9 cortes, dibujada por nosotros. Se genera una sola vez
    /// como asset del proyecto: asi los estudiantes pueden verla y cambiarla, y no
    /// dependemos de los recursos internos del editor.
    /// </summary>
    static Sprite SpritePastilla()
    {
        var existente = AssetDatabase.LoadAssetAtPath<Sprite>(RutaPastilla);
        if (existente != null) return existente;

        AsegurarCarpetas();

        const int lado = 48;
        const int radio = 16;
        var textura = new Texture2D(lado, lado, TextureFormat.RGBA32, false);
        for (var y = 0; y < lado; y++)
        {
            for (var x = 0; x < lado; x++)
            {
                // Distancia a la esquina redondeada mas cercana.
                var dx = Mathf.Max((float)(radio - x), 0f, (float)(x - (lado - 1 - radio)));
                var dy = Mathf.Max((float)(radio - y), 0f, (float)(y - (lado - 1 - radio)));
                var distancia = Mathf.Sqrt(dx * dx + dy * dy);
                var alfa = Mathf.Clamp01(radio - distancia + 0.5f);
                textura.SetPixel(x, y, new Color(1f, 1f, 1f, alfa));
            }
        }
        textura.Apply();
        File.WriteAllBytes(RutaPastilla, textura.EncodeToPNG());
        Object.DestroyImmediate(textura);
        AssetDatabase.ImportAsset(RutaPastilla, ImportAssetOptions.ForceUpdate);

        var importador = (TextureImporter)AssetImporter.GetAtPath(RutaPastilla);
        importador.textureType = TextureImporterType.Sprite;
        importador.spriteImportMode = SpriteImportMode.Single;
        importador.spriteBorder = new Vector4(radio, radio, radio, radio);
        importador.alphaIsTransparency = true;
        importador.mipmapEnabled = false;
        importador.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(RutaPastilla);
    }

    /// <summary>Deja que el numero se encoja si el estudiante pone valores enormes.</summary>
    static void AjustarSolo(TextMeshProUGUI etiqueta)
    {
        var maximo = etiqueta.fontSize;
        etiqueta.enableAutoSizing = true;
        etiqueta.fontSizeMin = 24f;
        etiqueta.fontSizeMax = maximo;
    }

    /// <summary>
    /// El cartel del mensaje no puede ser una barra fija: el texto cambia de largo.
    /// Con un layout horizontal y un ContentSizeFitter, la pastilla se cine al texto.
    /// </summary>
    static TextMeshProUGUI CenirAlTexto(GameObject pastilla, string nombre, string texto, int tamano, Color color)
    {
        var grupo = pastilla.AddComponent<HorizontalLayoutGroup>();
        grupo.padding = new RectOffset(36, 36, 12, 14);
        grupo.childAlignment = TextAnchor.MiddleCenter;
        grupo.childControlWidth = true;
        grupo.childControlHeight = true;
        grupo.childForceExpandWidth = false;
        grupo.childForceExpandHeight = false;

        var cenidor = pastilla.AddComponent<ContentSizeFitter>();
        cenidor.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        cenidor.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(pastilla.transform, false);

        var etiqueta = go.AddComponent<TextMeshProUGUI>();
        etiqueta.text = texto;
        etiqueta.fontSize = tamano;
        etiqueta.alignment = TextAlignmentOptions.Center;
        etiqueta.color = color;
        etiqueta.fontStyle = FontStyles.Bold;
        etiqueta.raycastTarget = false;

        LayoutRebuilder.ForceRebuildLayoutImmediate(pastilla.GetComponent<RectTransform>());
        return etiqueta;
    }

    /// <summary>
    /// El arte del jugador es gris: el turquesa sale del color del SpriteRenderer.
    /// Sin copiar ese tinte, el icono del HUD saldria blanco.
    /// </summary>
    static Color TinteDe(string rutaPrefab)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(rutaPrefab);
        var dibujo = prefab != null ? prefab.GetComponent<SpriteRenderer>() : null;
        return dibujo != null ? dibujo.color : Color.white;
    }

    static Sprite SpriteDeLaMoneda()
    {
        var moneda = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabMoneda);
        if (moneda == null) return null;
        var ficha = moneda.GetComponent<TokenInstance>();
        if (ficha != null && ficha.idleAnimation != null && ficha.idleAnimation.Length > 0)
            return ficha.idleAnimation[0];
        var dibujo = moneda.GetComponent<SpriteRenderer>();
        return dibujo != null ? dibujo.sprite : null;
    }

    static Sprite SpriteDelJugador()
    {
        var jugador = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabJugador);
        if (jugador == null) return null;
        var dibujo = jugador.GetComponent<SpriteRenderer>();
        return dibujo != null ? dibujo.sprite : null;
    }

    /// <summary>Rectangulo redondeado de fondo: el ladrillo del HUD.</summary>
    static GameObject CrearPastilla(Transform padre, string nombre, Vector2 ancla,
        Vector2 posicion, Vector2 medida, Color color)
    {
        var go = new GameObject(nombre, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(padre, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = ancla;
        rect.anchorMax = ancla;
        rect.pivot = ancla;
        rect.anchoredPosition = posicion;
        rect.sizeDelta = medida;

        var imagen = go.GetComponent<Image>();
        imagen.sprite = SpritePastilla();
        imagen.type = Image.Type.Sliced;
        imagen.color = color;
        imagen.raycastTarget = false;
        return go;
    }

    /// <summary>
    /// Ojo con los tamanos: el frame del jugador mide 128x126 pero el alien solo
    /// ocupa unos 60x57 abajo a la izquierda, asi que su icono necesita una caja
    /// bastante mas grande y un empujon en "posicion" para quedar centrado.
    /// </summary>
    static Image CrearIcono(Transform padre, string nombre, Sprite sprite, Vector2 posicion, float lado, Color tinte)
    {
        var go = new GameObject(nombre, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(padre, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0.5f);
        rect.anchorMax = new Vector2(0f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = posicion;
        rect.sizeDelta = new Vector2(lado, lado);

        var imagen = go.GetComponent<Image>();
        imagen.sprite = sprite;
        imagen.color = tinte;
        imagen.preserveAspect = true;
        imagen.raycastTarget = false;
        if (sprite == null) imagen.enabled = false;
        return imagen;
    }

    /// <summary>Texto que rellena su pastilla, con margenes a lado y lado.</summary>
    static TextMeshProUGUI CrearEtiqueta(Transform padre, string nombre, string texto, int tamano,
        float margenIzquierdo, float margenDerecho, TextAlignmentOptions alineacion, Color color)
    {
        var go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(padre, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(margenIzquierdo, 0f);
        rect.offsetMax = new Vector2(-margenDerecho, 0f);

        var etiqueta = go.AddComponent<TextMeshProUGUI>();
        etiqueta.text = texto;
        etiqueta.fontSize = tamano;
        etiqueta.alignment = alineacion;
        etiqueta.color = color;
        etiqueta.fontStyle = FontStyles.Bold;
        etiqueta.raycastTarget = false;
        return etiqueta;
    }

    static GameObject CrearPanelFinal(Transform padre, string nombre, string titulo, Color acento,
        out TextMeshProUGUI resumen)
    {
        var panel = new GameObject(nombre, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(padre, false);

        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var velo = panel.GetComponent<Image>();
        velo.color = new Color(0.02f, 0.05f, 0.09f, 0.82f);
        velo.raycastTarget = false;

        var tarjeta = CrearPastilla(panel.transform, "Tarjeta", new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(940f, 424f), new Color(0.06f, 0.12f, 0.19f, 0.98f));

        var franja = CrearPastilla(tarjeta.transform, "Franja", new Vector2(0.5f, 1f),
            new Vector2(0f, -18f), new Vector2(860f, 10f), acento);

        var textoTitulo = CrearEtiqueta(tarjeta.transform, "Titulo", titulo, 128, 40f, 40f,
            TextAlignmentOptions.Center, acento);
        var rectTitulo = textoTitulo.GetComponent<RectTransform>();
        rectTitulo.anchorMin = new Vector2(0f, 0.5f);
        rectTitulo.anchorMax = new Vector2(1f, 0.5f);
        rectTitulo.pivot = new Vector2(0.5f, 0.5f);
        rectTitulo.anchoredPosition = new Vector2(0f, 78f);
        rectTitulo.sizeDelta = new Vector2(-80f, 150f);

        var textoResumen = CrearEtiqueta(tarjeta.transform, "Resumen", "Monedas 5 / 5     Puntos 250",
            42, 40f, 40f, TextAlignmentOptions.Center, AmarilloMoneda);
        var rectResumen = textoResumen.GetComponent<RectTransform>();
        rectResumen.anchorMin = new Vector2(0f, 0.5f);
        rectResumen.anchorMax = new Vector2(1f, 0.5f);
        rectResumen.pivot = new Vector2(0.5f, 0.5f);
        rectResumen.anchoredPosition = new Vector2(0f, -24f);
        rectResumen.sizeDelta = new Vector2(-80f, 64f);

        var textoTecla = CrearEtiqueta(tarjeta.transform, "Instruccion",
            "Presiona  R  para volver a jugar", 46, 40f, 40f, TextAlignmentOptions.Center, TintaSuave);
        var rectTecla = textoTecla.GetComponent<RectTransform>();
        rectTecla.anchorMin = new Vector2(0f, 0.5f);
        rectTecla.anchorMax = new Vector2(1f, 0.5f);
        rectTecla.pivot = new Vector2(0.5f, 0.5f);
        rectTecla.anchoredPosition = new Vector2(0f, -122f);
        rectTecla.sizeDelta = new Vector2(-80f, 80f);

        resumen = textoResumen;
        return panel;
    }

    static void AsegurarCarpetas()
    {
        if (!AssetDatabase.IsValidFolder("Assets/_Taller")) AssetDatabase.CreateFolder("Assets", "_Taller");
        foreach (var sub in new[] { "Scripts", "Editor", "Prefabs", "Scenes", "Arte" })
            if (!AssetDatabase.IsValidFolder("Assets/_Taller/" + sub))
                AssetDatabase.CreateFolder("Assets/_Taller", sub);
    }
}
