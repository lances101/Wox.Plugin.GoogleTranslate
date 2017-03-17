using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Threading;
using System.Web;
using System.Windows;

namespace Wox.Plugin.GoogleTranslate
{
    public class GoogleTranslatePlugin : IPlugin, IContextMenu
    {
        private PluginInitContext _context;

        #region Languages

        public string[,] Languages = new string[,]
        {
            {
                "Afrikaans",
                "af"
            },
            {
                "Albanian",
                "sq"
            },
            {
                "Arabic",
                "ar"
            },
            {
                "Azerbaijani",
                "az"
            },
            {
                "Bengali",
                "bn"
            },
            {
                "Basque",
                "eu"
            },
            {
                "Korean",
                "ko"
            },
            {
                "Belarusian",
                "be"
            },
            {
                "Bulgarian",
                "bg"
            },
            {
                "Catalan",
                "ca"
            },
            {
                "Chinese Simplified",
                "zh-CN"
            },
            {
                "Chinese Traditional",
                "zh-TW"
            },
            {
                "Croatian",
                "hr"
            },
            {
                "Czech",
                "cs"
            },
            {
                "Danish",
                "da"
            },
            {
                "Dutch",
                "nl"
            },
            {
                "English",
                "en"
            },
            {
                "Esperanto",
                "eo"
            },
            {
                "Estonian",
                "et"
            },
            {
                "Filipino",
                "tl"
            },
            {
                "Finnish",
                "fi"
            },
            {
                "French",
                "fr"
            },
            {
                "Galician",
                "gl"
            },
            {
                "Georgian",
                "ka"
            },
            {
                "German",
                "de"
            },
            {
                "Greek",
                "el"
            },
            {
                "Gujarati",
                "gu"
            },
            {
                "Haitian Creole",
                "ht"
            },
            {
                "Hebrew",
                "iw"
            },
            {
                "Hindi",
                "hi"
            },
            {
                "Hungarian",
                "hu"
            },
            {
                "Icelandic",
                "is"
            },
            {
                "Indonesian",
                "id"
            },
            {
                "Irish",
                "ga"
            },
            {
                "Italian",
                "it"
            },
            {
                "Japanese",
                "ja"
            },
            {
                "Kannada",
                "kn"
            },
            {
                "Bengali",
                "bn"
            },
            {
                "Latin",
                "la"
            },
            {
                "Latvian",
                "lv"
            },
            {
                "Lithuanian",
                "lt"
            },
            {
                "Macedonian",
                "mk"
            },
            {
                "Malay",
                "ms"
            },
            {
                "Maltese",
                "mt"
            },
            {
                "Norwegian",
                "no"
            },
            {
                "Persian",
                "fa"
            },
            {
                "Polish",
                "pl"
            },
            {
                "Portuguese",
                "pt"
            },
            {
                "Romanian",
                "ro"
            },
            {
                "Russian",
                "ru"
            },
            {
                "Serbian",
                "sr"
            },
            {
                "Slovak",
                "sk"
            },
            {
                "Slovenian",
                "sl"
            },
            {
                "Spanish",
                "es"
            },
            {
                "Swahili",
                "sw"
            },
            {
                "Swedish",
                "sv"
            },
            {
                "Tamil",
                "ta"
            },
            {
                "Telugu",
                "te"
            },
            {
                "Thai",
                "th"
            },
            {
                "Turkish",
                "tr"
            },
            {
                "Ukrainian",
                "uk"
            },
            {
                "Urdu",
                "ur"
            },
            {
                "Vietnamese",
                "vi"
            },
            {
                "Welsh",
                "cy"
            },
            {
                "Yiddish",
                "yi"
            }
        };

        #endregion

        public void Init(PluginInitContext context)
        {
            _context = context;
        }

        public List<Result> Query(Query query)
        {
            _context.API.StartLoadingBar();
            var list = new List<Result>();


            if (query.ThirdSearch == string.Empty || query.ThirdSearch == " ")
            {
                for (var index = 0; index < Languages.GetLength(0); ++index)
                {
                    var str = Languages[index, 0];
                    var countrycode = Languages[index, 1];
                        list.Add(new Result
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
                var str = TranslateText(input, from, to);
                list.Add(new Result
                {
                    Title = str,
                    SubTitle = "from " + from + " to " + to + " : " + input,
                    IcoPath = "Images\\pic.png",
                    Action = e =>
                    {
                        Process.Start("http://www.google.com/translate_t?hl=en&ie=UTF8&text=" + input + "&langpair=" +
                                      @from + "|" + to);
                        return false;
                    }
                });
            }
            return list;
        }
        public string TranslateText(string input, string from, string to)
        {
            var str1 = GetPageHtml($"http://www.google.com/translate_t?hl=en&ie=UTF8&text={input}&langpair={from}|{to}");
            var str2 = str1.Substring(str1.IndexOf("<span title=\"") + "<span title=\"".Length);
            var str3 = str2.Substring(str2.IndexOf(">") + 1);

            return HttpUtility.HtmlDecode(str3.Substring(0, str3.IndexOf("</span>")).Trim());
        }

        public static string GetPageHtml(string link)
        {
            var client = new WebClient { Encoding = Encoding.UTF8 };
            client.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");

            using (client)
            {
                try
                {
                    return client.DownloadString(link);
                }
                catch
                {
                    return null;
                }
            }

        }

        public List<Result> LoadContextMenus(Result selectedResult)
        {
            return new List<Result>
            {
                new Result
                {
                    Title = "Copy to clipboard",
                    IcoPath = "Images\\pic.png",
                    Action = (e) =>
                    {
                        Clipboard.SetText(selectedResult.Title);
                        return true;
                    }
                }
            };
        }
    }
}
