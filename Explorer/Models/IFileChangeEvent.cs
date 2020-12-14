namespace Explorer.Models
{
    public interface IFileChangeEvent
    {
        void GetBack();

        void InvokeEvent();
    }
}
