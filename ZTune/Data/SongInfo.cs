using Engine;
using Engine.Graphics;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace Game
{
    public class SongInfo
    {
        public string Title { get; set; }
        public string Author { get; set; }
        public string SongUrl { get; set; }
        public string IconUrl { get; set; }

        private Texture2D m_loadedIcon;

        [JsonIgnore]
        public Texture2D LoadedIcon
        {
            get => m_loadedIcon;
            set
            {
                if (m_loadedIcon != null && m_loadedIcon != value)
                    m_loadedIcon.Dispose();

                m_loadedIcon = value;
            }
        }

    }
}
