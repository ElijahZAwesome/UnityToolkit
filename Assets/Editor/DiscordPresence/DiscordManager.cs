using System;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using Discord;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Editor.DiscordPresence
{
    [ExecuteInEditMode]
    public class DiscordManager : MonoBehaviour
    {
        private static DiscordManager _instance;
        
        public const long CLIENT_ID = 481126411986534410;
        public const long APP_ID = 481126411986534410;
        
        private Discord.Discord _client;
        private ActivityManager _presence;
        private bool _started = false;

        private DiscordManager(long? clientId = null)
        {
            _instance = this;
            _client = new Discord.Discord(clientId ?? CLIENT_ID, (ulong) Discord.CreateFlags.NoRequireDiscord);
            _presence = _client.GetActivityManager();

            EditorApplication.playModeStateChanged += PlayStateChanged;
            EditorApplication.hierarchyChanged += HierarchyChanged;
            EditorApplication.quitting += Shutdown;
            InvokeRepeating(nameof(EditorUpdate), 0, 1000 / 60);
        }

        private void EditorUpdate() => _client.RunCallbacks();

        public static void Start(long? clientId = null)
        {
            EnsureInitialized();
            if (clientId != null)
            {
                Shutdown();
                _instance._client = new Discord.Discord(clientId.Value, (ulong) Discord.CreateFlags.NoRequireDiscord);
                _instance._presence = _instance._client.GetActivityManager();
            }

            _instance._started = true;
            UpdatePresence();
        }

        public static void UpdatePresence(long? applicationId = null)
        {
            if (!_instance._started)
                return;
            EnsureInitialized();
            var sceneName = EditorSceneManager.GetActiveScene().name;
            if (string.IsNullOrEmpty(sceneName))
                sceneName = "an unsaved scene";
            _instance._presence.UpdateActivity(new Activity()
            {
                ApplicationId = applicationId ?? APP_ID,
                Assets = new ActivityAssets()
                {
                    LargeImage = "unityicon",
                    LargeText = "Unity Editor"
                },
                Name = "Unity Editor",
                Details = "Working on " + sceneName,
                Timestamps = new ActivityTimestamps()
                {
                    Start = (long) (DateTime.UtcNow.AddSeconds(-EditorApplication.timeSinceStartup)
                                    - new DateTime(1970, 1, 1)).TotalSeconds
                },
                Type = ActivityType.Playing
            }, result =>
            {
                if(result != Result.Ok)
                    Debug.LogError("Failed to update Discord Activity: " + result);
            });
        }

        public static void Stop()
        {       
            EnsureInitialized();
            _instance._started = false;
            _instance._presence.ClearActivity(result => { Debug.Log(result); });
        }

        public static void Shutdown()
        {
            EnsureInitialized();
            _instance._client.Dispose();
            _instance.CancelInvoke(nameof(EditorUpdate));
        }

        private void HierarchyChanged()
        {
            UpdatePresence();
        }

        private void PlayStateChanged(PlayModeStateChange obj)
        {
            if(obj == PlayModeStateChange.ExitingEditMode)
                Stop();
            else if(obj == PlayModeStateChange.ExitingPlayMode)
                Start();
        }

        private static void EnsureInitialized() => _instance ??= new DiscordManager();
    }
}