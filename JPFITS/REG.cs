using Microsoft.Win32;

namespace JPFITS
{
	public class REG
	{
		public static void SetReg(string programName, string keyName, object keyValue)
		{
			RegistryKey User = Registry.CurrentUser;
			RegistryKey SW = User.OpenSubKey("Software", true);
			RegistryKey AstroWerks = SW.CreateSubKey("AstroWerks");
			RegistryKey SUBKEY = AstroWerks.CreateSubKey(programName);
			SUBKEY.SetValue(keyName, keyValue);
		}

		public static object GetReg(string programName, string keyName)
		{
			RegistryKey User = Registry.CurrentUser;
			RegistryKey SW = User.OpenSubKey("Software", true);
			RegistryKey AstroWerks = SW.CreateSubKey("AstroWerks");
			RegistryKey SUBKEY = AstroWerks.CreateSubKey(programName);
			return SUBKEY.GetValue(keyName);
		}
	}
}

//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Text.Json;
//using Microsoft.Win32;

//namespace JPFITS
//{
//    public static class JSONSettings
//    {
//        private static readonly string LocalBasePath = Path.Combine(
//            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
//            "AstroWerks");

//        private static readonly string SharedBasePath = Path.Combine(
//            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
//            "OneDrive", "AstroWerks");

//        public static void SetReg(string programName, string keyName, object keyValue, bool isShared = false)
//        {
//            try
//            {
//                string filePath = GetFilePath(programName, isShared);
//                Dictionary<string, object> settings = LoadSettings(filePath);

//                settings[keyName] = keyValue;

//                var options = new JsonSerializerOptions { WriteIndented = true };
//                string json = JsonSerializer.Serialize(settings, options);
//                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
//                File.WriteAllText(filePath, json);
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Error setting {keyName} (shared={isShared}): {ex.Message}");
//            }
//        }

//        public static object GetReg(string programName, string keyName, bool isShared = false)
//        {
//            try
//            {
//                string filePath = GetFilePath(programName, isShared);
//                Dictionary<string, object> settings = LoadSettings(filePath);

//                if (settings.TryGetValue(keyName, out object value))
//                    return value;
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Error getting {keyName} (shared={isShared}): {ex.Message}");
//            }
//            return null;
//        }

//        //public static void MigrateFromRegistry(string programName)
//        //{
//        //    try
//        //    {
//        //        using RegistryKey user = Registry.CurrentUser;
//        //        using RegistryKey sw = user.OpenSubKey("Software", false);
//        //        using RegistryKey astroWerks = sw?.OpenSubKey("AstroWerks", false);
//        //        using RegistryKey subKey = astroWerks?.OpenSubKey(programName, false);

//        //        if (subKey == null)
//        //            return;

//        //        Dictionary<string, object> sharedSettings = new Dictionary<string, object>();
//        //        Dictionary<string, object> localSettings = new Dictionary<string, object>();

//        //        // Categorize settings (example logic; adjust based on your needs)
//        //        foreach (string valueName in subKey.GetValueNames())
//        //        {
//        //            object value = subKey.GetValue(valueName);
//        //            // Example: Settings like "Theme", "FontSize", "IsDebug" are shared; others are local
//        //            if (valueName is "Theme" or "FontSize" or "IsDebug")
//        //                sharedSettings[valueName] = value;
//        //            else
//        //                localSettings[valueName] = value;
//        //        }

//        //        // Save shared settings
//        //        if (sharedSettings.Count > 0)
//        //        {
//        //            string sharedFilePath = GetFilePath(programName, true);
//        //            var options = new JsonSerializerOptions { WriteIndented = true };
//        //            string json = JsonSerializer.Serialize(sharedSettings, options);
//        //            Directory.CreateDirectory(Path.GetDirectoryName(sharedFilePath));
//        //            File.WriteAllText(sharedFileName, json);
//        //        }

//        //        // Save local settings
//        //        if (localSettings.Count > 0)
//        //        {
//        //            string localFilePath = GetFilePath(programName, false);
//        //            var options = new JsonSerializerOptions { WriteIndented = true };
//        //            string json = JsonSerializer.Serialize(localSettings, options);
//        //            Directory.CreateDirectory(Path.GetDirectoryName(localFilePath));
//        //            File.WriteAllText(localFilePath, json);
//        //        }
//        //    }
//        //    catch (Exception ex)
//        //    {
//        //        Console.WriteLine($"Error migrating {programName}: {ex.Message}");
//        //    }
//        //}

//        private static string GetFilePath(string programName, bool isShared)
//        {
//            string basePath = isShared ? SharedBasePath : LocalBasePath;
//            string fileName = isShared ? "shared_settings.json" : "local_settings.json";
//            return Path.Combine(basePath, programName, fileName);
//        }

//        private static Dictionary<string, object> LoadSettings(string filePath)
//        {
//            if (File.Exists(filePath))
//            {
//                string json = File.ReadAllText(filePath);
//                return JsonSerializer.Deserialize<Dictionary<string, object>>(json) ?? new Dictionary<string, object>();
//            }
//            return new Dictionary<string, object>();
//        }
//    }
//}