using UnityEngine;

/// <summary>Detecta que una bola entró en la zona de meta.</summary>
public class GoalSensor : MonoBehaviour
{
    public MachineController control;

    void OnTriggerEnter(Collider other)
    {
        if (control == null) return;
        if (other.attachedRigidbody == null) return;
        if (other.attachedRigidbody.isKinematic) return;
        control.CompletarMeta();
    }
}
