using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Valida la cadena de los 12 toques simulando la física en el editor (sin entrar en Play).
/// Sirve para comprobar que la máquina llega hasta la meta y para ajustar masas/posiciones.
/// </summary>
public static class RubeGoldbergValidator
{
    [MenuItem("Rube Goldberg/Validar la cadena (12 toques)", priority = 30)]
    public static void ValidarMenu()
    {
        if (!EditorUtility.DisplayDialog(
                "Validar la cadena",
                "Se abrirá una escena nueva para simular la física de los 12 toques.\n\n" +
                "Guarda la escena actual antes de continuar.",
                "Continuar", "Cancelar"))
            return;

        string informe = Simular();
        Debug.Log(informe);
        EditorUtility.DisplayDialog("Validación de la máquina", informe, "Cerrar");
    }

    /// <summary>Entrada para ejecutar en batch: Unity -batchmode -executeMethod RubeGoldbergValidator.ValidarBatch</summary>
    public static void ValidarBatch()
    {
        string informe = Simular();
        Debug.Log("[VALIDACION]\n" + informe);
        EditorApplication.Exit(0);
    }

    static string Simular()
    {
        EscenaNuevaLimpia();
        var maquina = RubeGoldbergBuilder.Build();
        var control = maquina.GetComponent<MachineController>();

        // La bola 1 empieza quieta (kinemática); la soltamos como hace el controlador.
        var rb1 = control.bola1.GetComponent<Rigidbody>();
        rb1.isKinematic = false;
        rb1.WakeUp();

        var modoAnterior = Physics.simulationMode;
        Physics.simulationMode = SimulationMode.Script;

        float dt = Mathf.Clamp(Time.fixedDeltaTime, 1f / 240f, 1f / 30f);
        const int pasosMax = 4200; // 70 s
        int toquesAnteriores = -1;
        float t = 0f;
        int paso = 0;

        for (; paso < pasosMax; paso++)
        {
            Physics.Simulate(dt);
            t += dt;
            control.EvaluarToques();

            int hechos = control.ToquesCumplidos;
            if (hechos != toquesAnteriores)
            {
                toquesAnteriores = hechos;
                Debug.Log("[VALIDACION] t=" + t.ToString("0.00") + " s  toques=" + hechos + "/12");
            }

            if (hechos >= 12 && control.objetoFinal != null && control.objetoFinal.position.y < 0.43f)
                break;
        }

        Physics.simulationMode = modoAnterior;

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Simulación: " + t.ToString("0.00") + " s (" + paso + " pasos, dt=" + dt.ToString("0.0000") + ")");
        sb.AppendLine("Toques completados: " + control.ToquesCumplidos + "/12");

        for (int i = 0; i < control.toques.Count; i++)
        {
            var toque = control.toques[i];
            string estado = toque.cumplido ? "OK " : "FALLO";
            Vector3 p = toque.cuerpo != null ? toque.cuerpo.position : Vector3.zero;
            sb.AppendLine("  " + (i + 1).ToString("00") + " [" + estado + "] " + toque.descripcion +
                          "   pos=" + p.ToString("0.00"));
        }

        if (control.objetoFinal != null)
        {
            Vector3 p = control.objetoFinal.position;
            sb.AppendLine("Ficha final: " + p.ToString("0.00") +
                          "  meta=" + ((p.x > 4.6f && p.x < 6.7f && p.y < 0.43f) ? "SÍ" : "NO"));

            var zona = GameObject.Find("Zona de meta");
            var caja = zona != null ? zona.GetComponent<BoxCollider>() : null;
            bool solapa = false;
            if (caja != null)
            {
                var cols = Physics.OverlapBox(zona.transform.position,
                                              Vector3.Scale(caja.size, zona.transform.lossyScale) * 0.5f,
                                              zona.transform.rotation);
                for (int i = 0; i < cols.Length; i++)
                    if (cols[i] != null && cols[i].attachedRigidbody == control.objetoFinal) solapa = true;
            }
            sb.AppendLine("Solape real con el trigger de meta: " + (solapa ? "SÍ" : "NO"));
        }
        return sb.ToString();
    }

    static void EscenaNuevaLimpia()
    {
        // Se crea una escena vacía para que la simulación no choque con la máquina guardada.
        var escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorSceneManager.MarkSceneDirty(escena);
    }
}
