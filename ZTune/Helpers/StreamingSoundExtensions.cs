using Engine;
using Engine.Audio;

namespace Game
{
    public static class StreamingSoundExtensions
    {
        // Lấy phần trăm tiến trình bài hát (Trả về từ 0.0f đến 1.0f)
        // Dùng số này để set giá trị cho ProgressBar UI
        public static float GetProgressPercentage(this StreamingSound sound)
        {
            // Dùng dấu '?' để check null an toàn và ngắn gọn
            if (sound?.StreamingSource != null)
            {
                long currentPos = sound.StreamingSource.Position;
                long totalBytes = sound.StreamingSource.BytesCount;

                if (totalBytes > 0)
                {
                    return (float)currentPos / totalBytes;
                }
            }
            return 0f;
        }

        // Lấy thời gian đã phát (tính bằng Giây)
        public static float GetCurrentTimeSeconds(this StreamingSound sound)
        {
            if (sound?.StreamingSource != null)
            {
                long currentPos = sound.StreamingSource.Position;
                int sampleRate = sound.SamplingFrequency;
                int channels = sound.ChannelsCount;

                // Công thức: Tần số * Số kênh * 2 byte (16-bit audio)
                int bytesPerSecond = sampleRate * channels * 2;

                if (bytesPerSecond > 0)
                {
                    return (float)currentPos / bytesPerSecond;
                }
            }
            return 0f;
        }

        // Lấy tổng thời lượng bài hát (tính bằng Giây)
        public static float GetTotalDurationSeconds(this StreamingSound sound)
        {
            if (sound?.StreamingSource != null)
            {
                long totalBytes = sound.StreamingSource.BytesCount;
                int sampleRate = sound.SamplingFrequency;
                int channels = sound.ChannelsCount;

                int bytesPerSecond = sampleRate * channels * 2;

                if (bytesPerSecond > 0)
                {
                    return (float)totalBytes / bytesPerSecond;
                }
            }
            return 0f;
        }

        // Trả về chuỗi định dạng thời gian đẹp (Ví dụ: "01:45 / 04:30")
        // Dùng để hiển thị lên LabelWidget
        public static string GetFormattedTime(this StreamingSound sound)
        {
            // Gọi lại chính các extension method ở trên
            int currentSeconds = (int)sound.GetCurrentTimeSeconds();
            int totalSeconds = (int)sound.GetTotalDurationSeconds();

            string currentStr = $"{currentSeconds / 60:D2}:{currentSeconds % 60:D2}";
            string totalStr = $"{totalSeconds / 60:D2}:{totalSeconds % 60:D2}";

            return $"{currentStr} / {totalStr}";
        }
    }
}
