
using SaccFlightAndVehicles;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

/*
UI Script controller
For SaccFlight 1.5

by: Zhakami Zhako
Discord: ZhakamiZhako#2147
Twitter: @ZZhako
Email: zhintamizhakami@gmail.com

Note: Reduced as 'global' World movement controller for now as the script is a port of PlayerUIScript. 

Treat this as the local player controller & settings.

!!!IMPORTANT!!!!
Add **ALL** SyncScripts in the SAV_SyncScript[].
*/
[DefaultExecutionOrder(-11)]
[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class ZHK_UIScript : UdonSharpBehaviour
{
    [HideInInspector] public UdonBehaviour PlayerAircraft;
    [HideInInspector] public ZHK_OpenWorldMovementLogic OWML;

    [InspectorName("REQUIRED: Map space object / マップオブジェクト")] public Transform Map; //<- Required
    public Transform EmptyTransform; // <- Just place an empty transform. Used for the respawning fix.

    [System.NonSerializedAttribute] public ZHK_OWML_Station stationObject;
    
    [Tooltip("REQUIRED: Your Scene's Player list manager script. (ZHK_OWML_Player) \n \n あなたのシーンのプレイヤーリスト管理スクリプトです。 (ZHK_OWML_Player)")]
    public ZHK_OWML_Player PlayerManager;

    [Tooltip("Recommended distance is around 3km (3000). Floating point errors will start appearing after 1000m away from the origin point. " +
             "A higher value may result to noticable jitter, and a lower value may result to more frequent update calls. \n \n " +
             "推奨距離は3km(3000)程度です。浮動小数点演算の誤差は原点から1000m離れたあたりから出始めます。値を大きくするとジッターが目立つようになり、小さくすると更新の呼び出しが頻繁になる可能性があります。")]
    public float ChunkDistance = 3000f;

    [Tooltip("Sky settings for procedural skybox. If you don't have procedural skybox, please don't use this.\n \n " +
            "プロシージャルスカイボックス用の空の設定です。プロシージャルスカイボックスをお持ちでない方は、使用しないでください。")] 
    public Material Skybox;
    public float baseAtmos = 1f;
    [Tooltip("Specifies at what altitude does the atmosphere starts to darken \n \n " +
             "大気が暗くなり始める高度を指定します。")]
    public float AtmosphereDarkStart = 30000;
    [Tooltip("Specifies at what altitude does the atmosphere darkening to stop \n \n " +
             "どの高度で大気の暗転を停止させるかを指定する")]
    public float AtmosphereDarkMax = 50000;
    //
    // [Header("Cloud Settings")] public Material CloudMat;
    // public float baseHeight = 10000f;
    
    [Tooltip("Enable Player Debugging 'Sphere' \n \n プレイヤーデバッグ「スフィア」を有効化する")]
    public bool showDebugPlayerPos; // <-- Positional debugger for the stations
    
    [Tooltip("Enables Synchronization even when the player is indeed inside a vehicle. May cause heavier network traffic \n \n " +
             "プレイヤーが車内にいる場合でも、同期が可能になります。ネットワークトラフィックが重くなる可能性があります。")]
    public bool syncEvenInVehicle;
    
    [Tooltip("Enable this to allow player and map offset according to the distance threshold. Note that this will be disabled in editor mode due to a bug in CyanEmu. " +
             "\n \n これを有効にすると、距離の閾値に応じてプレイヤーとマップのオフセットが可能になります。なお、エディタモードではCyanEmuのバグにより無効化されます。")]
    [InspectorName("Enable Player OWML")]public bool allowPlayerOWML;

    [Header("!!! WHEN CHANGING THE AMOUNT OF VEHICLES PRESENT IN THE SCENE, PLEASE ADD EVERY SYNC SCRIPT INTO THIS VARIABLE. !!!")]
    [Header("!!! シーンに存在する車両の量を変更する場合、この変数にすべての同期スクリプトを追加してください。 !!!")]
    public SAV_SyncScript_OWML[] saccSyncList;

    [Tooltip("This specifies the update interval rate for a player to send his current position when not piloting an aircraft. Best to keep it 0.4 seconds. \n \n" +
             "航空機を操縦していない時に、プレイヤーが現在位置を送信する際の更新間隔レートを指定します。0.4秒を目安にするとよいでしょう。")]
    public float PlayerUpdateRate = 0.4f;

    [Tooltip("Specifies the interval time whenever a player hasn't received a station from the instance owner and requests for one. Best to keep it at 15 seconds. \n \n " +
             "インスタンスの所有者からステーションを受け取っていないプレイヤーが、ステーションを要求する際のインターバル時間を指定します。15秒を目安にするとよいでしょう。")]
    public float recheckInterval = 15f;
    private float timer = 0f;

    [Tooltip("Specifies whether to use Sacchan's Atmosphere thinning or not. Disable this to allow space travel or specify a huge amount on your aircraft's SAVController. \n \n " +
             "SacchanのAtmosphere Thinningを使用するかどうかを指定します。これを無効にすると宇宙旅行ができるようになるか、機体のSAVControllerで大量に指定することになります。")]
    public bool UseAtmosphere = true;

    [Header("For your needs if you have a group of objects to follow the current local player.")]
    public Transform PlayerFollowObject; // For a moving skybox or anything in regards to following a player.
    
    private VRCPlayerApi localPlayer;
    private UdonSharpBehaviour[] SAVs;

    [Tooltip("Optional. Assign an animator to translate atmosphere. \n \n " +
             "TranslationJP")]
    public Animator AltitudeAnimator;
    public string AltitudeParameter = "altitudeFloat";
    public float MaximumAltitude = 60000;
    
    // private CollisionDetectionMode[] CDMs;
    private int kIndex = 0;
    
    [Header("To set as 'kinematic' on vehicles that are 'away' from you")]
    public bool DoKinematicCheck = true;
    public float DistanceKinematicCheck = 3000f;

    private float offsetHeight = 0f;
    public float StationTimeout = 20f;

    public bool CallResyncs = true;
    public float resyncTimer = 15;
    private float resyncTimerProper = 0f;

    public Slider OWMLSlider;
    public Text OWMLSliderText;

    [Tooltip("Useful before uploading when you have like > 10 vehicles.")]public bool DisableVehiclesUponUpload = true;

    public bool initialized = false;
    public float initializedTimer = 15f;
    public float initTimer = 0f;
    

    // public UdonBehaviour TriggerScriptPlugin; // For future plugin

    void Start()
    {
        if (Skybox != null)
        {
            // baseAtmos = Skybox.GetFloat("_AtmosphereThickness");
            Skybox.SetFloat("_AtmosphereThickness", baseAtmos);
        }
        //
        // if (CloudMat != null)
        // {
        //     CloudMat.SetFloat("_FromHeight", baseHeight);
        // }

        localPlayer = Networking.LocalPlayer;

        if (Map == null)
        {
            Debug.LogError("WARNING: NO MAP OBJECT IS SET!", this);
        }
        
        #if UNITY_EDITOR //Forces the OWML-player to be turned off due to null stuff. 
        allowPlayerOWML = false;
        #endif

        SAVs = new UdonSharpBehaviour[saccSyncList.Length];
        int xx = 0;
        foreach (SAV_SyncScript_OWML x in saccSyncList)
        {
            SAVs[xx] = x.SAVControl;
            xx = xx + 1;
        }
        
        // CDMs = new CollisionDetectionMode[saccSyncList.Length];
        // int xx2 = 0;
        // foreach (SAV_SyncScript_OWML x in saccSyncList)
        // {
        //     Rigidbody t = ((Rigidbody) x.SAVControl.GetProgramVariable("VehicleRigidBody"));
        //     if (t != null)
        //     {
        //         CDMs[xx2] = t.collisionDetectionMode;
        //     }
        //     xx2 = xx2 + 1;
        // }
        timer = (recheckInterval) / .9f;
        SetOWMLValue();
    }

    public void SetOWMLValue()
    {
        ChunkDistance = OWMLSlider.value;
        OWMLSliderText.text = OWMLSlider.value + "";
    }

    //Chunk update function - Used when entering a 'chunk' area. This will override every aircraft in this list to adjust according to your offset
    //since once you enter a chunk; you're moved backwards.
    public void doChunkUpdate(Vector3 mapCoords)
    {
        Debug.Log("Chunk update");
        foreach (var x in saccSyncList)
        {
            if (OWML!=null && OWML.SaccSync!=null &&  x == OWML.SaccSync && OWML.Piloting)
            {
                continue;
            }

            // x.L_LastPingAdjustedPosition = (x.L_LastPingAdjustedPosition - mapCoords);
            x.Extrapolation_Raw = (x.Extrapolation_Raw - mapCoords);
            x.L_PingAdjustedPosition = (x.L_PingAdjustedPosition - mapCoords);
        }
        
        foreach (var xstation in PlayerManager.Stations)
        {
            if (xstation.Player != null)
            {
                xstation.oldPos = xstation.CurrentPlayerPosition + Map.position;
                xstation.stationObject.transform.position = xstation.CurrentPlayerPosition + Map.position;
                Debug.Log("Updated Thingy: "+ xstation.PlayerID);
            }
        }
    }

    public void doKinematicChecks()
    {
        if (kIndex == SAVs.Length)
        {
            kIndex = 0;
        }
        
        Rigidbody xx = ((Rigidbody) SAVs[kIndex].GetProgramVariable("VehicleRigidbody"));
        
        if (xx!=null &&  Vector3.Distance(localPlayer.GetPosition(), xx.position) > DistanceKinematicCheck)
        {
            // Debug.Log("Is Kinematic set loop " + kIndex);
            SaccAirVehicle sav;
            if ((!(bool) SAVs[kIndex].GetProgramVariable("Piloting") ||
                 !(bool) SAVs[kIndex].GetProgramVariable("Passenger")))
            {
                if(!xx.isKinematic)
                {
                    xx.isKinematic = true;
                    Debug.Log("Kinematic set to:" + xx.isKinematic);
                }
            }
            else
            {
                if(xx.isKinematic)
                {
                    xx.isKinematic = false;
                    Debug.Log("Kinematic set to:" + xx.isKinematic);
                    // xx.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; // return to continuous dynamic
                }
            }
        }
        else if (xx!=null && Vector3.Distance(localPlayer.GetPosition(), xx.position) < DistanceKinematicCheck)
        {
            if(xx.isKinematic)
            {
                xx.isKinematic = false;
                Debug.Log("Kinematic set to:" + xx.isKinematic);
                // xx.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; // return to continuous dynamic
            }
        }

        kIndex = kIndex + 1;
    }
    public override void OnPlayerRespawn(VRCPlayerApi player)
    {
        if (player.isLocal)
        {
            if(Skybox!=null) { Skybox.SetFloat("_AtmosphereThickness", 1); }
        }
    }

    void Update()
    {
        offsetHeight = OWML!=null ? (-Map.position.y +  OWML.VehicleRigidBody.transform.position.y) * 3.28084f : (-Map.position.y + localPlayer.GetPosition().y) * 3.28084f ;

        if (initTimer > initializedTimer && !initialized)
        {
            initialized = true;
        }
        else
        {
            initTimer = initTimer + Time.deltaTime;
        }
        
        if (stationObject == null)
        {
            if (timer > recheckInterval && (!Networking.IsOwner(PlayerManager.gameObject)) ||
                ((Networking.IsOwner(PlayerManager.gameObject) && initialized)))
            {
                Debug.Log("Player has no Station yet after " + recheckInterval + "s. Re-sending request.");
                timer = 0f;
                PlayerManager.recheckPlayerIDs();
            }
            else
            {
                timer = timer + Time.deltaTime;
            }
        }

        if (CallResyncs && Networking.IsOwner(PlayerManager.gameObject))
        {
            resyncTimerProper = resyncTimerProper + Time.deltaTime;
            if (resyncTimerProper > resyncTimer)
            {
                Debug.Log("Resync Interval Call");
                resyncTimerProper = 0f;
                PlayerManager.resyncCall();
            }
        }

        // Useful for a moving skybox or if you need anything that needs to follow a player.
        if (PlayerFollowObject != null)
        {
            PlayerFollowObject.position = localPlayer.GetPosition();
        }
        
        if(DoKinematicCheck) doKinematicChecks();
        
        if (AltitudeAnimator != null)
        {
            AltitudeAnimator.SetFloat(AltitudeParameter, offsetHeight - MaximumAltitude);
        }

        if (Skybox != null)
        {
            if (OWML != null)
            {
                if (offsetHeight > AtmosphereDarkStart)
                {
                    Skybox.SetFloat("_AtmosphereThickness",
                        baseAtmos - ((offsetHeight - AtmosphereDarkStart) / AtmosphereDarkMax));
                }
                else
                {
                    Skybox.SetFloat("_AtmosphereThickness", 1);
                }
                
                // Code Block for clouds as a shader. You may add your own system for the clouds. 

                // if (CloudMat != null)
                // {
                //     CloudMat.SetVector("_OffsetXYZ", new Vector4(Offsets.x, baseHeight - Offsets.y, Offsets.z, 0));
                // }
            }

            if (OWML == null)
            {
                Skybox.SetFloat("_AtmosphereThickness",  baseAtmos - (( offsetHeight - AtmosphereDarkStart) / AtmosphereDarkMax));
            }
        }
    }
}