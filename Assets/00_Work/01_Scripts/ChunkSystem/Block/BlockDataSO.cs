using _00_Work._01_Scripts.ChunkSystem.Block;
using UnityEngine;

[System.Serializable]
public class BlockTextureData
{
    public string    blockName;
    public Vector2Int topFace;
    public Vector2Int sideFace;
    public Vector2Int bottomFace;

    [Header("채굴")]
    public int   hardness       = 1;    // 블록 체력
    public float miningSpeedMul = 1f;   // 채굴 속도 배수
    public ToolType requiredTool = ToolType.None; // 필요 도구
    public int   requiredToolLevel = 0;
}

public enum ToolType
{
    None,      // 맨손
    Pickaxe,   // 곡괭이
    Axe,       // 도끼
    Shovel,    // 삽
}

[CreateAssetMenu(fileName = "BlockData", menuName = "Block/BlockData")]
public class BlockDataSO : ScriptableObject
{
    public Texture2D atlas;        // 아틀라스 텍스처
    public Material atlasMaterial;
    public Material leavesMaterial;
    public Material waterMaterial;
    public int atlasSize = 4;      // 4x4
    public BlockTextureData[] blocks;

    public Vector2[] GetUVs(BlockType type, int faceIndex)
    {
        int index = (int)type;
        if (index <= 0 || index >= blocks.Length)
            return GetUVsFromAtlas(Vector2Int.zero);

        var data = blocks[index];

        Vector2Int atlasPos;
        if (faceIndex == 0)      atlasPos = data.topFace;
        else if (faceIndex == 1) atlasPos = data.bottomFace;
        else                     atlasPos = data.sideFace;

        return GetUVsFromAtlas(atlasPos);
    }

    Vector2[] GetUVsFromAtlas(Vector2Int atlasPos)
    {
        float tileSize = 1f / atlasSize;
        float x = atlasPos.x * tileSize;
        float y = 1f - (atlasPos.y + 1) * tileSize;

        return new Vector2[]
        {
            new Vector2(x,            y),
            new Vector2(x + tileSize, y),
            new Vector2(x + tileSize, y + tileSize),
            new Vector2(x,            y + tileSize),
        };
    }
}