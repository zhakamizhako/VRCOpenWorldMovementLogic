using UnityEngine;
using System.Linq;
using UnityEditor;
using UdonSharp;
using UdonSharpEditor;
using UnityEngine.SceneManagement;
using SaccFlightAndVehicles;
using VRC.SDK3.Components;
using VRC.Udon;

#if UNITY_EDITOR
public class OWML_Installer : EditorWindow
{
    private readonly BuildTargetGroup[] buildTargetGroups = 
        {
            BuildTargetGroup.Standalone,
            BuildTargetGroup.Android,
        };
    private readonly GUILayoutOption[] miniButtonLayout = 
        {
            GUILayout.ExpandWidth(false),
            GUILayout.Width(100),
        };
    private readonly GUILayoutOption[] normalButtonLayout =
        {
            GUILayout.ExpandWidth(false),
            GUILayout.Width(200),
        };


    public GameObject OWMLPrefab;

    public ZHK_UIScript UIScript;
    public Transform targetParents;
    public Transform mapObject;
    public Transform playerRespawnHandler;
    public FFRDEBUGSCRIPT Debugger;

    private Vector2 vehicleScrollPosition;

    bool enforceCustomSpace;
    bool enforceLocalSpace;

    [MenuItem("SaccFlight/Open World Movement Logic")]
    public static void ShowWindow()
    {
        var window = GetWindow<OWML_Installer>();
        window.titleContent = new GUIContent("OWML Installer");
        window.minSize = new Vector2(520, 620);
        window.Show();
        
    }

    private void OnEnable()
    {
        titleContent = new GUIContent("OWML Installer for SaccFlight");
        
        UpdateOWMLVariables(); // auto get
    }
    //checkList for OWML
    /*
     no static batching
    dont bring qvpen cross a chunk
    uncheck repeating world
    */
    //OWML setupp
    /*
     add prefab
    move all map object
    for each plane
    move & config all plane

     */
    private void OnGUI()
    {
        var scene = SceneManager.GetActiveScene();
        //为了省事，这里选择的是所有包含SaccEntity的组件，然而例如防空导弹等物体也会包含SaccEntity，可能需要处理？
        /*
        EditorGUILayout.HelpBox("Check list before install OWML:\n" +
            "1.Backup the scene. \n" +
            "2.For vehicles in scence:\n" +
            "   There is a SyncScript in Sacc Entity's child)\n" +
            "   ... \n" +
            "3.VRC Scence Discriptor and Reference Camera are at the root of the scence",MessageType.Info);
        */
        EditorGUILayout.LabelField("Step 1: Place the OWMLPrefab here, the prefab is in Assets/FFR/OWML");
        

        //you can modify it for your own porpose(like remove SaccFlightAccessories if there is already one in your scence)
        OWMLPrefab = EditorGUILayout.ObjectField("OWMLPrefab", OWMLPrefab, typeof(GameObject), true) as GameObject;
        //修改场景
        #region SENCE_SET_UP
        EditorGUILayout.LabelField("Scene setup");
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Step 2:Use \"place prefab\" to add the OWML prefab to the scene");
        EditorGUILayout.LabelField("use \"place objects\" to  move objects to 'mapObject' transform");
        EditorGUILayout.LabelField("if the prefab has already been placed, go step 3");

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button(new GUIContent("Place prefab"), EditorStyles.miniButtonLeft, miniButtonLayout))
            {
                PlaceOWMLPrefab();
            }
            if (GUILayout.Button(new GUIContent("Place objects"), EditorStyles.miniButtonLeft, miniButtonLayout))
            {
                PlaceWorldObjects();
            }
        }
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Step 3:After place prefab, the fields below should be filled, if not, try find above");
        UIScript = (ZHK_UIScript)EditorGUILayout.ObjectField("UI Script", UIScript, typeof(ZHK_UIScript), true);
        targetParents = (Transform)EditorGUILayout.ObjectField("Target parents", targetParents, typeof(Transform), true);
        mapObject = (Transform)EditorGUILayout.ObjectField("map object", mapObject, typeof(Transform), true);
        playerRespawnHandler = (Transform)EditorGUILayout.ObjectField("player respawn handler", playerRespawnHandler, typeof(Transform), true);
        
        if (GUILayout.Button(new GUIContent("Find above"), EditorStyles.miniButtonLeft, normalButtonLayout))
        { 
            UpdateOWMLVariables();
        }
        
        EditorGUILayout.LabelField("Map Object should be the parent for your entire map that involves being moved around.");
        EditorGUILayout.LabelField("(Terrains, Airports, planes, etc.)");
        
        /*
        EditorGUILayout.HelpBox("Check list after set scene:\n" +
            "1.Every vehicle, map objects, and anything that has to be involved in the 'mapObject' are placed \n",MessageType.Info);
        */
        #endregion

        //创建所有载具的列表
        #region VEHICLE_SET_UP
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Step 4: ONLY for each plane below");
        EditorGUILayout.LabelField("\"set scripts\", then assign Engine Control with corresponding SaccAirVehicle Object");
        EditorGUILayout.LabelField("\"set particles\", then \"set weapons\"");
        EditorGUILayout.LabelField("Note: This will replace every particle system that is outside EffectsController/AttachedEffects as Custom Space.");
        EditorGUILayout.LabelField("Weapons: This will only affect AAM, AGM, Rockets, and Bomb. Custom weapons have to be manually setup.");
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Particle Replacement Options");
        enforceLocalSpace = EditorGUILayout.Toggle("Local Space Particles", enforceLocalSpace);
        enforceCustomSpace = EditorGUILayout.Toggle("Custom Space Particles", enforceCustomSpace);

        var vehicleList = scene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<SaccEntity>());
        using (var scrollScope = new EditorGUILayout.ScrollViewScope(vehicleScrollPosition))
        {
            vehicleScrollPosition = scrollScope.scrollPosition;
                foreach (var each in vehicleList)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        using (new EditorGUI.DisabledGroupScope(true))
                        {
                            EditorGUILayout.ObjectField(each.gameObject, typeof(GameObject), true);
                        }
                        /*
                        using (new EditorGUI.DisabledGroupScope(true))//这里判断条件改为飞机是否移动进了plane(GameObject)
                        {
                            if (GUILayout.Button(new GUIContent("place vehicle"), EditorStyles.miniButtonLeft, miniButtonLayout)) 
                                ModifyPlane(each.gameObject);
                        }
                        */
                        using (new EditorGUI.DisabledGroupScope(false))//这里判断条件改为飞机是否位于正确的目录
                        {
                            if (GUILayout.Button(new GUIContent("Set Scripts"), EditorStyles.miniButtonLeft, miniButtonLayout))
                                ModifyPlane(each.gameObject);//参数：" sacc entity"所在的对象
                            if (GUILayout.Button(new GUIContent("Set Particles"), EditorStyles.miniButtonLeft, miniButtonLayout))
                                ModifyParticlePlane(each.gameObject);//参数：" sacc entity"所在的对象
                            if (GUILayout.Button(new GUIContent("Set Weapons"), EditorStyles.miniButtonLeft, miniButtonLayout))
                                ModifyWeapon(each.gameObject);//参数：" sacc entity"所在的对象
                        }
                    }
                }
        }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(new GUIContent("Update All Scripts"), EditorStyles.miniButtonLeft, normalButtonLayout))
                {
                    foreach (var x in vehicleList)
                    {
                        ModifyPlane(x.gameObject);
                    }
                }
                if (GUILayout.Button(new GUIContent("Update All Particles"), EditorStyles.miniButtonLeft, normalButtonLayout))
                {
                    foreach (var x in vehicleList)
                    {
                        ModifyParticlePlane(x.gameObject);
                    }
                }
                if (GUILayout.Button(new GUIContent("Update All Weapons"), EditorStyles.miniButtonLeft, normalButtonLayout))
                {
                    foreach (var x in vehicleList)
                    {
                        ModifyWeapon(x.gameObject);
                    }
                }
            }


        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Step 5: \"update UIscript\", this will sync all OWMLSync in scene to UIScript.");
        if (GUILayout.Button(new GUIContent("update UIscript"), EditorStyles.miniButtonLeft, normalButtonLayout))
        {
            //TODO: search doesn't work, apply using proxy
            var OWMLSyncInScence = scene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<SAV_SyncScript_OWML>());
            UIScript.saccSyncList = OWMLSyncInScence.ToArray();
        }
        /*
        EditorGUILayout.HelpBox("Check list after set vehicles:\n" +
            "1. OWMLScript Engine control has been assigned" +
            "2. OWMLScript and SyncScript_OWML have beeen created, fields have been assigned correctly \n" +
            "3. Original SyncScript has been disactived\n" +
            "4. Following scripts can be found in Sacc entity/Udon Extension Behaviours\n" +
            "a.SyncScript_OWML\n" +
            "b.OWMLScript", MessageType.Info);
        */
        #endregion

        #region utility
        //其他功能
        //启用所有静态物件
        //重设重生位置
        //设置参考摄像机
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Step 6: Other fuctions might be needed");
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("utilities");
        /*No idea how to implent yet
        if (GUILayout.Button(new GUIContent("disables all Static"), EditorStyles.miniButtonLeft, normalButtonLayout))
        {
            foreach (var obj in scene.GetRootGameObjects())
            {
                obj.isStatic = false;   
            }
            
        }
        */
        if (GUILayout.Button(new GUIContent("disables all Static"), EditorStyles.miniButtonLeft, normalButtonLayout))
        {
            int x = 0;
            foreach (GameObject obj in GameObject.FindObjectsOfType(typeof(GameObject)))
            {
                obj.isStatic = false;
                x = x + 1;
            }
            
            Debug.Log("is Static false on "+x+" Objects");
            
        }
        
        
        
        if (GUILayout.Button(new GUIContent("set respawn position"), EditorStyles.miniButtonLeft, normalButtonLayout))
        {
            var sceneDescriptor = scene.GetRootGameObjects().Select(o => o.GetComponent<VRCSceneDescriptor>()).FirstOrDefault(a => a != null);
            if (sceneDescriptor != null)
            {
                sceneDescriptor.RespawnHeightY = -9999999f;
                //if (sceneDescriptor.spawns[1] != null)
                    //playerRespawnHandler.parent = sceneDescriptor.spawns[1];
                sceneDescriptor.spawns = new Transform[] { playerRespawnHandler };
            }
        }
        if (GUILayout.Button(new GUIContent("set cam render distance"), EditorStyles.miniButtonLeft, normalButtonLayout))
        {
            var cameras = GameObject.FindObjectOfType<Camera>();
            cameras.farClipPlane = 900000;
            cameras.nearClipPlane = 0.1f;
        }
        #endregion
         
        EditorGUILayout.LabelField("UI Installer by 西改改Yuxi TW: @YUXI917", EditorStyles.boldLabel);

    }

    private void ModifyPlane(GameObject vehicleObject)
    {
        //修改飞机的参数以适配owml
        //参数："Vehicle Main"所在的对象

        //set OWMLScript

        var SAVControl = vehicleObject.GetComponentInChildren<SaccAirVehicle>();
        if(SAVControl!=null) SAVControl.RepeatingWorld = false; //Added a checker if not SAVControl based vehicle
        var SGVControl = vehicleObject.GetComponentInChildren<SaccGroundVehicle>();
        var SSVControl = vehicleObject.GetComponentInChildren<SaccSeaVehicle>();

        //Add check if OWMLComponent is already present or not.
        GameObject OWMLScriptObject;
        ZHK_OpenWorldMovementLogic OWMLComponent = vehicleObject.GetComponentInChildren<ZHK_OpenWorldMovementLogic>();
        if (vehicleObject.GetComponentInChildren<ZHK_OpenWorldMovementLogic>() != null)
        {
            OWMLScriptObject = OWMLComponent.gameObject; //Can be commented; or used as a reference later.
        }else
        {
            OWMLScriptObject = new GameObject("OWMLScript");
            OWMLScriptObject.transform.SetParent(vehicleObject.transform, false);
            OWMLComponent = OWMLScriptObject.AddComponent<ZHK_OpenWorldMovementLogic>();
            
        }

        if (SAVControl!=null) OWMLComponent.EngineControl = SAVControl.gameObject.GetComponent<UdonBehaviour>(); // If Aircraft
        if (SGVControl!=null) OWMLComponent.EngineControl = SGVControl.gameObject.GetComponent<UdonBehaviour>(); // If Ground Vehicle
        if (SSVControl != null) OWMLComponent.EngineControl = SSVControl.gameObject.GetComponent<UdonBehaviour>(); // If SaccSeaVehicle
        
        OWMLComponent.UIScript = UIScript;
        OWMLComponent.targetParent = targetParents;
        OWMLComponent.originalParent = vehicleObject.transform.parent;
        OWMLComponent.VehicleRigidBody = vehicleObject.GetComponent<Rigidbody>();

        //set SAV_SyncScript_OWML
        GameObject OWMLSyncScriptObject;
        SAV_SyncScript_OWML OWMLSyncScriptComponent = vehicleObject.GetComponentInChildren<SAV_SyncScript_OWML>();
        if (vehicleObject.GetComponentInChildren<SAV_SyncScript_OWML>() != null)
        {
            OWMLScriptObject = OWMLSyncScriptComponent.gameObject; //Can be commented; or used as a reference later.
        }
        else
        {
            OWMLSyncScriptObject = new GameObject("SyncScript_OWML");
            OWMLSyncScriptObject.transform.SetParent(vehicleObject.transform, false);
            OWMLSyncScriptComponent = OWMLSyncScriptObject.AddComponent<SAV_SyncScript_OWML>();
            
        }

        OWMLSyncScriptComponent.OWML = OWMLComponent;
        // OWMLSyncScriptComponent.SAVControl =
        //     (SAVControl != null ? SAVControl : 
        //         ((UdonSharpBehaviour)SGVControl!=null ? (UdonSharpBehaviour) SGVControl : (SSVControl!=null ? (UdonSharpBehaviour)SSVControl : null) ));
        //
        
        if (SAVControl != null)
        {
            OWMLSyncScriptComponent.SAVControl = SAVControl;
        }

        if (SGVControl != null)
        {
            OWMLSyncScriptComponent.SAVControl =  SGVControl;
        }

        if (SSVControl != null)
        {
            OWMLSyncScriptComponent.SAVControl = SSVControl;
        }
        
        var syncScriptFound = vehicleObject.GetComponentInChildren<SAV_SyncScript>();
        if (syncScriptFound != null)
        {
            //TODO:复制其他来自旧脚本的参数
            OWMLSyncScriptComponent.SAVControl = syncScriptFound.SAVControl;
            syncScriptFound.gameObject.SetActive(false);
        }

        //update OWMLScript
        OWMLComponent.SaccSync = OWMLSyncScriptComponent;

        //modifiy script from the list of Udon Extension Behaviours
        //替换 SyncScript 为 SyncScript_OWML（如果载具没有SyncScript， 可能发生问题）
        //添加 OWMLScript
        var entityControl = vehicleObject.GetComponent<SaccEntity>();

        bool foundOWMLSyncScript = false;
        bool foundOWML = false;
        
        foreach (UdonSharpBehaviour x in entityControl.ExtensionUdonBehaviours)
        {
            if (x == OWMLSyncScriptComponent) foundOWMLSyncScript = true;
            if (x == OWMLComponent) foundOWML = true;
        }

        UdonSharpBehaviour[] ExtensionUdonBehaviours = new UdonSharpBehaviour[entityControl.ExtensionUdonBehaviours.Length + (!foundOWML ? 1 : 0)];
        int idx = 0;
        foreach (var each in entityControl.ExtensionUdonBehaviours)
        {
            if (each.name == "SyncScript")
            {
                ExtensionUdonBehaviours[idx] = OWMLSyncScriptComponent;
            }
            else
            { 
            ExtensionUdonBehaviours[idx] = each;
            }
            idx += 1;
        }
        if(!foundOWML) ExtensionUdonBehaviours[entityControl.ExtensionUdonBehaviours.Length] = OWMLComponent;
        entityControl.ExtensionUdonBehaviours = ExtensionUdonBehaviours;

        //Configuring HUDController
        var HUDController = vehicleObject.GetComponentInChildren<SAV_HUDController>(true);
        var HUDController_OWML = vehicleObject.GetComponentInChildren<SAV_HUDController_OWML>(true);
        if (HUDController != null && HUDController_OWML==null)
        {
            var OWMLHudObject = new GameObject("HUDController_OWML");
            OWMLHudObject.transform.SetParent(HUDController.transform.parent, false);
            var OWMLHudController = OWMLHudObject.AddComponent<SAV_HUDController_OWML>();
            OWMLHudController.HBOld = HUDController;
            OWMLHudController.OWML = OWMLComponent;
            HUDController.gameObject.GetComponent<UdonSharpBehaviour>().enabled = false;
        }
        else if (HUDController != null && HUDController_OWML != null)
        {
            HUDController_OWML.HBOld = HUDController;
            HUDController.gameObject.GetComponent<UdonSharpBehaviour>().enabled = false;
            HUDController_OWML.OWML = OWMLComponent;
        }

        return;
    }

    private void ModifyParticlePlane(GameObject vehicleObject)
    {
        //修改plann entity/effect control/ 非attached 下的所有粒子
        //创建粒子列表
        var particles = vehicleObject.GetComponentsInChildren<ParticleSystem>(true);
            // .Where(o => o.transform.parent.name != "AttachedEffects");
        foreach (var each in particles)
        {
            ModifyParticle(each);
        }
        Debug.Log(string.Format("{0} particle systems modified for {1}", particles.ToArray().Length, vehicleObject.name));
    }
    
    private void ModifyParticle(ParticleSystem particle)
    {
        //修改粒子的参数以适配owml
        if (mapObject != null)
        { 
            var particleSystemMain = particle.main;

            if ((particleSystemMain.simulationSpace == ParticleSystemSimulationSpace.Local && enforceLocalSpace) ||
                (particleSystemMain.simulationSpace == ParticleSystemSimulationSpace.Custom && enforceCustomSpace) ||
                particleSystemMain.simulationSpace == ParticleSystemSimulationSpace.World)
            {
                particleSystemMain.simulationSpace = ParticleSystemSimulationSpace.Custom;
                particleSystemMain.customSimulationSpace = mapObject;
                
                Debug.Log("Modifying Particle: "+particle.gameObject.name, particle.gameObject); //Added debug log
            }
            else
            {
                Debug.Log("Simulation Space for "+particle.gameObject.name+" not 'World'. Skipping", this);
            }
        }
        else
        {
            Debug.LogError("set mapObject first");
        }
        return;
    }

    private void ModifyWeapon(GameObject vehicleObject)
    {
        //TODO:无法定位非原生的武器
        var AAMs = vehicleObject.GetComponentsInChildren<DFUNC_AAM>(true);
        var AGMs = vehicleObject.GetComponentsInChildren<DFUNC_AGM>(true);
        var bombs = vehicleObject.GetComponentsInChildren<DFUNC_Bomb>(true);
        var rockets = vehicleObject.GetComponentsInChildren<DFUNCP_Rockets>(true);
        foreach (var i in AAMs)
        {
            i.WorldParent = mapObject.transform;
        }
        foreach (var i in AGMs)
        {
            i.WorldParent = mapObject.transform;
        }
        foreach (var i in AGMs)
        {
            i.WorldParent = mapObject.transform;
        }
        foreach (var i in rockets)
        {
            i.WorldParent = mapObject.transform;
        }
        //Added debugger
        Debug.Log(string.Format("{0} AAM, {1} AGM, {2} Bomb, {3} rocket modified", AAMs.Length, AGMs.Length, bombs.Length, rockets.Length));
    }
    
    private void PlaceOWMLPrefab()
    {
        //查找场景中是否已经有预制件,如果没有，进行一个创建
        var found = SceneManager.GetActiveScene().GetRootGameObjects().Select(o => o.GetComponent<ZHK_UIScript>()).FirstOrDefault(a => a != null);
        if (found != null)
        {
            Debug.Log("OWMLPrefab existed");
            UpdateOWMLVariables();
            return;
        }
        else
        {
            //放置一个新的预制件，设置位置
            if (OWMLPrefab != null)
            {
                GameObject OWMLObject = PrefabUtility.InstantiatePrefab(OWMLPrefab) as GameObject;
                OWMLObject.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0, 0, 0));
                PrefabUtility.UnpackPrefabInstance(OWMLObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                OWMLObject.transform.DetachChildren();
                //Undo.RegisterCreatedObjectUndo(OWMLObject, "Install OWML"); // do i need to regist?
                //解压预制件，取出其中对象
                DestroyImmediate(OWMLObject);
                Debug.Log("Prefab has been placed!");
            }
            else
            {
                Debug.LogError("set OWML prefab first");
            }
        }
        //return UdonSharpEditorUtility.GetProxyBehaviour(udon) as UdonRadioCommunication;
        UpdateOWMLVariables();
        return;
    }

    private void PlaceWorldObjects()
    {
        //移动场景中的各个对象到指定位置
        //大部分元素移动到MapObjecte 除了以下内容
        /*
         * Main Camera
         * Directional Light
         * EventSystem
         * Scene Descriptor
         * 包含sacc entity 的载具
         */
        if (mapObject != null)
        {
            var scence = SceneManager.GetActiveScene();
            var rootObject = scence.GetRootGameObjects();
            foreach (var o in rootObject)
            {
                if (IsMapObject(o))
                {
                    o.transform.SetParent(mapObject.transform, true);
                }
            }
        }
        else
        {
            Debug.LogError("set mapObject first");
        }
    }
   
    private bool IsMapObject(GameObject obj)
    {
        //检查是否为
        /*
         * Main Camera
         * Directional Light
         * EventSystem
         * Scene Descriptor
         * 包含sacc entity 的载具
         */
        //否 则返回真
        string objectName = obj.name;
        string[] keyNames= { "Main Camera", "Directional Light", "EventSystem", "Scene Descriptor", "UIObject",
        "SpawnArea", "PlayerParent", "MapObject","VRCWorld","UdonRadioCommunication"};
        foreach (var keyName in keyNames)
        { 
            if (objectName.Contains(keyName))
            //考虑到可能有"PlayerParent (1)"之类的命名
            {
                //Debug.Log(objectName + " False");
                return false;
            }
        }
        if(obj.GetComponent<Camera>() != null)
        {
            return false;
        }
        //Debug.Log(objectName + " True");
        return true; 
    }

    public void UpdateOWMLVariables()
    { 
        //update UIScript, Targrt Parent etc components after set/find OWML prefab
        UIScript = GameObject.Find("/UIObject").GetComponent<ZHK_UIScript>();
        targetParents = GameObject.Find("/PlayerParent").transform;
        mapObject = GameObject.Find("/MapObject").transform;
        playerRespawnHandler = FindObjectOfType<ZHK_PlayerRespawnHandler>().transform;
        Debugger = (Resources.FindObjectsOfTypeAll<FFRDEBUGSCRIPT>())[0];
        UIScript.Debugger = Debugger.gameObject;
        ZHK_OWML_Player StationController = (Resources.FindObjectsOfTypeAll<ZHK_OWML_Player>())[0];
        Debugger.Stations = StationController.Stations;
        Debugger.UIScript = UIScript;
    }
}

#endif
