using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Principal;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using System.Diagnostics;

namespace VLC_Shell_Purge
{
    public static class ThingDoer
    {
        public enum ThingToDo
        {
            Disable,
            Hide,
            Remove,
            Undo,
            Null
        }

        public static void DoTheThing(ThingToDo thingToDo, bool playOption, bool playlistOption, bool skipBackup)
        {
            if (thingToDo == ThingToDo.Null)
                throw new Exception("No option was selected of Disable, Remove, or Hide.");

            if (!playOption && !playlistOption)
                throw new Exception("No option was selected of \"Play with VLC media player\" or \"Add to VLC media player's Playlist\".");
            
            bool isElevated;
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            isElevated = principal.IsInRole(WindowsBuiltInRole.Administrator);

            if (!isElevated)
                throw new Exception("This program must be run as an administrator.");

            if (!skipBackup)
                BackupRegistry();

            //Matches strings that begin with VLC then . then some sequence of non-whitespace characters
            //This includes all VLC.<filename extension> as well as extras like VLC.CDAudio
            const string VLC_FILE_TYPE_KEY_REGEX = @"VLC\.\S+";
            string[] classSubkeysNotOtherwiseMatched = new string[]
            {
                "Directory"
            };
            const string SHELL_KEY_NAME = "shell";
            const string PLAY_KEY_NAME = "PlayWithVLC";
            const string PLAYLIST_KEY_NAME = "AddToPlaylistVLC";

            //reference: http://www.howtogeek.com/howto/windows-vista/how-to-clean-up-your-messy-windows-context-menu/
            const string DISABLE_MAGIC_WORD = "LegacyDisable";
            const string HIDE_MAGIC_WORD = "Extended";

            foreach (string subKeyName in Registry.ClassesRoot.GetSubKeyNames())
            {
                if (Regex.Match(subKeyName, VLC_FILE_TYPE_KEY_REGEX).Value == string.Empty
                    && !classSubkeysNotOtherwiseMatched.Contains(subKeyName)) //did not match
                    continue;

                RegistryKey vlcFileTypeKey = Registry.ClassesRoot.OpenSubKey(subKeyName, true);

                RegistryKey shellKey = vlcFileTypeKey.OpenSubKey(SHELL_KEY_NAME, true);
                if (shellKey == null)
                    continue;

                //The next section is a little boiler-platey, but I don't anticipate it will
                //require much expansion.

                if (thingToDo == ThingToDo.Remove)
                {
                    if (playOption && shellKey.GetSubKeyNames().Contains(PLAY_KEY_NAME))
                        shellKey.DeleteSubKeyTree(PLAY_KEY_NAME);
                    if (playlistOption && shellKey.GetSubKeyNames().Contains(PLAYLIST_KEY_NAME))
                        shellKey.DeleteSubKeyTree(PLAYLIST_KEY_NAME);
                    continue;
                }

                if (thingToDo == ThingToDo.Undo)
                {
                    if (playOption)
                    {
                        RegistryKey playKey = shellKey.OpenSubKey(PLAY_KEY_NAME, true);
                        if (playKey != null)
                        {
                            if (playKey.GetValue(DISABLE_MAGIC_WORD) != null)
                                playKey.DeleteValue(DISABLE_MAGIC_WORD);
                            if (playKey.GetValue(HIDE_MAGIC_WORD) != null)
                                playKey.DeleteValue(HIDE_MAGIC_WORD);
                        }
                    }
                    if (playlistOption)
                    {
                        RegistryKey playlistKey = shellKey.OpenSubKey(PLAYLIST_KEY_NAME, true);
                        if (playlistKey != null)
                        {
                            if (playlistKey.GetValue(DISABLE_MAGIC_WORD) != null)
                                playlistKey.DeleteValue(DISABLE_MAGIC_WORD);
                            if (playlistKey.GetValue(HIDE_MAGIC_WORD) != null)
                                playlistKey.DeleteValue(HIDE_MAGIC_WORD);
                        }
                    }

                    continue;
                }

                string magicWordToAdd = string.Empty;
                string magicWordToRemove = string.Empty; //if present

                if (thingToDo == ThingToDo.Disable)
                {
                    magicWordToAdd = DISABLE_MAGIC_WORD;
                    magicWordToRemove = HIDE_MAGIC_WORD;
                }
                else if (thingToDo == ThingToDo.Hide)
                {
                    magicWordToAdd = HIDE_MAGIC_WORD;
                    magicWordToRemove = DISABLE_MAGIC_WORD;
                }


                if (magicWordToAdd == string.Empty)
                    throw new Exception("Expected a magic word, but somehow the options failed to produce one."); //shouldn't be possible, but who knows.
                
                if (playOption)
                {
                    RegistryKey playKey = shellKey.OpenSubKey(PLAY_KEY_NAME, true);
                    if (playKey != null)
                    {
                        playKey.SetValue(magicWordToAdd, string.Empty, RegistryValueKind.String);
                        if (playKey.GetValue(magicWordToRemove) != null)
                            playKey.DeleteValue(magicWordToRemove);
                    }
                }
                if (playlistOption)
                {
                    RegistryKey playlistKey = shellKey.OpenSubKey(PLAYLIST_KEY_NAME, true);
                    if (playlistKey != null)
                    {
                        playlistKey.SetValue(magicWordToAdd, string.Empty, RegistryValueKind.String);
                        if (playlistKey.GetValue(magicWordToRemove) != null)
                            playlistKey.DeleteValue(magicWordToRemove);
                    }
                }

            }
        }
        
        private static void BackupRegistry()
        {
            //code borrowed from http://stackoverflow.com/questions/16316827/how-to-export-a-registry-in-c-sharp
            
            if (!System.IO.Directory.Exists(@"C:\temp"))
                System.IO.Directory.CreateDirectory(@"C:\temp");

            string path = "\"" + "C:\\temp\\undo_vlc_shell_purge.reg" + "\"";
            string key = "\"" + Registry.ClassesRoot.Name + "\"";

            System.IO.File.Delete("C:\\temp\\undo_vlc_shell_purge.reg");
            //If the file already exists, this will get rid of it for us.
            //If the file is already in use, then this will throw an exception,
            //which is useful because otherwise we wouldn't know.

            Process proc = new Process();
            
            try
            {
                proc.StartInfo.FileName = "regedit.exe";
                proc.StartInfo.UseShellExecute = false;

                proc = Process.Start("regedit.exe", "/e " + path + " " + key);
                proc.WaitForExit();
            }
            catch (Exception)
            {
                proc.Dispose();
            }
        }
    }
}
