using System.Windows.Forms;

namespace ChessEngine.UI;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new ChessBoard());
    }
}