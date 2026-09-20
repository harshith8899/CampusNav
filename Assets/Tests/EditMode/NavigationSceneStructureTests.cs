using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CampusNav.EditorTools.Tests
{
    /// <summary>
    /// There's no runtime behavior to unit-test against NavigationScene yet
    /// (its components are mostly unwired, waiting on assets we don't have —
    /// see the scene's own notes). This instead guards the scene's structure
    /// itself, so someone accidentally deleting the AR Session / AR Session
    /// Origin / NavigationController hierarchy later gets caught here
    /// instead of discovered at runtime on a device.
    /// </summary>
    public class NavigationSceneStructureTests
    {
        private const string ScenePath = "Assets/Scenes/NavigationScene.unity";

        [Test]
        public void NavigationScene_ContainsRequiredRootObjects()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            Assert.IsTrue(scene.IsValid(), $"Could not open scene at '{ScenePath}'.");
            Assert.IsNotNull(GameObject.Find("AR Session"), "NavigationScene is missing its 'AR Session' GameObject.");
            Assert.IsNotNull(GameObject.Find("AR Session Origin"), "NavigationScene is missing its 'AR Session Origin' GameObject.");
            Assert.IsNotNull(GameObject.Find("NavigationController"), "NavigationScene is missing its 'NavigationController' GameObject.");
        }
    }
}
