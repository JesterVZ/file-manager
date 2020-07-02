using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Runtime.InteropServices;

namespace test_task
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public class FileItem
    {
        public string FileName { get; set; }
        public string FullPath;
        public double Size;
        public int CountOfFiles;
        public DateTime Created;
        public BitmapSource ItemIcon { get; set; }

        public override string ToString()
        {
            return FileName;
        }
        
    }

    public static class DefaultIcons
    {
        private static readonly Lazy<Icon> _lazyFolderIcon = new Lazy<Icon>(FetchIcon, true);

        public static Icon FolderLarge
        {
            get { return _lazyFolderIcon.Value; }
        }

        private static Icon FetchIcon()
        {
            var tmpDir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())).FullName;
            var icon = ExtractFromPath(tmpDir);
            Directory.Delete(tmpDir);
            return icon;
        }

        private static Icon ExtractFromPath(string path)
        {
            SHFILEINFO shinfo = new SHFILEINFO();
            SHGetFileInfo(
                path,
                0, ref shinfo, (uint)Marshal.SizeOf(shinfo),
                SHGFI_ICON | SHGFI_LARGEICON);
            return System.Drawing.Icon.FromHandle(shinfo.hIcon);
        }

        //Struct used by SHGetFileInfo function
        [StructLayout(LayoutKind.Sequential)]
        private struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        };

        [DllImport("shell32.dll")]
        private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbSizeFileInfo, uint uFlags);

        private const uint SHGFI_ICON = 0x100;
        private const uint SHGFI_LARGEICON = 0x0;
        private const uint SHGFI_SMALLICON = 0x000000001;
    }

    public partial class MainWindow : Window
    {
        private string path = "C:/";
        private bool isFile = false;
        private string FileName = "";
        private string FileSize = "";
        private int CountOfFiles = 0;
        private DateTime FileDate;
        private bool isRemoveComplete = true;

        public MainWindow()
        {
            InitializeComponent();
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

            FileInfoPanel.Visibility = Visibility.Hidden;
            FolderInfoPanel.Visibility = Visibility.Hidden;
            filePath.Text = path;
            LoadFiles();
        }
        private void LoadFiles()
        {
            DirectoryInfo directoryInfo;
            try
            {
                if (isFile) {
                    if (File.Exists(filePath.Text))
                    {
                        Process.Start(filePath.Text);
                    } else
                    {
                        MessageBox.Show("файл не найден");
                    }

                } else
                {
                    directoryInfo = new DirectoryInfo(path);
                    FileInfo[] files = directoryInfo.GetFiles();
                    DirectoryInfo[] dirs = directoryInfo.GetDirectories();
                    if (FileListView.Items.Count != 0)
                    {
                        isRemoveComplete = false;
                        FileListView.Items.Clear();
                        isRemoveComplete = true;
                    }
                    for (int i = 0; i < files.Length; i++)
                    {
                        FileItem fileItem = new FileItem();
                        fileItem.FileName = files[i].Name;
                        fileItem.FullPath = files[i].FullName;
                        fileItem.Size = files[i].Length;
                        fileItem.Created = files[i].CreationTime;
                        var sysicon = System.Drawing.Icon.ExtractAssociatedIcon(files[i].FullName);
                        var bmpSrc = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                                    sysicon.Handle,
                                    System.Windows.Int32Rect.Empty,
                                    System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
                        sysicon.Dispose();
                        fileItem.ItemIcon = bmpSrc;
                        FileListView.Items.Add(fileItem);
                    }
                    for (int i = 0; i < dirs.Length; i++)
                    {
                        FileItem dirItem = new FileItem();
                        dirItem.FileName = dirs[i].Name;
                        dirItem.FullPath = dirs[i].FullName;
                        try
                        {
                            dirItem.CountOfFiles = dirs[i].GetFiles().Length + dirs[i].GetDirectories().Length;
                        }
                        catch (Exception e)
                        {

                        }
                        var sysicon = DefaultIcons.FolderLarge;
                        var bmpSrc = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                                    sysicon.Handle,
                                    System.Windows.Int32Rect.Empty,
                                    System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
                        sysicon.Dispose();
                        dirItem.ItemIcon = bmpSrc;
                        FileListView.Items.Add(dirItem);

                    }
                }


            }
            catch (UnauthorizedAccessException e)
            {
                MessageBox.Show("Отказано в доступе");
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }
        }
        private string fileSizeConvert(double size)
        {
            double kilobytes = Math.Round(size / 1024);
            double megabytes = Math.Round(kilobytes / 1024);
            double gigabytes = Math.Round(megabytes / 1024);
            if(gigabytes > 1)
            {
                return gigabytes + " GB";
            } else if(megabytes > 1)
            {
                return megabytes + " MB";
            } else if(kilobytes > 1)
            {
                return kilobytes + " KB";
            } else
            {
                return size + " B";
            }
            
        }
        private void loadBtnAction()
        {
            path = filePath.Text;
            LoadFiles();
            isFile = false;
        }

        private void ShowFileInfo(string fileName, string fileSize, DateTime dateTime, bool itemIsFile)
        {
            LabelName.Content = fileName;
            LabelSize.Content = fileSize;
            LabelDate.Content = dateTime.ToString();
        }
        private void ShowFolderInfo(int count)
        {
            LabelCount.Content = count;
        }

        private void NextBtn_Click(object sender, RoutedEventArgs e)
        {
            loadBtnAction();
        }

        private void FileListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isRemoveComplete)
            {
                path = ((FileItem)this.FileListView.Items.GetItemAt(this.FileListView.SelectedIndex)).FullPath;
                FileName = ((FileItem)this.FileListView.Items.GetItemAt(this.FileListView.SelectedIndex)).FileName;
                FileSize = fileSizeConvert(((FileItem)this.FileListView.Items.GetItemAt(this.FileListView.SelectedIndex)).Size);
                FileDate = ((FileItem)this.FileListView.Items.GetItemAt(this.FileListView.SelectedIndex)).Created;
                CountOfFiles = ((FileItem)this.FileListView.Items.GetItemAt(this.FileListView.SelectedIndex)).CountOfFiles;

                FileAttributes fileAttributes = File.GetAttributes(path);
                if ((fileAttributes & FileAttributes.Directory) == FileAttributes.Directory)
                {
                    FileInfoPanel.Visibility = Visibility.Hidden;
                    FolderInfoPanel.Visibility = Visibility.Visible;
                    isFile = false;
                    filePath.Text = path;
                    ShowFolderInfo(CountOfFiles);
                }
                else
                {
                    FolderInfoPanel.Visibility = Visibility.Hidden;
                    FileInfoPanel.Visibility = Visibility.Visible;
                    path = ((FileItem)this.FileListView.Items.GetItemAt(this.FileListView.SelectedIndex)).FullPath;
                    filePath.Text = path;
                    isFile = true;
                    ShowFileInfo(FileName, FileSize, FileDate, isFile);
                }
            }

        }

        private void FileListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            loadBtnAction();
        }
        private void removeSlash()
        {
            char[] pathCharArray;
            List<char> pathCharList = new List<char>();
            string backPath = filePath.Text;
            if (backPath != "C:\\")
            {
                pathCharArray = backPath.ToCharArray();

                for (int i = 0; i < pathCharArray.Length; i++)
                {
                    pathCharList.Add(pathCharArray[i]);
                }
                for (int i = backPath.Length - 1; i > 0; i--)
                {
                    if (pathCharList[i] != '\\')
                    {
                        pathCharList.RemoveAt(i);
                    }
                    else
                    {
                        backPath = "";
                        if(pathCharList.Count == 3)
                        {
                            backPath = "C:\\";
                            filePath.Text = backPath;
                            break;
                        }
                        pathCharList.RemoveAt(i);
                        for (int j = 0; j < i; j++)
                        {
                            backPath += pathCharList[j].ToString();
                        }
                        filePath.Text = backPath;
                        break;
                    }
                }
            }

        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            LoadBack();
            loadBtnAction();
        }
        private void LoadBack()
        {
            try
            {
                removeSlash();
                string backPath = filePath.Text;
                this.isFile = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }
    }
}
