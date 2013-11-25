﻿using EnvDTE80;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Web.Editor;

namespace MadsKristensen.EditorExtensions
{
    internal class CssSelectBrowsers : CommandTargetBase
    {
        private DTE2 _dte;
        private List<string> _possible = new List<string>() { ".CSS", ".LESS", ".SCSS" };

        public CssSelectBrowsers(IVsTextView adapter, IWpfTextView textView)
            : base(adapter, textView, GuidList.guidMinifyCmdSet, PkgCmdIDList.SelectBrowsers)
        {
            _dte = EditorExtensionsPackage.DTE;
        }

        protected override bool Execute(uint commandId, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            BrowserSelector selector = new BrowserSelector();
            selector.ShowDialog();

            return true;
        }

        protected override bool IsEnabled()
        {
            if (TextView.GetSelection("css") == null)
                return false;
            var item = _dte.Solution.FindProjectItem(_dte.ActiveDocument.FullName);
            return item != null && item.ContainingProject != null && !string.IsNullOrEmpty(item.ContainingProject.FullName);
        }
    }
}