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
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using System.Xml.Linq;
using System.Text.RegularExpressions;

namespace WindowsPhoneGoogleTranslate
{
    public partial class MainPage : PhoneApplicationPage
    {
        // Developer application ID. Obtain a free key from https://code.google.com/apis/console/.
        private readonly string APP_ID = "YOUR-API-KEY";

        // Google Translate REST service URL.
        private readonly string SERVICE_URL = "https://www.googleapis.com/language/translate/v2?key={0}&source={1}&target={2}&q={3}";

        // List of all the available languages.
        private List<Language> _languages = new List<Language>();

        // Http request / response manager.
        private WebClient _proxy = new WebClient();

        // Constructor
        public MainPage()
        {
            InitializeComponent();

            Loaded += new RoutedEventHandler(MainPage_Loaded);
            _proxy.DownloadStringCompleted += new DownloadStringCompletedEventHandler(DownloadStringCompleted);
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
            Translate();
        }

        private void txtInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Translate();
                btnTranslate.Focus(); // Focus on something else to hide the keyboard ;-)
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

        private void Translate()
        {
            if (lbxFrom.SelectedItem == null || lbxTo.SelectedItem == null)
            {
                MessageBox.Show("Please, select languages.");
            }
            else
            {
                Language from = lbxFrom.SelectedItem as Language;
                Language to = lbxTo.SelectedItem as Language;

                string googleTranslateUrl = string.Format(SERVICE_URL, APP_ID, from.Code, to.Code, txtInput.Text);

                _proxy.DownloadStringAsync(new Uri(googleTranslateUrl));
            }
        }

        private string GetTranslationText(string json)
        {
            // You'd better use JSON serilization instead of regular expressions in order to parse the results.
            // Windows Phone 7 supports DataContractJsonSerializer.
            string text = Regex.Match(json, "\"translatedText\":\"(.*?)\"").Groups[1].Value;

            return text;
        }
    }
}