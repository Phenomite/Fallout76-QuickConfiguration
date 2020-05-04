﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace Fo76ini
{
    public class MsgBox
    {
        public String Title;
        public String Text;
        private String id;

        public String ID
        {
            get { return id; }
        }

        public MsgBox (String id, String title, String text)
        {
            this.Title = title;
            this.Text = text;
            this.id = id;
        }

        /*
         * I don't know where to put this, so I'll put this here:
         */
        public static void AddSharedMessageBoxes()
        {
            // Form1:
            MsgBox.Add("backupAndSave",
                "Backup and save",
                "Do you want to create a backup before applying all changes?\n\n" +
                "    Press \"Yes\" to create a backup and save.\n" + 
                "    Press \"No\" to save without backup.\n" +
                "    Press \"Cancel\" to abort."
            );

            MsgBox.Add("changesApplied",
                "Changes applied",
                "Changes have been applied. You may start the game now."
            );

            MsgBox.Add("chooseGameEdition",
                "Choose Game Edition",
                "Please pick your game edition under the General tab."
            );

            MsgBox.Add("runGameToGenerateINI",
                "Fallout76.ini and Fallout76Prefs.ini not found",
                "Please run the game first before using this tool.\n" +
                "The game will generate those files on first start-up."
            );

            MsgBox.Add("oldValuesResetToDefault",
                "Done",
                "Some values have been reset to default.\n" +
                "Only unstable values from previous versions are affected.\n" +
                "Don't forget to click 'Apply'."
            );

            MsgBox.Add("onLoadFuncException",
                "Error while loading UI",
                "{0} exception(s) occured while the UI was loaded.\n" +
                "This might be caused by corrupted ini files.\n" +
                "Some UI elements might not be functioning correctly.\n\n" +
                "{1}"
            );

            // Mods:
            MsgBox.Add("modsArchive2Missing",
                "Archive2 is missing",
                ".\\Archive2\\Archive2.exe is missing.\nPlease download this tool again, or install Archive2 manually."
            );

            MsgBox.Add("modsGamePathNotSet",
                "Game path not set",
                "Please set the path to the game (where Fallout76.exe resides)."
            );

            MsgBox.Add("modsGamePathInvalid",
                "Wrong path",
                "Wrong game folder path."
            );

            MsgBox.Add("modsDeployErrorModDirNotFound",
                "Mod {0} couldn't be deployed.",
                "Directory {0} does not exist.\n" +
                "Please restart the mod manager and add the mod again."
            );

            MsgBox.Add("modsDeployErrorBA2RootIsNotData",
                "Mod {0} couldn't be deployed.",
                "Directory {0} does not exist.\n" +
                "Please restart the mod manager and add the mod again."
            );

            MsgBox.Add("modsDeleteBtn",
                "Are you sure?",
                "Are you sure you want to delete the mod {0}?"
            );

            MsgBox.Add("modsDeleteBulkBtn",
                "Are you sure?",
                "Are you sure you want to delete {0} mods?"
            );

            MsgBox.Add("modsExtractUnknownError",
                "Archive couldn't be uncompressed",
                "{0}"
            );

            MsgBox.Add("modsExtractUnknownErrorText",
                "Archive couldn't be uncompressed",
                "Please uncompress the archive and add the mod as a folder."
            );

            MsgBox.Add("modsExtractUnknownError7zip",
                "7-Zip archive couldn't be uncompressed",
                "{0}"
            );

            MsgBox.Add("modsArchiveTypeNotSupported",
                "Unsupported file format",
                "{0} files are not supported.\n" +
                "Please uncompress the archive and add the mod as a folder."
            );

            MsgBox.Add("modsNoConflictingFiles",
                "Yay!",
                "No conflicting files found."
            );

            MsgBox.Add("modsDeploymentNecessary",
                "Deployment necessary",
                "For this action, you have to deploy all mods first."
            );

            MsgBox.Add("modsOnCloseDeploymentNecessary",
                "Are you sure?",
                "You haven't deployed the changes you made.\n" + 
                "Are you sure you want to close the mod manager?"
            );

            MsgBox.Add("modDirNotExist",
                "Mod folder does not exist.",
                "The path {0} does not exist.\nThe mod folder was removed without removing the entry in the manifest."
            );

            MsgBox.Add("modsInvalidManifestEntry",
                "Invalid manifest.xml entry",
                "A mod entry is invalid and was ignored.\nError: {0}"
            );

            MsgBox.Add("modsInvalidManifestRoot",
                "Invalid manifest.xml entry",
                "The <Mods> tag has invalid attributes.\nError: {0}"
            );

            MsgBox.Add("modsImportQuestion",
                "Import manually installed mods?",
                "Are you sure you want to add mods installed outside of the mod manager?"
            );

            MsgBox.Add("modsSameArchiveName",
                "Conflicting mods",
                "Some mods have the same archive name: \"{0}\".\nConflicting mod name: {1}"
            );

            MsgBox.Add("modDetailsMoveManagedFolderFailed",
                "Failed to rename folder",
                "Managed mod folder couldn't be renamed.\nError message: \"{0}\"."
            );

            MsgBox.Add("modsRepairDDSQuestion",
                "Are you sure?",
                "This will attempt to repair all *.dds files.\nIt can take a long time, depending on file size and number of files.\nAre you sure you want to continue?"
            );
        }

        private static Dictionary<String, MsgBox> msgBoxes = new Dictionary<String, MsgBox>();
        public static void Add(MsgBox msgBox)
        {
            MsgBox.msgBoxes[msgBox.id] = msgBox;
        }
        public static void Add(String id, String title, String text)
        {
            MsgBox.msgBoxes[id] = new MsgBox(id, title, text);
        }
        public static MsgBox Get(String key)
        {
            if (MsgBox.msgBoxes.ContainsKey(key))
                return MsgBox.msgBoxes[key];
            else
                return new MsgBox("notfound", $"-- Messagebox \"{key}\" not found --", $"If you read this, the programmer screwed up, lol.\nAvailable messageboxes:\n{String.Join("\n", MsgBox.msgBoxes.Keys.ToArray())}");
        }

        public MsgBox FormatTitle(params String[] values)
        {
            try
            {
                return new MsgBox(this.ID, String.Format(this.Title, values), this.Text);
            }
            catch (FormatException ex)
            {
                return this;
            }
        }

        public MsgBox FormatText(params String[] values)
        {
            try
            {
                return new MsgBox(this.ID, this.Title, String.Format(this.Text, values));
            }
            catch (FormatException ex)
            {
                return this;
            }
        }



        public static DialogResult ShowID(String id)
        {
            return MsgBox.Get(id).Show();
        }

        public static DialogResult ShowID(String id, MessageBoxButtons buttons, MessageBoxIcon icon)
        {
            return MsgBox.Get(id).Show(buttons, icon);
        }

        public static DialogResult ShowID(String id, MessageBoxButtons buttons)
        {
            return MsgBox.Get(id).Show(buttons);
        }

        public static DialogResult ShowID(String id, MessageBoxIcon icon)
        {
            return MsgBox.Get(id).Show(icon);
        }

        public DialogResult Show()
        {
            return MessageBox.Show(this.Text, this.Title);
        }

        public DialogResult Show(MessageBoxButtons buttons, MessageBoxIcon icon)
        {
            return MessageBox.Show(this.Text, this.Title, buttons, icon);
        }

        public DialogResult Show(MessageBoxButtons buttons)
        {
            return MessageBox.Show(this.Text, this.Title, buttons);
        }

        public DialogResult Show(MessageBoxIcon icon)
        {
            return MessageBox.Show(this.Text, this.Title, MessageBoxButtons.OK, icon);
        }

        public static XElement Serialize()
        {
            XElement xmlMessageBoxes = new XElement("Messageboxes");
            foreach (KeyValuePair<String, MsgBox> entry in MsgBox.msgBoxes)
            {
                xmlMessageBoxes.Add(
                    new XElement("Messagebox",
                        new XAttribute("title", entry.Value.Title),
                        new XAttribute("id", entry.Value.ID),
                        entry.Value.Text
                    )
                );
            }
            return xmlMessageBoxes;
        }

        public static void Deserialize(XElement xmlMessageBoxes)
        {
            if (xmlMessageBoxes != null)
            {
                foreach (XElement xmlMessageBox in xmlMessageBoxes.Descendants("Messagebox"))
                {
                    MsgBox.Add(new MsgBox(
                        xmlMessageBox.Attribute("id").Value,
                        xmlMessageBox.Attribute("title").Value,
                        xmlMessageBox.Value
                    ));
                }
            }
        }
    }
}
