using System;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RedditSharp;
using EasyCapture;
using System.Web;
using System.IO;
using System.Threading;
using System.Text.RegularExpressions;
using System.Diagnostics;
using Newtonsoft.Json;

namespace ScreenshotBot
{
    class Core
    {
        static void Main(string[] args)
        {
            Init();
            monitor();
        }

        static Reddit reddit;
        static Subreddit sub;
        static IniFile ini;

        static BlackList popular;

        static List<Post> Work1;
        static List<Post> Work2;

        static bool useWorker2 = false;

        static List<string> ProcessedURLs;

        static string www = "";
        static string template = "";

        static void Init()
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("ScreenshotBot is bootstrapping...");
            
            try
            {
                reddit = new Reddit();
                ini = new IniFile("sst.ini");
                reddit.LogIn(ini.IniReadValue("ACCOUNT", "Username"), ini.IniReadValue("ACCOUNT", "Password"));
                var inisub = ini.IniReadValue("BOT", "Subreddit");
                if (inisub == "all") sub = Subreddit.GetRSlashAll(reddit);
                else sub = reddit.GetSubreddit(inisub);
                popular = new BlackList("popular", ini);
                www = ini.IniReadValue("BOT", "www");
                template = ini.IniReadValue("BOT", "Template");

                Work1 = new List<Post>();
                Work2 = new List<Post>();
                ProcessedURLs = new List<string>();

                ThreadPool.QueueUserWorkItem(new WaitCallback(ScreenshotWorker), 1);
                ThreadPool.QueueUserWorkItem(new WaitCallback(ScreenshotWorker), 2);
                
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Initialization failure! Error details: \r\n" + ex.ToString());
                Console.ReadLine();
                return;
            }
            

            Console.WriteLine("ScreenshotBot was successfully initialized!\n----------------------------------------------------------\r\n\r\n");
        }

        static void monitor()
        {
            while (true)
            {
                var start = new Stopwatch();
                start.Start();

                var posts = sub.GetNew();
                foreach(Post post in posts) {
                    if (Scan(post))
                    {
                        useWorker2 = !useWorker2;
                        if (useWorker2)
                        {
                            Work2.Add(post);
                        }
                        else
                        {
                            Work1.Add(post);
                        }
                    }
                }


                var wait = 2000 - start.ElapsedMilliseconds;
                wait = wait < 0 ? 0 : wait;

                Console.WriteLine(string.Format("Fetched {0} results. Sleeping {1} ms...", posts.Count(), wait));
                Thread.Sleep(Convert.ToInt32(wait));
            }
        }

        static bool Scan(Post post)
        {
            if (post.IsSelfPost) return false;
            if(ProcessedURLs.Contains(post.Url)) return false;
            else ProcessedURLs.Add(post.Url);

            //domain detection

            /*var domain = Regex.Matches(post.Url, "(?:http://|https://)([\\dA-Za-z]+\\.[\\d.A-Za-z]+)");
            var subdomain = Regex.Matches(post.Url, "(?:http://|https://)[\\d.A-Za-z]+\\.([\\dA-Za-z]+\\.[A-Za-z]+)");*/

            var domain = Regex.Matches(post.Url, "(?:http://|https://)[\\d.A-Za-z]+\\.([\\dA-Za-z]+\\.[A-Za-z]+)");
            var subdomain = Regex.Matches(post.Url, "(?:http://|https://)([\\dA-Za-z]+\\.[\\d.A-Za-z]+)");

            if (domain.Count != 1) return false;
            string rdomain = domain[0].Groups[1].Value;
            if (popular.isBlackListed(rdomain)) return false;
            if (subdomain.Count == 1)
            {
                string rsubdomain = subdomain[0].Groups[1].Value;
                if(popular.isBlackListed(rsubdomain)) return false;
            }

            return true;
        }

        public static bool IsRunningOnMono()
        {
            return Type.GetType("Mono.Runtime") != null;
        }

        static void ScreenshotWorker(object arg)
        {
            int worker = (int)arg;
            Console.WriteLine("Worker #"+worker+" online");
            while (true)
            {
                Post[] work = new Post[worker == 1 ? Work1.Count : Work2.Count];
                if(worker == 2) Work2.CopyTo(work);
                else Work1.CopyTo(work);

                if (work.Length == 0) Thread.Sleep(100);
                foreach (Post post in work)
                {
                    var path = "screenshots/" + Path.GetRandomFileName().Replace(".", "") + ".png";
                    Process process = new Process();
                    if (IsRunningOnMono())
                    {
                        //we will now assume we're running in linux...
                        var args = string.Format("--auto-servernum wkhtmltoimage --use-xserver --width 1366 \"{1}\" \"{0}\"", path, post.Url);
                        Console.WriteLine(">>> xvfb-run " + args);
                        process.StartInfo = new ProcessStartInfo("xvfb-run", args);
                        process.Start();
                        process.WaitForExit();
                    }
                    else
                    {
                        //we're in windows
                        path = path.Replace("/", "\\");
                        var args = string.Format("--width 1366 \"{1}\" \"{0}\"", path, post.Url);
                        Console.WriteLine(">>> wkhtmltoimage " + args);
                        process.StartInfo = new ProcessStartInfo("wkhtmltopdf\\wkhtmltoimage.exe", args);
                        process.Start();
                        process.WaitForExit();
                    }
                    if (!File.Exists(path)) Console.WriteLine("Failed to take scrn of " + post.Url);
                    else
                    {
                        /*try
                        {*/
                            //Don't upload anymore...
                            //Upload(path);

                            var message = template;
                            message = message.Replace("{url}", www + Path.GetFileName(path));
                            message = message.Replace("\\n", "\n");
                            post.Comment(message);
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("Successfully mirrored & commented " + post.Id);
                            Console.ForegroundColor = ConsoleColor.Gray;
                        /*}
                        catch (Exception z)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Failed to upload " + path + "\r\n" + z.ToString());
                            Console.ForegroundColor = ConsoleColor.Gray;
                        }*/
                    }
                }

            }
        }

        static void Upload(string path)
        {
            var http = HttpWebRequest.Create("http://api.imgur.com/3/image");
            http.Method = "POST";
            http.Headers.Add("Authorization", "Client-ID " + ini.IniReadValue("IMGUR", "Key"));
            http.ContentType = "application/x-www-form-urlencoded";
            using (var writer = new StreamWriter(http.GetRequestStream()))
            {
                writer.Write(String.Format("image={0}&type=base64&name=Test", HttpUtility.UrlEncode(Convert.ToBase64String(File.ReadAllBytes(path)))));
                writer.Close();
            }
            var resp = http.GetResponse();
            var imgur_raw = Encoding.Default.GetString(ReadFully(resp.GetResponseStream()));
            Console.WriteLine(imgur_raw);
            dynamic imgur = JsonConvert.DeserializeObject(imgur_raw);
        }

        public static byte[] ReadFully(Stream input)
        {
            byte[] buffer = new byte[16 * 1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
            }
        }
    }
}
