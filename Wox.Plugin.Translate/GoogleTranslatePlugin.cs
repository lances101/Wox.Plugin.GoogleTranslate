using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Web;
using System.Windows;

namespace Wox.Plugin.GoogleTranslate
{
    /// <summary>
    ///     Google Translate plugin for Wox. Parses the website, does not use the API. This is against the TOS.
    /// </summary>
    public class GoogleTranslatePlugin : IPlugin, IContextMenu
    {
        /// <summary>
        ///     Name of the file that is checked for in the plugin's directory.
        /// </summary>
        private const string FirstRunFileName = "accepted_terms";

        /// <summary>
        ///     Identifier of the top thread. Is rewritten every time a new query is
        ///     executed and checked by already running queries to be able to return
        ///     without actually sending any requests.
        /// </summary>
        public static int topThreadId;

        /// <summary>
        ///     Stored <see cref="PluginInitContext" /> reference
        /// </summary>
        private PluginInitContext _context;

        private bool _isFirstRun;
        private static string _firstRunFilePath;

        public string[,] Languages =
        {
            {"Afrikaans", "af"}, {"Albanian", "sq"}, {"Arabic", "ar"},
            {"Azerbaijani", "az"}, {"Bengali", "bn"}, {"Basque", "eu"},
            {"Korean", "ko"}, {"Belarusian", "be"}, {"Bulgarian", "bg"},
            {"Catalan", "ca"}, {"Chinese Simplified", "zh-CN"},
            {"Chinese Traditional", "zh-TW"}, {"Croatian", "hr"}, {"Czech", "cs"},
            {"Danish", "da"}, {"Dutch", "nl"}, {"English", "en"},
            {"Esperanto", "eo"}, {"Estonian", "et"}, {"Filipino", "tl"},
            {"Finnish", "fi"}, {"French", "fr"}, {"Galician", "gl"},
            {"Georgian", "ka"}, {"German", "de"}, {"Greek", "el"},
            {"Gujarati", "gu"}, {"Haitian Creole", "ht"}, {"Hebrew", "iw"},
            {"Hindi", "hi"}, {"Hungarian", "hu"}, {"Icelandic", "is"},
            {"Indonesian", "id"}, {"Irish", "ga"}, {"Italian", "it"},
            {"Japanese", "ja"}, {"Kannada", "kn"}, {"Bengali", "bn"},
            {"Latin", "la"}, {"Latvian", "lv"}, {"Lithuanian", "lt"},
            {"Macedonian", "mk"}, {"Malay", "ms"}, {"Maltese", "mt"},
            {"Norwegian", "no"}, {"Persian", "fa"}, {"Polish", "pl"},
            {"Portuguese", "pt"}, {"Romanian", "ro"}, {"Russian", "ru"},
            {"Serbian", "sr"}, {"Slovak", "sk"}, {"Slovenian", "sl"},
            {"Spanish", "es"}, {"Swahili", "sw"}, {"Swedish", "sv"},
            {"Tamil", "ta"}, {"Telugu", "te"}, {"Thai", "th"}, {"Turkish", "tr"},
            {"Ukrainian", "uk"}, {"Urdu", "ur"}, {"Vietnamese", "vi"},
            {"Welsh", "cy"}, {"Yiddish", "yi"}
        };

        /// <summary>
        ///     Returns a list of <see cref="Result" /> as the contextual menu for a specific <see cref="Result" />
        /// </summary>
        /// <param name="selectedResult">
        ///     The selected result.
        /// </param>
        /// <returns>
        ///     The <see cref="List" />.
        /// </returns>
        public List<Result> LoadContextMenus(Result selectedResult)
        {
            return new List<Result>
            {
                new Result
                {
                    Title = "Copy to clipboard",
                    IcoPath = "Images\\pic.png",
                    Action = e =>
                    {
                        Clipboard.SetText(selectedResult.Title);
                        return true;
                    }
                }
            };
        }

        public void Init(PluginInitContext context)
        {
            _context = context;
            _firstRunFilePath = Path.Combine(context.CurrentPluginMetadata.PluginDirectory, FirstRunFileName);
            _isFirstRun = GetFirstRunStatus(_firstRunFilePath);
        }

        public List<Result> Query(Query query)
        {
            if (_isFirstRun)
            {
                var result = ShowFirstRunDialog();
                if (result != MessageBoxResult.OK)
                {
                    return null;
                }
                using (File.Create(_firstRunFilePath)) { }
                _isFirstRun = false;
            }
            topThreadId = Thread.CurrentThread.ManagedThreadId;
            var list = new List<Result>();
            if (query.ThirdSearch == string.Empty || query.ThirdSearch == " ")
            {
                for (var index = 0; index < Languages.GetLength(0); ++index)
                {
                    var str = Languages[index, 0];
                    var countrycode = Languages[index, 1];
                    list.Add(
                        new Result
                        {
                            Title = str,
                            SubTitle = countrycode,
                            IcoPath = "Images\\pic.png",
                            Action = e =>
                            {
                                _context.API.ChangeQuery(query.RawQuery + countrycode + " ");
                                return false;
                            }
                        });
                }
            }
            else
            {
                var input = query.SecondToEndSearch.Substring(query.SecondToEndSearch.IndexOf(query.ThirdSearch));
                var from = query.FirstSearch;
                var to = query.SecondSearch;
                Thread.Sleep(500);
                if (topThreadId != Thread.CurrentThread.ManagedThreadId) return list;
                var str = TranslateText(input, from, to);
                list.Add(
                    new Result
                    {
                        Title = str,
                        SubTitle = "from " + from + " to " + to + " : " + input,
                        IcoPath = "Images\\pic.png",
                        Action = e =>
                        {
                            Process.Start(
                                "http://www.google.com/translate_t?hl=en&ie=UTF8&text=" + input
                                + "&langpair=" + from + "|" + to);
                            return false;
                        }
                    });
            }

            return list;
        }

        /// <summary>
        ///     Returns true if this is the first time this plugin is used
        /// </summary>
        /// <returns>
        ///     The <see cref="bool" />.
        /// </returns>
        public bool GetFirstRunStatus(string checkFilePath)
        {
            return !File.Exists(checkFilePath);
        }

        /// <summary>
        ///     Displays the dialog notifying the user that the plugin does not use the official Google Translate API
        /// </summary>
        private MessageBoxResult ShowFirstRunDialog()
        {
            const string titleText = "Google Translate plugin";
            const string mainText =
                "This plugin does not use the official Google Translate API.\r\n"
                + "Using this plugin is against Google's Terms of Service.\r\n"
                + "We are not responsible for any repercussions that happen \r\n"
                + "due to the use of this plugin.\r\n"
                + "Pressing OK confirms that you acknowledge this.\r\n" 
                + "If you do not agree with this, uninstall the plugin.";
            return MessageBox.Show(mainText, titleText, MessageBoxButton.OKCancel, MessageBoxImage.Warning);
        }

        /// <summary>
        ///     Translates the <paramref name="input" /> using the languages passed
        ///     in the <paramref name="from" /> and <paramref name="to" /> parameters
        ///     by formatting a url with them and executing a query against
        ///     Google's servers. The result is then parsed by extracting the
        ///     translation from the HTML file
        /// </summary>
        /// <param name="input">
        ///     text to translate
        /// </param>
        /// <param name="from">
        ///     language code to translate from
        /// </param>
        /// <param name="to">
        ///     language code to translate to
        /// </param>
        /// <returns>
        ///     resulting translation
        /// </returns>
        public string TranslateText(string input, string from, string to)
        {
            var str1 =
                GetPageHtml($"http://www.google.com/translate_t?hl=en&ie=UTF8&text={input}&langpair={from}|{to}");
            var str2 = str1.Substring(str1.IndexOf("<span title=\"") + "<span title=\"".Length);
            var str3 = str2.Substring(str2.IndexOf(">") + 1);
            return HttpUtility.HtmlDecode(str3.Substring(0, str3.IndexOf("</span>")).Trim());
        }

        /// <summary>
        ///     Downloads the page located at the specified <paramref name="url" />,
        ///     returns it as a string
        /// </summary>
        /// <param name="url">url to download the document from</param>
        /// <returns>downloaded string</returns>
        public string GetPageHtml(string url)
        {
            var client = new WebClient {Encoding = Encoding.UTF8};
            client.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");

            using (client)
            {
                try
                {
                    return client.DownloadString(url);
                }
                catch
                {
                    return null;
                }
            }
        }
    }
}