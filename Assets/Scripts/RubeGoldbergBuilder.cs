using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Construye la máquina de Rube Goldberg usando primitivas de Unity.
/// Todo se genera por código: no hace falta arrastrar prefabs ni configurar nada a mano.
/// </summary>
public static class RubeGoldbergBuilder
{
    public const string RootName = "Maquina Rube Goldberg";

    // ------------------- Medidas principales (edítalas si quieres) -------------------
    const float MesaAlto      = 1.00f;   // altura de la mesa
    const float BolaR         = 0.25f;   // radio de la bola 1
    const float BolaBR        = 0.30f;   // radio de la bola 2

    const int   Fichas        = 6;       // cantidad de fichas de dominó
    const float FichaX0       = -1.00f;  // posición X de la primera ficha
    const float FichaPaso     = 0.60f;   // separación entre fichas
    const float FichaAlto     = 1.10f;
    const float FichaEspesor  = 0.16f;
    const float FichaAncho    = 0.62f;

    const float ParedZ        = 10.0f;   // pared del fondo

    // ------------------- Paleta -------------------
    static Color C(int r, int g, int b) { return new Color(r / 255f, g / 255f, b / 255f); }

    static readonly Color ColSuelo     = C(233, 239, 245);
    static readonly Color ColPared     = C(203, 217, 231);
    static readonly Color ColRampa     = C(242, 138, 48);
    static readonly Color ColBaranda   = C(70, 78, 88);
    static readonly Color ColMadera    = C(152, 108, 66);
    static readonly Color ColSoporte   = C(146, 158, 170);
    static readonly Color ColBolaA     = C(214, 40, 40);
    static readonly Color ColBolaB     = C(29, 78, 216);
    static readonly Color ColRepisa    = C(106, 76, 147);
    static readonly Color ColPlaca     = C(45, 55, 66);
    static readonly Color ColBorde     = C(245, 247, 250);
    static readonly Color ColPoste     = C(70, 80, 90);
    static readonly Color ColBandera   = C(255, 195, 0);

    static readonly Color[] ColFichas =
    {
        C(230, 57, 70), C(244, 114, 43), C(242, 193, 78),
        C(67, 170, 139), C(39, 125, 161), C(106, 76, 147)
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
        CrearEntorno();

        // ---------------- 1. Rampa con bolas que ruedan ----------------
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

        // ---------------- 2. Mesa donde corre el dominó ----------------
        var mesa = Prim(PrimitiveType.Cube, "Mesa", tr,
                        new Vector3(-0.10f, MesaAlto * 0.5f, 0f),
                        new Vector3(5.00f, MesaAlto, 1.90f),
                        Quaternion.identity, Mat("Madera", ColMadera, 0.25f));

        // ---------------- 3. Fichas de dominó en cadena ----------------
        for (int i = 0; i < Fichas; i++)
        {
            float x = FichaX0 + i * FichaPaso;
            var ficha = Prim(PrimitiveType.Cube, "Ficha " + (i + 1), tr,
                             new Vector3(x, MesaAlto + FichaAlto * 0.5f, 0f),
                             new Vector3(FichaEspesor, FichaAlto, FichaAncho),
                             Quaternion.identity, Mat("Ficha" + i, ColFichas[i % ColFichas.Length], 0.3f));
            // la primera ficha pesa un poco más para frenar la bola
            Cuerpo(ficha, i == 0 ? 0.9f : 0.45f);
        }

        // ---------------- 4. Repisa con la bola 2 ----------------
        Prim(PrimitiveType.Cube, "Repisa", tr,
             new Vector3(2.85f, MesaAlto - 0.09f, 0f),
             new Vector3(1.00f, 0.18f, 1.25f),
             Quaternion.identity, Mat("Repisa", ColRepisa, 0.4f));

        Pilar(tr, "Soporte repisa", 2.62f, MesaAlto - 0.18f, 0.45f, 1.05f);

        var bolaB = Bola(tr, "Bola 2", new Vector3(2.90f, MesaAlto + BolaBR + 0.02f, 0f),
                         BolaBR, Mat("BolaB", ColBolaB, 0.75f), 0.9f, false);

        // ---------------- 5. Meta: plato, bandera y lámpara ----------------
        Prim(PrimitiveType.Cube, "Plato de meta", tr,
             new Vector3(4.35f, 0.07f, 0f),
             new Vector3(2.70f, 0.14f, 1.90f),
             Quaternion.identity, Mat("Placa", ColPlaca, 0.5f));

        // bordes (el lado que mira a la máquina queda abierto)
        Prim(PrimitiveType.Cube, "Borde lejos", tr, new Vector3(5.63f, 0.34f, 0f),
             new Vector3(0.14f, 0.40f, 1.90f), Quaternion.identity, Mat("Borde", ColBorde, 0.6f));
        Prim(PrimitiveType.Cube, "Borde A", tr, new Vector3(4.35f, 0.34f, 0.88f),
             new Vector3(2.70f, 0.40f, 0.14f), Quaternion.identity, Mat("Borde", ColBorde, 0.6f));
        Prim(PrimitiveType.Cube, "Borde B", tr, new Vector3(4.35f, 0.34f, -0.88f),
             new Vector3(2.70f, 0.40f, 0.14f), Quaternion.identity, Mat("Borde", ColBorde, 0.6f));

        // poste + bandera + lámpara
        var poste = Prim(PrimitiveType.Cylinder, "Poste", tr,
                         new Vector3(5.25f, 1.52f, 0.68f),
                         new Vector3(0.12f, 1.38f, 0.12f),
                         Quaternion.identity, Mat("Poste", ColPoste, 0.6f));

        var bandera = Prim(PrimitiveType.Cube, "Bandera", tr,
                           new Vector3(5.61f, 0.55f, 0.68f),
                           new Vector3(0.70f, 0.42f, 0.04f),
                           Quaternion.identity, Mat("Bandera", ColBandera, 0.3f), false);

        var lampara = Prim(PrimitiveType.Sphere, "Lampara", tr,
                           new Vector3(5.25f, 2.97f, 0.68f),
                           Vector3.one * 0.30f,
                           Quaternion.identity, Mat("LamparaOff", C(120, 45, 45), 0.8f), false);

        var lamparaOn = Mat("LamparaOn", C(45, 230, 120), 0.9f);
        Emisivo(lamparaOn, C(45, 230, 120), 2.2f);
        var lamparaOff = Mat("LamparaOff", C(120, 45, 45), 0.8f);

        // zona invisible que detecta la bola en la meta
        var zona = new GameObject("Zona de meta");
        zona.transform.SetParent(tr, false);
        zona.transform.localPosition = new Vector3(4.35f, 0.75f, 0f);
        var zonaCol = zona.AddComponent<BoxCollider>();
        zonaCol.size = new Vector3(2.35f, 1.20f, 1.60f);
        zonaCol.isTrigger = true;

        var confeti = new GameObject("Confeti");
        confeti.transform.SetParent(tr, false);

        // ---------------- 6. Controlador del juego ----------------
        var control = root.AddComponent<MachineController>();
        control.bola1 = bolaA.transform;
        control.bola2 = bolaB.transform;
        control.bandera = bandera.transform;
        control.banderaAbajo = 0.55f;
        control.banderaArriba = 2.45f;
        control.lampara = lampara.GetComponent<Renderer>();
        control.lamparaOn = lamparaOn;
        control.lamparaOff = lamparaOff;
        control.confeti = confeti.transform;

        var sensor = zona.AddComponent<GoalSensor>();
        sensor.control = control;

        control.CaptureInitialState();
        return root;
    }

    // =====================================================================
    //  ENTORNO
    // =====================================================================
    static void CrearEntorno()
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
        cam.transform.position = new Vector3(-1.2f, 7.2f, -14.2f);
        cam.transform.rotation = Quaternion.identity;
        cam.transform.LookAt(new Vector3(-1.2f, 1.9f, 0.2f));
        cam.fieldOfView = 42f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = C(191, 212, 232);
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 300f;

        Light sol = null;
        var luces = Object.FindObjectsOfType<Light>();
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
        RenderSettings.ambientLight = new Color(0.62f, 0.66f, 0.72f);
        RenderSettings.fog = false;
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
