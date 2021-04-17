using System;
using Discord;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Editor.DiscordPresence
{
    [InitializeOnLoad]
    public static class DiscordManager
    {
        public const long CLIENT_ID = 481126411986534410;
        public const long APP_ID = 481126411986534410;

        private static Discord.Discord _client;
        private static ActivityManager _presence;

        static DiscordManager()
        {
            EditorApplication.playModeStateChanged += OnPlayStateChanged;

            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            Initialize();
        }

        private static void Initialize()
        {
            if (Application.isBatchMode)
            {
                Debug.Log($"Discord RPC can't be initialized in BatchMode.");
                return;
            }

            if (_client != null)
                return;

            _client = new Discord.Discord(CLIENT_ID, (long) CreateFlags.NoRequireDiscord);
            _presence = _client.GetActivityManager();

            EditorApplication.update += Update;
            EditorApplication.hierarchyChanged += UpdatePresence;
            EditorApplication.quitting += Shutdown;
            UpdatePresence();
        }

        private static void Update()
        {
            try
            {
                _client?.RunCallbacks();
            }
            catch (ResultException e)
            {
                Debug.LogError("Discord GameSDK crashed! " + e);
                Shutdown();
            }
        }

        private static void UpdatePresence()
        {
            if (_client == null)
                return;

            var scene = EditorSceneManager.GetActiveScene().name;
            if (string.IsNullOrEmpty(scene))
                scene = "In an unnamed scene";
            else
                scene = "In scene " + scene;

            var activity = new Activity()
            {
                ApplicationId = APP_ID,
                Assets = new ActivityAssets()
                {
                    LargeImage = "unityicon",
                    LargeText = "Unity Editor " + Application.unityVersion
                },
                Name = "Unity Editor",
                Details = "Working on " + Application.productName,
                State = scene,
                Timestamps = new ActivityTimestamps()
                {
                    Start = (long) (DateTime.UtcNow.AddSeconds(-EditorApplication.timeSinceStartup) -
                                    new DateTime(1970, 1, 1)).TotalSeconds
                }
            };

            _presence.UpdateActivity(activity, _ => { });
        }

        private static void Shutdown()
        {
            if (_client != null)
            {
                _client.Dispose();
                _client = null;
            }
        }

        private static void OnPlayStateChanged(PlayModeStateChange playModeStateChange)
        {
            if (playModeStateChange == PlayModeStateChange.ExitingEditMode)
                Shutdown();
            else if (playModeStateChange == PlayModeStateChange.EnteredEditMode)
                Initialize();
        }
    }
}