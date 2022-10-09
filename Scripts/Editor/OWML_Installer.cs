
using UnityEngine;
using System.Linq;
using UnityEditor;
using UdonSharp;
using UdonSharpEditor;
using UnityEngine.SceneManagement;
using SaccFlightAndVehicles;
using VRC.SDK3.Components;
using VRC.Udon;

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

    private Vector2 vehicleScrollPosition;

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
        EditorGUILayout.LabelField("sence set up");
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Step 2:Use \"place prefab\" to add the OWML prefab to the scence");
        EditorGUILayout.LabelField("use \"place objects\" to  move objects to 'mapObject' transform");
        EditorGUILayout.LabelField("if the prefab has already been placed, go step 3");

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button(new GUIContent("place prefab"), EditorStyles.miniButtonLeft, miniButtonLayout))
            {
                PlaceOWMLPrefab();
            }
            if (GUILayout.Button(new GUIContent("place objects"), EditorStyles.miniButtonLeft, miniButtonLayout))
            {
                PlaceWorldObjects();
            }
        }
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Step 3:After place prefab, the fields below should be filled, if not, try find above");
        UIScript = (ZHK_UIScript)EditorGUILayout.ObjectField("UI Script", UIScript, typeof(ZHK_UIScript), true);
        targetParents = (Transform)EditorGUILayout.ObjectField("target parents", targetParents, typeof(Transform), true);
        mapObject = (Transform)EditorGUILayout.ObjectField("map object", mapObject, typeof(Transform), true);
        playerRespawnHandler = (Transform)EditorGUILayout.ObjectField("player respawn handler", playerRespawnHandler, typeof(Transform), true);

        if (GUILayout.Button(new GUIContent("find above"), EditorStyles.miniButtonLeft, normalButtonLayout))
        {
            UpdateOWMLVariables();
        }
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
                            if (GUILayout.Button(new GUIContent("set scripts"), EditorStyles.miniButtonLeft, miniButtonLayout))
                                ModifyPlane(each.gameObject);//参数：" sacc entity"所在的对象
                            if (GUILayout.Button(new GUIContent("set particles"), EditorStyles.miniButtonLeft, miniButtonLayout))
                                ModifyParticlePlane(each.gameObject);//参数：" sacc entity"所在的对象
                            if (GUILayout.Button(new GUIContent("set weapons"), EditorStyles.miniButtonLeft, miniButtonLayout))
                                ModifyWeapon(each.gameObject);//参数：" sacc entity"所在的对象
                        }
                    }
                }
        }
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Step 5: \"update UIscript\", this will sync all OWMLSync in scence to UIScript.");
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
        if (GUILayout.Button(new GUIContent("set cam rander dst"), EditorStyles.miniButtonLeft, normalButtonLayout))
        {
            var cameras = GameObject.FindObjectOfType<Camera>();
            cameras.farClipPlane = 50000;
        }
        #endregion

    }

    private void ModifyPlane(GameObject vehicleObject)
    {
        //修改飞机的参数以适配owml
        //参数："Vehicle Main"所在的对象

        //set OWMLScript
        var OWMLScriptObject = new GameObject("OWMLScript");
        OWMLScriptObject.transform.SetParent(vehicleObject.transform, false);
        var OWMLComponent = OWMLScriptObject.AddComponent<ZHK_OpenWorldMovementLogic>();
        
        var SAVControl = vehicleObject.GetComponentInChildren<SaccAirVehicle>();
        SAVControl.RepeatingWorld = false;
        //type of SAVControl should be u# behaviour, but ZHK_OWML.cs requires a udon behaviour? 
        //here is a workaround
        //var syncScriptComponent = vehicleObject.GetComponentInChildren<SAV_SyncScript>();
        //OWMLComponent.EngineControl = syncScriptComponent.SAVControl as UdonBehaviour;
        //emmmmm... not work

        //OWMLComponent.EngineControl = SAVControl.gameObject;
        OWMLComponent.UIScript = UIScript;
        OWMLComponent.targetParent = targetParents;
        OWMLComponent.originalParent = vehicleObject.transform.parent;
        OWMLComponent.VehicleRigidBody = vehicleObject.GetComponent<Rigidbody>();

        //set SAV_SyncScript_OWML
        var OWMLSyncScriptObject = new GameObject("SyncScript_OWML");
        OWMLSyncScriptObject.transform.SetParent(vehicleObject.transform, false);
        var OWMLSyncScriptComponent = OWMLSyncScriptObject.AddComponent<SAV_SyncScript_OWML>();
        OWMLSyncScriptComponent.OWML = OWMLComponent;
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
        UdonSharpBehaviour[] ExtensionUdonBehaviours = new UdonSharpBehaviour[entityControl.ExtensionUdonBehaviours.Length + 1];
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
        ExtensionUdonBehaviours[entityControl.ExtensionUdonBehaviours.Length] = OWMLComponent;
        entityControl.ExtensionUdonBehaviours = ExtensionUdonBehaviours;

        //Configuring HUDController
        var HUDController = vehicleObject.GetComponentInChildren<SAV_HUDController>(true);
        if (HUDController != null)
        {
            var OWMLHudObject = new GameObject("HUDController_OWML");
            OWMLHudObject.transform.SetParent(HUDController.transform.parent, false);
            var OWMLHudController = OWMLHudObject.AddComponent<SAV_HUDController_OWML>();
            OWMLHudController.HBOld = HUDController;
            OWMLHudController.OWML = OWMLComponent;    
        }

        return;
    }

    private void ModifyParticlePlane(GameObject vehicleObject)
    {
        //修改plann entity/effect control/ 非attached 下的所有粒子
        //创建粒子列表
        var particles = vehicleObject.GetComponentsInChildren<ParticleSystem>(true)
            .Where(o => o.transform.parent.name != "AttachedEffects");
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
            particleSystemMain.simulationSpace = ParticleSystemSimulationSpace.Custom;
            particleSystemMain.customSimulationSpace = mapObject;
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
        playerRespawnHandler = GameObject.FindObjectOfType<ZHK_PlayerRespawnHandler>().transform;
    }
}
