using System;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio;

namespace GitScc
{
    /// <summary>
    /// Summary description for SccProviderToolWindow.
    /// </summary>
    [Guid("75EDECF4-68D8-4B7B-92A9-5915461DA6D9")]
    public class PendingChangesToolWindow : ToolWindowWithEditor
    {
        private SccProviderService sccProviderService;

        public PendingChangesToolWindow()
        {
            // set the window title
            this.Caption = Resources.ResourceManager.GetString("PendingChangesToolWindowCaption");

            //// set the CommandID for the window ToolBar
            base.ToolBar = new CommandID(GuidList.guidSccProviderCmdSet, CommandId.imnuPendingChangesToolWindowToolbarMenu);

            // set the icon for the frame
            this.BitmapResourceID = CommandId.ibmpToolWindowsImages;  // bitmap strip resource ID
            this.BitmapIndex = CommandId.iconSccProviderToolWindow;   // index in the bitmap strip
        }

        protected override void Initialize()
        {
            base.Initialize();
            control = new PendingChangesView(this);

            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on 
            // the object returned by the Content property.
            base.Content = control;

            OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;

            var cmd = new CommandID(GuidList.guidSccProviderCmdSet, CommandId.icmdPendingChangesCommit);
            var menu = new MenuCommand(new EventHandler(OnCommitCommand), cmd);
            mcs.AddCommand(menu);

            cmd = new CommandID(GuidList.guidSccProviderCmdSet, CommandId.icmdPendingChangesAmend);
            menu = new MenuCommand(new EventHandler(OnAmendCommitCommand), cmd);
            mcs.AddCommand(menu);

            cmd = new CommandID(GuidList.guidSccProviderCmdSet, CommandId.icmdPendingChangesRefresh);
            menu = new MenuCommand(new EventHandler(OnRefreshCommand), cmd);
            mcs.AddCommand(menu);

            sccProviderService = BasicSccProvider.GetServiceEx<SccProviderService>();

            Refresh(sccProviderService.CurrentTracker, true); // refresh when the tool window becomes visible
        }

        private void OnCommitCommand(object sender, EventArgs e)
        {
            ((PendingChangesView) control).Commit();
        }

        private void OnAmendCommitCommand(object sender, EventArgs e)
        {
            ((PendingChangesView) control).AmendCommit();
        }

        private void OnRefreshCommand(object sender, EventArgs e)
        {
            sccProviderService.OpenTracker();
            sccProviderService.RefreshNodesGlyphs();
            Refresh(sccProviderService.CurrentTracker, true);
        }

        internal void Refresh(GitFileStatusTracker tracker, bool force = false)
        {
            //var frame = this.Frame as IVsWindowFrame;
            //if (frame == null || frame.IsVisible() == 1) return;

            try
            {
                var repository = (tracker == null || !tracker.HasGitRepository) ? "" :
                    string.Format(" - {0}", tracker.CurrentBranch, tracker.GitWorkingDirectory);

                this.Caption = Resources.ResourceManager.GetString("PendingChangesToolWindowCaption") + repository;

                if (!GitSccOptions.Current.DisableAutoRefresh || force || tracker == null)
                {
                    ((PendingChangesView)control).Refresh(tracker);
                }
                if (GitSccOptions.Current.DisableAutoRefresh)
                {
                    this.Caption += " - [AUTO REFRESH DISABLED]";
                }

                sccProviderService.lastTimeRefresh = DateTime.Now;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Pending Changes Tool Window Refresh: {0}", ex.ToString());
            }
        }
    }
}
