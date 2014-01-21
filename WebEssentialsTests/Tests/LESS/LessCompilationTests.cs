﻿using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using FluentAssertions;
using MadsKristensen.EditorExtensions;
using MadsKristensen.EditorExtensions.Compilers;
using Microsoft.CSS.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WebEssentialsTests
{
    [TestClass]
    public class LessCompilationTests
    {
        private static string originalPath;
        private static readonly string BaseDirectory = Path.GetDirectoryName(typeof(NodeModuleImportedTests).Assembly.Location);

        #region Helper Methods
        private static async Task<string> CompileLess(string fileName, string targetFileName)
        {
            var result = await new LessCompiler().CompileAsync(fileName, targetFileName);

            if (result.IsSuccess)
            {
                File.WriteAllText(targetFileName, File.ReadAllText(targetFileName).Trim());

                return result.Result;
            }
            else
            {
                throw new ExternalException(result.Errors.First().Message);
            }
        }
        #endregion

        [ClassInitialize]
        public static void ObscureNode(TestContext context)
        {
            SettingsStore.EnterTestMode();
            originalPath = Environment.GetEnvironmentVariable("PATH");
            Environment.SetEnvironmentVariable("PATH", originalPath.Replace(@";C:\Program Files\nodejs\", ""), EnvironmentVariableTarget.Process);
        }

        [ClassCleanup]
        public static void RestoreNode()
        {
            Environment.SetEnvironmentVariable("PATH", originalPath);
        }

        [TestMethod]
        public async Task PathCompilationTest()
        {
            var sourcePath = Path.Combine(BaseDirectory, "fixtures\\less");
            foreach (var lessFilename in Directory.EnumerateFiles(sourcePath, "*.less", SearchOption.AllDirectories))
            {
                var compiledFile = Path.ChangeExtension(lessFilename, ".css");
                var compiled = await CompileLess(lessFilename, compiledFile);
                var expected = File.ReadAllText(compiledFile)
                               .Replace("\r", "");

                compiled.Trim().Should().Be(expected.Trim());
            }
        }

        [TestMethod]
        public async Task PathNormalizationTest()
        {
            foreach (var lessFilename in Directory.EnumerateFiles(Path.Combine(BaseDirectory, "fixtures\\less"), "*.less", SearchOption.AllDirectories))
            {
                var expectedPath = Path.Combine(Path.GetDirectoryName(lessFilename), "css", Path.ChangeExtension(lessFilename, ".css"));

                if (!File.Exists(expectedPath))
                    continue;

                var compiled = await CompileLess(lessFilename, expectedPath);
                var expected = File.ReadAllText(expectedPath);

                compiled = new CssFormatter().Format(compiled).Replace("\r", "");
                expected = new CssFormatter().Format(expected).Replace("\r", "");

                compiled.Should().Be(expected);
            }
        }
    }
}
