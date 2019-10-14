using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.UI
{
    [CustomEditor(typeof(VirtuakListView), true)]
    [CanEditMultipleObjects]
    public class VirtuakListViewEditor : ScrollRectEditor
    {
        SerializedProperty m_Padding;
        SerializedProperty m_CellSize;
        SerializedProperty m_Spacing;
        SerializedProperty m_StartCorner;
        SerializedProperty m_StartAxis;
        SerializedProperty m_ChildAlignment;
        SerializedProperty m_Constraint;
        SerializedProperty m_ConstraintCount;
        SerializedProperty m_templet;
        SerializedProperty m_scrollType;
        SerializedProperty m_itemCount;
        protected override void OnEnable()
        {
            base.OnEnable();
            m_Padding = serializedObject.FindProperty("m_Padding");
            m_CellSize = serializedObject.FindProperty("m_CellSize");
            m_Spacing = serializedObject.FindProperty("m_Spacing");
            m_StartCorner = serializedObject.FindProperty("m_StartCorner");
            m_StartAxis = serializedObject.FindProperty("m_StartAxis");
            //m_ChildAlignment = serializedObject.FindProperty("m_ChildAlignment");
            m_Constraint = serializedObject.FindProperty("m_Constraint");
            m_ConstraintCount = serializedObject.FindProperty("m_ConstraintCount");
            m_templet = serializedObject.FindProperty("m_templet");
            m_scrollType = serializedObject.FindProperty("m_scrollType");
            m_itemCount = serializedObject.FindProperty("m_itemCount");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();
            EditorGUILayout.PropertyField(m_templet, true);
            EditorGUILayout.PropertyField(m_scrollType, true);
            EditorGUILayout.PropertyField(m_itemCount, true);
            EditorGUILayout.PropertyField(m_Padding, true);
            EditorGUILayout.PropertyField(m_CellSize, true);
            EditorGUILayout.PropertyField(m_Spacing, true);
            EditorGUILayout.PropertyField(m_StartCorner, true);
            EditorGUILayout.PropertyField(m_StartAxis, true);
            //EditorGUILayout.PropertyField(m_ChildAlignment, true);
            EditorGUILayout.PropertyField(m_Constraint, true);
            if (m_Constraint.enumValueIndex > 0)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_ConstraintCount, true);
                EditorGUI.indentLevel--;
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
