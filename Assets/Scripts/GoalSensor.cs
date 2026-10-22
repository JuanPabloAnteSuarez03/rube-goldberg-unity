using UnityEngine;

/// <summary>Detecta que la ficha final entró en la zona de meta.</summary>
public class GoalSensor : MonoBehaviour
{
    public MachineController control;

    void OnTriggerEnter(Collider other)
    {
        if (control == null) return;

        var rb = other.attachedRigidbody;
        if (rb == null || rb.isKinematic) return;

        // Solo la ficha final puede completar la máquina.
        if (control.objetoFinal != null && rb != control.objetoFinal) return;

        control.CompletarMeta();
    }
}
