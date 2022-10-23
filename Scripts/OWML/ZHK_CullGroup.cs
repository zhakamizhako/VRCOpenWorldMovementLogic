
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

// ZHK Cull by distance and angle
// Contact: Twitter: @zzhako / Discord: ZhakamiZhako#2147
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class ZHK_CullGroup : UdonSharpBehaviour
{
    public float RenderDistance = 200000;
    public float cullAngle = 180;
    public GameObject[] CullObjects;
    private int index = 0;
    private int maxLength = 0;
    private VRCPlayerApi localPlayer;
    public bool useForeach = false;
    public float waitTimer = 10f;
    private float timer;

    void Start()
    {
        localPlayer = Networking.LocalPlayer;
        maxLength = CullObjects.Length;
    }
    void Update()
    {
        if (timer < waitTimer)
        {
            timer = timer + Time.deltaTime;
            return;
        }
        if (CullObjects == null)
        {
            return;
        }

        LookLogic();
    }

    void cullLogic(int index)
    {
        var distance = Vector3.Distance(Networking.LocalPlayer != null ? Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position : Vector3.zero, CullObjects[index].transform.position);
        var ObjectToTargetVector = CullObjects[index].transform.position - (Networking.LocalPlayer != null ? Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position : Vector3.zero);
        var Forward = Networking.LocalPlayer != null ? Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation * Vector3.forward : Vector3.zero;
        var targetDirection = ObjectToTargetVector.normalized;
        var tempAngleCheck = Vector3.Angle(targetDirection, Forward);

        if (distance > RenderDistance || tempAngleCheck > cullAngle)
        {
            CullObjects[index].SetActive(false);
        }
        else
        {
            CullObjects[index].SetActive(true);
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
