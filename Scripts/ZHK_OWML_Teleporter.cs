
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class ZHK_OWML_Teleporter : UdonSharpBehaviour
{
    public ZHK_UIScript UIScript;
    public Transform TargetLocation;
    public Vector3 offsetPos;

    public override void Interact()
    {
        Networking.LocalPlayer.TeleportTo(new Vector3(0,0,0), Networking.LocalPlayer.GetRotation());
        UIScript.Map.position = UIScript.Map.position - TargetLocation.position + offsetPos;
        Networking.LocalPlayer.SetVelocity(Vector3.zero);
    }
}
