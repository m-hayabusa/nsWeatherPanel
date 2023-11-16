using UnityEngine;
using UnityEditor;
using UnityEngine.EventSystems;
using System.Linq;

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
            var eventSystems = Resources.FindObjectsOfTypeAll<EventSystem>().Where(e => (e.hideFlags & HideFlags.HideInHierarchy) == 0);
            if (!eventSystems.Any())
            {
                var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                Undo.RegisterCreatedObjectUndo(eventSystem, "EventSystem");
            }

            Selection.activeObject = res;
        }
    }
}