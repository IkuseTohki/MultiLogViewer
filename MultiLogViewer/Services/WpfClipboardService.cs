using System.Windows;

namespace MultiLogViewer.Services
{
    public class WpfClipboardService : IClipboardService
    {
        public void SetText(string text)
        {
            if (string.IsNullOrEmpty(text)) return;

            // クリップボードが他アプリにロックされている場合を考慮し、リトライを行う
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    Clipboard.SetText(text);
                    return;
                }
                catch (System.Runtime.InteropServices.COMException)
                {
                    System.Threading.Thread.Sleep(50);
                }
            }
        }

        public string? GetText()
        {
            if (Clipboard.ContainsText())
            {
                return Clipboard.GetText();
            }
            return null;
        }
    }
}
