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
        if (vieja != null) Object.DestroyImmediate(vieja);

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
        if (camara != null) camara.transform.position = new Vector3(-1.2f, 6.6f, -12.8f);
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
            "3) La bola se suelta sola y recorre: rampa → dominó → bola 2 → meta\n" +
            "4) R reinicia · F cámara lenta\n\n" +
            "Para ver la máquina en el editor sin pulsar Play, usa\n" +
            "Rube Goldberg → Construir máquina en la escena\n" +
            "(y guarda con Ctrl+S).",
            "¡Vamos!");
    }
}
