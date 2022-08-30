
using UnityEngine;
using System.Linq;
using UnityEditor;
using UdonSharpEditor;
using UnityEngine.SceneManagement;
using SaccFlightAndVehicles;

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
    private Vector2 vehicleScrollPosition;
    private Vector2 particleScrollPosition;
    [MenuItem("SaccFlight/Open World Movement Logic/Installer")]
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
        #endregion

        //创建所有粒子的列表
        #region PARTICLE_SET_UP
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("particle set up");
        var particleList = scene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<ParticleSystem>());

        /*//debug only :绘制所有粒子列表
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
        return;
    }
    private void ModifyParticle(GameObject particleObject)
    {
        //修改粒子的参数以适配owml
        return;
    }
    private void PlaceOWMLPrefab()
    {
        //查找场景中是否已经有预制件,如果没有，进行一个创建
        var found = SceneManager.GetActiveScene().GetRootGameObjects().Select(o => o.GetComponent<ZHK_UIScript>()).FirstOrDefault(a => a != null);
        if (found != null)
        {
            Debug.Log("OWMLPrefab existd");
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
            Destroy(OWMLObject);
            Debug.Log("Prefab has been placed!");
        }
        //return UdonSharpEditorUtility.GetProxyBehaviour(udon) as UdonRadioCommunication;
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
        "SpawnArea", "PlayerParent", "MapObject"};
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
}
