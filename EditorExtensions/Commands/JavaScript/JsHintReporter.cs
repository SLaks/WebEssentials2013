﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace MadsKristensen.EditorExtensions
{
    internal class JsHintReporter : LintReporter
    {
        public JsHintReporter(string fileName) : base(new JsHintCompiler(), WESettings.Instance.JavaScript, fileName) { }

        public override Task RunCompiler()
        {
            if (ShouldIgnore(FileName))
            {
                // In case this file was ignored after it was opened (which is unlikely), clear the existing errors.
                _provider.Tasks.Clear();
                return Task.FromResult(true);
            }
            return base.RunCompiler();
        }

        public static bool ShouldIgnore(string file)
        {
            if (!Path.GetExtension(file).Equals(".js", StringComparison.OrdinalIgnoreCase) ||
                file.EndsWith(".min.js", StringComparison.OrdinalIgnoreCase) ||
                file.EndsWith(".debug.js", StringComparison.OrdinalIgnoreCase) ||
                file.EndsWith(".intellisense.js", StringComparison.OrdinalIgnoreCase) ||
                file.Contains("-vsdoc.js") ||
                !File.Exists(file))
            {
                return true;
            }

            ProjectItem item = ProjectHelpers.GetProjectItem(file);

            if (item == null)
                return true;

            // Ignore files nested under other files such as bundle or TypeScript output
            ProjectItem parent = item.Collection.Parent as ProjectItem;
            if (parent != null && !Directory.Exists(parent.FileNames[1]) || File.Exists(item.FileNames[1] + ".bundle"))
                return true;

            string name = Path.GetFileName(file);
            return _builtInIgnoreRegex.IsMatch(name);
        }

        private static Regex _builtInIgnoreRegex = new Regex("(" + String.Join(")|(", new[] {
            @"_references\.js",
            @"amplify\.js",
            @"angular\.js",
            @"backbone\.js",
            @"bootstrap\.js",
            @"dojo\.js",
            @"ember\.js",
            @"ext-core\.js",
            @"handlebars.*",
            @"highlight\.js",
            @"history\.js",
            @"jquery-([0-9\.]+)\.js",
            @"jquery.blockui.*",
            @"jquery.validate.*",
            @"jquery.unobtrusive.*",
            @"jquery-ui-([0-9\.]+)\.js",
            @"json2\.js",
            @"knockout-([0-9\.]+)\.js",
            @"MicrosoftAjax([a-z]+)\.js",
            @"modernizr-([0-9\.]+)\.js",
            @"mustache.*",
            @"prototype\.js ",
            @"qunit-([0-9a-z\.]+)\.js",
            @"require\.js",
            @"respond\.js",
            @"sammy\.js",
            @"scriptaculous\.js ",
            @"swfobject\.js",
            @"underscore\.js",
            @"webfont\.js",
            @"yepnope\.js",
            @"zepto\.js",
        }) + ")", RegexOptions.IgnoreCase);
    }
}