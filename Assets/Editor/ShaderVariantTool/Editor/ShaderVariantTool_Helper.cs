using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using System;
using System.IO;
using System.Globalization;
using System.Linq;
using UnityEditor;

namespace GfxQA.ShaderVariantTool
{
    public static class Helper
    {
        public static string GetCSVFolderPath()
        {
            return Application.dataPath.Replace("/Assets","/");
        }

        private static string culturePref = "ShaderVariantTool_Culture";
        public static CultureInfo culture;
        public static List<CultureInfo> cinfo;

        public static void SetupCultureInfo()
        {
            if(cinfo == null) cinfo = CultureInfo.GetCultures(CultureTypes.AllCultures & ~CultureTypes.NeutralCultures).ToList<CultureInfo>();
            string cultureString = EditorPrefs.GetString(culturePref,"English (United States)");
            if(culture == null ) culture = cinfo.Find(x => x.DisplayName == cultureString);
        }

        public static void UpdateCultureInfo(string displayName)
        {
            EditorPrefs.SetString(culturePref,displayName);
            culture = cinfo.Find(x => x.DisplayName == displayName);
        }

        public static string NumberSeperator(string input)
        {
            UInt64 outNum = 0;
            bool success = UInt64.TryParse(input, out outNum);
            if(success)
            {
                return outNum.ToString("N0", culture);
            }
            else
            {
                return input;
            }
        }

        public static string TimeFormatString (double timeInSeconds)
        {
            SetupCultureInfo();

            float t = (float)timeInSeconds;

            float hour = t / 3600f;
            hour = Mathf.Floor(hour);
            t -= hour*3600f;

            float minute = t / 60f;
            minute = Mathf.Floor(minute);
            t -= minute*60f;

            float second = t;

            string timeString = "";

            if(hour > 0) timeString += hour + "hr ";
            if(minute > 0) timeString += minute + "m ";
            timeString += NumberSeperator(second.ToString()) + "s";

            return timeString;
        }

        public static string GetRemainingString(string line, string from)
        {
            int index = line.IndexOf(from) + from.Length;
            return line.Substring( index , line.Length - index);
        }

        public static string ExtractString(string line, string from, string to, bool takeLastIndexOfTo = true)
        {
            int pFrom = 0;
            if(from != "")
            {
                int index = line.IndexOf(from);
                if(index >= 0) pFrom = index + from.Length;
            }
            
            int pTo = line.Length;
            if(to != "")
            {
                int index = line.LastIndexOf(to);
                if(!takeLastIndexOfTo)
                {
                    index = line.IndexOf(to);
                }

                if(index >= 0) pTo = index;
            }

            return line.Substring(pFrom, pTo - pFrom);
        }

        public static void DebugLog(string msg)
        {
            Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, msg);
        }

        public static string GetPlatformKeywordList(PlatformKeywordSet pks)
        {
            string enabledPKeys = "";
            foreach(BuiltinShaderDefine sd in System.Enum.GetValues(typeof(BuiltinShaderDefine))) 
            {
                //Only pay attention to SHADER_API_MOBILE, SHADER_API_DESKTOP and SHADER_API_GLES30
                if( sd.ToString().Contains("SHADER_API") && pks.IsEnabled(sd) )
                {
                    if(enabledPKeys != "") enabledPKeys += " ";
                    enabledPKeys += sd.ToString();
                }
            }
            return enabledPKeys;
        }

        public static string GetEditorLogPath()
        {
            string editorLogPath = "";
            switch(Application.platform)
            {
                case RuntimePlatform.WindowsEditor: editorLogPath=Environment.GetEnvironmentVariable("AppData").Replace("Roaming","")+"Local\\Unity\\Editor\\Editor.log"; break;
                case RuntimePlatform.OSXEditor: editorLogPath=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Library")+"/Logs/Unity/Editor.log"; break;
                case RuntimePlatform.LinuxEditor: editorLogPath="~/.config/unity3d/Editor.log"; break;
            }
            return editorLogPath;
        }
    }
}