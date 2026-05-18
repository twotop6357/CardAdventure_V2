using UnityEngine;

namespace CardAdventure
{
    /// <summary>
    /// Stores and applies a placed NPC's initial facing direction.
    /// Editor tools can set this component so the direction is saved in the scene.
    /// </summary>
    [DisallowMultipleComponent]
    public class NpcFacingDirection : MonoBehaviour
    {
        public enum Facing
        {
            Down,
            Up,
            Left,
            Right
        }

        [SerializeField] private Facing initialFacing = Facing.Down;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Animator animator;
        [SerializeField] private bool invertVisualFlip;

        [Header("Common Idle State Names")]
        [SerializeField] private string idleDownState = "MaleNPC_IdleDown";
        [SerializeField] private string idleSideState = "MaleNPC_IdleSide";
        [SerializeField] private string idleUpState = "MaleNPC_IdleBack";

        public Facing InitialFacing
        {
            get => initialFacing;
            set
            {
                initialFacing = value;
                Apply();
            }
        }

        public Vector2 Direction => ToVector(initialFacing);

        private void Reset()
        {
            ResolveReferences();
        }

        private void Awake()
        {
            ResolveReferences();
            Apply();
        }

        private void Start()
        {
            Apply();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ResolveReferences();
            if (!Application.isPlaying)
            {
                Apply();
            }
        }
#endif

        public void Apply()
        {
            ResolveReferences();
            Vector2 dir = ToVector(initialFacing);

            ApplyAnimatorParameters(dir);
            ApplyAnimatorState(dir);
            ApplySpriteFlip(dir);
        }

        public void SetFacing(Facing facing)
        {
            initialFacing = facing;
            Apply();
        }

        public static Vector2 ToVector(Facing facing)
        {
            switch (facing)
            {
                case Facing.Up:
                    return Vector2.up;
                case Facing.Left:
                    return Vector2.left;
                case Facing.Right:
                    return Vector2.right;
                case Facing.Down:
                default:
                    return Vector2.down;
            }
        }

        private void ResolveReferences()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>(true);
            }

            if (animator == null)
            {
                animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>(true);
            }

            NpcTileAlignment alignment = GetComponent<NpcTileAlignment>();
            if (alignment != null)
            {
                invertVisualFlip = alignment.invertVisualFlip;
            }
            else
            {
                NpcMovement movement = GetComponent<NpcMovement>();
                if (movement != null)
                {
                    invertVisualFlip = movement.invertVisualFlip;
                }
            }
        }

        private void ApplyAnimatorParameters(Vector2 dir)
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
        }

        private void ApplyAnimatorState(Vector2 dir)
        {
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                return;
            }

            string stateName = dir.y > 0f ? idleUpState : dir.y < 0f ? idleDownState : idleSideState;
            if (string.IsNullOrWhiteSpace(stateName))
            {
                return;
            }

            if (!animator.HasState(0, Animator.StringToHash(stateName)))
            {
                return;
            }

            animator.Play(stateName, 0, 0f);
            animator.Update(0f);
        }

        private void ApplySpriteFlip(Vector2 dir)
        {
            if (spriteRenderer == null || Mathf.Abs(dir.x) < 0.01f)
            {
                return;
            }

            bool flip = dir.x > 0f;
            if (invertVisualFlip)
            {
                flip = !flip;
            }

            spriteRenderer.flipX = flip;
        }

        private static bool HasParameter(Animator targetAnimator, string parameterName)
        {
            if (targetAnimator == null)
            {
                return false;
            }

            foreach (AnimatorControllerParameter parameter in targetAnimator.parameters)
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
