using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ReverbOnDeath.Configuration
{
    public class AudioConfig
    {
        private PlayerControllerB playerControllerB;

        private float stopEchoAt = 0f;
        private float stopVoiceAt = 0f;

        private bool lpOn;
        private bool hpOn;
        private float panStereo;
        private float playerVoicePitchTarget;
        private float playerPitch;

        public bool EchoOn => Time.time < stopEchoAt;
        public bool VoiceOn => Time.time < stopVoiceAt;

        public AudioConfig(PlayerControllerB playerControllerB, float stopEchoAt, float stopVoiceAt, bool lpOn, bool hpOn, float panStereo, float playerVoicePitchTarget, float playerPitch)
        {
            this.playerControllerB = playerControllerB;
            this.stopEchoAt = stopEchoAt;
            this.stopVoiceAt = stopVoiceAt;
            this.lpOn = lpOn;
            this.hpOn = hpOn;
            this.panStereo = panStereo;
            this.playerVoicePitchTarget = playerVoicePitchTarget;
            this.playerPitch = playerPitch;
        }

        public float StopEchoAt { get => stopEchoAt; }
        public float StopVoiceAt { get => stopVoiceAt; }
        public bool LpOn { get => lpOn; }
        public bool HpOn { get => hpOn; }
        public float PanStereo { get => panStereo; }
        public float PlayerVoicePitchTarget { get => playerVoicePitchTarget; }
        public float PlayerPitch { get => playerPitch; }
        public Transform DeadBodyT { get => playerControllerB.deadBody.transform; }
        public Transform AudioSourceT { get => playerControllerB.currentVoiceChatAudioSource.transform; }
    }
}

