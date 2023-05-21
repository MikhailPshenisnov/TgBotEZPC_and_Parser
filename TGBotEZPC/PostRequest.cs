using System.Net;
using System.Text;

namespace TGBotEZPC;

public class PostRequest
{
    private HttpWebRequest _request;
    private string _address;
    public string Response { get; set; }
    public string Accept { get; set; }
    public string Host { get; set; }
    public WebProxy Proxy { get; set; }
    public Dictionary<string, string> Headers { get; set; }
    public string ContentType { get; set; }
    public string Data { get; set; }
    
    public PostRequest(string address)
    {
        _address = address;
        Headers = new Dictionary<string, string>();
    }
    
    public void Run()
    {
        _request = (HttpWebRequest)WebRequest.Create(_address);
        _request.Method = "Post";

        try
        {
            HttpWebResponse response = (HttpWebResponse)_request.GetResponse();
            var stream = response.GetResponseStream();

            if (stream != null) Response = new StreamReader(stream).ReadToEnd();
        }
        catch (Exception e)
        {
            Console.WriteLine("Что-то пошло не так");
            Console.WriteLine(e);
        }
    }
    
    public void Run(CookieContainer cookieContainer)
    {
        _request = (HttpWebRequest)WebRequest.Create(_address);
        _request.Method = "Post";
        _request.CookieContainer = cookieContainer;
        _request.Accept = Accept;
        _request.Host = Host;
        _request.Proxy = Proxy;
        _request.ContentType = ContentType;

        byte[] sentData = Encoding.UTF8.GetBytes(Data);
        _request.ContentLength = sentData.Length;
        Stream sendStream = _request.GetRequestStream();
        sendStream.Write(sentData, 0, sentData.Length);
        sendStream.Close();

        foreach (var pair in Headers)
        {
            _request.Headers.Add(pair.Key, pair.Value);
        }

        try
        {
            HttpWebResponse response = (HttpWebResponse)_request.GetResponse();
            var stream = response.GetResponseStream();

            if (stream != null) Response = new StreamReader(stream).ReadToEnd();
        }
        catch (Exception e)
        {
            Console.WriteLine("Что-то пошло не так");
            Console.WriteLine(e);
        }
    }
}