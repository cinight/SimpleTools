//script originally comes from https://docs.unity3d.com/2022.2/Documentation/ScriptReference/Unity.Profiling.LowLevel.Unsafe.ProfilerRecorderHandle.GetAvailable.html

using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEditor;

public class AvailableProfilerMarkersAndSamplers
{
    struct StatInfo
    {
        public ProfilerCategory Cat;
        public string Name;
        public ProfilerMarkerDataUnit Unit;
    }

    [MenuItem("Test/GetAvailableProfilerMarkers")]
    static unsafe void EnumerateProfilerStats()
    {
        var availableStatHandles = new List<ProfilerRecorderHandle>();
        ProfilerRecorderHandle.GetAvailable(availableStatHandles);

        var availableStats = new List<StatInfo>(availableStatHandles.Count);
        foreach (var h in availableStatHandles)
        {
            var statDesc = ProfilerRecorderHandle.GetDescription(h);
            var statInfo = new StatInfo()
            {
                Cat = statDesc.Category,
                Name = statDesc.Name,
                Unit = statDesc.UnitType
            };
            availableStats.Add(statInfo);
        }
        availableStats.Sort((a, b) =>
        {
            var result = string.Compare(a.Cat.ToString(), b.Cat.ToString());
            if (result != 0)
                return result;

            return string.Compare(a.Name, b.Name);
        });

        var sb = new StringBuilder("Available stats:"+" "+availableStats.Count+"\n");
        foreach (var s in availableStats)
        {
            sb.AppendLine($"{(int)s.Cat}\t\t - {s.Name}\t\t - {s.Unit}");
        }

        string filePath = Application.dataPath+"/AvailableProfilerMarkers.txt";
        using (System.IO.StreamWriter file = new System.IO.StreamWriter(filePath))
        {
            file.WriteLine(sb.ToString());
        }

        Debug.Log("saved_to: "+filePath);

        AssetDatabase.Refresh();
    }

    [MenuItem("Test/GetAvailableSamplers")]
    static unsafe void EnumerateSamplers()
    {
        List<string> names = new List<string>();
        UnityEngine.Profiling.Sampler.GetNames(names);

        var sb = new StringBuilder("Active Samplers:"+" "+names.Count+"\n");
        foreach (var n in names)
        {
            sb.AppendLine(n);
        }

        string filePath = Application.dataPath+"/AvailableSamplers.txt";
        using (System.IO.StreamWriter file = new System.IO.StreamWriter(filePath))
        {
            file.WriteLine(sb.ToString());
        }

        Debug.Log("saved_to: "+filePath);

        AssetDatabase.Refresh();
    }
}