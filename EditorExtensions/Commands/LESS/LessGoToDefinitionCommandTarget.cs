using CssSorter;
using EnvDTE;
using EnvDTE80;
using Microsoft.CSS.Core;
using Microsoft.CSS.Editor;
using Microsoft.Less.Core;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.IO;
using System.Linq;

namespace MadsKristensen.EditorExtensions
{
    internal class LessGoToDefinition : CommandTargetBase
    {
        private DTE2 _dte;
        private LessStyleSheet _sheet;

        public LessGoToDefinition(IVsTextView adapter, IWpfTextView textView)
            : base(adapter, textView, typeof(VSConstants.VSStd97CmdID).GUID, (uint)VSConstants.VSStd97CmdID.GotoDefn)
        {
            _dte = EditorExtensionsPackage.DTE;
        }

        protected override bool Execute(uint commandId, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            if (!EnsureInitialized())
                return false;

            return true;
        }

        public bool EnsureInitialized()
        {
            if (_sheet != null)
                return true;

            try
            {
                CssEditorDocument document = CssEditorDocument.FromTextBuffer(TextView.TextBuffer);
                _sheet = document.StyleSheet as LessStyleSheet;
            }
            catch (ArgumentNullException)
            { }

            return _sheet != null;
        }

        protected override bool IsEnabled()
        {
            return true;
        }
    }
}