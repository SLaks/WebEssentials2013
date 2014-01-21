﻿using System;
using System.ComponentModel.Composition;
using System.IO;
using MadsKristensen.EditorExtensions.Commands;
using Microsoft.VisualStudio.Utilities;

namespace MadsKristensen.EditorExtensions.Optimization.Minification
{
    [Export]    // This class is imported for its own methods, and as a general save listener
    [Export(typeof(IFileSaveListener))]
    [ContentType("HTMLX")]
    [ContentType("CSS")]
    [ContentType("JavaScript")]
    class MinificationSaveListener : IFileSaveListener
    {
        public void FileSaved(IContentType contentType, string path)
        {
            // This will also be called for derived ContentTypes like LESS & Markdown.  Ignore those.
            var settings = WESettings.Instance.ForContentType<IMinifierSettings>(contentType);
            if (settings == null || !settings.AutoMinify)
                return;
            ReMinify(contentType, path, settings);
        }
        ///<summary>Minifies an existing file if it should be minified.</summary>
        public void ReMinify(IContentType contentType, string path, IMinifierSettings settings = null)
        {
            // Don't minify ".min" files
            if (!ShouldMinify(path))
                return;
            if (!File.Exists(GetMinFileName(path)))
                return;

            MinifyFile(contentType, path, settings ?? WESettings.Instance.ForContentType<IMinifierSettings>(contentType));
        }
        public static string GetMinFileName(string path)
        {
            return path.Insert(path.Length - Path.GetExtension(path).Length, ".min");
        }
        public static bool ShouldMinify(string path)
        {
            var baseName = Path.GetFileNameWithoutExtension(path);
            return !baseName.EndsWith(".min", StringComparison.OrdinalIgnoreCase)
                && !baseName.EndsWith(".bundle", StringComparison.OrdinalIgnoreCase);
        }

        public void CreateMinFile(IContentType contentType, string sourcePath)
        {
            var settings = WESettings.Instance.ForContentType<IMinifierSettings>(contentType);
            MinifyFile(contentType, sourcePath, settings);

            var minPath = GetMinFileName(sourcePath);

            //TODO: Should be unnecessary: ProjectHelpers.AddFileToActiveProject(minPath);
            ProjectHelpers.AddFileToProject(sourcePath, minPath);
            if (File.Exists(minPath + ".map"))
                ProjectHelpers.AddFileToProject(sourcePath, minPath + ".map");
            if (File.Exists(minPath + ".map.gzip"))
                ProjectHelpers.AddFileToProject(sourcePath, minPath + ".map.gzip");
            if (settings.GzipMinifiedFiles)
                ProjectHelpers.AddFileToProject(sourcePath, minPath + ".gzip");
        }

        private void MinifyFile(IContentType contentType, string sourcePath, IMinifierSettings settings)
        {
            var minifier = Mef.GetImport<IFileMinifier>(contentType);

            var minPath = GetMinFileName(sourcePath);
            bool changed = minifier.MinifyFile(sourcePath, minPath);
            if (settings.GzipMinifiedFiles && (changed || !File.Exists(minPath + ".gzip")))
            {
                FileHelpers.GzipFile(minPath);
                if (minifier.GenerateSourceMap && File.Exists(minPath + ".map"))
                    FileHelpers.GzipFile(minPath + ".map");
            }
        }
    }
}
