using System;
using System.Text;
using System.Net;
using System.Threading.Tasks;
using System.IO;
using Npgsql;

namespace HttpListenerExample
{
    class HttpServer
    {
        public static HttpListener listener;
        public static string url = "http://+:8082/";
        public static string cs = "Host=db-pg;Username=postgres;Password=postgres;Database=postgres";

        public static async Task HandleIncomingConnections()
        {
            bool runServer = true;
            while (runServer)
            {
                HttpListenerContext ctx = await listener.GetContextAsync();
                HttpListenerRequest req = ctx.Request;
                HttpListenerResponse resp = ctx.Response;
                if (req.Url.ToString() == "http://localhost:8082/links")
                {
                    if (req.HttpMethod == "GET") 
                        await HandleGetLinks(resp);
                    else 
                        HandlePostLinks(resp, req);
                    resp.Close();
                    continue;
                }
                string pageData = File.ReadAllText("./pages/index.html");
                byte[] data = Encoding.UTF8.GetBytes(String.Format(pageData));
                resp.ContentType = "text/html";
                resp.ContentEncoding = Encoding.UTF8;
                resp.ContentLength64 = data.LongLength;
                await resp.OutputStream.WriteAsync(data, 0, data.Length);
                resp.Close();
            }
        }

        private static async Task HandleGetLinks(HttpListenerResponse rs)
        {
            using var con = new NpgsqlConnection(cs);
            con.Open();
            var sql = "SELECT * from LINKS";
            using var cmd = new NpgsqlCommand(sql, con);
            var rdr = cmd.ExecuteReader();
            string addText = "";
            while (rdr.Read())
                addText += "uuid: "+ rdr["uuid"] + "<br>link: " + rdr["link"] + "<br><br>";
            con.Close();
            string page = File.ReadAllText("./pages/links.html") +addText+ "</body></html>";
            byte[] data = Encoding.UTF8.GetBytes(String.Format(page));
            rs.ContentType = "text/html";
            rs.ContentEncoding = Encoding.UTF8;
            rs.ContentLength64 = data.LongLength;
            await rs.OutputStream.WriteAsync(data, 0, data.Length);
        }

        private static void HandlePostLinks(HttpListenerResponse rs, HttpListenerRequest rq)
        {
            string uuid = Guid.NewGuid().ToString();
            string link = new StreamReader(rq.InputStream).ReadToEnd().ToLower().Replace("link=", String.Empty);
            string q = "INSERT INTO LINKS VALUES (\'" + uuid + "\', \'" + link + "\')";
            using var con = new NpgsqlConnection(cs);
            con.Open();
            using var cmd = new NpgsqlCommand(q, con);
            cmd.ExecuteNonQuery();
            con.Close();
            rs.Redirect("/links");
        }

        public static void Main(string[] args)
        {
            string query = "CREATE TABLE IF NOT EXISTS LINKS(UUID VARCHAR(256) CONSTRAINT id PRIMARY KEY, link VARCHAR(256))";
            using var con = new NpgsqlConnection(cs);
            con.Open();
            using var cmd = new NpgsqlCommand(query, con);
            cmd.ExecuteNonQuery();
            con.Close();
            listener = new HttpListener();
            listener.Prefixes.Add(url);
            listener.Start();
            Task listenTask = HandleIncomingConnections();
            listenTask.GetAwaiter().GetResult();
            listener.Close();
        }
    }
}
