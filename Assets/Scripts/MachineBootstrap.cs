using UnityEngine;

/// <summary>
/// Arranca la máquina automáticamente al pulsar Play, sin importar qué escena esté abierta.
/// Si la escena ya tiene la máquina (por ejemplo, generada con el menú del editor), no la duplica.
/// </summary>
public static class MachineBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Arrancar()
    {
        if (GameObject.Find(RubeGoldbergBuilder.RootName) != null) return;
        RubeGoldbergBuilder.Build();
    }
}
