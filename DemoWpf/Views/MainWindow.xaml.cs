using System.Diagnostics;
using System.Windows.Input;

namespace DemoWpf.Views
{
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        // TODO: move this into the MVVM pattern
        // NB: copied from: https://bitbucket.org/mbassit/myyoutubedownloader/commits/e2b8371603ea2df28c6e2c9816c9383c2ff0b328
        private void HyperlinkPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Process.Start(@"C:\Users\Marco\Downloads\YoutubeExplode");   //NB: I had to use a 'preview' event, see http://stackoverflow.com/questions/32667648/wpf-ribbonsplitbutton-doesnt-fire-mousedown-or-mouseleftbuttondown-event
        }
    }
}