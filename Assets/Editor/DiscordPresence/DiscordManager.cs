using System;
using System.Runtime.InteropServices.ComTypes;
using Discord;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Editor.DiscordPresence
{
    public class DiscordManager
    {
        private static DiscordManager _instance;
        
        public const long CLIENT_ID = 481126411986534410;
        public const long APP_ID = 481126411986534410;

        public DateTime TimeStarted;
        
        private Discord.Discord _client;
        private ActivityManager _presence;

        private DiscordManager(long? clientId = null)
        {
            _instance = this;
            TimeStarted = DateTime.Now;
            _client = new Discord.Discord(clientId ?? CLIENT_ID, (ulong) Discord.CreateFlags.NoRequireDiscord);
            _presence = _client.GetActivityManager();

            EditorApplication.playModeStateChanged += PlayStateChanged;
            EditorApplication.hierarchyChanged += HierarchyChanged;
            EditorApplication.quitting += Shutdown;
        }

        public static void Start(long? clientId = null)
        {
            EnsureInitialized();
            if (clientId != null)
            {
                Shutdown();
                _instance._client = new Discord.Discord(clientId.Value, (ulong) Discord.CreateFlags.NoRequireDiscord);
                _instance._presence = _instance._client.GetActivityManager();
            }
            UpdatePresence();
        }

        public static void UpdatePresence(long? applicationId = null)
        {
            EnsureInitialized();
            _instance._presence.UpdateActivity(new Activity()
            {
                ApplicationId = applicationId ?? APP_ID,
                Assets = new ActivityAssets()
                {
                    LargeImage = "unityicon",
                    LargeText = "Unity Editor"
                },
                Details = "Working on " + EditorSceneManager.GetActiveScene().name;
            }, null);
        }

        public static void Stop()
        {       
            EnsureInitialized();
            _instance._presence.ClearActivity(null);
        }

        public static void Shutdown()
        {
            EnsureInitialized();
            Stop();
            _instance._client.Dispose();
        }

        private void HierarchyChanged()
        {
            UpdatePresence();
        }

        private void PlayStateChanged(PlayModeStateChange obj)
        {
            if(obj == PlayModeStateChange.EnteredPlayMode)
                Stop();
            else if(obj == PlayModeStateChange.ExitingPlayMode)
                Start();
        }

        private static void EnsureInitialized() => _instance ??= new DiscordManager();
    }
}