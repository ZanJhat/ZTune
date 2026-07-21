using Engine;
using Engine.Input;
using Engine.Graphics;
using Game;

namespace Game
{
    public class BubbleWidget : CanvasWidget
    {
        private Vector2? m_lastDragGlobalPos;
        private bool m_clickEnable;
        private bool m_isDragging;
        private int? m_trackingTouchId; // Khóa mục tiêu vào đúng 1 ID ngón tay duy nhất
        public bool IsClicked { get; private set; }

        public Vector2 BubblePosition;

        public RectangleWidget m_rectangleWidget;

        public override bool IsHitTestVisible => true;

        public BubbleWidget()
        {
            Size = new Vector2(64f);
            Margin = Vector2.Zero;
            BubblePosition = Vector2.Zero;

            Texture2D icon = null;

            if (ModsManager.GetModEntity("survivalcraft", out ModEntity modEntity))
                icon = modEntity.Icon;
            else
                icon = ContentManager.Get<Texture2D>("Textures/Gui/DefaultModIcon");

            m_rectangleWidget = new RectangleWidget
            {
                Name = "BubbleIcon",
                Subtexture = icon,
                FillColor = Color.White,
                OutlineColor = Color.Transparent
            };
            Children.Add(m_rectangleWidget);

            Reset();
        }

        public bool SetIcon(Texture2D texture)
        {
            if (texture == null || m_rectangleWidget == null)
                return false;

            m_rectangleWidget.Subtexture = texture;
            return true;
        }

        public override void Update()
        {
            IsClicked = false;

            // 1. NHẬN DIỆN CHẠM VÀO BONG BÓNG
            if (!m_isDragging)
            {
                bool touchFound = false;

                // A. Ưu tiên kiểm tra danh sách cảm ứng đa điểm (Trên điện thoại)
                foreach (TouchLocation touch in Touch.TouchLocations)
                {
                    if (touch.State == TouchLocationState.Pressed && HitTestPanel(touch.Position))
                    {
                        m_trackingTouchId = touch.Id; // Lưu ID ngón tay
                        m_lastDragGlobalPos = touch.Position;
                        m_isDragging = true;
                        m_clickEnable = true;
                        touchFound = true;
                        break;
                    }
                }

                // B. Fallback: Nếu không có Touch, hỗ trợ nhấp Chuột (Trên PC)
                if (!touchFound && Input.Tap.HasValue && HitTestPanel(Input.Tap.Value))
                {
                    m_trackingTouchId = -1; // Dùng ID = -1 để đại diện cho Chuột
                    m_lastDragGlobalPos = Input.Tap.Value;
                    m_isDragging = true;
                    m_clickEnable = true;
                }
            }

            // 2. XỬ LÝ KHI ĐANG KÉO (Cho ngón tay đã lưu)
            if (m_isDragging && m_lastDragGlobalPos.HasValue)
            {
                Vector2? currentGlobalPos = null;
                bool isReleased = false;

                if (m_trackingTouchId == -1)
                {
                    // Đang dùng chuột PC
                    if (Input.Press.HasValue)
                        currentGlobalPos = Input.Press.Value;
                    else
                        isReleased = true;
                }
                else if (m_trackingTouchId.HasValue)
                {
                    // Đang dùng cảm ứng: CHỈ tìm đúng ID đã khóa
                    bool touchStillActive = false;
                    foreach (TouchLocation touch in Touch.TouchLocations)
                    {
                        if (touch.Id == m_trackingTouchId.Value)
                        {
                            touchStillActive = true;
                            if (touch.State == TouchLocationState.Released)
                                isReleased = true;
                            else
                                currentGlobalPos = touch.Position; // Cập nhật đúng tọa độ của ngón này
                            break;
                        }
                    }

                    // Nếu ngón tay bị hệ thống ngắt đột ngột (VD: vuốt ra khỏi màn hình)
                    if (!touchStillActive)
                        isReleased = true;
                }

                // ÁP DỤNG DI CHUYỂN
                if (currentGlobalPos.HasValue)
                {
                    Vector2 screenDelta = currentGlobalPos.Value - m_lastDragGlobalPos.Value;

                    if (screenDelta.Length() >= 5f || !m_clickEnable)
                    {
                        m_clickEnable = false;

                        Vector2 deltaMove = screenDelta;
                        if (ParentWidget != null)
                        {
                            Vector2 currentLocal = ParentWidget.ScreenToWidget(currentGlobalPos.Value);
                            Vector2 lastLocal = ParentWidget.ScreenToWidget(m_lastDragGlobalPos.Value);
                            deltaMove = currentLocal - lastLocal;
                        }

                        BubblePosition += deltaMove;
                        UpdateBubbleTransform();
                        m_lastDragGlobalPos = currentGlobalPos.Value;
                    }
                }
                else if (isReleased)
                {
                    // 3. XỬ LÝ KHI BUÔNG TAY
                    if (m_clickEnable)
                    {
                        IsClicked = true;
                        AudioManager.PlaySound("Audio/UI/ButtonClick", 1f, 0f, 0f);
                    }

                    // RESET để sẵn sàng cho lần chạm tiếp theo
                    m_lastDragGlobalPos = null;
                    m_clickEnable = true;
                    m_isDragging = false;
                    m_trackingTouchId = null;
                }
            }
            else
            {
                UpdateBubbleTransform();
            }
        }

        // Tránh tràn khỏi Parent
        private void UpdateBubbleTransform()
        {
            if (ParentWidget != null)
            {
                Vector2 screenSize = ParentWidget.ActualSize;
                Vector2 size = ActualSize;

                float maxX = MathUtils.Max(0f, screenSize.X - size.X);
                float maxY = MathUtils.Max(0f, screenSize.Y - size.Y);

                BubblePosition.X = MathUtils.Clamp(BubblePosition.X, 0f, maxX);
                BubblePosition.Y = MathUtils.Clamp(BubblePosition.Y, 0f, maxY);
            }

            LayoutTransform = Matrix.CreateTranslation(BubblePosition.X, BubblePosition.Y, 0f);
        }

        // Hàm kiểm tra ngón tay có chạm trúng bong bóng không
        private bool HitTestPanel(Vector2 position)
        {
            bool found = false;
            HitTestGlobal(position, delegate (Widget widget)
            {
                found = widget.IsChildWidgetOf(this) || widget == this;
                return true;
            });
            return found;
        }

        public void Reset()
        {
            m_lastDragGlobalPos = null;
            m_clickEnable = true;
            m_isDragging = false;
            m_trackingTouchId = null;
            IsClicked = false;
        }
    }
}
