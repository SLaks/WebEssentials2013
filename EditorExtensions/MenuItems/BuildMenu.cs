﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EnvDTE;
using EnvDTE80;
using MadsKristensen.EditorExtensions.Compilers;
using MadsKristensen.EditorExtensions.Optimization.Minification;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.Editor;
using Task = System.Threading.Tasks.Task;

namespace MadsKristensen.EditorExtensions
{
    internal class BuildMenu
    {
        private readonly DTE2 _dte;
        private readonly OleMenuCommandService _mcs;

        [Import]
        public IContentTypeRegistryService ContentTypes { get; set; }
        [Import]
        public ProjectCompiler Compiler { get; set; }

        public BuildMenu(DTE2 dte, OleMenuCommandService mcs)
        {
            Mef.SatisfyImportsOnce(this);
            _dte = dte;
            _mcs = mcs;
        }

        public void SetupCommands()
        {
            AddCommand(CommandId.BuildLess, ContentTypes.GetContentType("LESS"));
            AddCommand(CommandId.BuildSass, ContentTypes.GetContentType("SASS"));
            AddCommand(CommandId.BuildCoffeeScript, ContentTypes.GetContentType("CoffeeScript"));
            //TODO: Iced CoffeeScript?

            CommandID cmdBundles = new CommandID(CommandGuids.guidBuildCmdSet, (int)CommandId.BuildBundles);
            OleMenuCommand menuBundles = new OleMenuCommand((s, e) => UpdateBundleFiles(), cmdBundles);
            _mcs.AddCommand(menuBundles);

            CommandID cmdMinify = new CommandID(CommandGuids.guidBuildCmdSet, (int)CommandId.BuildMinify);
            OleMenuCommand menuMinify = new OleMenuCommand((s, e) => Task.Run(new Action(Minify)), cmdMinify);
            _mcs.AddCommand(menuMinify);

        }

        private void AddCommand(CommandId id, IContentType contentType)
        {
            var cid = new CommandID(CommandGuids.guidBuildCmdSet, (int)id);
            var command = new OleMenuCommand(async (s, e) =>
            {
                EditorExtensionsPackage.DTE.StatusBar.Text = "Compiling " + contentType + "...";
                await Task.Run(() => Compiler.CompileSolutionAsync(contentType));
                EditorExtensionsPackage.DTE.StatusBar.Clear();
            }, cid);
            _mcs.AddCommand(command);
        }

        public static void UpdateBundleFiles()
        {
            EditorExtensionsPackage.DTE.StatusBar.Text = "Updating bundles...";
            BundleFilesMenu.UpdateBundles(null, true);
            EditorExtensionsPackage.DTE.StatusBar.Clear();
        }

        private void Minify()
        {
            _dte.StatusBar.Text = "Web Essentials: Minifying files...";
            var extensions = new HashSet<string>(
                Mef.GetSupportedExtensions<IFileMinifier>(),
                StringComparer.OrdinalIgnoreCase
            );

            var files = ProjectHelpers.GetAllProjects()
                            .Select(ProjectHelpers.GetRootFolder)
                            .SelectMany(p => Directory.EnumerateFiles(p, "*", SearchOption.AllDirectories))
                            .Where(f => extensions.Contains(Path.GetExtension(f)));

            var extensionService = WebEditor.ExportProvider.GetExport<IFileExtensionRegistryService>();
            var minifyService = WebEditor.ExportProvider.GetExport<MinificationSaveListener>();

            // Perform expensive blocking work in parallel
            Parallel.ForEach(files, file =>
                minifyService.Value.ReMinify(
                    extensionService.Value.GetContentTypeForExtension(Path.GetExtension(file).TrimStart('.')),
                    file
                )
            );

            EditorExtensionsPackage.DTE.StatusBar.Clear();
        }
    }
}