using UnityEditor;
using UnityEngine;

namespace CardAdventure.Editor
{
    /// <summary>
    /// Editor menu for setting selected NPC initial facing directions.
    /// </summary>
    public static class NpcFacingDirectionEditorTool
    {
        private const string MenuRoot = "CardAdventure/NPC/Facing/";

        [MenuItem(MenuRoot + "Face Up")]
        public static void FaceUp()
        {
            ApplyToSelection(NpcFacingDirection.Facing.Up);
        }

        [MenuItem(MenuRoot + "Face Down")]
        public static void FaceDown()
        {
            ApplyToSelection(NpcFacingDirection.Facing.Down);
        }

        [MenuItem(MenuRoot + "Face Left")]
        public static void FaceLeft()
        {
            ApplyToSelection(NpcFacingDirection.Facing.Left);
        }

        [MenuItem(MenuRoot + "Face Right")]
        public static void FaceRight()
        {
            ApplyToSelection(NpcFacingDirection.Facing.Right);
        }

        [MenuItem(MenuRoot + "Face Up", true)]
        [MenuItem(MenuRoot + "Face Down", true)]
        [MenuItem(MenuRoot + "Face Left", true)]
        [MenuItem(MenuRoot + "Face Right", true)]
        private static bool ValidateFacingMenu()
        {
            return Selection.gameObjects != null && Selection.gameObjects.Length > 0;
        }

        private static void ApplyToSelection(NpcFacingDirection.Facing facing)
        {
            int changedCount = 0;

            foreach (GameObject selected in Selection.gameObjects)
            {
                if (selected == null)
                {
                    continue;
                }

                GameObject npcRoot = FindNpcRoot(selected);
                if (npcRoot == null)
                {
                    Debug.LogWarning($"[NpcFacingDirectionEditorTool] NPC 컴포넌트를 찾지 못했습니다: {selected.name}", selected);
                    continue;
                }

                Undo.RegisterFullObjectHierarchyUndo(npcRoot, "Set NPC Facing Direction");

                NpcFacingDirection facingComponent = npcRoot.GetComponent<NpcFacingDirection>();
                if (facingComponent == null)
                {
                    facingComponent = Undo.AddComponent<NpcFacingDirection>(npcRoot);
                }

                SerializedObject facingSo = new SerializedObject(facingComponent);
                facingSo.FindProperty("initialFacing").enumValueIndex = (int)facing;
                facingSo.ApplyModifiedPropertiesWithoutUndo();

                ApplyKnownNpcSerializedFields(npcRoot, facing);

                facingComponent.Apply();
                EditorUtility.SetDirty(facingComponent);
                EditorUtility.SetDirty(npcRoot);
                changedCount++;
            }

            if (changedCount > 0)
            {
                Debug.Log($"[NpcFacingDirectionEditorTool] NPC {changedCount}개의 방향을 {facing}로 설정했습니다.");
            }
        }

        private static GameObject FindNpcRoot(GameObject selected)
        {
            Transform current = selected.transform;
            while (current != null)
            {
                if (IsNpc(current.gameObject))
                {
                    return current.gameObject;
                }

                current = current.parent;
            }

            return selected.GetComponentInChildren<NpcInteractable>(true) != null
                || selected.GetComponentInChildren<NpcMovement>(true) != null
                || selected.GetComponentInChildren<NpcTileAlignment>(true) != null
                || selected.GetComponentInChildren<NpcChaser>(true) != null
                || selected.GetComponentInChildren<JobChangerNpc>(true) != null
                ? selected
                : null;
        }

        private static bool IsNpc(GameObject target)
        {
            return target.GetComponent<NpcInteractable>() != null
                || target.GetComponent<NpcMovement>() != null
                || target.GetComponent<NpcTileAlignment>() != null
                || target.GetComponent<NpcChaser>() != null
                || target.GetComponent<JobChangerNpc>() != null;
        }

        private static void ApplyKnownNpcSerializedFields(GameObject npcRoot, NpcFacingDirection.Facing facing)
        {
            Vector2 dir = NpcFacingDirection.ToVector(facing);

            NpcChaser chaser = npcRoot.GetComponent<NpcChaser>();
            if (chaser != null)
            {
                SerializedObject chaserSo = new SerializedObject(chaser);
                SerializedProperty initialFacingDir = chaserSo.FindProperty("initialFacingDir");
                if (initialFacingDir != null)
                {
                    initialFacingDir.vector2Value = dir;
                    chaserSo.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(chaser);
                }
            }

            JobChangerNpc jobChanger = npcRoot.GetComponent<JobChangerNpc>();
            if (jobChanger != null)
            {
                Animator animator = jobChanger.GetComponent<Animator>();
                ApplyAnimatorPreview(animator, dir);
                EditorUtility.SetDirty(jobChanger);
            }
        }

        private static void ApplyAnimatorPreview(Animator animator, Vector2 dir)
        {
            if (animator == null)
            {
                return;
            }

            if (HasParameter(animator, "DirectionX"))
            {
                animator.SetFloat("DirectionX", dir.x);
            }

            if (HasParameter(animator, "DirectionY"))
            {
                animator.SetFloat("DirectionY", dir.y);
            }

            animator.Update(0f);
            EditorUtility.SetDirty(animator);
        }

        private static bool HasParameter(Animator animator, string parameterName)
        {
            if (animator == null)
            {
                return false;
            }

            foreach (AnimatorControllerParameter parameter in animator.parameters)
            {
                if (parameter.name == parameterName)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
