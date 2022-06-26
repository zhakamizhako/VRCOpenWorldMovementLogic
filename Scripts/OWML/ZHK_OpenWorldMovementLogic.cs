
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

// ZHK Open World Movement Logic
// For use with SaccFlight 1.5 Onwards
// Dependencies:
// ZHK_UIScript - Local Player Manager
// SAV_SyncScript - Modified Syncscript for SaccFlight
// Contact: Twitter: @zzhako / Discord: ZhakamiZhako#2147
[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
[DefaultExecutionOrder(-21)]
public class ZHK_OpenWorldMovementLogic : UdonSharpBehaviour
{
    [Tooltip("Required: The Aircraft's SAVController \n \n 必須 航空機のSAVController")]
    public UdonBehaviour EngineControl;
    
    [System.NonSerialized] public  Transform Map; 
    private Transform startAreaTransform;
    [System.NonSerializedAttribute] public Vector3 AnchorCoordsPosition = Vector3.zero;
    [System.NonSerializedAttribute] public Vector3 PosSync = Vector3.zero;
    // [System.NonSerializedAttribute] public VRCPlayerApi localPlayer;
    // private Quaternion startRot;
    private Vector3 startPos;
    [System.NonSerializedAttribute] public bool AlwaysActive = false;
    [Tooltip("Required: Your Scene's  ZHK_UIScript Gameobject. \n \n 必須 あなたのシーンのZHK_UIScript Gameobject。")]
    public ZHK_UIScript UIScript;
    [Tooltip("Required: Your Aircraft's Parent GameObject. \n \n 必須 自機の親GameObject。")]
    public Transform targetParent;
    [Tooltip("Required: Your Scene's Target Parent Object \n \n 必須 シーンのターゲット親オブジェクト")]
    public Transform originalParent;
    [System.NonSerializedAttribute] public bool moved = false;
    
    [Tooltip("Required: Vehicle's Sync Script. \n \n 必須 車両のSync Script。")]
    public SAV_SyncScript_OWML SaccSync;

    private SaccEntity Entity;
    private SaccAirVehicle SAV;
    [System.NonSerializedAttribute] public bool Piloting = false;
    [System.NonSerializedAttribute] public bool Passenger = false;
    
    [Tooltip("Required: Vehicle's Rigid Body \n \n 必須 車両のRigidBody ")]
    public Rigidbody VehicleRigidBody;
    public float respawnHeight = 1.8f;
    public void Start()
    {
        Entity = (SaccEntity)EngineControl.GetProgramVariable("EntityControl");
        GameObject xx = VRCInstantiate(UIScript.EmptyTransform.gameObject);
        startAreaTransform = xx.transform;
        Vector3 pos = Entity.transform.position;
        startAreaTransform.position = new Vector3(pos.x, pos.y + respawnHeight, pos.z);
        startAreaTransform.parent = originalParent;

        SAV = EngineControl.gameObject.GetComponent<SaccAirVehicle>();
        
            if (UIScript.Map != null)
            {
                Map = UIScript.Map;
            }
            else
            {
                Debug.LogError("[ZHK_OWML] MAP NOT FOUND. THIS SCRIPT WILL NOT WORK WITHOUT IT.", this);
                gameObject.SetActive(false);
            }
    }

    public void SFEXT_L_EntityStart()
    {
        gameObject.SetActive(true);
        Entity = (SaccEntity)EngineControl.GetProgramVariable("EntityControl");
        VehicleRigidBody = (Rigidbody)EngineControl.GetProgramVariable("VehicleRigidbody");

        // localPlayer = Networking.LocalPlayer;
        AnchorCoordsPosition = Entity.transform.position;
        startPos = Entity.transform.position;
        originalParent = Entity.transform.parent;
        PosSync = -Map.transform.position + AnchorCoordsPosition;

        SAV.UseAtmospherePositionOffset = UIScript.UseAtmosphere;
    }

    // public void SFEXT_O_RespawnButton()
    // {
    //     CallForRespawn();
    // }

    // public void SFEXT_G_ReAppear()
    // {
    //     CallForRespawn();
    // }

    public void SFEXT_O_MoveToSpawn()
    {
        CallForRespawn();
    }

    public void SFEXT_O_PilotEnter()
    {
        AnchorCoordsPosition = Entity.transform.position;
        PosSync = -Map.transform.position + AnchorCoordsPosition;
        startPos = Entity.transform.position;
        originalParent = Entity.transform.parent;

        Piloting = true;
        
        RequestSerialization();
        movePersonToOWML();
    }

    public void SFEXT_O_PilotExit()
    {
        Piloting = false;
        exitPersonOWML();
    }

    public void SFEXT_G_PilotExit()
    {
        if (AlwaysActive && UIScript.OWML==this)
        {
            UIScript.OWML = null;
            UIScript.PlayerAircraft = null;
            Entity.transform.SetParent(originalParent);
            
            exitPersonOWML();
        }
    }
    public void SFEXT_P_PassengerEnter()
    {
        Passenger = true;
        // walkingMode = false;
        movePersonToOWML();
    }

    public void SFEXT_P_PassengerExit()
    {
        Passenger = false;
        PassengerExit();
    }
    
    public void EnableScript()
    {
        if (Piloting)
        {
            AnchorCoordsPosition = Entity.transform.position;
            PosSync = -Map.transform.position + AnchorCoordsPosition;
            startPos = Entity.transform.position;
            originalParent = Entity.transform.parent;
            
        }
    }

    public void CallForRespawn()
    {
        AnchorCoordsPosition = startPos;
        bool Occupied = (bool) EngineControl.GetProgramVariable("Occupied");
        // bool dead = Entity.dead;
        if (Networking.IsOwner(gameObject) &&  !Occupied )
        {
            Entity.transform.position = startAreaTransform.position;
            SAV.AtmospherePositionOffset = 0f;
        }
    }
    
    void Update()
    {
        if (Entity == null)
        {
            Entity = (SaccEntity)EngineControl.GetProgramVariable("EntityControl");
        }
        if (EngineControl != null) MovementLogic();
    }

    public void movePersonToOWML()
    {
        if (UIScript != null && UIScript.PlayerAircraft == null)
        {
            UIScript.PlayerAircraft = EngineControl;
            UIScript.OWML = this;
        }
    }

    public void exitPersonOWML()
    {
        if (UIScript != null && UIScript.PlayerAircraft != null)
        {
            UIScript.PlayerAircraft = null;
            UIScript.OWML = null;
            
            Debug.Log("EXIIIIT");
            Entity.transform.SetParent(originalParent);
            moved = false;
        }
    }
    
    public void PassengerExit()
    {
        // if (/*AlwaysActive && */UIScript.PlayerAircraft.GetComponent<SaccAirVehicle>().Occupied)
        // {
        //     walkingMode = true;
        // }
        if (UIScript != null && UIScript.PlayerAircraft != null/* && !AlwaysActive*/)
        {
            Passenger = false;
            UIScript.PlayerAircraft = null;
            UIScript.OWML = null;
            
            Debug.Log("EXIIIIT");
            Entity.transform.SetParent(originalParent);
            moved = false;
        }
    }

    void MovementLogic()
    {
        if (Map !=null)
        {
            if (Networking.IsOwner(gameObject) && !Piloting)
            {
                // Needed to 'send' coordinates even if nobody is piloting. 
                PosSync = (-Map.transform.position + Entity.transform.position);
            }
            
            if (Piloting || Passenger /*|| AlwaysActive*/) // Always Active is not working properly at the moment.
            {
                if (!moved) // Transfer to the target parent
                {
                    targetParent.position = AnchorCoordsPosition;
                    Entity.transform.SetParent(targetParent);
                    moved = true;
                    UIScript.OWML = this;
                    UIScript.PlayerAircraft = EngineControl;
                }

                //Move by chunks
                var dist = Vector3.Distance(AnchorCoordsPosition, Entity.transform.position);
                if (dist > (UIScript.ChunkDistance) /*|| walkingMode*/){ //If more than set chunk distance
                    AnchorCoordsPosition = Entity.transform.position;
                    Entity.transform.position = Vector3.zero;
                    Map.transform.position = Map.position - AnchorCoordsPosition;
                    //Call chunk update to update every aircraft other than yours based on the offset when entering a chunk
                    UIScript.doChunkUpdate(AnchorCoordsPosition);
                }
                //Synchronize to SAV_SyncScript
                if(Piloting || (AlwaysActive && Networking.IsOwner(gameObject))) PosSync = (-Map.transform.position + Entity.transform.position);
                
                
                //Atmosphere binding
                if(UIScript.UseAtmosphere) SAV.AtmospherePositionOffset = (-Map.transform.position.y + Entity.transform.position.y);
            }
        }
    }
}

