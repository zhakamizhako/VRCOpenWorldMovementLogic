
using UnityEngine;
using System.Linq;
using UnityEditor;
using UdonSharp;
using UdonSharpEditor;
using UnityEngine.SceneManagement;
using SaccFlightAndVehicles;
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
            GUILayout.Width(200),
        };

    
    public GameObject OWMLPrefab;

    public ZHK_UIScript UIScript;
    public Transform targetParents;
    public Transform mapObject;

    private Vector2 vehicleScrollPosition;
    private Vector2 particleScrollPosition;
    [MenuItem("SaccFlight/Open World Movement Logic")]
    public static void ShowWindow()
    {
        var window = GetWindow<OWML_Installer>();
        window.titleContent = new GUIContent("OWML Installer");
        window.minSize = new Vector2(520, 520);
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
        OWMLPrefab = EditorGUILayout.ObjectField("OWMLPrefab", OWMLPrefab, typeof(GameObject), true) as GameObject;
        //修改场景
        #region SENCE_SET_UP
        EditorGUILayout.LabelField("sence set up");
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
        UIScript = (ZHK_UIScript)EditorGUILayout.ObjectField(UIScript, typeof(ZHK_UIScript), true);
        targetParents = (Transform)EditorGUILayout.ObjectField(targetParents, typeof(Transform), true);
        mapObject = (Transform)EditorGUILayout.ObjectField(mapObject, typeof(Transform), true);
        if (GUILayout.Button(new GUIContent("find above"), EditorStyles.miniButtonLeft, miniButtonLayout))
        {
            UpdateOWMLVariables();
        }
        #endregion

        //创建所有载具的列表
        #region VEHICLE_SET_UP
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("vehicle set up");
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
                            if (GUILayout.Button(new GUIContent("modify scripts"), EditorStyles.miniButtonLeft, miniButtonLayout))
                                ModifyPlane(each.gameObject);//参数：" sacc entity"所在的对象
                        }
                    }
                }
        }
        if (GUILayout.Button(new GUIContent("sync vehicles to UI script"), EditorStyles.miniButtonLeft, miniButtonLayout))
        {
            //TODO: search doesn't work, apply using proxy
            var OWMLSyncInScence = scene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<SAV_SyncScript_OWML>());
            UIScript.saccSyncList = OWMLSyncInScence.ToArray();
        }
        #endregion

        //创建所有粒子的列表
        #region PARTICLE_SET_UP
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("particle set up");
        //Not all particle is needed to modified, how to judge?
        var particleList = scene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<ParticleSystem>(true));
        /*
         //debug only :绘制所有粒子列表
        using (var scrollScope = new EditorGUILayout.ScrollViewScope(particleScrollPosition))
        {
            particleScrollPosition = scrollScope.scrollPosition;
            using (new EditorGUI.DisabledGroupScope(true))//这里不做判断，随时都可以修改所有粒子
            {
                foreach (var each in particleList)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.ObjectField(each.gameObject, typeof(GameObject), true);
                    }
                }
            }
        }
        */
        if (GUILayout.Button(new GUIContent("modify particle"), EditorStyles.miniButtonLeft, miniButtonLayout))
        {
            foreach (var each in particleList)
                ModifyParticle(each.gameObject);
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
        //type of SAVControl should be u# behaviour, but ZHK_OWML.cs reuqests a udon behaviour? 
        //OWMLComponent.EngineControl = SAVControl;
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
        //TODO: apply using proxy
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

        //config weapon
        ModifyWespon(vehicleObject);

        return;
    }
    private void ModifyParticle(GameObject particleObject)
    {
        //修改粒子的参数以适配owml
        var particle = particleObject.GetComponent<ParticleSystem>();
        var particleSystemMain = particle.main;
        particleSystemMain.simulationSpace = ParticleSystemSimulationSpace.Custom;
        particleSystemMain.customSimulationSpace = mapObject;

        return;
    }

    private void ModifyWespon(GameObject vehicleObject)
    {
        var AAMs = vehicleObject.GetComponentsInChildren<DFUNC_AAM>();
        foreach (var AAM in AAMs)
        {
            AAM.WorldParent = mapObject;
        }
        var AGMs = vehicleObject.GetComponentsInChildren<DFUNC_AGM>();
        foreach (var AGM in AGMs)
        {
            AGM.WorldParent = mapObject;
        }
        var Bombs = vehicleObject.GetComponentsInChildren<DFUNC_Bomb>();
        foreach (var Bomb in Bombs)
        {
            Bomb.WorldParent = mapObject;
        }
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
            GameObject OWMLObject = PrefabUtility.InstantiatePrefab(OWMLPrefab) as GameObject;
            OWMLObject.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0, 0, 0));
            PrefabUtility.UnpackPrefabInstance(OWMLObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            OWMLObject.transform.DetachChildren();
            //Undo.RegisterCreatedObjectUndo(OWMLObject, "Install OWML"); // do i need to regist?
            //解压预制件，取出其中对象
            DestroyImmediate(OWMLObject);
            Debug.Log("Prefab has been placed!");
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
        var scence = SceneManager.GetActiveScene();
        var rootObject = scence.GetRootGameObjects();
        var mapObject = GameObject.Find("/MapObject");
        if (mapObject != null)
        { 
            foreach (var o in rootObject)
            {
                if (IsMapObject(o))
                {
                    o.transform.SetParent(mapObject.transform, true);
                }
            }
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
        { //考虑到可能有"PlayerParent (1)"之类的命名
            if (objectName.Contains(keyName))
            {
                //Debug.Log(objectName + " False");
                return false;
            }   
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
    }
}
