﻿using Microsoft.Html.Core;
using Microsoft.Html.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace MadsKristensen.EditorExtensions
{
    internal class ImageHtmlQuickInfo : IQuickInfoSource
    {
        public void AugmentQuickInfoSession(IQuickInfoSession session, IList<object> qiContent, out ITrackingSpan applicableToSpan)
        {
            applicableToSpan = null;

            SnapshotPoint? point = session.GetTriggerPoint(session.TextView.TextBuffer.CurrentSnapshot);

            if (!point.HasValue)
                return;

            HtmlEditorTree tree = HtmlEditorDocument.FromTextView(session.TextView).HtmlEditorTree;

            ElementNode node = null;
            AttributeNode attr = null;

            tree.GetPositionElement(point.Value.Position, out node, out attr);

            if (attr == null || (attr.Name != "href" && attr.Name != "src"))
                return;

            string url = ImageQuickInfo.GetFileName(attr.Value.Trim('\'', '"').TrimStart('~'), session.TextView.TextBuffer);
            if (string.IsNullOrEmpty(url))
                return;

            applicableToSpan = session.TextView.TextBuffer.CurrentSnapshot.CreateTrackingSpan(point.Value.Position, 1, SpanTrackingMode.EdgeNegative);

            var image = ImageQuickInfo.CreateImage(url);
            qiContent.Add(image);

            if (image.Tag == null)
                qiContent.Add(Math.Round(image.Source.Width) + " × " + Math.Round(image.Source.Height));
            else
                qiContent.Add(image.Tag);
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}
