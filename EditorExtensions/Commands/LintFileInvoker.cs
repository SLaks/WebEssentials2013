﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;
using Microsoft.VisualStudio.Text;

namespace MadsKristensen.EditorExtensions
{
    ///<summary>Invokes a <see cref="LintReporter"/> for an open document in the editor, updating the results when the file is saved.</summary>
    internal class LintFileInvoker : IDisposable
    {
        private readonly LinterCreator _creator;
        private readonly ITextDocument _document;
        private LintReporter _runner;
        private bool _isDisposed;

        public LintFileInvoker(LinterCreator creator, ITextDocument document)
        {
            _creator = creator;
            _document = document;
            _document.FileActionOccurred += DocumentSavedHandler;
            _runner = _creator(_document.FilePath);

            if (_runner.Settings.LintOnSave)
            {
                Dispatcher.CurrentDispatcher.InvokeAsync(
                    () => _runner.RunCompiler().DontWait("linting " + _document.FilePath),
                    DispatcherPriority.ApplicationIdle
                );
            }
        }

        private void DocumentSavedHandler(object sender, TextDocumentFileActionEventArgs e)
        {
            if (!_runner.Settings.LintOnSave)
                return;

            ITextDocument document = (ITextDocument)sender;
            if (_isDisposed || document.TextBuffer == null)
                return;

            switch (e.FileActionType)
            {
                case FileActionTypes.ContentLoadedFromDisk:
                    break;
                case FileActionTypes.DocumentRenamed:
                    _runner.Dispose();
                    _runner = _creator(_document.FilePath);

                    goto case FileActionTypes.ContentSavedToDisk;
                case FileActionTypes.ContentSavedToDisk:
                    Dispatcher.CurrentDispatcher.InvokeAsync(
                        () => _runner.RunCompiler().DontWait("linting " + _document.FilePath),
                        DispatcherPriority.ApplicationIdle
                    );
                    break;
            }
        }

        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        public static Task RunOnAllFilesInProject(string extension, Func<string, LintReporter> runnerFactory)
        {
            string dir = ProjectHelpers.GetRootFolder();

            if (dir == null || !Directory.Exists(dir))
                return Task.FromResult(true);

            return Task.WhenAll(
                Directory.EnumerateFiles(dir, "*" + extension, SearchOption.AllDirectories)
                            .Select(f => runnerFactory(f).RunCompiler().HandleErrors("linting " + f))
            );
        }

        public void Dispose()
        {
            if (!_isDisposed)
                _runner.Dispose();

            _isDisposed = true;
        }
    }
    internal delegate LintReporter LinterCreator(string fileName);
}