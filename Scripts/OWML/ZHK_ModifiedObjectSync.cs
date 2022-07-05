
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class ZHK_ModifiedObjectSync : UdonSharpBehaviour
{
    [UdonSynced] public Vector3 syncPos;
    [UdonSynced] public Quaternion syncRot;
    
    // Will add the velocity later.
    // [UdonSynced] public Vector3 velocity;
    
    public bool useLocal = true;
    public float updaterate = 0.2f;
    private float updateTimer = 0;
    private Rigidbody self;
    private bool startWithKinematic = false;

    public void Start()
    {
        self = gameObject.GetComponent<Rigidbody>();
        if (self!=null && self.isKinematic) startWithKinematic = true;
    }
    public void Update()
    {
        if (Networking.IsOwner(gameObject))
        {
            UpdateOwner();
        }
        else
        {
            UpdateNotOwner();
        }
    }

    public void UpdateOwner()
    {
        if (self!=null && !startWithKinematic && self.isKinematic) self.isKinematic = false; // 
                
        if (updateTimer > updaterate)
        {
            if (useLocal)
            {
                syncPos = transform.localPosition;
                syncRot = transform.localRotation;   
            }
            else
            {
                syncPos = transform.position;
                syncRot = transform.rotation;
            }
            RequestSerialization();
        }
        else
        {
            updateTimer = updateTimer + Time.deltaTime;
        }
    }

    public void UpdateNotOwner()
    {
        if (self!=null) self.isKinematic = true; // set object to iskinematic when you're not the owner

        Vector3 oldPos;
        Quaternion oldRot;
        
        if (useLocal)
        {
            oldPos = transform.localPosition;
            oldRot = transform.localRotation;
        }
        else
        {
            oldPos = transform.position;
            oldRot = transform.localRotation;
        }
        
            if (useLocal)
            {
                            
                transform.localPosition = Vector3.Lerp(oldPos, syncPos, Time.deltaTime);
                transform.localRotation = Quaternion.Lerp(oldRot, syncRot, Time.deltaTime);
            }
            else
            {
                transform.position = Vector3.Lerp(oldPos, syncPos, Time.deltaTime);
                transform.rotation = Quaternion.Lerp(oldRot, syncRot, Time.deltaTime);                
            }

    }
}

