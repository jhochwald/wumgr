#region

using System;
using System.IO;
using System.Net;
using System.Windows.Threading;

#endregion

namespace wumgr.Common;

internal class HttpTask
{
    //const int DefaultTimeout = 2 * 60 * 1000; // 2 minutes timeout
    private const int MF_BUFFER_SIZE = 1024;
    private readonly Dispatcher _mDispatcher;
    private readonly string _mUrl;
    private byte[] _bufferRead;
    private bool _canceled;
    private DateTime _lastTime;
    private int _mLength = -1;
    private int _mOffset = -1;
    private int _mOldPercent = -1;
    private HttpWebRequest _request;
    private HttpWebResponse _response;
    private Stream _streamResponse;
    private Stream _streamWriter;

    public HttpTask(string url, string dlPath, string dlName = null, bool update = false)
    {
        _mUrl = url;
        this.DlPath = dlPath;
        this.DlName = dlName;

        _bufferRead = null;
        _request = null;
        _response = null;
        _streamResponse = null;
        _streamWriter = null;
        _mDispatcher = Dispatcher.CurrentDispatcher;
    }

    public string DlPath { get; }

    public string DlName { get; private set; }

    // Abort the request if the timer fires.
    /*private static void TimeoutCallback(object state, bool timedOut)
{
    if (timedOut)
    {
        HttpWebRequest request = state as HttpWebRequest;
        if (request != null)
            request.Abort();
    }
}*/

    public bool Start()
    {
        _canceled = false;
        try
        {
            // Create a HttpWebrequest object to the desired URL. 
            _request = (HttpWebRequest)WebRequest.Create(_mUrl);
            //myHttpWebRequest.AllowAutoRedirect = false;

            /**
            * If you are behind a firewall and you do not have your browser proxy setup
            * you need to use the following proxy creation code.

            // Create a proxy object.
            WebProxy myProxy = new WebProxy();

            // Associate a new Uri object to the _wProxy object, using the proxy address
            // selected by the user.
            myProxy.Address = new Uri("http://myproxy");


            // Finally, initialize the Web request object proxy property with the _wProxy
            // object.
            myHttpWebRequest.Proxy=myProxy;
            ***/

            _bufferRead = new byte[MF_BUFFER_SIZE];
            _mOffset = 0;

            // Start the asynchronous request.
            IAsyncResult result = _request.BeginGetResponse(RespCallback, this);

            // this line implements the timeout, if there is a timeout, the callback fires and the request becomes aborted
            //ThreadPool.RegisterWaitForSingleObject(result.AsyncWaitHandle, new WaitOrTimerCallback(TimeoutCallback), request, DefaultTimeout, true);          
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine(@"
Main Exception raised!");
            Console.WriteLine(@"
Message:{0}", e.Message);
        }

        return false;
    }

    public void Cancel()
    {
        _canceled = true;
        _request?.Abort();
    }

    private void Finish(int success, int errCode, Exception error = null)
    {
        // Release the HttpWebResponse resource.
        if (_response != null)
        {
            _response.Close();
            _streamResponse?.Close();
            _streamWriter?.Close();
        }

        _response = null;
        _request = null;
        _streamResponse = null;
        _bufferRead = null;

        if (success == 1)
        {
            try
            {
                if (File.Exists(DlPath + @"\" + DlName))
                    File.Delete(DlPath + @"\" + DlName);
                File.Move(DlPath + @"\" + DlName + ".tmp", DlPath + @"\" + DlName);
            }
            catch
            {
                AppLog.Line("Failed to rename download {0}", DlPath + @"\" + DlName + ".tmp");
                DlName += ".tmp";
            }

            try
            {
                File.SetLastWriteTime(DlPath + @"\" + DlName, _lastTime);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            } // set last mod time
        }
        else if (success == 2)
        {
            AppLog.Line("File already downloaded {0}", DlPath + @"\" + DlName);
        }
        else
        {
            try
            {
                File.Delete(DlPath + @"\" + DlName + ".tmp");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            } // delete partial file

            AppLog.Line("Failed to download file {0}", DlPath + @"\" + DlName);
        }

        Finished?.Invoke(this, new FinishedEventArgs(success > 0 ? 0 : _canceled ? -1 : errCode, error));
    }

    private static string GetNextTempFile(string path, string baseName)
    {
        for (int i = 0; i < 10000; i++)
            if (!File.Exists(path + @"\" + baseName + "_" + i + ".tmp"))
                return baseName + "_" + i;
        return baseName;
    }

    private static void RespCallback(IAsyncResult asynchronousResult)
    {
        int success = 0;
        int errCode = 0;
        Exception error = null;
        HttpTask task = (HttpTask)asynchronousResult.AsyncState;
        try
        {
            // State of request is asynchronous.
            task._response = (HttpWebResponse)task._request.EndGetResponse(asynchronousResult);

            errCode = (int)task._response.StatusCode;

            Console.WriteLine(@"The server at {0} returned {1}", task._response.ResponseUri, task._response.StatusCode);

            string fileName = Path.GetFileName(task._response.ResponseUri.ToString());
            task._lastTime = DateTime.Now;

            Console.WriteLine(@"With headers:");
            foreach (string key in task._response.Headers.AllKeys)
            {
                Console.WriteLine(@"	{0}:{1}", key, task._response.Headers[key]);

                if (key.Equals("Content-Length", StringComparison.CurrentCultureIgnoreCase))
                {
                    task._mLength = int.Parse(task._response.Headers[key]);
                }
                else if (key.Equals("Content-Disposition", StringComparison.CurrentCultureIgnoreCase))
                {
                    string cd = task._response.Headers[key];
                    fileName = cd.Substring(cd.IndexOf("filename=", StringComparison.Ordinal) + 9).Replace("\"", "");
                }
                else if (key.Equals("Last-Modified", StringComparison.CurrentCultureIgnoreCase))
                {
                    task._lastTime = DateTime.Parse(task._response.Headers[key]);
                }
            }

            //Console.WriteLine(task.lastTime);

            task.DlName ??= fileName;

            FileInfo testInfo = new(task.DlPath + @"\" + task.DlName);
            if (testInfo.Exists && testInfo.LastWriteTime == task._lastTime && testInfo.Length == task._mLength)
            {
                task._request.Abort();
                success = 2;
            }
            else
            {
                // prepare download filename
                if (!Directory.Exists(task.DlPath))
                    Directory.CreateDirectory(task.DlPath);
                if (task.DlName.Length == 0 || task.DlName[0] == '?')
                    task.DlName = GetNextTempFile(task.DlPath, "Download");

                FileInfo info = new(task.DlPath + @"\" + task.DlName + ".tmp");
                if (info.Exists)
                    info.Delete();

                // Read the response into a Stream object.
                task._streamResponse = task._response.GetResponseStream();

                task._streamWriter = info.OpenWrite();

                // Begin the Reading of the contents of the HTML page and print it to the console.
                task._streamResponse!.BeginRead(task._bufferRead, 0, MF_BUFFER_SIZE, ReadCallBack, task);
                return;
            }
        }
        catch (WebException e)
        {
            if (e.Response != null)
            {
                string fileName = Path.GetFileName(e.Response.ResponseUri.AbsolutePath);

                task.DlName ??= fileName;

                FileInfo testInfo = new(task.DlPath + @"\" + task.DlName);
                if (testInfo.Exists)
                    success = 2;
            }

            if (success == 0)
            {
                errCode = -2;
                error = e;
                Console.WriteLine(@"
RespCallback Exception raised!");
                Console.WriteLine(@"
Message:{0}", e.Message);
                Console.WriteLine(@"
Status:{0}", e.Status);
            }
        }
        catch (Exception e)
        {
            errCode = -2;
            error = e;
            Console.WriteLine(@"
RespCallback Exception raised!");
            Console.WriteLine(@"
Message:{0}", e.Message);
        }

        task._mDispatcher.Invoke(() => { task.Finish(success, errCode, error); });
    }

    private static void ReadCallBack(IAsyncResult asyncResult)
    {
        int success = 0;
        int errCode = 0;
        Exception error = null;
        HttpTask task = (HttpTask)asyncResult.AsyncState;
        try
        {
            int read = task._streamResponse.EndRead(asyncResult);
            // Read the HTML page and then print it to the console.
            if (read > 0)
            {
                task._streamWriter.Write(task._bufferRead, 0, read);
                task._mOffset += read;

                int percent = task._mLength > 0 ? (int)((long)100 * task._mOffset / task._mLength) : -1;
                if (percent != task._mOldPercent)
                {
                    task._mOldPercent = percent;
                    task._mDispatcher.Invoke(() => { task.Progress?.Invoke(task, new ProgressEventArgs(percent)); });
                }

                // setup next read
                task._streamResponse.BeginRead(task._bufferRead, 0, MF_BUFFER_SIZE, ReadCallBack, task);
                return;
            }

            // this is done on finish
            //task.streamWriter.Close();
            //task.streamResponse.Close();
            success = 1;
        }
        catch (Exception e)
        {
            errCode = -3;
            error = e;
            Console.WriteLine(@"ReadCallBack Exception raised!");
            Console.WriteLine(@"Message:{0}", e.Message);
        }

        task._mDispatcher.Invoke(() => { task.Finish(success, errCode, error); });
    }

    public event EventHandler<FinishedEventArgs> Finished;
    public event EventHandler<ProgressEventArgs> Progress;

    public class FinishedEventArgs(int errCode = 0, Exception error = null) : EventArgs
    {
        public bool Success => errCode == 0;
        public bool Cancelled => errCode == -1;

        public string GetError()
        {
            if (error != null)
                return error.ToString();
            switch (errCode)
            {
                case 0: return "Ok";
                case -1: return "Canceled";
                default: return errCode.ToString();
            }
        }
    }

    public class ProgressEventArgs(int percent) : EventArgs
    {
        public readonly int Percent = percent;
    }
}