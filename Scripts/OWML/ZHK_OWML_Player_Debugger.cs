
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

public class ZHK_OWML_Player_Debugger : UdonSharpBehaviour
{
    public Text DebugText;
    public ZHK_OWML_Station StationBase;

    void Start()
    {
        if (DebugText == null || StationBase == null)
        {
            gameObject.SetActive(false);
        }
    }
    void Update()
    {
        if (DebugText != null)
        {
            string output = "";
            output += StationBase.Player.displayName;
            output += "\n x:" + StationBase.CurrentPlayerPosition.x;
            output += "\n y:" + StationBase.CurrentPlayerPosition.y;
            output += "\n z:" + StationBase.CurrentPlayerPosition.z;
            output += "\n inVehicle:" + StationBase.inVehicle;
            output += "\n PlayerID:" + StationBase.PlayerID;

            DebugText.text = output;
        }
    }
}
