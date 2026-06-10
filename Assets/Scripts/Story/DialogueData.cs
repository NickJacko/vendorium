using System;
using System.Collections.Generic;
using UnityEngine;

namespace Vendorium
{
    // Überschreibt den Platzhalter aus VendoriumEventManager.cs
    [CreateAssetMenu(menuName = "VendoriumData/DialogueData", fileName = "New_DialogueData")]
    public class DialogueData : ScriptableObject
    {
        [Header("Charakter")]
        public string CharacterName;
        public Sprite Portrait;

        [Header("Dialogzeilen")]
        public List<DialogueLine> Lines = new List<DialogueLine>();

        [Header("Auswahl am Ende (optional)")]
        public List<DialogueChoice> Choices = new List<DialogueChoice>();
    }

    [Serializable]
    public class DialogueLine
    {
        [TextArea(2, 5)] public string Text;
        public AudioClip VoiceClip; // optional (ElevenLabs)
    }

    [Serializable]
    public class DialogueChoice
    {
        public string ChoiceText;
        public string StoryFlagToSet;  // z.B. "accepted_viktors_offer"
        public DialogueData NextDialogue; // null = Ende
    }
}
