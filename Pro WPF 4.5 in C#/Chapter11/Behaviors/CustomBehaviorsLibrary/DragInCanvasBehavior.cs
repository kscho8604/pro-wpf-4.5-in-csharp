using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Xaml.Behaviors; // .NET 10 표준 네임스페이스

namespace CustomBehaviorsLibrary
{
    public class DragInCanvasBehavior : Behavior<UIElement>
    {
        private Canvas canvas;

        protected override void OnAttached()
        {
            base.OnAttached();

            // 이벤트 핸들러 등록
            this.AssociatedObject.MouseLeftButtonDown += AssociatedObject_MouseLeftButtonDown;
            this.AssociatedObject.MouseMove += AssociatedObject_MouseMove;
            this.AssociatedObject.MouseLeftButtonUp += AssociatedObject_MouseLeftButtonUp;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();

            // 이벤트 핸들러 해제 (메모리 누수 방지)
            this.AssociatedObject.MouseLeftButtonDown -= AssociatedObject_MouseLeftButtonDown;
            this.AssociatedObject.MouseMove -= AssociatedObject_MouseMove;
            this.AssociatedObject.MouseLeftButtonUp -= AssociatedObject_MouseLeftButtonUp;
        }

        // 드래그 상태 추적
        private bool isDragging = false;

        // 클릭된 정확한 마우스 오프셋 저장
        private Point mouseOffset;

        private void AssociatedObject_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 부모 Canvas 찾기
            if (canvas == null) canvas = VisualTreeHelper.GetParent(this.AssociatedObject) as Canvas;

            // 드래그 모드 시작
            isDragging = true;

            // 요소 기준의 마우스 클릭 좌표 저장
            mouseOffset = e.GetPosition(AssociatedObject);

            // 마우스 캡처 (요소를 벗어나도 마우스 이벤트를 계속 수신하도록 설정)
            AssociatedObject.CaptureMouse();
        }

        private void AssociatedObject_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                // Canvas 기준의 마우스 현재 위치 가져오기
                Point point = e.GetPosition(canvas);

                // 마우스 오프셋을 계산하여 요소 위치 이동
                AssociatedObject.SetValue(Canvas.TopProperty, point.Y - mouseOffset.Y);
                AssociatedObject.SetValue(Canvas.LeftProperty, point.X - mouseOffset.X);
            }
        }

        private void AssociatedObject_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (isDragging)
            {
                // 마우스 캡처 해제 및 드래그 종료
                AssociatedObject.ReleaseMouseCapture();
                isDragging = false;
            }
        }
    }
}
