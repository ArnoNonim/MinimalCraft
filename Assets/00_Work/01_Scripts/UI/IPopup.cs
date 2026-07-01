namespace _00_Work._01_Scripts.UI
{
    public interface IPopup
    {
        bool IsOpen { get; }
        void Open();
        void Close();
        void Toggle();
    }
}