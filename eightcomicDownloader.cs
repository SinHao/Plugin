using System;
using System.Collections.Generic;
using System.Text;
using CSNovelCrawler.Class;
using System.Text.RegularExpressions;
using System.Net;

namespace CSNovelCrawler.Plugin
{
    public class eightcomicDownloader : AbstractDownloader
    {
        public eightcomicDownloader()
        {
            CurrentParameter = new DownloadParameter
            {
                UserAgent = "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.1; WOW64; Trident/6.0)",
            };
        }
        
        private Dictionary<int, List<string>> ImgUrl;

        private string GetCH(string URL)
        {
            string ch = UseRegex(URL, "ch");
            if (string.IsNullOrEmpty(ch))
            {
                CurrentParameter.Url = URL;
                string sHTML_CODE = Network.GetHtmlSource(CurrentParameter, Encoding.GetEncoding("BIG5"));
                string BOOKNUM = UseRegex(sHTML_CODE, "URL");
                try
                {
                    int NUM = int.Parse(UseRegex(sHTML_CODE, "NUM"));
                    ch = GetCH(ChangeURL(NUM, BOOKNUM));
                }
                catch
                {
                    throw;
                }
            }
            else
            {
                TaskInfo.Url = URL;
            }
            return ch;
        }

        private string ChangeURL(int NUM, string BOOKNUM)
        {
            string NewURL="";
         
            if (NUM == 4 || NUM == 6 || NUM == 12 || NUM == 22) NewURL = "http://new.comicvip.com/show/cool-";
            if (NUM == 1 || NUM == 17 || NUM == 19 || NUM == 21) NewURL = "http://new.comicvip.com/show/cool-";
            if (NUM == 2 || NUM == 5 || NUM == 7 || NUM == 9) NewURL = "http://new.comicvip.com/show/cool-";
            if (NUM == 10 || NUM == 11 || NUM == 13 || NUM == 14) NewURL = "http://new.comicvip.com/show/best-manga-";
            if (NUM == 3 || NUM == 8 || NUM == 15 || NUM == 16 || NUM == 18 || NUM == 20) NewURL = "http://new.comicvip.com/show/best-manga-";

            return NewURL + BOOKNUM + ".html?ch=1";
        }

        public override bool Analysis()
        {
            try
            {
                string ch = GetCH(TaskInfo.Url);
                TaskInfo.Tid = UseRegex(TaskInfo.Url, "TID");
                CurrentParameter.Url = TaskInfo.Url;
                string sHTML_CODE = Network.GetHtmlSource(CurrentParameter, Encoding.GetEncoding("BIG5"));
                Regex r = new Regex("<title>(?<title>\\S*).*<\\/title>");
                Match m = r.Match(sHTML_CODE);
                if (m.Success)
                {
                    TaskInfo.Title = m.Groups["title"].Value;
                }
                
                string itemid = string.Empty;
                string chs = string.Empty;
                string allcodes = string.Empty;
                r = new Regex("var\\schs=(?<chs>\\d*);var\\sitemid=(?<itemid>\\d*);var\\sallcodes=\"(?<allcodes>.*)\";");
                m = r.Match(sHTML_CODE);
                if (m.Success)
                {
                    itemid = m.Groups["itemid"].Value;
                    chs = m.Groups["chs"].Value;
                    allcodes = m.Groups["allcodes"].Value;
                    ImgUrl = getImgUrl(itemid, chs, allcodes);
                    if (ImgUrl.Count != 0)
                    {                        
                        TaskInfo.BeginSection = 0;
                        TaskInfo.CurrentSection = 0;
                        TaskInfo.EndSection = TaskInfo.TotalSection;                         
                        return true;
                    }
                }                
            }
            catch
            {
                
            }            
            
            return false;
        }

        private Dictionary<int, List<string>> getImgUrl(string itemid, string chs, string allcodes)
        {
            Dictionary<int, List<string>> allImg = new Dictionary<int, List<string>>();
            string[] Codes = allcodes.Split('|');            
            for(int j=0;j<Codes.Length;j++)
            {
                List<string> Img = new List<string>();
                string[] Code = Codes[j].Split(' ');                
                for (int i = 1; i <= int.Parse(Code[3]); i++)
                {
                    int idx = (((i - 1) / 10) % 10) + (((i - 1) % 10) * 3);
                    Img.Add("http://img" + Code[1] + ".8comic.com/" + Code[2] + "/" + itemid + "/" + Code[0] + "/" + i.ToString("000") + "_" + Code[4].Substring(idx, 3) + ".jpg");                    
                }
                allImg.Add(j, Img);
            }
            TaskInfo.TotalSection = Codes.Length;
            return allImg;
        }

       public override bool Download()
       {
           for (; TaskInfo.BeginSection <= TaskInfo.EndSection && !CurrentParameter.IsStop; TaskInfo.BeginSection++)
           {
               int page = 1;
               try
               {                   
                   foreach (string Url in ImgUrl[TaskInfo.BeginSection-1])
                   {
                       string Img = Network.GetHtmlSource((HttpWebRequest)WebRequest.Create(Url), Encoding.GetEncoding(1251));
                       FileWrite.TxtWrire(Img, TaskInfo.SaveDirectoryName + "\\" + TaskInfo.Title + (TaskInfo.BeginSection).ToString() + "\\" + page.ToString("000") + ".jpg", Encoding.GetEncoding(1251));
                       page++;
                   }
               }
               catch
               {
                   FileWrite.TxtWrire("", TaskInfo.SaveDirectoryName + "\\" + page.ToString("000") + "下載失敗", Encoding.GetEncoding("big5"));

                   continue;
               }

               TaskInfo.HasStopped = CurrentParameter.IsStop;
           }

           bool finish = TaskInfo.CurrentSection == TaskInfo.EndSection;
           return finish;
       }

       public static string UseRegex(string url, string DataName)
       {
           string[] regexString = new string[]
            {
                "^http:\\/\\/new\\.comicvip\\.com\\/show\\/(?<TID>\\D.*)\\.\\S*\\?ch=(?<ch>\\d*)",
                "^http:\\/\\/\\w*\\.8comic\\.com\\/html\\/(?<TID>\\d*)\\.html",
                "onclick=\"cview\\(\\'(?<URL>\\d*)\\-\\S*\\'\\,(?<NUM>\\d*)\\)"
            };

           Regex r = new Regex("");
           Match m = r.Match(url);
           foreach (string i in regexString)
           {
               r = new Regex(i);
               m = r.Match(url);
               if (m.Success)
               {
                   return m.Groups[DataName].Value;
               }
           }
           return "";
       }
    }
}
