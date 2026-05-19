namespace CardAdventure
{
    public interface IDialogueEndHandler
    {
        bool TryHandleDialogueEnd(DialogueManager manager, DialogueData dialogueData);
    }
}
