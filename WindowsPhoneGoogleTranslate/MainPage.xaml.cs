

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Microsoft.Phone.Controls;
using System.Xml.Linq;
using System.Text.RegularExpressions;
using System.IO;
using Windows.Storage;
using SQLite;
using System.Text;

namespace WindowsPhoneGoogleTranslate
{
    public partial class MainPage : PhoneApplicationPage
    {
        Language _from;
        Language _to;

        // List of all the available languages.
        private List<Language> _languages = new List<Language>();

        // Http request / response manager.
        private WebClient _proxy = new WebClient();

        // The database path.
        public static string DB_PATH = Path.Combine(Path.Combine(ApplicationData.Current.LocalFolder.Path, "sample.sqlite"));

        // The sqlite connection.
        private SQLiteConnection dbConn;

        public MainPage()
        {
           
            
            // Create the database connection.
            dbConn = new SQLiteConnection(DB_PATH);
            // Create the table Task, if it doesn't exist.
            dbConn.CreateTable<Word>();

            InitializeComponent();

            Loaded += new RoutedEventHandler(MainPage_Loaded);
            _proxy.DownloadStringCompleted += new DownloadStringCompletedEventHandler(DownloadStringCompleted);

            // Complete tests

            #if( MAKE_TESTS )
            Tests test = new Tests();
            bool res = test.Make_tests();
            if (!res)
                Application.Current.Terminate();
            #endif

        }

        void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadLanguages();
        }

        void DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            string text = GetTranslationText(e.Result);

            txtOutput.Text = text;
        }

        private void btnTranslate_Click(object sender, RoutedEventArgs e)
        {
            Translate(new Language(), new Language(), "");
        }

        private void txtInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Translate( new Language(), new Language(), "" );
                btnTranslate.Focus();
            }
        }

        private void LoadLanguages()
        {
            XDocument langDocument = XDocument.Load("Languages.xml");

            _languages = (from language in langDocument.Descendants("language")
                          select new Language
                          {
                              Code = language.Element("code").Value,
                              Name = language.Element("name").Value
                          }).ToList();

            lbxFrom.ItemsSource = _languages;
            lbxTo.ItemsSource = _languages;
        }

        public void Translate( Language _from, Language _to, string _txtInput )
        {
            Language from = lbxFrom.SelectedItem as Language;
            Language to = lbxTo.SelectedItem as Language;


            // Retriving Data 
            var tp = dbConn.Query<Word>("select * from word where Language='" + from.Code + "' and targetLanguage='" + to.Code + "' and Text='" + txtInput.Text + "' ").FirstOrDefault();
            if (tp != null && (lbxFrom.SelectedItem != null || lbxTo.SelectedItem != null))
                txtOutput.Text = tp.targetText;

            else if (lbxFrom.SelectedItem == null || lbxTo.SelectedItem == null)
            {
                MessageBox.Show("Please, select languages.");
            }
            else
            {
                string url = "http://translate.google.com/translate_t?text=" + txtInput.Text + "&sl=" + from.Code + "&tl=" + to.Code;
                WebClient webclient = new WebClient();

                webclient.DownloadStringCompleted += new DownloadStringCompletedEventHandler(webclient_DownloadStringCompleted);
                webclient.DownloadStringAsync(new Uri(url, UriKind.RelativeOrAbsolute));

            }
        }

        public void Translate_test(Language _from, Language _to, string _txtInput)
        {
            Language from = _from;
            Language to = _to;
            
                string url = "http://translate.google.com/translate_t?text=" + _txtInput + "&sl=" + from.Code + "&tl=" + to.Code;
                WebClient webclient = new WebClient();

                webclient.DownloadStringCompleted += new DownloadStringCompletedEventHandler(webclient_DownloadStringCompleted);
                webclient.DownloadStringAsync(new Uri(url, UriKind.RelativeOrAbsolute));

           
        }

        void webclient_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            if (e.Error == null)
            {
                try
                {
                    Match CompareText = Regex.Match(e.Result, "(?<=<span id=result_box class=\"short_text\">)(.*?)(?=</span>)");
                    string[] TranslatedText = Regex.Split(CompareText.Value, "\">");
                    UTF8Encoding utf8 = new UTF8Encoding();
                    byte[] bytes = utf8.GetBytes(TranslatedText.GetValue(1).ToString());
                    txtOutput.Text = System.Text.Encoding.UTF8.GetString(bytes,0,bytes.Length);


                }
                catch (Exception ex) { }
            }

            Language from = lbxFrom.SelectedItem as Language;
            Language to = lbxTo.SelectedItem as Language;

            // Create a new task.
            Word task = new Word()
            {
                Language = from.Code,
                Text = txtInput.Text,
                targetLanguage = to.Code,
                targetText = txtOutput.Text

            };
            // Insert the new task in the Task table.
            dbConn.Insert(task);

        }

        private string GetTranslationText(string json)
        {
            string text = Regex.Match(json, "\"translatedText\":\"(.*?)\"").Groups[1].Value;

            return text;
        }
    }

    public sealed class Word
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Language { get; set; }
        public string Text { get; set; }
        public string targetLanguage { get; set; }
        public string targetText { get; set; }
    }
}