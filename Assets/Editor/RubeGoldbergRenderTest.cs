using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Renderiza un fotograma con el trazador de rayos y lo guarda en PNG.
/// Sirve para comprobar que el shader compila y que la vista RT no sale en negro.
/// </summary>
public static class RubeGoldbergRenderTest
{
    public static void Probar()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        RubeGoldbergBuilder.Build();

        var cam = Camera.main;
        var rt = cam != null ? cam.GetComponent<RayTracingView>() : null;
        string ruta = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "RayTracing_Prueba.png");
        string rutaNormal = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Normal_Prueba.png");

        // Render normal (desactiva el efecto para ver la escena rasterizada).
        if (cam != null)
        {
            if (rt != null) rt.enabled = false;
            var destino = new RenderTexture(1280, 720, 24);
            cam.targetTexture = destino;
            cam.Render();
            Guardar(destino, rutaNormal);
            cam.targetTexture = null;
            destino.Release();
            Object.DestroyImmediate(destino);
        }

        bool ok = rt != null && rt.RenderizarPrueba(1280, 720, ruta);
        Debug.Log("[RayTracing] Prueba " + (ok ? "OK" : "FALLÓ") + " -> " + ruta);

        if (Application.isBatchMode) EditorApplication.Exit(ok ? 0 : 1);
    }

    static void Guardar(RenderTexture origen, string ruta)
    {
        var anterior = RenderTexture.active;
        RenderTexture.active = origen;
        var tex = new Texture2D(origen.width, origen.height, TextureFormat.RGB24, false, false);
        tex.ReadPixels(new Rect(0, 0, origen.width, origen.height), 0, 0);
        tex.Apply();
        RenderTexture.active = anterior;
        System.IO.File.WriteAllBytes(ruta, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }
}
