using System;
using System.Threading;
using Discord;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Editor.DiscordPresence
{
    public class DiscordManager
    {
        private static DiscordManager _instance;

        public const long CLIENT_ID = 481126411986534410;
        public const long APP_ID = 481126411986534410;

        private readonly Discord.Discord _client;
        private readonly ActivityManager _presence;
        private readonly long _startTime;
        private bool _started = false;

        private DiscordManager(long? clientId = null)
        {
            _startTime = (long) (DateTime.UtcNow.AddSeconds(-EditorApplication.timeSinceStartup)
                                 - new DateTime(1970, 1, 1)).TotalSeconds;
            _client = new Discord.Discord(clientId ?? CLIENT_ID, (ulong) CreateFlags.NoRequireDiscord);
            _presence = _client.GetActivityManager();

            EditorApplication.playModeStateChanged += PlayStateChanged;
            EditorApplication.hierarchyChanged += HierarchyChanged;
            EditorApplication.quitting += Shutdown;
            EditorApplication.update += EditorUpdate;
            _client.SetLogHook(LogLevel.Debug, (level, message) =>
            {
                switch (level)
                {
                    case LogLevel.Info:
                        Debug.Log(message);
                        break;
                    case LogLevel.Warn:
                        Debug.LogWarning(message);
                        break;
                    case LogLevel.Error:
                        Debug.LogError(message);
                        break;
                    case LogLevel.Debug:
                        Debug.Log(message);
                        break;
                }
            });
        }

        private void EditorUpdate() => _client.RunCallbacks();

        public static void Initialize(long? clientId = null)
        {
            if (_instance != null)
                return;

            _instance = new DiscordManager(clientId);
            Start();
        }

        public static void UpdatePresence(long? applicationId = null)
        {
            if (_instance == null || !_instance._started)
                return;

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
                    Start = _instance._startTime
                },
                Type = ActivityType.Playing
            }, result =>
            {
                if (result != Result.Ok)
                    Debug.LogError("Failed to update Discord Activity: " + result);
            });
        }

        public static void Start()
        {
            if (_instance == null)
                return;
            _instance._started = true;
            Debug.Log("Started");
            UpdatePresence();
        }

        public static void Stop()
        {
            if (_instance == null)
                return;
            _instance._started = false;
            Debug.Log("Stopped");
            _instance._presence.ClearActivity(_ => { });
        }

        public static void Shutdown()
        {
            if (_instance == null)
                return;
            Debug.Log("Shutting Down");
            EditorApplication.playModeStateChanged -= _instance.PlayStateChanged;
            EditorApplication.hierarchyChanged -= _instance.HierarchyChanged;
            EditorApplication.quitting -= Shutdown;
            EditorApplication.update -= _instance.EditorUpdate;
            _instance._client.Dispose();
            _instance._started = false;
            _instance = null;
        }

        private void HierarchyChanged() => UpdatePresence();

        private void PlayStateChanged(PlayModeStateChange obj)
        {
            if (obj == PlayModeStateChange.ExitingEditMode)
                Stop();
            else if (obj == PlayModeStateChange.ExitingPlayMode)
                Start();
        }

        ~DiscordManager()
        {
            Debug.Log("Destroyed DiscordManager");
        }
    }
}