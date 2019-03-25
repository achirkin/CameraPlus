using System;
using System.IO;
using UnityEngine;
using XInputDotNetPure;

namespace CameraPlus.DroneCam.Input
{
    public class InputConfig
    {
        public string FilePath { get; }

        public string inputType = "XInput";
        public Type InputType { get; }
        public float deadzone = 0.05f;
        public int player = 0;
        public PlayerIndex Player { get; }

        public string comPort = "COM3";
                     
        public event Action<InputConfig> ConfigChangedEvent;

        private readonly FileSystemWatcher _configWatcher;
        private bool _saving;

        public InputConfig(string filePath)
        {
            FilePath = filePath;

            if (!Directory.Exists(Path.GetDirectoryName(FilePath)))
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));

            if (File.Exists(FilePath))
            {
                Load();
                var text = File.ReadAllText(FilePath);
            }
            Save();

            switch (inputType)
            {
                case "XInput":
                    InputType = typeof(XInput);
                    break;
                case "DJI":
                    InputType = typeof(DJIInput);
                    break;
                default:
                    InputType = typeof(CustomInput);
                    break;
            }
            Player = (PlayerIndex)player;

            _configWatcher = new FileSystemWatcher(Path.GetDirectoryName(FilePath))
            {
                NotifyFilter = NotifyFilters.LastWrite,
                Filter = Path.GetFileName(FilePath),
                EnableRaisingEvents = true
            };
            _configWatcher.Changed += ConfigWatcherOnChanged;
        }

        ~InputConfig()
        {
            _configWatcher.Changed -= ConfigWatcherOnChanged;
        }

        public void Save()
        {
            _saving = true;
            ConfigSerializer.SaveConfig(this, FilePath);
        }

        public void Load()
        {
            ConfigSerializer.LoadConfig(this, FilePath);
        }

        private void ConfigWatcherOnChanged(object sender, FileSystemEventArgs fileSystemEventArgs)
        {
            if (_saving)
            {
                _saving = false;
                return;
            }

            Load();

            if (ConfigChangedEvent != null)
            {
                ConfigChangedEvent(this);
            }
        }

        public class OldRotConfig
        {
            public float rotx;
            public float roty;
            public float rotz;
            public float rotw;
        }
    }
}