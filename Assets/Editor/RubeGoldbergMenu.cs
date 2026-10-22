using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Menú del editor para generar la máquina dentro de la escena abierta (vista previa en Edit Mode).
/// Al pulsar Play la máquina también se genera sola, aunque la escena esté vacía.
/// </summary>
public static class RubeGoldbergMenu
{
    const string RutaEscena = "Assets/Scenes/RubeGoldberg.unity";

    [MenuItem("Rube Goldberg/Construir máquina en la escena", priority = 0)]
    public static void Construir()
    {
        var vieja = GameObject.Find(RubeGoldbergBuilder.RootName);
        if (vieja != null)
        {
            // Ya está construida: la seleccionamos para no duplicar el entorno.
            Selection.activeGameObject = vieja;
            EditorGUIUtility.PingObject(vieja);
            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.FrameSelected();
                SceneView.lastActiveSceneView.Repaint();
            }
            return;
        }

        var maquina = RubeGoldbergBuilder.Build();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = maquina;
        EditorGUIUtility.PingObject(maquina);

        if (SceneView.lastActiveSceneView != null)
        {
            SceneView.lastActiveSceneView.FrameSelected();
            SceneView.lastActiveSceneView.Repaint();
        }
    }

    /// <summary>Reconstruye y guarda la escena desde cero (se usa también en batch).</summary>
    [MenuItem("Rube Goldberg/Reconstruir y guardar escena", priority = 10)]
    public static void ReconstruirYGuardar()
    {
        if (!Application.isBatchMode && !EditorUtility.DisplayDialog(
                "Reconstruir escena",
                "Se reemplazará Assets/Scenes/RubeGoldberg.unity con la máquina nueva.\n\n" +
                "Guarda cualquier cambio que quieras conservar antes de continuar.",
                "Reconstruir", "Cancelar"))
            return;

        var escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        RubeGoldbergBuilder.Build();

        if (!System.IO.Directory.Exists("Assets/Scenes"))
            System.IO.Directory.CreateDirectory("Assets/Scenes");

        EditorSceneManager.SaveScene(escena, RutaEscena);
        AssetDatabase.SaveAssets();

        if (Application.isBatchMode)
        {
            EditorApplication.Exit(0);
        }
        else
        {
            EditorUtility.DisplayDialog("Rube Goldberg",
                "Escena reconstruida y guardada en " + RutaEscena + ".\n" +
                "Pulsa Play para lanzar los 12 toques.", "¡Vamos!");
        }
    }

    [MenuItem("Rube Goldberg/Eliminar máquina de la escena", priority = 20)]
    public static void Eliminar()
    {
        var maquina = GameObject.Find(RubeGoldbergBuilder.RootName);
        if (maquina != null)
        {
            Object.DestroyImmediate(maquina);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        var camara = Camera.main;
        if (camara != null) camara.transform.position = new Vector3(-0.3f, 7.0f, -15.8f);
    }

    [MenuItem("Rube Goldberg/Abrir escena RubeGoldberg", priority = 40)]
    public static void AbrirEscena()
    {
        if (System.IO.File.Exists(RutaEscena))
        {
            EditorSceneManager.OpenScene(RutaEscena);
        }
        else
        {
            EditorUtility.DisplayDialog(
                "Rube Goldberg",
                "No se encontró " + RutaEscena + ".\n\nPuedes crear una escena nueva y pulsar Play: la máquina se genera sola.",
                "Entendido");
        }
    }

    [MenuItem("Rube Goldberg/Instrucciones", priority = 60)]
    public static void Instrucciones()
    {
        EditorUtility.DisplayDialog(
            "Máquina de Rube Goldberg",
            "1) Abre Assets/Scenes/RubeGoldberg.unity\n" +
            "2) Pulsa Play (▶)\n" +
            "3) La bola recorre los 12 toques: rampa → 6 fichas → torre →\n" +
            "   4 fichas → ficha final → plato de meta\n" +
            "4) R reinicia · F cámara lenta · T ray tracing\n\n" +
            "Para comprobar la cadena sin Play:\n" +
            "Rube Goldberg → Validar la cadena (12 toques)\n\n" +
            "Para reconstruir la escena:\n" +
            "Rube Goldberg → Reconstruir y guardar escena",
            "¡Vamos!");
    }
}
