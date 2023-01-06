using UnityEngine;
using UnityEditor;

namespace nekomimiStudio.weatherPanel
{
    public class CreateWeatherPanel
    {
        [MenuItem("GameObject/nekomimiStudio/WeatherPanel", false, 10)]
        public static void Create(MenuCommand menu)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Packages/studio.nekomimi.weatherpanel/Runtime/weatherPanel.prefab");
            GameObject res = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab);
            GameObjectUtility.SetParentAndAlign(res, (GameObject)menu.context);
            Undo.RegisterCreatedObjectUndo(res, "weatherPanel");
            Selection.activeObject = res;
        }
    }
}