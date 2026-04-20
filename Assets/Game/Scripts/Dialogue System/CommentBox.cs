/*---------------------------
File: CommentBox.cs
Author: Chandler Mays
----------------------------*/
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PolyQuest.Dialogues
{
    /* -----------------------------------------------------------------------
     * Role: Represents a comment box on the dialogue editor canvas.         *
     *                                                                        *
     * Responsibilities:                                                      *
     *      - Stores the label, tint color, and rect of the comment box.     *
     *      - Provides editor-only setters used by DialogueEditorWindow.     *
     *      - Participates in Undo/Redo and EditorUtility.SetDirty so all   *
     *        changes are tracked and serialized with the parent Dialogue     *
     *        asset as a sub-asset.                                           *
     * ---------------------------------------------------------------------- */
    public class CommentBox : ScriptableObject
    {
        [SerializeField] private string m_label = "Comment";
        [SerializeField] private Color m_color = new Color(0.2f, 0.6f, 1f, 0.18f);
        [SerializeField] private Rect m_rect = new Rect(100f, 100f, 300f, 200f);

        // ── Public read accessors ──────────────────────────────────────────
        public string Label => m_label;
        public Color BoxColor => m_color;
        public Rect Rect => m_rect;

        /// <summary>Height of the header bar drawn by the editor window.</summary>
        public const float HeaderHeight = 24f;

#if UNITY_EDITOR
        // ── Editor-only setters (undo-tracked) ────────────────────────────

        public void SetLabel(string label)
        {
            if (label == m_label) return;
            Undo.RecordObject(this, "Rename Comment Box");
            m_label = label;
            EditorUtility.SetDirty(this);
        }

        public void SetColor(Color color)
        {
            if (color == m_color) return;
            Undo.RecordObject(this, "Recolor Comment Box");
            m_color = color;
            EditorUtility.SetDirty(this);
        }

        public void SetRect(Rect rect)
        {
            Undo.RecordObject(this, "Move/Resize Comment Box");
            m_rect = rect;
            EditorUtility.SetDirty(this);
        }

        /// <summary>Translates the box origin by <paramref name="delta"/>.</summary>
        public void Move(Vector2 delta)
        {
            Undo.RecordObject(this, "Move Comment Box");
            m_rect.position += delta;
            EditorUtility.SetDirty(this);
        }

        /// <summary>Sets only the size, clamped to a sensible minimum.</summary>
        public void Resize(Vector2 newSize)
        {
            Undo.RecordObject(this, "Resize Comment Box");
            m_rect.size = new Vector2(Mathf.Max(120f, newSize.x),
                                      Mathf.Max(60f, newSize.y));
            EditorUtility.SetDirty(this);
        }
#endif
    }
}