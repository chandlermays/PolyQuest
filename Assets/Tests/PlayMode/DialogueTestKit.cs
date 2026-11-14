/*---------------------------
| File: DialogueTestKit.cs
| Author: Chandler Mays
----------------------------*/
using NUnit.Framework;
//---------------------------------
using PolyQuest.Dialogues;
using PolyQuest.Tools;
using PolyQuest.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PolyQuest.Tests
{
    // Builds test-made dialogues, NPCs and the dialogue UI on top of a PlayerRig.
    internal sealed class DialogueTestKit : IDisposable
    {
        private const BindingFlags kAny = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        private readonly List<Object> m_created = new();

        // Every RecordingDialogueEvent made by this kit writes here.
        public List<string> Fired { get; } = new();

        public T Track<T>(T obj) where T : Object
        {
            m_created.Add(obj);
            return obj;
        }

        /* --- Conditions --- */

        public static Conjunction Always => JsonUtility.FromJson<Conjunction>("{\"m_AND\":[]}");

        public static Conjunction When(ConditionType type, string parameter, bool negate = false)
        {
            string condition = $"{{\"m_condition\":{(int)type},\"m_parameters\":[\"{parameter}\"],\"m_negate\":{(negate ? "true" : "false")}}}";
            return JsonUtility.FromJson<Conjunction>($"{{\"m_AND\":[{{\"m_OR\":[{condition}]}}]}}");
        }

        /* --- Nodes and dialogues --- */

        public DialogueNode NpcLine(string text, Conjunction condition = null) => Line(text, false, condition);
        public DialogueNode PlayerLine(string text, Conjunction condition = null) => Line(text, true, condition);

        private DialogueNode Line(string text, bool isPlayerSpeaking, Conjunction condition)
        {
            DialogueNode node = Track(ScriptableObject.CreateInstance<DialogueNode>());
            node.name = text;                                   // node IDs are the asset names; the fixtures keep texts unique
            PlayModeTestKit.Set(node, "m_text", text);
            PlayModeTestKit.Set(node, "m_isPlayerSpeaking", isPlayerSpeaking);
            PlayModeTestKit.Set(node, "m_condition", condition ?? Always);
            return node;
        }

        public static void Link(DialogueNode parent, params DialogueNode[] children)
        {
            parent.Children.AddRange(children.Select(c => c.name));
        }

        public static void OnEnter(DialogueNode node, DialogueEvent dialogueEvent)
            => PlayModeTestKit.Get<List<DialogueEvent>>(node, "m_onEnterEvents").Add(dialogueEvent);

        public static void OnExit(DialogueNode node, DialogueEvent dialogueEvent)
            => PlayModeTestKit.Get<List<DialogueEvent>>(node, "m_onExitEvents").Add(dialogueEvent);

        // Adds an enter and an exit recorder to each node: "enter:<text>" / "exit:<text>".
        public void RecordEntersAndExits(params DialogueNode[] nodes)
        {
            foreach (DialogueNode node in nodes)
            {
                OnEnter(node, Recorder("enter:" + node.Text));
                OnExit(node, Recorder("exit:" + node.Text));
            }
        }

        public RecordingDialogueEvent Recorder(string label)
        {
            RecordingDialogueEvent recorder = Track(ScriptableObject.CreateInstance<RecordingDialogueEvent>());
            recorder.Log = Fired;
            recorder.Label = label;
            return recorder;
        }

        public T Event<T>() where T : DialogueEvent => Track(ScriptableObject.CreateInstance<T>());

        // Nodes are stored in the given order; that is the authored order.
        public Dialogue Dialogue(string name, params DialogueNode[] nodes)
        {
            Dialogue dialogue = Track(ScriptableObject.CreateInstance<Dialogue>());
            dialogue.name = name;
            PlayModeTestKit.Set(dialogue, "m_nodes", new List<DialogueNode>(nodes));
            MethodInfo rebuild = typeof(Dialogue).GetMethod("RebuildNodeDictionary", kAny);
            Assert.That(rebuild, Is.Not.Null, "Dialogue.RebuildNodeDictionary was not found. Was it renamed?");
            rebuild.Invoke(dialogue, null);
            return dialogue;
        }

        /* --- NPC --- */

        public AIDialogueHandler Npc(string speakerName, Dialogue dialogue, Vector3 position, bool withDialogueCamera = false)
        {
            GameObject go = new GameObject(speakerName);
            go.SetActive(false);
            go.transform.position = position;
            Track(go);

            go.AddComponent<Outline>();
            GameObject labelRoot = new GameObject("LabelRoot");
            labelRoot.transform.SetParent(go.transform, false);
            TextMeshProUGUI labelText = labelRoot.AddComponent<TextMeshProUGUI>();
            WorldLabel label = go.AddComponent<WorldLabel>();
            PlayModeTestKit.Set(label, "m_labelRoot", labelRoot);
            PlayModeTestKit.Set(label, "m_labelText", labelText);

            AIDialogueHandler npc = go.AddComponent<AIDialogueHandler>();
            PlayModeTestKit.Set(npc, "m_dialogue", dialogue);
            PlayModeTestKit.Set(npc, "m_speakerName", speakerName);

            if (withDialogueCamera)
            {
                GameObject cameraObject = new GameObject("DialogueCamera");
                cameraObject.SetActive(false);
                cameraObject.transform.SetParent(go.transform, false);
                Type cameraType = typeof(AIDialogueHandler).GetField("m_dialogueCamera", kAny).FieldType;
                PlayModeTestKit.Set(npc, "m_dialogueCamera", cameraObject.AddComponent(cameraType));
            }

            go.SetActive(true);
            return npc;
        }

        // The key DialogueHistory records for an NPC.
        public static string HistoryKey(AIDialogueHandler npc) => npc.SpeakerName;

        // Starts the conversation the way arriving at the NPC does, without the walk.
        public static void Talk(PlayerDialogueHandler handler, AIDialogueHandler npc)
        {
            handler.BeginDialogueAction(npc, PlayModeTestKit.Get<Dialogue>(npc, "m_dialogue"));
            MethodInfo begin = typeof(PlayerDialogueHandler).GetMethod("BeginDialogue", kAny);
            Assert.That(begin, Is.Not.Null, "PlayerDialogueHandler.BeginDialogue was not found. Was it renamed?");
            try
            {
                begin.Invoke(handler, null);
            }
            catch (TargetInvocationException e)
            {
                throw e.InnerException;
            }
        }

        /* --- Dialogue UI --- */

        public sealed class UIParts
        {
            public DialogueUI UI;
            public TextMeshProUGUI Speaker;
            public TextMeshProUGUI Text;
            public Transform ChoiceRoot;
            public UnityEngine.UI.Button Next;
            public UnityEngine.UI.Button End;
            public UnityEngine.UI.Button Quit;

            public List<GameObject> Choices()
            {
                List<GameObject> choices = new();
                foreach (Transform child in ChoiceRoot)
                    choices.Add(child.gameObject);
                return choices;
            }
        }

        public UIParts DialogueUI(PlayerDialogueHandler handler)
        {
            GameObject root = new GameObject("DialogueUI");
            root.SetActive(false);
            Track(root);

            UIParts parts = new UIParts
            {
                Speaker = Child<TextMeshProUGUI>(root, "Speaker"),
                Text = Child<TextMeshProUGUI>(root, "Text"),
                ChoiceRoot = new GameObject("Choices").transform,
                Next = Child<UnityEngine.UI.Button>(root, "Next"),
                End = Child<UnityEngine.UI.Button>(root, "End"),
                Quit = Child<UnityEngine.UI.Button>(root, "Quit"),
            };
            parts.ChoiceRoot.SetParent(root.transform, false);

            GameObject choicePrefab = new GameObject("ChoicePrefab");
            choicePrefab.SetActive(false);
            Track(choicePrefab);
            choicePrefab.AddComponent<UnityEngine.UI.Button>();
            new GameObject("Label").AddComponent<TextMeshProUGUI>().transform.SetParent(choicePrefab.transform, false);
            choicePrefab.SetActive(true);
            choicePrefab.transform.position = new Vector3(1000f, 1000f, 1000f);

            parts.UI = root.AddComponent<DialogueUI>();
            PlayModeTestKit.Set(parts.UI, "m_playerDialogueHandler", handler);
            PlayModeTestKit.Set(parts.UI, "m_dialogueSpeaker", parts.Speaker);
            PlayModeTestKit.Set(parts.UI, "m_dialogueText", parts.Text);
            PlayModeTestKit.Set(parts.UI, "m_choiceRoot", parts.ChoiceRoot);
            PlayModeTestKit.Set(parts.UI, "m_choicePrefab", choicePrefab);
            PlayModeTestKit.Set(parts.UI, "m_nextButton", parts.Next);
            PlayModeTestKit.Set(parts.UI, "m_endButton", parts.End);
            PlayModeTestKit.Set(parts.UI, "m_quitButton", parts.Quit);

            root.SetActive(true);
            return parts;
        }

        private static T Child<T>(GameObject parent, string name) where T : Component
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent.transform, false);
            return child.AddComponent<T>();
        }

        public void Dispose()
        {
            for (int i = m_created.Count - 1; i >= 0; --i)
            {
                if (m_created[i] != null)
                    Object.DestroyImmediate(m_created[i]);
            }
            m_created.Clear();
        }
    }
}