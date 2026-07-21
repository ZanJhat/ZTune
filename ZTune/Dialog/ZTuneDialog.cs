using Engine;
using Engine.Graphics;
using Engine.Media;
using Engine.Serialization;
using GameEntitySystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using TemplatesDatabase;
using System.IO;
using System.Text;
using XmlUtilities;

namespace Game
{
    public class ZTuneDialog : Dialog
    {
        public enum State
        {
            Now,
            Songs,
            Settings
        }

        public ScrollPanelWidget m_scrollPanel;
        public StackPanelWidget m_contentsPanel;

        public ButtonWidget m_nowButton;
        public ButtonWidget m_songsButton;
        public ButtonWidget m_settingsButton;
        public ButtonWidget m_closeButton;

        public State m_state;

        public ZTuneDialog()
        {
            XElement node = ContentManager.Get<XElement>("Dialogs/ZTuneDialog");
            LoadContents(this, node);
            m_scrollPanel = Children.Find<ScrollPanelWidget>("ScrollPanel");
            m_contentsPanel = Children.Find<StackPanelWidget>("ContentsPanel");

            m_nowButton = Children.Find<ButtonWidget>("NowButton");
            m_songsButton = Children.Find<ButtonWidget>("SongsButton");
            m_settingsButton = Children.Find<ButtonWidget>("SettingsButton");
            m_closeButton = Children.Find<ButtonWidget>("CloseButton");

            if (ZTuneManager.CurrentPlayingSong == null)
                UpdateState(State.Songs);
            else
                UpdateState(State.Now);
        }

        public override void Update()
        {
            if (m_nowButton.IsClicked)
            {
                UpdateState(State.Now);
            }

            if (m_songsButton.IsClicked)
            {
                UpdateState(State.Songs);
            }

            if (m_settingsButton.IsClicked)
            {
                UpdateState(State.Settings);
            }

            if (m_closeButton.IsClicked)
                DialogsManager.HideDialog(this);
        }

        public void UpdateState(State state)
        {
            m_state = state;
            m_contentsPanel.Children.Clear();

            if (m_state == State.Now)
            {
                UpdateStateNow();
            }
            else if (m_state == State.Songs)
            {
                UpdateStateSongs();
            }
            else if (m_state == State.Settings)
            {
                UpdateStateSettings();
            }

            m_scrollPanel.ScrollPosition = 0f;
        }

        public void UpdateStateNow()
        {

        }

        public void UpdateStateSongs()
        {
            foreach (SongInfo song in ZTuneManager.AvailableSongs)
            {
                SongItemWidget songWidget = new SongItemWidget();
                songWidget.SetSongData(song);
                m_contentsPanel.Children.Add(songWidget);
            }
        }

        public void UpdateStateSettings()
        {

        }
    }
}
