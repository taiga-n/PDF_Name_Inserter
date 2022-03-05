using System;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Reflection;

using PdfSharp;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using PdfSharp.Fonts;


namespace PdfNameInserter
{
    public partial class Form1 : Form
    {
        string dataPath = "";
        string CSVPath = "";
        string myDocumentPath = "";
        string masterPath = "";
        string fileName = "";
        bool isSelectedPdf = false;

        float xPos;
        float yPos;
        int fontSize;

        public Form1()
        {
            InitializeComponent();
            Start();
        }

        void Action()
        {
            if (!isSelectedPdf)
            {            
                MessageBox.Show("please select a PDF file", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }else if(CSVPath == "")
            {
                MessageBox.Show("please select a CSV file", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }else if (IsFileLocked(dataPath))
            {
                MessageBox.Show("Cannot access a PDF file because it's in use", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            else if (IsFileLocked(CSVPath))
            {
                MessageBox.Show("Cannot access a CSV file because it's in use", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Create a new PDF document
            PdfDocument document = PdfReader.Open(dataPath, PdfDocumentOpenMode.Import);


            int i = 0;//重複していた場合の新規フォルダ作成
            while (System.IO.Directory.Exists(masterPath + "\\" + fileName + (i == 0 ? "" : "_" + i.ToString())))
            {
                i++;
            }
            string parentFolderPath = masterPath + "\\" + fileName + (i == 0 ? "" : "_" + i.ToString());
            System.IO.Directory.CreateDirectory(parentFolderPath);

            foreach (string str in GetUserNames())
            {
                insertName(document, str, parentFolderPath);
            }

            System.Diagnostics.Process.Start("EXPLORER.EXE", parentFolderPath);
            //Process.Start("edited_" + fileName);

        }

        string[] GetUserNames()
        {
            System.IO.StreamReader sr = new System.IO.StreamReader(CSVPath, Encoding.GetEncoding("UTF-8"));
            string str = sr.ReadToEnd();
            return str.Split('\n').Select(s => s.Replace("\r", "")).Where(s => s != "").ToArray();
            
        }

        void insertName(PdfDocument _document, string _name, string _parentFolderPath)
        {
            PdfDocument newDocument = new PdfDocument();

            for (int Pg = 0; Pg < _document.Pages.Count; Pg++)
            {
                PdfPage pp = newDocument.AddPage(_document.Pages[Pg]);

                XGraphics gfx = XGraphics.FromPdfPage(pp);
                XFont font = new XFont("Gen Shin Gothic", fontSize, XFontStyle.Regular);

                gfx.DrawString(_name, font, XBrushes.Black, new XRect(-xPos, -yPos, pp.Width, pp.Height), XStringFormats.BottomRight);
            }

            newDocument.Info.Author = "DISTRIBUTED FOR " + _name;
            newDocument.Info.Keywords = "DISTRIBUTED FOR " + _name;
            newDocument.Save(_parentFolderPath + "\\" + _name + "_" + fileName);//fileName includes ".pdf"
        }

        void Start()
        {
            PdfSharp.Fonts.GlobalFontSettings.FontResolver = new JapaneseFontResolver();

            myDocumentPath = System.Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            masterPath = GetMasterPath();
            label4.Text = "保存先のパス : " + masterPath;

            loadSettings();
            refreshUI();
        }

        void loadSettings()
        {
            CSVPath = Properties.Settings.Default.CSVPath;
            if (!System.IO.File.Exists(CSVPath) || !isRightCSVPath(CSVPath)) CSVPath = "";
            xPos = Properties.Settings.Default.Xpos == 0 ? 5 : Properties.Settings.Default.Xpos;
            yPos = Properties.Settings.Default.Ypos == 0 ? 5 : Properties.Settings.Default.Ypos;
            fontSize = Properties.Settings.Default.FontSize == 0 ? 10 : Properties.Settings.Default.FontSize;
        }

        void SaveSettings()
        {
            Properties.Settings.Default.CSVPath = CSVPath;
            Properties.Settings.Default.Xpos = xPos;
            Properties.Settings.Default.Ypos = yPos;
            Properties.Settings.Default.FontSize = fontSize;
            Properties.Settings.Default.Save();
        }

        void refreshUI()
        {
            label3.Text = "CSV file path : " + CSVPath;
            fontsizeTB.Text = fontSize.ToString();
            XposTB.Text = xPos.ToString();
            YposTB.Text = yPos.ToString();
        }

        string GetMasterPath()
        {
            string _path = myDocumentPath + "\\PdfNameInserter";
            if (!System.IO.Directory.Exists(_path))
            {
                System.IO.Directory.CreateDirectory(_path);
            }
            return _path;
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            Action();
        }

        private void FormMain_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop, false);
                  
            dataPath = files[0];
            fileName = System.IO.Path.GetFileName(dataPath);

            if (files.Length > 1)
            {
                MessageBox.Show("Only one file can be loaded at once", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                isSelectedPdf = false;
            }
            else 
            if (System.IO.Path.GetExtension(dataPath) == ".pdf")
            {
                textBox1.Text = "Reading PDF file path : " + dataPath;
                isSelectedPdf = true;
            }
            else
            {
                MessageBox.Show("Please select a PDF format file", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                isSelectedPdf = false;
            }
            
        }

        private void FormMain_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.All;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            DialogResult dr = openFileDialog1.ShowDialog();
            if (dr == System.Windows.Forms.DialogResult.OK)
            {
                
                if (isRightCSVPath(openFileDialog1.FileName))
                {
                    CSVPath = openFileDialog1.FileName;
                    label3.Text = "CSV file path : " + CSVPath;
                    SaveSettings();
                }
                else
                {
                    MessageBox.Show("Please select a CSV file",　"ERROR",　MessageBoxButtons.OK,　MessageBoxIcon.Error);
                }
            }
        }

        bool isRightCSVPath(string _path)//is referencing csv files
        {
            return System.IO.Path.GetExtension(_path) == ".csv";
        }

        private void button3_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("EXPLORER.EXE", masterPath);
        }

        private bool IsFileLocked(string path)
        {
            System.IO.FileStream stream = null;

            try
            {
                stream = new System.IO.FileStream(path, System.IO.FileMode.Open, System.IO.FileAccess.ReadWrite, System.IO.FileShare.None);
            }
            catch
            {
                return true;
            }
            finally
            {
                if (stream != null)
                {
                    stream.Close();
                }
            }

            return false;
        }

        private void label8_Click(object sender, EventArgs e)
        {

        }

        private void fontsizeTB_TextChanged(object sender, EventArgs e)
        {
            int val;
            if (int.TryParse(fontsizeTB.Text, out val) && val > 0)
            {
                fontSize = val;
                SaveSettings();
            }
            else
            {
                MessageBox.Show("Please enter correct number", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void XposTB_TextChanged(object sender, EventArgs e)
        {
            float val;
            if (float.TryParse(XposTB.Text, out val))
            {
                xPos = val;
                SaveSettings();
            }
            else
            {
                MessageBox.Show("Please enter correct number", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }           
        }

        private void YposTB_TextChanged(object sender, EventArgs e)
        {
            float val;
            if (float.TryParse(YposTB.Text, out val))
            {
                yPos = val;
                SaveSettings();
            }
            else
            {
                MessageBox.Show("Please enter correct number", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    // font resolver to japanese font
    public class JapaneseFontResolver : IFontResolver
    {
        // 源真ゴシック（ http://jikasei.me/font/genshin/）
        private static readonly string GEN_SHIN_GOTHIC_MEDIUM_TTF =
            "PdfNameInserter.fonts.GenShinGothic-Monospace-Medium.ttf";

        public byte[] GetFont(string faceName)
        {
            switch (faceName)
            {
                case "GenShinGothic#Medium":
                    return LoadFontData(GEN_SHIN_GOTHIC_MEDIUM_TTF);
            }
            return null;
        }

        public FontResolverInfo ResolveTypeface(
                    string familyName, bool isBold, bool isItalic)
        {
            var fontName = familyName.ToLower();

            switch (fontName)
            {
                case "gen shin gothic":
                    return new FontResolverInfo("GenShinGothic#Medium");
            }

            // default fonts
            return PlatformFontResolver.ResolveTypeface("Arial", isBold, isItalic);
        }

        // read font file from embedded resources
        private byte[] LoadFontData(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();

            using (System.IO.Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                    throw new ArgumentException("No resource with name " + resourceName);

                int count = (int)stream.Length;
                byte[] data = new byte[count];
                stream.Read(data, 0, count);
                return data;
            }
        }
    }
}


