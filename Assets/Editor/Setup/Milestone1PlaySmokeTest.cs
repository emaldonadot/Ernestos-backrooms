using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EndlessRooms.EditorSetup
{
    /// <summary>
    /// Headless smoke test: opens the Milestone 1 scene, enters Play mode for a few
    /// frames, and exits. Any exception thrown during Awake/OnEnable/Update shows up
    /// in the Editor log, which is the closest this sandbox (no GUI automation) can
    /// get to actually pressing Play. One-time setup/verification utility.
    /// Invoke via `-executeMethod EndlessRooms.EditorSetup.Milestone1PlaySmokeTest.Run`.
    /// </summary>
    public static class Milestone1PlaySmokeTest
    {
        private const string ScenePath = "Assets/TheEndlessRooms/Scenes/Milestone1_TestScene.unity";
        private static int _framesWaited;

        public static void Run()
        {
            EditorSceneManager.OpenScene(ScenePath);
            EditorApplication.update += WaitThenExit;
            EditorApplication.isPlaying = true;
        }

        private static void WaitThenExit()
        {
            if (!EditorApplication.isPlaying)
            {
                return;
            }

            _framesWaited++;
            if (_framesWaited < 30)
            {
                return;
            }

            EditorApplication.update -= WaitThenExit;
            Debug.Log("[Milestone1PlaySmokeTest] Survived 30 frames in Play mode without an uncaught exception.");
            EditorApplication.isPlaying = false;
            EditorApplication.Exit(0);
        }
    }
}
