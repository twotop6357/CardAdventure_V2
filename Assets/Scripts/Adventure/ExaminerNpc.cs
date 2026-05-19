using UnityEngine;

namespace CardAdventure
{
    [RequireComponent(typeof(NpcInteractable))]
    public class ExaminerNpc : MonoBehaviour, IDialogueEndHandler
    {
        [Header("Dialogue")]
        [SerializeField] private DialogueData questionDialogue;
        [SerializeField] private DialogueData intoBattleDialogue;
        [SerializeField] private DialogueData battleEndDialogue;

        [Header("Battle")]
        [SerializeField] private EnemyData bossEnemyData;
        [SerializeField] private BattleIntroData battleIntroData;
        [SerializeField] private string battleSceneReturnScene = "AdventureScene";

        private NpcInteractable interactable;

        private void Awake()
        {
            interactable = GetComponent<NpcInteractable>();
        }

        private System.Collections.IEnumerator Start()
        {
            RefreshDialogueState();
            
            // 매니저 초기화 타이밍 안정을 위해 프레임 딜레이를 준다.
            yield return null;
            yield return null;
            
            CheckAndTriggerAutoDialogue();
        }

        private void OnEnable()
        {
            RefreshDialogueState();
        }

        private void CheckAndTriggerAutoDialogue()
        {
            if (GameDataManager.Instance == null || DialogueManager.Instance == null)
            {
                return;
            }

            if (GameDataManager.Instance.ExaminerBattleCompleted && !GameDataManager.Instance.ExaminerDialogueCompleted)
            {
                if (battleEndDialogue != null && interactable != null)
                {
                    DialogueManager.Instance.BeginDialogueWithNpc(battleEndDialogue, interactable, true);
                }
            }
        }

        public bool TryHandleDialogueEnd(DialogueManager manager, DialogueData dialogueData)
        {
            if (manager == null)
            {
                return false;
            }

            if (dialogueData == questionDialogue && !IsBattleCompleted())
            {
                manager.ShowChoices("예", "아니오", HandleAcceptBattle, manager.FinishCurrentDialogue);
                return true;
            }

            if (dialogueData == intoBattleDialogue && !IsBattleCompleted())
            {
                manager.FinishCurrentDialogue();
                EnterBossBattle();
                return true;
            }

            if (dialogueData == battleEndDialogue)
            {
                manager.FinishCurrentDialogue();

                if (GameDataManager.Instance != null)
                {
                    GameDataManager.Instance.ExaminerDialogueCompleted = true;
                }

                // 대화 완료 시점에 캐릭터 위치 및 방향 백업
                PlayerController player = FindFirstObjectByType<PlayerController>();
                if (player != null && GameDataManager.Instance != null)
                {
                    GameDataManager.Instance.HasSavedPosition = true;
                    GameDataManager.Instance.SavedPosition = player.transform.position;
                    GameDataManager.Instance.SavedFacingDirection = player.FacingDirection;
                }

                // 세이브 파일 자동 저장
                SaveManager.SaveGame(SaveManager.CurrentSlotIndex);
                
                return true;
            }

            return false;
        }

        private void HandleAcceptBattle()
        {
            if (DialogueManager.Instance == null)
            {
                return;
            }

            if (intoBattleDialogue != null)
            {
                DialogueManager.Instance.BeginDialogueWithNpc(intoBattleDialogue, interactable);
            }
            else
            {
                DialogueManager.Instance.FinishCurrentDialogue();
                EnterBossBattle();
            }
        }

        private void EnterBossBattle()
        {
            if (bossEnemyData == null)
            {
                Debug.LogWarning("[ExaminerNpc] Boss enemy data is not assigned.", this);
                return;
            }

            if (battleIntroData != null)
            {
                bossEnemyData.introData = battleIntroData;
            }

            GameDataManager.Instance?.BeginExaminerBattle();

            if (SceneLoader.Instance != null)
            {
                SceneLoader.Instance.EnterBossBattle(bossEnemyData, battleSceneReturnScene);
            }
        }

        private void RefreshDialogueState()
        {
            if (interactable == null)
            {
                interactable = GetComponent<NpcInteractable>();
            }

            if (interactable == null)
            {
                return;
            }

            interactable.SetDialogueData(IsDialogueCompleted() || IsBattleCompleted() ? battleEndDialogue : questionDialogue);
        }

        private static bool IsBattleCompleted()
        {
            return GameDataManager.Instance != null && GameDataManager.Instance.ExaminerBattleCompleted;
        }

        private static bool IsDialogueCompleted()
        {
            return GameDataManager.Instance != null && GameDataManager.Instance.ExaminerDialogueCompleted;
        }
    }
}
