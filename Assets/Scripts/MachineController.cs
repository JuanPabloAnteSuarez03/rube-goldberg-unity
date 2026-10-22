using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controla la máquina: suelta la primera bola, mide el tiempo, muestra el HUD,
/// reinicia con R, cambia a cámara lenta con F y activa el ray tracing con T.
/// Lleva la cuenta de los 12 toques de la cadena.
/// </summary>
public class MachineController : MonoBehaviour
{
    public enum TipoToque { Inclinacion, Desplazamiento, Caida, Meta }

    [System.Serializable]
    public class Toque
    {
        public string descripcion;
        public TipoToque tipo;
        public Rigidbody cuerpo;
        public float umbral;
        public Vector3 referencia;
        public bool cumplido;
    }

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
    public Rigidbody objetoFinal;
    public Transform bandera;
    public float banderaAbajo = 0.65f;
    public float banderaArriba = 2.45f;
    public Renderer lampara;
    public Material lamparaOn;
    public Material lamparaOff;
    public Transform confeti;

    [Header("Marcador de toques")]
    public Renderer[] indicadores;
    public Material indicadorOn;
    public Material indicadorOff;

    [Header("Ajustes")]
    public float retardoInicio = 1.6f;

    public List<Toque> toques = new List<Toque>();
    public List<EstadoCuerpo> estados = new List<EstadoCuerpo>();

    bool soltada;
    bool terminada;
    bool camaraLenta;
    int ultimoToque = -1;

    /// <summary>True cuando la bola final ya llegó a la meta.</summary>
    public bool Terminada { get { return terminada; } }

    /// <summary>Número de toques completados (0 a 12).</summary>
    public int ToquesCumplidos
    {
        get
        {
            int n = 0;
            for (int i = 0; i < toques.Count; i++) if (toques[i].cumplido) n++;
            return n;
        }
    }

    /// <summary>Etapa actual de la cadena (0 a 12).</summary>
    public int Etapa { get { return ToquesCumplidos; } }

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
        AplicarMarcadores();
    }

    void Update()
    {
        LeerEntrada();

        if (!soltada && Time.unscaledTime - inicioEspera >= retardoInicio) SoltarBola();

        if (soltada && !terminada) EvaluarToques();

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

        for (int i = 0; i < toques.Count; i++) toques[i].cumplido = false;
        ultimoToque = -1;
        AplicarMarcadores();

        soltada = false;
        terminada = false;
        tiempoInicio = Time.time;
        inicioEspera = Time.unscaledTime;
    }

    // ---------------- Detección de los 12 toques ----------------
    /// <summary>Comprueba las condiciones de todos los toques. Público para poder validar en el editor.</summary>
    public void EvaluarToques()
    {
        for (int i = 0; i < toques.Count; i++)
        {
            var t = toques[i];
            if (t == null || t.cumplido || t.cuerpo == null) continue;

            bool ok = false;
            switch (t.tipo)
            {
                case TipoToque.Inclinacion:
                    // La ficha se ha inclinado lo suficiente como para considerarla caída.
                    ok = Vector3.Angle(t.cuerpo.transform.up, Vector3.up) > t.umbral;
                    break;
                case TipoToque.Desplazamiento:
                    Vector3 d = t.cuerpo.position - t.referencia;
                    d.y = 0f;
                    ok = d.magnitude > t.umbral;
                    break;
                case TipoToque.Caida:
                    ok = t.cuerpo.position.y < t.umbral;
                    break;
            }

            if (ok) MarcarToque(i);
        }
    }

    void MarcarToque(int indice)
    {
        if (indice < 0 || indice >= toques.Count) return;
        if (toques[indice].cumplido) return;

        toques[indice].cumplido = true;
        ultimoToque = indice;
        if (indicadores != null && indice < indicadores.Length)
            AplicarMarcador(indice);
    }

    void AplicarMarcadores()
    {
        if (indicadores == null) return;
        for (int i = 0; i < indicadores.Length; i++) AplicarMarcador(i);
    }

    void AplicarMarcador(int i)
    {
        if (indicadores == null || i < 0 || i >= indicadores.Length) return;
        var r = indicadores[i];
        if (r == null) return;
        bool hecho = i < toques.Count && toques[i].cumplido;
        var mat = hecho ? indicadorOn : indicadorOff;
        if (mat != null) r.sharedMaterial = mat;
    }

    public void CompletarMeta()
    {
        if (terminada) return;
        terminada = true;
        tiempoFinal = Time.time;

        // La meta cierra la cadena: todos los toques quedan completados.
        for (int i = 0; i < toques.Count; i++)
            if (!toques[i].cumplido) MarcarToque(i);

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
        var colores = new[]
        {
            new Color(0.84f, 0.16f, 0.16f), new Color(0.11f, 0.31f, 0.85f),
            new Color(1.00f, 0.84f, 0.25f), new Color(1.00f, 0.76f, 0.00f),
            new Color(0.90f, 0.22f, 0.27f), new Color(0.26f, 0.67f, 0.55f),
            new Color(0.15f, 0.49f, 0.63f)
        };
        for (int i = 0; i < 28; i++)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Confeti";
            go.transform.SetParent(confeti, false);
            go.transform.position = new Vector3(6.00f, 3.0f, 0f) + Random.insideUnitSphere * 0.5f;
            go.transform.localScale = Vector3.one * Random.Range(0.08f, 0.16f);
            go.transform.rotation = Random.rotation;

            var mat = RubeGoldbergBuilder.Mat("Confeti" + (i % colores.Length),
                                              colores[i % colores.Length], 0.4f);
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
        if (terminada) return "¡Máquina completada!";
        if (!soltada) return "Preparando la máquina...";
        if (ultimoToque >= 0 && ultimoToque < toques.Count)
            return "Toque " + (ultimoToque + 1) + "/" + RubeGoldbergBuilder.ToquesTotal +
                   " · " + toques[ultimoToque].descripcion;
        return "Soltando la bola 1...";
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
        estiloEtapa.fontSize = Mathf.RoundToInt(18f * s);
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
        var tarjeta = new Rect(24f * s, 24f * s, 600f * s, 218f * s);
        Panel(tarjeta, new Color(0.06f, 0.09f, 0.13f, 0.82f));

        GUI.Label(new Rect(tarjeta.x + 18f * s, tarjeta.y + 10f * s, tarjeta.width - 36f * s, 34f * s),
                  "MÁQUINA DE RUBE GOLDBERG", estiloTitulo);
        GUI.Label(new Rect(tarjeta.x + 18f * s, tarjeta.y + 46f * s, tarjeta.width - 36f * s, 24f * s),
                  "Taller Unity · 12 toques · motor de físicas", estiloTexto);
        GUI.Label(new Rect(tarjeta.x + 18f * s, tarjeta.y + 74f * s, tarjeta.width - 36f * s, 28f * s),
                  TextoEtapa(), estiloEtapa);

        float tiempo = (terminada ? tiempoFinal : Time.time) - tiempoInicio;
        if (!soltada) tiempo = 0f;
        GUI.Label(new Rect(tarjeta.x + 18f * s, tarjeta.y + 106f * s, tarjeta.width - 36f * s, 24f * s),
                  "Tiempo: " + tiempo.ToString("0.00") + " s   ·   Toques: " +
                  ToquesCumplidos + "/" + RubeGoldbergBuilder.ToquesTotal, estiloTexto);

        // barra de 12 toques
        float anchoPunto = 38f * s;
        float altoPunto = 12f * s;
        for (int i = 0; i < RubeGoldbergBuilder.ToquesTotal; i++)
        {
            bool hecho = i < toques.Count && toques[i].cumplido;
            var color = hecho ? new Color(0.18f, 0.90f, 0.47f) : new Color(0.24f, 0.29f, 0.36f);
            Panel(new Rect(tarjeta.x + 18f * s + i * anchoPunto, tarjeta.y + 148f * s,
                           anchoPunto - 6f * s, altoPunto), color);
        }

        // estado del ray tracing
        string estadoRT = RayTracingView.ActivoGlobal ? "ON" : "OFF";
        var colorRT = RayTracingView.ActivoGlobal
            ? new Color(0.40f, 1f, 0.70f)
            : new Color(0.62f, 0.68f, 0.76f);
        var estiloRT = new GUIStyle(estiloTexto);
        estiloRT.normal.textColor = colorRT;
        GUI.Label(new Rect(tarjeta.x + 18f * s, tarjeta.y + 172f * s, tarjeta.width - 36f * s, 26f * s),
                  "Ray tracing: " + estadoRT + "   ·   [T] activar / desactivar", estiloRT);

        // aviso de controles
#if ENABLE_LEGACY_INPUT_MANAGER
        GUI.Label(new Rect(24f * s, Screen.height - 40f * s, 760f * s, 24f * s),
                  "[R] Reiniciar   ·   [F] Cámara lenta   ·   [T] Ray tracing", estiloTexto);
#endif

        // cartel de meta
        if (terminada)
        {
            float ancho = 780f * s;
            float alto = 150f * s;
            var r = new Rect((Screen.width - ancho) * 0.5f, Screen.height * 0.12f, ancho, alto);
            Panel(r, new Color(0.05f, 0.55f, 0.25f, 0.94f));
            GUI.Label(r, "¡MÁQUINA COMPLETADA!\n12 toques · Tiempo total: " +
                         (tiempoFinal - tiempoInicio).ToString("0.00") + " s",
                      estiloGrande);
        }
    }
}
