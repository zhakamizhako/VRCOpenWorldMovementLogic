
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class ZHK_PlayerFollower : UdonSharpBehaviour
{
    public Transform FollowingObject;
    private VRCPlayerApi localPlayer;

    void Start()
    {
        localPlayer = Networking.LocalPlayer;
    }
    void PostLateUpdate()
    {
        FollowingObject.position = localPlayer.GetPosition();
    }
}
