using System.Collections.Generic;
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
        ReubicarPuntosClave();
        SembrarContenidoInicial();
        AjustarLimitesDeCamara();
        ConstruirHudYReglas();

        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena);
        RegistrarEnBuildSettings();
        Debug.Log("[Taller] Nivel_Taller generado.");
    }

    [MenuItem("Taller/Preparar todo (2 + 3)", false, 20)]
    public static void PrepararTodo()
    {
        GenerarPrefabsDelTaller();
        GenerarNivelTaller();
        Debug.Log("[Taller] Preparacion completa.");
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

        var textoMonedas = CrearTexto(canvasGO.transform, "Monedas", "Monedas  0 / 5", 52,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(40f, -30f), new Vector2(600f, 70f), TextAlignmentOptions.TopLeft);

        var textoVidas = CrearTexto(canvasGO.transform, "Vidas", "Vidas  3     Puntos  0", 52,
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-40f, -30f), new Vector2(700f, 70f), TextAlignmentOptions.TopRight);

        var textoMensaje = CrearTexto(canvasGO.transform, "Mensaje", "", 44,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 60f), new Vector2(1400f, 70f), TextAlignmentOptions.Bottom);
        textoMensaje.color = new Color(1f, 0.92f, 0.35f);

        var panelVictoria = CrearPanel(canvasGO.transform, "Panel Victoria", "GANASTE",
            new Color(0.06f, 0.35f, 0.15f, 0.88f));
        var panelDerrota = CrearPanel(canvasGO.transform, "Panel Derrota", "PERDISTE",
            new Color(0.4f, 0.06f, 0.1f, 0.88f));

        var gameController = Object.FindFirstObjectByType<GameController>(FindObjectsInactive.Include);
        if (gameController == null) { Debug.LogError("[Taller] No hay GameController en la escena."); return; }

        var reglas = gameController.GetComponent<ReglasDelJuego>();
        if (reglas == null) reglas = gameController.gameObject.AddComponent<ReglasDelJuego>();

        reglas.meta = Object.FindFirstObjectByType<VictoryZone>(FindObjectsInactive.Include);
        reglas.textoMonedas = textoMonedas;
        reglas.textoVidas = textoVidas;
        reglas.textoMensaje = textoMensaje;
        reglas.panelVictoria = panelVictoria;
        reglas.panelDerrota = panelDerrota;
        EditorUtility.SetDirty(reglas);

        panelVictoria.SetActive(false);
        panelDerrota.SetActive(false);
    }

    static TextMeshProUGUI CrearTexto(Transform padre, string nombre, string texto, int tamano,
        Vector2 anclaMin, Vector2 anclaMax, Vector2 pivote, Vector2 posicion, Vector2 medida,
        TextAlignmentOptions alineacion)
    {
        var go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(padre, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anclaMin;
        rect.anchorMax = anclaMax;
        rect.pivot = pivote;
        rect.anchoredPosition = posicion;
        rect.sizeDelta = medida;

        var etiqueta = go.AddComponent<TextMeshProUGUI>();
        etiqueta.text = texto;
        etiqueta.fontSize = tamano;
        etiqueta.alignment = alineacion;
        etiqueta.color = Color.white;
        etiqueta.fontStyle = FontStyles.Bold;
        return etiqueta;
    }

    static GameObject CrearPanel(Transform padre, string nombre, string titulo, Color color)
    {
        var panel = new GameObject(nombre, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(padre, false);

        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        panel.GetComponent<Image>().color = color;

        CrearTexto(panel.transform, "Titulo", titulo, 140,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 60f), new Vector2(1600f, 200f), TextAlignmentOptions.Center);

        CrearTexto(panel.transform, "Instruccion", "Presiona  R  para volver a jugar", 56,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -90f), new Vector2(1600f, 120f), TextAlignmentOptions.Center);

        return panel;
    }

    static void AsegurarCarpetas()
    {
        if (!AssetDatabase.IsValidFolder("Assets/_Taller")) AssetDatabase.CreateFolder("Assets", "_Taller");
        foreach (var sub in new[] { "Scripts", "Editor", "Prefabs", "Scenes" })
            if (!AssetDatabase.IsValidFolder("Assets/_Taller/" + sub))
                AssetDatabase.CreateFolder("Assets/_Taller", sub);
    }
}
