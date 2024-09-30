#region

using System;
using System.IO;
using System.Net;
using System.Windows.Threading;

#endregion

internal class HttpTask
{
    //const int DefaultTimeout = 2 * 60 * 1000; // 2 minutes timeout
    private const int BUFFER_SIZE = 1024;
    private byte[] BufferRead;
    private bool Canceled;
    private DateTime lastTime;
    private readonly Dispatcher mDispatcher;
    private int mLength = -1;
    private int mOffset = -1;

    private int mOldPercent = -1;
    private readonly string mUrl;
    private HttpWebRequest request;
    private HttpWebResponse response;
    private Stream streamResponse;
    private Stream streamWriter;

    public HttpTask(string Url, string DlPath, string DlName = null, bool Update = false)
    {
        mUrl = Url;
        this.DlPath = DlPath;
        this.DlName = DlName;

        BufferRead = null;
        request = null;
        response = null;
        streamResponse = null;
        streamWriter = null;
        mDispatcher = Dispatcher.CurrentDispatcher;
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
        Canceled = false;
        try
        {
            // Create a HttpWebrequest object to the desired URL. 
            request = (HttpWebRequest)WebRequest.Create(mUrl);
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

            BufferRead = new byte[BUFFER_SIZE];
            mOffset = 0;

            // Start the asynchronous request.
            IAsyncResult result = request.BeginGetResponse(RespCallback, this);

            // this line implements the timeout, if there is a timeout, the callback fires and the request becomes aborted
            //ThreadPool.RegisterWaitForSingleObject(result.AsyncWaitHandle, new WaitOrTimerCallback(TimeoutCallback), request, DefaultTimeout, true);          
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine("\nMain Exception raised!");
            Console.WriteLine("\nMessage:{0}", e.Message);
        }

        return false;
    }

    public void Cancel()
    {
        Canceled = true;
        if (request != null)
            request.Abort();
    }

    private void Finish(int Success, int ErrCode, Exception Error = null)
    {
        // Release the HttpWebResponse resource.
        if (response != null)
        {
            response.Close();
            if (streamResponse != null)
                streamResponse.Close();
            if (streamWriter != null)
                streamWriter.Close();
        }

        response = null;
        request = null;
        streamResponse = null;
        BufferRead = null;

        if (Success == 1)
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
                File.SetLastWriteTime(DlPath + @"\" + DlName, lastTime);
            }
            catch
            {
            } // set last mod time
        }
        else if (Success == 2)
        {
            AppLog.Line("File already downloaded {0}", DlPath + @"\" + DlName);
        }
        else
        {
            try
            {
                File.Delete(DlPath + @"\" + DlName + ".tmp");
            }
            catch
            {
            } // delete partial file

            AppLog.Line("Failed to download file {0}", DlPath + @"\" + DlName);
        }

        Finished?.Invoke(this, new FinishedEventArgs(Success > 0 ? 0 : Canceled ? -1 : ErrCode, Error));
    }

    public static string GetNextTempFile(string path, string baseName)
    {
        for (int i = 0; i < 10000; i++)
            if (!File.Exists(path + @"\" + baseName + "_" + i + ".tmp"))
                return baseName + "_" + i;
        return baseName;
    }

    private static void RespCallback(IAsyncResult asynchronousResult)
    {
        int Success = 0;
        int ErrCode = 0;
        Exception Error = null;
        HttpTask task = (HttpTask)asynchronousResult.AsyncState;
        try
        {
            // State of request is asynchronous.
            task.response = (HttpWebResponse)task.request.EndGetResponse(asynchronousResult);

            ErrCode = (int)task.response.StatusCode;

            Console.WriteLine("The server at {0} returned {1}", task.response.ResponseUri, task.response.StatusCode);

            string fileName = Path.GetFileName(task.response.ResponseUri.ToString());
            task.lastTime = DateTime.Now;

            Console.WriteLine("With headers:");
            foreach (string key in task.response.Headers.AllKeys)
            {
                Console.WriteLine("\t{0}:{1}", key, task.response.Headers[key]);

                if (key.Equals("Content-Length", StringComparison.CurrentCultureIgnoreCase))
                {
                    task.mLength = int.Parse(task.response.Headers[key]);
                }
                else if (key.Equals("Content-Disposition", StringComparison.CurrentCultureIgnoreCase))
                {
                    string cd = task.response.Headers[key];
                    fileName = cd.Substring(cd.IndexOf("filename=") + 9).Replace("\"", "");
                }
                else if (key.Equals("Last-Modified", StringComparison.CurrentCultureIgnoreCase))
                {
                    task.lastTime = DateTime.Parse(task.response.Headers[key]);
                }
            }

            //Console.WriteLine(task.lastTime);

            if (task.DlName == null)
                task.DlName = fileName;

            FileInfo testInfo = new(task.DlPath + @"\" + task.DlName);
            if (testInfo.Exists && testInfo.LastWriteTime == task.lastTime && testInfo.Length == task.mLength)
            {
                task.request.Abort();
                Success = 2;
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
                task.streamResponse = task.response.GetResponseStream();

                task.streamWriter = info.OpenWrite();

                // Begin the Reading of the contents of the HTML page and print it to the console.
                task.streamResponse.BeginRead(task.BufferRead, 0, BUFFER_SIZE, ReadCallBack, task);
                return;
            }
        }
        catch (WebException e)
        {
            if (e.Response != null)
            {
                string fileName = Path.GetFileName(e.Response.ResponseUri.AbsolutePath);

                if (task.DlName == null)
                    task.DlName = fileName;

                FileInfo testInfo = new(task.DlPath + @"\" + task.DlName);
                if (testInfo.Exists)
                    Success = 2;
            }

            if (Success == 0)
            {
                ErrCode = -2;
                Error = e;
                Console.WriteLine("\nRespCallback Exception raised!");
                Console.WriteLine("\nMessage:{0}", e.Message);
                Console.WriteLine("\nStatus:{0}", e.Status);
            }
        }
        catch (Exception e)
        {
            ErrCode = -2;
            Error = e;
            Console.WriteLine("\nRespCallback Exception raised!");
            Console.WriteLine("\nMessage:{0}", e.Message);
        }

        task.mDispatcher.Invoke(() => { task.Finish(Success, ErrCode, Error); });
    }

    private static void ReadCallBack(IAsyncResult asyncResult)
    {
        int Success = 0;
        int ErrCode = 0;
        Exception Error = null;
        HttpTask task = (HttpTask)asyncResult.AsyncState;
        try
        {
            int read = task.streamResponse.EndRead(asyncResult);
            // Read the HTML page and then print it to the console.
            if (read > 0)
            {
                task.streamWriter.Write(task.BufferRead, 0, read);
                task.mOffset += read;

                int Percent = task.mLength > 0 ? (int)((long)100 * task.mOffset / task.mLength) : -1;
                if (Percent != task.mOldPercent)
                {
                    task.mOldPercent = Percent;
                    task.mDispatcher.Invoke(() => { task.Progress?.Invoke(task, new ProgressEventArgs(Percent)); });
                }

                // setup next read
                task.streamResponse.BeginRead(task.BufferRead, 0, BUFFER_SIZE, ReadCallBack, task);
                return;
            }

            // this is done on finisch
            //task.streamWriter.Close();
            //task.streamResponse.Close();
            Success = 1;
        }
        catch (Exception e)
        {
            ErrCode = -3;
            Error = e;
            Console.WriteLine("\nReadCallBack Exception raised!");
            Console.WriteLine("\nMessage:{0}", e.Message);
        }

        task.mDispatcher.Invoke(() => { task.Finish(Success, ErrCode, Error); });
    }

    public event EventHandler<FinishedEventArgs> Finished;
    public event EventHandler<ProgressEventArgs> Progress;

    public class FinishedEventArgs : EventArgs
    {
        public int ErrCode;
        public Exception Error;

        public FinishedEventArgs(int ErrCode = 0, Exception Error = null)
        {
            this.ErrCode = ErrCode;
            this.Error = Error;
        }

        public bool Success => ErrCode == 0;
        public bool Cancelled => ErrCode == -1;

        public string GetError()
        {
            if (Error != null)
                return Error.ToString();
            switch (ErrCode)
            {
                case 0: return "Ok";
                case -1: return "Canceled";
                default: return ErrCode.ToString();
            }
        }
    }

    public class ProgressEventArgs : EventArgs
    {
        public int Percent;

        public ProgressEventArgs(int Percent)
        {
            this.Percent = Percent;
        }
    }
}
