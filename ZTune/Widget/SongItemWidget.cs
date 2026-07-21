using System.Xml.Linq;
using Engine;

namespace Game
{
    public class SongItemWidget : CanvasWidget
    {
        public RectangleWidget m_iconRectangle;
        public LabelWidget m_titleLabel;
        public LabelWidget m_authorLabel;

        public SongInfo SongData { get; private set; }
        public Subtexture m_defaultIcon;

        public SongItemWidget()
        {
            XElement node = ContentManager.Get<XElement>("Widgets/SongItemWidget");
            LoadContents(this, node);

            m_iconRectangle = Children.Find<RectangleWidget>("IconRectangle");
            m_titleLabel = Children.Find<LabelWidget>("TitleLabel");
            m_authorLabel = Children.Find<LabelWidget>("AuthorLabel");

            m_defaultIcon = m_iconRectangle.Subtexture;
        }

        public override void Update()
        {
        }

        public void SetSongData(SongInfo song)
        {
            SongData = song;

            if (SongData != null)
            {
                if (SongData.LoadedIcon != null)
                    m_iconRectangle.Subtexture = SongData.LoadedIcon;

                m_titleLabel.Text = SongData.Title;
                m_authorLabel.Text = SongData.Author;
            }
            else
            {
                m_iconRectangle.Subtexture = m_defaultIcon;
                m_titleLabel.Text = "";
                m_authorLabel.Text = "";
            }
        }
    }
}
