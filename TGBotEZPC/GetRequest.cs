using System.Net;

namespace TGBotEZPC;

public class GetRequest
{
    private HttpWebRequest _request;
    private string _address;
    public string Response { get; set; }
    public string Accept { get; set; }
    public string Host { get; set; }
    public WebProxy Proxy { get; set; }
    public Dictionary<string, string> Headers { get; set; }
    
    public GetRequest(string address)
    {
        _address = address;
        Headers = new Dictionary<string, string>();
    }

    public void Run()
    {
        _request = (HttpWebRequest)WebRequest.Create(_address);
        _request.Method = "Get";

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
        _request.Method = "Get";
        _request.CookieContainer = cookieContainer;
        _request.Accept = Accept;
        _request.Host = Host;
        _request.Proxy = Proxy;

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