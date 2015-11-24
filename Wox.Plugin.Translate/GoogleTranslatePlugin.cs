// Decompiled with JetBrains decompiler
// Type: TranslateWox.Main
// Assembly: TranslateWox, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 0A51035E-8791-4FC6-8557-D3506E321CD1

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using System.Windows;

namespace Wox.Plugin.GoogleTranslate
{
    public class GoogleTranslatePlugin : IPlugin, IContextMenu
    {
        private PluginInitContext _context;

        #region Languages

        public string[,] Languages = new string[64, 2]
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

        public static int topThreadId;
        public List<Result> Query(Query query)
        {
            topThreadId = Thread.CurrentThread.ManagedThreadId;
            _context.API.StartLoadingBar();
            var list = new List<Result>();
            var actionParameters = query.ActionParameters;
            if (actionParameters.Count <= 2)
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
                            _context.API.ChangeQuery(query.RawQuery + countrycode + " ", false);
                            return false;
                        }
                    });
                }
            }
            else if (actionParameters.Count > 2)
            {
                var input = actionParameters[2];
                var from = actionParameters[0];
                var to = actionParameters[1];
                input = String.Join(" ", actionParameters.Skip(2).ToArray());
                Thread.Sleep(500);
                if (topThreadId != Thread.CurrentThread.ManagedThreadId)
                {
                    
                    return list;
                }
                
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
            var wc = new WebClient {Encoding = Encoding.UTF8};
            var data= wc.DownloadData(string.Format("http://www.google.com/translate_t?hl=en&ie=UTF8&text={0}&langpair={1}|{2}",
                input, @from, to));
            string charset = Regex.Match(wc.ResponseHeaders["Content-Type"], "(?<=charset=)[\\w-]+").Value;

            var str1 = Encoding.GetEncoding(charset).GetString(data);
            var str2 = str1.Substring(str1.IndexOf("<span title=\"") + "<span title=\"".Length);
            var str3 = str2.Substring(str2.IndexOf(">") + 1);
            
            return HttpUtility.HtmlDecode(str3.Substring(0, str3.IndexOf("</span>")).Trim());
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