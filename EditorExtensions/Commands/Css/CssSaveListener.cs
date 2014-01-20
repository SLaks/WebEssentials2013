﻿using System;
using System.ComponentModel.Composition;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType(Microsoft.Web.Editor.CssContentTypeDefinition.CssContentType)]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    public class CssSaveListener : IWpfTextViewCreationListener
    {
        //TODO: Move to common base class
        [Import]
        public ITextDocumentFactoryService TextDocumentFactoryService { get; set; }

        private ITextDocument _document;

        public void TextViewCreated(IWpfTextView textView)
        {
            if (TextDocumentFactoryService.TryGetTextDocument(textView.TextDataModel.DocumentBuffer, out _document))
            {
                _document.FileActionOccurred += document_FileActionOccurred;
            }
        }

        void document_FileActionOccurred(object sender, TextDocumentFileActionEventArgs e)
        {
            if (!WESettings.Instance.Css.MinifyOnSave)
                return;

            if (e.FileActionType == FileActionTypes.ContentSavedToDisk && e.FilePath.EndsWith(".css", StringComparison.OrdinalIgnoreCase))
            {
                string minFile = e.FilePath.Insert(e.FilePath.Length - 3, "min.");

                if (File.Exists(minFile) && ProjectHelpers.GetProjectItem(minFile) != null)
                {
                    Task.Run(() =>
                    {
                        Minify(e.FilePath, minFile);
                    });
                }
            }
        }

        public static void Minify(string file, string minFile)
        {
            if (file.EndsWith(".min.css", StringComparison.OrdinalIgnoreCase))
                return;

            try
            {
                string content = MinifyFileMenu.MinifyString(".css", File.ReadAllText(file));

                ProjectHelpers.CheckOutFileFromSourceControl(minFile);
                using (StreamWriter writer = new StreamWriter(minFile, false, new UTF8Encoding(true)))
                {
                    writer.Write(content);
                }

                if (WESettings.Instance.Css.GzipMinifiedFiles)
                    GzipFile(file, minFile, content);
            }
            catch
            {
                Logger.Log("Error minifying: " + file);
            }
        }

        public static void GzipFile(string file, string minFile, string content)
        {
            string gzipFile = minFile + ".gzip";
            ProjectHelpers.CheckOutFileFromSourceControl(gzipFile);
            byte[] gzipContent = Compress(content);
            File.WriteAllBytes(gzipFile, gzipContent);
            ProjectHelpers.AddFileToActiveProject(gzipFile);
            ProjectHelpers.AddFileToProject(file, gzipFile);
        }

        public static byte[] Compress(string text)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(text);

            using (MemoryStream ms = new MemoryStream())
            {
                using (GZipStream zip = new GZipStream(ms, CompressionMode.Compress))
                {
                    zip.Write(buffer, 0, buffer.Length);
                }

                return ms.ToArray();
            }
        }
    }
}
