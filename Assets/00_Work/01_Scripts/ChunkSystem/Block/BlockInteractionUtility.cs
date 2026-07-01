namespace _00_Work._01_Scripts.ChunkSystem.Block
{
    public static class BlockInteractionUtility
    {
        public static bool IsInteractable(
            BlockType blockType)
        {
            return blockType switch
            {
                BlockType.Workbench => true,
                BlockType.Furnace   => true,
                _ => false
            };
        }

        public static string GetPromptText(
            BlockType blockType)
        {
            return blockType switch
            {
                BlockType.Workbench => "작업대 사용",
                BlockType.Furnace   => "화로 사용",
                _ => string.Empty
            };
        }
    }
}