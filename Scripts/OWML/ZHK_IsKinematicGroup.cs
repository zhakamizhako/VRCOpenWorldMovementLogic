
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class ZHK_IsKinematicGroup : UdonSharpBehaviour
{

    public float RenderDistance = 200000;
    public Rigidbody[] RigidBodies;
    private int index = 0;
    private int maxLength = 0;
    public bool useForeach = false;
    public float waitTimer = 10f;
    private float timer;

    void Start()
    {
        maxLength = RigidBodies.Length;
    }
    void Update()
    {
        if (timer < waitTimer)
        {
            timer = timer + Time.deltaTime;
            return;
        }
        if (RigidBodies == null)
        {
            return;
        }

        LookLogic();
    }

    void cullLogic(int index)
    {
        var distance = Vector3.Distance(Networking.LocalPlayer != null ? Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position : Vector3.zero, RigidBodies[index].transform.position);

        if (distance > RenderDistance )
        {
            RigidBodies[index].isKinematic =true;
        }
        else
        {
            RigidBodies[index].isKinematic =false;
        }

    }

    public void LookLogic()
    {

        if (useForeach)
        {
            for(int i=0;i<maxLength;i++){
                cullLogic(i);
            }

            if(index!=0){
                index=0;
            }
        }
        else
        {
            if (index < maxLength)
            {
                cullLogic(index);
                if (index + 1 < maxLength)
                {
                    index = index + 1;
                }
                else
                {
                    index = 0;
                }
            }
        }
    }
}
