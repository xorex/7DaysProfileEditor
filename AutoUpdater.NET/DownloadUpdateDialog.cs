﻿using System;
using System.ComponentModel;
using System.Net.Cache;
using System.Windows.Forms;
using System.Net;
using System.IO;
using System.Diagnostics;
using System.Reflection;

namespace AutoUpdaterDotNET
{
    internal partial class DownloadUpdateDialog : Form
    {
        private readonly string _downloadURL;

        private string _tempPath;

        private WebClient _webClient;

        public DownloadUpdateDialog(string downloadURL)
        {
            InitializeComponent();

            _downloadURL = downloadURL;
        }

        private void DownloadUpdateDialogLoad(object sender, EventArgs e)
        {
            _webClient = new WebClient {CachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.NoCacheNoStore)};

            var uri = new Uri(_downloadURL);

            _tempPath = Path.Combine(Path.GetTempPath(), GetFileName(_downloadURL));

            _webClient.DownloadProgressChanged += OnDownloadProgressChanged;

            _webClient.DownloadFileCompleted += OnDownloadComplete;

            _webClient.DownloadFileAsync(uri, _tempPath);
        }

        private void OnDownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            progressBar.Value = e.ProgressPercentage;
        }

        private void OnDownloadComplete(object sender, AsyncCompletedEventArgs e) {
            Ourself.Rename();
            string _exePath = Application.ExecutablePath;
            File.Copy(_tempPath, _exePath);

            var processStartInfo = new ProcessStartInfo { FileName = _exePath, UseShellExecute = true };
            Process.Start(processStartInfo);
            if (Application.MessageLoop) {
                Application.Exit();
            }
            else {
                Environment.Exit(1);
            }
        }

        private static string GetFileName(string url, string httpWebRequestMethod = "HEAD")
        {
            try
            {
                var fileName = string.Empty;
                var uri = new Uri(url);
                if (uri.Scheme.Equals(Uri.UriSchemeHttp) || uri.Scheme.Equals(Uri.UriSchemeHttps))
                {
                    var httpWebRequest = (HttpWebRequest)WebRequest.Create(uri);
                    httpWebRequest.CachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.NoCacheNoStore);
                    httpWebRequest.Method = httpWebRequestMethod;
                    httpWebRequest.AllowAutoRedirect = false;
                    string contentDisposition;
                    using (var httpWebResponse = (HttpWebResponse) httpWebRequest.GetResponse())
                    {
                        if (httpWebResponse.StatusCode.Equals(HttpStatusCode.Redirect) ||
                            httpWebResponse.StatusCode.Equals(HttpStatusCode.Moved) ||
                            httpWebResponse.StatusCode.Equals(HttpStatusCode.MovedPermanently))
                        {
                            if (httpWebResponse.Headers["Location"] != null)
                            {
                                var location = httpWebResponse.Headers["Location"];
                                fileName = GetFileName(location);
                                return fileName;
                            }
                        }
                        contentDisposition = httpWebResponse.Headers["content-disposition"];
                    }
                    if (!string.IsNullOrEmpty(contentDisposition))
                    {
                        const string lookForFileName = "filename=";
                        var index = contentDisposition.IndexOf(lookForFileName, StringComparison.CurrentCultureIgnoreCase);
                        if (index >= 0)
                            fileName = contentDisposition.Substring(index + lookForFileName.Length);
                        if (fileName.StartsWith("\"") && fileName.EndsWith("\""))
                        {
                            fileName = fileName.Substring(1, fileName.Length - 2);
                        }
                    }
                }
                if (string.IsNullOrEmpty(fileName))
                {
                    fileName = Path.GetFileName(uri.LocalPath);
                }
                return fileName;
            }
            catch (WebException)
            {
                return GetFileName(url, "GET");
            }
        }

        private void DownloadUpdateDialog_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_webClient.IsBusy)
            {
                _webClient.CancelAsync();
                DialogResult = DialogResult.Cancel;
            }
            else
            {
                DialogResult = DialogResult.OK;
            }
        }
    }
}