using System;
using System.Collections.Generic;
using UnityEngine;

namespace Vendorium
{
    [CreateAssetMenu(menuName = "VendoriumData/AudioData", fileName = "AudioData")]
    public class AudioData : ScriptableObject
    {
        [Header("Hintergrundmusik")]
        public AudioClip MainTheme;
        public AudioClip ShopMusic;
        public AudioClip NightMusic;

        [Header("Ambient")]
        public AudioClip ShopAmbient;
        public AudioClip OutsideAmbient;
        public AudioClip NightAmbient;

        [Header("Schritte")]
        public List<AudioClip> FootstepsConcrete = new List<AudioClip>();
        public List<AudioClip> FootstepsTile = new List<AudioClip>();

        [Header("SFX")]
        public AudioClip CoinPickup;
        public AudioClip MachineSale;
        public AudioClip MachineEmpty;
        public AudioClip MachineUpgrade;
        public AudioClip MachineBroken;
        public AudioClip DoorOpen;
        public AudioClip DoorClose;
        public AudioClip RoomUnlock;
        public AudioClip ButtonClick;
        public AudioClip ButtonHover;
        public AudioClip NotificationIn;
        public AudioClip DailyReport;
        public AudioClip SynergyDiscovered;
        public AudioClip EventStart;

        [Header("SFX Name-Lookup (für AudioManager.PlaySFX)")]
        public List<NamedClip> NamedSFXClips = new List<NamedClip>();

        public AudioClip GetSFXByName(string clipName)
        {
            foreach (var entry in NamedSFXClips)
                if (entry.Name == clipName) return entry.Clip;
            return null;
        }
    }

    [Serializable]
    public class NamedClip
    {
        public string Name;
        public AudioClip Clip;
    }
}
