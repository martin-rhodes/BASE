using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;

namespace BatchAudioStreamEncoder
{
    public class Encoder : INotifyPropertyChanged
    {

        public Encoder()
        {
            SourceFolderList = new ObservableCollection<string>();
            AvailableBitRates = new ObservableCollection<string>();

            foreach (var bitrate in Properties.Settings.Default.SupportedBitrates)
            {
                AvailableBitRates.Add(bitrate);
            }
        }

        public ObservableCollection<string> SourceFolderList { get; set; }
        public Process Process;

        #region User settings

        public string VLCExecutable
        {
            get { return Properties.Settings.Default.VLCExecutable; }

            set
            {
                Properties.Settings.Default.VLCExecutable = value;
                PropertyChanged(this, new PropertyChangedEventArgs("VLCExecutable"));
            }
        }

        public string OutputFolder
        {
            get { return Properties.Settings.Default.OutputFolder; }

            set
            {
                Properties.Settings.Default.OutputFolder = value;
                Properties.Settings.Default.Save();
                PropertyChanged(this, new PropertyChangedEventArgs("OutputFolder"));
            }
        }

        public string SupportedFileTypes
        {
            get { return Properties.Settings.Default.DefaultFileMask; }

            set
            {
                Properties.Settings.Default.DefaultFileMask = value;
                Properties.Settings.Default.Save();
                PropertyChanged(this, new PropertyChangedEventArgs("SupportedFileTypes"));
            }
        }

        public bool IsPrefixingEnabled
        {
            get { return Properties.Settings.Default.IsPrefixingEnabled; }
            set
            {
                Properties.Settings.Default.IsPrefixingEnabled = value;
                Properties.Settings.Default.Save();
            }
        }

        public string SelectedBitRate
        {
            get { return Properties.Settings.Default.DefaultBitrate; }
            set
            {
                Properties.Settings.Default.DefaultBitrate = value;
                Properties.Settings.Default.Save();
            }

        }


        #endregion

        # region GUI Properties

        private double _currentProgressPercentage;
        public double CurrentProgressPercentage
        {
            get { return _currentProgressPercentage; }

            set
            {
                _currentProgressPercentage = value;
                PropertyChanged(this, new PropertyChangedEventArgs("CurrentProgressPercentage"));
            }
        }

        private string _currentProgressText;
        public string CurrentProgressText
        {
            get { return _currentProgressText; }

            set
            {
                _currentProgressText = value;
                PropertyChanged(this, new PropertyChangedEventArgs("CurrentProgressText"));
            }
        }

        private string _currentSourceFile;
        public string CurrentSourceFile
        {
            get { return _currentSourceFile; }
            set
            {
                _currentSourceFile = value;
                PropertyChanged(this, new PropertyChangedEventArgs("CurrentSourceFile"));
            }
        }

        public ObservableCollection<string> AvailableBitRates { get; set; }

        #endregion

        public void Encode()
        {
            CurrentProgressPercentage = 0.0;
            CurrentProgressText = String.Empty;

            if (SourceFolderList.Count < 1)
            {
                CurrentProgressText = "No source folders specified";
                CurrentSourceFile = "Done";
                return;
            }


            //TODO: make it work without depending on a settings file
            // TODO: allow user to simply re-name files/folders that have already been encoded when choosing Force File Order option on an existing encode

            IsProcessAborted = false;
 
            string sampleRate = "44100";
            // 32, 40, 48, 56, 64, 80, 96, 112, 128, 160, 192, 224, 256 and 320

            string destinationFile;

            List<FileInfo> allFiles = new List<FileInfo>();
            string[] allDirs;
            int currentFileIndex = 0;

            // first count how many files there are in total
            foreach (string rootDir in SourceFolderList)
            {
                DirectoryInfo rootDirInfo = new DirectoryInfo(rootDir);
                // get all subdirectories for current directory
                allDirs = Directory.GetDirectories(rootDir, "*", SearchOption.AllDirectories);

                // go through each directory and get a list of all the files
                for (int dirIndex = 0; dirIndex < allDirs.Length; dirIndex++)
                {
                    DirectoryInfo dirInfo = new DirectoryInfo(allDirs[dirIndex]);
                    allFiles.AddRange(GetFiles(dirInfo, SupportedFileTypes));
                }
            }

            // loop through each root folder selected by the user
            foreach (string rootDir in SourceFolderList)
            {
                DirectoryInfo rootDirInfo = new DirectoryInfo(rootDir);
                // get all subdirectories for current directory
                allDirs = Directory.GetDirectories(rootDir, "*", SearchOption.AllDirectories);

                // go through each directory and get a list of all the files
                for (int dirIndex = 0; dirIndex < allDirs.Length; dirIndex++)
                {
                    DirectoryInfo dirInfo = new DirectoryInfo(allDirs[dirIndex]);
                    FileInfo[] fileInfo = GetFiles(dirInfo, SupportedFileTypes);

                    // if we've got some processing to do in the current directory
                    if (fileInfo.Length > 0)
                    {
                        string rootDirInfoName;
                        string dirInfoName;

                        // if the rootdir is a drive (e.g. a mounted ISO or optical media) then only extract the drive letter of the rootdir (otherwise Path.Combine fails)
                        if (rootDirInfo.Name.Contains(":")) rootDirInfoName = rootDirInfo.Name.Remove(1, 1);
                        else rootDirInfoName = rootDirInfo.Name;

                        if (dirInfo.FullName.Contains(":"))
                            dirInfoName = dirInfo.FullName.Remove(1, 1);
                        else dirInfoName = dirInfo.FullName;



                        string folderName = dirInfoName.Substring(dirInfoName.IndexOf(rootDirInfoName));

                        if (IsPrefixingEnabled)
                        {
                            StringBuilder sb = new StringBuilder();

                            string[] foldersToCheckForPrefixing = folderName.Split('\\');

                            foreach (var folder in foldersToCheckForPrefixing)
                            {
                                sb.Append(PrefixName(folder));
                                sb.Append('\\');
                            }

                            folderName = sb.ToString();
                        }


                        // set the destination path and create the destination folder if it doesn't exist
                        string targetDir = Path.Combine(OutputFolder,
                                                        folderName);    

                        if (!Directory.Exists(targetDir))
                        {
                            try
                            {
                                Directory.CreateDirectory(targetDir);
                            }
                            catch (Exception)
                            {
                                MessageBox.Show(String.Format("Unable to create output directory {0}", targetDir),
                                                "Unable to create output directory");
                                
                                IsProcessAborted = true;
                                return;
                            }

                        }

                        CurrentProgressText = String.Format("Processing file {0} of {1} ({2:F1}%)",
                                                           currentFileIndex + 1,
                                                           allFiles.Count, CurrentProgressPercentage);

                        foreach (FileInfo file in fileInfo)
                        {
                            CurrentSourceFile = file.FullName;
                            string fileName = file.Name;

                            if (IsPrefixingEnabled)
                            {
                                fileName = PrefixName(file.Name);
                            }

                            destinationFile = Path.Combine(targetDir, fileName);
                            destinationFile += ".mp3";

                            if (!File.Exists(destinationFile))
                            {
                                string encodeParameters = @"-I dummy -v """
                                                          + CurrentSourceFile
                                                          + @""" :sout=#transcode{vcodec=none,acodec=mp3,ab="
                                                          + SelectedBitRate
                                                          + ",channels=1,samplerate="
                                                          + sampleRate
                                                          + @"}:standard{access=""file"",mux=dummy,dst="""
                                                          + destinationFile
                                                          + @"""}"
                                                          + @" vlc://quit";

                                ProcessStartInfo encodeProcessStartInfo = new ProcessStartInfo(VLCExecutable,
                                                                                               encodeParameters)
                                                                              {
                                                                                  UseShellExecute = true,
                                                                                  RedirectStandardOutput = false,
                                                                                  CreateNoWindow = true,
                                                                                  WindowStyle =
                                                                                      ProcessWindowStyle.Hidden
                                                                              };

                                Process = Process.Start(encodeProcessStartInfo);
                                Process.WaitForExit();

                                // if the process has been aborted by the user, tidy up and delete the incomplete file target file
                                if (IsProcessAborted)
                                {
                                    File.Delete(destinationFile);
                                    return;
                                }
                            }



                     
                            CurrentProgressPercentage = (currentFileIndex / (double)allFiles.Count) * 100.0;

                            CurrentProgressText = String.Format("Processing file {0} of {1} ({2:F1}%)",
                                                           currentFileIndex + 1,
                                                           allFiles.Count, CurrentProgressPercentage);

                            currentFileIndex++;
                        }
                    }
                }
            }

            CurrentProgressPercentage = 100.0;
            CurrentProgressText = String.Format("Finished processing {0} files ({1:F1}%)",
                                             allFiles.Count, CurrentProgressPercentage);

            CurrentSourceFile = "Complete";
        }

        private string PrefixName(string fileName)
        {
            char[] separator =
                {
                    ' ',
                    '.',
                    '-'
                };

     

            if (fileName.IndexOfAny(separator) != -1)
            {
                int potentialPrefixNumber;
                
                string potentialPrefixString = fileName.Substring(0, fileName.IndexOfAny(separator));

                bool success = int.TryParse(potentialPrefixString, out potentialPrefixNumber);

                if (success && potentialPrefixString.Length <= 26)
                {
                    char calculatedPrefix = Convert.ToChar(potentialPrefixString.Length + 64);
                    return String.Format("{0} {1}", calculatedPrefix, fileName);
                }
            }

            return fileName;
        }

        public static FileInfo[] GetFiles(DirectoryInfo path, string searchPattern)
        {
            string[] searchPatterns = searchPattern.Split('|');
            List<FileInfo> files = new List<FileInfo>();

            foreach (string sp in searchPatterns)
                files.AddRange(path.GetFiles(sp));

            return files.ToArray();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public bool IsProcessAborted;

    }
}
