﻿using Fo76ini.Interface;
using Fo76ini.Mods;
using Fo76ini.NexusAPI;
using Fo76ini.Properties;
using Fo76ini.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace Fo76ini
{
    public partial class FormMods : Form
    {
        private enum SidePanelStatus
        {
            Expanded,
            Collapsed,
            Closed
        }

        private SidePanelStatus sidePanelStatus = SidePanelStatus.Closed;

        private bool isUpdatingSidePanel = false;
        private int editedModCount = 1;

        private ManagedMod editedMod
        {
            get { return Mods[editedIndex]; }
        }

        private bool editingBulk
        {
            get { return editedModCount > 1; }
        }

        private int editedIndex;
        private List<int> editedIndices;

        /// <summary>
        /// Sets up elements, adds event handlers, etc.
        /// </summary>
        private void InitializeDetailControls()
        {
            DropDown.Add("ModInstallAs", new DropDown(
                this.comboBoxModInstallAs,
                new string[] {
                    "Bundled *.ba2 archive",
                    "Separate *.ba2 archive",
                    "Loose files"
                }
            ));

            DropDown.Add("ModArchivePreset", new DropDown(
                this.comboBoxModArchivePreset,
                new string[] {
                    "-- Please select --",
                    "Auto-detect",
                    "General / Interface / Materials / Animations", /* Materials: *.bgsm; Interface: *.swf; */
                    "Textures (*.dds files)",
                    "Sound FX / Music / Voice",                     /* Voice: *.fuz; Lip-Sync: *.lip; Sound FX: *.xwm; */
                }
            ));

            this.pictureBoxCollapseDetails.MouseEnter += new EventHandler((mouseSender, mouseEventArgs) =>
            {
                this.pictureBoxCollapseDetails.BackColor = Color.LightGray;
                this.pictureBoxCollapseDetails.Cursor = Cursors.Hand;
            });
            this.pictureBoxCollapseDetails.MouseLeave += new EventHandler((mouseSender, mouseEventArgs) =>
            {
                this.pictureBoxCollapseDetails.BackColor = Color.Silver;
                this.pictureBoxCollapseDetails.Cursor = Cursors.Hand;
            });

            this.Resize += this.FormModsDetails_Resize;

            // Disable scroll wheel on UI elements to prevent the user from accidentally changing values:
            Utils.PreventChangeOnMouseWheelForAllElements(this.panelModDetailsInner);

            /*
             * Drag&Drop
             */
            this.panelModDetailsReplaceDragAndDrop.AllowDrop = true;
            this.panelModDetailsReplaceDragAndDrop.DragEnter += new DragEventHandler(panelModDetailsReplaceDragAndDrop_DragEnter);
            this.panelModDetailsReplaceDragAndDrop.DragDrop += new DragEventHandler(panelModDetailsReplaceDragAndDrop_DragDrop);

            CloseSidePanel();
        }

        private void FormModsDetails_Resize(object sender, EventArgs e)
        {
            UpdateSidePanelGroupBoxesWidth();
        }

        private void EditMod(int index)
        {
            this.editedIndex = index;
            this.editedIndices = new List<int> { index };
            this.editedModCount = 1;

            if (editedIndex < 0 || editedIndex >= Mods.Count())
            {
                CloseSidePanel();
                return;
            }

            UpdateSidePanel();
            if (sidePanelStatus == SidePanelStatus.Closed)
                ExpandSidePanel();
        }

        private void EditMods(List<int> indices)
        {
            this.editedIndex = -1;
            this.editedIndices = indices.ToList();
            this.editedModCount = editedIndices.Count();

            if (this.editedModCount <= 0)
            {
                CloseSidePanel();
                return;
            }
            else if (this.editedModCount == 1)
            {
                EditMod(editedIndices[0]);
                return;
            }

            UpdateSidePanel();
            if (sidePanelStatus == SidePanelStatus.Closed)
                ExpandSidePanel();
        }


        /*
            Side panel
        */

        private void pictureBoxCollapseDetails_Click(object sender, EventArgs e)
        {
            if (sidePanelStatus != SidePanelStatus.Expanded)
                ExpandSidePanel();
            else
                CollapseSidePanel();
        }

        /// <summary>
        /// Hides the side panel with no way to open it again for the user.
        /// </summary>
        private void CloseSidePanel()
        {
            this.pictureBoxCollapseDetails.Visible = false;
            this.panelModDetails.Visible = false;

            int tabWidth = this.tabPageModOrder.Width;
            this.listViewMods.Width = tabWidth - this.listViewMods.Location.X;

            // Reset image, so the thumbnail gets unloaded:
            this.pictureBoxModThumbnail.Image = Resources.bg;

            sidePanelStatus = SidePanelStatus.Closed;
        }

        /// <summary>
        /// Collapses the side panel but it can be opened again by the user.
        /// </summary>
        private void CollapseSidePanel()
        {
            this.pictureBoxCollapseDetails.Visible = true;
            this.pictureBoxCollapseDetails.Image = Resources.arrow_left_black;
            this.panelModDetails.Visible = false;

            int tabWidth = this.tabPageModOrder.Width;
            int buttonWidth = this.pictureBoxCollapseDetails.Width;
            this.pictureBoxCollapseDetails.Location = new Point(tabWidth - buttonWidth, this.pictureBoxCollapseDetails.Location.Y);
            this.listViewMods.Width = tabWidth - this.listViewMods.Location.X - buttonWidth + 1;

            sidePanelStatus = SidePanelStatus.Collapsed;
        }

        /// <summary>
        /// Shows the side panel.
        /// </summary>
        private void ExpandSidePanel()
        {
            this.pictureBoxCollapseDetails.Visible = true;
            this.pictureBoxCollapseDetails.Image = Resources.arrow_right_black;
            this.panelModDetails.Visible = true;

            int tabWidth = this.tabPageModOrder.Width;
            int buttonWidth = this.pictureBoxCollapseDetails.Width;
            int panelWidth = this.panelModDetails.Width;
            this.pictureBoxCollapseDetails.Location = new Point(tabWidth - panelWidth - buttonWidth + 1, this.pictureBoxCollapseDetails.Location.Y);
            this.listViewMods.Width = tabWidth - this.listViewMods.Location.X - panelWidth - buttonWidth + 2;

            sidePanelStatus = SidePanelStatus.Expanded;

            UpdateSidePanelControls();
        }


        /*
         * Update UI:
         */

        public void UpdateSidePanel()
        {
            if (!editingBulk && (editedIndex < 0 || editedIndex >= Mods.Count()))
                return;
            if (editingBulk && editedIndices.Count() < 0)
                return;

            isUpdatingSidePanel = true;

            if (editingBulk)
                UpdateSidePanelBulk();
            else
                UpdateSidePanelOneMod();

            UpdateWarningLabel();
            UpdateSidePanelControls();

            isUpdatingSidePanel = false;
        }

        private void UpdateSidePanelOneMod()
        {
            this.checkBoxModDetailsEnabled.Visible = true;
            this.groupBoxModDetailsDetails.Visible = true;
            this.groupBoxModReplace.Visible = true;

            // Update thumbnail:
            if (this.editedMod.RemoteInfo != null &&
                this.editedMod.RemoteInfo.ThumbnailFileName != "" &&
                File.Exists(this.editedMod.RemoteInfo.ThumbnailFilePath))
            {
                // Thumbnail exists, load it:
                this.pictureBoxModThumbnail.Image = Image.FromFile(this.editedMod.RemoteInfo.ThumbnailFilePath);
                this.pictureBoxModThumbnail.Visible = true;
            }
            else
            {
                // No thumbnail for us:
                this.pictureBoxModThumbnail.Image = Resources.bg;
                this.pictureBoxModThumbnail.Visible = false;
            }

            // Populate values:
            this.checkBoxModDetailsEnabled.Checked = this.editedMod.Enabled;
            this.checkBoxFreezeArchive.Checked = this.editedMod.Freeze;
            this.textBoxModArchiveName.Text = this.editedMod.ArchiveName;
            this.textBoxModName.Text = this.editedMod.Title;
            this.textBoxModFolderName.Text = this.editedMod.ManagedFolderName;
            this.textBoxModRootDir.Text = this.editedMod.RootFolder;
            this.textBoxModURL.Text = this.editedMod.URL;
            this.textBoxModVersion.Text = this.editedMod.Version;
            this.textBoxNotes.Text = this.editedMod.Notes.Replace("\n", "\r\n"); ;

            switch (this.editedMod.Method)
            {
                case ManagedMod.DeploymentMethod.BundledBA2:
                    this.comboBoxModInstallAs.SelectedIndex = 0;
                    break;
                case ManagedMod.DeploymentMethod.SeparateBA2:
                    this.comboBoxModInstallAs.SelectedIndex = 1;
                    break;
                case ManagedMod.DeploymentMethod.LooseFiles:
                    this.comboBoxModInstallAs.SelectedIndex = 2;
                    break;
            }

            this.labelModTitle.Text = this.editedMod.RemoteInfo != null && this.editedMod.RemoteInfo.Title != "" ? this.editedMod.RemoteInfo.Title : this.editedMod.Title;

            // Install into visible?
            this.labelModInstallInto.Enabled = this.editedMod.Method == ManagedMod.DeploymentMethod.LooseFiles;
            this.textBoxModRootDir.Enabled = this.editedMod.Method == ManagedMod.DeploymentMethod.LooseFiles;
            this.buttonModPickRootDir.Enabled = this.editedMod.Method == ManagedMod.DeploymentMethod.LooseFiles;

            bool installIntoVisible = this.editedMod.Method == ManagedMod.DeploymentMethod.LooseFiles || this.editedMod.Method == ManagedMod.DeploymentMethod.SeparateBA2;
            this.labelModInstallInto.Visible = installIntoVisible;
            this.textBoxModRootDir.Visible = installIntoVisible;
            this.buttonModPickRootDir.Visible = installIntoVisible;

            if (this.editedMod.Method == ManagedMod.DeploymentMethod.SeparateBA2)
                this.textBoxModRootDir.Text = "Data";

            // Preset visible?
            this.comboBoxModArchivePreset.Visible = this.editedMod.Method == ManagedMod.DeploymentMethod.SeparateBA2;
            this.labelModArchivePreset.Visible = this.editedMod.Method == ManagedMod.DeploymentMethod.SeparateBA2;

            // Archive name visible?
            this.labelModArchiveName.Visible = this.editedMod.Method == ManagedMod.DeploymentMethod.SeparateBA2;
            this.textBoxModArchiveName.Visible = this.editedMod.Method == ManagedMod.DeploymentMethod.SeparateBA2;
            this.buttonModDetailsSuggestArchiveName.Visible = this.editedMod.Method == ManagedMod.DeploymentMethod.SeparateBA2;

            // Frozen visible?
            this.checkBoxFreezeArchive.Visible = this.editedMod.Method == ManagedMod.DeploymentMethod.SeparateBA2;
            this.linkLabelModInvalidateFrozenArchive.Visible = this.editedMod.Method == ManagedMod.DeploymentMethod.SeparateBA2 && 
                                                                this.editedMod.Frozen;

            // Preset
            if (this.editedMod.Method == ManagedMod.DeploymentMethod.SeparateBA2)
            {
                bool isCompressed = this.editedMod.Compression == ManagedMod.ArchiveCompression.Compressed;
                switch (this.editedMod.Format)
                {
                    case ManagedMod.ArchiveFormat.General:
                        if (isCompressed)
                            this.comboBoxModArchivePreset.SelectedIndex = 2; // General
                        else
                            this.comboBoxModArchivePreset.SelectedIndex = 4; // Sound FX
                        break;
                    case ManagedMod.ArchiveFormat.Textures:
                        this.comboBoxModArchivePreset.SelectedIndex = 3;     // Textures
                        break;
                    case ManagedMod.ArchiveFormat.Auto:
                        this.comboBoxModArchivePreset.SelectedIndex = 1;     // Auto-detect
                        break;
                    default:
                        this.comboBoxModArchivePreset.SelectedIndex = 0;     // Please select
                        break;
                }
                if (this.editedMod.Compression == ManagedMod.ArchiveCompression.Auto)
                    this.comboBoxModArchivePreset.SelectedIndex = 1;
            }

            // NexusMods info:
            if (this.editedMod.RemoteInfo != null)
            {
                this.panelModDetailsNexusMods.Visible = true;
                this.buttonModEndorse.Visible = true;
                this.buttonModAbstain.Visible = true;
                this.labelModEndorseStatus.Visible = true;
                this.labelModSummary.Visible = true;

                this.labelModLatestVersion.Text = this.editedMod.RemoteInfo.LatestVersion;
                this.labelModAuthor.Text = this.editedMod.RemoteInfo.Author;
                this.labelModSummary.Text =
                    this.editedMod.RemoteInfo.Summary
                        .Replace("<br>", "\n")
                        .Replace("<br/>", "\n")
                        .Replace("<br />", "\n");
                this.buttonModEndorse.Enabled = true;
                this.buttonModAbstain.Enabled = true;
                switch (this.editedMod.RemoteInfo.Endorsement)
                {
                    case NMMod.EndorseStatus.Endorsed:
                        this.labelModEndorseStatus.Text = Localization.GetString("endorsedText");
                        this.labelModEndorseStatus.ForeColor = Color.DarkGreen;
                        this.buttonModEndorse.Enabled = false;
                        break;
                    case NMMod.EndorseStatus.Abstained:
                        this.labelModEndorseStatus.Text = Localization.GetString("abstainedText");
                        this.labelModEndorseStatus.ForeColor = Color.DarkRed;
                        this.buttonModAbstain.Enabled = false;
                        break;
                    case NMMod.EndorseStatus.Undecided:
                    default:
                        this.labelModEndorseStatus.Text = Localization.GetString("notEndorsedText");
                        this.labelModEndorseStatus.ForeColor = Color.DarkOrange;
                        break;
                }
            }
            else
            {
                this.panelModDetailsNexusMods.Visible = false;
                this.buttonModEndorse.Visible = false;
                this.buttonModAbstain.Visible = false;
                this.labelModEndorseStatus.Visible = false;
                this.labelModSummary.Visible = false;
            }
        }

        private void UpdateSidePanelBulk()
        {
            this.checkBoxModDetailsEnabled.Visible = false;
            this.groupBoxModDetailsDetails.Visible = false;
            this.groupBoxModReplace.Visible = false;

            this.pictureBoxModThumbnail.Image = Resources.bg;
            this.labelModTitle.Text = string.Format(Localization.localizedStrings["modDetailsTitleBulkSelected"], this.editedModCount);

            // Install into visible
            this.labelModInstallInto.Visible = true;
            this.textBoxModRootDir.Visible = true;
            this.buttonModPickRootDir.Visible = true;
            this.labelModInstallInto.Enabled = true;
            this.textBoxModRootDir.Enabled = true;
            this.buttonModPickRootDir.Enabled = true;

            // Preset visible
            this.comboBoxModArchivePreset.Visible = true;
            this.labelModArchivePreset.Visible = true;

            // Archive name not visible
            this.labelModArchiveName.Visible = false;
            this.textBoxModArchiveName.Visible = false;
            this.buttonModDetailsSuggestArchiveName.Visible = false;

            // Frozen visible
            this.checkBoxFreezeArchive.Visible = true;


            // Populate values:
            this.comboBoxModInstallAs.SelectedIndex = 0; // BundledBA2
            this.comboBoxModArchivePreset.SelectedIndex = 0; // Please select

            // NexusMods:
            this.panelModDetailsNexusMods.Visible = false;
            this.buttonModEndorse.Visible = false;
            this.buttonModAbstain.Visible = false;
            this.labelModEndorseStatus.Visible = false;
            this.labelModSummary.Visible = false;
        }

        /// <summary>
        /// Resizes the group boxes within the side bar to fit, regardless of whether or not the scrollbar is shown.
        /// </summary>
        private void UpdateSidePanelGroupBoxesWidth()
        {
            int width;
            if (this.panelModDetailsInner.VerticalScroll.Visible)
                width = this.panelModDetailsInner.Width - 28;
            else
                width = this.panelModDetailsInner.Width - 14;

            this.groupBoxModDetailsInstallationOptions.Width = width;
            this.groupBoxModDetailsDetails.Width = width;
            this.groupBoxModReplace.Width = width;
        }

        private void UpdateSidePanelControls()
        {
            int groupboxMargin = 6;
            int controlMargin = 8;
            int groupboxBottomMargin = 12;

            // Is thumbnail visible?
            if (this.pictureBoxModThumbnail.Visible)
            {
                // Move header underneath thumbnail:
                this.panelModDetailsHeader.Top = this.pictureBoxModThumbnail.Top + this.pictureBoxModThumbnail.Height;
                this.panelModDetailsInner.Height = this.panelModDetails.Height - this.panelModDetailsHeader.Height - this.pictureBoxModThumbnail.Height;
            }
            else
            {
                // Move header up:
                this.panelModDetailsHeader.Top = 0;
                this.panelModDetailsInner.Height = this.panelModDetails.Height - this.panelModDetailsHeader.Height;
            }
            this.panelModDetailsInner.Top = this.panelModDetailsHeader.Top + this.panelModDetailsHeader.Height;

            // Is summary visible?
            if (this.labelModSummary.Visible)
                // Place first groupbox underneath summary:
                this.groupBoxModDetailsDetails.Top = this.labelModSummary.Top + this.labelModSummary.Height + controlMargin;
            else
                // Place first groupbox on top:
                this.groupBoxModDetailsDetails.Top = groupboxMargin;

            // Place controls on the bottom:
            int newTop = 0;
            foreach (Control control in new Control[] { comboBoxModInstallAs, textBoxModRootDir, comboBoxModArchivePreset, textBoxModArchiveName, checkBoxFreezeArchive })
                if (control.Visible && control.Top + control.Height > newTop)
                    newTop = control.Top + control.Height;

            this.labelModInstallWarning.Top = newTop + controlMargin;
            if (this.labelModInstallWarning.Visible)
                this.linkLabelModAutoDetectInstallOptions.Top = this.labelModInstallWarning.Top + this.labelModInstallWarning.Height + controlMargin;
            else
                this.linkLabelModAutoDetectInstallOptions.Top = newTop + controlMargin;

            // Determine optimal height of groupboxes:
            foreach (GroupBox box in new GroupBox[] { groupBoxModDetailsDetails, groupBoxModDetailsInstallationOptions })
            {
                int newHeight = 0;
                foreach (Control control in box.Controls)
                {
                    if (control.Visible)
                    {
                        int temp = control.Top + control.Height + groupboxBottomMargin;
                        if (newHeight < temp)
                            newHeight = temp;
                    }
                }
                box.Height = newHeight;
            }

            // Place groupboxes underneath each other:
            GroupBox[] groupBoxes = new GroupBox[] { groupBoxModDetailsDetails, groupBoxNotes, groupBoxModDetailsInstallationOptions, groupBoxModReplace };
            for (int i = 1; i < groupBoxes.Length; i++)
                groupBoxes[i].Top = groupBoxes[i - 1].Top + groupBoxes[i - 1].Height + groupboxMargin;

            UpdateSidePanelGroupBoxesWidth();
        }

        /// <summary>
        /// Attempt to detect whether the chosen mod installation options could cause the mod not to work properly.
        /// Update the label with (hopefully) helpful information for troubleshooting.
        /// </summary>
        private void UpdateWarningLabel()
        {
            if (editingBulk)
            {
                this.labelModInstallWarning.Text = "";
                this.labelModInstallWarning.Visible = false;
                return;
            }

            this.labelModInstallWarning.Visible = true;

            /*
             * "Index" the mod folder first:
             */

            bool resourceFoldersFound = false;
            bool generalFoldersFound = false;
            bool texturesFolderFound = false;
            bool soundFoldersFound = false;
            bool stringsFolderFound = false;
            bool dllFound = false;

            foreach (string folderPath in Directory.EnumerateDirectories(editedMod.ManagedFolderPath))
            {
                string folderName = Path.GetFileName(folderPath).ToLower();

                if (ModHelpers.ResourceFolders.Contains(folderName))
                    resourceFoldersFound = true;
                if (ModHelpers.GeneralFolders.Contains(folderName))
                    generalFoldersFound = true;
                if (ModHelpers.TextureFolders.Contains(folderName))
                    texturesFolderFound = true;
                if (ModHelpers.SoundFolders.Contains(folderName))
                    soundFoldersFound = true;
                if (folderName == "strings")
                    stringsFolderFound = true;
            }

            foreach (string filePath in Directory.EnumerateFiles(editedMod.ManagedFolderPath))
            {
                string fileExtension = Path.GetExtension(filePath).ToLower();

                if (fileExtension == ".dll")
                    dllFound = true;
            }


            /*
             * Check for possible issues:
             */

            // No resource folders in the top directory?
            if ((editedMod.Method == ManagedMod.DeploymentMethod.BundledBA2 ||
                editedMod.Method == ManagedMod.DeploymentMethod.SeparateBA2) &&
                !resourceFoldersFound)
            {
                this.labelModInstallWarning.Text = "Couldn't find common resource folders (meshes, textures, sounds, materials, interface, and so on). The mod might fail to load.";
                return;
            }

            // Mixed archives might cause issues:
            if (editedMod.Method == ManagedMod.DeploymentMethod.SeparateBA2 &&
                (generalFoldersFound && texturesFolderFound ||
                 generalFoldersFound && soundFoldersFound   ||
                 texturesFolderFound && soundFoldersFound))
            {
                this.labelModInstallWarning.Text = "Tip: Mixing general, texture, and sound files *might* cause the mod not to load correctly.\nIn case the mod doesn't work, you could try to set it to \"Bundled *.ba2 archive\".";
                return;
            }

            // General files found, but wrong format chosen:
            if (generalFoldersFound &&
                editedMod.Method == ManagedMod.DeploymentMethod.SeparateBA2 &&
                editedMod.Format != ManagedMod.ArchiveFormat.General && editedMod.Format != ManagedMod.ArchiveFormat.Auto)
            {
                this.labelModInstallWarning.Text = "Tip: For general mods, select the general preset or leave it on \"Auto-detect\".";
                return;
            }

            // Textures found, but wrong format chosen:
            if (texturesFolderFound &&
                editedMod.Method == ManagedMod.DeploymentMethod.SeparateBA2 &&
                (editedMod.Format != ManagedMod.ArchiveFormat.Textures && editedMod.Format != ManagedMod.ArchiveFormat.Auto ||
                editedMod.Compression != ManagedMod.ArchiveCompression.Compressed && editedMod.Compression != ManagedMod.ArchiveCompression.Auto))
            {
                this.labelModInstallWarning.Text = "Tip: For texture replacement mods, select the \"Textures (*.dds files)\" preset or leave it on \"Auto-detect\".";
                return;
            }

            // Sound files found, but wrong format chosen:
            if (soundFoldersFound &&
                editedMod.Method == ManagedMod.DeploymentMethod.SeparateBA2 &&
                (editedMod.Format != ManagedMod.ArchiveFormat.General && editedMod.Format != ManagedMod.ArchiveFormat.Auto ||
                editedMod.Compression != ManagedMod.ArchiveCompression.Uncompressed && editedMod.Compression != ManagedMod.ArchiveCompression.Auto))
            {
                this.labelModInstallWarning.Text = "Tip: For sound replacement mods, select the \"Sound FX / Music / Voice\" preset or leave it on \"Auto-detect\".";
                return;
            }

            // Any *.dll files?
            if (dllFound && (editedMod.Method != ManagedMod.DeploymentMethod.LooseFiles || (editedMod.RootFolder != "." && editedMod.RootFolder != "")))
            {
                this.labelModInstallWarning.Text = "Tip: *.dll files are usually installed as \"Loose files\" into the top directory (\".\").";
                return;
            }

            // Strings not installed as loose files?
            if (stringsFolderFound && (editedMod.Method != ManagedMod.DeploymentMethod.LooseFiles || editedMod.RootFolder != "Data"))
            {
                this.labelModInstallWarning.Text = "Tip: Strings are usually installed as \"Loose files\" into the \"Data\" folder.";
                return;
            }

            this.labelModInstallWarning.Text = "";
            this.labelModInstallWarning.Visible = false;
        }


        /*
         * Properties changed:
         */


        private void comboBoxModInstallAs_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (isUpdatingSidePanel)
                return;
            foreach (int index in editedIndices)
            {
                ManagedMod editedMod = Mods[index];
                switch (this.comboBoxModInstallAs.SelectedIndex)
                {
                    case 0: // Bundled *.ba2 archive
                        editedMod.Method = ManagedMod.DeploymentMethod.BundledBA2;
                        break;
                    case 1: // Separate *.ba2 archive
                        editedMod.Method = ManagedMod.DeploymentMethod.SeparateBA2;
                        break;
                    case 2: // Loose files
                        editedMod.Method = ManagedMod.DeploymentMethod.LooseFiles;
                        break;
                }
                if (editedMod.Frozen && (editedMod.Method != ManagedMod.DeploymentMethod.SeparateBA2 &&
                    editedMod.PreviousMethod == ManagedMod.DeploymentMethod.SeparateBA2))
                    ModActions.Unfreeze(editedMod);
            }
            if (!editingBulk)
                UpdateSidePanel();
            UpdateModList();
            UpdateStatusStrip();
            UpdateWarningLabel();
            UpdateSidePanelControls();
            Mods.Save();
        }

        private void comboBoxModArchivePreset_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (isUpdatingSidePanel)
                return;
            foreach (int index in editedIndices)
            {
                ManagedMod editedMod = Mods[index];
                switch (this.comboBoxModArchivePreset.SelectedIndex)
                {
                    case 0: // Please select
                    case 1: // Auto-detect
                        editedMod.Format = ManagedMod.ArchiveFormat.Auto;
                        editedMod.Compression = ManagedMod.ArchiveCompression.Auto;
                        break;
                    case 2: // General
                        editedMod.Format = ManagedMod.ArchiveFormat.General;
                        editedMod.Compression = ManagedMod.ArchiveCompression.Compressed;
                        break;
                    case 3: // Textures
                        editedMod.Format = ManagedMod.ArchiveFormat.Textures;
                        editedMod.Compression = ManagedMod.ArchiveCompression.Compressed;
                        break;
                    case 4: // Audio
                        editedMod.Format = ManagedMod.ArchiveFormat.General;
                        editedMod.Compression = ManagedMod.ArchiveCompression.Uncompressed;
                        break;
                }
                if (editedMod.Frozen &&
                    (editedMod.Format != editedMod.CurrentFormat ||
                     editedMod.Compression != editedMod.CurrentCompression))
                    ModActions.Unfreeze(editedMod);
            }
            if (!editingBulk)
                UpdateSidePanel();
            UpdateModList();
            UpdateStatusStrip();
            UpdateWarningLabel();
            UpdateSidePanelControls();
            Mods.Save();
        }

        private void buttonModPickRootDir_Click(object sender, EventArgs e)
        {
            if (isUpdatingSidePanel)
                return;
            this.folderBrowserDialogPickRootDir.SelectedPath = Path.Combine(this.Mods.GamePath, "Data");
            if (this.folderBrowserDialogPickRootDir.ShowDialog() == DialogResult.OK)
            {
                string rootFolder = Utils.MakeRelativePath(this.Mods.GamePath, this.folderBrowserDialogPickRootDir.SelectedPath);
                this.textBoxModRootDir.Text = rootFolder;
                foreach (int index in editedIndices)
                    Mods[index].RootFolder = rootFolder;
            }
            UpdateModList();
            UpdateStatusStrip();
            UpdateWarningLabel();
            UpdateSidePanelControls();
            Mods.Save();
        }

        private void textBoxModRootDir_TextChanged(object sender, EventArgs e)
        {
            if (isUpdatingSidePanel)
                return;
            if (this.textBoxModRootDir.Focused)
                foreach (int index in editedIndices)
                    Mods[index].RootFolder = this.textBoxModRootDir.Text;
            UpdateModList();
            UpdateStatusStrip();
            UpdateWarningLabel();
            UpdateSidePanelControls();
            Mods.Save();
        }

        private void textBoxModArchiveName_TextChanged(object sender, EventArgs e)
        {
            if (isUpdatingSidePanel)
                return;
            if (this.textBoxModArchiveName.Focused)
            {
                this.editedMod.ArchiveName = this.textBoxModArchiveName.Text;
                UpdateModList();
                UpdateStatusStrip();
                Mods.Save();
            }
        }

        private void textBoxModName_TextChanged(object sender, EventArgs e)
        {
            if (isUpdatingSidePanel)
                return;
            if (textBoxModName.Focused)
            {
                this.editedMod.Title = this.textBoxModName.Text;
                if (this.editedMod.RemoteInfo == null || this.editedMod.RemoteInfo.Title == "")
                    this.labelModTitle.Text = this.editedMod.Title;
                UpdateModList();
                Mods.Save();
            }
        }

        private void textBoxNotes_TextChanged(object sender, EventArgs e)
        {
            if (this.textBoxNotes.Focused)
            {
                this.editedMod.Notes = this.textBoxNotes.Text.Replace("\r\n", "\n");
                Mods.Save();
            }
        }

        private void checkBoxModDetailsEnabled_CheckedChanged(object sender, EventArgs e)
        {
            if (isUpdatingSidePanel)
                return;
            this.editedMod.Enabled = this.checkBoxModDetailsEnabled.Checked;
            UpdateModList();
            UpdateStatusStrip();
            Mods.Save();
        }

        private void checkBoxFreezeArchive_CheckedChanged(object sender, EventArgs e)
        {
            if (isUpdatingSidePanel)
                return;
            foreach (int index in editedIndices)
                Mods[index].Freeze = this.checkBoxFreezeArchive.Checked;
            UpdateModList();
            UpdateStatusStrip();
            Mods.Save();
        }

        private void textBoxModURL_TextChanged(object sender, EventArgs e)
        {
            if (isUpdatingSidePanel)
                return;
            this.editedMod.URL = this.textBoxModURL.Text;
            Mods.Save();
        }

        private void textBoxModVersion_TextChanged(object sender, EventArgs e)
        {
            if (isUpdatingSidePanel)
                return;
            this.editedMod.Version = this.textBoxModVersion.Text;
            UpdateModList();
            Mods.Save();
        }

        private void textBoxModFolderName_TextChanged(object sender, EventArgs e)
        {
            if (this.textBoxModFolderName.Focused)
            {
                if (ModActions.RenameFolder(editedMod, this.textBoxModFolderName.Text))
                {
                    Mods.Save();
                    this.textBoxModFolderName.ForeColor = Color.Black;
                }
                else
                {
                    //this.textBoxModFolderName.Text = this.editedMod.ManagedFolderName;
                    this.textBoxModFolderName.ForeColor = Color.Red;
                }
            }
        }


        /*
         * Actions
         */

        // Suggest archive name
        private void buttonModDetailsSuggestArchiveName_Click(object sender, EventArgs e)
        {
            this.editedMod.ArchiveName = Utils.GetValidFileName(this.editedMod.Title, ".ba2");
            this.textBoxModArchiveName.Text = this.editedMod.ArchiveName;
            UpdateSidePanel();
            UpdateModList();
            Mods.Save();
        }

        // Open webpage
        private void buttonModOpenPage_Click(object sender, EventArgs e)
        {
            if (editedMod.ID >= 0)
                Process.Start("https://www.nexusmods.com/fallout76/mods/" + editedMod.ID.ToString()/* + "?tab=files"*/);
            else if (editedMod.URL != "")
                Process.Start(editedMod.URL);
        }

        // Set latest version
        private void linkLabelModSetLatestVersion_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (!editingBulk && editedMod.RemoteInfo != null && editedMod.RemoteInfo.LatestVersion != "")
            {
                this.editedMod.Version = editedMod.RemoteInfo.LatestVersion;
                this.textBoxModVersion.Text = editedMod.RemoteInfo.LatestVersion;
            }
        }

        // Delete folder contents
        private void linkLabelModDeleteFolderContents_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (MsgBox.Get("areYouSure").FormatText("Are you sure, you want to delete the folder contents?").Show(MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                if (Directory.Exists(editedMod.ManagedFolderPath))
                    Directory.Delete(editedMod.ManagedFolderPath, true);
                Directory.CreateDirectory(editedMod.ManagedFolderPath);
                UpdateProgress(Progress.Done("Deleted files."));
            }
        }

        // Import from archive
        private void linkLabelModReplaceFilesWithArchive_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (this.openFileDialogMod.ShowDialog() == DialogResult.OK)
                AddArchiveToModThreaded(this.openFileDialogMod.FileName);
        }

        // Import from folder
        private void linkLabelModReplaceFilesWithFolder_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (this.folderBrowserDialogMod.ShowDialog() == DialogResult.OK)
                AddFolderToModThreaded(this.folderBrowserDialogMod.SelectedPath);
        }

        // Auto-detect installation options
        private void linkLabelModAutoDetectInstallOptions_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (!editingBulk)
                ModActions.DetectOptimalModInstallationOptions(editedMod);
            else
            {
                foreach (int index in editedIndices)
                    ModActions.DetectOptimalModInstallationOptions(Mods[index]);
            }
            UpdateSidePanel();
            UpdateModList();
        }

        // Invalidate frozen archive
        private void linkLabelModInvalidateFrozenArchive_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (editingBulk)
                return;
            editedMod.Frozen = false;
            UpdateModList();
            UpdateSidePanel();
        }


        /*
         * NexusAPI
         */

        private void buttonModEndorse_Click(object sender, EventArgs e)
        {
            if (editingBulk)
                return;

            if (this.editedMod.Version == "")
            {
                MsgBox.Show("Can't endorse", "Please enter a version and try again.", MessageBoxIcon.Information);
                return;
            }

            if (!NexusMods.User.IsLoggedIn)
            {
                MsgBox.ShowID("nexusModsNotLoggedIn", MessageBoxIcon.Information);
                return;
            }

            if (this.editedMod.RemoteInfo != null)
                if (this.editedMod.RemoteInfo.Endorse(this.editedMod.Version))
                    UpdateSidePanel();
        }

        private void buttonModAbstain_Click(object sender, EventArgs e)
        {
            if (editingBulk)
                return;

            if (this.editedMod.Version == "")
            {
                MsgBox.Show("Can't abstain from endorsing", "Please enter a version and try again.", MessageBoxIcon.Information);
                return;
            }

            if (!NexusMods.User.IsLoggedIn)
            {
                MsgBox.ShowID("nexusModsNotLoggedIn", MessageBoxIcon.Information);
                return;
            }

            if (this.editedMod.RemoteInfo != null)
                if (this.editedMod.RemoteInfo.Abstain(this.editedMod.Version))
                    UpdateSidePanel();
        }


        /*
         * Drag & drop
         */

        private void panelModDetailsReplaceDragAndDrop_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
        }

        private void panelModDetailsReplaceDragAndDrop_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            AddToModBulkThreaded(files);
        }

        private void AddFolderToModThreaded(string folderPath)
        {
            RunThreaded(() => {
                DisableUI();
            }, () => {
                try
                {
                    ModInstallations.AddFolder(editedMod, folderPath, false, UpdateProgress);
                }
                catch (Exception exc)
                {
                    MsgBox.Get("failed").FormatText(exc.Message).Show(MessageBoxIcon.Error);
                    return false;
                }
                return true;
            }, (success) => {
                EnableUI();
                UpdateSidePanel();
            });
        }

        private void AddArchiveToModThreaded(string filePath)
        {
            RunThreaded(() => {
                DisableUI();
            }, () => {
                try
                {
                    ModInstallations.AddArchive(editedMod, filePath, UpdateProgress);
                }
                catch (Archive2RequirementsException exc)
                {
                    MsgBox.ShowID("archive2InstallRequirements", MessageBoxIcon.Error);
                    return false;
                }
                catch (Archive2Exception exc)
                {
                    MsgBox.ShowID("archive2Error", MessageBoxIcon.Error);
                    return false;
                }
                catch (Exception exc)
                {
                    MsgBox.Get("failed").FormatText(exc.Message).Show(MessageBoxIcon.Error);
                    return false;
                }
                return true;
            }, (success) => {
                EnableUI();
                UpdateSidePanel();
            });
        }

        private void AddToModBulkThreaded(string[] files)
        {
            RunThreaded(() => {
                DisableUI();
            }, () => {
                try
                {
                    AddToModBulk(files, UpdateProgress);
                }
                catch (Archive2RequirementsException exc)
                {
                    MsgBox.ShowID("archive2InstallRequirements", MessageBoxIcon.Error);
                    return false;
                }
                catch (Archive2Exception exc)
                {
                    MsgBox.ShowID("archive2Error", MessageBoxIcon.Error);
                    return false;
                }
                catch (Exception exc)
                {
                    MsgBox.Get("failed").FormatText(exc.Message).Show(MessageBoxIcon.Error);
                    return false;
                }
                return true;
            }, (success) => {
                EnableUI();
                UpdateSidePanel();
            });
        }

        private void AddToModBulk(string[] files, Action<Progress> ProgressChanged = null)
        {
            if (editingBulk)
                return;
            int i = 0;
            string phaseStr = "Importing {0} of {1} files/folders - {2}";
            foreach (string filePath in files)
            {
                Action<Progress> PhasedProgressChanged = Progress.BuildPhasedProgressChanged(ProgressChanged, phaseStr, i++, files.Length);

                string fileExtension = Path.GetExtension(filePath);
                string fileName = Path.GetFileName(filePath);
                string longFilePath = ModInstallations.EnsureLongPathSupport(filePath);

                if (Directory.Exists(longFilePath))
                    ModInstallations.AddFolder(editedMod, filePath, true, PhasedProgressChanged);
                else if ((new string[] { ".ba2", ".zip", ".rar", ".tar", ".7z" }).Contains(fileExtension.ToLower()))
                    ModInstallations.AddArchive(editedMod, filePath, PhasedProgressChanged);
                else
                {
                    PhasedProgressChanged.Invoke(Progress.Indetermined($"Copying '{fileName}'..."));
                    File.Copy(longFilePath, Path.Combine(editedMod.ManagedFolderPath, fileName), true);
                }
            }
            ProgressChanged?.Invoke(Progress.Done("File(s)/folder(s) added."));
        }
    }
}
