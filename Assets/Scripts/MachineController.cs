using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controla la máquina: suelta la primera bola, mide el tiempo, muestra el HUD,
/// reinicia la escena con R y celebra cuando la bola 2 cae en la meta.
/// </summary>
public class MachineController : MonoBehaviour
{
    [System.Serializable]
    public class EstadoCuerpo
    {
        public Rigidbody cuerpo;
        public Vector3 posicion;
        public Quaternion rotacion;
        public bool kinematico;
    }

    [Header("Referencias (las asigna RubeGoldbergBuilder)")]
    public Transform bola1;
    public Transform bola2;
    public Transform bandera;
    public float banderaAbajo = 0.55f;
    public float banderaArriba = 2.45f;
    public Renderer lampara;
    public Material lamparaOn;
    public Material lamparaOff;
    public Transform confeti;

    [Header("Ajustes")]
    public float retardoInicio = 1.6f;

    public List<EstadoCuerpo> estados = new List<EstadoCuerpo>();

    bool soltada;
    bool terminada;
    bool camaraLenta;
    int etapa;             // 0 = espera, 1 = rampa, 2 = dominó, 3 = caída, 4 = meta

    /// <summary>True cuando la bola 2 ya llegó a la meta.</summary>
    public bool Terminada { get { return terminada; } }

    /// <summary>Etapa actual de la cadena (0 a 4).</summary>
    public int Etapa { get { return etapa; } }

    float tiempoInicio;
    float tiempoFinal;
    float inicioEspera;
    float objetivoBandera;

    // ---------------- Estilos del HUD ----------------
    GUIStyle estiloTitulo;
    GUIStyle estiloTexto;
    GUIStyle estiloEtapa;
    GUIStyle estiloGrande;
    float escalaEstilos = -1f;

    void Start()
    {
        if (estados == null || estados.Count == 0) CaptureInitialState();
        objetivoBandera = banderaAbajo;
        if (bandera != null)
        {
            var p = bandera.localPosition;
            p.y = banderaAbajo;
            bandera.localPosition = p;
        }
        tiempoInicio = Time.time;
        inicioEspera = Time.unscaledTime;
    }

    void Update()
    {
        LeerEntrada();

        if (!soltada && Time.unscaledTime - inicioEspera >= retardoInicio) SoltarBola();

        if (soltada && !terminada)
        {
            if (etapa < 1) etapa = 1;
            if (bola1 != null && bola1.position.x > -2.2f && etapa < 2) etapa = 2;
            if (bola2 != null && bola2.position.y < 0.85f && etapa < 3) etapa = 3;
        }

        AnimarBandera();
    }

    void LeerEntrada()
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.R)) Reiniciar();
        if (Input.GetKeyDown(KeyCode.F))
        {
            camaraLenta = !camaraLenta;
            Time.timeScale = camaraLenta ? 0.25f : 1f;
        }
#endif
    }

    // ---------------- Flujo ----------------
    public void SoltarBola()
    {
        soltada = true;
        if (etapa < 1) etapa = 1;
        tiempoInicio = Time.time;
        if (bola1 != null)
        {
            var rb = bola1.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.WakeUp();
            }
        }
    }

    public void CaptureInitialState()
    {
        estados.Clear();
        var cuerpos = GetComponentsInChildren<Rigidbody>(true);
        for (int i = 0; i < cuerpos.Length; i++)
        {
            estados.Add(new EstadoCuerpo
            {
                cuerpo = cuerpos[i],
                posicion = cuerpos[i].position,
                rotacion = cuerpos[i].rotation,
                kinematico = cuerpos[i].isKinematic
            });
        }
    }

    public void Reiniciar()
    {
        Time.timeScale = 1f;
        camaraLenta = false;

        for (int i = 0; i < estados.Count; i++)
        {
            var e = estados[i];
            if (e.cuerpo == null) continue;

            e.cuerpo.isKinematic = true;
            e.cuerpo.position = e.posicion;
            e.cuerpo.rotation = e.rotacion;
            e.cuerpo.isKinematic = e.kinematico;
            Detener(e.cuerpo);
            if (!e.kinematico) e.cuerpo.WakeUp();
        }

        if (confeti != null)
        {
            for (int i = confeti.childCount - 1; i >= 0; i--)
                Destroy(confeti.GetChild(i).gameObject);
        }

        if (lampara != null && lamparaOff != null) lampara.sharedMaterial = lamparaOff;
        objetivoBandera = banderaAbajo;

        soltada = false;
        terminada = false;
        etapa = 0;
        tiempoInicio = Time.time;
        inicioEspera = Time.unscaledTime;
    }

    public void CompletarMeta()
    {
        if (terminada) return;
        terminada = true;
        etapa = 4;
        tiempoFinal = Time.time;

        if (lampara != null && lamparaOn != null) lampara.sharedMaterial = lamparaOn;
        objetivoBandera = banderaArriba;
        LanzarConfeti();
    }

    static void Detener(Rigidbody rb)
    {
#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = Vector3.zero;
#else
        rb.velocity = Vector3.zero;
#endif
        rb.angularVelocity = Vector3.zero;
    }

    void AnimarBandera()
    {
        if (bandera == null) return;
        var p = bandera.localPosition;
        p.y = Mathf.Lerp(p.y, objetivoBandera, 1f - Mathf.Exp(-5f * Time.deltaTime));
        bandera.localPosition = p;
    }

    void LanzarConfeti()
    {
        if (confeti == null) return;
        var colores = new[] { "BolaA", "BolaB", "Bandera", "Ficha0", "Ficha3", "Ficha4" };
        for (int i = 0; i < 28; i++)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Confeti";
            go.transform.SetParent(confeti, false);
            go.transform.position = new Vector3(4.3f, 3.0f, 0f) + Random.insideUnitSphere * 0.5f;
            go.transform.localScale = Vector3.one * Random.Range(0.08f, 0.16f);
            go.transform.rotation = Random.rotation;

            var mat = RubeGoldbergBuilder.Mat(colores[i % colores.Length], Color.white, 0.4f);
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;

            var rb = go.AddComponent<Rigidbody>();
            rb.mass = 0.05f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = new Vector3(Random.Range(-2.5f, 2.5f), Random.Range(3f, 7f), Random.Range(-2.5f, 2.5f));
#else
            rb.velocity = new Vector3(Random.Range(-2.5f, 2.5f), Random.Range(3f, 7f), Random.Range(-2.5f, 2.5f));
#endif
            rb.angularVelocity = new Vector3(Random.Range(-8f, 8f), Random.Range(-8f, 8f), Random.Range(-8f, 8f));
            Destroy(go, 30f);
        }
    }

    // ---------------- HUD ----------------
    string TextoEtapa()
    {
        switch (etapa)
        {
            case 0: return "Preparando la máquina...";
            case 1: return "Etapa 1/3 · La bola rueda por la rampa";
            case 2: return "Etapa 2/3 · Dominó en cadena";
            case 3: return "Etapa 3/3 · La bola cae en la meta";
            case 4: return "¡Máquina completada!";
            default: return "";
        }
    }

    void PrepararEstilos(float s)
    {
        if (estiloTitulo != null && Mathf.Approximately(escalaEstilos, s)) return;
        escalaEstilos = s;

        estiloTitulo = new GUIStyle(GUI.skin.label);
        estiloTitulo.fontSize = Mathf.RoundToInt(26f * s);
        estiloTitulo.fontStyle = FontStyle.Bold;
        estiloTitulo.normal.textColor = Color.white;

        estiloTexto = new GUIStyle(GUI.skin.label);
        estiloTexto.fontSize = Mathf.RoundToInt(17f * s);
        estiloTexto.normal.textColor = new Color(0.80f, 0.87f, 0.95f);

        estiloEtapa = new GUIStyle(GUI.skin.label);
        estiloEtapa.fontSize = Mathf.RoundToInt(19f * s);
        estiloEtapa.fontStyle = FontStyle.Bold;
        estiloEtapa.normal.textColor = new Color(0.55f, 0.85f, 1f);

        estiloGrande = new GUIStyle(GUI.skin.label);
        estiloGrande.fontSize = Mathf.RoundToInt(40f * s);
        estiloGrande.fontStyle = FontStyle.Bold;
        estiloGrande.alignment = TextAnchor.MiddleCenter;
        estiloGrande.normal.textColor = Color.white;
    }

    static void Panel(Rect r, Color color)
    {
        var antes = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = antes;
    }

    void OnGUI()
    {
        float s = Mathf.Clamp(Screen.height / 900f, 0.65f, 2.4f);
        PrepararEstilos(s);

        // barra superior
        Panel(new Rect(0f, 0f, Screen.width, 6f * s), new Color(0.83f, 0.14f, 0.19f));

        // tarjeta de información
        var tarjeta = new Rect(24f * s, 24f * s, 560f * s, 154f * s);
        Panel(tarjeta, new Color(0.06f, 0.09f, 0.13f, 0.80f));

        GUI.Label(new Rect(tarjeta.x + 18f * s, tarjeta.y + 10f * s, tarjeta.width - 36f * s, 34f * s),
                  "MÁQUINA DE RUBE GOLDBERG", estiloTitulo);
        GUI.Label(new Rect(tarjeta.x + 18f * s, tarjeta.y + 46f * s, tarjeta.width - 36f * s, 24f * s),
                  "Taller Unity · motor de físicas", estiloTexto);
        GUI.Label(new Rect(tarjeta.x + 18f * s, tarjeta.y + 74f * s, tarjeta.width - 36f * s, 26f * s),
                  TextoEtapa(), estiloEtapa);

        float tiempo = (terminada ? tiempoFinal : Time.time) - tiempoInicio;
        if (!soltada) tiempo = 0f;
        GUI.Label(new Rect(tarjeta.x + 18f * s, tarjeta.y + 106f * s, tarjeta.width - 36f * s, 24f * s),
                  "Tiempo: " + tiempo.ToString("0.00") + " s", estiloTexto);

        // aviso de controles
#if ENABLE_LEGACY_INPUT_MANAGER
        GUI.Label(new Rect(24f * s, Screen.height - 40f * s, 600f * s, 24f * s),
                  "[R] Reiniciar   ·   [F] Cámara lenta", estiloTexto);
#endif

        // cartel de meta
        if (terminada)
        {
            float ancho = 760f * s;
            float alto = 150f * s;
            var r = new Rect((Screen.width - ancho) * 0.5f, Screen.height * 0.12f, ancho, alto);
            Panel(r, new Color(0.05f, 0.55f, 0.25f, 0.94f));
            GUI.Label(r, "¡MÁQUINA COMPLETADA!\nTiempo total: " + (tiempoFinal - tiempoInicio).ToString("0.00") + " s",
                      estiloGrande);
        }
    }
}
