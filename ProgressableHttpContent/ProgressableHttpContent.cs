using System.IO;
using System.Threading.Tasks;

namespace System.Net.Http {
    public class ProgressableHttpContent : HttpContent {
        private const int DEFAULT_BUFFER_SIZE = 5 * 4096; // 20KB

        private HttpContent _content;
        private int _bufferSize;
        private IProgress<long> _progress;

        /// <summary>
        /// Creates a proxy-like http content that reports its progress when it gets converted into a stream (e.g. when it uploads).
        /// The progress will be reported as a <see cref="UInt64"/> which represents the total bytes sent at this point.
        /// </summary>
        /// <param name="content">The original http content to send</param>
        /// <param name="progress">The progress implemetation for receiving progress updates</param>
        public ProgressableHttpContent(HttpContent content, IProgress<long> progress)
            : this(content, DEFAULT_BUFFER_SIZE, progress) { }

        /// <summary>
        /// Creates a proxy-like http content that reports its progress when it gets converted into a stream (e.g. when it uploads).
        /// The progress will be reported as a <see cref="UInt64"/> which represents the total bytes sent at this point.
        /// </summary>
        /// <param name="content">The original http content to send</param>
        /// <param name="bufferSize">The buffer size used to copy the stream to the requester</param>
        /// <param name="progress">The progress implemetation for receiving progress updates</param>
        public ProgressableHttpContent(HttpContent content, int bufferSize, IProgress<long> progress) {
            if (bufferSize <= 0) {
                throw new ArgumentOutOfRangeException(nameof(bufferSize));
            }

            _content = content ?? throw new ArgumentNullException(nameof(content));
            _bufferSize = bufferSize;
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));

            foreach (var header in content.Headers) {
                Headers.Add(header.Key, header.Value);
            }
        }

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context) {
            using (var contentStream = await _content.ReadAsStreamAsync()) {
                await contentStream.CopyToAsync(stream, _bufferSize, _progress);
            }
        }

        protected override bool TryComputeLength(out long length) {
            length = _content.Headers.ContentLength.GetValueOrDefault();
            return _content.Headers.ContentLength.HasValue;
        }

        protected override void Dispose(bool disposing) {
            if (disposing) {
                _content.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
