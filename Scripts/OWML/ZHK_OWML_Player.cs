using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

/*
OWML Player Controller
For SaccFlight 1.5

by: Zhakami Zhako
Discord: ZhakamiZhako#2147
Twitter: @ZZhako
Email: zhintamizhakami@gmail.com
*/
[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class ZHK_OWML_Player : UdonSharpBehaviour
{
    [Tooltip("Required: Your Scene's  ZHK_UIScript Gameobject. ")]
    public ZHK_UIScript UIScript;
    VRCPlayerApi[] players = new VRCPlayerApi[80];
    [Tooltip("Required: 80 Player Stations. (ZHK_OWML_Station)")]
    public ZHK_OWML_Station[] Stations;

    public override void OnPlayerJoined(VRCPlayerApi player)
    {
        // VRCPlayerApi.GetPlayers(players);
        register(player);
        
    }

    public override void OnPlayerLeft(VRCPlayerApi player)
    {
        // VRCPlayerApi.GetPlayers(players);
        unregister(player);
    }

    public override void OnOwnershipTransferred(VRCPlayerApi player)
    {
        if (player.isLocal)
        {
            Debug.Log("Owner Disconnected. Re-Sorting");
            // sort(-1);
        }
    }

    public void checkPlayer()
    {
        foreach (var x in Stations)
        {
            x.checkIfPlayerPresent();
        }
    }

    public void recheckPlayerIDs()
    {
        Debug.Log("recheck Players request");
        SendCustomNetworkEvent(NetworkEventTarget.All, nameof(recheckPlayers));
    }

    // public bool debugPrintPlayers()
    // {
    //     foreach (var x in players)
    //     {
    //         if(x!=null) Debug.Log("P:id:"+x.playerId);
    //     }
    //
    //     return true;
    //
    // }
    public void recheckPlayers()
    {
        if (!Networking.IsOwner(gameObject))  {return; }
        Debug.Log("Someone still has no station after 15 seconds. Rechecking Players");
        players = VRCPlayerApi.GetPlayers(players);
        
        Debug.Log("TestPlayers");
        Debug.Log(players.ToString());
        // debugPrintPlayers();

        // int stationIndex = -1;
        // int playerIndex = -1;
        bool noMiss = true;
        for (int x = 0; x < players.Length; x++)
        {
            if (players[x] != null)
            {
                Debug.Log("PlayerCheck;; ID:"+players[x].playerId+" || Name:"+ (players[x].displayName!=null ? players[x].displayName : "null?") + "PlayerIsValid?:" +
                          (players[x].IsValid() ? "true" : "false"));   
            }
            if (players[x] != null && players[x].displayName!=null)
            {
                bool found = false;
                for (int y = 0; y < Stations.Length; y++)
                {
                    if (Stations[y].PlayerID == players[x].playerId)
                    {
                        Debug.Log("Found PlayerID "+ players[x].playerId + " on stationid" + Stations[y]);
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    Debug.Log("Loop done, player station not found for "+players[x].playerId);
                    register(players[x]);
                    noMiss = false;
                }
            }
        }

        // if (noMiss)
        // {
        Debug.Log("Ownership recheck...");
            ownershipRechecks();
        // }
    }

    public void resyncCall()
    {
        Debug.Log("Resynchronize Call!");
        foreach (var x in Stations)
        {
            x.SendCustomNetworkEvent(NetworkEventTarget.All, nameof(x.checkOwner));
        }
    }

    public void ownershipRechecks()
    {
        foreach (var x in Stations)
        {
            x.broadcastOwnershipRecheck();
        }
    }
    
    public void register(VRCPlayerApi xx)
    {
        if (!Networking.IsOwner(gameObject))
        {
            Debug.Log("!!Cannot register; Not the owner. ");
            return;
        }
        int target = -1;
        for(int x=0;x<Stations.Length;x++){
            if(Stations[x].PlayerID==-1){
                target = x;
                break;
            }
        }
        if(target == -1) { return; }
        Debug.Log("Player REGISTER:" + xx.displayName);
        Stations[target].register(xx);
        
        // Stations[index].gameObject.SetActive(true);
        SendCustomEventDelayedSeconds(nameof(doDelay), 7);
    }
    
    public void doDelay()
    {
        SendCustomNetworkEvent(NetworkEventTarget.All, nameof(checkPlayer));
    }

    public void unregister(VRCPlayerApi xx)
    {
        if(!Networking.IsOwner(gameObject)) return;
        int d_player_id = xx.playerId;
        int target = -1;
        for(int x=0;x<Stations.Length;x++){
            if(Stations[x].PlayerID==d_player_id){
                target = x;
                break;
            }
        }
        if(target == -1){ return; }
        Debug.Log("Player Unregister:" + xx.displayName);
        // Stations[target].gameObject.GetComponent<UdonBehaviour>().SendCustomNetworkEvent(NetworkEventTarget.All, "unregister");
        // SendCustomNetworkEvent( NetworkEventTarget.All,Stations[target].unregister());   
        Stations[target].unregister();
    }
}
