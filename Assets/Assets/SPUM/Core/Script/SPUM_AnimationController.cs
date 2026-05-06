// SPUM - 2D Pixel Unit Maker
// SPUM_AnimationController: 애니메이션 재생 제어 유틸리티 클래스
using System.Collections;
using UnityEngine;

public class SPUM_AnimationController : MonoBehaviour
{
    [Header("Animation Settings")]
    public Animator animator;
    public float playbackSpeed = 1f;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    /// <summary>
    /// 지정한 애니메이션 클립을 재생합니다.
    /// </summary>
    public void PlayAnimation(string stateName, int layer = 0)
    {
        if (animator == null) return;
        animator.speed = playbackSpeed;
        animator.Play(stateName, layer);
    }

    /// <summary>
    /// 애니메이션 재생 속도를 설정합니다.
    /// </summary>
    public void SetSpeed(float speed)
    {
        playbackSpeed = speed;
        if (animator != null)
            animator.speed = speed;
    }

    /// <summary>
    /// 애니메이션의 정규화된 재생 위치를 설정합니다 (0~1).
    /// </summary>
    public void SetNormalizedTime(float normalizedTime, int layer = 0)
    {
        if (animator == null) return;
        var stateInfo = animator.GetCurrentAnimatorStateInfo(layer);
        animator.speed = 0f;
        animator.Play(stateInfo.shortNameHash, layer, normalizedTime);
        animator.Update(0f);
    }

    /// <summary>
    /// 현재 재생 중인 애니메이션 클립의 길이를 반환합니다 (초).
    /// </summary>
    public float GetClipLength(int layer = 0)
    {
        if (animator == null) return 0f;
        var clips = animator.GetCurrentAnimatorClipInfo(layer);
        if (clips.Length == 0) return 0f;
        return clips[0].clip.length;
    }

    /// <summary>
    /// 애니메이션을 일시정지합니다.
    /// </summary>
    public void Pause()
    {
        if (animator != null) animator.speed = 0f;
    }

    /// <summary>
    /// 일시정지된 애니메이션을 재개합니다.
    /// </summary>
    public void Resume()
    {
        if (animator != null) animator.speed = playbackSpeed;
    }
}
