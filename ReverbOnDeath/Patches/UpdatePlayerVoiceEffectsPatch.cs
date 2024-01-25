using Dissonance.Integrations.Unity_NFGO;
using GameNetcodeStuff;
using HarmonyLib;
using ReverbOnDeath.Configuration;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ReverbOnDeath.Patches
{
    [HarmonyPatch(typeof(StartOfRound), "UpdatePlayerVoiceEffects")]
    class UpdatePlayerVoiceEffectsPatch
    {
        private static bool updateStarted = false;
        private static Dictionary<PlayerControllerB, AudioConfig> configs = new Dictionary<PlayerControllerB, AudioConfig>();

        public static Dictionary<PlayerControllerB, AudioConfig> Configs { get => configs; }

        private static void Prefix()
        {
            if (configs == null) configs = new Dictionary<PlayerControllerB, AudioConfig>();

            if (!updateStarted)
            {
                HUDManager.Instance.StartCoroutine(UpdateNumerator());
                updateStarted = true;
            }

            if (GameNetworkManager.Instance == null || GameNetworkManager.Instance.localPlayerController == null) return;

            if (StartOfRound.Instance == null || StartOfRound.Instance.allPlayerScripts == null) return;

            for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
            {
                PlayerControllerB pcb = StartOfRound.Instance.allPlayerScripts[i];

                if (pcb == null) continue;

                if ((pcb.isPlayerControlled || pcb.isPlayerDead) && (pcb != GameNetworkManager.Instance.localPlayerController))
                {
                    AudioSource currentVoiceChatAudioSource = pcb.currentVoiceChatAudioSource;

                    if (currentVoiceChatAudioSource == null) continue;

                    // add and configure echo filter
                    if (currentVoiceChatAudioSource.gameObject.GetComponent<AudioEchoFilter>() == null)
                    {
                        currentVoiceChatAudioSource.gameObject.AddComponent<AudioEchoFilter>();
                        AudioEchoFilter aef = currentVoiceChatAudioSource.gameObject.GetComponent<AudioEchoFilter>();
                        aef.delay = 25f;
                        aef.decayRatio = 0.9f;
                        aef.wetMix = 1f;
                        aef.dryMix = 0f;
                        aef.enabled = false;
                    }

                    if (pcb.isPlayerDead) // add dead player info to configs
                    {
                        if (!configs.ContainsKey(pcb))
                        {
                            // copy current settings from currentVoiceChatAudioSource
                            configs.Add(pcb,
                                new AudioConfig(
                                        pcb,
                                        Time.time + 1.5f, // echo cutoff
                                        Time.time + 0.1f, // voice cutoff
                                        currentVoiceChatAudioSource.GetComponent<AudioLowPassFilter>().enabled,
                                        currentVoiceChatAudioSource.GetComponent<AudioHighPassFilter>().enabled,
                                        currentVoiceChatAudioSource.panStereo,
                                        SoundManager.Instance.playerVoicePitchTargets[(int)((IntPtr)pcb.playerClientId)],
                                        GetPitch(pcb)
                                )
                            );
                        }
                    }
                }
            }
        }

        private static void Postfix()
        {
            if (configs == null) configs = new Dictionary<PlayerControllerB, AudioConfig>();

            if (GameNetworkManager.Instance == null || GameNetworkManager.Instance.localPlayerController == null) return;

            foreach (var pcb in configs.Keys.ToArray())
            {
                if (pcb == null) continue;

                AudioConfig config = configs[pcb];

                if (config == null) continue;

                if ((pcb.isPlayerControlled || pcb.isPlayerDead) && !(pcb == GameNetworkManager.Instance.localPlayerController))
                {
                    if (pcb.currentVoiceChatAudioSource == null) continue;

                    AudioSource currentVoiceChatAudioSource = pcb.currentVoiceChatAudioSource;

                    if (config.EchoOn)
                    {
                        if (pcb.deadBody != null) currentVoiceChatAudioSource.transform.position = pcb.deadBody.transform.position;

                        // enable echo
                        currentVoiceChatAudioSource.gameObject.GetComponent<AudioEchoFilter>().enabled = true;

                        // restore voice settings

                        if (currentVoiceChatAudioSource.GetComponent<AudioLowPassFilter>() != null)
                            currentVoiceChatAudioSource.GetComponent<AudioLowPassFilter>().enabled = config.LpOn;

                        if (currentVoiceChatAudioSource.GetComponent<AudioHighPassFilter>() != null)
                            currentVoiceChatAudioSource.GetComponent<AudioHighPassFilter>().enabled = config.HpOn;

                        currentVoiceChatAudioSource.panStereo = config.PanStereo;

                        if (SoundManager.Instance != null)
                        {
                            SoundManager.Instance.playerVoicePitchTargets[(int)((IntPtr)pcb.playerClientId)] = config.PlayerVoicePitchTarget;
                            SoundManager.Instance.SetPlayerPitch(config.PlayerPitch, unchecked((int)pcb.playerClientId));
                        }

                        currentVoiceChatAudioSource.spatialBlend = 1f;
                        pcb.currentVoiceChatIngameSettings.set2D = false;
                        pcb.voicePlayerState.Volume = config.VoiceOn ? 1f : 0f;
                        currentVoiceChatAudioSource.volume = 1f; // used in MoreScreams, not LC source.
                    }
                    else // disable reverb after delay
                    {
                        if (currentVoiceChatAudioSource.gameObject.GetComponent<AudioEchoFilter>().enabled)
                            currentVoiceChatAudioSource.gameObject.GetComponent<AudioEchoFilter>().enabled = false;
                    }
                }
                else if (!pcb.isPlayerDead) configs.Remove(pcb);
            }
        }

        private static IEnumerator UpdateNumerator()
        {
            yield return 0;

            while (true)
            {
                UpdatePlayersStatus();
                yield return new WaitForFixedUpdate();
            }
        }

        private static void UpdatePlayersStatus()
        {
            if (configs == null) return;

            bool voiceEffectsNeedsUpdate = false;

            foreach (var player in configs.ToArray())
            {
                if (player.Key == null) continue;

                if (!player.Key.isPlayerDead)
                {
                    configs.Remove(player.Key);
                    voiceEffectsNeedsUpdate = true;
                }
                else if (player.Value.DeadBodyT != null && player.Value.AudioSourceT != null)
                    player.Value.AudioSourceT.position = player.Value.DeadBodyT.position;
            }

            if (voiceEffectsNeedsUpdate) StartOfRound.Instance.UpdatePlayerVoiceEffects();

        }

        private static float GetPitch(PlayerControllerB playerControllerB)
        {
            int playerObjNum = (int)playerControllerB.playerClientId;
            float pitch;
            SoundManager.Instance.diageticMixer.GetFloat($"PlayerPitch{playerObjNum}", out pitch);
            return pitch;
        }
    }
}
