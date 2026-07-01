namespace _00_Work._01_Scripts.ChunkSystem.Chunk
{
    public class ChunkData
    {
        public const int Width     = 16;
        public const int Height    = 168;   // 128 + 40 (y -40 ~ 127)
        public const int YOffset   = 40;    // 월드 y → 배열 인덱스 변환: arrayY = worldY + YOffset

        public byte[] blocks = new byte[Width * Width * Height];

        public byte GetBlock(int x, int y, int z)
            => blocks[x + Width * ((y + YOffset) + Height * z)];

        public void SetBlock(int x, int y, int z, byte type)
            => blocks[x + Width * ((y + YOffset) + Height * z)] = type;
    }
}