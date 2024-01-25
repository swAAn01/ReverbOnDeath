using Dissonance.Audio.Playback;
using HarmonyLib;
using ReverbOnDeath.Configuration;
using System;
using System.Reflection;

namespace ReverbOnDeath.Patches
{
    [HarmonyPatch]
    class DissonancePatch
    {
        public static MethodBase TargetMethod()
        {
            // use normal reflection or helper methods in <AccessTools> to find the method/constructor
            // you want to patch and return its MethodInfo/ConstructorInfo
            //
            return AccessTools.FirstMethod(typeof(VoicePlayback), method => method.Name.Contains("SetTransform"));
        }

        static bool Prefix(object __instance)
        {
            foreach (AudioConfig conf in UpdatePlayerVoiceEffectsPatch.Configs.Values)
            {
                if (!conf.EchoOn) continue;

                if ((__instance as VoicePlayback).transform.Equals(conf.AudioSourceT))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
