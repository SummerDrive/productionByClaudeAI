using System.Windows;

namespace StepSeq;

public partial class App : Application
{
    protected override void OnExit(ExitEventArgs e)
    {
        Audio.AudioPlayer.Instance.Dispose();
        base.OnExit(e);
    }
}
