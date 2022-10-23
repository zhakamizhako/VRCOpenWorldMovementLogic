using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

// ZHK Player Station
// Per station / player synchronization script for ZHK_OpenWorldMovementLogic
// For use with SaccFlight 1.5 Onwards & ZHK_OpenWorldMovementLogic
// Dependencies:
// ZHK_UIScript - Local Player Manager
// ZHK_OWML_Player - Global Player Manager script
// Contact: Twitter: @zzhako / Discord: ZhakamiZhako#2147

// TODO: Further Optimization & smoother player movement
[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class ZHK_OWML_Station : UdonSharpBehaviour
{
[VRC.Udon.Serialization.OdinSerializer.OdinSerialize] /* UdonSharp auto-upgrade: serialization */     public VRCPlayerApi Player;
    [Tooltip("Required: Your Scene's UI Script / 必須 あなたのシーンのUIスクリプト")]
    public ZHK_UIScript UIScript;
    [Tooltip("A mesh or gameobject that's an indicator to let you know that this player's station is working or not. You may replace it with something else. \n \n " +
             "このプレイヤーのステーションが機能しているかどうかを知らせるためのインジケータとなるメッシュやゲームオブジェクトです。他のものに置き換えてもよい。")]
    public GameObject IndicatorDebug;
    
    [Tooltip("This station's VRCStation. Best to set this up on this very same gameobject. " +
             "\n \n このステーションのVRCStationです。この全く同じゲームオブジェクトに設定するのがベストです。")]
    public VRCStation stationObject;
    
    [System.NonSerializedAttribute] [UdonSynced] public Vector3 CurrentPlayerPosition = Vector3.zero;
    [System.NonSerializedAttribute] [UdonSynced] public Quaternion CurrentPlayerRotation;
    [System.NonSerializedAttribute] [UdonSynced, FieldChangeCallback(nameof(inVehicle))] public bool _inVehicle = false;
    [System.NonSerializedAttribute] [UdonSynced, FieldChangeCallback((nameof(PlayerID)))] public int _PlayerID = -1;
    public Vector3 oldPos = Vector3.zero;
    private Quaternion oldRot;
    private float timerPlayerUpdate = 0f;
    private float distanceFromCenter = 0f;

    private Vector3 PlayerPosition = Vector3.zero;
    private Vector3 TemporaryVelocity = Vector3.zero;
    private bool nextFrame = false;

    private float timeoutTimer = 0f;
    public int PlayerID
    {
        set
        {
            _PlayerID = value;
            if(Networking.LocalPlayer.playerId!=value && value!=-1 && IndicatorDebug!=null && UIScript.showDebugPlayerPos){ IndicatorDebug.SetActive(true); }
            else
            {
                if(IndicatorDebug!=null) IndicatorDebug.SetActive(false);
            }
        }
        get => _PlayerID;
    }
    [HideInInspector]public bool isMe = false;
[VRC.Udon.Serialization.OdinSerializer.OdinSerialize] /* UdonSharp auto-upgrade: serialization */     public VRCPlayerApi LocalPlayer;
    [HideInInspector]public bool playerSet = false;

    public bool inVehicle
    {
        set
        {
            _inVehicle = value;
            Debug.Log("In vehicle Called "+ value, this);
            if (Networking.IsOwner(gameObject) && UIScript.stationObject == this)
            {
                if (!value)
                {
                    stationObject.transform.position = Networking.LocalPlayer.GetPosition();
                    stationObject.PlayerMobility = VRCStation.Mobility.Mobile;
                    stationObject.UseStation(Networking.LocalPlayer);
                }
            }
            // Debug.Log("In vehicle Called "+ value, this);
            // if (Player == null)
            // {
            //     Debug.Log("Player null?");
            //     return;
            // }
            //
            // if (Networking.LocalPlayer == null)
            // {
            //     _inVehicle = value;}
            //
            // if(Networking.IsOwner(gameObject)){ _inVehicle = value; }
            //
            // if (Networking.LocalPlayer!=null &&  Networking.IsOwner(gameObject) && UIScript.stationObject==this)
            // {
            //     _inVehicle = value;
            //     if (!value)
            //     {
            //         stationObject.transform.position = Networking.LocalPlayer.GetPosition();
            //         stationObject.PlayerMobility = VRCStation.Mobility.Mobile;
            //         stationObject.UseStation(Networking.LocalPlayer);
            //     }
            //     RequestSerialization();
            // }
        }
        get => _inVehicle;
    }
    [Tooltip("Required: Your Scene's OWML PlayerController Gameobject. \n \n 必須 シーンの OWML PlayerController Gameobject。")]
    public ZHK_OWML_Player OWML_Player;
    void Start()
    {
        UIScript = OWML_Player.UIScript;
        transform.position = Vector3.zero;
        stationObject.PlayerMobility = VRCStation.Mobility.Mobile;
        // stationObject.disableStationExit = true;
        LocalPlayer = Networking.LocalPlayer;
    }

    public void register(VRCPlayerApi z)
    {
        if (z.playerId == Networking.LocalPlayer.playerId && UIScript.stationObject != null && UIScript.stationObject != this)
        {
            Debug.Log("Aborting Assignment on station due to a duplicate risk");
            return;
        }
        // Start stuff
        // UIScript = OWML_Player.UIScript;
        // transform.position = Vector3.zero;
        // stationObject.PlayerMobility = VRCStation.Mobility.Mobile;
        // // stationObject.disableStationExit = true;
        LocalPlayer = Networking.LocalPlayer;
        // //
        
        Debug.Log("Player REGISTER RECEIVED:" + z.displayName);
        Player = z;
        if (Player == Networking.LocalPlayer || Player.isLocal)
        {
            isMe = true;
            Player = Networking.LocalPlayer;
        }
        else
        {
            isMe = false;
            Player = z; // <---- This is local only, but will be assigned once the other player 'acknowledges' it
        }

        PlayerID = z.playerId;
        RequestSerialization();

        if (!isMe)
        {
            gameObject.SetActive(true);
            Networking.SetOwner(z, gameObject);//transfer this chair to the other player.
            SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(forceRegister));    
        }
        
        if (isMe) // same as onOwnership transferred but locally. 
        {
            gameObject.SetActive(true);
            UIScript.stationObject = this;
            isMe = true;
            PlayerID = LocalPlayer.playerId;
            RequestSerialization();
            SendCustomEventDelayedSeconds(nameof(useSeat),2);
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(broadcastRegistered));
        }
    }

    public void forceRegister()
    {
        register(Networking.LocalPlayer);
    }

    public void broadcastRegistered()
    {
        Debug.Log("registered for "+ PlayerID);
        gameObject.SetActive(true);
        
    }
    public void unregister()
    {
        Debug.Log("UNREGISTERING" + PlayerID);
        inVehicle = false;
        CurrentPlayerPosition = Vector3.zero;
        Player = null;
        PlayerID = -1;
        playerSet = false;

        if(Networking.IsOwner(gameObject))
        {
            RequestSerialization(); 
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(publicUnregister));
        }
        gameObject.SetActive(false);
    }

    public void publicUnregister()
    {
        inVehicle = false;
        CurrentPlayerPosition = Vector3.zero;
        Player = null;
        PlayerID = -1;
        playerSet = false;
        gameObject.SetActive(false);
    }

    public override void OnPlayerRespawn(VRCPlayerApi player)
    {
        if (Networking.IsOwner(gameObject))
        {
            inVehicle = false;
            RequestSerialization();   
            useSeat(); // Ey, quick fix
        }
    }

    public override void OnOwnershipTransferred(VRCPlayerApi player)
    {
        Debug.Log("OnOwnershipcall ASD");
        Player = Networking.GetOwner(gameObject);
        if (player.isLocal)
        {
            if (UIScript.stationObject == null)
            {
                Debug.Log("OnOwnershipcall IN!");
                gameObject.SetActive(true);
                UIScript.stationObject = this;
                isMe = true;
                Player = Networking.LocalPlayer;
                PlayerID = Networking.LocalPlayer.playerId;
                RequestSerialization();
                SendCustomEventDelayedSeconds(nameof(useSeat),2);   
            }
            else
            {
                unregister();
                // OWML_Player.sort(PlayerID);
            }
        }
        //Rule: UIScript.stationObject is YOUR station. If there's an ownership transfer, probably means that someone got disconnected.
        if(player.isLocal && UIScript.stationObject!=null){ // Probably what happened is someone disconnected and the player stuff wasn't even unregistered
            unregister();
            // OWML_Player.sort(PlayerID);
        }
    }

    public void broadcastOwnershipRecheck()
    {
        SendCustomNetworkEvent(NetworkEventTarget.All, nameof(OwnershipRecheck));
    }

    public void OwnershipRecheck()
    {
        Debug.Log("Player ID:" + Networking.LocalPlayer.playerId + "- Ownership recheck : HAS STATION:"+(UIScript.stationObject? "Yes" : "No"+ "And do I own it? "+ (Networking.IsOwner(gameObject))));
        gameObject.SetActive(true);
        if (UIScript.stationObject == null && Networking.IsOwner(gameObject))
        {
            register(Networking.LocalPlayer);
        }
    }

    public void useSeat()
    {
        if(Player == Networking.LocalPlayer)
        {
            stationObject.PlayerMobility = VRCStation.Mobility.Mobile;
            stationObject.UseStation(Networking.LocalPlayer);
        }
    }

    public override void OnStationEntered(VRCPlayerApi player) // station enter logic
    {
        Debug.Log(player.displayName +" -- Player Sync mode");
        if (!player.isLocal)
        {
            stationObject.PlayerMobility = VRCStation.Mobility.Immobilize; // locally set the station to immobilize, disabling this player's movement on that station locally
        }

        if (player.isLocal)
        {
            stationObject.PlayerMobility = VRCStation.Mobility.Mobile;
            Networking.LocalPlayer.SetVelocity(TemporaryVelocity);
        }
    }

    public override void OnStationExited(VRCPlayerApi player)
    {
        Debug.Log(player.displayName+ " Entered a vehicle or something else.");
        stationObject.PlayerMobility = VRCStation.Mobility.Mobile;
        //Do something over here if in case player has been using another seat.
    }

    public void checkIfPlayerPresent() // function call to 'synchronize' the player that's not in a station locally
    {
        if (Player != null && PlayerID != -1 && !inVehicle)
        {
            // stationObject.PlayerMobility = VRCStation.Mobility.Mobile;
            SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(checkOwner));
            playerSet = true;
        }
    }

    public void checkOwner() // function call to 'synchronize' the player that's not in a station, broadcasts to the owner that he has to be in one.
    {
        if (Networking.IsOwner(gameObject) && UIScript.stationObject==this)
        {
            if(Player==null)  Player = Networking.LocalPlayer;
            stationObject.transform.position = Player.GetPosition();
            stationObject.transform.rotation = Player.GetRotation();
            Debug.Log("Resynchronize Call received for station "+gameObject.name +" of player:"+ Player.playerId + " for station player id:"+ PlayerID);
            if(!inVehicle) SendCustomNetworkEvent(NetworkEventTarget.All, nameof(useSeat));   
        }
    }
    
    void Update()
    {
        if (PlayerID == -1 && UIScript.stationObject != this)
        {
            timeoutTimer = timeoutTimer + Time.deltaTime;

            if (timeoutTimer > UIScript.StationTimeout)
            {
                Debug.Log("Deactivating: "+gameObject.name + " due to timeout.");
                timeoutTimer = 0;
                gameObject.SetActive(false);
            }
        }
        // if (Networking.IsOwner(gameObject) && UIScript.stationObject!=this)
        // {
        //     unregister();
        // }

        if (PlayerID != -1 || UIScript.stationObject==this)
        {
            if (UIScript.stationObject == this && PlayerID==-1)
            {
                PlayerID = Networking.LocalPlayer.playerId;
                RequestSerialization();
                SendCustomEventDelayedSeconds(nameof(useSeat),1);
                playerSet = true;
            }
            if (isMe)
            {
                if (!nextFrame && !inVehicle)
                {
                    nextFrame = true;
                    SendCustomNetworkEvent(NetworkEventTarget.All, nameof(useSeat));
                }
                if (UIScript.PlayerAircraft != null && !inVehicle)
                {
                    inVehicle = true;
                    RequestSerialization();
                }

                if (inVehicle && UIScript.PlayerAircraft == null)
                {
                    inVehicle = false;  
                    RequestSerialization();
                }
                
                if ((!inVehicle || UIScript.syncEvenInVehicle))
                {
                    PlayerPosition = Networking.LocalPlayer.GetPosition();
                    if(/*!Networking.IsClogged && */timerPlayerUpdate > UIScript.PlayerUpdateRate)
                    {
                        CurrentPlayerPosition = -UIScript.Map.position + Networking.LocalPlayer.GetPosition();  
                        // stationObject.transform.position = PlayerPosition; //<-- StationObject syncing 'calling it a day'
                        // CurrentPlayerPosition = stationObject.transform.localPosition;//<-- StationObject syncing 'calling it a day'
                        CurrentPlayerRotation = Networking.LocalPlayer.GetRotation();
                        RequestSerialization();
                        timerPlayerUpdate = 0f;
                    }
                }
                
                if (!inVehicle && UIScript.allowPlayerOWML) // Player Teleport MUST NOT BE CALLED WHILE FLYING! 
                {
                    if (Vector3.Distance(PlayerPosition, Vector3.zero) > UIScript.ChunkDistance)
                    {
                        var tempAnchor = PlayerPosition;
                        UIScript.Map.position = UIScript.Map.position - PlayerPosition;
                        TemporaryVelocity = Networking.LocalPlayer.GetVelocity();
                        Networking.LocalPlayer.TeleportTo(Vector3.zero, Networking.LocalPlayer.GetRotation());
                        UIScript.doChunkUpdate(tempAnchor);
                        nextFrame = false;
                    }
                }

                timerPlayerUpdate = timerPlayerUpdate + Time.deltaTime;
            }
            else
            {
                if (Player == null) { Player = Networking.GetOwner(gameObject); playerSet=true; }
                if (stationObject != null)
                {
                    if (!inVehicle || UIScript.syncEvenInVehicle)
                    {
                        // stationObject.transform.position = Vector3.MoveTowards(oldPos, UIScript.Map.position + CurrentPlayerPosition, 6);
                        stationObject.transform.position = Vector3.Lerp(oldPos, UIScript.Map.position + CurrentPlayerPosition, Time.deltaTime * 10);
                        // stationObject.transform.localPosition = Vector3.Lerp(oldPos,CurrentPlayerPosition, Time.deltaTime); //<-- stationObject local position syncing *Call it a day*
                        stationObject.transform.rotation =  Quaternion.Slerp(oldRot, CurrentPlayerRotation, Time.deltaTime * 10);
                        oldPos =  stationObject.transform.position;
                        oldRot = stationObject.transform.rotation;
                    }
                }
            }
        }
    }
}

