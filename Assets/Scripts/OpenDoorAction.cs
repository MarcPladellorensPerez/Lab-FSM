using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "OpenDoor", story: "Open [DoorBlocker] with [HasKey] or [HasTool] and set [DoorOpen]", category: "Action", id: "790c4d9cf48f3c07b70345d129386422")]
public partial class OpenDoorAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> DoorBlocker;
    [SerializeReference] public BlackboardVariable<bool> HasKey;
    [SerializeReference] public BlackboardVariable<bool> HasTool;
    [SerializeReference] public BlackboardVariable<bool> DoorOpen;

    protected override Status OnStart()
    {
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        return Status.Success;
    }

    protected override void OnEnd()
    {
    }
}

