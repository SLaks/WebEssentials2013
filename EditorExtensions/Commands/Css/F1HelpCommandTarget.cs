﻿using EnvDTE80;
using Microsoft.CSS.Core;
using Microsoft.CSS.Editor;
using Microsoft.CSS.Editor.Intellisense;
using Microsoft.CSS.Editor.Schemas;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using Microsoft.VisualStudio.Text;

namespace MadsKristensen.EditorExtensions
{
    internal class F1Help : CommandTargetBase
    {
        public F1Help(IVsTextView adapter, IWpfTextView textView)
            : base(adapter, textView, typeof(VSConstants.VSStd97CmdID).GUID, (uint)VSConstants.VSStd97CmdID.F1Help)
        { }

        protected override bool Execute(uint commandId, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            var selection = TextView.GetSelection("css");
            if (selection == null)
                return false;
            var tree = CssEditorDocument.FromTextBuffer(selection.Value.Snapshot.TextBuffer);
            ParseItem item = tree.StyleSheet.ItemBeforePosition(selection.Value);

            if (item == null)
                return false;

            return SchemaLookup(item, selection.Value.Snapshot.TextBuffer);
        }

        private delegate ICssCompletionListEntry Reference(string name);

        private bool SchemaLookup(ParseItem item, ITextBuffer buffer)
        {
            if (item is ClassSelector || item is IdSelector || item is ItemName || item.Parent is RuleBlock || item.Parent is StyleSheet)
                return false;

            ICssSchemaInstance schema = CssSchemaManager.SchemaManager.GetSchemaRootForBuffer(buffer);

            Declaration dec = item.FindType<Declaration>();
            if (dec != null && dec.PropertyName != null)
                return OpenReferenceUrl(schema.GetProperty, dec.PropertyName.Text, "http://realworldvalidator.com/css/properties/");

            PseudoClassFunctionSelector pseudoClassFunction = item.FindType<PseudoClassFunctionSelector>();
            if (pseudoClassFunction != null)
                return OpenReferenceUrl(schema.GetPseudo, pseudoClassFunction.Colon.Text + pseudoClassFunction.Function.FunctionName.Text + ")", "http://realworldvalidator.com/css/pseudoclasses/");

            PseudoElementFunctionSelector pseudoElementFunction = item.FindType<PseudoElementFunctionSelector>();
            if (pseudoElementFunction != null)
                return OpenReferenceUrl(schema.GetPseudo, pseudoElementFunction.DoubleColon.Text + pseudoElementFunction.Function.FunctionName.Text + ")", "http://realworldvalidator.com/css/pseudoelements/");

            PseudoElementSelector pseudoElement = item.FindType<PseudoElementSelector>();
            if (pseudoElement != null && pseudoElement.PseudoElement != null)
                return OpenReferenceUrl(schema.GetPseudo, pseudoElement.DoubleColon.Text + pseudoElement.PseudoElement.Text, "http://realworldvalidator.com/css/pseudoelements/");

            PseudoClassSelector pseudoClass = item.FindType<PseudoClassSelector>();
            if (pseudoClass != null && pseudoClass.PseudoClass != null)
                return OpenReferenceUrl(schema.GetPseudo, pseudoClass.Colon.Text + pseudoClass.PseudoClass.Text, "http://realworldvalidator.com/css/pseudoclasses/");

            AtDirective directive = item.FindType<AtDirective>();
            if (directive != null)
                return OpenReferenceUrl(schema.GetAtDirective, directive.At.Text + directive.Keyword.Text, "http://realworldvalidator.com/css/atdirectives/");

            return false;
        }

        private bool OpenReferenceUrl(Reference reference, string name, string baseUrl)
        {
            ICssCompletionListEntry entry = reference.Invoke(name);
            if (entry != null)
            {
                string text = entry.DisplayText;
                Uri url;

                if (Uri.TryCreate(baseUrl + text, UriKind.Absolute, out url))
                {
                    System.Diagnostics.Process.Start(url.ToString());
                    return true;
                }
            }

            return false;
        }

        protected override bool IsEnabled()
        {
            return TextView.GetSelection("css").HasValue;
        }
    }
}