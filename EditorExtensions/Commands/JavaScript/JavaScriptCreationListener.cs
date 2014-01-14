﻿using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(IVsTextViewCreationListener))]
    [ContentType("JavaScript")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    public class JavaScriptSortPropertiesViewCreationListener : IVsTextViewCreationListener
    {
        [Import]
        public IVsEditorAdaptersFactoryService EditorAdaptersFactoryService { get; set; }

        [Import(typeof(ITextStructureNavigatorSelectorService))]
        public ITextStructureNavigatorSelectorService Navigator { get; set; }

        [Import]
        public ITextDocumentFactoryService TextDocumentFactoryService { get; set; }

        [Import]
        public ICompletionBroker CompletionBroker { get; set; }

        [Import]
        internal IClassifierAggregatorService AggregatorService;

        public void VsTextViewCreated(IVsTextView textViewAdapter)
        {
            var textView = EditorAdaptersFactoryService.GetWpfTextView(textViewAdapter);

            textView.Properties.GetOrCreateSingletonProperty(() => new MinifySelection(textViewAdapter, textView));
            textView.Properties.GetOrCreateSingletonProperty(() => new JavaScriptFindReferences(textViewAdapter, textView, Navigator));
            textView.Properties.GetOrCreateSingletonProperty(() => new CssExtractToFile(textViewAdapter, textView));
            textView.Properties.GetOrCreateSingletonProperty(() => new NodeModuleGoToDefinition(textViewAdapter, textView));
            textView.Properties.GetOrCreateSingletonProperty(() => new ReferenceTagGoToDefinition(textViewAdapter, textView));
            textView.Properties.GetOrCreateSingletonProperty(() => new CommentCompletionCommandTarget(textViewAdapter, textView, AggregatorService));
            textView.Properties.GetOrCreateSingletonProperty(() => new CommentIndentationCommandTarget(textViewAdapter, textView, AggregatorService, CompletionBroker));

            ITextDocument document;
            if (TextDocumentFactoryService.TryGetTextDocument(textView.TextDataModel.DocumentBuffer, out document))
            {
                var lintInvoker = new LintFileInvoker(f => new JsHintReporter(f), document);
                textView.Closed += (s, e) => lintInvoker.Dispose();

                textView.TextBuffer.Properties.GetOrCreateSingletonProperty(() => lintInvoker);
            }
        }
    }
}

