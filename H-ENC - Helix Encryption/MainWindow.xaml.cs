using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MahApps.Metro.Controls;
using System.Threading;
using System.Security.Cryptography;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Checksums;
using System.ComponentModel;
using System.Xml;
using System.Xml.Linq;

namespace H_ENC___Helix_Encryption
{

    public partial class Enc : MetroWindow
    {

        BackgroundWorker worker;
        private bool workerPaused;

        int workerData;

        //R.S.A Enc
        CspParameters cspp = new CspParameters();
        RSACryptoServiceProvider rsa;
        const string EncrFolder = @"Data\HENC\"; // Krypterad fil sparas i programkatalogen, DATA etc....
        string DecrFolder = @"Data\DENC\"; // som ovan fast decrypterade... 
        // Key container name for 
        // private/public key value pair. 
        string keyName = "_hpk.xml";

        private void EncryptFile(string inFile)
        {

            RijndaelManaged rjndl = new RijndaelManaged();
            rjndl.KeySize = 256;
            rjndl.BlockSize = 256;
            rjndl.Mode = CipherMode.CBC;
            ICryptoTransform transform = rjndl.CreateEncryptor();

            // Use RSACryptoServiceProvider to 
            // enrypt the Rijndael key. 
            // rsa is previously instantiated:  
            rsa = new RSACryptoServiceProvider(cspp); 
            byte[] keyEncrypted = rsa.Encrypt(rjndl.Key, false);


            // Create byte arrays to contain 
            // the length values of the key and IV. 
            byte[] LenK = new byte[4];
            byte[] LenIV = new byte[4];

            int lKey = keyEncrypted.Length;
            LenK = BitConverter.GetBytes(lKey);
            int lIV = rjndl.IV.Length;
            LenIV = BitConverter.GetBytes(lIV);

            // Write the following to the FileStream 
            // for the encrypted file (outFs): 
            // - length of the key 
            // - length of the IV 
            // - ecrypted key 
            // - the IV 
            // - the encrypted cipher content 

            int startFileName = inFile.LastIndexOf("\\") + 1;
            string outFile = EncrFolder + inFile.Substring(startFileName, inFile.LastIndexOf(".") - startFileName) + ".enc";

            using (FileStream outFs = new FileStream(outFile, FileMode.Create))
            {
                outFs.Write(LenK, 0, 4);
                outFs.Write(LenIV, 0, 4);
                outFs.Write(keyEncrypted, 0, lKey);
                outFs.Write(rjndl.IV, 0, lIV);

                // Now write the cipher text using 
                // a CryptoStream for encrypting. 
                using (CryptoStream outStreamEncrypted = new CryptoStream(outFs, transform, CryptoStreamMode.Write))
                {

                    // By encrypting a chunk at 
                    // a time, you can save memory 
                    // and accommodate large files. 
                    int count = 0;
                    int offset = 0;

                    // blockSizeBytes can be any arbitrary size. 
                    int blockSizeBytes = rjndl.BlockSize / 8;
                    byte[] data = new byte[blockSizeBytes];
                    int bytesRead = 0;

                    using (FileStream inFs = new FileStream(inFile, FileMode.Open, FileAccess.Read))
                    {
                        do
                        {
                            count = inFs.Read(data, 0, blockSizeBytes);
                            offset += count;
                            outStreamEncrypted.Write(data, 0, count);
                            bytesRead += blockSizeBytes;
                        }
                        while (count > 0);
                        inFs.Close();
                    }
                    outStreamEncrypted.FlushFinalBlock();
                    outStreamEncrypted.Close();
                }
                outFs.Close();
            }

        }
        private void DecryptFile(string inFile)
        {

            // Create instance of Rijndael for 
            // symetric decryption of the data.
            RijndaelManaged rjndl = new RijndaelManaged();
            rjndl.KeySize = 256;
            rjndl.BlockSize = 256;
            rjndl.Mode = CipherMode.CBC;

            // Create byte arrays to get the length of 
            // the encrypted key and IV. 
            // These values were stored as 4 bytes each 
            // at the beginning of the encrypted package. 
            byte[] LenK = new byte[4];
            byte[] LenIV = new byte[4];

            int startFileName = inFile.LastIndexOf("\\") + 1;
            // Consruct the file name for the decrypted file. 

            string outFile = DecrFolder + inFile.Substring(0, inFile.LastIndexOf(".")) + ".denc";


            //string outFile = DecrFolder + inFile.Substring(startFileName, inFile.LastIndexOf(".") - startFileName) + ".denc";

            // Use FileStream objects to read the encrypted 
            // file (inFs) and save the decrypted file (outFs). 
            using (FileStream inFs = new FileStream(EncrFolder + inFile, FileMode.Open))
            {

                inFs.Seek(0, SeekOrigin.Begin);
                inFs.Seek(0, SeekOrigin.Begin);
                inFs.Read(LenK, 0, 3);
                inFs.Seek(4, SeekOrigin.Begin);
                inFs.Read(LenIV, 0, 3);

                // Convert the lengths to integer values. 
                int lenK = BitConverter.ToInt32(LenK, 0);
                int lenIV = BitConverter.ToInt32(LenIV, 0);

                // Determine the start postition of 
                // the ciphter text (startC) 
                // and its length(lenC). 
                int startC = lenK + lenIV + 8;
                int lenC = (int)inFs.Length - startC;

                // Create the byte arrays for 
                // the encrypted Rijndael key, 
                // the IV, and the cipher text. 
                byte[] KeyEncrypted = new byte[lenK];
                byte[] IV = new byte[lenIV];

                // Extraherar key och IV 
                // börjar från index 8 
                // efter "lenght" värdena.
                inFs.Seek(8, SeekOrigin.Begin);
                inFs.Read(KeyEncrypted, 0, lenK);
                inFs.Seek(8 + lenK, SeekOrigin.Begin);
                inFs.Read(IV, 0, lenIV);
                Directory.CreateDirectory(DecrFolder);
                

                byte[] KeyDecrypted = rsa.Decrypt(KeyEncrypted, false);

                //Decypterar nycklen...
                    ICryptoTransform transform = rjndl.CreateDecryptor(KeyDecrypted, IV);

                    //Decrypterar valda filen.
                    using (FileStream outFs = new FileStream(outFile, FileMode.Create))
                    {
                        Properties.Settings.Default.dec_last = outFile;
                        int count = 0;
                        int offset = 0;

                        // blockSizeBytes can be any arbitrary size. 
                        int blockSizeBytes = rjndl.BlockSize / 8;
                        byte[] data = new byte[blockSizeBytes];
                        //Börjar i början av den crypterade strängen.
                        inFs.Seek(startC, SeekOrigin.Begin);
                        using (CryptoStream outStreamDecrypted = new CryptoStream(outFs, transform, CryptoStreamMode.Write))
                        {
                            do
                            {
                                count = inFs.Read(data, 0, blockSizeBytes);
                                offset += count;
                                outStreamDecrypted.Write(data, 0, count);

                            }
                            while (count > 0);

                            outStreamDecrypted.FlushFinalBlock();
                            outStreamDecrypted.Close();
                        }
                        outFs.Close();
                    }
                    inFs.Close();
                }
        }

        private void GridVList()
        {
            string FileType;
            FileType = ".henc";
            string Folder;

            Folder = System.AppDomain.CurrentDomain.BaseDirectory + Properties.Settings.Default.encPATH;
            DirectoryInfo dinfo = new DirectoryInfo(Folder);
            FileInfo[] Files = dinfo.GetFiles(FileType);

            GridView view = new GridView();
            //var view = new GridView();
            view.Columns.Add(new GridViewColumn { Header = "H-ID:", DisplayMemberBinding = new Binding("FileId") });
            view.Columns.Add(new GridViewColumn { Header = "Filename:", DisplayMemberBinding = new Binding("File") });
            view.Columns.Add(new GridViewColumn { Header = "Size:", DisplayMemberBinding = new Binding("Size") });
            view.Columns.Add(new GridViewColumn { Header = "Encrypted:", DisplayMemberBinding = new Binding("Date") });

            lstBrow1.View = view;
            foreach (FileInfo f in dinfo.GetFiles())
                lstBrow1.Items.Add(new { File = f.Name, Size = GetFileSize(f), Date = f.CreationTimeUtc });
            //lstbox1.Items.Add("Test!" + System.AppDomain.CurrentDomain.BaseDirectory + Properties.Settings.Default.encPATH);
        }
        public Enc()
        {

            InitializeComponent();
            this.Closed += new EventHandler(Enc_Closed);

        }

        void Enc_Closed(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += worker_DoWork;
            worker.ProgressChanged += worker_ProgressChanged;

            worker.RunWorkerAsync();
        }

        public partial class MainWindow : UserControl
        {

        }

        public class HId
        {
            public string Id { get; set; }
        }
 

        private void EnC_OnLoad(object sender, RoutedEventArgs e)
        {
            txAbout.Text = "Made for a contest in 2011, Released for those who want to use/learn"
                            + Environment.NewLine + "Private use, we doesnt take responsibility for your use!"
                            + Environment.NewLine + "This is a early release!"
                            + Environment.NewLine + "Last updated 2014/01/09"
                            + Environment.NewLine + "Contact: nozzish@gmail.com";

            // laddar Listview, filer.
            GridVList();
            //bgworker
            this.workerPaused = false;
            InitializeBGW();
            
            string hPath = Properties.Settings.Default.encPATH;
            string HENCpath = AppDomain.CurrentDomain.BaseDirectory + hPath;
            fileopen.Text = Properties.Settings.Default.defaultFolder;

            if (!Directory.Exists(Properties.Settings.Default.encPATH))
            {
                lstbox1.Items.Add(Environment.NewLine + "Loading Resources...\\Data\\HENC\\ encryption folder found!" + Environment.NewLine);
                lstbox1.Items.Add("Loading Settings from Userapplication....");
            }
            else
            {
                Directory.CreateDirectory(System.AppDomain.CurrentDomain.BaseDirectory + Properties.Settings.Default.encPATH);
                lstbox1.Items.Add(Environment.NewLine + "Loading Resources...No \\Data\\HENC\\ encryption folder found!" + Environment.NewLine);
            }
        }

        public void uZip(string zipFileName, string targetDir)
        {

            FastZip fastZip = new FastZip();
            string fileFilter = null;

            // Will always overwrite if target filenames already exist
            fastZip.ExtractZip(zipFileName, targetDir, fileFilter);
        }
        private string GetFileSize(FileInfo f)
        {
            string[] fileUnits = { "B", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
            int fuCount = 0;
            long size = f.Length;

            while (size >= 1024)
            {
                fuCount++;
                size /= 1024;
            }
            return string.Format("{0} {1}", size, fileUnits[fuCount]);
        }

        private void InitializeBGW()
        {
            this.worker = null;
            this.worker = new BackgroundWorker();
            this.worker.WorkerReportsProgress = true;
            this.worker.WorkerSupportsCancellation = true;
            this.worker.DoWork += new DoWorkEventHandler(worker_DoWork);
            this.worker.ProgressChanged += new ProgressChangedEventHandler(worker_ProgressChanged);
            this.worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(worker_RunWorkerCompleted);
            this.worker.Disposed += new EventHandler(worker_Disposed);
            
            return;
        }

        private void fileopen_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            System.Windows.Forms.DialogResult result = dialog.ShowDialog();
            fileopen.Text = dialog.SelectedPath;
        }

        private int _uptoFileCount;
        private int _totalFileCount;

        public void doit_Click(object sender, RoutedEventArgs e)
        {
            this.InitializeBGW();

            this.workerPaused = false;
            this.worker.RunWorkerAsync(this.workerData);
            string hPath = @"Data\HENC\";
            string hExt = Properties.Settings.Default.extFile;
            _totalFileCount = FolderContentsCount(fileopen.Text);
            string HENCpath = AppDomain.CurrentDomain.BaseDirectory + hPath;

            FastZip fastZip = new FastZip();
            bool recurse = true; // Inkludera alla filer, + katalogstrukturer.
            string filter = null; //inget filter, tar allt!
            string dirPath = fileopen.Text; //Källa
            string sPath = HENCpath + txName.Text + hExt; //Källa +filnam, sparas

            fastZip.CreateZip(sPath, dirPath, recurse, filter);

        }
        void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            DoTheWork(e);
        }

        private void ProcessFileMethod(object sender, ScanEventArgs args)
        {
            _uptoFileCount++;
            int percentCompleted = _uptoFileCount * 100 / _totalFileCount;
            // do something here with a progress bar

            // file counts are easier as sizes take more work to calculate, and compression levels vary by file type
            string fileName = args.Name;
            // To terminate the process, set args.ContinueRunning = false
            if (fileName == "stop on this file")
                args.ContinueRunning = false;

        }
        private void DoTheWork(DoWorkEventArgs e)
        {
            int startPosition = (int)e.Argument;
            for (int i = startPosition; i < 101; i++)
            {
                System.Threading.Thread.Sleep(90);
                worker.ReportProgress(i, "BGW Compressing, Encrypting ...");
                if (worker.CancellationPending)
                {
                    e.Cancel = true;  // Avslutar loopen ordentligt.
                    i = 100; 

                }
            }
            return;
        }
        void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            this.workerData = e.ProgressPercentage;
            string progressReport = (string)e.UserState;
            this.prgBar.Value = this.workerData;
            return;
        }

        void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
        //Laddar egen messagebox.
         msgBox MsgBox = new msgBox();

        //Bytar källa beroende på Status på MSGBOX formen. Läses in i if och else satserna.
        BitmapImage bmiOK = new BitmapImage(new Uri("ok_like.png", UriKind.Relative));
        BitmapImage bmiER = new BitmapImage(new Uri("error_cooking.png", UriKind.Relative));

       //kortar ner , ska implenteras i user applications settings senare.
       string contMSGOK = "Compressed & Encrypted!";
       string contMSGER = "A Error was encounted\r\nCheck Log!";
       string rsaErr = "No Key!, Set it before Encrypting";
       string rsaOK = "Keys are Set!";

            if (e.Error != null)
            {
                MsgBox.imgvalid.Source = bmiER;
                MsgBox.txtBlock.Text = contMSGER;
                lstbox1.Items.Add(Environment.NewLine + "Processing folder: " + Environment.NewLine + fileopen.Text + "with total files: " + Environment.NewLine + _totalFileCount + "errors where predicted" + Environment.NewLine + "check settings!");
                MsgBox.ShowDialog();
            }
            else
            {
                settings Sett = new settings();
                //lagrar nycklar i container.
                cspp.KeyContainerName = keyName;
                rsa = new RSACryptoServiceProvider(cspp);
                MsgBox.txtBlock.Text = rsaOK;
                MsgBox.imgvalid.Source = bmiER;
                MsgBox.ShowDialog();

//--------------------------------------------------------------------------------------------------------------------------------------------
// Äldre försök fungerade inte på rätt sett, ingen error handler.
//                //rsa.PersistKeyInCsp = true;
//                if (rsa == null)
//                    lstbox1.Items.Add(Environment.NewLine + "Public: " + Environment.NewLine + cspp.KeyContainerName + Environment.NewLine);
//                else
//                    lstbox1.Items.Add(Environment.NewLine + "Public: " + Environment.NewLine + cspp.KeyContainerName + Environment.NewLine);
//----------------------------------------------------------------------------------------------------------------------------------------------

                if (rsa == null)
                {
                    MsgBox.txtBlock.Text = rsaErr;
                    MsgBox.imgvalid.Source = bmiER;    
                    MsgBox.ShowDialog();
                }
                else
                {
                    string fName = EncrFolder + txName.Text + ".henc";
                    //till filhanteringen
                    string oldenc = EncrFolder + txName.Text + ".enc";
                    if (fName != null)
                    {
                        FileInfo fInfo = new FileInfo(fName);
                        // Pass the file name without the path. 
                        string name = fInfo.FullName;
                        EncryptFile(name);
                    }
                    MsgBox.imgvalid.Source = bmiOK;
                    MsgBox.txtBlock.Text = contMSGOK;
                    //skickar status till Loggen...
                    lstbox1.Items.Add(Environment.NewLine + "Folder " + fileopen.Text + "with total: " + _totalFileCount + " files " + Environment.NewLine + "was Compressed and Encrypted" + Environment.NewLine + "Without errors" + Environment.NewLine);
                    //Hanterar gamla och nya encrypterade filer.
                    FileInfo enF = new FileInfo(fName);
                    enF.Delete();
                    FileInfo f = new FileInfo(oldenc);
                    f.CopyTo(fName);
                    f.Delete();
                    prgBar.Value = 0;
                    lstBrow1.Items.Clear();
                    GridVList();
                    this.worker.Dispose();
                    return;
                }
            }
        }
        void worker_Disposed(object sender, EventArgs e)
        {
            return;
        }
        // Returns the number of files in this and all subdirectories
        private int FolderContentsCount(string path)
        {
            int result = Directory.GetFiles(path).Length;
            string[] subFolders = Directory.GetDirectories(path);
            foreach (string subFolder in subFolders)
            {
                result += FolderContentsCount(subFolder);
            }
            return result;
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void btnprKey_Click(object sender, RoutedEventArgs e)
        {

            //msgBox MsgBox = new msgBox();


            H_ENC___Helix_Encryption.settings settings = new settings();
            settings.ShowDialog();
        }


        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            lstbox1.Items.Clear();
        }

        private void DecButton_Click(object sender, RoutedEventArgs e)
        {
            //Laddar in egen MessageBox.
           msgBox MsgBox = new msgBox();

        //Bytar källa beroende på Status på MSGBOX formen. Läses in i if och else satserna.
        BitmapImage bmiOK = new BitmapImage(new Uri("ok_like.png", UriKind.Relative));
        BitmapImage bmiER = new BitmapImage(new Uri("error_cooking.png", UriKind.Relative));

        //kortar ner , ska implenteras i user applications settings senare.
        string contMSGOK = "Decrypted & Uncompressed";
        string contMSGER = "A Error was encounted, Check Log!";
        string rsaErr = "No Key!, Set it before Encrypting";
        string rsaOK = "Keys are Set!";
            
            string tarG;
            tarG = Properties.Settings.Default.defaultCompress;
            // Fetcher.
            cspp.KeyContainerName = keyName;
            rsa = new RSACryptoServiceProvider(cspp);
            rsa.PersistKeyInCsp = true;
            
            var selectedItem = (dynamic)lstBrow1.SelectedItems[0];
            // Lagrar nyckelpar i cont.
            if (rsa == null)
            {
                MsgBox.imgvalid.Source = bmiER;
                MsgBox.txtBlock.Text = rsaErr;
                MsgBox.ShowDialog();
            }
            else
            {
                DecryptFile(selectedItem.File); // Decrypt på vald fil.
                //Loggar
                lstbox1.Items.Add(Environment.NewLine + "Got the keys... " + Environment.NewLine);
                lstbox1.Items.Add(Environment.NewLine + "File " + selectedItem.File + "with total: " + _totalFileCount + " files " + Environment.NewLine + "was Decrypted and uncompressed" + Environment.NewLine + "Without errors" + Environment.NewLine);
                // Filhantering om det lyckas!
                string fName = EncrFolder + selectedItem.File;
                if (fName != null)
                {
                    FileInfo fInfo = new FileInfo(fName);
                    string name = fInfo.FullName;
                    FileInfo enF = new FileInfo(fName);
                    enF.Delete();
                    //GridView ListView, uppdateras!
                    lstBrow1.Items.Clear();
                    GridVList();
                    
                    uZip(Properties.Settings.Default.dec_last, tarG); // Kör igång uppackning.
                    
                }
            }
        }
        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            lstBrow1.Items.Clear();
            GridVList();
        }
            }
        }
