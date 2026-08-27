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
            this.AssociatedObject.MouseWheel += AssociatedObject_MouseWheel;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();

            // 이벤트 핸들러 해제 (메모리 누수 방지)
            this.AssociatedObject.MouseLeftButtonDown -= AssociatedObject_MouseLeftButtonDown;
            this.AssociatedObject.MouseMove -= AssociatedObject_MouseMove;
            this.AssociatedObject.MouseLeftButtonUp -= AssociatedObject_MouseLeftButtonUp;
            this.AssociatedObject.MouseWheel -= AssociatedObject_MouseWheel;
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
        private void AssociatedObject_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            // 이벤트가 부모 컨트롤(Canvas 등)로 전파되어 화면이 스크롤되는 것을 방지
            e.Handled = true;

            var element = this.AssociatedObject as FrameworkElement;
            if (element == null) return;

            // 회전 중심점을 요소의 정중앙(0.5, 0.5)으로 설정
            element.RenderTransformOrigin = new Point(0.5, 0.5);

            // 기존 RenderTransform이 RotateTransform인지 확인하고, 없으면 새로 생성
            var rotateTransform = element.RenderTransform as RotateTransform;
            if (rotateTransform == null)
            {
                rotateTransform = new RotateTransform(0);
                element.RenderTransform = rotateTransform;
            }

            // 휠 방향에 따른 회전 각도 설정 (한 번 돌릴 때마다 15도씩 회전)
            // e.Delta > 0 이면 시계 방향(+15), 아니면 반시계 방향(-15)
            double angleDelta = e.Delta > 0 ? 15 : -15;

            // 새로운 각도 적용
            rotateTransform.Angle += angleDelta;

            // 각도가 너무 커지거나 작아지지 않도록 0~360도 사이로 보정 (선택 사항)
            rotateTransform.Angle %= 360;
        }
    }
}
