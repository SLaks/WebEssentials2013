﻿using EnvDTE;
using EnvDTE80;
using Microsoft.CSS.Core;
using Microsoft.CSS.Editor;
using Microsoft.CSS.Editor.Intellisense;
using Microsoft.CSS.Editor.Schemas;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MadsKristensen.EditorExtensions
{
    internal class CssAddMissingStandard : CommandTargetBase
    {
        private DTE2 _dte;

        public CssAddMissingStandard(IVsTextView adapter, IWpfTextView textView)
            : base(adapter, textView, GuidList.guidCssCmdSet, PkgCmdIDList.addMissingStandard)
        {
            _dte = EditorExtensionsPackage.DTE;
        }

        protected override bool Execute(uint commandId, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            var selection = TextView.GetSelection("css");
            if (selection == null) return false;

            ITextBuffer buffer = selection.Value.Snapshot.TextBuffer;
            CssEditorDocument doc = CssEditorDocument.FromTextBuffer(buffer);
            ICssSchemaInstance rootSchema = CssSchemaManager.SchemaManager.GetSchemaRoot(null);

            StringBuilder sb = new StringBuilder(buffer.CurrentSnapshot.GetText());

            EditorExtensionsPackage.DTE.UndoContext.Open("Add Missing Standard Property");

            string result = AddMissingStandardDeclaration(sb, doc, rootSchema);
            Span span = new Span(0, buffer.CurrentSnapshot.Length);
            buffer.Replace(span, result);

            TextView.Selection.Select(new SnapshotSpan(selection.Value, 0), false);

            EditorExtensionsPackage.DTE.ExecuteCommand("Edit.FormatDocument");
            EditorExtensionsPackage.DTE.UndoContext.Close();

            return true;
        }

        private string AddMissingStandardDeclaration(StringBuilder sb, CssEditorDocument doc, ICssSchemaInstance rootSchema)
        {
            var visitor = new CssItemCollector<RuleBlock>(true);
            doc.Tree.StyleSheet.Accept(visitor);

            //var items = visitor.Items.Where(d => d.IsValid && d.IsVendorSpecific());
            foreach (RuleBlock rule in visitor.Items.Reverse())
            {
                HashSet<string> list = new HashSet<string>();
                foreach (Declaration dec in rule.Declarations.Where(d => d.IsValid && d.IsVendorSpecific()).Reverse())
                {
                    ICssSchemaInstance schema = CssSchemaManager.SchemaManager.GetSchemaForItem(rootSchema, dec);
                    ICssCompletionListEntry entry = VendorHelpers.GetMatchingStandardEntry(dec, schema);

                    if (entry != null && !list.Contains(entry.DisplayText) && !rule.Declarations.Any(d => d.PropertyName != null && d.PropertyName.Text == entry.DisplayText))
                    {
                        int index = dec.Text.IndexOf(":", StringComparison.Ordinal);
                        string standard = entry.DisplayText + dec.Text.Substring(index);

                        sb.Insert(dec.AfterEnd, standard);
                        list.Add(entry.DisplayText);
                    }
                }
            }

            return sb.ToString();
        }

        private string GetVendorDeclarations(IEnumerable<string> prefixes, Declaration declaration)
        {
            StringBuilder sb = new StringBuilder();
            string separator = true ? Environment.NewLine : " ";

            foreach (var entry in prefixes)
            {
                sb.Append(entry + declaration.Text + separator);
            }

            return sb.ToString();
        }

        protected override bool IsEnabled()
        {
            return TextView.GetSelection("css").HasValue;
        }
    }
}