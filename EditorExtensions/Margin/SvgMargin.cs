using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace MadsKristensen.EditorExtensions
{
    internal class SvgMargin : MarginBase
    {
        private WebBrowser _browser;
        private string _fileName;

        protected override bool CanWriteToDisk
        {
            get { return false; }
        }

        public SvgMargin(string contentType, string source, ITextDocument document)
            : base(source, contentType, WESettings.Instance.General, document)
        {
            _fileName = document.FilePath;
        }

        protected override void StartCompiler(string source)
        {
            if (_browser != null && File.Exists(_fileName))
            {
                _browser.Navigate(_fileName);
            }
        }

        // TODO: Extract browser, override new TControl method, eliminate
        protected override void CreateControls(IWpfTextViewHost host, string source)
        {
            int width;
            using (var key = EditorExtensionsPackage.Instance.UserRegistryRoot)
            {
                var raw = key.GetValue("WE_SvgMargin");
                width = raw != null ? (int)raw : -1;
            }
            width = width == -1 ? 400 : width;

            _browser = new WebBrowser();
            _browser.HorizontalAlignment = HorizontalAlignment.Stretch;

            Grid grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(0, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(5, GridUnitType.Pixel) });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(width) });
            grid.RowDefinitions.Add(new RowDefinition());

            grid.Children.Add(_browser);
            this.Children.Add(grid);

            Grid.SetColumn(_browser, 2);
            Grid.SetRow(_browser, 0);

            GridSplitter splitter = new GridSplitter();
            splitter.Width = 5;
            splitter.ResizeDirection = GridResizeDirection.Columns;
            splitter.VerticalAlignment = System.Windows.VerticalAlignment.Stretch;
            splitter.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
            splitter.DragCompleted += splitter_DragCompleted;

            grid.Children.Add(splitter);
            Grid.SetColumn(splitter, 1);
            Grid.SetRow(splitter, 0);
        }

        // TODO: Eliminate everything below this line with extreme prejudice
        void splitter_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            using (var key = EditorExtensionsPackage.Instance.UserRegistryRoot)
            {
                key.SetValue("WE_SvgMargin", (int)_viewHost.HostControl.ActualWidth);
            }
        }

        protected override void MinifyFile(string fileName, string source)
        {
            // Nothing to minify
        }

        public override bool IsSaveFileEnabled
        {
            get { return false; }
        }

        public override string CompileToLocation
        {
            get { return string.Empty; }
        }
    }
}