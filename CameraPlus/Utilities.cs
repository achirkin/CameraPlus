﻿using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using IPA.Old;
using IPA.Loader;
using UnityEngine;

namespace CameraPlus
{
    class Utils
    {
        public static Texture2D LoadTextureRaw(byte[] file)
        {
            if (file.Count() > 0)
            {
                Texture2D Tex2D = new Texture2D(2, 2);
                if (Tex2D.LoadImage(file))
                    return Tex2D;
            }
            return null;
        }

        private static Dictionary<string, Texture2D> _cachedTextures = new Dictionary<string, Texture2D>();
        public static Texture2D LoadTextureFromResources(string resourcePath)
        {
            if (!_cachedTextures.ContainsKey(resourcePath))
                _cachedTextures.Add(resourcePath, LoadTextureRaw(GetResource(Assembly.GetCallingAssembly(), resourcePath)));
            return _cachedTextures[resourcePath];
        }

        private static Dictionary<string, System.Drawing.Image> _cachedImages = new Dictionary<string, System.Drawing.Image>();
        public static System.Drawing.Image LoadImageFromResources(string resourcePath)
        {
            if(!_cachedImages.ContainsKey(resourcePath))
                _cachedImages.Add(resourcePath, new System.Drawing.Bitmap(Assembly.GetCallingAssembly().GetManifestResourceStream(resourcePath)));
            return _cachedImages[resourcePath];
        }

        public static byte[] GetResource(Assembly asm, string ResourceName)
        {
            System.IO.Stream stream = asm.GetManifestResourceStream(ResourceName);
            byte[] data = new byte[stream.Length];
            stream.Read(data, 0, (int)stream.Length);
            return data;
        }

        public static bool WithinRange(int numberToCheck, int bottom, int top)
        {
            return (numberToCheck >= bottom && numberToCheck <= top);
        }

        public static bool IsModInstalled(string modName)
        {
            Logger.log.Debug($"Looking in BSIPA for {modName}.");
            foreach (PluginLoader.PluginInfo p in PluginManager.AllPlugins)
            {
                if (p.Metadata.Name == modName || p.Metadata.Id == modName)
                    return true;
            }

            Logger.log.Debug($"{modName} not found in BSIPA. Looking through the legacy list instead...");
            foreach (IPlugin p in PluginManager.Plugins)
            {
                if (p.Name == modName)
                    return true;
            }

            Logger.log.Debug($"{modName} was not found.");
            return false;
        }
    }
}
