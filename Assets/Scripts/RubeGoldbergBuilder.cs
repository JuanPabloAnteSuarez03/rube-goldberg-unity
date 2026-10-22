using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Construye una máquina de Rube Goldberg de 12 toques usando primitivas de Unity.
/// Todo se genera por código: no hace falta arrastrar prefabs ni configurar nada a mano.
///
/// Cadena:
///   1-6  Bola 1 → 6 fichas de dominó en la mesa alta
///   7    La última ficha derriba la torre de la mesa baja
///   8-11 La torre y las fichas 8, 9, 10 y 11
///   12   La ficha 11 empuja la ficha final, que cae en el plato de meta
/// </summary>
public static class RubeGoldbergBuilder
{
    public const string RootName = "Maquina Rube Goldberg";
    public const int ToquesTotal = 12;

    // ------------------- Medidas principales (edítalas si quieres) -------------------
    const float MesaAlto  = 1.00f;   // altura de la mesa alta
    const float MesaBaja  = 0.45f;   // altura de la mesa baja
    const float BolaR     = 0.25f;   // radio de la bola 1

    public static int   Fichas       = 6;       // fichas de la mesa alta
    public static float FichaX0      = -1.00f;  // posición X de la primera ficha
    public static float FichaPaso    = 0.60f;   // separación entre fichas
    public static float FichaAlto    = 1.10f;
    public static float FichaEspesor = 0.16f;
    public static float FichaAncho   = 0.62f;
    public static float FichaMasa    = 0.45f;   // masa de cada ficha
    public static float MasaPrimera  = 0.90f;   // masa de la primera ficha

    // Segunda mesa (más baja): x de 2.55 a 5.45
    const float Mesa2X0 = 2.55f;
    const float Mesa2X1 = 5.45f;
    const float Mesa2Z  = 0.85f;

    // Fichas de la mesa baja. La primera es alta ("torre") para que la derribe
    // el último dominó de la mesa alta.
    public static int   Fichas2       = 5;
    public static float Ficha2X0      = 3.10f;
    public static float Ficha2Paso    = 0.42f;
    public static float Ficha2Alto    = 0.80f;
    public static float Ficha2Espesor = 0.09f;
    public static float Ficha2Ancho   = 0.46f;
    public static float Ficha2Masa    = 0.35f;

    public static float FichaTorreAlto    = 1.50f;
    public static float FichaTorreEspesor = 0.12f;
    public static float FichaTorreAncho   = 0.60f;
    public static float FichaTorreMasa    = 0.55f;

    public static float FichaFinalX    = 5.20f;
    public static float FichaFinalMasa = 0.30f;

    const float ParedZ = 10.0f;   // pared del fondo

    // ------------------- Paleta -------------------
    static Color C(int r, int g, int b) { return new Color(r / 255f, g / 255f, b / 255f); }

    static readonly Color ColSuelo     = C(233, 239, 245);
    static readonly Color ColPared     = C(203, 217, 231);
    static readonly Color ColRampa     = C(242, 138, 48);
    static readonly Color ColBaranda   = C(70, 78, 88);
    static readonly Color ColMadera    = C(152, 108, 66);
    static readonly Color ColMadera2   = C(120, 86, 54);
    static readonly Color ColSoporte   = C(146, 158, 170);
    static readonly Color ColBolaA     = C(214, 40, 40);
    static readonly Color ColBolaB     = C(29, 78, 216);
    static readonly Color ColBolaC     = C(255, 214, 64);
    static readonly Color ColPlaca     = C(45, 55, 66);
    static readonly Color ColBorde     = C(245, 247, 250);
    static readonly Color ColPoste     = C(70, 80, 90);
    static readonly Color ColBandera   = C(255, 195, 0);
    static readonly Color ColTablero   = C(24, 32, 44);
    static readonly Color ColToqueOff  = C(120, 45, 45);
    static readonly Color ColToqueOn   = C(45, 230, 120);

    static readonly Color[] ColFichas =
    {
        C(230, 57, 70), C(244, 114, 43), C(242, 193, 78),
        C(67, 170, 139), C(39, 125, 161), C(106, 76, 147)
    };

    static readonly Color[] ColFichas2 =
    {
        C(0, 187, 167), C(72, 149, 239), C(233, 196, 106)
    };

    static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();

    // =====================================================================
    //  PUNTO DE ENTRADA
    // =====================================================================
    public static GameObject Build()
    {
        var existente = GameObject.Find(RootName);
        if (existente != null) return existente;

        cache.Clear();
        var root = new GameObject(RootName);
        var tr = root.transform;

        // ---------------- 0. Entorno (suelo, pared, cámara, luces) ----------------
        var camara = CrearEntorno();

        // ---------------- 1. Rampa con la bola 1 ----------------
        Vector3 rampaInicio = new Vector3(-8.00f, 2.60f, 0f);
        Vector3 rampaFin    = new Vector3(-2.45f, MesaAlto + 0.02f, 0f);
        CrearRampa(tr, "Rampa", rampaInicio, rampaFin, 1.05f, 0.22f, 0.20f);

        // soportes de la rampa (quedan justo debajo, sin sobresalir)
        Pilar(tr, "Soporte rampa 1", -7.40f, 2.20f, 0.50f, 1.30f);
        Pilar(tr, "Soporte rampa 2", -4.90f, 1.48f, 0.40f, 1.20f);

        // bola 1: espera quieta arriba de la rampa y luego se suelta
        float yRampa = AlturaRampa(rampaInicio, rampaFin, -7.15f);
        var bolaA = Bola(tr, "Bola 1", new Vector3(-7.15f, yRampa + BolaR + 0.02f, 0f),
                         BolaR, Mat("BolaA", ColBolaA, 0.75f), 1.0f, true);

        // ---------------- 2. Mesa alta donde corre el primer dominó ----------------
        Prim(PrimitiveType.Cube, "Mesa alta", tr,
             new Vector3(-0.025f, MesaAlto * 0.5f, 0f),
             new Vector3(5.15f, MesaAlto, 1.90f),
             Quaternion.identity, Mat("Madera", ColMadera, 0.25f));

        // ---------------- 3. Primer dominó (toques 1-6) ----------------
        var fichasA = new List<Rigidbody>();
        for (int i = 0; i < Fichas; i++)
        {
            float x = FichaX0 + i * FichaPaso;
            var ficha = Prim(PrimitiveType.Cube, "Ficha " + (i + 1), tr,
                             new Vector3(x, MesaAlto + FichaAlto * 0.5f, 0f),
                             new Vector3(FichaEspesor, FichaAlto, FichaAncho),
                             Quaternion.identity, Mat("Ficha" + i, ColFichas[i % ColFichas.Length], 0.3f));
            // la primera ficha pesa un poco más para frenar la bola
            Cuerpo(ficha, i == 0 ? MasaPrimera : FichaMasa);
            fichasA.Add(ficha.GetComponent<Rigidbody>());
        }

        // ---------------- 4. Mesa baja (un escalón por debajo de la mesa alta) ----------------
        float mesa2Ancho = Mesa2X1 - Mesa2X0;
        Prim(PrimitiveType.Cube, "Mesa baja", tr,
             new Vector3((Mesa2X0 + Mesa2X1) * 0.5f, MesaBaja * 0.5f, 0f),
             new Vector3(mesa2Ancho, MesaBaja, Mesa2Z * 2f),
             Quaternion.identity, Mat("Madera2", ColMadera2, 0.25f));

        // barandillas laterales para que las fichas no se salgan
        for (int lado = -1; lado <= 1; lado += 2)
        {
            Prim(PrimitiveType.Cube, "Barandilla mesa baja", tr,
                 new Vector3((Mesa2X0 + Mesa2X1) * 0.5f, MesaBaja + 0.08f, lado * (Mesa2Z - 0.03f)),
                 new Vector3(mesa2Ancho, 0.16f, 0.06f),
                 Quaternion.identity, Mat("Baranda", ColBaranda, 0.4f));
        }

        // ---------------- 5. Segundo dominó: torre + 4 fichas + ficha final (toques 7-12) ----------------
        var fichasB = new List<Rigidbody>();

        // La "torre": es alta para que el dominó de la mesa alta la alcance al caer.
        var torre = Prim(PrimitiveType.Cube, "Ficha " + (Fichas + 1) + " (torre)", tr,
                         new Vector3(Ficha2X0, MesaBaja + FichaTorreAlto * 0.5f, 0f),
                         new Vector3(FichaTorreEspesor, FichaTorreAlto, FichaTorreAncho),
                         Quaternion.identity, Mat("Ficha2_0", ColFichas2[0], 0.3f));
        Cuerpo(torre, FichaTorreMasa);
        fichasB.Add(torre.GetComponent<Rigidbody>());

        for (int i = 1; i < Fichas2; i++)
        {
            float x = Ficha2X0 + i * Ficha2Paso;
            var ficha = Prim(PrimitiveType.Cube, "Ficha " + (Fichas + i + 1), tr,
                             new Vector3(x, MesaBaja + Ficha2Alto * 0.5f, 0f),
                             new Vector3(Ficha2Espesor, Ficha2Alto, Ficha2Ancho),
                             Quaternion.identity, Mat("Ficha2_" + i, ColFichas2[i % ColFichas2.Length], 0.3f));
            Cuerpo(ficha, Ficha2Masa);
            fichasB.Add(ficha.GetComponent<Rigidbody>());
        }

        // Ficha final: al borde de la mesa baja. Cuando cae, entra en el plato de meta.
        var fichaFinal = Prim(PrimitiveType.Cube, "Ficha final", tr,
                              new Vector3(FichaFinalX, MesaBaja + Ficha2Alto * 0.5f, 0f),
                              new Vector3(Ficha2Espesor, Ficha2Alto, Ficha2Ancho),
                              Quaternion.identity, Mat("FichaFinal", ColBolaC, 0.5f));
        Cuerpo(fichaFinal, FichaFinalMasa);

        // ---------------- 6. Meta: plato, bandera y lámpara ----------------
        const float metaX = 6.00f;
        Prim(PrimitiveType.Cube, "Plato de meta", tr,
             new Vector3(metaX, 0.07f, 0f),
             new Vector3(2.40f, 0.14f, 2.40f),
             Quaternion.identity, Mat("Placa", ColPlaca, 0.5f));

        // bordes (el lado que mira a la máquina queda abierto)
        Prim(PrimitiveType.Cube, "Borde lejos", tr, new Vector3(metaX + 1.19f, 0.34f, 0f),
             new Vector3(0.14f, 0.40f, 2.40f), Quaternion.identity, Mat("Borde", ColBorde, 0.6f));
        Prim(PrimitiveType.Cube, "Borde A", tr, new Vector3(metaX, 0.34f, 1.13f),
             new Vector3(2.40f, 0.40f, 0.14f), Quaternion.identity, Mat("Borde", ColBorde, 0.6f));
        Prim(PrimitiveType.Cube, "Borde B", tr, new Vector3(metaX, 0.34f, -1.13f),
             new Vector3(2.40f, 0.40f, 0.14f), Quaternion.identity, Mat("Borde", ColBorde, 0.6f));

        // poste + bandera + lámpara
        var poste = Prim(PrimitiveType.Cylinder, "Poste", tr,
                         new Vector3(metaX + 0.95f, 1.52f, 0.82f),
                         new Vector3(0.12f, 1.38f, 0.12f),
                         Quaternion.identity, Mat("Poste", ColPoste, 0.6f));

        var bandera = Prim(PrimitiveType.Cube, "Bandera", tr,
                           new Vector3(metaX + 1.31f, 0.65f, 0.82f),
                           new Vector3(0.70f, 0.42f, 0.04f),
                           Quaternion.identity, Mat("Bandera", ColBandera, 0.3f), false);

        var lampara = Prim(PrimitiveType.Sphere, "Lampara", tr,
                           new Vector3(metaX + 0.95f, 2.97f, 0.82f),
                           Vector3.one * 0.30f,
                           Quaternion.identity, Mat("LamparaOff", C(120, 45, 45), 0.8f), false);

        var lamparaOn = Mat("LamparaOn", ColToqueOn, 0.9f);
        Emisivo(lamparaOn, ColToqueOn, 2.2f);
        var lamparaOff = Mat("LamparaOff", C(120, 45, 45), 0.8f);

        // zona invisible que detecta la bola final en la meta.
        // Está por debajo de la mesa baja (Y max 0.38) para no activarse antes de que caiga.
        var zona = new GameObject("Zona de meta");
        zona.transform.SetParent(tr, false);
        zona.transform.localPosition = new Vector3(metaX - 0.35f, 0.24f, 0f);
        var zonaCol = zona.AddComponent<BoxCollider>();
        zonaCol.size = new Vector3(2.00f, 0.38f, 2.20f);
        zonaCol.isTrigger = true;

        var confeti = new GameObject("Confeti");
        confeti.transform.SetParent(tr, false);

        // ---------------- 7. Marcador de los 12 toques ----------------
        var indicadores = CrearMarcador(tr);

        // ---------------- 8. Controlador del juego ----------------
        var control = root.AddComponent<MachineController>();
        control.bola1 = bolaA.transform;
        control.objetoFinal = fichaFinal.GetComponent<Rigidbody>();
        control.bandera = bandera.transform;
        control.banderaAbajo = 0.65f;
        control.banderaArriba = 2.45f;
        control.lampara = lampara.GetComponent<Renderer>();
        control.lamparaOn = lamparaOn;
        control.lamparaOff = lamparaOff;
        control.confeti = confeti.transform;
        control.indicadores = indicadores.ToArray();
        control.indicadorOn = Mat("MarcadorOn", ColToqueOn, 0.35f);
        Emisivo(control.indicadorOn, ColToqueOn, 1.8f);
        control.indicadorOff = Mat("MarcadorOff", ColToqueOff, 0.35f);

        control.toques = new List<MachineController.Toque>();

        // Toques 1-6: la bola 1 y el primer dominó
        control.toques.Add(Toque("La bola 1 golpea la ficha 1", MachineController.TipoToque.Inclinacion, fichasA[0], 18f));
        for (int i = 1; i < Fichas; i++)
            control.toques.Add(Toque("La ficha " + i + " golpea la ficha " + (i + 1),
                                     MachineController.TipoToque.Inclinacion, fichasA[i], 18f));

        // Toques 7-11: la torre y las fichas de la mesa baja
        control.toques.Add(Toque("La ficha " + Fichas + " derriba la torre",
                                 MachineController.TipoToque.Inclinacion, fichasB[0], 18f));
        for (int i = 1; i < Fichas2; i++)
            control.toques.Add(Toque("La ficha " + (Fichas + i) + " golpea la ficha " + (Fichas + i + 1),
                                     MachineController.TipoToque.Inclinacion, fichasB[i], 18f));

        // Toque 12: la última ficha empuja la ficha final al plato
        control.toques.Add(Toque("La ficha " + (Fichas + Fichas2) + " empuja la ficha final",
                                 MachineController.TipoToque.Inclinacion, fichaFinal.GetComponent<Rigidbody>(), 18f));

        var sensor = zona.AddComponent<GoalSensor>();
        sensor.control = control;

        // ---------------- 9. Ray tracing ----------------
        var rt = camara.gameObject.GetComponent<RayTracingView>();
        if (rt == null) rt = camara.gameObject.AddComponent<RayTracingView>();
        rt.Configurar(root.transform);

        control.CaptureInitialState();
        return root;
    }

    static MachineController.Toque Toque(string descripcion,
                                         MachineController.TipoToque tipo,
                                         Rigidbody cuerpo, float umbral)
    {
        return new MachineController.Toque
        {
            descripcion = descripcion,
            tipo = tipo,
            cuerpo = cuerpo,
            umbral = umbral,
            referencia = cuerpo != null ? cuerpo.position : Vector3.zero
        };
    }

    // =====================================================================
    //  MARCADOR DE 12 TOQUES (tablero sobre la máquina)
    // =====================================================================
    static List<Renderer> CrearMarcador(Transform parent)
    {
        var lista = new List<Renderer>();

        const float ancho = 8.6f;
        const float y = 3.62f;
        const float z = 1.65f;
        const float paso = 0.66f;
        float x0 = -2.35f;

        Prim(PrimitiveType.Cube, "Tablero de toques", parent,
             new Vector3(x0 + (ToquesTotal - 1) * paso * 0.5f, y, z + 0.10f),
             new Vector3(ancho, 0.82f, 0.10f),
             Quaternion.identity, Mat("Tablero", ColTablero, 0.2f));

        // patas del tablero
        for (int i = 0; i < 2; i++)
        {
            float px = i == 0 ? x0 - 0.55f : x0 + (ToquesTotal - 1) * paso + 0.55f;
            Prim(PrimitiveType.Cube, "Pata tablero", parent,
                 new Vector3(px, (y - 0.41f) * 0.5f, z + 0.10f),
                 new Vector3(0.08f, y - 0.41f, 0.08f),
                 Quaternion.identity, Mat("Baranda", ColBaranda, 0.4f));
        }

        var materialOff = Mat("MarcadorOff", ColToqueOff, 0.35f);

        for (int i = 0; i < ToquesTotal; i++)
        {
            float x = x0 + i * paso;
            var cubo = Prim(PrimitiveType.Cube, "Marcador " + (i + 1), parent,
                            new Vector3(x, y, z + 0.02f),
                            new Vector3(0.46f, 0.46f, 0.06f),
                            Quaternion.identity, materialOff);
            lista.Add(cubo.GetComponent<Renderer>());
        }

        return lista;
    }

    // =====================================================================
    //  ENTORNO
    // =====================================================================
    static Camera CrearEntorno()
    {
        var suelo = GameObject.CreatePrimitive(PrimitiveType.Plane);
        suelo.name = "Suelo";
        suelo.transform.position = Vector3.zero;
        suelo.transform.localScale = new Vector3(16f, 1f, 16f);
        suelo.GetComponent<MeshRenderer>().sharedMaterial = Mat("Suelo", ColSuelo, 0.15f);

        Prim(PrimitiveType.Cube, "Pared de fondo", null,
             new Vector3(0f, 10.5f, ParedZ),
             new Vector3(60f, 22f, 0.4f),
             Quaternion.identity, Mat("Pared", ColPared, 0.1f));

        Camera cam = Camera.main;
        if (cam == null)
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            cam = go.AddComponent<Camera>();
            go.AddComponent<AudioListener>();
        }
        cam.transform.position = new Vector3(-0.3f, 7.0f, -15.8f);
        cam.transform.rotation = Quaternion.identity;
        cam.transform.LookAt(new Vector3(0.9f, 1.9f, 0.2f));
        cam.fieldOfView = 44f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = C(191, 212, 232);
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 300f;

        Light sol = null;
#if UNITY_2022_2_OR_NEWER
        var luces = Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude);
#else
        var luces = Object.FindObjectsOfType<Light>();
#endif
        for (int i = 0; i < luces.Length; i++)
        {
            if (luces[i].type == LightType.Directional) { sol = luces[i]; break; }
        }
        if (sol == null)
        {
            var go = new GameObject("Luz principal");
            sol = go.AddComponent<Light>();
            sol.type = LightType.Directional;
        }
        sol.transform.rotation = Quaternion.Euler(46f, -34f, 0f);
        sol.color = new Color(1f, 0.96f, 0.90f);
        sol.intensity = 1.25f;
        sol.shadows = LightShadows.Soft;
        sol.shadowStrength = 0.75f;

        var relleno = new GameObject("Luz de relleno");
        var rl = relleno.AddComponent<Light>();
        rl.type = LightType.Directional;
        rl.transform.rotation = Quaternion.Euler(55f, 150f, 0f);
        rl.color = new Color(0.72f, 0.82f, 1f);
        rl.intensity = 0.45f;
        rl.shadows = LightShadows.None;

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.46f, 0.50f, 0.58f);
        RenderSettings.fog = false;

        return cam;
    }

    // =====================================================================
    //  PIEZAS
    // =====================================================================
    static void CrearRampa(Transform parent, string nombre, Vector3 desde, Vector3 hasta,
                           float ancho, float grosor, float altoBaranda)
    {
        Vector3 dir = hasta - desde;
        float largo = dir.magnitude;
        float angulo = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        Quaternion rot = Quaternion.Euler(0f, 0f, angulo);
        Vector3 normal = rot * Vector3.up;
        Vector3 medio = (desde + hasta) * 0.5f;

        Prim(PrimitiveType.Cube, nombre, parent,
             medio - normal * (grosor * 0.5f),
             new Vector3(largo, grosor, ancho), rot,
             Mat("Rampa", ColRampa, 0.35f));

        for (int lado = -1; lado <= 1; lado += 2)
        {
            Prim(PrimitiveType.Cube, "Baranda", parent,
                 medio + normal * (altoBaranda * 0.5f - 0.02f) + new Vector3(0f, 0f, lado * (ancho * 0.5f - 0.05f)),
                 new Vector3(largo, altoBaranda, 0.08f), rot,
                 Mat("Baranda", ColBaranda, 0.4f));
        }
    }

    static void Pilar(Transform parent, string nombre, float x, float alto, float ancho, float fondo)
    {
        Prim(PrimitiveType.Cube, nombre, parent,
             new Vector3(x, alto * 0.5f, 0f),
             new Vector3(ancho, alto, fondo),
             Quaternion.identity, Mat("Soporte", ColSoporte, 0.35f));
    }

    static float AlturaRampa(Vector3 desde, Vector3 hasta, float x)
    {
        float t = (x - desde.x) / (hasta.x - desde.x);
        return Mathf.Lerp(desde.y, hasta.y, t);
    }

    static GameObject Bola(Transform parent, string nombre, Vector3 pos, float radio,
                           Material mat, float masa, bool kinematico)
    {
        var go = Prim(PrimitiveType.Sphere, nombre, parent, pos,
                      Vector3.one * (radio * 2f), Quaternion.identity, mat);
        Cuerpo(go, masa, kinematico);
        return go;
    }

    static Rigidbody Cuerpo(GameObject go, float masa, bool kinematico = false)
    {
        var rb = go.AddComponent<Rigidbody>();
        rb.mass = masa;
        rb.isKinematic = kinematico;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        if (!kinematico) rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
#if UNITY_6000_0_OR_NEWER
        rb.linearDamping = 0f;
        rb.angularDamping = 0.05f;
#else
        rb.drag = 0f;
        rb.angularDrag = 0.05f;
#endif
        return rb;
    }

    // =====================================================================
    //  UTILIDADES
    // =====================================================================
    static GameObject Prim(PrimitiveType tipo, string nombre, Transform parent,
                           Vector3 pos, Vector3 escala, Quaternion rot, Material mat,
                           bool conservarCollider = true)
    {
        var go = GameObject.CreatePrimitive(tipo);
        go.name = nombre;

        var t = go.transform;
        if (parent != null) t.SetParent(parent, false);
        t.localPosition = pos;
        t.localRotation = rot;
        t.localScale = escala;

        var mr = go.GetComponent<MeshRenderer>();
        if (mr != null) mr.sharedMaterial = mat;

        if (!conservarCollider)
        {
            var col = go.GetComponent<Collider>();
            if (col != null) Destruir(col);
        }
        return go;
    }

    static void Destruir(Object obj)
    {
        if (obj == null) return;
        if (Application.isPlaying) Object.Destroy(obj);
        else Object.DestroyImmediate(obj);
    }

    /// <summary>Crea (o reutiliza) un material. En el editor lo guarda en Assets/Materials.</summary>
    public static Material Mat(string id, Color color, float brillo = 0.45f)
    {
        Material m;
        if (cache.TryGetValue(id, out m) && m != null) return m;

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            string ruta = "Assets/Materials/RG_" + id + ".mat";
            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (asset != null) { cache[id] = asset; return asset; }

            if (!UnityEditor.AssetDatabase.IsValidFolder("Assets/Materials"))
                UnityEditor.AssetDatabase.CreateFolder("Assets", "Materials");

            asset = CrearMaterial("RG_" + id, color, brillo);
            UnityEditor.AssetDatabase.CreateAsset(asset, ruta);
            cache[id] = asset;
            return asset;
        }
#endif
        m = CrearMaterial("RG_" + id, color, brillo);
        cache[id] = m;
        return m;
    }

    static Material CrearMaterial(string nombre, Color color, float brillo)
    {
        var shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Legacy Shaders/Diffuse");
        var m = new Material(shader);
        m.name = nombre;
        if (m.HasProperty("_Color")) m.SetColor("_Color", color);
        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", brillo);
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0.03f);
        return m;
    }

    public static void Emisivo(Material m, Color color, float intensidad)
    {
        if (m == null) return;
        m.EnableKeyword("_EMISSION");
        m.SetColor("_EmissionColor", color * intensidad);
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
    }
}
