﻿using System.IO;

namespace MadsKristensen.EditorExtensions
{
    public class IcedCoffeeScriptCompiler : CoffeeScriptCompiler
    {
        private static readonly string _compilerPath = Path.Combine(WebEssentialsResourceDirectory, @"nodejs\tools\node_modules\iced-coffee-script\bin\coffee");

        public override string ServiceName
        {
            get { return "IcedCoffeeScript"; }
        }
        protected override string CompilerPath
        {
            get { return _compilerPath; }
        }

        protected override string GetArguments(string sourceFileName, string targetFileName)
        {
            return "--runtime inline " + base.GetArguments(sourceFileName, targetFileName);
        }
    }
}