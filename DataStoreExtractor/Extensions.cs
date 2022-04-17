using System;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace DataStoreExtractor
{
    internal static class Extensions
    {
        private class ProgressData
        {
            internal long BytesReceived = 0;
            internal long TotalBytesToReceive = -1;

            internal void Send(HttpClient httpClient, ProgressChangedEventHandler progressChangedEvent, CancellationToken cancellationToken)
            {
                int progressPercentage = TotalBytesToReceive < 0 ? 0 : TotalBytesToReceive == 0 ? 100 : (int)((100 * BytesReceived) / TotalBytesToReceive);
                progressChangedEvent.Invoke(httpClient, new ProgressChangedEventArgs(progressPercentage, cancellationToken));
            }
        }

        public static async Task DownloadFileTaskAsync(this HttpClient httpClient, Uri address, string fileName, ProgressChangedEventHandler progressChangedEvent)
        {
            await httpClient.DownloadFileTaskAsync(address.ToString(), fileName, progressChangedEvent);
        }

        public static async Task DownloadFileTaskAsync(this HttpClient httpClient, string address, string fileName, ProgressChangedEventHandler progressChangedEvent)
        {
            CancellationTokenSource source = new CancellationTokenSource();
            CancellationToken cancellationToken = source.Token;

            using (var responseMessage = await httpClient.GetAsync(address, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false))
            {
                responseMessage.EnsureSuccessStatusCode();
                var content = responseMessage.Content;
                if (content == null)
                {
                    return;
                }

                int bufferSize = 65536;

                var headers = content.Headers;
                var contentLength = headers.ContentLength;
                using (var responseStream = await content.ReadAsStreamAsync().ConfigureAwait(false))
                {
                    byte[] buffer = new byte[bufferSize];
                    int bytesRead;

                    var downloadProgress = new ProgressData();
                    if (contentLength.HasValue)
                    {
                        downloadProgress.TotalBytesToReceive = contentLength.Value;
                    }
                    downloadProgress.Send(httpClient, progressChangedEvent, cancellationToken);

                    using (FileStream fileStream = File.OpenWrite(fileName))
                    {
                        while ((bytesRead = await responseStream.ReadAsync(buffer, 0, bufferSize, cancellationToken).ConfigureAwait(false)) > 0)
                        {
                            fileStream.Write(buffer, 0, bytesRead);

                            downloadProgress.BytesReceived += bytesRead;
                            downloadProgress.Send(httpClient, progressChangedEvent, cancellationToken);
                        }
                    }

                    return;
                }
            }
        }
    }
}
