﻿using System.IO;
using System.Linq;
using EnvDTE;
using Microsoft.VisualStudio.Text;

namespace MadsKristensen.EditorExtensions
{
    internal class CoffeeScriptMargin : MarginBase
    {
        private static NodeExecutorBase _compiler = new CoffeeScriptCompiler();

        protected virtual string ServiceName { get { return "CoffeeScript"; } }
        protected virtual NodeExecutorBase Compiler { get { return _compiler; } }

        public CoffeeScriptMargin(string contentType, string source, ITextDocument document)
            : base(source, contentType, WESettings.Instance.CoffeeScript, document)
        { }

        protected override async void StartCompiler(string source)
        {
            string sourceFilePath = Document.FilePath;
            string jsFileName = GetCompiledFileName(sourceFilePath, ".js", CompileToLocation);

            if (!IsSaveFileEnabled)
                jsFileName = Path.GetTempFileName();

            if (IsFirstRun && File.Exists(jsFileName))
            {
                OnCompilationDone(File.ReadAllText(jsFileName), sourceFilePath);
                return;
            }

            Logger.Log(ServiceName + ": Compiling " + Path.GetFileName(sourceFilePath));

            var result = await Compiler.CompileAsync(sourceFilePath, jsFileName);

            if (result.IsSuccess)
            {
                OnCompilationDone(result.Result, result.SourceFileName);
            }
            else
            {
                result.Errors.First().Message = ServiceName + ": " + result.Errors.First().Message;

                CreateTask(result.Errors.First());

                base.OnCompilationDone("ERROR:" + result.Errors.First().Message, sourceFilePath);
            }
        }

        protected override void MinifyFile(string fileName, string source)
        {
            if (!IsSaveFileEnabled)
                return;

            if (WESettings.GetBoolean(WESettings.Keys.CoffeeScriptMinify))
            {
                FileHelpers.MinifyFile(fileName, source, ".js");
            }
        }

        public override string CompileToLocation
        {
            get { return WESettings.GetString(WESettings.Keys.CoffeeScriptCompileToLocation); }
        }

        public override bool IsSaveFileEnabled
        {
            get { return WESettings.GetBoolean(WESettings.Keys.GenerateJsFileFromCoffeeScript); }
        }
    }
}