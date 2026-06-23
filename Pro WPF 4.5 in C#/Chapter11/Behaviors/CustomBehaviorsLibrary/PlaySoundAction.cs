using System;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Xaml.Behaviors; // 현대적인 네임스페이스만 사용
using System.Windows;

namespace CustomBehaviorsLibrary
{
    // .NET 10 / Xaml.Behaviors 표준에 맞춰 불필요하고 에러를 유발하는 [DefaultTrigger] 속성은 제거합니다.
    public class PlaySoundAction : TriggerAction<FrameworkElement>
    {
        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register("Source", typeof(Uri),
            typeof(PlaySoundAction), new PropertyMetadata(null));

        public Uri Source
        {
            get { return (Uri)GetValue(PlaySoundAction.SourceProperty); }
            set { SetValue(PlaySoundAction.SourceProperty, value); }
        }

        protected override void Invoke(object args)
        {
            // MediaElement를 삽입할 컨테이너를 찾습니다.
            Panel container = FindContainer();

            if (container != null)
            {
                // MediaElement 생성 및 설정
                MediaElement media = new MediaElement();
                media.Source = this.Source;

                // 재생이 끝나거나 실패하면 컨테이너에서 제거하는 이벤트 연결
                media.MediaEnded += delegate
                {
                    container.Children.Remove(media);
                };

                media.MediaFailed += delegate
                {
                    container.Children.Remove(media);
                };

                // MediaElement를 추가하고 재생을 시작합니다.
                container.Children.Add(media);
            }
        }

        private Panel FindContainer()
        {
            FrameworkElement element = this.AssociatedObject;

            // MediaElement를 넣을 수 있는 Panel을 찾을 때까지 Visual Tree를 거슬러 올라갑니다.
            while (element != null)
            {
                if (element is Panel) return (Panel)element;

                element = VisualTreeHelper.GetParent(element) as FrameworkElement;
            }
            return null;
        }
    }
}
