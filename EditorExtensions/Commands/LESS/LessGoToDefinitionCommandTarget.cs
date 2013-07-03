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

            int position = TextView.Caret.Position.BufferPosition.Position;
            ParseItem item = _sheet.ItemBeforePosition(position);

            var variable = item.FindAncestor<LessVariableReference>();
            if (variable != null)
            {
                if (variable.IsArgumentsVariable())
                    return false;
                if (variable.IsInMixinBody())
                {
                    // TODO: Find a RuleSet that has a LessMixinDeclaration as a SubSelector
                    var parentMixin = variable.FindAncestor<LessMixinDeclaration>();
                    var argDef = parentMixin.Arguments.FirstOrDefault(a => a.Variable.VariableName.Name.Text == variable.Variable.Name.Text);
                    GoTo(argDef);
                    return true;
                }

                var def = _sheet.Variables.FirstOrDefault(d => d.VariableName.Name.Text == variable.Variable.Name.Text);
                if (def == null)
                    return false;
                GoTo(def);
                return true;
            }

            var mixin = item.FindAncestor<LessMixinReference>();
            if (mixin != null)
            {
                var subSelectors = _sheet.RuleSets.SelectMany(s => s.Selectors).SelectMany(s => s.SimpleSelectors).SelectMany(s => s.SubSelectors);
                var def = subSelectors.OfType<LessMixinDeclaration>().FirstOrDefault(d => d.MixinName.Name == mixin.MixinName.Name)
                        ?? subSelectors.FirstOrDefault(d => d.Text == mixin.MixinName.Name);  // Normal classes can also be used as mixins
                if (def == null)
                    return false;
                GoTo(def);
                return true;
            }

            return true;
        }

        private void GoTo(ParseItem item)
        {
            //TODO: How do I open the preview tab?
            System.Windows.MessageBox.Show(item.Text + "\r\n" + item.Range.Start);
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