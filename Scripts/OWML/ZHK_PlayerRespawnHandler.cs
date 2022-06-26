
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class ZHK_PlayerRespawnHandler : UdonSharpBehaviour
{
    public Transform MapObject;
    public bool PlayerEntered = false;
    public void OnPlayerTriggerEnter (VRCPlayerApi Player){
        if(!PlayerEntered && Networking.LocalPlayer == Player){
            MapObject.position = Vector3.zero;
            Player.SetVelocity(Vector3.zero);
            PlayerEntered = true;
        }
    }
    public void OnPlayerTriggerExit (VRCPlayerApi player)
    {
        PlayerEntered = false;
    }

    public override void OnPlayerRespawn(VRCPlayerApi player)
    {
        if(player.isLocal){
            MapObject.position = Vector3.zero;
            player.SetVelocity(Vector3.zero);
            PlayerEntered = true;
        }
    }
}
