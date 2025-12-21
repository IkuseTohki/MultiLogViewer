namespace MultiLogViewer.Services
{
    public interface IUserDialogService
    {
        string? OpenFileDialog();
        void ShowError(string title, string message);
    }
}
