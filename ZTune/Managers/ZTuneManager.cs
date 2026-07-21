using Engine;
using Engine.Graphics;
using Engine.Audio;
using Engine.Media;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using System.IO;

namespace Game
{
    public static class ZTuneManager
    {
        public static string LogPrefix => "ZTune";

        // Danh sách lưu trữ các bài hát hợp lệ
        public static List<SongInfo> AvailableSongs = new List<SongInfo>();

        public static string MainDirectoryPath => Path.Combine(ModsManager.DocPath, "ZanJhat/ZTune");
        public static string CacheDirectoryPath => Path.Combine(MainDirectoryPath, "Cache");

        public static string JsonFilePath => Storage.CombinePaths(MainDirectoryPath, "MusicList_Cache.json");

        public static SongInfo CurrentPlayingSong; // Bài hát đang phát hiện tại
        public static MusicManager.Mix PreviousMix = MusicManager.Mix.Menu; // Lưu lại trạng thái âm thanh gốc của game

        public static BubbleWidget ZTuneBubbleWidget;

        public static void Initialize()
        {
            Load();
            Window.Frame += Update;
        }

        public static void Load()
        {
            // Kích hoạt tiến trình tải danh sách JSON chạy ngầm ở nền.
            // Dấu '_' báo cho C# biết rằng chúng ta bắn tín hiệu đi và không cần luồng chính phải chờ.
            _ = LoadMusicListAsync();

            LoadWidget();
        }

        public static async Task LoadMusicListAsync()
        {
            string jsonUrl = "https://raw.githubusercontent.com/ZanJhat/ZTune/main/MusicList.json";

            // Đảm bảo thư mục gốc của Mod đã được tạo sẵn
            if (!Storage.DirectoryExists(MainDirectoryPath))
                Storage.CreateDirectory(MainDirectoryPath);

            try
            {
                Log.Information($"[{LogPrefix}] Connecting to the server to fetch the music list...");

                // Cố gắng tải dữ liệu JSON từ GitHub
                byte[] jsonBytes = await WebManager.GetAsync(jsonUrl);

                // Phân tích dữ liệu JSON vừa tải
                AvailableSongs = JsonSerializer.Deserialize<List<SongInfo>>(jsonBytes);

                Log.Information($"[{LogPrefix}] Music list loaded successfully. Total songs: {AvailableSongs.Count}");

                // Lưu đè vào ổ cứng để làm cache fallback khi cần
                try
                {
                    using (Stream stream = Storage.OpenFile(JsonFilePath, OpenFileMode.Create))
                    {
                        await stream.WriteAsync(jsonBytes, 0, jsonBytes.Length);
                    }
                    Log.Information($"[{LogPrefix}] Music list cache updated.");
                }
                catch (Exception jsonEx)
                {
                    Log.Warning($"[{LogPrefix}] Failed to write JSON cache file: {jsonEx.Message}");
                }
            }
            catch (Exception ex)
            {
                // Xử lý khi mất mạng hoặc lỗi tải: kích hoạt hệ thống fallback offline
                Log.Warning($"[{LogPrefix}] Unable to connect to the server ({ex.Message}). Looking for offline cache...");

                if (Storage.FileExists(JsonFilePath))
                {
                    try
                    {
                        byte[] localBytes;

                        // Đọc file JSON từ bộ nhớ máy
                        using (Stream stream = Storage.OpenFile(JsonFilePath, OpenFileMode.Read))
                        using (MemoryStream ms = new MemoryStream())
                        {
                            await stream.CopyToAsync(ms);
                            localBytes = ms.ToArray();
                        }

                        // Phân tích dữ liệu từ file local
                        AvailableSongs = JsonSerializer.Deserialize<List<SongInfo>>(localBytes);
                        Log.Information($"[{LogPrefix}] Offline mode enabled. Loaded {AvailableSongs.Count} songs from the local cache.");
                    }
                    catch (Exception localEx)
                    {
                        Log.Error($"[{LogPrefix}] Critical error! Failed to read the local JSON cache file: {localEx.Message}");
                    }
                }
                else
                {
                    Log.Error($"[{LogPrefix}] Offline cache file not found on the device. Music list is empty.");
                }
            }

            // In log danh sách bài hát ra sau khi nạp thành công (Bất kể trực tuyến hay ngoại tuyến)
            if (AvailableSongs != null && AvailableSongs.Count > 0)
            {
                int index = 1;
                foreach (SongInfo song in AvailableSongs)
                {
                    Log.Information($"- {index++}. {song.Title} (by {song.Author})");
                }
            }
        }

        public static void LoadWidget()
        {
            ContainerWidget root = ScreensManager.RootWidget;
            if (root == null)
                return;

            ZTuneBubbleWidget = new();
            root.Children.Add(ZTuneBubbleWidget);

            ZTuneBubbleWidget.SetIcon(ContentManager.Get<Texture2D>("Textures/BubbleIcon"));
        }

        public static void Update()
        {
            ContainerWidget root = ScreensManager.RootWidget;
            if (root == null || ZTuneBubbleWidget == null)
                return;

            if (ZTuneBubbleWidget.IsClicked)
            {
                DialogsManager.ShowDialog(root, new ZTuneDialog());

                // Test
                /*if (AvailableSongs.Count > 0)
                    _ = PlaySongAsync(AvailableSongs[0]);*/
            }

            UpdateStateMix();
        }

        public static void UpdateStateMix()
        {
            if (CurrentPlayingSong != null)
            {
                if (MusicManager.IsPlaying)
                {
                    // Nếu nhạc đang phát, nhưng game cố tình đổi sang bài khác (VD: Load vào World, Menu)
                    if (MusicManager.CurrentMix != MusicManager.Mix.Other)
                    {
                        // Lưu lại đích đến mới của game
                        PreviousMix = MusicManager.CurrentMix;

                        // Ép buộc game quay lại trạng thái Other để bảo vệ bài hát
                        MusicManager.CurrentMix = MusicManager.Mix.Other;
                    }
                }
                else
                {
                    // Nhạc đã kết thúc (Hoặc bị tắt chủ động)
                    Log.Information($"[{LogPrefix}] Song '{CurrentPlayingSong.Title}' has finished. Restoring original music.");

                    CurrentPlayingSong = null;

                    // Trả lại quyền điều khiển (Mix) cho game gốc để nó tự bật nhạc sinh tồn
                    MusicManager.CurrentMix = PreviousMix;
                }
            }
            else
            {
                // Nếu không có nhạc nào đang phát, luôn luôn đồng bộ PreviousMix
                // Đề phòng trường hợp người chơi đang ở ngoài Menu (Mix.Menu) rồi vào World (Mix.InGame)
                if (MusicManager.CurrentMix != MusicManager.Mix.Other)
                {
                    PreviousMix = MusicManager.CurrentMix;
                }
            }
        }

        public static async Task PlaySongAsync(SongInfo song)
        {
            if (song == null)
                return;

            try
            {
                // Tạo thư mục Cache nếu chưa có
                if (!Storage.DirectoryExists(CacheDirectoryPath))
                    Storage.CreateDirectory(CacheDirectoryPath);

                // --- TẢI VÀ CACHE ICON BÀI HÁT ---
                if (!string.IsNullOrEmpty(song.IconUrl))
                {
                    try
                    {
                        // Kiểm tra RAM Cache trước tiên
                        if (song.LoadedIcon != null)
                        {
                            Log.Information($"[{LogPrefix}] Loading song icon from RAM cache: {song.Title}");

                            // Lấy luôn Texture trên RAM gán vào bong bóng, bỏ qua hoàn toàn việc đọc ổ cứng/mạng
                            ZTuneBubbleWidget?.SetIcon(song.LoadedIcon);
                        }
                        else
                        {
                            // Nếu RAM chưa có, tiến hành xử lý Disk Cache hoặc Tải mạng

                            // Lấy tên file ở cuối đường link URL của GitHub
                            // Ví dụ: "https://.../MusicList/DayScVN_Avatar.png" -> sẽ lấy ra "DayScVN_Avatar.png"
                            string iconFileName = Storage.GetFileName(song.IconUrl);

                            // Fallback nếu tên file trống, không có đuôi hợp lệ, hoặc chứa ký tự cấm
                            if (string.IsNullOrEmpty(iconFileName) || !iconFileName.Contains(".") || HasInvalidFileNameChars(iconFileName))
                            {
                                // Ép về tên file an toàn tuyệt đối, loại bỏ hoàn toàn ký tự cấm
                                // Cho dù tắt game bật lại 1000 lần, chuỗi URL lỗi này vẫn sẽ cho ra ĐÚNG một tên file duy nhất
                                int stableHash = GetDeterministicHashCode(song.IconUrl);
                                iconFileName = $"icon_{stableHash}.png";
                            }

                            string iconFilePath = Storage.CombinePaths(CacheDirectoryPath, iconFileName);

                            byte[] imgData = null;

                            // Kiểm tra xem file ảnh này đã tồn tại dưới ổ cứng chưa
                            if (!Storage.FileExists(iconFilePath))
                            {
                                Log.Information($"[{LogPrefix}] Downloading icon from network for: {song.Title}");

                                imgData = await WebManager.GetAsync(song.IconUrl);

                                // Lưu mảng byte của ảnh vào ổ cứng (Disk Cache)
                                if (imgData.Length > 0)
                                {
                                    using (Stream stream = Storage.OpenFile(iconFilePath, OpenFileMode.Create))
                                    {
                                        await stream.WriteAsync(imgData, 0, imgData.Length);
                                    }
                                }
                            }
                            else
                            {
                                // Nếu link ảnh trùng với bài đã tải trước đó, đọc luôn file cũ mà không cần tải lại!
                                Log.Information($"[{LogPrefix}] Loading shared icon from disk cache: {song.Title}");

                                using (Stream stream = Storage.OpenFile(iconFilePath, OpenFileMode.Read))
                                using (MemoryStream ms = new MemoryStream())
                                {
                                    await stream.CopyToAsync(ms);
                                    imgData = ms.ToArray();
                                }
                            }

                            // Giải mã mảng byte thành Texture và lưu lại vào RAM Cache
                            if (imgData != null && imgData.Length > 0)
                            {
                                // Bắt buộc đẩy việc tạo đối tượng đồ họa OpenGL về Luồng chính (Main Thread)
                                Dispatcher.Dispatch(delegate
                                {
                                    try
                                    {
                                        // Tạo luồng MemoryStream an toàn từ mảng byte thô
                                        using (MemoryStream imageStream = new MemoryStream(imgData))
                                        {
                                            // Lúc này hàm chạy trên Luồng chính nên OpenGL Context sẽ hoạt động hoàn hảo!
                                            Texture2D texture = Texture2D.Load(imageStream, premultiplyAlpha: true);

                                            song.LoadedIcon = texture;
                                            ZTuneBubbleWidget?.SetIcon(song.LoadedIcon);

                                            Log.Information($"[{LogPrefix}] Successfully loaded and bound texture for: {song.Title}");
                                        }
                                    }
                                    catch (Exception glEx)
                                    {
                                        Log.Warning($"[{LogPrefix}] Error creating Texture2D on Main Thread: {glEx.Message}");
                                    }
                                });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Warning($"[{LogPrefix}] Failed to load/read song icon: {ex.Message}");
                    }
                }

                // --- TẢI VÀ CACHE FILE BÀI HÁT ---
                string songFileName = Storage.GetFileName(song.SongUrl);

                // Fallback an toàn nếu link nhạc có tham số mạng ẩn hoặc dị dạng
                if (string.IsNullOrEmpty(songFileName) || !songFileName.Contains(".") || HasInvalidFileNameChars(songFileName))
                {
                    int stableSongHash = GetDeterministicHashCode(song.SongUrl);

                    // Tự động phân tích và giữ lại đuôi nhạc gốc (.ogg hoặc .mp3) nếu có thể để Engine Audio nhận diện đúng định dạng
                    string extension = ".ogg";
                    if (!string.IsNullOrEmpty(songFileName) && songFileName.Contains(".mp3"))
                        extension = ".mp3";

                    songFileName = $"music_{stableSongHash}{extension}";
                }

                string filePath = Storage.CombinePaths(CacheDirectoryPath, songFileName);

                // Kiểm tra xem file đã có trong ổ cứng chưa
                if (!Storage.FileExists(filePath))
                {
                    Log.Information($"[{LogPrefix}] Downloading song: {song.Title}");

                    // Tải dữ liệu từ GitHub
                    byte[] musicData = await WebManager.GetAsync(song.SongUrl);

                    // Lưu file vào hệ thống Storage
                    using (Stream stream = Storage.OpenFile(filePath, OpenFileMode.Create))
                    {
                        await stream.WriteAsync(musicData, 0, musicData.Length);
                    }

                    Log.Information($"[{LogPrefix}] Download completed: {song.Title}");
                }

                // --- PHÁT NHẠC ---

                // Mở luồng đọc từ file Cache
                Stream readStream = Storage.OpenFile(filePath, OpenFileMode.Read);

                // Sử dụng SoundData.Stream để khởi tạo
                StreamingSource source = SoundData.Stream(readStream);

                // Lưu lại trạng thái Mix hiện tại trước khi cướp quyền
                if (MusicManager.CurrentMix != MusicManager.Mix.Other)
                    PreviousMix = MusicManager.CurrentMix;

                MusicManager.CurrentMix = MusicManager.Mix.Other;
                MusicManager.StopMusic();

                MusicManager.m_sound = new StreamingSound(
                    source,
                    MusicManager.Volume,
                    1f, 0f, false, true, 1f
                );

                MusicManager.m_sound.Play();

                CurrentPlayingSong = song;

                Log.Information($"[{LogPrefix}] Now playing: {song.Title}");
            }
            catch (Exception ex)
            {
                Log.Error($"[{LogPrefix}] Failed to play '{song.Title}': {ex.Message}");
            }
        }

        public static void StopMusic()
        {
            if (CurrentPlayingSong != null)
                MusicManager.StopMusic();
        }

        // Kiểm tra xem tên file có chứa ký tự cấm của hệ điều hành hay không
        public static bool HasInvalidFileNameChars(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return false;

            foreach (char c in Path.GetInvalidFileNameChars())
            {
                if (fileName.Contains(c))
                    return true;
            }

            return false;
        }


        // Thuật toán DJB2: Băm một chuỗi văn bản thành một số nguyên 32-bit cố định.
        // Kết quả trả về sẽ LUÔN LUÔN giống nhau giữa các lần chạy game (Bất kể Runtime/Hệ điều hành).
        public static int GetDeterministicHashCode(string str)
        {
            if (string.IsNullOrEmpty(str))
                return 0;

            // Sử dụng số Magic Number chuẩn của thuật toán DJB2
            uint hash = 5381;

            for (int i = 0; i < str.Length; i++)
            {
                // hash * 33 + c
                hash = ((hash << 5) + hash) + str[i];
            }

            // Lấy giá trị tuyệt đối để tránh số âm khi đặt tên file, 
            // và ép kiểu về int chuẩn C#
            return (int)(hash & 0x7FFFFFFF);
        }
    }
}
