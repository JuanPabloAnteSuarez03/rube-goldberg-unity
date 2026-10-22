using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// Vista de ray tracing por GPU para la máquina.
/// Traza rayos primarios, rayos de sombra, una reflexión y oclusión ambiental
/// sobre las primitivas de la escena (esferas, cajas y el suelo).
///
/// No necesita HDRP ni DXR: funciona en cualquier GPU con compute shaders,
/// así que también corre en Linux/macOS.
///
/// Se activa y desactiva con la tecla T.
/// </summary>
[RequireComponent(typeof(Camera))]
public class RayTracingView : MonoBehaviour
{
    [StructLayout(LayoutKind.Sequential)]
    struct EsferaGpu
    {
        public Vector4 centroRadio;
        public Vector4 albedoBrillo;
        public Vector4 emision;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct CajaGpu
    {
        public Vector4 centro;
        public Vector4 medios;
        public Vector4 rotacion;
        public Vector4 albedoBrillo;
        public Vector4 emision;
    }

    const int MaxObjetos = 256;

    public Transform maquina;
    public bool activo = true;
    [Range(0.35f, 1f)] public float escalaResolucion = 0.75f;

    /// <summary>Estado global para que el HUD pueda mostrarlo.</summary>
    public static bool ActivoGlobal { get; private set; }

    ComputeShader trazador;
    Camera camara;
    ComputeBuffer bufferEsferas;
    ComputeBuffer bufferCajas;
    RenderTexture destino;
    int kernel = -1;
    bool avisado;

    readonly List<EsferaGpu> listaEsferas = new List<EsferaGpu>(128);
    readonly List<CajaGpu> listaCajas = new List<CajaGpu>(128);

    public void Configurar(Transform raiz)
    {
        maquina = raiz;
        camara = GetComponent<Camera>();
    }

    void Awake()
    {
        camara = GetComponent<Camera>();
    }

    void OnEnable()
    {
        camara = GetComponent<Camera>();
        if (trazador == null) trazador = Resources.Load<ComputeShader>("RayTracing");
        ActivoGlobal = activo;
    }

    void OnDisable()
    {
        ActivoGlobal = false;
        Liberar();
    }

    void Update()
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.T)) activo = !activo;
#endif
        ActivoGlobal = activo;
    }

    void OnRenderImage(RenderTexture origen, RenderTexture final)
    {
        if (!activo || !Preparar())
        {
            Graphics.Blit(origen, final);
            return;
        }

        Recolectar();
        Dibujar();
        Graphics.Blit(destino, final);
    }

    // ------------------------------------------------------------------
    bool Preparar(int anchoForzado = 0, int altoForzado = 0)
    {
        if (!SystemInfo.supportsComputeShaders)
        {
            Avisar("Tu GPU no soporta compute shaders; se usa el render normal.");
            return false;
        }

        if (trazador == null) trazador = Resources.Load<ComputeShader>("RayTracing");
        if (trazador == null)
        {
            Avisar("No se encontró Assets/Resources/RayTracing.compute; se usa el render normal.");
            return false;
        }

        if (kernel < 0)
        {
            kernel = trazador.FindKernel("CSMain");
            if (kernel < 0)
            {
                Avisar("El shader de ray tracing no compiló; se usa el render normal.");
                return false;
            }
        }

        int ancho, alto;
        if (anchoForzado > 0 && altoForzado > 0)
        {
            ancho = anchoForzado;
            alto = altoForzado;
        }
        else
        {
            int anchoPantalla = Screen.width > 16 ? Screen.width : 1280;
            int altoPantalla = Screen.height > 16 ? Screen.height : 720;
            ancho = Mathf.Max(64, Mathf.RoundToInt(anchoPantalla * Mathf.Clamp(escalaResolucion, 0.35f, 1f)));
            alto  = Mathf.Max(64, Mathf.RoundToInt(altoPantalla * Mathf.Clamp(escalaResolucion, 0.35f, 1f)));
        }

        if (destino == null || destino.width != ancho || destino.height != alto)
        {
            if (destino != null)
            {
                destino.Release();
                if (Application.isPlaying) Destroy(destino); else DestroyImmediate(destino);
            }
            destino = new RenderTexture(ancho, alto, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            destino.name = "RT_RayTracing";
            destino.enableRandomWrite = true;
            destino.filterMode = FilterMode.Bilinear;
            destino.wrapMode = TextureWrapMode.Clamp;
            destino.Create();
        }

        if (bufferEsferas == null)
            bufferEsferas = new ComputeBuffer(MaxObjetos, Marshal.SizeOf(typeof(EsferaGpu)));
        if (bufferCajas == null)
            bufferCajas = new ComputeBuffer(MaxObjetos, Marshal.SizeOf(typeof(CajaGpu)));

        return true;
    }

    /// <summary>Renderiza un fotograma y lo guarda en PNG (para probar el trazador en el editor).</summary>
    public bool RenderizarPrueba(int ancho, int alto, string ruta)
    {
        camara = GetComponent<Camera>();
        if (camara == null) camara = Camera.main;
        if (camara == null) return false;

        if (!Preparar(ancho, alto)) return false;

        Recolectar();
        Dibujar();

        var anterior = RenderTexture.active;
        RenderTexture.active = destino;
        var tex = new Texture2D(destino.width, destino.height, TextureFormat.RGB24, false, false);
        tex.ReadPixels(new Rect(0, 0, destino.width, destino.height), 0, 0);
        tex.Apply();
        RenderTexture.active = anterior;

        System.IO.File.WriteAllBytes(ruta, tex.EncodeToPNG());
        if (Application.isPlaying) Destroy(tex); else DestroyImmediate(tex);
        Debug.Log("[RayTracing] Imagen de prueba guardada en " + ruta);
        return true;
    }

    // ------------------------------------------------------------------
    void Recolectar()
    {
        listaEsferas.Clear();
        listaCajas.Clear();

        if (maquina == null)
        {
            var encontrada = GameObject.Find(RubeGoldbergBuilder.RootName);
            if (encontrada != null) maquina = encontrada.transform;
        }

        Vector3 sueloAlbedo = new Vector3(0.85f, 0.88f, 0.92f);
        Vector3 suelEmision = Vector3.zero;

        // Se recoge TODO el entorno (máquina, pared, suelo...) para que el trazado
        // de rayos vea lo mismo que la cámara normal.
        var renderers = FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude);
        for (int i = 0; i < renderers.Length; i++)
        {
            var r = renderers[i];
            if (r == null || !r.enabled || !r.gameObject.activeInHierarchy) continue;

            var mf = r.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) continue;

            string nombreMalla = mf.sharedMesh.name;
            Transform t = r.transform;

            Color albedo = Color.gray;
            float brillo = 0.3f;
            Color emision = Color.black;

            var mat = r.sharedMaterial;
            if (mat != null)
            {
                if (mat.HasProperty("_Color")) albedo = mat.GetColor("_Color");
                if (mat.HasProperty("_Glossiness")) brillo = Mathf.Clamp01(mat.GetFloat("_Glossiness"));
                if (mat.IsKeywordEnabled("_EMISSION") && mat.HasProperty("_EmissionColor"))
                    emision = mat.GetColor("_EmissionColor");
            }

            Vector3 lineal = AGammaColor(albedo);
            Vector3 emiLineal = AGammaColor(emision);

            if (nombreMalla.StartsWith("Plane"))
            {
                sueloAlbedo = lineal;
                suelEmision = emiLineal;
                continue; // el suelo se traza como plano infinito
            }

            if (listaEsferas.Count + listaCajas.Count >= MaxObjetos) break;

            Vector3 escala = t.lossyScale;
            Vector3 pos = t.position;
            Quaternion rot = t.rotation;

            if (nombreMalla.StartsWith("Sphere") || nombreMalla.StartsWith("Capsule"))
            {
                float radio = 0.5f * Mathf.Max(Mathf.Abs(escala.x), Mathf.Max(Mathf.Abs(escala.y), Mathf.Abs(escala.z)));

                listaEsferas.Add(new EsferaGpu
                {
                    centroRadio = new Vector4(pos.x, pos.y, pos.z, radio),
                    albedoBrillo = new Vector4(lineal.x, lineal.y, lineal.z, brillo),
                    emision = new Vector4(emiLineal.x, emiLineal.y, emiLineal.z, 0f)
                });
            }
            else if (nombreMalla.StartsWith("Cylinder"))
            {
                // El cilindro de Unity mide 2 de alto y 1 de diámetro.
                Vector3 medios = new Vector3(
                    0.5f * Mathf.Abs(escala.x),
                    1.0f * Mathf.Abs(escala.y),
                    0.5f * Mathf.Abs(escala.z));

                AgregarCaja(pos, medios, rot, lineal, brillo, emiLineal);
            }
            else
            {
                Vector3 medios = new Vector3(
                    0.5f * Mathf.Abs(escala.x),
                    0.5f * Mathf.Abs(escala.y),
                    0.5f * Mathf.Abs(escala.z));

                AgregarCaja(pos, medios, rot, lineal, brillo, emiLineal);
            }
        }

        if (bufferEsferas != null) bufferEsferas.SetData(listaEsferas);
        if (bufferCajas != null) bufferCajas.SetData(listaCajas);

        trazador.SetInt("_NumEsferas", listaEsferas.Count);
        trazador.SetInt("_NumCajas", listaCajas.Count);

        // Cámara
        if (camara == null) camara = GetComponent<Camera>();
        trazador.SetVector("_CamaraPos", camara.transform.position);
        trazador.SetVector("_CamaraDerecha", camara.transform.right);
        trazador.SetVector("_CamaraArriba", camara.transform.up);
        trazador.SetVector("_CamaraAdelante", camara.transform.forward);
        trazador.SetFloat("_TanMedioFov", Mathf.Tan(camara.fieldOfView * 0.5f * Mathf.Deg2Rad));
        trazador.SetFloat("_Aspecto", (float)destino.width / Mathf.Max(1, destino.height));
        trazador.SetInt("_Ancho", destino.width);
        trazador.SetInt("_Alto", destino.height);

        // Luces
        Light sol = null;
        var luces = FindObjectsByType<Light>(FindObjectsInactive.Exclude);
        for (int i = 0; i < luces.Length; i++)
        {
            if (luces[i].type == LightType.Directional && luces[i].intensity > 0.01f)
            {
                if (sol == null || luces[i].intensity > sol.intensity) sol = luces[i];
            }
        }

        Vector3 direccionSol = sol != null ? -sol.transform.forward : new Vector3(0.5f, 0.8f, 0.4f);
        Vector3 colorSol = sol != null ? AGammaColor(sol.color) * (sol.intensity * 1.1f) : new Vector3(1f, 1f, 1f);

        Vector3 ambiente = AGammaColor(RenderSettings.ambientLight) * 0.65f;
        Color colorCielo = camara != null ? camara.backgroundColor : new Color(0.75f, 0.83f, 0.92f);
        Vector3 cielo = AGammaColor(colorCielo);
        Vector3 horizonte = Vector3.Lerp(cielo, Vector3.one, 0.45f);

        trazador.SetVector("_SolDir", direccionSol.normalized);
        trazador.SetVector("_SolColor", colorSol);
        trazador.SetVector("_Ambiente", ambiente);
        trazador.SetVector("_Cielo", cielo);
        trazador.SetVector("_Horizonte", horizonte);
        trazador.SetVector("_SueloAlbedo", sueloAlbedo);
        trazador.SetVector("_SueloEmision", suelEmision);
    }

    void AgregarCaja(Vector3 centro, Vector3 medios, Quaternion rot,
                     Vector3 albedo, float brillo, Vector3 emision)
    {
        if (listaCajas.Count >= MaxObjetos) return;

        listaCajas.Add(new CajaGpu
        {
            centro = new Vector4(centro.x, centro.y, centro.z, 0f),
            medios = new Vector4(medios.x, medios.y, medios.z, 0f),
            rotacion = new Vector4(rot.x, rot.y, rot.z, rot.w),
            albedoBrillo = new Vector4(albedo.x, albedo.y, albedo.z, brillo),
            emision = new Vector4(emision.x, emision.y, emision.z, 0f)
        });
    }

    // ------------------------------------------------------------------
    void Dibujar()
    {
        trazador.SetBuffer(kernel, "_Esferas", bufferEsferas);
        trazador.SetBuffer(kernel, "_Cajas", bufferCajas);
        trazador.SetTexture(kernel, "_Resultado", destino);

        int gruposX = Mathf.CeilToInt(destino.width / 8f);
        int gruposY = Mathf.CeilToInt(destino.height / 8f);
        trazador.Dispatch(kernel, gruposX, gruposY, 1);
    }

    static Vector3 AGammaColor(Color c)
    {
        return new Vector3(
            Mathf.GammaToLinearSpace(Mathf.Max(0f, c.r)),
            Mathf.GammaToLinearSpace(Mathf.Max(0f, c.g)),
            Mathf.GammaToLinearSpace(Mathf.Max(0f, c.b)));
    }

    static Vector3 AGamma(Vector3 c)
    {
        return new Vector3(
            Mathf.GammaToLinearSpace(Mathf.Max(0f, c.x)),
            Mathf.GammaToLinearSpace(Mathf.Max(0f, c.y)),
            Mathf.GammaToLinearSpace(Mathf.Max(0f, c.z)));
    }

    void Avisar(string mensaje)
    {
        if (avisado) return;
        avisado = true;
        activo = false;
        ActivoGlobal = false;
        Debug.LogWarning("[RayTracing] " + mensaje);
    }

    void Liberar()
    {
        if (bufferEsferas != null) { bufferEsferas.Release(); bufferEsferas = null; }
        if (bufferCajas != null) { bufferCajas.Release(); bufferCajas = null; }
        if (destino != null)
        {
            destino.Release();
            if (Application.isPlaying) Destroy(destino); else DestroyImmediate(destino);
            destino = null;
        }
    }

    void OnDestroy()
    {
        Liberar();
    }
}
