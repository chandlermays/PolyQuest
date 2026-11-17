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

        /*----------------------------------------------------------------------
        | --- Track: Registers an object for the kit to destroy on Dispose --- |
        ----------------------------------------------------------------------*/
        public T Track<T>(T obj) where T : Object
        {
            m_created.Add(obj);
            return obj;
        }


        /*--------------------
        | --- Conditions --- |
        --------------------*/
        /*------------------------------------------------
        | --- Always: A condition that always passes --- |
        ------------------------------------------------*/
        public static Conjunction Always => JsonUtility.FromJson<Conjunction>("{\"m_AND\":[]}");

        /*-----------------------------------------------------------------------------------
        | --- When: A condition on one condition type and parameter, optionally negated --- |
        -----------------------------------------------------------------------------------*/
        public static Conjunction When(ConditionType type, string parameter, bool negate = false)
        {
            string condition = $"{{\"m_condition\":{(int)type},\"m_parameters\":[\"{parameter}\"],\"m_negate\":{(negate ? "true" : "false")}}}";
            return JsonUtility.FromJson<Conjunction>($"{{\"m_AND\":[{{\"m_OR\":[{condition}]}}]}}");
        }


        /*---------------------------
        | --- Nodes & Dialogues --- |
        ---------------------------*/
        /*-----------------------------------------------------------------
        | --- NpcLine: Creates an NPC line with an optional condition --- |
        -----------------------------------------------------------------*/
        public DialogueNode NpcLine(string text, Conjunction condition = null) => Line(text, false, condition);

        /*----------------------------------------------------------------------
        | --- PlayerLine: Creates a Player line with an optional condition --- |
        ----------------------------------------------------------------------*/
        public DialogueNode PlayerLine(string text, Conjunction condition = null) => Line(text, true, condition);

        /*--------------------------------------------------------------------------------------
        | --- Line: Creates a dialogue node named by its text, which keeps node IDs unique --- |
        --------------------------------------------------------------------------------------*/
        private DialogueNode Line(string text, bool isPlayerSpeaking, Conjunction condition)
        {
            DialogueNode node = Track(ScriptableObject.CreateInstance<DialogueNode>());
            node.name = text;                                   // node IDs are the asset names; the fixtures keep texts unique
            PlayModeTestKit.Set(node, "m_text", text);
            PlayModeTestKit.Set(node, "m_isPlayerSpeaking", isPlayerSpeaking);
            PlayModeTestKit.Set(node, "m_condition", condition ?? Always);
            return node;
        }

        /*---------------------------------------------------------
        | --- Link: Makes the children follow the parent node --- |
        ---------------------------------------------------------*/
        public static void Link(DialogueNode parent, params DialogueNode[] children)
        {
            parent.Children.AddRange(children.Select(c => c.name));
        }

        /*---------------------------------------------------------------
        | --- OnEnter: Adds an event fired when the node is entered --- |
        ---------------------------------------------------------------*/
        public static void OnEnter(DialogueNode node, DialogueEvent dialogueEvent)
            => PlayModeTestKit.Get<List<DialogueEvent>>(node, "m_onEnterEvents").Add(dialogueEvent);

        /*-----------------------------------------------------------
        | --- OnExit: Adds an event fired when the node is left --- |
        -----------------------------------------------------------*/
        public static void OnExit(DialogueNode node, DialogueEvent dialogueEvent)
            => PlayModeTestKit.Get<List<DialogueEvent>>(node, "m_onExitEvents").Add(dialogueEvent);

        /*-------------------------------------------------------------------------------------------------------------------------
        | --- RecordEntersAndExits: Adds an enter and an exit recorder to each node, logging "enter:<text>" and "exit:<text>" --- |
        -------------------------------------------------------------------------------------------------------------------------*/
        public void RecordEntersAndExits(params DialogueNode[] nodes)
        {
            foreach (DialogueNode node in nodes)
            {
                OnEnter(node, Recorder("enter:" + node.Text));
                OnExit(node, Recorder("exit:" + node.Text));
            }
        }

        /*-----------------------------------------------------------------------------------
        | --- Recorder: Creates a RecordingDialogueEvent that writes its label to Fired --- |
        -----------------------------------------------------------------------------------*/
        public RecordingDialogueEvent Recorder(string label)
        {
            RecordingDialogueEvent recorder = Track(ScriptableObject.CreateInstance<RecordingDialogueEvent>());
            recorder.Log = Fired;
            recorder.Label = label;
            return recorder;
        }

        /*-----------------------------------------------------------
        | --- Event: Creates a dialogue event of the given type --- |
        -----------------------------------------------------------*/
        public T Event<T>() where T : DialogueEvent => Track(ScriptableObject.CreateInstance<T>());

        /*-------------------------------------------------------------------------------------------------------------
        | --- Dialogue: Creates a dialogue whose nodes are stored in the given order, which is the authored order --- |
        -------------------------------------------------------------------------------------------------------------*/
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


        /*-------------
        | --- NPC --- |
        -------------*/
        /*---------------------------------------------------------------------------------------------------
        | --- Npc: Creates an NPC with a label, outline and dialogue, optionally with a dialogue camera --- |
        ---------------------------------------------------------------------------------------------------*/
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

        /*----------------------------------------------------------------
        | --- HistoryKey: The key DialogueHistory records for an NPC --- |
        ----------------------------------------------------------------*/
        public static string HistoryKey(AIDialogueHandler npc) => npc.SpeakerName;

        /*------------------------------------------------------------------------------------------
        | --- Talk: Starts the conversation the way arriving at the NPC does, without the walk --- |
        ------------------------------------------------------------------------------------------*/
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


        /*---------------------
        | --- Dialogue UI --- |
        ---------------------*/
        /*------------------------------------------------------------------
        | --- UIParts: The dialogue UI and the parts the tests inspect --- |
        ------------------------------------------------------------------*/
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

        /*-------------------------------------------------------------------------------------------------------
        | --- DialogueUI: Builds a dialogue UI with its text, choice and button parts, wired to the handler --- |
        -------------------------------------------------------------------------------------------------------*/
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

        /*----------------------------------------------------------------
        | --- Child: Creates a child object with the given component --- |
        ----------------------------------------------------------------*/
        private static T Child<T>(GameObject parent, string name) where T : Component
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent.transform, false);
            return child.AddComponent<T>();
        }

        /*--------------------------------------------------------
        | --- Dispose: Destroys every object the kit created --- |
        --------------------------------------------------------*/
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