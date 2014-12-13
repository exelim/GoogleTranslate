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
using System.Threading;
using System.Net;

/*
Test plan:
 * Translate from Italian --> English
 * Translate from English --> Italian
 * Translate from English --> Russian
 * Translate from Russian --> English
 * Translate same word few times --> using database to prevent 2nd request to google
 * 
 * Translate numbers ( no effect )
 * Translate no existings word ( no effect )
*/
namespace WindowsPhoneGoogleTranslate
{
    class Tests
    {

        // Http request / response manager.
        private WebClient _proxy = new WebClient();

        // The database path.
        public static string DB_PATH = Path.Combine(Path.Combine(ApplicationData.Current.LocalFolder.Path, "test.sqlite"));

        // The sqlite connection.
        private SQLiteConnection dbConn;

        string txtOutput, txtInput;

        Language from;
        Language to;

        int resp = 0;

        public Tests( )
        {

             dbConn = new SQLiteConnection(DB_PATH);
            // Create the table Task, if it doesn't exist.
            dbConn.CreateTable<Word>();

            _proxy.DownloadStringCompleted += new DownloadStringCompletedEventHandler(DownloadStringCompleted);
        }

        public bool Make_tests()
        {
            bool test_1 = ItalianTOEnglish();
            bool test_2 = EnglishTOItalien();
            //bool test_3 = EnglishTORussian();
            //bool test_4 = RussianTOEnglish();
            //bool test_5 = DataBaseUsage();
            bool test_6 = TranslateNumbers();
            bool test_7 = TranslateNotExistingWord();

            return test_1 && test_2 && /*test_5 &&*/ test_6 && test_7;
        }

        public bool ItalianTOEnglish()
        {
            Language from = new Language();
            from.Code = "it";
            from.Name = "Italian";

            Language to = new Language();
            to.Code = "en";
            to.Name = "English";

            txtInput = "gatto";

            Translate_test( from, to );
                     
            return txtOutput == "cat" ? true : false ;
        }

        public bool EnglishTOItalien()
        {
            Language to = new Language();
            to.Code = "it";
            to.Name = "Italian";

            Language from= new Language();
            from.Code = "en";
            from.Name = "English";

            txtInput = "cat";

            Translate_test( from, to );
            
            return txtOutput == "gatto" ? true : false ;
        }

        public bool EnglishTORussian()
        {
            Language to = new Language();
            to.Code = "ru";
            to.Name = "Russian";

            Language from = new Language();
            from.Code = "en";
            from.Name = "English";

            txtInput = "cat";

            Translate_test(from, to);

            return txtOutput == "кот" ? true : false;
        }

        public bool RussianTOEnglish()
        {
            Language from = new Language();
            from.Code = "ru";
            from.Name = "Russian";

            Language to = new Language();
            to.Code = "en";
            to.Name = "English";

            txtInput = "кот";

            Translate_test(from, to);

            return txtOutput == "cat" ? true : false;
        }

        public bool DataBaseUsage()
        {
            Language to = new Language();
            to.Code = "it";
            to.Name = "Italian";

            Language from = new Language();
            from.Code = "en";
            from.Name = "English";

            txtInput = "dog";

            Translate_test(from, to);
            Translate_test(from, to);

            SQLiteConnection dbConn = new SQLiteConnection(MainPage.DB_PATH);

            var tp = dbConn.Query<Word>("select * from word where Language='" + from.Code + "' and targetLanguage='" + to.Code + "' and Text='" + txtInput + "' ").FirstOrDefault();
            txtOutput  = tp.targetText;

            return txtOutput == "cane" ? true : false;
        }

        public bool TranslateNumbers()
        {
            Language from = new Language();
            from.Code = "it";
            from.Name = "Italian";

            Language to = new Language();
            to.Code = "en";
            to.Name = "English";

            txtInput = "1234";

            Translate_test(from, to);

            return txtOutput == "1234" ? true : false;
        }

        public bool TranslateNotExistingWord()
        {
            Language from = new Language();
            from.Code = "it";
            from.Name = "Italian";

            Language to = new Language();
            to.Code = "en";
            to.Name = "English";

            txtInput = "adfadsf";

            Translate_test(from, to);

            return txtOutput == "adfadsf" ? true : false;
        }

        

        void DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            string text = GetTranslationText(e.Result);

            txtOutput = text;
        }

        public void Translate_test(Language _from, Language _to)
        {
            from = _from;
            to = _to;
            
                string url = "http://translate.google.com/translate_t?text=" + txtInput + "&sl=" + from.Code + "&tl=" + to.Code;
                WebClient webclient = new WebClient();

                webclient.DownloadStringCompleted += new DownloadStringCompletedEventHandler(webclient_DownloadStringCompleted);
                webclient.DownloadStringAsync (new Uri(url, UriKind.RelativeOrAbsolute));
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
                    txtOutput = System.Text.Encoding.UTF8.GetString(bytes,0,bytes.Length);


                }
                catch (Exception ex) { }
            }

            Language from1 = from;
            Language to1 = to;

            // Create a new task.
            Word task = new Word()
            {
                Language = from.Code,
                Text = txtInput,
                targetLanguage = to.Code,
                targetText = txtOutput

            };
            // Insert the new task in the Task table.
            dbConn.Insert(task);
            resp = 1;
        }

        private string GetTranslationText(string json)
        {
            string text = Regex.Match(json, "\"translatedText\":\"(.*?)\"").Groups[1].Value;

            return text;
        }




    }
}
