using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "OpenDoorAction", story: "Open [DoorBlocker] using [HasKey] or [HasTool] and set [DoorOpen]", category: "Action", id: "12984776c3ddf3784c9bc56ed8924525")]
public partial class OpenDoorAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> DoorBlocker;
    [SerializeReference] public BlackboardVariable<bool> HasKey;
    [SerializeReference] public BlackboardVariable<bool> HasTool;
    [SerializeReference] public BlackboardVariable<bool> DoorOpen;

    protected override Status OnStart()
    {
        if (DoorOpen != null && DoorOpen.Value) // Puerta ya abierta, Success
        {
            return Status.Success;
        }

        // Si falta la referencia a la puerta o no tenemos ni llave ni herramienta, Failure
        bool hasResource = (HasKey != null && HasKey.Value) || (HasTool != null && HasTool.Value);
        if (DoorBlocker == null || DoorBlocker.Value == null || !hasResource)
        {
            return Status.Failure;
        }

        // Abrimos la puerta desactivando el obstáculo y guardamos el estado en el Blackboard
        DoorBlocker.Value.SetActive(false);
        if (DoorOpen != null)
        {
            DoorOpen.Value = true;
        }

        return Status.Success;
    }
}

